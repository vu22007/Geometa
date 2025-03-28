using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleShape : NetworkBehaviour
{
    [Networked] private TickTimer disableTimer { get; set; }

    private TriangleCollider triangleCollider;
    private PolygonCollider2D polygonCollider;
    private float triangleSizeOffset = 0.8f;

    public override void Spawned()
    {
        // Only the server can spawn the collider
        if (HasStateAuthority)
        {
            Player parentPlayer = GetComponentInParent<Player>();
            PlayerRef parentPlayerRef = parentPlayer.Object.InputAuthority;
            int team = parentPlayer.GetTeam();

            // Spawn the triangle collider
            GameObject worldColliderPrefab = Resources.Load("Prefabs/TriangleCollider") as GameObject;
            NetworkObject triangleColliderObject = PrefabFactory.SpawnWorldCollider(Runner, parentPlayerRef, worldColliderPrefab, team);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (disableTimer.Expired(Runner))
        {
            polygonCollider.enabled = false;
            triangleCollider.RestartCollider();
            disableTimer = TickTimer.None;
        }
    }

    public void CastAbility(List<Vector3> playerPositions, float score)
    {
        List<Vector2> points = new List<Vector2>();
        foreach (Vector3 position in playerPositions)
        {
            points.Add(new Vector2(position.x, position.y));
        }

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < points.Count; i++)
        {
            centroid += points[i];
        }
        centroid /= points.Count;  // for a triangle, this is the average of the three vertices

        // 2. Scale around centroid
        for (int i = 0; i < points.Count; i++)
        {
            // Translate vertex so that centroid is at origin
            Vector2 direction = points[i] - centroid;

            float distance = direction.magnitude;

            direction = direction.normalized * (distance + triangleSizeOffset);
            points[i] = centroid + direction;
        }


        triangleCollider.SetScore(score);
        polygonCollider.points = points.ToArray();
        polygonCollider.enabled = true;
        disableTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }

    public void DrawTriangle(List<Vector3> vertices, bool activate, float score)
    {
        // Convert to 2D
        List<Vector2> points2D = new List<Vector2>();
        foreach (var v in vertices)
        {
            points2D.Add(new Vector2(v.x, v.y));
        }

        // Compute 2D centroid
        Vector2 centroid2D = Vector2.zero;
        for (int i = 0; i < points2D.Count; i++)
        {
            centroid2D += points2D[i];
        }
        centroid2D /= points2D.Count;

        // Offset each vertex *in 2D* by offsetAmount
        for (int i = 0; i < points2D.Count; i++)
        {
            Vector2 direction = points2D[i] - centroid2D;
            float distance = direction.magnitude;
            if (distance > 0f)
            {
                direction = direction.normalized * (distance + triangleSizeOffset);
                points2D[i] = centroid2D + direction;
            }
        }

        // Now rebuild the 3D vertices:
        // Keep original z, but apply the updated x,y
        for (int i = 0; i < vertices.Count; i++)
        {
            var oldZ = vertices[i].z;
            vertices[i] = new Vector3(points2D[i].x, points2D[i].y, oldZ);
        }

        triangleCollider.DrawTriangle(vertices, activate, score);
    }

    // For triangle collider to call when it spawns
    public void RegisterTriangleCollider(TriangleCollider triangleCollider)
    {
        this.triangleCollider = triangleCollider;
        polygonCollider = triangleCollider.GetComponent<PolygonCollider2D>();
    }
}
