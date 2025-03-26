using UnityEngine;
using Fusion;

public class CoordinatesDataHolder : NetworkBehaviour
{
    public static CoordinatesDataHolder Instance;

    [Networked] public float Float1 { get; set; }
    [Networked] public float Float2 { get; set; }
    [Networked] public float Float3 { get; set; }
    [Networked] public float Float4 { get; set; }

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

    public void SetCoordinates(float f1, float f2, float f3, float f4)
    {
        Float1 = f1;
        Float2 = f2;
        Float3 = f3;
        Float4 = f4;
    }
}
