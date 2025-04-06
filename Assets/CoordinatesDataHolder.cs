using UnityEngine;
using Fusion;
using System;

public class CoordinatesDataHolder : NetworkBehaviour
{
    public static CoordinatesDataHolder Instance;

    [Networked] public double lowLat { get; set; }
    [Networked] public double highLat { get; set; }
    [Networked] public double lowLon { get; set; }
    [Networked] public double highLon { get; set; }
    [Networked] public float respawnP1lat { get; set; }
    [Networked] public float respawnP1lon { get; set; }
    [Networked] public float respawnP2lat { get; set; }
    [Networked] public float respawnP2lon { get; set; }

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void SetCoordinates(double f1, double f2, double f3, double f4)
    {
        lowLat = f1;
        highLat = f2;
        lowLon = f3;
        highLon = f4;
    }

    public void SetRespawnPoint1(double lat, double lon)
    {
        respawnP1lat = ConvertSinglePoint(lat, lon).x;
        respawnP1lon = ConvertSinglePoint(lat, lon).y;
    }

    public void SetRespawnPoint2(double lat, double lon)
    {
        respawnP2lat = ConvertSinglePoint(lat, lon).x;
        respawnP2lon = ConvertSinglePoint(lat, lon).y;
    }

    public Vector2 ConvertSinglePoint(double lat, double lon)
    {
        // Calculate shifts and scale (same as in LoadMapFromBoundingBox)
        double xShift = lowLon + (highLon - lowLon) / 2;
        double yShift = LatToY(lowLat) + (LatToY(highLat) - LatToY(lowLat)) / 2;
        double midLat = (lowLat + highLat) / 2;
        double scale = Mathf.Cos((float)midLat * Mathf.Deg2Rad) * 111319.488f;

        double xPos = (lon - xShift) * scale;
        double yPos = (LatToY(lat) - yShift) * scale;

        return new Vector2((float)xPos, (float)yPos);
    }

    double LatToY(double latitude)
    {
        return Math.Log(Math.Tan((latitude + 90) / 360 * Math.PI)) / Math.PI * 180;
    }

}
