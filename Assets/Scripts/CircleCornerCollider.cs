using Fusion;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCornerCollider : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    private CircleCollider2D circleCollider;
    private List<Player> slowedPlayers;
    public override void Spawned()
    {
        slowedPlayers = new List<Player>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    public void ActivateCollider(Vector3 pos)
    {
        circleCollider.transform.position = pos;
        circleCollider.enabled = true;

        StartCoroutine(DelayDisable(0.1f));
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        circleCollider.enabled = false;
        //triangleCollider.RestartCollider();
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
                Debug.Log("Collided with enemy");
                // player.GetSlowed(10f * score);
                slowedPlayers.Add(player);
            }
        }
    }
}
