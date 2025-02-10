using Fusion;
using UnityEngine;

public class PickupFlag : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnPickupChange))] private NetworkBool isPickedUp { get; set; } // Track if the object is picked up
    [Networked] private Player picker { get; set; } // Track which player picked up the object

    public override void Spawned()
    {
        // Initialize the object's state when it spawns
        isPickedUp = false;
        picker = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();
            if (player != null && !isPickedUp && !player.isCarrying)
            {
                Pickup(player);
            }
        }
    }
    public void Pickup(Player player)
    {
        if (!isPickedUp) // Only allow pickup if the object is not already picked up
        {
            isPickedUp = true;
            picker = player;
            transform.SetParent(player.holdPosition);
            transform.localPosition = new Vector3(0.7f, 0, 0);
            player.CarryObject(Object);
            Debug.Log($"flag has been pickup");
        }
    }

    public void Drop()
    {
        if (isPickedUp) // Only allow drop if the object is picked up
        {
            isPickedUp = false;
            picker = null;
            transform.SetParent(null); // Detach from the player
            Debug.Log($"flag dropped");
        }
    }

    void OnPickupChange()
    {
        if (isPickedUp)
        {
            transform.SetParent(picker.holdPosition); // Attach to the player
            transform.localPosition = new Vector3(0.7f, 0, 0);
        }
        else
        {
            transform.SetParent(null); // Detach from the player
        }
    }

    public override void FixedUpdateNetwork()
    {
        //if (isPickedUp && picker != null)
        //{
        //    // Move the object to the player's hold position
        //    transform.position = picker.holdPosition.position + picker.transform.right * 0.7f;
        //}
    }
}
