using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PickupFlag : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnPickupChanged))] private NetworkBool isPickedUp { get; set; } // Track if the object is picked up
    [Networked] private Player picker { get; set; } // Track which player picked up the object
    [Networked] public int team { get; set; }

    [SerializeField] Image spriteIndicator;
    private SpriteRenderer spriteRenderer;
    private Collider2D flagCollider;

    public void OnCreated(int team)
    {
        // Initialize the object's state
        isPickedUp = false;
        picker = null;
        this.team = team;
    }

    public override void Spawned()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        flagCollider = GetComponent<Collider2D>();

        // Set state depending on isPickedUp networked variable
        OnPickupChanged();

        // Set flag sprite depending on local player's team
        if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject networkPlayerObject))
        {
            Player localPlayer = networkPlayerObject.GetComponent<Player>();
            int playerTeam = localPlayer.GetTeam();

            // Blue for local player's team flag, red for enemy's flag
            string spritePath = playerTeam == team ? "Sprites/BlueFlag" : "Sprites/RedFlag";

            spriteRenderer.sprite = Resources.Load<Sprite>(spritePath);
            spriteIndicator.sprite = Resources.Load<Sprite>(spritePath);
        }
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
        }
    }

    public void Drop()
    {
        if (isPickedUp) // Only allow drop if the object is picked up
        {
            isPickedUp = false;
            picker = null;
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
        SetActive(!isPickedUp);
    }

    public bool IsInsideCollider()
    {
        Vector2 flagDimensions = spriteRenderer.bounds.size;
        int layerMask = LayerMask.GetMask("Default", "Map"); // Only check colliders on the given layers
        Collider2D otherCollider = Physics2D.OverlapBox(transform.position, flagDimensions, 0, layerMask);
        return otherCollider != null;
    }
}
