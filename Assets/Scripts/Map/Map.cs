using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

public class Map : MonoBehaviour
{
    [SerializeField] GameObject buildingPrefab;

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
        Debug.Log(jsonResponse);

        // Remove colons from required tags
        jsonResponse = jsonResponse.Replace("building:levels", "buildingLevels");

        // Parse JSON
        MapData mapData = JsonUtility.FromJson<MapData>(jsonResponse);
        Debug.Log(mapData.elements.Length);

        // Calculate horizontal shift, vertical shift and scale required to convert GPS coords into scene position
        double xShift = lowLong + (highLong - lowLong) / 2;
        double yShift = LatToY(lowLat) + (LatToY(highLat) - LatToY(lowLat)) / 2;
        double scale = 20000;

        // Add map elements to scene
        foreach (MapElement element in mapData.elements)
        {
            // Deal with buildings (but only ways and not relations)
            if (element.type == "way" && element.tags.building != null)
            {
                // Get scene positions for each building vertex using GPS coords
                Vector2[] vertices = new Vector2[element.geometry.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    MapElement.Coords coords = element.geometry[i];
                    double xPos = (coords.lon - xShift) * scale;
                    double yPos = (LatToY(coords.lat) - yShift) * scale;
                    vertices[i] = new Vector2((float)xPos, (float)yPos);
                }

                // Instantiate building from prefab with the map as the parent
                GameObject building = Instantiate(buildingPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);

                // Get components
                PolygonCollider2D collider = building.GetComponent<PolygonCollider2D>();
                SpriteShapeController spriteShapeController = building.GetComponent<SpriteShapeController>();
                Spline spline = spriteShapeController.spline;

                // Set the polygon collider's points to the building's vertices
                //collider.points = vertices;

                // Add building vertices to sprite shape (ignore last vertex since it is the same as the first)
                spline.Clear();
                for (int i = 0; i < vertices.Length - 1; i++)
                {
                    Debug.Log("On point: " + i);
                    spline.InsertPointAt(i, vertices[i]);
                    spline.SetTangentMode(i, ShapeTangentMode.Linear);
                    spline.SetCorner(i, false);
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
}
