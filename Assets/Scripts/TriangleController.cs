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
    public InputAction actionTriangle;
    public PlayerInputActions playerInputActions;

    private void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInputActions();
        }
        actionTriangle = playerInputActions.Player.Triangle;
        actionTriangle.Enable();
    }

    private void OnDisable()
    {
        actionTriangle.Disable();
    }
    public void ActivateTriangle()
    {
        cam = GetComponentInChildren<Camera>();
        Vector2 mousePos = Input.mousePosition;
        // World point of the cursor
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        // Direction of cursor
        Vector3 direction = worldPoint - transform.position;
        // This wont be needed with an orthographic camera maybe
        direction.z = 0;
        angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;

        previewTriangle = Instantiate(previewTrianglePrefab, worldPoint, Quaternion.Euler(0, 0, angle));
        isPlacing = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (!actionTriangle.IsPressed())
        {
            isPlacing=false;
            Destroy(previewTriangle);
            return;
        }
        if (isPlacing)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            
            Vector3 direction = worldPoint - transform.position;
            direction.z = 0;
            angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;
            previewTriangle.transform.position = worldPoint;
            
            previewTriangle.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Place triangle if right clicked while holding Q
            if (Input.GetMouseButtonDown(1))
            {
                PlaceTriangle(angle);
                return;
            }
        }
    }

    public void PlaceTriangle(float angle)
    {
        isPlacing = false;
        
        Instantiate(trianglePrefab, previewTriangle.transform.position, Quaternion.Euler(0, 0, angle));
     
        Destroy(previewTriangle);
        previewTriangle = null;
    }
}
