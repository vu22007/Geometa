using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    void LateUpdate()
    {
        // Lock the camera's rotation
        transform.rotation = Quaternion.identity;
    }
}

