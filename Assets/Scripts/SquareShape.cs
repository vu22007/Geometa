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
            GameObject circleColliderPrefab = Resources.Load("Prefabs/CircleCornerCollider") as GameObject;
            for (int i = 0; i < 4; i++)
            {
                NetworkObject circleColliderObject = PrefabFactory.SpawnCircleCollider(Runner, circleColliderPrefab);
                CircleCornerCollider temp = circleColliderObject.GetComponent<CircleCornerCollider>();
                temp.team = transform.GetComponentInParent<Player>().GetTeam();
                circleColliders.Add(temp);
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
}