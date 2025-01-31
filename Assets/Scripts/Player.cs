using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public Camera cam;
    float currentHealth;
    public bool isAlive;
    float respawnTime = 10.0f;
    float currentRespawn;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Vector3 respawnPoint;
    public int team;
    [Networked] bool spriteIsFlipped { get; set; }

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

        // Disable the camera if client does not control this player
        if (!HasInputAuthority)
        {
            cam.gameObject.SetActive(false);
        }

        // Add this player to game controller player list (do this for all found game controllers to ensure that it is added to the correct one)
        foreach (GameObject gameControllerObject in GameObject.FindGameObjectsWithTag("GameController"))
        {
            GameController gameController = gameControllerObject.GetComponent<GameController>();
            gameController.RegisterPlayer(this);
        }

        Respawn();
    }

    // Called on each client and server when player is despawned in network
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Remove this player from game controller player list (do this for all found game controllers to ensure that it is removed from the correct one)
        foreach (GameObject gameControllerObject in GameObject.FindGameObjectsWithTag("GameController"))
        {
            GameController gameController = gameControllerObject.GetComponent<GameController>();
            gameController.UnregisterPlayer(this);
        }
    }

    //For the prefab factory (For when we have multiple players), to be called on instantiation of the prefab
    public void OnCreated(Character character, Vector3 respawnPoint, int team)
    {
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

        // If player is dead then add to respawn timer and return
        if (!isAlive)
        {
            currentRespawn += Runner.DeltaTime;
            return null;
        }

        // Decrease bullet timer and clamp to 0 if below 0
        timeToWaitForBullet = (timeToWaitForBullet > 0) ? timeToWaitForBullet - Runner.DeltaTime : 0;

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData data))
        {
            // WASD movement
            PlayerMovement(data.moveDirection);

            // Firing the weapon
            if (data.shoot)
            {
                Shoot(data.aimDirection);
            }

            // Reloading
            if (data.reload)
            {
                Reload();
            }
        }

        // Flip the player sprite if necessary (this is done on all clients and server)
        spriteRenderer.flipX = spriteIsFlipped;

        return bullet;
    }

    // Player moves according to key presses and player speed
    void PlayerMovement(Vector2 moveDirection)
    {
        // Move the player by setting the velocity using the supplied movement direction vector
        Vector2 velocity = moveDirection.normalized * speed;
        //rb.linearVelocity = velocity;
        transform.Translate(new Vector3(velocity.x, velocity.y, 0) * Runner.DeltaTime);

        // Flip sprite to face direction the player is moving in
        // Note: This sets a networked property so all clients can set the sprite correctly for this player
        if (velocity.x < 0)
        {
            spriteIsFlipped = true;
        }
        else if (velocity.x > 0)
        {
            spriteIsFlipped = false;
        }
    }

    Bullet Shoot(Vector2 aimDirection)
    {
        Bullet bullet = null;

        if (timeToWaitForBullet <= 0)
        {
            timeToWaitForBullet = 1 / fireRate;
            if (currentAmmo != 0)
            {
                Vector3 direction = new Vector3(aimDirection.x, aimDirection.y);
                bullet = ShootBullet(direction);
            }
            else
            {
                Debug.Log("Press R to reload!!");
            }
        }

        return bullet;
    }

    // Shoots a bullet by spawning the prefab
    Bullet ShootBullet(Vector3 direction)
    {
        GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
        //Bullet bullet = PrefabFactory.SpawnBullet(bulletPrefab, gameObject.transform.position, direction, 40.0f, damage, team);

        Quaternion wantedRotation = Quaternion.LookRotation(direction, new Vector3(0.0f, 0.0f, -1.0f));
        NetworkObject networkPlayerObject = Runner.Spawn(bulletPrefab, gameObject.transform.position, wantedRotation);
        Bullet bullet = networkPlayerObject.GetComponent<Bullet>();
        direction.z = 0;
        bullet.OnCreated(direction.normalized, speed, damage, team);

        currentAmmo--;
        return bullet;
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

    void Reload()
    {
        Debug.Log("Reloading");
        timeToWaitForBullet = reloadTime;
        currentAmmo = maxAmmo;
    }
}
