using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    Vector3 velocity;
    float damage;
    public bool done;
    float lifespan;
    int team;
    
    public void OnCreated(Vector3 startDirection, float speed, float damage, int team)
    {
        velocity = startDirection * speed;
        velocity.z = 0;
        this.damage = damage;
        done = false;
        lifespan = 20.0f;
        this.team = team;
    }

    // On colliding with a collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if object is a player
        if (other.CompareTag("Player")) {
            Player player = other.GetComponent<Player>();
            if (player != null) {
                // Check if player is from the enemy team
                if (player.team != team) {
                    player.TakeDamage(damage);
                    done = true; // Doesn't pierce
                }
            }
        }
    }

    // To be called in the game controller with the list of all currently active bullets
    public void BulletUpdate()
    {
        gameObject.transform.position += velocity * Runner.DeltaTime;
        
        lifespan -= Runner.DeltaTime;
        if (lifespan <= 0.0f) {
            done = true;
        }
    }

    public void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
