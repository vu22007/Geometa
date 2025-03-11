using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TriangleCollider : NetworkBehaviour
{
    [Networked, HideInInspector] public int team { get; set; }
    [Networked] float score { get; set; }
    [Networked] public PlayerRef parentPlayerRef { get; set; }

    private EdgeCollider2D edgeCollider;

    List<Player> zappedPlayers { get; set; } = new List<Player>();

    // This object is created with no parent because it should be static with a 
    // position in (0, 0, 0). If the object is attached to the ShapeController the
    // edge collider works with local coordinates and the coordinates of the vertices
    // change with the rotation of the player which is a problem
    public override void Spawned()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        edgeCollider.enabled = false;
        edgeCollider.isTrigger = true;
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
