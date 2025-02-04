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

    private bool isPlacing = false;
    private Vector3 cursorWorldPoint;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
    private float cooldown = 0;

    [Networked] NetworkButtons previousButtons { get; set; }

    public override void FixedUpdateNetwork()
    {
        // TODO: Need cooldown for every shape separately
        cooldown = (cooldown > 0) ? cooldown - Runner.DeltaTime : 0;

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this shape controller)
        // So the following is ran for just the server and the client who controls this shape controller
        if (GetInput(out NetworkInputData input))
        {
            cursorWorldPoint = new Vector3(input.cursorWorldPoint.x, input.cursorWorldPoint.y);
            Vector3 direction = new Vector3(input.aimDirection.x, input.aimDirection.y);
            angle = CalculateAngle(direction);

            // On key down for specific shape (only on moment when key is pressed down)
            if (input.buttons.WasPressed(previousButtons, InputButtons.Triangle)) TrianglePerformed();
            if (input.buttons.WasPressed(previousButtons, InputButtons.Square)) SquarePerformed();
            if (input.buttons.WasPressed(previousButtons, InputButtons.Pentagon)) PentagonPerformed();

            // Place down the shape if place shape button pressed
            if (input.buttons.WasPressed(previousButtons, InputButtons.PlaceShape)) PlaceShapePerformed();

            // If all shape keys are released
            if (!input.buttons.IsSet(InputButtons.Triangle) && !input.buttons.IsSet(InputButtons.Square) && !input.buttons.IsSet(InputButtons.Pentagon))
            {
                isPlacing = false;
                if (previewShape != null)
                {
                    // Despawn the shape (only the server can do this)
                    if (HasStateAuthority)
                    {
                        Runner.Despawn(previewShape.GetComponent<NetworkObject>());
                    }
                    previewShape = null;
                }
                return;
            }

            // If a shape key is still held down
            if (isPlacing)
            {
                previewShape.transform.position = cursorWorldPoint;
                previewShape.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            previousButtons = input.buttons;
        }
    }
    
    private void TrianglePerformed()
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 0;
            SpawnShape(trianglePrefab);
        }
    }

    private void SquarePerformed()
    {
        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 45;
            SpawnShape(squarePrefab);
        }
    }

    private void PentagonPerformed()
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

    private void PlaceShapePerformed()
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

    private float CalculateAngle(Vector3 direction)
    {
        return Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180 + plusAngle;
    }
}
