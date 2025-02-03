using UnityEngine;
using UnityEngine.InputSystem;

public class ShapeController : MonoBehaviour
{
    [SerializeField] GameObject trianglePrefab;
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject pentagonPrefab;
    private GameObject currentPrefab;
    [SerializeField] GameObject previewTrianglePrefab;
    [SerializeField] GameObject previewSquarePrefab;
    [SerializeField] GameObject previewPentagonPrefab;
    private Shape currentShape;
    private GameObject previewShape;    
    private bool isPlacing = false;
    private Camera cam;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
    private InputAction actionTriangle;
    private InputAction actionSquare;
    private InputAction actionPentagon;   
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
        actionSquare = playerInputActions.Player.Square;
        actionPentagon = playerInputActions.Player.Pentagon;        
        placeShape = playerInputActions.Player.PlaceShape;

        // What function should be ran when action is performed(when a key is pressed)
        actionTriangle.performed += trianglePerformed;
        actionSquare.performed += squarePerformed;
        actionPentagon.performed += pentagonPerformed;
        placeShape.performed += placeShapePerformed;    

        actionTriangle.Enable();
        actionSquare.Enable();
        actionPentagon.Enable();
        placeShape.Enable(); 
    }

    private void OnDisable()
    {
        actionTriangle.Disable();
        actionSquare.Disable();
        actionPentagon.Disable();
        placeShape.Disable();
    }

    void Start()
    {
        cam = GetComponentInParent<Camera>();
        if(cam == null)
        {
            Debug.LogError("Triangle controller doesn't have a Camera in parent");
        }
    }

    void Update()
    {
        // Need cooldown for every shape separately
        cooldown = (cooldown > 0) ? cooldown - Time.deltaTime : 0;
        if (!actionTriangle.IsPressed() && !actionSquare.IsPressed() && !actionPentagon.IsPressed())
        {
            isPlacing = false;
            Destroy(previewShape);   
            return;
        }
        if (isPlacing)
        {
            Vector2 mousePos = Input.mousePosition;
            // World point of the cursor
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            float angle = CalculateAngle(cursorWorldPoint);

            previewShape.transform.position = cursorWorldPoint;
            previewShape.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    private void trianglePerformed(InputAction.CallbackContext context)
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 0;
            isPlacing = true;
            
            currentPrefab = trianglePrefab;
            if (currentPrefab == null)
            {
                Debug.LogError("Shape prefab isn't set");
            }

            Vector2 mousePos = Input.mousePosition;
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            angle = CalculateAngle(cursorWorldPoint);
           
            // Instantiate a preview triangle
            previewShape= Instantiate(currentPrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle));

            currentShape = previewShape.GetComponent<Shape>();
            if (currentShape == null)
            {
                Debug.LogError("Shape prefab doesn't have a Shape component");
            }
        }
    }

    private void squarePerformed(InputAction.CallbackContext context)
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 45;
            isPlacing = true;            
            
            currentPrefab = squarePrefab;
            if (currentPrefab == null)
            {
                Debug.LogError("Shape prefab isn't set");
            }
            
            Vector2 mousePos = Input.mousePosition;
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            angle = CalculateAngle(cursorWorldPoint);

            previewShape = Instantiate(squarePrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle));

            currentShape = currentPrefab.GetComponent<Shape>();
            if (currentShape == null)
            {
                Debug.LogError("Shape prefab doesn't have a Shape component");
            }
        }
    }

    private void pentagonPerformed(InputAction.CallbackContext context)
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 0;
            isPlacing = true;    
            
            currentPrefab = pentagonPrefab;
            if (currentPrefab == null)
            {
                Debug.LogError("Shape prefab isn't set");
            }

            Vector2 mousePos = Input.mousePosition;
            Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            angle = CalculateAngle(cursorWorldPoint);

            previewShape = Instantiate(previewPentagonPrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle));

            currentShape = currentPrefab.GetComponent<Shape>();
            if (currentShape == null)
            {
                Debug.LogError("Shape prefab doesn't have a Shape component");
            }
        }
    }

    private void placeShapePerformed(InputAction.CallbackContext context)
    {
        // Place shape if right clicked while holding Q
        if (!isPlacing || cooldown > 0)
        {
            Debug.Log("Triangle cooldown remaning: " + cooldown.ToString());
            return;
        }
        cooldown = currentShape.Cooldown();
        isPlacing = false;

        currentShape.EnableCorners();
        previewShape = null;
        
        return;
    }

    private float CalculateAngle(Vector3 cursorWorldPoint)
    {
        Vector3 direction = cursorWorldPoint - transform.position;
        // This wont be needed with an orthographic camera maybe
        direction.z = 0;

        angle = Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180 + plusAngle;

        return angle;
    }
}
