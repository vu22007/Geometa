using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Networked] Vector2 velocity { get; set; }
    [Networked] float damage { get; set; }
    [Networked] public bool done { get; set; }
    [Networked] float lifespan { get; set; }
    [Networked] int team { get; set; }

    // Bullet intialisation (called from a player object on server when creating the bullet)
    public void OnCreated(Vector2 startDirection, float speed, float damage, int team)
    {
        velocity = startDirection.normalized * speed;
        this.damage = damage;
        done = false;
        lifespan = 20.0f;
        this.team = team;
    }

    // Bullet initialisation (called on each client and server when bullet is spawned on network)
    public override void Spawned()
    {
        // Add this bullet to game controller bullet list (do this for all found game controllers to ensure that it is added to the correct one)
        foreach (GameObject gameControllerObject in GameObject.FindGameObjectsWithTag("GameController"))
        {
            GameController gameController = gameControllerObject.GetComponent<GameController>();
            gameController.RegisterBullet(this);
        }
    }

    // Called on each client and server when bullet is despawned from network
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Remove this bullet from game controller bullet list (do this for all found game controllers to ensure that it is removed from the correct one)
        foreach (GameObject gameControllerObject in GameObject.FindGameObjectsWithTag("GameController"))
        {
            GameController gameController = gameControllerObject.GetComponent<GameController>();
            gameController.UnregisterBullet(this);
        }
    }

    // On colliding with a collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if object is a player
        if (other.CompareTag("Player")) {
            Player player = other.GetComponent<Player>();
            if (player != null) {
                // Check if player is from the enemy team
                if (player.GetTeam() != team) {
                    player.TakeDamage(damage);
                    done = true; // Doesn't pierce
                }
            }
        }
    }

    // Update function (called from the game controller on all clients and server with the list of all currently active bullets)
    public void BulletUpdate()
    {
        gameObject.transform.position += new Vector3(velocity.x, velocity.y) * Runner.DeltaTime;

        lifespan -= Runner.DeltaTime;
        if (lifespan <= 0.0f) {
            done = true;
        }
    }
}
