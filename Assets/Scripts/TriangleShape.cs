using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleShape : NetworkBehaviour
{
    private TriangleCollider triangleCollider;
    private PolygonCollider2D polygonCollider;

    public override void Spawned()
    {
        // Only the server can spawn the collider
        if (HasStateAuthority)
        {
            // This is the object
            GameObject worldColliderPrefab = Resources.Load("Prefabs/TriangleCollider") as GameObject;
            NetworkObject triangleColliderObject = PrefabFactory.SpawnWorldCollider(Runner, worldColliderPrefab);

            PlayerRef parentPlayerRef = GetComponentInParent<Player>().Object.InputAuthority;

            // This is the script of the object
            triangleCollider = triangleColliderObject.GetComponent<TriangleCollider>();
            triangleCollider.team = transform.GetComponentInParent<Player>().GetTeam();
            triangleCollider.parentPlayerRef = parentPlayerRef;

            polygonCollider = triangleCollider.GetComponent<PolygonCollider2D>();
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
        StartCoroutine(DelayDisable(0.1f));
    }

    public void DrawTriangle(List<Vector3> vertices, bool activate, float score)
    {
        triangleCollider.DrawTriangle(vertices, activate, score);
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        polygonCollider.enabled = false;
        triangleCollider.RestartCollider();
    }
}
