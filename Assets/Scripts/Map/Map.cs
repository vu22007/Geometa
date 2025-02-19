using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

public class Map : MonoBehaviour
{
    [SerializeField] GameObject backgroundPrefab;
    [SerializeField] GameObject buildingPrefab;
    [SerializeField] GameObject buildingHolePrefab;
    [SerializeField] GameObject roadPrefab;
    [SerializeField] GameObject pathPrefab;
    [SerializeField] GameObject grassPrefab;
    [SerializeField] GameObject waterPrefab;

    void Start()
    {
        StartCoroutine(LoadMapFromBoundingBox(51.453990, -2.605788, 51.456203, -2.598647));
    }

    IEnumerator LoadMapFromBoundingBox(double lowLat, double lowLong, double highLat, double highLong)
    {
        // Construct request body
        // Note: We are fetching buildings (both as ways and as relations) and roads
        string data = "data=" + UnityWebRequest.EscapeURL("" +
            $"[out:json][timeout:25][bbox:{lowLat},{lowLong},{highLat},{highLong}];" +
            "(" +
            "  way[building];" +
            "  rel[building];" +
            "  way[highway];" +
            "  way[landuse=grass];" +
            "  way[natural=wood];" +
            "  way[leisure=park];" +
            "  way[leisure=garden];" +
            "  way[leisure=nature_reserve];" +
            "  way[natural=water];" +
            ");" +
            "out geom;");

        // Send API request
        UnityWebRequest webRequest = UnityWebRequest.Post("https://overpass-api.de/api/interpreter", data, "application/json");
        yield return webRequest.SendWebRequest();

        // Check for unsuccessful response
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(webRequest.error);
            yield break;
        }

        // Get JSON response
        string jsonResponse = webRequest.downloadHandler.text;

        // Remove colons from required tags
        jsonResponse = jsonResponse.Replace("building:levels", "buildingLevels");

        // Parse JSON
        MapData mapData = JsonUtility.FromJson<MapData>(jsonResponse);

        // Calculate horizontal shift, vertical shift and scale required to convert GPS coords into world space
        double xShift = lowLong + (highLong - lowLong) / 2;
        double yShift = LatToY(lowLat) + (LatToY(highLat) - LatToY(lowLat)) / 2;
        double scale = 80000;

        // Add background to scene (scaled to cover whole map)
        GameObject background = Instantiate(backgroundPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);
        double xScale = (highLong - lowLong) * scale;
        double yScale = (LatToY(highLat) - LatToY(lowLat)) * scale;
        background.transform.localScale = new Vector3((float)xScale, (float)yScale, 1);

        // Add map elements to scene
        foreach (MapElement element in mapData.elements)
        {
            // Deal with ways
            if (element.type == "way")
            {
                // Get points in world space from GPS coords
                Vector2[] points = GetPointsFromGPSCoords(element.geometry, xShift, yShift, scale);

                // Deal with buildings (but only ways and not relations)
                if (IsBuilding(element))
                {
                    // Create and add building to scene
                    AddBuildingToScene(points);
                }
                // Deal with roads
                else if (IsRoad(element))
                {
                    // Create and add road to scene
                    AddRoadToScene(points);
                }
                else if (IsPath(element))
                {
                    // Create and add path to scene
                    AddPathToScene(points);
                }
                else if (IsGrass(element))
                {
                    // Create and add grass to scene
                    AddGrassToScene(points);
                }
                else if (IsWater(element))
                {
                    // Create and add water to scene
                    AddWaterToScene(points);
                }
            }
            // Deal with relations
            else if (element.type == "relation")
            {
                if (IsBuilding(element))
                {
                    // Building relations have an "outer" way and a number of "inner" ways (holes in the polygon)
                    foreach (MapElement.RelationMember member in element.members)
                    {
                        if (member.type == "way")
                        {
                            // Get points in world space from GPS coords
                            Vector2[] points = GetPointsFromGPSCoords(member.geometry, xShift, yShift, scale);

                            if (member.role == "outer")
                            {
                                // Create and add building to scene
                                AddBuildingToScene(points);
                            }
                            else if (member.role == "inner")
                            {
                                // Create and add building hole to scene
                                AddBuildingHoleToScene(points);
                            }
                        }
                    }
                }
            }
        }
    }

    double LatToY(double latitude)
    {
        return System.Math.Log(System.Math.Tan(
            (latitude + 90) / 360 * System.Math.PI
        )) / System.Math.PI * 180;
    }

    Vector2[] GetPointsFromGPSCoords(MapElement.Coords[] geometry, double xShift, double yShift, double scale)
    {
        Vector2[] points = new Vector2[geometry.Length];
        for (int i = 0; i < points.Length; i++)
        {
            MapElement.Coords coords = geometry[i];
            double xPos = (coords.lon - xShift) * scale;
            double yPos = (LatToY(coords.lat) - yShift) * scale;
            points[i] = new Vector2((float)xPos, (float)yPos);
        }
        return points;
    }

    bool IsBuilding(MapElement element)
    {
        return element.tags.building != null;
    }

    bool IsRoad(MapElement element)
    {
        return element.tags.highway != null && !IsPath(element);
    }

    bool IsPath(MapElement element)
    {
        return element.tags.highway == "footway" ||
               element.tags.highway == "pedestrian" ||
               element.tags.highway == "steps";
    }

    bool IsGrass(MapElement element)
    {
        return element.tags.landuse == "grass" ||
               element.tags.natural == "wood" ||
               element.tags.leisure == "park" ||
               element.tags.leisure == "garden" ||
               element.tags.leisure == "nature_reserve";
    }

    bool IsWater(MapElement element)
    {
        return element.tags.natural == "water";
    }

    void AddBuildingToScene(Vector2[] vertices)
    {
        // Instantiate building from prefab with the map as the parent
        GameObject building = Instantiate(buildingPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = building.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add building vertices to sprite shape (ignore last vertex since it is the same as the first)
        spline.Clear();
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, vertices[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
        }
    }

    void AddBuildingHoleToScene(Vector2[] vertices)
    {
        // Instantiate building hole from prefab with the map as the parent
        GameObject hole = Instantiate(buildingHolePrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = hole.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add building hole vertices to sprite shape (ignore last vertex since it is the same as the first)
        spline.Clear();
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, vertices[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
        }
    }

    void AddRoadToScene(Vector2[] nodes)
    {
        // Instantiate road from prefab with the map as the parent
        GameObject road = Instantiate(roadPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = road.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add road nodes to sprite shape
        spline.Clear();
        for (int i = 0; i < nodes.Length; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, nodes[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetHeight(i, 4.0f); // Thickness of road
        }
    }

    void AddPathToScene(Vector2[] nodes)
    {
        // Instantiate path from prefab with the map as the parent
        GameObject path = Instantiate(pathPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = path.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add path nodes to sprite shape
        spline.Clear();
        for (int i = 0; i < nodes.Length; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, nodes[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetHeight(i, 1.0f); // Thickness of path
        }
    }

    void AddGrassToScene(Vector2[] vertices)
    {
        // Instantiate grass from prefab with the map as the parent
        GameObject grass = Instantiate(grassPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = grass.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add grass vertices to sprite shape (ignore last vertex since it is the same as the first)
        spline.Clear();
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, vertices[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
        }
    }

    void AddWaterToScene(Vector2[] vertices)
    {
        // Instantiate water from prefab with the map as the parent
        GameObject water = Instantiate(waterPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = water.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // Add water vertices to sprite shape (ignore last vertex since it is the same as the first)
        spline.Clear();
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, vertices[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
        }
    }
}
