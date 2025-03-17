using Fusion;
using UnityEngine;

public class SummonAI : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] private PlayerRef summonerRef { get; set; }
    [Networked] private float speed { get; set; } = 5f;
    [Networked] private float lifetime { get; set; } = 10f; 
    [Networked] private float damage { get; set; } = 10f; 
    [Networked] private TickTimer despawnTimer { get; set; }
    
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
                player.TakeDamage(damage, summonerRef);
                Runner.Despawn(Object);
            }
        }
    }
}
