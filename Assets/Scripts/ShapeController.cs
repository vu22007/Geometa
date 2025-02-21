using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class ShapeController : NetworkBehaviour
{
    [Networked] public bool isActive { get; set; }
    [Networked] private float cooldown { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }

    private int triangleCost = 3;
    private int squareCost = 5;
    private int pentagonCost = 8;

    GameController gameController { get; set; }
    Player parentPlayer { get; set; }

    private LineRenderer lineRenderer;
    private LineRenderer triangleLineRenderer;
    private LineRenderer squareLineRenderer;
    private LineRenderer pentagonLineRenderer;

    private TriangleShape triangleShape;
    private SquareShape squareShape;
    private PentagonShape pentagonShape;

    // Shape controller intialisation (called on each client and server when shape controller is spawned on network)
    public override void Spawned()
    {
        // Find game controller component (Fusion creates copies of the game controller object so we need to choose the correct one)
        if (GameObject.Find("Host") != null)
            gameController = GameObject.Find("Host").GetComponent<GameController>();
        else
            gameController = GameObject.Find("Client A").GetComponent<GameController>();
        
        parentPlayer = GetComponentInParent<Player>();
   
        triangleShape = GetComponentInChildren<TriangleShape>();
        squareShape = GetComponentInChildren<SquareShape>();
        pentagonShape = GetComponentInChildren<PentagonShape>();

        triangleLineRenderer = triangleShape.GetComponent<LineRenderer>();
        triangleLineRenderer.enabled = false; 
        squareLineRenderer = squareShape.GetComponent<LineRenderer>();
        squareLineRenderer.enabled = false;
        pentagonLineRenderer = pentagonShape.GetComponent<LineRenderer>();
        pentagonLineRenderer.enabled = false;

        isActive = true;
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
            // On key down for specific shape (only on moment when key is pressed down)
            if (input.buttons.IsSet(InputButtons.Triangle)) TrianglePerformed();
            if (input.buttons.IsSet(InputButtons.Square)) SquarePerformed();
            if (input.buttons.IsSet(InputButtons.Pentagon)) PentagonPerformed();

            if (input.buttons.WasReleased(previousButtons, InputButtons.Triangle))
            {
                TriangleActivated();
            }
            if (input.buttons.WasReleased(previousButtons, InputButtons.Square))
            {
                SquareActivated();
            }
            if (input.buttons.WasReleased(previousButtons, InputButtons.Pentagon))
            {
                PentagonActivated();
            }

            previousButtons = input.buttons;
        }
    }
    
    private void TrianglePerformed()
    {
        // Preview shape only locally 
        // The line renderer will be disable for all others
        if (HasInputAuthority)
        {
            previewShape(3, false);
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

    private void TriangleActivated()
    {
        // Preview shape only locally 
        // The line renderer will be disable for all others
        if (HasStateAuthority)
        {
            previewShape(3, true);
        }
    }

    private void SquareActivated()
    {
        if (HasStateAuthority)
        {
            previewShape(4, true);
        }
    }

    private void PentagonActivated()
    {
        if (HasStateAuthority)
        {
            previewShape(5, true);
        }
    }

    void previewShape(int nVertices, bool activate)
    {
        List<Player> closestPlayers = GetClosestPlayers(parentPlayer, nVertices - 1);

        // Making a list of vector3 positions of the players
        List<Vector3> playerPositions = new List<Vector3>();
        playerPositions.Add(parentPlayer.transform.position);
        foreach (Player player in closestPlayers)
        {
            playerPositions.Add(player.transform.position);
        }

        // Checking if there is enough players for each vertice
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

        float score = CalculateScore(angles);

        // Give buffs/do damage if the player activates the ability
        if (activate && HasStateAuthority)
        {
            // When activated the server will draw the lines
            // TODO: MAke the lines darker for activate and lighter for preview
            DrawLines(playerPositions);
            if (nVertices == 3)
            {
                if (parentPlayer.GetPoints() >= triangleCost)
                {
                    triangleShape.CastAbility(playerPositions, score);
                    StartCoroutine(DelayDisable(0.1f));
                    parentPlayer.SpendPoints(triangleCost);
                }
                else
                {
                    lineRenderer.enabled = false;
                    Debug.Log("You don't have enough points to activate a triangle");
                }
            }
            else if (nVertices == 4)
            {
                if (parentPlayer.GetPoints() < squareCost)
                {
                    lineRenderer.enabled = false;
                    Debug.Log("You don't have enough points to activate a square");
                }
                // If it's not convex don't activate it
                else if (!IsConvex(angles))
                {
                    lineRenderer.enabled = false;
                    Debug.Log("Shape is not convex - can't activate buff!");
                    return;
                }
                else
                {
                    parentPlayer.SpendPoints(squareCost);
                    StartCoroutine(DelayDisable(0.1f));
                }

            }
            else if (nVertices == 5)
            {
                if (parentPlayer.GetPoints() < pentagonCost)
                {
                    lineRenderer.enabled = false;
                    Debug.Log("You don't have enough points to activate a pentagon");
                }
                else if (!IsConvex(angles))
                {
                    lineRenderer.enabled = false;
                    Debug.Log("Shape is not convex - can't activate buff!");
                    return;
                }
                else
                {
                    Vector3 centroid = Vector3.zero;
                    foreach (var v in playerPositions)
                    {
                        centroid += v;
                    }
                    centroid /= playerPositions.Count;

                    // TODO: Spawn creature in the center

                    parentPlayer.SpendPoints(pentagonCost);
                    StartCoroutine(DelayDisable(0.1f));
                }
            }
        }

        // Draw lines locally when just preview
        if(HasInputAuthority && !activate)
        {
            DrawLines(playerPositions);
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
        closestPlayers.Sort((b, a) =>
            Vector3.Distance(position, b.transform.position).CompareTo(Vector3.Distance(position, a.transform.position))
        );
        // closestPlayers.ForEach(a => Debug.Log(a.transform.position));
        return closestPlayers.Take(count).ToList();
    }

    void DrawLines(List<Vector3> vertices)
    {
        int nVertices = vertices.Count;
        // Choose different lines for different abilities
        ChooseLineRenderer(nVertices);
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

    // Calculate the angles for each vertice of the shape
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
            float angle = GetAngle(angleVertices);
            angles.Add(angle);
        } 

        return angles;
    }

    bool IsConvex(List<float> angles)
    {
        int count = angles.Count;
        float sum = angles.Sum();

        // sum is a sum of floating points so we put 0.1 as an allowed error margin
        if(Mathf.Abs(sum - ((count - 2) * 180f)) > 0.1)
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
        return angle;
    }

    float CalculateScore(List<float> angles)
    {
        float score = 0;
        int count = angles.Count;
        // The angle for a regular polygon
        float regularAngle = ((count - 2) * 180)/count;
        
        // Adding how much each angle is close to a regular angle
        foreach (float angle in angles)
        {
            score += Mathf.Abs(angle - regularAngle);
        }
        // Getting the inverse because the value will be smaller the more regular the shape is
        // And we divide by count so shapes with more vertices are not penalised
        score = 1 / (1 + score/((count-2)*180)); 
        return score;
    }

    // This function delays the disabling of the line renderer so the players can see 
    // the lines for a moment.
    // TODO: Should be replaced with an animation
    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        lineRenderer.enabled = false;
    }

    void ChooseLineRenderer(int nVertices)
    {
        if (nVertices == 3)
        {
            lineRenderer = triangleLineRenderer;
        }
        else if (nVertices == 4)
        {
            lineRenderer = squareLineRenderer;
        }
        else if (nVertices == 5)
        {
            lineRenderer = pentagonLineRenderer;
        }
    }
}
