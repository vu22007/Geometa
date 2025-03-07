using Fusion;
using UnityEngine;

public class AoESpell : NetworkBehaviour
{
    [Networked] public float damage { get; set; }
    [Networked] public int team { get; set; }
    [Networked] public float duration { get; set; }
    [Networked] private TickTimer despawnTimer { get; set; }
    [Networked] private PlayerRef playerCasting { get; set; }
    [Networked] private Vector2 direction { get; set; }
    [Networked] private float speed { get; set; }
    [Networked] private float maxDistance { get; set; }
    [Networked] private float distanceTraveled { get; set; }
    [Networked] private bool isActivated { get; set; }

    public void OnCreated(Vector2 direction, float speed, float maxDistance, float damage, int team, float duration, PlayerRef playerCasting)
    {
        this.damage = damage;
        this.team = team;
        this.duration = duration;
        this.playerCasting = playerCasting;
        this.direction = direction.normalized;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.distanceTraveled = 0f;
        this.isActivated = false;

    }

    public override void FixedUpdateNetwork()
    {
        if (isActivated)
        {
            // Check if the despawn timer has expired
            if (despawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
        else
        {
            // Move the spell
            Vector2 movement = direction * speed * Runner.DeltaTime;
            transform.position += new Vector3(movement.x, movement.y, 0);
            distanceTraveled += movement.magnitude;

            if (distanceTraveled >= maxDistance)
            {
                isActivated = true;
                despawnTimer = TickTimer.CreateFromSeconds(Runner, duration);
            }
        }
        
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (isActivated && other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                player.TakeDamage(damage * Runner.DeltaTime, playerCasting);
            }
        }
    }
    
}