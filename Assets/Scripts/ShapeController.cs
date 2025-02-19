using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class ShapeController : NetworkBehaviour
{
    [SerializeField] GameObject trianglePrefab;
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject pentagonPrefab;
    [HideInInspector] public Shape currentShape;
    // [HideInInspector] public GameObject previewShape;
    private Vector3 cursorWorldPoint;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
    [Networked] public bool isActive { get; set; }
    [Networked] private bool isPlacing { get; set; }
    [Networked] private float cooldown { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }

    GameController gameController { get; set; }

    private LineRenderer lineRenderer;

    // Shape controller intialisation (called on each client and server when shape controller is spawned on network)
    public override void Spawned()
    {
        // Find game controller component (Fusion creates copies of the game controller object so we need to choose the correct one)
        if (GameObject.Find("Host") != null)
            gameController = GameObject.Find("Host").GetComponent<GameController>();
        else
            gameController = GameObject.Find("Client A").GetComponent<GameController>();
        
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

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
            if (input.buttons.IsSet(InputButtons.Triangle)) TrianglePerformed();
            if (input.buttons.IsSet(InputButtons.Square)) SquarePerformed();
            if (input.buttons.IsSet(InputButtons.Pentagon)) PentagonPerformed();

            if(input.buttons.WasReleased(previousButtons, InputButtons.Triangle)) lineRenderer.enabled = false;
            if (input.buttons.WasReleased(previousButtons, InputButtons.Square)) lineRenderer.enabled = false;
            if (input.buttons.WasReleased(previousButtons, InputButtons.Pentagon)) lineRenderer.enabled = false;

            previousButtons = input.buttons;
        }
    }
    
    private void TrianglePerformed()
    {   
        previewShape(3);
    }

    void previewShape(int nVertices)
    {
        // Preview shape only locally 
        // The line renderer will be disable for all others
        if (HasInputAuthority)
        {
            List<Player> closestPlayers = gameController.GetClosestPlayers(GetComponentInParent<Player>(), nVertices - 1);

            List<Vector3> playerPositions = new List<Vector3>();
            playerPositions.Add(GetComponentInParent<Player>().transform.position);
            foreach (Player player in closestPlayers)
            {
                playerPositions.Add(player.transform.position);
            }

            if (playerPositions.Count < nVertices)
            {
                Debug.Log("Not enough players to activate shape");
                return;
            }

            playerPositions = SortVerticesAroundCentroid(playerPositions);

            List<float> angles = GetAngles(playerPositions);
            if (!IsConvex(angles))
            {
                Debug.Log("Shape is non-convex - can't activate buff!");
                return;
            }

            float score = CalculateScore(angles);
            Debug.Log(score);

            lineRenderer.positionCount = nVertices + 1;
            lineRenderer.SetPosition(0, playerPositions[nVertices - 1]);
            for (int i = 0; i < nVertices; i++)
            {
                lineRenderer.SetPosition(i + 1, playerPositions[i]);
            }
            lineRenderer.enabled = true;
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
        vertices = vertices.OrderBy(v => Mathf.Atan2(v.y - centroid.y, v.x - centroid.x)).ToList<Vector3>();
        return vertices;
    }

    List<float> GetAngles(List<Vector3> vertices)
    {
        List<float> angles = new List<float>();
        int count = vertices.Count;

        for (int i = 0; i < count; i++)
        {
            List<Vector3> angleVertices = new List<Vector3>();
            for (int j = -1; j <= 1; j++)
            {
                angleVertices.Add(vertices[(i + j + count) % count]);
            }
            angle = GetAngle(angleVertices);
            angles.Add(angle);
        } 

        return angles;
    }

    bool IsConvex(List<float> angles)
    {
        int count = angles.Count;
        float sum = angles.Sum();

        // sum is a sum of floating points so we put 0.01 as an allowed error margin
        if(Mathf.Abs(sum - ((count - 2) * 180f)) > 0.01)
        {
            return false;
        } 
        return true;
    }

    // Getting the angle between 3 vertices for the angle on the second element (vertices[1])
    float GetAngle(List<Vector3> vertices)
    {
        if(vertices.Count != 3)
        {
            Debug.LogError("3 vertices not given to calculate angle");
        }

        Vector3 direction1 = (vertices[0] - vertices[1]).normalized;
        Vector3 direction2 = (vertices[2] - vertices[1]).normalized;
        float angle = Vector3.Angle(direction1, direction2);
        // Debug.Log(angle);
        
        return angle;
    }

    float CalculateScore(List<float> angles)
    {
        float score = 0;
        int count = angles.Count;
        // The angle for a regular polygon
        float regularAngle = (count - 2) * 180;
        
        // Adding how much each angle is close to a regular angle
        foreach (float angle in angles)
        {
            score += Mathf.Abs(angle - regularAngle);
        }
        // Getting the inverse because the value will be smaller the more regular the shape is
        // And we divide by count so shapes with more vertices are not penalised
        score = 1 / (1 + score/count); 
        return score;
    }

    private void SquarePerformed()
    {
        previewShape(4);
    }

    private void PentagonPerformed()
    {
        previewShape(5);
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
        //previewShape = shapeNetworkObject.gameObject;

        //currentShape = previewShape.GetComponent<Shape>();
        //if (currentShape == null)
        //{
        //    Debug.LogError("Shape prefab doesn't have a Shape component");
        //}
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
        // previewShape = null;
    }

    private float CalculateAngle(Vector3 direction)
    {
        return Vector3.SignedAngle(Vector3.up, direction, Vector3.forward) - 180 + plusAngle;
    }
}
