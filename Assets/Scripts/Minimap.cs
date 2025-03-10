using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [SerializeField] Camera cameraView;
    [SerializeField] RawImage minimap;
    RenderTexture renderTexture;
    private float minFOV;
    private float maxFOV;


    public void Setup(){
        //Create render texture
        renderTexture = new RenderTexture(180, 180, 32, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        //Assign render texture to camera
        cameraView.targetTexture = renderTexture;

        //Set fov limits
        minFOV = 0.2f;
        maxFOV = 3f;

        //Set render texture to raw image
        minimap.texture = renderTexture;
    }

    public void SetZoom(float scale){
        cameraView.fieldOfView = scale;

        //disregard extreme values
        cameraView.fieldOfView = Mathf.Clamp(cameraView.fieldOfView, minFOV, maxFOV);
    }
}
