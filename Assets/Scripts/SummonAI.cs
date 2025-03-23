using Fusion;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SummonAI : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] private PlayerRef summonerRef { get; set; }
    [Networked] private float speed { get; set; } = 3f;
    [Networked] private float lifetime { get; set; } = 10f; 
    [Networked] private float damage { get; set; } = 10f; 
    [Networked] private TickTimer despawnTimer { get; set; }

    
    [Networked, OnChangedRender(nameof(OnHealthChanged))] 
    private float currentHealth { get; set; }

    [Networked] private float maxHealth { get; set; }

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    [SerializeField] private Image healthBar;

    private Coroutine damageCoroutine;

    private Transform target;

    public void OnCreated(int team, PlayerRef summonerRef)
    {
        this.team = team;
        this.summonerRef = summonerRef;
        maxHealth = 30f;
        currentHealth = maxHealth;
    }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateHealthBar(currentHealth);
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
            FindTarget();
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Runner.DeltaTime;
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
        if (!HasStateAuthority) return;

        // Check if it's a bullet
        if (other.TryGetComponent<Bullet>(out var bullet))
        {
            // Make sure it's an enemy bullet
            if (bullet.Team != team)
            {
                TakeDamage(bullet.Damage);
                Runner.Despawn(other.GetComponent<NetworkObject>());
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

    public void TakeDamage(float damage)
    {
        if (HasStateAuthority == false) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Runner.Despawn(GetComponent<NetworkObject>());
        }
    }

    void OnHealthChanged()
    {
        UpdateHealthBar(currentHealth);
    }

    void UpdateHealthBar(float health)
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = health / maxHealth;
        }
    }
}
