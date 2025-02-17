using UnityEngine;

public class Map : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(MapLoader.LoadMapFromBoundingBox(51.453990, -2.605788, 51.456203, -2.598647));
    }
}
