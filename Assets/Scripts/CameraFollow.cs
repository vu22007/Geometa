using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public bool lockRelativePosition;
    void LateUpdate()
    {
        // Lock the camera's rotation
        transform.rotation = Quaternion.identity;
    }
}

