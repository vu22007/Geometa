using UnityEngine;

public class Minimap : MonoBehaviour
{
    private Camera cameraView;
    private float minFOV;
    private float maxFOV;


    public void Setup(Camera camera){
        cameraView = camera;
        minFOV = 0.2f;
        maxFOV = 3f;
    }

    public void SetZoom(float scale){
        cameraView.fieldOfView = scale;

        //disregard extreme values
        cameraView.fieldOfView = Mathf.Clamp(cameraView.fieldOfView, minFOV, maxFOV);
    }
}
