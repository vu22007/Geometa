using System.Collections.Generic;
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
    [Networked] private float damageCooldown { get; set; }
    [Networked] private float maxDamageCooldown { get; set; }
    [Networked, OnChangedRender(nameof(OnActivatedChanged))] private bool isActivated { get; set; }
    [SerializeField] private Sprite aoeSmall;
    [SerializeField] private Sprite aoeNormal;
    private SpriteRenderer spriteRenderer;
    private List<Player> players;

    public void OnCreated(Vector2 direction, float speed, float maxDistance, float damage, int team, float duration, PlayerRef playerCasting)
    {
        this.damage = damage;
        this.team = team;
        this.duration = duration;
        this.playerCasting = playerCasting;
        this.direction = direction.normalized;
        this.speed = speed;
        this.maxDistance = maxDistance;
        distanceTraveled = 0f;
        isActivated = false;
        maxDamageCooldown = 1f;
        damageCooldown = 0.2f;
    }

    public override void Spawned()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && aoeSmall != null)
        {
            spriteRenderer.sprite = aoeSmall;
        }
        players = new List<Player>();
    }

    public override void FixedUpdateNetwork()
    {
        if (isActivated)
        {
            damageCooldown -= Runner.DeltaTime;
            if(damageCooldown < -1f){
                DamagePlayers();
                damageCooldown = maxDamageCooldown;
            }
            
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
                if (spriteRenderer != null && aoeNormal != null)
                {
                    spriteRenderer.sprite = aoeNormal;
                }
                despawnTimer = TickTimer.CreateFromSeconds(Runner, duration);
            }
        }
        
    }

    private void DamagePlayers()
    {
        Debug.Log(players);
        foreach (Player player in players)
        {
            player.TakeDamage(damage, playerCasting);
        }
    }

    private void OnTriggerEnter2D(Collider2D other){
        if(isActivated && other.CompareTag("Player")){
            Player player = other.GetComponentInParent<Player>();
            if(player != null){
                Debug.Log("Found player");
                if(player.GetTeam() != team){
                    Debug.Log("Added a player");
                    players.Add(player);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other){
        if(isActivated && other.CompareTag("Player")){
            Player player = other.GetComponentInParent<Player>();
            if(player != null){
                if(players.Contains(player)){
                    Debug.Log("Removed a player");
                    players.Remove(player);
                }
            }
        }
    }



    // Called on all clients when AoE spell is activated, so that all clients see the sprite change
    void OnActivatedChanged()
    {
        if (isActivated && spriteRenderer != null && aoeNormal != null)
        {
            spriteRenderer.sprite = aoeNormal;
        }
    }
}