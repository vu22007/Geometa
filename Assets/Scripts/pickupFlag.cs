using Fusion;
using UnityEngine;

public class pickupFlag : NetworkBehaviour
{
    [Networked] private NetworkBool isPickedUp { get; set; } // Track if the object is picked up
    [Networked] private Player picker { get; set; } // Track which player picked up the object
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    public override void Spawned()
    {
        // Initialize the object's state when it spawns
        isPickedUp = false;
        picker = null;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null && !isPickedUp)
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
            rb.bodyType = RigidbodyType2D.Kinematic; // Stop physics movement
            transform.SetParent(player.holdPosition);
            transform.localPosition = new Vector3(0.7f, 0, 0);
            Debug.Log($"flag has been pickup");
        }
    }

    public void Drop()
    {
        if (isPickedUp) // Only allow drop if the object is picked up
        {
            isPickedUp = false;
            picker = null;
            rb.bodyType = RigidbodyType2D.Dynamic;
            transform.SetParent(null); // Detach from the player
            Debug.Log($"flag dropped");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (isPickedUp && picker != null)
        {
            // Move the object to the player's hold position
            transform.position = picker.holdPosition.position + picker.transform.right * 0.7f;
        }
    }
}
