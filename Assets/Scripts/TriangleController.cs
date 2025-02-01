using UnityEngine;
using UnityEngine.InputSystem;

public class TriangleController : MonoBehaviour
{
    [SerializeField] GameObject trianglePrefab;
    [SerializeField] GameObject previewTrianglePrefab;
    private GameObject previewTriangle;    
    private bool isPlacing = false;
    private Camera cam;
    private float angle; // angle of cursor wrt y axis unit vector
    private InputAction actionTriangle;
    private InputAction actionSquare;
    private InputAction placeShape;
    public PlayerInputActions playerInputActions;
    private float cooldown = 0;

    private void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInputActions();
        }
        actionTriangle = playerInputActions.Player.Triangle;
        placeShape = playerInputActions.Player.PlaceShape;
        actionSquare = playerInputActions.Player.Square;

        actionTriangle.performed += trianglePerformed;
        placeShape.performed += placeShapePerformed;    

        actionTriangle.Enable();
        actionSquare.Enable();
        placeShape.Enable(); 
    }

    private void OnDisable()
    {
        actionTriangle.Disable();
        actionSquare.Disable();
        placeShape.Disable();
    }

    void Start()
    {
        cam = GetComponentInParent<Camera>();
        if(cam == null)
        {
            Debug.LogError("Triangle controller doesn't have a Camera parent");
        }
    }

    void Update()
    {
        cooldown = (cooldown > 0) ? cooldown - Time.deltaTime : 0;
        if (!actionTriangle.IsPressed())
        {
            isPlacing = false;
            Destroy(previewTriangle);
            return;
        }
        if (isPlacing)
        {
            Vector2 mousePos = Input.mousePosition;
            // World point of the cursor
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            // Direction of cursor
            Vector3 direction = cursorWorldPoint - transform.position;
            // This wont be needed with an orthographic camera maybe
            direction.z = 0;

            angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;

            previewTriangle.transform.position = cursorWorldPoint;

            previewTriangle.transform.rotation = Quaternion.Euler(0, 0, angle);

        }
    }
    
    private void trianglePerformed(InputAction.CallbackContext context)
    {
        // Place triangle if right clicked while holding Q
        if (!isPlacing)
        {
            isPlacing = true;
            Vector2 mousePos = Input.mousePosition;
            // World point of the cursor
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            // Direction of cursor
            Vector3 direction = cursorWorldPoint - transform.position;
            // This wont be needed with an orthographic camera maybe
            direction.z = 0;
            angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180;

            // Instantiate a preview triangle
            previewTriangle = Instantiate(previewTrianglePrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle));
        }
    }

    private void placeShapePerformed(InputAction.CallbackContext context)
    { 
        if (isPlacing && cooldown > 0)
        {
            Debug.Log("Triangle cooldown remaning: " + cooldown.ToString());
            return;
        }
        cooldown = 5;
        isPlacing = false;
        PlaceTriangle(angle);
        return;
    }

    public void PlaceTriangle(float angle)
    {
        // Place the triangle with the colliders
        Instantiate(trianglePrefab, previewTriangle.transform.position, Quaternion.Euler(0, 0, angle));
     
        Destroy(previewTriangle);
        previewTriangle = null;
    }
}
