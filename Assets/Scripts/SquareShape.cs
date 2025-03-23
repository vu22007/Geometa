using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class SquareShape : NetworkBehaviour
{
    private List<CircleCornerCollider> circleColliders;
    [SerializeField] private GameObject summonPrefab;

    public override void Spawned()
    {
        circleColliders = new List<CircleCornerCollider>();

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
        Player activatingPlayer = GetComponentInParent<Player>();

        if (HasStateAuthority && activatingPlayer.GetCharacterName() == "Wizard")
        {
            Vector3 spawnPosition = activatingPlayer.transform.position + new Vector3(0, 1, 0);
            PrefabFactory.SpawnSummon(Runner, summonPrefab, spawnPosition, activatingPlayer.GetTeam(), activatingPlayer.Object.InputAuthority);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                circleColliders[i].ActivateCollider(playerPositions[i], score);
            }
        }
    }

    // For circle corner collider to call when it spawns
    public void RegisterCircleCornerCollider(CircleCornerCollider circleCornerCollider)
    {
        circleColliders.Add(circleCornerCollider);
    }
}