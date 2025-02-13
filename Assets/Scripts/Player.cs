using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Physics;

public class Player : NetworkBehaviour
{
    [Networked] float speed { get; set; }
    [Networked] float maxHealth { get; set; }
    [Networked] float damage { get; set; }
    [Networked] int maxAmmo { get; set; }
    [Networked] int currentAmmo { get; set; }
    [Networked] float fireRate { get; set; }
    [Networked] float reloadTime { get; set; }
    [Networked] int points { get; set; }
    [Networked] float timeToWaitForBullet { get; set; }
    [Networked] float currentHealth { get; set; }
    [Networked] int team { get; set; }
    [Networked] Vector3 respawnPoint { get; set; }
    [Networked] bool isAlive { get; set; }
    [Networked] float respawnTime { get; set; }
    [Networked] float currentRespawn { get; set; }
    [Networked, Capacity(50)] string characterPath { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }
    [Networked] private NetworkObject carriedObject { get; set; }
    [Networked, HideInInspector] public bool isCarrying { get; set; }
    [Networked] bool isMoving { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    public Animator animator;
    [SerializeField] Image mainHealthBar;
    [SerializeField] Image smallHealthBar;
    Image healthBar;
    public TextMeshProUGUI ammoText;
    [HideInInspector] public Transform holdPosition;

    // Player intialisation (called from game controller on server when creating the player)
    public void OnCreated(string characterPath, Vector3 respawnPoint, int team)
    {
        Character character = Resources.Load(characterPath) as Character;
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        fireRate = character.FireRate;

        this.respawnPoint = respawnPoint;
        this.team = team;
        this.characterPath = characterPath;

        points = 0;
        reloadTime = 1.0f;
        respawnTime = 10.0f;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;
        isCarrying = false;
    }

    // Player initialisation (called on each client and server when player is spawned on network)
    public override void Spawned()
    {
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

        // Get components
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        animator = gameObject.GetComponent<Animator>();

        // Set sprite from resource path
        Character character = Resources.Load(characterPath) as Character;
        spriteRenderer.sprite = character.Sprite;

        // If client controls this player then use main health bar
        if (HasInputAuthority)
        {
            healthBar = mainHealthBar;
            smallHealthBar.GetComponentInParent<Canvas>().enabled = false;
        }
        // If this player is on the other team to the client's player then use small health bar
        else if (Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<Player>().GetTeam() != team)
        {
            healthBar = smallHealthBar;
            mainHealthBar.GetComponentInParent<Canvas>().enabled = false;
        }
        // If this player is on the same team to the client's player then use no health bar
        else
        {
            healthBar = null;
            mainHealthBar.GetComponentInParent<Canvas>().enabled = false;
            smallHealthBar.GetComponentInParent<Canvas>().enabled = false;
        }

        // Set the health bar
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        // Set the ammo counter
        ammoText.text = "Bullets: " + currentAmmo;
    }

    // Called on each client and server when player is despawned from network
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Remove this player from game controller player list (do this for all found game controllers to ensure that it is removed from the correct one)
        foreach (GameObject gameControllerObject in GameObject.FindGameObjectsWithTag("GameController"))
        {
            GameController gameController = gameControllerObject.GetComponent<GameController>();
            gameController.UnregisterPlayer(this);
        }
    }
    
    // Player initialisation when respawning
    public void Respawn()
    {
        //gameObject.transform.position = respawnPoint;
        gameObject.GetComponent<NetworkRigidbody2D>().Teleport(respawnPoint);
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;

        // Refill the health bar
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        // Set the ammo counter
        ammoText.text = "Bullets: " + currentAmmo;

        // Activate the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = true;
    }

    // Update function (called from the game controller on all clients and server)
    public void PlayerUpdate()
    {
        // Check if player is dead
        if (!isAlive)
        {
            // Update respawn timer
            currentRespawn += Runner.DeltaTime;

            // Stop player movement and prevent player from infinitely sliding when pushed by another player
            rb.linearVelocity = new Vector2(0, 0);

            return;
        }

        // Decrease bullet timer and clamp to 0 if below 0
        timeToWaitForBullet = (timeToWaitForBullet > 0) ? timeToWaitForBullet - Runner.DeltaTime : 0;

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData input))
        {
            // WASD movement
            PlayerMovement(input.moveDirection);

            // Firing the weapon
            if (input.buttons.IsSet(InputButtons.Shoot))
            {
                Shoot(input.aimDirection);
            }

            // Reloading
            if (input.buttons.WasPressed(previousButtons, InputButtons.Reload))
            {
                Reload();
            }

            // Drop object with 'C'
            if (input.buttons.WasPressed(previousButtons, InputButtons.Pickup))
            {
                if (isCarrying)
                {
                    DropObject();
                }
            }

            // Testing damage
            if (input.buttons.WasPressed(previousButtons, InputButtons.TakeDamage))
            {
                TakeDamage(10);
            }

            //Character rotates to mouse position
            Vector2 lookDirection = input.aimDirection.normalized;
            Quaternion wantedRotation = Quaternion.LookRotation(transform.forward, lookDirection);
            gameObject.transform.rotation = wantedRotation;

            cam.gameObject.transform.rotation = Quaternion.identity;

            previousButtons = input.buttons;
        }

        // Play idle or walking animation
        if (isMoving)
            animator.SetFloat("Speed", 0.02f);
        else
            animator.SetFloat("Speed", 0f);

        // If carrying an object, move it to player's position
        if (isCarrying && carriedObject != null)
        {
            carriedObject.transform.position = transform.position + new Vector3(2.0f, 0, 0);
        }
    }

    // Player moves according to key presses and player speed
    void PlayerMovement(Vector2 moveDirection)
    {
        // Move the player by setting the velocity using the supplied movement direction vector
        Vector2 velocity = moveDirection.normalized * speed;
        rb.linearVelocity = velocity;

        isMoving = velocity.x != 0 || velocity.y != 0;
    }

    // Shoots a bullet by spawning the prefab on the network
    void Shoot(Vector2 aimDirection)
    {
        if (timeToWaitForBullet <= 0)
        {
            timeToWaitForBullet = 1 / fireRate;
            if (currentAmmo != 0)
            {
                // Spawn bullet (only the server can do this)
                if (HasStateAuthority)
                {
                    GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
                    PrefabFactory.SpawnBullet(Runner, Object.InputAuthority, bulletPrefab, gameObject.transform.position, aimDirection, 40.0f, damage, team);
                }
                currentAmmo--;
                ammoText.text = "Bullets: " + currentAmmo;
            }
            else
            {
                Debug.Log("Press R to reload!!");
            }
        }
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        //Play hurt animation and sounds
        HurtEffects();

        if (currentHealth <= 0.0f) {
            Die();
        }
    }

    void HurtEffects(){
        animator.SetTrigger("Damaged");
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
        if (currentHealth >= maxHealth) {
            currentHealth = maxHealth;
        }
    }

    public void GainPoints(int amount)
    {
        points += amount;
    }

    void Die()
    {
        isAlive = false;

        // Only show message for client who controls this player
        if (HasInputAuthority)
            Debug.Log("You died :((");
        
        // Ensure health bar is empty
        if (healthBar != null)
            healthBar.fillAmount = 0.0f;

        // Disable the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = false;

        if (isCarrying)
        {
            // Player will drop the flag if they died
            DropObject();
        }
    }

    void Reload()
    {
        Debug.Log("Reloading");
        timeToWaitForBullet = reloadTime;
        currentAmmo = maxAmmo;
        ammoText.text = "Bullets: " + currentAmmo;
    }

    public void CarryObject(NetworkObject networkObject)
    {
        if (carriedObject == null)
        {
            carriedObject = networkObject;
            isCarrying = true;
            speed /= 2;
            Debug.Log("Player is carrying the flag");
        }
    }

    void DropObject()
    {
        if (carriedObject != null)
        {
            PickupFlag flag = carriedObject.GetComponent<PickupFlag>();
            if (flag != null)
            {
                flag.Drop(); // Call the Drop method on the pickupable object
            }
            carriedObject = null;
            isCarrying = false;
            speed *= 2;
            FindFirstObjectByType<GameController>()?.CheckForWinCondition();
            Debug.Log("Dropped the flag!");
        }
    }

    public bool RespawnTimerDone()
    {
        return currentRespawn >= respawnTime;
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public int GetTeam()
    {
        return team;
    }
}
