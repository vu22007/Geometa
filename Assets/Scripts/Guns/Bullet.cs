using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [Networked] int initialTick { get; set; }
    [Networked] Vector2 initialPosition { get; set; }
    [Networked] Vector2 velocity { get; set; }
    [Networked] Quaternion rotation { get; set; }
    [Networked] float damage { get; set; }
    [Networked] public bool done { get; set; }
    [Networked] float lifespan { get; set; }
    [Networked] int team { get; set; }

    GameController gameController;

    // Bullet intialisation (called from a player object on server when creating the bullet)
    public void OnCreated(Vector2 position, Vector2 direction, Quaternion rotation, float speed, float damage, int team)
    {
        initialTick = Runner.Tick;
        initialPosition = position;
        velocity = direction.normalized * speed;
        this.rotation = rotation;
        this.damage = damage;
        done = false;
        lifespan = 20.0f;
        this.team = team;
    }

    // Bullet initialisation (called on each client and server when bullet is spawned on network)
    public override void Spawned()
    {
        // Find game controller component (Fusion creates copies of the game controller object so we need to choose the correct one)
        if (GameObject.Find("Host") != null)
            gameController = GameObject.Find("Host").GetComponent<GameController>();
        else
            gameController = GameObject.Find("Client A").GetComponent<GameController>();

        // Add this bullet to game controller bullet list
        gameController.RegisterBullet(this);

        // Rotate the bullet into the correct orientation
        transform.rotation = rotation;
    }

    // Called on each client and server when bullet is despawned from network
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Remove this bullet from game controller bullet list
        gameController.UnregisterBullet(this);
    }

    // Update function (called from the game controller on all clients and server with the list of all currently active bullets)
    public void BulletUpdate()
    {
        // Check if bullet's lifespan has been reached
        lifespan -= Runner.DeltaTime;
        if (lifespan <= 0.0f)
        {
            done = true;
            return;
        }

        // Set current position of bullet
        Vector3 position = GetMovePosition(Runner.Tick);
        gameObject.transform.position = position;

        // Lag-compensated hit detection
        int layerMask = LayerMask.GetMask("Default"); // Only register collisions with colliders and hitboxes on the "Default" layer
        HitOptions options = HitOptions.IncludeBox2D | HitOptions.IgnoreInputAuthority;
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
        if (Runner.LagCompensation.OverlapBox(position, new Vector3(0.07f, 0.13f, 0), rotation, Object.InputAuthority, hits, layerMask, options) != 0)
        {
            // Resolve collision
            OnCollision(hits[0]);
        }
    }

    // Get current position given current tick using initial state of bullet
    Vector3 GetMovePosition(int currentTick)
    {
        float time = (currentTick - initialTick) * Runner.DeltaTime;

        if (time <= 0.0f)
            return initialPosition;

        Vector2 position = initialPosition + velocity * time;

        return new Vector3(position.x, position.y);
    }

    // To be called when a lag-compensated hit occurs
    void OnCollision(LagCompensatedHit hit)
    {
        // Check if collision is with a Hitbox or Collider2D
        if (hit.Hitbox != null)
            OnCollisionHitbox(hit.Hitbox);
        else if (hit.Collider2D != null)
            OnCollisionCollider2D(hit.Collider2D);
    }

    // On colliding with a hitbox (e.g. a player)
    void OnCollisionHitbox(Hitbox hitbox)
    {
        // Check if object is a player
        if (hitbox.CompareTag("Player"))
        {
            Player player = hitbox.GetComponent<Player>();
            if (player != null)
            {
                // Check if player is from the enemy team
                if (player.GetTeam() != team)
                {
                    player.TakeDamage(damage);
                    done = true; // Doesn't pierce
                }
            }
        }
    }

    // On colliding with a Collider2D (e.g. a wall)
    void OnCollisionCollider2D(Collider2D collider)
    {
        // Destroy bullet if collide with wall
        if (collider.CompareTag("Wall"))
        {
            done = true;
        }
    }
}
