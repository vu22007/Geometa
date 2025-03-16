using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
using UnityEngine;

public class ShapeController : NetworkBehaviour
{
    [Networked] public bool isActive { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }
    [Networked, OnChangedRender(nameof(OnTriangleActivated))] private int activatedTriangle { get; set; }
    [Networked, OnChangedRender(nameof(OnSquareActivated))] private int activatedSquare { get; set; }
    [Networked] private float score { get; set; }
    [Networked, Capacity(5)] private NetworkLinkedList<Vector3> vertices { get; }
    [Networked] private TickTimer triangleCooldownTimer { get; set; }
    [Networked] private TickTimer squareCooldownTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnActivateShapeFailed))] private int activateShapeFailed { get; set; }
    [Networked, Capacity(50)] private string errorMessage { get; set; }

    private List<Vector3> playerPositions;
    private int triangleCost = 3;
    private int squareCost = 5;
    private float triangleCooldown = 3f;
    private float squareCooldown = 5f;

    GameController gameController { get; set; }
    Player parentPlayer { get; set; }

    private float maxDistance = 30;

    private LineRenderer triangleLineRenderer;
    private LineRenderer squareLineRenderer;

    private TriangleShape triangleShape;
    private SquareShape squareShape;

    private AudioSource audioSource;
    private AudioClip triangleKnightSound;
    private AudioClip triangleWizardSound;
    private AudioClip squareKnightSound;
    private AudioClip squareWizardSound;

    // Shape controller intialisation (called on each client and server when shape controller is spawned on network)
    public override void Spawned()
    {
        // Get game controller component
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();

        parentPlayer = GetComponentInParent<Player>();
   
        triangleShape = GetComponentInChildren<TriangleShape>();
        squareShape = GetComponentInChildren<SquareShape>();

        triangleLineRenderer = triangleShape.GetComponent<LineRenderer>();
        triangleLineRenderer.enabled = false; 
        squareLineRenderer = squareShape.GetComponent<LineRenderer>();
        squareLineRenderer.enabled = false;

        audioSource = GetComponentInParent<AudioSource>();
        triangleKnightSound = Resources.Load<AudioClip>("Sounds/TriangleKnight");
        triangleWizardSound = Resources.Load<AudioClip>("Sounds/Shoot");
        squareKnightSound = Resources.Load<AudioClip>("Sounds/SquareKnight");   
        squareWizardSound = Resources.Load<AudioClip>("Sounds/Shoot");

        isActive = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!isActive) return;

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this shape controller)
        // So the following is ran for just the server and the client who controls this shape controller
        if (GetInput(out NetworkInputData input))
        {
            // On key down for specific shape
            if (input.buttons.IsSet(InputButtons.Triangle)) TrianglePerformed();
            if (input.buttons.IsSet(InputButtons.Square)) SquarePerformed();

            if (input.buttons.WasReleased(previousButtons, InputButtons.Triangle))
            {
                TriangleActivated();
            }
            if (input.buttons.WasReleased(previousButtons, InputButtons.Square))
            {
                SquareActivated();
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
            PreviewShape(3, false);
        }
    }

    private void SquarePerformed()
    {
        // Preview shape only locally 
        // The line renderer will be disable for all others
        if (HasInputAuthority)
        {
            PreviewShape(4, false);
        }
    }

    private void TriangleActivated()
    {
        PreviewShape(3, true);
    }

    private void SquareActivated()
    {
        PreviewShape(4, true);
    }

    void PreviewShape(int nVertices, bool activate)
    {
        // Stop both shape preview and activation if cooldown or point requirements are not met
        if (nVertices == 3)
        {
            if (triangleCooldownTimer.IsRunning && !triangleCooldownTimer.Expired(Runner))
            {
                if (activate)
                {
                    errorMessage = "Cooldown on triangle!";
                    activateShapeFailed++;
                }
                return;
            }

            if (parentPlayer.GetPoints() < triangleCost)
            {
                triangleLineRenderer.enabled = false;
                if (activate)
                {
                    errorMessage = "Not enough points!";
                    activateShapeFailed++;
                }
                return;
            }
        }
        else if (nVertices == 4)
        {
            if (squareCooldownTimer.IsRunning && !squareCooldownTimer.Expired(Runner))
            {
                if (activate)
                {
                    errorMessage = "Cooldown on square!";
                    activateShapeFailed++;
                }
                return;
            }

            if (parentPlayer.GetPoints() < squareCost)
            {
                squareLineRenderer.enabled = false;
                if (activate)
                {
                    errorMessage = "Not enough points!";
                    activateShapeFailed++;
                }
                return;
            }
        }

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
            ChooseLineRenderer(nVertices).enabled = false;
            if (activate)
            {
                errorMessage = "Not enough players to activate shape!";
                activateShapeFailed++;
            }
            return;
        }

        // checking if all players are close enough
        if (Vector3.Distance(parentPlayer.transform.position, playerPositions.Last()) > maxDistance)
        {
            ChooseLineRenderer(nVertices).enabled = false;
            if (activate)
            {
                errorMessage = "Players too far away to activate shape!";
                activateShapeFailed++;
            }
            return;
        }

        // Sort by angle relative to centroid, counterclockwise. If this isn't done 
        // we might connect the diagonal of square instead of the edge
        playerPositions = SortVerticesAroundCentroid(playerPositions);
        this.playerPositions = playerPositions;

        // Calculate the angles for each vertice of the shape
        List<float> angles = GetAngles(playerPositions);

        float score = CalculateScore(angles);

        // Give buffs/do damage if the player activates the ability, and make shape visible to everyone
        if (activate)
        {
            // Set score and vertices networked properties for everyone (server, input authority and all other clients) to use to draw lines in OnShapeActivationToggleChanged method
            this.score = score;
            vertices.Clear();
            foreach (Vector3 position in playerPositions)
            {
                vertices.Add(position);
            }

            if (nVertices == 3)
            {
                // Signal that the triangle was activated for OnTriangleActivated to be called
                activatedTriangle++;

                triangleCooldownTimer = TickTimer.CreateFromSeconds(Runner, triangleCooldown);
                parentPlayer.SpendPoints(triangleCost);

                if (parentPlayer.GetCharacterName() == "Wizard")
                {
                    parentPlayer.ActivateTri(true);
                }
                else
                {
                    triangleShape.CastAbility(playerPositions, score);
                }
            }
            else if (nVertices == 4)
            {
                // If it's not convex don't activate it
                if (!IsConvex(angles))
                {
                    squareLineRenderer.enabled = false;
                    errorMessage = "Shape is not convex!";
                    activateShapeFailed++;
                    return;
                }
                else
                {
                    // Signal that the square was activated for OnSquareActivated to be called
                    activatedSquare++;

                    squareCooldownTimer = TickTimer.CreateFromSeconds(Runner, squareCooldown);
                    parentPlayer.SpendPoints(squareCost);

                    squareShape.CastAbility(playerPositions, score);
                }
            }
        }

        // Draw lines locally when just preview
        if (HasInputAuthority && !activate)
        {
            DrawLines(playerPositions, false, score);
        }
    }

    void OnActivateShapeFailed()
    {
        if (HasInputAuthority)
        {
            parentPlayer.ShowMessage(errorMessage, 0.2f, Color.white);
        }
    }

    void OnTriangleActivated()
    {
        triangleLineRenderer.enabled = false;

        parentPlayer.activateTriCD(triangleCooldown);

        if (parentPlayer.GetCharacterName() == "Knight")
        {
            PlayShapeSound(vertices.ToArray(), 3, "Knight");
        }

        // Draw triangle for everyone
        triangleShape.DrawTriangle(vertices.ToList(), true, score);
    }

    void OnSquareActivated()
    {
        parentPlayer.activateSqCD(squareCooldown);
        PlayShapeSound(playerPositions.ToArray(), 4, "Knight");

        // Draw square for everyone
        DrawLines(vertices.ToList(), true, score);
    }

    public void PlayShapeSound(Vector3[] playerPositions, int nVertices, string character)
    {
        // Play sound locally if the players activating are in view 
        foreach (Vector3 pos in playerPositions) {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);
            bool onScreen =
                viewportPos.x >= 0f && viewportPos.x <= 1f &&
                viewportPos.y >= 0f && viewportPos.y <= 1f;
            if (onScreen)
            {
                if (nVertices == 3)
                {
                    if (character == "Knight")
                    {
                        audioSource.PlayOneShot(triangleKnightSound);
                    }
                    else
                    {
                        audioSource.PlayOneShot(triangleWizardSound);
                    }
                } else if (nVertices == 4)
                {
                    if (character == "Knight")
                    {
                        audioSource.PlayOneShot(squareKnightSound);
                    }
                    else
                    {
                        audioSource.PlayOneShot(squareWizardSound);
                    }
                }
                // If some of the players activating is on screen return
                return;
            }
        }
    }

    private List<Player> GetClosestPlayers(Player currentPlayer, int count)
    {
        List<Player> alivePlayers = gameController.GetAlivePlayers();
        // This can be optimised by having alive players separately if it slows down runtime
        List<Player> closestPlayers = new List<Player>(alivePlayers).FindAll(a => a.GetTeam() == currentPlayer.GetTeam());
        closestPlayers.Remove(currentPlayer);
        Vector3 position = currentPlayer.transform.position;

        //Sorting players by distance, 
        closestPlayers.Sort((b, a) =>
            Vector3.Distance(position, b.transform.position).CompareTo(Vector3.Distance(position, a.transform.position))
        );
        return closestPlayers.Take(count).ToList();
    }

    void DrawLines(List<Vector3> vertices, bool activate, float score)
    {
        int nVertices = vertices.Count;

        // Choose different lines for different abilities
        LineRenderer lineRenderer = ChooseLineRenderer(nVertices);
        lineRenderer.positionCount = nVertices + 1;

        // Lines are drawn between the adjacent vertices. The last vertice is added first so there
        // is a line between 0th and (nVertices - 1)th vertice
        lineRenderer.SetPosition(0, vertices[nVertices - 1]);
        for (int i = 0; i < nVertices; i++)
        {
            lineRenderer.SetPosition(i + 1, vertices[i]);
        }

        lineRenderer.startWidth = 0.5f;
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        if (activate)
        {
            startColor.a = 1f * score;
            endColor.a = 1f * score;
        }
        // More transparent color for preview
        else
        {
            startColor.a = 0.3f;
            endColor.a = 0.3f;
        }

        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
        lineRenderer.enabled = true;

        if (activate)
        {
            StartCoroutine(DelayDisable(1f, lineRenderer));
        }
    }

    // This function gradually increases the transparency of the line renderer 
    IEnumerator DelayDisable(float delay, LineRenderer lineRenderer)
    {
        float timer = 0f;
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;
        float startAlpha = startColor.a;

        while (timer < delay)
        {
            timer += Time.deltaTime;

            // Calculate a new alpha based on the elapsed time
            float newAlpha = Mathf.Lerp(startAlpha, 0f, timer / delay);
            startColor.a = endColor.a = newAlpha;
            
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;

            yield return null;
        }

        lineRenderer.enabled = false;
    }

    LineRenderer ChooseLineRenderer(int nVertices)
    {
        if (nVertices == 3)
        {
            return triangleLineRenderer;
        }
        else
        {
            return squareLineRenderer;
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
}
