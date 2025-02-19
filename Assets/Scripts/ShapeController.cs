using System.Collections.Generic;
using System.Linq;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class ShapeController : NetworkBehaviour
{
    [HideInInspector] public Shape currentShape;
    private Vector3 cursorWorldPoint;
    private float angle; // angle of cursor wrt y axis unit vector
    [SerializeField] float plusAngle = 0;
    [Networked] public bool isActive { get; set; }
    [Networked] private bool isPlacing { get; set; }
    [Networked] private float cooldown { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }

    GameController gameController { get; set; }

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;

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
        GameObject worldColliderPrefab = Resources.Load("Prefabs/WorldCollider") as GameObject;
        edgeCollider = PrefabFactory.SpawnWorldCollider(Runner, worldColliderPrefab).GetComponent<EdgeCollider2D>();
        edgeCollider.enabled = false;
        edgeCollider.isTrigger = true;

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

            if (input.buttons.WasReleased(previousButtons, InputButtons.Triangle))
            {
                lineRenderer.enabled = false;
                edgeCollider.enabled = false;
            }
            if (input.buttons.WasReleased(previousButtons, InputButtons.Square)) lineRenderer.enabled = false;
            if (input.buttons.WasReleased(previousButtons, InputButtons.Pentagon)) lineRenderer.enabled = false;

            previousButtons = input.buttons;
        }
    }
    
    private void TrianglePerformed()
    {
        // Preview shape only locally 
        // The line renderer will be disable for all others
        if (HasInputAuthority)
        {
            previewShape(3, true);
        }
    }

    private void SquarePerformed()
    {
        if (HasInputAuthority)
        {
            previewShape(4, false);
        }
    }

    private void PentagonPerformed()
    {
        if (HasInputAuthority)
        {
            previewShape(5, false);
        }
    }

    void previewShape(int nVertices, bool activate)
    {
        List<Player> closestPlayers = GetClosestPlayers(GetComponentInParent<Player>(), nVertices - 1);

        // Making a list of vector3 positions of the players
        List<Vector3> playerPositions = new List<Vector3>();
        playerPositions.Add(GetComponentInParent<Player>().transform.position);
        foreach (Player player in closestPlayers)
        {
            playerPositions.Add(player.transform.position);
        }

        // Checking if there is enough players
        if (playerPositions.Count < nVertices)
        {
            Debug.Log("Not enough players to activate shape");
            return;
        }

        // Sort by angle relative to centroid, counterclockwise. If this isn't done 
        // we might connect the diagonal of square instead of the edge
        playerPositions = SortVerticesAroundCentroid(playerPositions);

        // Calculate the angles for each vertice of the shape
        List<float> angles = GetAngles(playerPositions);
        // If it's not convex don't activate
        if (!IsConvex(angles))
        {
            Debug.Log("Shape is non-convex - can't activate buff!");
            return;
        }

        float score = CalculateScore(angles);
        // Debug.Log(score);

        if (activate)
        {
            List<Vector2> points = new List<Vector2>();
            foreach (Vector3 position in playerPositions) 
            { 
                points.Add(new Vector2(position.x, position.y));
            }

            edgeCollider.SetPoints(points);
            edgeCollider.enabled = true;
            //// float lineThickness = lineRenderer.startWidth;
            //float lineThickness = 1;

            //Debug.Log("Position count" + lineRenderer.positionCount);

            //if (lineRenderer.positionCount < 2)
            //{
            //    Debug.Log("No valid line");
            //    return;
            //}

            //List<Vector2> colliderPoints = new List<Vector2>();
            //List<Vector2> newColliderPoints = new List<Vector2>();

            //// Get half-width offset
            //float halfWidth = lineThickness / 2f;

            //// Get perpendicular direction to the line for thickness
            //for (int i = 0; i < lineRenderer.positionCount - 1; i++)
            //{
            //    Vector2 start = lineRenderer.GetPosition(i);
            //    Vector2 end = lineRenderer.GetPosition(i + 1);
            //    Vector2 direction = (end - start).normalized;
            //    Vector2 perpendicular = new Vector2(-direction.y, direction.x); // Rotate 90 degrees

            //    // Create thickness by adding and subtracting perpendicularly 
            //    Vector2 top1 = (Vector2)start + perpendicular * halfWidth;
            //    Vector2 bottom1 = (Vector2)start - perpendicular * halfWidth;
            //    Vector2 top2 = (Vector2)end + perpendicular * halfWidth;
            //    Vector2 bottom2 = (Vector2)end - perpendicular * halfWidth;

            //    colliderPoints.Add(top1);
            //    colliderPoints.Add(bottom1);
            //    colliderPoints.Add(bottom2);
            //    colliderPoints.Add(top2);
            //    colliderPoints = SortVerticesAroundCentroid(colliderPoints);
            //    //foreach(var colliderPoint in colliderPoints)
            //    //{
            //    //    newColliderPoints.Add(new Vector2(colliderPoint.x, colliderPoint.y));
            //    //}
            //}
            //polygonCollider.pathCount = 0;  
            //polygonCollider.pathCount = 1;
            //polygonCollider.SetPath(0, colliderPoints);
            //polygonCollider.enabled = true;
        }

        DrawLines(playerPositions);
    }

     void OnTriggerStay2D(Collider2D collider)
    {
        Debug.Log("Collider works");
        if(collider.gameObject.tag == "Player")
        {
            Debug.Log("On collider");
        }
    }

    private void ActivateTriangle(List<Vector3> vertices, int team)
    {
        List<Player> alivePlayers = gameController.GetAlivePlayers();
        List<Player> enemyPlayers = new List<Player>(alivePlayers).FindAll(a => a.GetTeam() != team);
        foreach (Player player in enemyPlayers)
        {

        }
    }

    private List<Player> GetClosestPlayers(Player currentPlayer, int count)
    {
        List<Player> alivePlayers = gameController.GetAlivePlayers();
        // This can be optimised by having alive players separately
        // if it slows down runtime
        List<Player> closestPlayers = new List<Player>(alivePlayers).FindAll(a => a.GetTeam() == currentPlayer.GetTeam());
        closestPlayers.Remove(currentPlayer);
        Vector3 position = currentPlayer.transform.position;

        //Sorting players by distance, 
        closestPlayers.Sort((a, b) =>
            Vector3.Distance(position, b.transform.position).CompareTo(Vector3.Distance(position, a.transform.position))
        );
        // closestPlayers.ForEach(a => Debug.Log(a.transform.position));
        return closestPlayers.Take(count).ToList();
    }

    void DrawLines(List<Vector3> vertices)
    {
        int nVertices = vertices.Count;
        lineRenderer.positionCount = nVertices + 1;
        // Lines are drawn between the adjacent vertices. The last vertice is added first so there
        // is a line between 0th and (nVertices - 1)th vertice
        lineRenderer.SetPosition(0, vertices[nVertices - 1]);
        for (int i = 0; i < nVertices; i++)
        {
            lineRenderer.SetPosition(i + 1, vertices[i]);
        }
        lineRenderer.enabled = true;
    }

    List<Vector3> SortVerticesAroundCentroid(List<Vector3> vertices)
    {
        Vector3 centroid = Vector3.zero;
        foreach (var v in vertices)
        {
            centroid += v;
        }
        centroid /= vertices.Count;

        // Sort by angle relative to centroid - Counterclockwise
        vertices = vertices.OrderBy(v => Mathf.Atan2(v.y - centroid.y, v.x - centroid.x)).ToList<Vector3>();
        return vertices;
    }

    List<Vector2> SortVerticesAroundCentroid(List<Vector2> vertices)
    {
        Vector2 centroid = Vector2.zero;
        foreach (var v in vertices)
        {
            centroid += v;
        }
        centroid /= vertices.Count;

        // Sort by angle relative to centroid - Counterclockwise
        vertices = vertices.OrderBy(v => Mathf.Atan2(v.y - centroid.y, v.x - centroid.x)).ToList<Vector2>();
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
