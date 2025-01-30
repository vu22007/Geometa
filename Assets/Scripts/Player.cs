using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    float speed;
    float maxHealth;
    float damage;
    int maxAmmo;
    int currentAmmo;
    float fireRate;
    float reloadTime;
    float timeToWaitForBullet;
    [SerializeField] Character character;
    [SerializeField] Camera cam;
    float currentHealth;
    public bool isAlive;
    float respawnTime = 10.0f;
    float currentRespawn;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Vector3 respawnPoint;
    public int team;
    [Networked] bool isFlipped { get; set; }

    // Called on each client and server when player is spawned in network
    public override void Spawned()
    {
        Character character = Resources.Load("ScriptableObjects/Characters/Army Vet") as Character;
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        fireRate = character.FireRate;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer.sprite = character.Sprite;
        this.character = character;
        team = 1;
        reloadTime = 1.0f;

        // Camera object is child of player object
        GameObject cameraObject = gameObject.transform.GetChild(0).gameObject;

        // Disable the camera object if client does not control this player
        if (!HasInputAuthority)
        {
            cameraObject.SetActive(false);
        }

        Respawn();
    }

    //For the prefab factory (For when we have multiple players), to be called on instantiation of the prefab
    public void OnCreated(Character character, Vector3 respawnPoint, int team){
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        fireRate = character.FireRate;
        this.respawnPoint = respawnPoint;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer.sprite = character.Sprite;
        this.character = character;
        this.team = team;
        reloadTime = 1.0f;

        Respawn();
    }
    
    //Player initialisation (Also used for respawning)
    public void Respawn()
    {
        gameObject.transform.position = respawnPoint;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;
    }

    //Update function, called from the game controller, returns a bullet if one is fired
    public Bullet PlayerUpdate()
    {
        Bullet bullet = null;
        if (isAlive) {
            // WASD movement
            PlayerMovement();

            // Decrease bullet timer and clamp to 0 if below 0
            timeToWaitForBullet = (timeToWaitForBullet > 0) ? timeToWaitForBullet - Runner.DeltaTime : 0;

            // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
            // So the following is ran for just the server and the client who controls this player
            if (GetInput(out NetworkInputData data))
            {
                // Firing the weapon
                if (data.shoot)
                {
                    if (timeToWaitForBullet <= 0)
                    {
                        timeToWaitForBullet = 1 / fireRate;
                        Debug.Log("In here 1");
                        if (currentAmmo != 0)
                        {
                            bullet = ShootBullet();
                        }
                        else
                        {
                            Debug.Log("Press R to reload!!");
                        }
                    }
                }

                // Reloading
                if (data.reload)
                {
                    Debug.Log("Reloading");
                    timeToWaitForBullet = reloadTime;
                    Reload();
                }
            }
        }
        else {
            currentRespawn += Runner.DeltaTime;
        }
        return bullet;
    }

    //player moves according to key presses and player speed
    void PlayerMovement()
    {
        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData data))
        {
            // Move the player by setting the velocity using the supplied movement direction vector
            Vector2 velocity = data.direction.normalized * speed;
            rb.linearVelocity = velocity;

            // Flip sprite to face direction the player is moving in
            // Note: This sets a networked property so all clients can set the sprite correctly for this player
            if (velocity.x < 0)
            {
                isFlipped = true;
            }
            else if (velocity.x > 0)
            {
                isFlipped = false;
            }
        }

        // The following is ran for all clients and server
        spriteRenderer.flipX = isFlipped;
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0.0f) {
            Die();
        }
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth >= maxHealth) {
            currentHealth = maxHealth;
        }
    }

    void Die()
    {
        Debug.Log("You died :((");
        isAlive = false;
        rb.linearVelocity = new Vector2(0, 0); // stop player from moving
    }

    public bool RespawnTimerDone()
    {
        return currentRespawn >= respawnTime;
    }

    void Reload(){
        currentAmmo = maxAmmo;
    }

    //Shoots a bullet by spawning the prefab
    Bullet ShootBullet()
    {
        GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
        Vector3 direction = CalculateDirectionFromMousePos();
        Bullet bullet = PrefabFactory.SpawnBullet(bulletPrefab, gameObject.transform.position, direction, 40.0f, damage, team);
        currentAmmo--;
        return bullet;
    }

    Vector3 CalculateDirectionFromMousePos()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        Vector3 direction = worldPoint - gameObject.transform.position;
        return direction;
    }
}
