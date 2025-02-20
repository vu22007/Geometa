using Fusion;
using UnityEngine;

public class TriangleCollider : NetworkBehaviour
{
    // [Networked] EdgeCollider2D edgeCollider { get; set; }
    [Networked] public int team { get; set; }
    [Networked] float score { get; set; }

    // This object is created with no parent because it should be static with a 
    // position in (0, 0, 0). If the object is attached to the ShapeController the
    // edge collider works with local coordinates and the coordinates of the vertices
    // change with the rotation of the player which is a problem
    void Spawn()
    {
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        if(collider.gameObject.tag == "Player")
        {
            Player player = collider.GetComponentInParent<Player>();
            Debug.Log("Edge team: " + team);
            Debug.Log("Player team: " + player.GetTeam());
            // Debug.Log("Collided with teamate");
            if (player.GetTeam() != team)
            {
                Debug.Log("Collided with enemy");
                //player.TakeDamage(10f * score);
            }
        }
    }
}
