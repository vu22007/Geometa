using System.Collections.Generic;
using System.Linq;
using Fusion;
using NUnit.Framework;
using UnityEngine;

public class ShapeController : NetworkBehaviour
{
    [SerializeField] GameObject trianglePrefab;
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject pentagonPrefab;
    [HideInInspector] public Shape currentShape;
    [HideInInspector] public GameObject previewShape;
    private Vector3 cursorWorldPoint;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
    [Networked] public bool isActive { get; set; }
    [Networked] private bool isPlacing { get; set; }
    [Networked] private float cooldown { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }

    GameController gameController { get; set; }

    // Shape controller intialisation (called on each client and server when shape controller is spawned on network)
    public override void Spawned()
    {
        // Find game controller component (Fusion creates copies of the game controller object so we need to choose the correct one)
        if (GameObject.Find("Host") != null)
            gameController = GameObject.Find("Host").GetComponent<GameController>();
        else
            gameController = GameObject.Find("Client A").GetComponent<GameController>();

        isActive = true;
        isPlacing = false;
        cooldown = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (!isActive) return;

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

            // Place down the shape if PlaceShape button pressed
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
            }

            // If a shape key is still held down
            if (isPlacing && previewShape != null)
            {
                previewShape.transform.position = cursorWorldPoint;
                previewShape.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            previousButtons = input.buttons;
        }
    }
    
    private void TrianglePerformed()
    {
        int vertices = 3;
        List<Player> closestPlayers = gameController.GetClosestPlayers(GetComponentInParent<Player>(), 2);
        List<Vector3> playerPositions = new List<Vector3>();
        playerPositions.Add(GetComponentInParent<Player>().transform.position);
        foreach (Player player in closestPlayers)
        {
            playerPositions.Add(player.transform.position);
        }

        if (playerPositions.Count < vertices)
        {
            Debug.Log("Not enough players to activate shape");
            return;
        }

        CheckConvex(playerPositions);

        if (!isPlacing && cooldown == 0)
        {
            plusAngle = 0;
            SpawnShape(trianglePrefab);
        }
    }

    List<Vector3> SortVerticesAroundCentroid(List<Vector3> vertices)
    {
        Vector3 centroid = Vector3.zero;
        foreach (var v in vertices)
        {
            centroid += v;
        }
        centroid /= vertices.Count;

        // Sort by angle relative to centroid  - Counterclockwise
        return vertices.OrderBy(v => Mathf.Atan2(v.y - centroid.y, v.x - centroid.x)).ToList();
    }

    bool CheckConvex(List<Vector3> vertices)
    {
        List<float> angles = new List<float>();
        int count = vertices.Count;
        foreach (var v in vertices)
        {
            for (int i = 0; i < count; i++)
            {
                List<Vector3> angleVertices = new List<Vector3>();
                for (int j = -1; j <= 1; j++)
                {
                    Debug.Log((i + j + count) % count);
                    angleVertices.Add(vertices[(i + j + count) % count]);
                }
                angle = GetAngle(angleVertices);
                if(angle > 180) return false;
            }
        }
        return true;
    }

    float GetAngle(List<Vector3> vertices)
    {
        if(vertices.Count != 3)
        {
            Debug.LogError("3 vertices not given to calculate angle");
        }
        Vector3 direction1 = (vertices[0] - vertices[1]).normalized;
        Vector3 direction2 = (vertices[2] - vertices[1]).normalized;
        float angle = Vector3.Angle(direction1, direction2);
        Debug.Log(angle);
        return angle;
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
        // Only the server can spawn shapes
        if (!HasStateAuthority) return;

        isPlacing = true;

        // Spawn an object of the shape prefab as a preview. The default colliders are disabled and they
        // are enabled once the shape is placed
        bool isPreview = true;
        NetworkObject shapeNetworkObject = PrefabFactory.SpawnShape(Runner, Object.InputAuthority, shapePrefab, cursorWorldPoint, Quaternion.Euler(0, 0, angle), isPreview);
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

        if (currentShape == null) return;

        // Shape is placed so it no longer is a preview
        currentShape.isPreview = false;

        cooldown = currentShape.Cooldown();
        isPlacing = false;

        currentShape.EnableCorners();
        previewShape = null;
    }

    private float CalculateAngle(Vector3 direction)
    {
        return Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180 + plusAngle;
    }
}
