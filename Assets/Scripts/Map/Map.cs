using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

public class Map : MonoBehaviour
{
    [SerializeField] GameObject backgroundPrefab;
    [SerializeField] GameObject buildingPrefab;
    [SerializeField] GameObject roadPrefab;

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

        // Calculate horizontal shift, vertical shift and scale required to convert GPS coords into scene point
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
            // Deal with buildings (but only ways and not relations)
            if (IsBuilding(element))
            {
                // Get scene positions for each building vertex using GPS coords
                Vector2[] vertices = GetPointsFromGPSCoords(element.geometry, xShift, yShift, scale);

                // Create and add building to scene
                AddBuildingToScene(vertices);
            }
            // Deal with roads
            else if (IsRoad(element))
            {
                // Get scene positions for each road node using GPS coords
                Vector2[] nodes = GetPointsFromGPSCoords(element.geometry, xShift, yShift, scale);

                // Create and add road to scene
                AddRoadToScene(nodes);
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
        return element.type == "way" && element.tags.building != null;
    }

    bool IsRoad(MapElement element)
    {
        return element.type == "way" && element.tags.highway != null && !IsPath(element);
    }

    bool IsPath(MapElement element)
    {
        return element.tags.highway == "footway" ||
               element.tags.highway == "pedestrian" ||
               element.tags.highway == "steps";
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
}
