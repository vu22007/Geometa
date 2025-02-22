using Fusion;
using UnityEngine;

public class PickupFlag : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnPickupChanged))] private NetworkBool isPickedUp { get; set; } // Track if the object is picked up
    [Networked] private Player picker { get; set; } // Track which player picked up the object
    [Networked] public int team { get; set; }

    public void OnCreated(int team)
    {
        // Initialize the object's state
        isPickedUp = false;
        picker = null;
        this.team = team;
    }

    public override void Spawned()
    {
        // Set state depending on isPickedUp networked variable
        OnPickupChanged();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();
            if (player != null && !isPickedUp && !player.isCarrying && player.IsAlive())
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
            player.CarryObject(Object);
            SetActive(false);
            Debug.Log($"flag has been pickup");
        }
    }

    public void Drop()
    {
        if (isPickedUp) // Only allow drop if the object is picked up
        {
            isPickedUp = false;
            picker = null;
            SetActive(true);
            Debug.Log($"flag dropped");
        }
    }

    void SetActive(bool active)
    {
        gameObject.GetComponent<Renderer>().enabled = active;
        gameObject.GetComponent<Collider2D>().enabled = active;
    }

    void OnPickupChanged()
    {
        // Disable flag if picked up, otherwise enable flag
        if (isPickedUp)
            SetActive(false);
        else
            SetActive(true);
    }
}
