using Fusion;
using UnityEngine;

public class AoESpell : NetworkBehaviour
{
    [Networked] public float damage { get; set; }
    [Networked] public int team { get; set; }
    [Networked] public float duration { get; set; }
    [Networked] private TickTimer despawnTimer { get; set; }
    [Networked] private PlayerRef playerCasting { get; set; }

    public void OnCreated(float damage, int team, float duration, PlayerRef playerCasting)
    {
        this.damage = damage;
        this.team = team;
        this.duration = duration;
        this.playerCasting = playerCasting;

        despawnTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    public override void FixedUpdateNetwork()
    {
        // Check if the despawn timer has expired
        if (despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                player.TakeDamage(damage * Runner.DeltaTime, playerCasting);
            }
        }
    }
    
}