using System;
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
    [SerializeField] GameObject stepsPrefab;
    [SerializeField] GameObject grassPrefab;
    [SerializeField] GameObject waterPrefab;

    void Start()
    {
        StartCoroutine(LoadMapFromBoundingBox(51.453232, -2.612708, 51.461628, -2.588997));
    }

    IEnumerator LoadMapFromBoundingBox(double lowLat, double lowLong, double highLat, double highLong)
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
        double scale = 80000;

        // Add background to scene (covers whole map)
        // Note: We specify the first map corner twice since AddWayToScene expects the last vertex to be the same as the first
        float halfMapWidth = (float)((highLong - lowLong) * scale / 2);
        float halfMapHeight = (float)((LatToY(highLat) - LatToY(lowLat)) * scale / 2);
        Vector2[] mapCorners = { new Vector2(-halfMapWidth, halfMapHeight), new Vector2(halfMapWidth, halfMapHeight), new Vector2(halfMapWidth, -halfMapHeight), new Vector2(-halfMapWidth, -halfMapHeight), new Vector2(-halfMapWidth, halfMapHeight) };
        AddWayToScene(mapCorners, backgroundPrefab, false, false);

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
                    AddWayToScene(vertices, buildingPrefab, false, false);

                // Create and add road to scene
                else if (IsRoad(element))
                    AddWayToScene(vertices, roadPrefab, true, true, 5.0f);

                // Create and add path to scene
                else if (IsPath(element))
                    AddWayToScene(vertices, pathPrefab, true, true, 2.0f);

                // Create and add steps to scene
                else if (IsSteps(element))
                    AddWayToScene(vertices, stepsPrefab, true, false, 1.0f);

                // Create and add grass to scene
                else if (IsGrass(element))
                    AddWayToScene(vertices, grassPrefab, false, false);

                // Create and add water to scene
                else if (IsWater(element))
                    AddWayToScene(vertices, waterPrefab, false, false);
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
                                AddWayToScene(vertices, buildingPrefab, false, false);

                            // Create and add building hole to scene
                            else if (member.role == "inner")
                                AddWayToScene(vertices, buildingHolePrefab, false, false);
                        }
                    }
                }
            }
        }
    }

    double LatToY(double latitude)
    {
        return Math.Log(Math.Tan((latitude + 90) / 360 * Math.PI)) / Math.PI * 180;
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
        return element.tags.highway != null && !IsPath(element) && !IsSteps(element);
    }

    bool IsPath(MapElement element)
    {
        return element.tags.highway == "footway" ||
               element.tags.highway == "pedestrian";
    }

    bool IsSteps(MapElement element)
    {
        return element.tags.highway == "steps";
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

    void AddWayToScene(Vector2[] vertices, GameObject prefab, bool isOpenEnded, bool convertToCloseEnded, float thickness = 1.0f)
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

        if (!isOpenEnded) // close-ended
        {
            // Reverse order if anti-clockwise (so that sprite shape is drawn correctly)
            if (!PolygonIsClockwise(vertices))
                Array.Reverse(vertices);

            // Add way vertices to sprite shape
            spline.Clear();
            for (int i = 0; i < numVertices; i++)
            {
                // Add point to sprite shape
                spline.InsertPointAt(i, vertices[i]);
                spline.SetTangentMode(i, tangentMode);
            }
        }
        else if (convertToCloseEnded) // open-ended but to be converted to close-ended
        {
            try
            {
                // Generate close-ended way from open-ended line and add the way vertices to sprite shape
                CreateCloseEndedWayFromOpenEndedLine(vertices, spline, tangentMode, thickness);
            }
            catch (ArgumentException e)
            {
                Debug.Log("Error adding point to spline: " + e.Message);
                spline.Clear(); // Remove any points that were successfully added
                return;
            }
        }
        else // open-ended
        {
            // Add way vertices to sprite shape
            spline.Clear();
            for (int i = 0; i < numVertices; i++)
            {
                // Add point to sprite shape
                spline.InsertPointAt(i, vertices[i]);
                spline.SetTangentMode(i, tangentMode);
                spline.SetHeight(i, thickness);
            }
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

    void CreateCloseEndedWayFromOpenEndedLine(Vector2[] vertices, Spline spline, ShapeTangentMode tangentMode, float thickness)
    {
        int numVertices = vertices.Length;

        // TEMP - DELETE THIS!
        Color[] colours = { Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.black };
        int colourIndex = 0;

        spline.Clear();

        Vector2 dirToPrev;
        Vector2 dirToNext = (vertices[1] - vertices[0]).normalized;
        Vector2 dirPerpToLine = Vector2.Perpendicular(dirToNext).normalized;
        Vector2 offsetFromLine = dirPerpToLine * thickness / 2;

        Vector2 point1 = vertices[0] + offsetFromLine;
        Vector2 point2 = vertices[0] - offsetFromLine;

        // Insert point 1
        spline.InsertPointAt(0, point1);
        spline.SetTangentMode(0, tangentMode);

        // TEMP - DELETE THIS!
        Vector2 temp1 = point1;
        Vector2 temp2 = point2;

        Vector2 prevOffsetFromLine = offsetFromLine;

        Vector2[] otherLine = new Vector2[numVertices];

        // Store point 2
        otherLine[numVertices - 1] = point2;

        for (int i = 1; i < numVertices - 1; i++)
        {
            dirToPrev = (vertices[i - 1] - vertices[i]).normalized;
            dirToNext = (vertices[i + 1] - vertices[i]).normalized;
            dirPerpToLine = (dirToPrev + dirToNext).normalized;
            offsetFromLine = dirPerpToLine * thickness / 2;

            // If offset is pointing in opposite direction to previous offset, then flip it to prevent the two lines crossing over
            float angle = Vector2.SignedAngle(offsetFromLine, prevOffsetFromLine);
            if (angle < -90 || angle > 90) offsetFromLine = -offsetFromLine;
            prevOffsetFromLine = offsetFromLine;

            point1 = vertices[i] + offsetFromLine;
            point2 = vertices[i] - offsetFromLine;

            // Insert point 1
            spline.InsertPointAt(i, point1);
            spline.SetTangentMode(i, tangentMode);

            // Store point 2
            int index = numVertices - i - 1;
            otherLine[index] = point2;

            // TEMP - DELETE THIS!
            Color colour = colours[colourIndex];
            colourIndex++;
            colourIndex %= colours.Length;
            Debug.DrawLine(vertices[i - 1], vertices[i], colour, 1000000000, false);
            Debug.DrawLine(temp1, point1, colour, 1000000000, false);
            Debug.DrawLine(temp2, point2, colour, 1000000000, false);
            temp1 = point1;
            temp2 = point2;
        }

        dirToPrev = (vertices[numVertices - 1] - vertices[numVertices - 2]).normalized;
        dirPerpToLine = Vector2.Perpendicular(dirToPrev).normalized;
        offsetFromLine = dirPerpToLine * thickness / 2;

        // If offset is pointing in opposite direction to previous offset, then flip it to prevent the two lines crossing over
        float angle2 = Vector2.SignedAngle(offsetFromLine, prevOffsetFromLine);
        if (angle2 < -90 || angle2 > 90) offsetFromLine = -offsetFromLine;

        point1 = vertices[numVertices - 1] + offsetFromLine;
        point2 = vertices[numVertices - 1] - offsetFromLine;

        // TEMP - DELETE THIS!
        Color colour2 = colours[colourIndex];
        colourIndex++;
        colourIndex %= colours.Length;
        Debug.DrawLine(vertices[numVertices - 2], vertices[numVertices - 1], colour2, 1000000000, false);
        Debug.DrawLine(temp1, point1, colour2, 1000000000, false);
        Debug.DrawLine(temp2, point2, colour2, 1000000000, false);

        // Insert point 1
        spline.InsertPointAt(numVertices - 1, point1);
        spline.SetTangentMode(numVertices - 1, tangentMode);

        // Store point 2
        otherLine[0] = point2;

        // Insert all points for other line
        for (int i = 0; i < numVertices; i++)
        {
            int splineIndex = numVertices + i;
            spline.InsertPointAt(splineIndex, otherLine[i]);
            spline.SetTangentMode(splineIndex, tangentMode);
        }
    }

    // This method uses the sign of the signed polygon area to determine if clockwise
    bool PolygonIsClockwise(Vector2[] vertices)
    {
        float sum = 0.0f;
        Vector2 v1 = vertices[vertices.Length - 1];

        foreach (Vector2 v2 in vertices)
        {
            sum += (v1[0] * v2[1]) - (v2[0] * v1[1]);
            v1 = v2;
        }

        // Sum is negative if clockwise
        return sum < 0.0f;
    }
}
