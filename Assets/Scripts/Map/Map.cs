using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Map : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadMapFromBoundingBox(51.453990, -2.605788, 51.456203, -2.598647));
    }

    public static IEnumerator LoadMapFromBoundingBox(double lowLat, double lowLong, double highLat, double highLong)
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
        double yShift = lowLat + (highLat - lowLat) / 2;
        double scale = 20000;

        // Add map elements to scene
        GameObject map = GameObject.Find("Map");
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
                    double yPos = (coords.lat - yShift) * scale;
                    vertices[i] = new Vector2((float)xPos, (float)yPos);
                }

                // Create building game object and assign it as parent of map object
                GameObject building = new GameObject("Building");
                building.transform.parent = map.transform;

                // Create and get polygon collider component
                building.AddComponent<PolygonCollider2D>();
                PolygonCollider2D collider = building.GetComponent<PolygonCollider2D>();

                // Set the polygon collider's points to the building's vertices
                collider.points = vertices;
            }
        }
    }
}
