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

    // This object is created with no parent because it should be static with a 
    // position in (0, 0, 0). If the object is attached to the ShapeController the
    // edge collider works with local coordinates and the coordinates of the vertices
    // change with the rotation of the player which is a problem
    public override void Spawned()
    {
        // Set up required components
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();

        //if (meshMaterial == null)
        //{
            
        //    //// Create a default material if none is provided
        //    //meshMaterial = new Material(Shader.Find("Default-Line"));

        //}

        meshMaterial = meshRenderer.material;
        meshMaterial.color = Color.blue;
        // meshRenderer.material = meshMaterial;
        // SetupTransparentMaterial();

        polygonCollider = GetComponent<PolygonCollider2D>();
        polygonCollider.enabled = false;
        polygonCollider.isTrigger = true;
    }

    private void SetupTransparentMaterial()
    {
        // Make sure the material is set to be transparent
        meshRenderer.material.SetFloat("_Mode", 3); // Transparent mode
        meshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        meshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        meshRenderer.material.SetInt("_ZWrite", 0);
        meshRenderer.material.DisableKeyword("_ALPHATEST_ON");
        meshRenderer.material.EnableKeyword("_ALPHABLEND_ON");
        meshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        meshRenderer.material.renderQueue = 3000;
    }

    public void DrawTriangle(List<Vector3> vertices, bool activate, float score)
    {
        // Draw debug lines to visualize the triangle in Scene view
        Debug.DrawLine(vertices[0], vertices[1], Color.red, 5f);
        Debug.DrawLine(vertices[1], vertices[2], Color.red, 5f);
        Debug.DrawLine(vertices[2], vertices[0], Color.red, 5f);

        int nVertices = vertices.Count;

        // Choose different lines for different abilities
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        // Simple triangle indices
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        // Assign the mesh to our mesh filter
        meshFilter.mesh = mesh;

        // Set up color and transparency
        Color meshColor = meshMaterial.color;
        if (activate)
        {
            meshColor.a = 1f * score;
        }
        else
        {
            meshColor.a = 0.3f;
        }
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
