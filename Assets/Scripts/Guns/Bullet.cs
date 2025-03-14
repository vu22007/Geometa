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
    [Networked] int team { get; set; }
    [Networked] private PlayerRef playerShooting { get; set; }
    [Networked] private TickTimer bulletLifespanTimer { get; set; }

    GameController gameController;

    // Bullet intialisation (called from a player object on server when creating the bullet)
    public void OnCreated(Vector2 position, Vector2 direction, Quaternion rotation, float speed, float damage, int team, PlayerRef playerShooting)
    {
        initialTick = Runner.Tick;
        initialPosition = position;
        velocity = direction.normalized * speed;
        this.rotation = rotation;
        this.damage = damage;
        done = false;
        this.team = team;
        this.playerShooting = playerShooting;

        float lifespan = 20.0f;
        bulletLifespanTimer = TickTimer.CreateFromSeconds(Runner, lifespan);
    }

    // Bullet initialisation (called on each client and server when bullet is spawned on network)
    public override void Spawned()
    {
        // Make FixedUpdateNetwork run on all clients
        Runner.SetIsSimulated(Object, true);

        // Get game controller component
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();

        // Rotate the bullet into the correct orientation
        transform.rotation = rotation;

        // If a dummy bullet for this bullet exists then destroy it
        foreach (DummyBullet dummyBullet in FindObjectsByType<DummyBullet>(FindObjectsSortMode.None))
        {
            // If this bullet and the dummy bullet were created in the same tick, then the dummy bullet
            // corresponds to this bullet, so destroy it
            if (dummyBullet.GetInitialTick() == initialTick)
            {
                Destroy(dummyBullet.gameObject);
            }
        }
    }

    public override void Render()
    {
        if (done) return;

        // Set current position of bullet
        Vector3 position = GetMovePosition(Runner.Tick);
        gameObject.transform.position = position;
    }

    // Runs on all clients and host, so that the clients can react the moment the bullet hits something
    public override void FixedUpdateNetwork()
    {
        if (done) return;

        // Check if bullet's lifespan has been reached
        if (bulletLifespanTimer.Expired(Runner))
        {
            BulletDone();
            return;
        }

        // Get current position of bullet for hit detection purposes
        Vector3 position = GetMovePosition(Runner.Tick);

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
                    player.TakeDamage(damage, playerShooting);
                    BulletDone(); // Doesn't pierce
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
            BulletDone();
        }
    }

    void BulletDone()
    {
        done = true;

        // Despawn bullet from network (only the server can do this)
        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
        // Hide the bullet so clients can't see it until the server's despawn is received
        else
        {
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }
}
