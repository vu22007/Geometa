using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleCollider : NetworkBehaviour
{
    [Networked, HideInInspector] public int team { get; set; }
    [Networked] float score { get; set; }
    [Networked] public PlayerRef parentPlayerRef { get; set; }

    private PolygonCollider2D polygonCollider;

    List<Player> zappedPlayers { get; set; } = new List<Player>();

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Material meshMaterial;

    Color lowScoreColor = new Color(0.2f, 0.4f, 0.8f);  // Blue
    Color medScoreColor = new Color(0.8f, 0.2f, 0.8f);  // Purple
    Color highScoreColor = new Color(1.0f, 0.2f, 0.2f); // Red

    // This object is created with no parent because it should be static with a 
    // position in (0, 0, 0). If the object is attached to the ShapeController the
    // edge collider works with local coordinates and the coordinates of the vertices
    // change with the rotation of the player which is a problem
    public override void Spawned()
    {
        // Set up required components
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();

        meshMaterial = meshRenderer.material;

        polygonCollider = GetComponent<PolygonCollider2D>();
        polygonCollider.enabled = false;
        polygonCollider.isTrigger = true;
    }

    // Activate is always true when used for now but it should stay
    public void DrawTriangle(List<Vector3> vertices, bool activate, float score)
    {
        int nVertices = vertices.Count;

        // Choose different lines for different abilities
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        // Simple triangle indices
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        // Assign the mesh to our mesh filter
        meshFilter.mesh = mesh;
        
        Color meshColor;
        if (score < 0.6f)
            meshColor = Color.Lerp(lowScoreColor, medScoreColor, score / 0.6f);
        else
            meshColor = Color.Lerp(medScoreColor, highScoreColor, (score - 0.6f) / 0.4f);
        meshMaterial.color = meshColor;

        meshRenderer.enabled = true;

        if (activate)
        {
            StartCoroutine(DelayDisableTriangle(1f));
        }
    }

    IEnumerator DelayDisableTriangle(float delay)
    {
        float timer = 0f;
        Color meshColor = meshMaterial.color;
        float startAlpha = meshColor.a;

        while (timer < delay)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, timer / delay);
            meshColor.a = newAlpha;
            meshMaterial.color = meshColor;
            yield return null;
        }

        meshRenderer.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            Player player = collider.GetComponentInParent<Player>();

            // This doesn't allow players to be damaged twice from the same ability
            // because the collider exists for 0.1 seconds and multiple frames
            if (player.GetTeam() != team && !zappedPlayers.Contains(player))
            {
                Debug.Log("Collided with enemy");
                player.TakeDamage(10f * score, parentPlayerRef);
                zappedPlayers.Add(player);
            }
        }
    }

    // Restart the collider after the ability is finished
    public void RestartCollider()
    {
        zappedPlayers = new List<Player>();
    }

    public void SetScore(float score)
    {
        this.score = score;
    }
}
