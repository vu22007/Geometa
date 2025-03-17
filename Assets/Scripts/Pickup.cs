using Fusion;
using UnityEngine;

public class Pickup : NetworkBehaviour
{
    [Networked] int amount { get; set; }
    [Networked] int type { get; set; }
    [Networked] TickTimer respawnTimer { get; set; }
    private float respawnTime;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider2D;

    // Pickup intialisation (called from game controller on server when creating the pickup)
    public void OnCreated(int type, int amount){
        this.type = type;
        this.amount = amount;
    }

    // Pickup initialisation (called on each client and server when pickup is spawned on network)
    public override void Spawned()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        boxCollider2D = gameObject.GetComponent<BoxCollider2D>();
        Sprite sprite = GetSprite();
        spriteRenderer.sprite = sprite;
        respawnTime = 20f;
    }

    public override void FixedUpdateNetwork()
    {
        if(respawnTimer.Expired(Runner)){
            RespawnPickup();
        }
    }

    void OnTriggerEnter2D(Collider2D other){
        // Check if object is a player
        if (other.CompareTag("Player")){
            Player player = other.GetComponentInParent<Player>();
            if (player != null) {
                // Check if player is from the enemy team
                switch (type)
                {
                    case 0: //Health
                        player.Heal(amount);
                        player.ShowMessage("+"+amount+" health", 0.2f, Color.green);
                        break;
                    case 1: //Mana
                        player.GainMana(amount);
                        player.ShowMessage("+"+amount+" mana", 0.2f, Color.cyan);
                        break;
                    case 2: //Speed
                        player.IncreaseSpeed(amount, 5f);
                        player.ShowMessage("Speed increased!!", 0.3f, Color.green);
                        break;
                    default:
                        Debug.Log("Unknown type of pickup");
                        break;
                }
            }
            DisablePickup();
        }
    }

    Sprite GetSprite()
    {
        Sprite sprite;

        switch (type)
        {
            case 0: //Health
                sprite = Resources.Load<Sprite>("Sprites/HealthPickup2");
                break;
            case 1: //Mana
                sprite = Resources.Load<Sprite>("Sprites/ManaPickup");
                break;
            case 2: //Speed
                sprite = Resources.Load<Sprite>("Sprites/SpeedPickup");
                break;
            default:
                sprite = null;
                break;
        }

        return sprite;
    }

    public void DisablePickup()
    {
        spriteRenderer.enabled = false;
        boxCollider2D.enabled = false;
        respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
    }

    public void RespawnPickup(){
        spriteRenderer.enabled = true;
        boxCollider2D.enabled = true;
    }

    public void DestroyPickup(){
        // Despawn the pickup (only the server can do this)
        if (HasStateAuthority)
        {
            Runner.Despawn(gameObject.GetComponent<NetworkObject>());
        }
    }
}
