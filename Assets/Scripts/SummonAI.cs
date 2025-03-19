using Fusion;
using UnityEngine;
using System.Collections;

public class SummonAI : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] private PlayerRef summonerRef { get; set; }
    [Networked] private float speed { get; set; } = 5f;
    [Networked] private float lifetime { get; set; } = 10f; 
    [Networked] private float damage { get; set; } = 10f; 
    [Networked] private TickTimer despawnTimer { get; set; }
    
    private Coroutine damageCoroutine;

    private Transform target;

    public void OnCreated(int team, PlayerRef summoner)
    {
        this.team = team;
        this.summonerRef = summoner;
    }

    public override void Spawned()
    {
        despawnTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        FindTarget();
    }

    public override void FixedUpdateNetwork()
    {
        // Make sure there's a target
        if (target != null)
        {
            // Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;

            // Rotate sprite to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

    
        if (despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Runner.DeltaTime;
        }
        else
        {
            FindTarget();
        }

    }

    private void FindTarget()
    {
        Player[] players = FindObjectsOfType<Player>();
        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (Player player in players)
        {
            if (player.GetTeam() != team) // Find enemy
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player.transform;
                }
            }
        }

        target = closestPlayer;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();
            if (player != null && player.GetTeam() != team)
            {
                // Start dealing continuous damage
                damageCoroutine = StartCoroutine(DealContinuousDamage(player));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Stop dealing damage when the summon leaves the player
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DealContinuousDamage(Player player)
    {
        while (true)
        {
            player.TakeDamage(damage, summonerRef);
            yield return new WaitForSeconds(1f); // Damage every second
        }
    }
}
