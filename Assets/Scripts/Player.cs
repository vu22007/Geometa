using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 

public class Player : NetworkBehaviour
{
    [Networked] float speed { get; set; }
    [Networked] float maxHealth { get; set; }
    [Networked] float damage { get; set; }
    [Networked] int maxAmmo { get; set; }
    [Networked] int currentAmmo { get; set; }
    [Networked] float fireRate { get; set; }
    [Networked] float reloadTime { get; set; }
    [Networked] float timeToWaitForBullet { get; set; }
    [Networked] float currentHealth { get; set; }
    [Networked] int team { get; set; }
    [Networked] Vector3 respawnPoint { get; set; }
    [Networked] bool isAlive { get; set; }
    [Networked] float respawnTime { get; set; }
    [Networked] float currentRespawn { get; set; }
    [Networked] bool spriteIsFlipped { get; set; }
    [Networked, Capacity(50)] string characterPath { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    public GameObject shapeControllerPrefab;
    private GameObject shapeController;
    public Image healthBar;
    public TextMeshProUGUI ammoText;

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
        reloadTime = 1.0f;
        respawnTime = 10.0f;

        this.characterPath = characterPath;
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

        // Set sprite from resource path
        Character character = Resources.Load(characterPath) as Character;
        spriteRenderer.sprite = character.Sprite;

        shapeController = Instantiate(shapeControllerPrefab, cam.transform);
        ammoText.text = "Bullets: " + currentAmmo;

        // Initialise player
        Respawn();
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
    
    // Player initialisation (also used for respawning)
    public void Respawn()
    {
        gameObject.transform.position = respawnPoint;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;
        // Refill the health bar
        healthBar.fillAmount = currentHealth/ 100f; 
    }

    // Update function (called from the game controller on all clients and server)
    public void PlayerUpdate()
    {
        // If player is dead then add to respawn timer and return
        if (!isAlive)
        {
            currentRespawn += Runner.DeltaTime;
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

            // Spacebar to take damage
            if( Input.GetKeyDown( KeyCode.Space ) ){
                Debug.Log("Damage taken");
                TakeDamage(10); 
            }
            // Enter to heal
            if( Input.GetKeyDown( KeyCode.Return ) ){
                Debug.Log("Healed");
                Heal(10); 
            }


            previousButtons = input.buttons;
        }

        // Flip the player sprite if necessary (this is done on all clients and server)
        spriteRenderer.flipX = spriteIsFlipped;
    }

    // Player moves according to key presses and player speed
    void PlayerMovement(Vector2 moveDirection)
    {
        // Move the player by setting the velocity using the supplied movement direction vector
        Vector2 velocity = moveDirection.normalized * speed;
        rb.linearVelocity = velocity;

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
                    PrefabFactory.SpawnBullet(Runner, bulletPrefab, gameObject.transform.position, aimDirection, 40.0f, damage, team);
                    currentAmmo--;
                }
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
        healthBar.fillAmount = currentHealth/ 100f;
        if (currentHealth <= 0.0f) {
            Die();
        }
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount)
    {
        currentHealth += amount;
        healthBar.fillAmount = currentHealth/ 100f;
        if (currentHealth >= maxHealth) {
            currentHealth = maxHealth;
        }
    }

    void Die()
    {
        if (HasInputAuthority) Debug.Log("You died :(("); // only show message for client who controls this player
        isAlive = false;
        rb.linearVelocity = new Vector2(0, 0); // stop player from moving
    }

    void Reload()
    {
        Debug.Log("Reloading");
        timeToWaitForBullet = reloadTime;
        currentAmmo = maxAmmo;
    }

    public bool RespawnTimerDone()
    {
        return currentRespawn >= respawnTime;
    }

    void Reload(){
        currentAmmo = maxAmmo;
        ammoText.text = "Bullets: " + currentAmmo;
    }

    //Shoots a bullet by spawning the prefab
    Bullet ShootBullet()
    public bool IsAlive()
    {
        GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
        Vector3 direction = CalculateDirectionFromMousePos();
        Bullet bullet = PrefabFactory.SpawnBullet(bulletPrefab, gameObject.transform.position, direction, 40.0f, damage, team);
        currentAmmo--;
        ammoText.text = "Bullets: " + currentAmmo;
        return bullet;
    }

    Vector3 CalculateDirectionFromMousePos()
        return isAlive;
    }

    public int GetTeam()
    {
        return team;
    }
}
