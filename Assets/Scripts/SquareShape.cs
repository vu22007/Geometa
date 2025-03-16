using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class SquareShape : NetworkBehaviour
{
    private List<CircleCornerCollider> circleColliders = new List<CircleCornerCollider>();

    public override void Spawned()
    {
        // Only the server can spawn the colliders
        if (HasStateAuthority)
        {
            Player parentPlayer = GetComponentInParent<Player>();
            PlayerRef parentPlayerRef = parentPlayer.Object.InputAuthority;
            int team = parentPlayer.GetTeam();

            GameObject circleColliderPrefab = Resources.Load("Prefabs/CircleCornerCollider") as GameObject;

            for (int i = 0; i < 4; i++)
            {
                // Spawn the circle corner collider
                // Note: The prefab is disabled by default
                NetworkObject circleColliderObject = PrefabFactory.SpawnCircleCollider(Runner, parentPlayerRef, circleColliderPrefab, team);
            }
        }
    }

    public void CastAbility(List<Vector3> playerPositions, float score)
    {
        for (int i = 0; i < 4; i++)
        {
            circleColliders[i].ActivateCollider(playerPositions[i], score);
        }
    }

    // For circle corner collider to call when it spawns
    public void RegisterCircleCornerCollider(CircleCornerCollider circleCornerCollider)
    {
        circleColliders.Add(circleCornerCollider);
    }
}
