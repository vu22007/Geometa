using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShapeController : NetworkBehaviour
{
    [SerializeField] GameObject trianglePrefab;
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject pentagonPrefab;
    private Shape currentShape;
    private GameObject previewShape;    

    public PlayerInputActions playerInputActions;    
    private InputAction actionTriangle;
    private InputAction actionSquare;
    private InputAction actionPentagon;   
    private InputAction placeShape;

    private bool isPlacing = false;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
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

    void Update()
    {
        // Need cooldown for every shape separately
        cooldown = (cooldown > 0) ? cooldown - Time.deltaTime : 0;
        if (!actionTriangle.IsPressed() && !actionSquare.IsPressed() && !actionPentagon.IsPressed())
        {
            isPlacing = false;
            if (previewShape != null)
            {
                Runner.Despawn(previewShape.GetComponent<NetworkObject>());
                previewShape = null;
            }
            return;
        }
        if (isPlacing)
        {
            // TEMP
            NetworkObject networkPlayerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
            Player localPlayer = networkPlayerObject.GetComponent<Player>();
            Camera cam = localPlayer.cam;

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
            SpawnShape(trianglePrefab);
        }
    }

    private void squarePerformed(InputAction.CallbackContext context)
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 45;
            SpawnShape(squarePrefab);
        }
    }

    private void pentagonPerformed(InputAction.CallbackContext context)
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 0;
            SpawnShape(pentagonPrefab);
        }
    }

    private void SpawnShape(GameObject shapePrefab)
    {
        isPlacing = true;

        // TEMP
        NetworkObject networkPlayerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
        Player localPlayer = networkPlayerObject.GetComponent<Player>();
        Camera cam = localPlayer.cam;

        Vector2 mousePos = Input.mousePosition;
        Vector3 cursorWorldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        angle = CalculateAngle(cursorWorldPoint);

        // Spawn an object of the shape prefab. The default colliders are dissable and they
        // are enable once the shape is placed
        NetworkObject shapeNetworkObject = PrefabFactory.SpawnShape(Runner, shapePrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle));
        previewShape = shapeNetworkObject.gameObject;

        currentShape = previewShape.GetComponent<Shape>();
        if (currentShape == null)
        {
            Debug.LogError("Shape prefab doesn't have a Shape component");
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
