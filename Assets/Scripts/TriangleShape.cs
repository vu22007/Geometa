using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleShape : NetworkBehaviour
{
    private TriangleCollider triangleCollider;
    private EdgeCollider2D edgeCollider;
    public override void Spawned()
    {
        // This is the object
        GameObject worldColliderPrefab = Resources.Load("Prefabs/TriangleCollider") as GameObject;
        NetworkObject triangleColliderObject = PrefabFactory.SpawnWorldCollider(Runner, worldColliderPrefab);

        // This is the script of the object
        triangleCollider = triangleColliderObject.GetComponent<TriangleCollider>();
        triangleCollider.team = transform.GetComponentInParent<Player>().GetTeam();

        edgeCollider = triangleCollider.GetComponent<EdgeCollider2D>();
    }

    public void CastAbility(List<Vector3> playerPositions, float score)
    {
        List<Vector2> points = new List<Vector2>();
        points.Add(playerPositions[2]);
        foreach (Vector3 position in playerPositions)
        {
            points.Add(new Vector2(position.x, position.y));
        }

        triangleCollider.SetScore(score);
        edgeCollider.SetPoints(points);
        edgeCollider.enabled = true;
        StartCoroutine(DelayDisable(0.1f));
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        edgeCollider.enabled = false;
        triangleCollider.RestartCollider();
    }
}
