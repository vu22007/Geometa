using UnityEngine;
using Fusion;

public class CoordinatesDataHolder : NetworkBehaviour
{
    public static CoordinatesDataHolder Instance;

    [Networked] public double Float1 { get; set; }
    [Networked] public double Float2 { get; set; }
    [Networked] public double Float3 { get; set; }
    [Networked] public double Float4 { get; set; }
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
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SetCoordinates(double f1, double f2, double f3, double f4)
    {
        Float1 = f1;
        Float2 = f2;
        Float3 = f3;
        Float4 = f4;
    }

    public void SetRespawnPoint1(double lat, double lon)
    {
        respawnP1lat = (float)lat;
        respawnP1lon = (float)lon;
    }

    public void SetRespawnPoint2(double lat, double lon)
    {
        respawnP2lat = (float)lat;
        respawnP2lon = (float)lon;
    }
}
