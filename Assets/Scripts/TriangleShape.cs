using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleShape : NetworkBehaviour
{
    [Networked] private TickTimer disableTimer { get; set; }

    private TriangleCollider triangleCollider;
    private PolygonCollider2D polygonCollider;

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

        triangleCollider.SetScore(score);
        polygonCollider.points = points.ToArray();
        polygonCollider.enabled = true;
        disableTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }

    public void DrawTriangle(List<Vector3> vertices, bool activate, float score)
    {
        triangleCollider.DrawTriangle(vertices, activate, score);
    }

    // For triangle collider to call when it spawns
    public void RegisterTriangleCollider(TriangleCollider triangleCollider)
    {
        this.triangleCollider = triangleCollider;
        polygonCollider = triangleCollider.GetComponent<PolygonCollider2D>();
    }
}
