using Fusion;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SummonAI : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] private PlayerRef summonerRef { get; set; }
    [Networked] private float speed { get; set; } = 4f;
     [Networked] private float lifetime { get; set; } = 10f; 
     [Networked] private float damage { get; set; } = 10f;

    [Networked, OnChangedRender(nameof(OnDespawnTimerChanged))] 
    private float despawnTimer { get; set; }
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    [SerializeField] private Image healthBar;

    private Coroutine damageCoroutine;

    private Transform target;
    private Player targetPlayer;

    public void OnCreated(int team, PlayerRef summonerRef)
    {
        this.team = team;
        this.summonerRef = summonerRef;
    }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateHealthBar();
        despawnTimer = lifetime;
        FindTarget();
    }

    public override void FixedUpdateNetwork()
    {
        if (targetPlayer != null && !targetPlayer.IsAlive())
        {
            //Debug.Log("Target died, finding new one...");
            FindTarget();
            return;
        }

        // Make sure there's a target
        if (target != null)
        {
            // Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;

            // Rotate sprite to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.position += direction * speed * Runner.DeltaTime;
        }

        despawnTimer -= Runner.DeltaTime;
        if (despawnTimer < 0)
        {
            Runner.Despawn(Object);
            return;
        }
    }

    private void FindTarget()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        float closestDistance = Mathf.Infinity;
        Transform closestPlayerTransform = null;
        Player closestPlayer = null;

        foreach (Player player in players)
        {
            if (player.GetTeam() != team && player.IsAlive())
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayerTransform = player.transform;
                    closestPlayer = player;
                }
            }
        }

        target = closestPlayerTransform;
        targetPlayer = closestPlayer;
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

            // Knockback
            float knockbackForce = 10f;
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }

            yield return new WaitForSeconds(1f); // Damage every second
        }
    }

    // public void TakeDamage(float damage)
    // {
    //     if (HasStateAuthority == false) return;

    //     currentHealth -= damage;

    //     if (currentHealth <= 0)
    //     {
    //         Runner.Despawn(GetComponent<NetworkObject>());
    //     }
    // }

    void OnDespawnTimerChanged()
    {
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = despawnTimer / lifetime;
        }
    }
}
