using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;
using static MapElement;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class Map : MonoBehaviour
{
    [SerializeField] GameObject backgroundPrefab;
    [SerializeField] GameObject mapBorderPrefab;
    [SerializeField] GameObject buildingPrefab;
    [SerializeField] GameObject buildingHolePrefab;
    [SerializeField] GameObject roadPrefab;
    [SerializeField] GameObject pathPrefab;
    [SerializeField] GameObject stepsPrefab;
    [SerializeField] GameObject grassPrefab;
    [SerializeField] GameObject waterPrefab;
    [SerializeField] GameObject wallPrefab;

    void Start()
    {
        StartCoroutine(LoadMapFromBoundingBox(51.453232, -2.612708, 51.461628, -2.588997));
    }

    IEnumerator LoadMapFromBoundingBox(double lowLat, double lowLong, double highLat, double highLong)
    {
        // Construct request body
        // Note: We are fetching buildings (both as ways and as relations), roads, paths, steps, grass, water, walls and fences
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
            "  way[barrier=wall];" +
            "  way[barrier=retaining_wall];" +
            "  way[barrier=fence];" +
            "  node[barrier=gate];" +
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
        float backgroundMargin = 100f;
        Vector2[] backgroundVertices = { mapCorners[0] + new Vector2(-backgroundMargin, backgroundMargin), mapCorners[1] + new Vector2(backgroundMargin, backgroundMargin), mapCorners[2] + new Vector2(backgroundMargin, -backgroundMargin), mapCorners[3] + new Vector2(-backgroundMargin, -backgroundMargin), mapCorners[4] + new Vector2(-backgroundMargin, backgroundMargin) };
        AddWayToScene(backgroundVertices, backgroundPrefab, false, false);

        // Add map border to scene
        AddWayToScene(mapCorners, mapBorderPrefab, false, false);

        List<Vector2> gates = new List<Vector2>();

        // Add map elements to scene
        foreach (MapElement element in mapData.elements)
        {
            // Deal with nodes
            if (element.type == "node")
            {
                if (IsGate(element))
                {
                    float xPos = (float)((element.lon - xShift) * scale);
                    float yPos = (float)((LatToY(element.lat) - yShift) * scale);
                    gates.Add(new Vector2(xPos, yPos));
                }
            }

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
                {
                    AddWayToScene(vertices, roadPrefab, true, false, 5.0f);
                }

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

                // Create and add wall to scene
                else if (IsWall(element))
                {
                    AddWallToScene(vertices, gates);
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

    bool IsWall(MapElement element)
    {
        return element.tags.barrier == "wall" ||
               element.tags.barrier == "retaining_wall" ||
               element.tags.barrier == "fence";
    }

    bool IsGate(MapElement element)
    {
        return element.tags.barrier == "gate";
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

        Vector2 initialPoint1 = vertices[0] + offsetFromLine;
        Vector2 initialPoint2 = vertices[0] - offsetFromLine;

        // Insert point 1
        spline.InsertPointAt(0, initialPoint1);
        spline.SetTangentMode(0, tangentMode);

        // TEMP - DELETE THIS!
        Vector2 temp1 = initialPoint1;
        Vector2 temp2 = initialPoint2;

        Vector2[] otherLine = new Vector2[numVertices];

        // Store point 2
        otherLine[numVertices - 1] = initialPoint2;

        for (int i = 1; i < numVertices - 1; i++)
        {
            dirToPrev = (vertices[i - 1] - vertices[i]).normalized;
            dirToNext = (vertices[i + 1] - vertices[i]).normalized;
            Vector2 dirAtVertex = (dirToNext - dirToPrev).normalized;
            dirPerpToLine = Vector2.Perpendicular(dirAtVertex).normalized;
            offsetFromLine = dirPerpToLine * thickness / 2;

            Vector2 point1 = vertices[i] + offsetFromLine;
            Vector2 point2 = vertices[i] - offsetFromLine;

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

        // If end is too close to start, move the end back a bit so they don't overlap and cause issues
        float threshold = 1f;
        float offsetAmount = 1f;
        if (Vector2.Distance(vertices[numVertices - 1], vertices[0]) < threshold)
        {
            Vector2 offset = (vertices[numVertices - 2] - vertices[numVertices - 1]).normalized * offsetAmount;
            vertices[numVertices - 1] += offset;
        }

        dirToPrev = (vertices[numVertices - 2] - vertices[numVertices - 1]).normalized;
        dirPerpToLine = Vector2.Perpendicular(-dirToPrev).normalized;
        offsetFromLine = dirPerpToLine * thickness / 2;

        Vector2 finalPoint1 = vertices[numVertices - 1] + offsetFromLine;
        Vector2 finalPoint2 = vertices[numVertices - 1] - offsetFromLine;

        // TEMP - DELETE THIS!
        Color colour2 = colours[colourIndex];
        colourIndex++;
        colourIndex %= colours.Length;
        Debug.DrawLine(vertices[numVertices - 2], vertices[numVertices - 1], colour2, 1000000000, false);
        Debug.DrawLine(temp1, finalPoint1, colour2, 1000000000, false);
        Debug.DrawLine(temp2, finalPoint2, colour2, 1000000000, false);

        // Insert point 1
        spline.InsertPointAt(numVertices - 1, finalPoint1);
        spline.SetTangentMode(numVertices - 1, tangentMode);

        // Store point 2
        otherLine[0] = finalPoint2;

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

    Vector2 FindClosestPointOnLine(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float distance = Vector2.Dot(ap, ab) / ab.sqrMagnitude;

        if (distance < 0) return a;
        if (distance > 1) return b;
        return a + ab * distance;
    }

    void AddWallToScene(Vector2[] vertices, List<Vector2> gates)
    {
        bool gateFound = false;

        // Split wall if there is a gate on it
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            Vector2 v1 = vertices[i];
            Vector2 v2 = vertices[i + 1];

            foreach (Vector2 gate in gates)
            {
                Vector2 closestPoint = FindClosestPointOnLine(gate, v1, v2);
                if (Vector2.Distance(closestPoint, gate) < float.Epsilon)
                {
                    // Make a split at closest point
                    Vector2[] wall1;
                    Vector2[] wall2;
                    float gapWidth = 4f;
                    SplitWall(vertices, closestPoint, gapWidth, out wall1, out wall2);

                    // Add both walls to scene
                    if (wall1 != null)
                        AddWayToScene(wall1, wallPrefab, true, false, 1.0f);
                    if (wall2 != null)
                        AddWayToScene(wall2, wallPrefab, true, false, 1.0f);

                    gateFound = true;
                    break;
                }
            }

            if (gateFound) break;
        }

        // Add the whole wall as one object if no gate sits on wall
        if (!gateFound)
            AddWayToScene(vertices, wallPrefab, true, false, 1.0f);
    }

    void SplitWall(Vector2[] vertices, Vector2 splitPoint, float gapWidth, out Vector2[] wall1, out Vector2[] wall2)
    {
        List<Vector2> verticesList = vertices.ToList();
        verticesList.RemoveAll(v => Vector2.Distance(v, splitPoint) <= gapWidth);

        if (verticesList.Count <= 1)
        {
            wall1 = null;
            wall2 = null;
            return;
        }

        // Get index of vertex with smallest distance to split point
        int index = 0;
        float smallestDistance = float.PositiveInfinity;
        for (int i = 0; i < verticesList.Count; i ++)
        {
            Vector2 vertex = verticesList[i];
            float distance = Vector2.Distance(vertex, splitPoint);
            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                index = i;
            }
        }

        // Get index of adjacent vertex that has smallest distance to split point
        float leftDist = (index > 0) ? Vector2.Distance(verticesList[index - 1], splitPoint) : float.PositiveInfinity;
        float rightDist = (index < verticesList.Count - 1) ? Vector2.Distance(verticesList[index + 1], splitPoint) : float.PositiveInfinity;
        int adjIndex = (leftDist < rightDist) ? index - 1 : index + 1;

        // Use adjacent index if it comes before index
        if (adjIndex < index)
            index = adjIndex;

        // Check if split point is before or after whole line (i.e. the points at index and index+1 are both on the same side of the split point)
        // If so then add one point to end of line up to half gap width away from split point, then return
        float angle = Vector2.SignedAngle(splitPoint - verticesList[index], splitPoint - verticesList[index + 1]);
        if (angle > -90 && angle < 90)
        {
            int i = (index == 0) ? 0 : verticesList.Count - 1;

            float distToSplitPoint = Vector2.Distance(verticesList[i], splitPoint);
            Vector2 dirToSplitPoint = (splitPoint - verticesList[i]).normalized;
            float remainingDist = (distToSplitPoint - gapWidth / 2);
            Vector2 newVertex = verticesList[i] + dirToSplitPoint * remainingDist;

            // Insert new vertex at either at beginning or at end of line
            if (index == 0) verticesList.Insert(0, newVertex);
            else verticesList.Insert(verticesList.Count, newVertex);

            // We only have one wall so set only the first output and return
            wall1 = verticesList.ToArray();
            wall2 = null;
            return;
        }

        // Add new vertex to one side of gap to make it match gap width
        float dist = Vector2.Distance(verticesList[index], splitPoint);
        if (dist > gapWidth / 2)
        {
            Vector2 dirToSplitPoint = (splitPoint - verticesList[index]).normalized;
            float remainingDist = (dist - gapWidth / 2);
            Vector2 newVertex = verticesList[index] + dirToSplitPoint * remainingDist;
            verticesList.Insert(index + 1, newVertex);
            index++; // Make index point to new vertex
        }

        // Add new vertex to other side of gap to make it match gap width
        dist = Vector2.Distance(verticesList[index + 1], splitPoint);
        if (dist > gapWidth / 2)
        {
            Vector2 dirToSplitPoint = (splitPoint - verticesList[index + 1]).normalized;
            float remainingDist = (dist - gapWidth / 2);
            Vector2 newVertex = verticesList[index + 1] + dirToSplitPoint * remainingDist;
            verticesList.Insert(index + 1, newVertex);
        }

        wall1 = verticesList.Take(index + 1).ToArray();
        wall2 = verticesList.Skip(index + 1).ToArray();
    }
}
