using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCornerCollider : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] float score { get; set; }
    private CircleCollider2D circleCollider;
    private List<Player> slowedPlayers;
    public override void Spawned()
    {
        slowedPlayers = new List<Player>();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.enabled = false;
    }

    public void ActivateCollider(Vector3 pos, float score)
    {
        this.score = score;
        
        circleCollider.transform.position = pos;
        circleCollider.enabled = true;

        StartCoroutine(DelayDisable(0.1f));
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        circleCollider.enabled = false;
        // Set an empty list when ability ends
        slowedPlayers = new List<Player>();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            Player player = collider.GetComponentInParent<Player>();

            // This doesn't allow players to be damaged twice from the same ability
            // because the collider exists for 0.1 seconds and multiple frames
            if (player.GetTeam() != team && !slowedPlayers.Contains(player))
            {
                Debug.Log("Enemy slowed");
                // The score determines how much the player will be slowed with the max amount of 2 = 2*1
                // It is set in the activate collider function above so it gets set before the collider gets enabled
                player.GetSlowed(2f * score, 1f);
                slowedPlayers.Add(player);
            }
        }
    }
}
