using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class SquareShape : NetworkBehaviour
{
    private List<CircleCornerCollider> circleColliders;
    public override void Spawned()
    {
        circleColliders = new List<CircleCornerCollider>();
        if (HasStateAuthority)
        {
            GameObject circleColliderPrefab = Resources.Load("Prefabs/CircleCornerCollider") as GameObject;
            for (int i = 0; i < 4; i++)
            {
                // The prefab is disabled by default
                
                NetworkObject circleColliderObject = PrefabFactory.SpawnCircleCollider(Runner, circleColliderPrefab);

                CircleCornerCollider temp = circleColliderObject.GetComponent<CircleCornerCollider>();
                temp.team = transform.GetComponentInParent<Player>().GetTeam();
                circleColliders.Add(temp);
            }
        }
    }

    public void CastAbility(List<Vector3> playerPositions, float score)
    {
        for (int i=0; i<4; i++)
        {
            circleColliders[i].ActivateCollider(playerPositions[i]);
        }

        //triangleCollider.SetScore(score);
        //edgeCollider.SetPoints(points);
        //edgeCollider.enabled = true;
        //StartCoroutine(DelayDisable(0.1f));
    }
    
}
