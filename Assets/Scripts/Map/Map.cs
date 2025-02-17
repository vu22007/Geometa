using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class Map
{
    //public static IEnumerator LoadFromBoundingBox(double south, double west, double north, double east)
    //{
    //    // Construct request body
    //    string data = "data=" + UnityWebRequest.EscapeURL("" +
    //        $"[out:json][timeout:25];" +
    //        $"way[building]({south}, {west}, {north}, {east});" +
    //        $"out geom;");

    //    // Send API request
    //    UnityWebRequest webRequest = UnityWebRequest.Post("https://overpass-api.de/api/interpreter", data, "application/json");
    //    yield return webRequest.SendWebRequest();

    //    // Check for unsuccessful response
    //    if (webRequest.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError(webRequest.error);
    //    }
    //    else
    //    {
    //        // Get JSON response
    //        string jsonResponse = webRequest.downloadHandler.text;
    //        Debug.Log(jsonResponse);
    //    }
    //}

    public static async void LoadMapFromBoundingBox(double lowLat, double lowLong, double highLat, double highLong)
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
        await webRequest.SendWebRequest();

        // Check for unsuccessful response
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(webRequest.error);
            return;
        }

        // Get JSON response
        string jsonResponse = webRequest.downloadHandler.text;
        Debug.Log(jsonResponse);

        // Remove colons from required tags
        jsonResponse = jsonResponse.Replace("building:levels", "buildingLevels");

        // Parse JSON
        MapData mapData = JsonUtility.FromJson<MapData>(jsonResponse);
        Debug.Log(mapData.elements.Length);
    }
}
