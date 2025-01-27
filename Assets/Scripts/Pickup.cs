using UnityEngine;

public class Pickup : MonoBehaviour
{
    int amount;
    int type;
    SpriteRenderer spriteRenderer;

    public void OnCreated(int type, int amount){
        this.type = type;
        this.amount = amount;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

        Sprite sprite = GetSprite();

        spriteRenderer.sprite = sprite;
    }

    void OnTriggerEnter2D(Collider2D other){
        // Check if object is a player
        if (other.CompareTag("Player")){
            Player player = other.GetComponent<Player>();
            if (player != null) {
                // Check if player is from the enemy team
                switch (type)
                {
                    case 0: //Health
                        player.Heal(amount);
                        break;
                    case 1: //Points
                        player.GainPoints(amount);
                        break;
                    default:
                        Debug.Log("Unknown type of pickup");
                        break;
                }
            }
        }
    }

    Sprite GetSprite()
    {
        Sprite sprite = null;

        switch (type)
        {
            case 0: //Health
                sprite = Resources.Load("Sprites/HealthPickup") as Sprite;
                break;
            case 1: //Points
                sprite = Resources.Load("Sprites/PointsPickup") as Sprite;
                break;
            default:
                Debug.Log("Unknown type of pickup");
                break;
        }

        return sprite;
    }
}
