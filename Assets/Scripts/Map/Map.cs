using GLTFast;
using System.Collections;
using System.IO;
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
    RunBlenderScript buildingsGenerator;
    private string outputFilePath;

    void Start()
    {
        // Filepath where the glb file got outputed
        // outputFilePath = Path.Combine("/", "home", "qc22435", "Documents", "Buildify3DBuildings.glb");
        outputFilePath = Path.Combine(Application.persistentDataPath, "Buildify3DBuildings.glb");

        if (CoordinatesDataHolder.Instance == null)
        {
            Debug.LogError("Coordinates aren't valid");
            //Debug.Log("Using default values for Map");
            GenerateMap(51.4585, 51.4590, -2.6026, -2.6016);
        }
        else
        {
            double lowLat = CoordinatesDataHolder.Instance.Float1;
            double highLat = CoordinatesDataHolder.Instance.Float2;
            double lowLong = CoordinatesDataHolder.Instance.Float3;
            double highLong = CoordinatesDataHolder.Instance.Float4;
            Debug.Log("Generating map for: " + lowLat + ", " + highLat + ", " + lowLong + ", " + highLong);
            GenerateMap(lowLat, highLat, lowLong, highLong);
        }
    }

    public void GenerateMap(double lowLat, double highLat, double lowLong, double highLong)
    {
        // Create 2D map
        StartCoroutine(LoadMapFromBoundingBox(lowLat, highLat, lowLong, highLong));
        // Instantiate the 3D buildings
        ImportGLTF(outputFilePath);
        // Create 3D buildings
        // StartCoroutine(buildingsGenerator.RunBlender(lowLat, highLat, lowLong, highLong));
    }

    public async void ImportGLTF(string outputFilePath)
    {
        // Create an importer instanse
        GltfImport gltfImport = new GLTFast.GltfImport();

        bool success = await gltfImport.Load(outputFilePath);
        if (!success)
        {
            Debug.LogError("Failed to load glTF file from: " + outputFilePath);
            return;
        }

        GameObject gltfInstance = new GameObject("glTF instance");
        await gltfImport.InstantiateSceneAsync(gltfInstance.transform);
        gltfInstance.transform.eulerAngles = new Vector3(90, 180, 0);

        Debug.Log("glTF file loaded.");
    }

    IEnumerator LoadMapFromBoundingBox(double lowLat, double highLat, double lowLong, double highLong)
    {
        // Construct request body
        // Note: We are fetching buildings (both as ways and as relations), roads, paths, grass and water
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
        // double scale = 80000;
        double midLat = (lowLat + highLat) / 2;
        double scale = Mathf.Cos((float)midLat * Mathf.Deg2Rad) * 111319.488;

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
                // Get way vertices in world space from GPS coords
                Vector2[] vertices = GetPointsFromGPSCoords(element.geometry, xShift, yShift, scale);

                // Create and add building to scene (but only if it is a way and not a relation)
                if (IsBuilding(element))
                    AddWayToScene(vertices, buildingPrefab, false);

                // Create and add road to scene
                else if (IsRoad(element))
                    AddWayToScene(vertices, roadPrefab, true, 4.0f);

                // Create and add path to scene
                else if (IsPath(element))
                    AddWayToScene(vertices, pathPrefab, true, 2.0f);

                // Create and add grass to scene
                else if (IsGrass(element))
                    AddWayToScene(vertices, grassPrefab, false);

                // Create and add water to scene
                else if (IsWater(element))
                    AddWayToScene(vertices, waterPrefab, false);
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
                            Vector2[] vertices = GetPointsFromGPSCoords(member.geometry, xShift, yShift, scale);

                            // Create and add building to scene
                            if (member.role == "outer")
                                AddWayToScene(vertices, buildingPrefab, false);

                            // Create and add building hole to scene
                            else if (member.role == "inner")
                                AddWayToScene(vertices, buildingHolePrefab, false);
                        }
                    }
                }
            }
        }
    }

    double LatToY(double latitude)
    {
        return System.Math.Log(System.Math.Tan(
            (latitude + 90d) / 360d * System.Math.PI
        )) / System.Math.PI * 180d;
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

    void AddWayToScene(Vector2[] vertices, GameObject prefab, bool isOpenEnded, float thickness = 1.0f)
    {
        // Instantiate way from prefab with the map as the parent
        GameObject way = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

        // Get components
        SpriteShapeController spriteShapeController = way.GetComponent<SpriteShapeController>();
        Spline spline = spriteShapeController.spline;

        // If the way is close-ended, we need to ignore the last vertex since it is the same as the first
        int numVertices = isOpenEnded ? vertices.Length : vertices.Length - 1;

        // If the way is open-ended (e.g. a road or path) then make it smoother by setting the tangent mode to continuous
        ShapeTangentMode tangentMode = isOpenEnded ? ShapeTangentMode.Continuous : ShapeTangentMode.Linear;

        // Add way vertices to sprite shape
        spline.Clear();
        for (int i = 0; i < numVertices; i++)
        {
            // Add point to sprite shape
            spline.InsertPointAt(i, vertices[i]);
            spline.SetTangentMode(i, tangentMode);

            // Set thickness of way at this point (if open-ended e.g. for a road or path but not for a building)
            if (isOpenEnded)
                spline.SetHeight(i, thickness);
        }

        // The above code creates sprite shapes that have their origin at the centre of the world, and this causes them to not be loaded
        // until the centre of the world is on-screen, so we need to set the bounding volume of the sprite shape geometry to ensure that
        // the sprite shape is visible at all times
        SetSpriteShapeBoundingVolume(spriteShapeController);
    }

    void SetSpriteShapeBoundingVolume(SpriteShapeController spriteShapeController)
    {
        Spline spline = spriteShapeController.spline;
        Bounds localBounds = new Bounds(spline.GetPosition(0), Vector2.zero);
        for (int i = 1; i < spline.GetPointCount(); i++)
        {
            Vector3 pos = spline.GetPosition(i);
            localBounds.Encapsulate(pos);
        }
        spriteShapeController.spriteShapeRenderer.SetLocalAABB(localBounds);
    }
}
