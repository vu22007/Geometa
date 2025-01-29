using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class TriangleController : MonoBehaviour
{
    public GameObject trianglePrefab;
    private GameObject previewTriangle;
    public GameObject previewTrianglePrefab;
    private bool isPlacing = false;
    [SerializeField] Camera cam;
    private float angle; // angle of cursor wrt y axis unit vector
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void ActivateTriangle()
    {   
        cam = GetComponentInChildren<Camera>();
        Vector2 mousePos = Input.mousePosition;
        // world point of the cursor
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        // Direction of cursor
        Vector3 direction = worldPoint - transform.position;
        direction.z = 0;
        angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;
        
        if (isPlacing)
        {
            return;
        }

        previewTriangle = Instantiate(previewTrianglePrefab, worldPoint, Quaternion.Euler(0, 0, angle));
        isPlacing = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (!Input.GetKey(KeyCode.Q))
        {
            isPlacing=false;
            Destroy(previewTriangle);
        }
        if (isPlacing)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            Vector3 direction = worldPoint - transform.position;
            // This wont be needed with an orthographic camera maybe
            direction.z = 0;
            angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;
            previewTriangle.transform.position = worldPoint;
            Debug.Log("Angle" + angle);
            Debug.DrawLine(transform.position, transform.position + new Vector3(0, 1, 0), Color.blue, 2f);
            Debug.DrawLine(transform.position, transform.position + (worldPoint - transform.position), Color.red, 2f);
            previewTriangle.transform.rotation = Quaternion.Euler(0, 0, angle);

            if (Input.GetMouseButtonDown(1))
            {
                PlaceTriangle(angle);
            }
        }
    }

    public void PlaceTriangle(float angle)
    {
        Instantiate(trianglePrefab, previewTriangle.transform.position, Quaternion.Euler(0, 0, angle));
        Debug.Log("Triangle Placed");

        Destroy(previewTriangle);
        previewTriangle = null;

        isPlacing = false;
    }
}
