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
    [Networked] int missingAmmo { get; set; }
    [Networked] float fireRate { get; set; }
    [Networked] float reloadTime { get; set; }
    [Networked] float reloadTimer { get; set; }
    [Networked] float reloadFraction { get; set; }
    [Networked] int points { get; set; }
    [Networked] float timeToWaitForBullet { get; set; }
    [Networked, OnChangedRender(nameof(OnHealthChanged))] float currentHealth { get; set; }
    [Networked] int team { get; set; }
    [Networked] Vector3 respawnPoint { get; set; }
    [Networked] bool isAlive { get; set; }
    [Networked] float respawnTime { get; set; }
    [Networked] float currentRespawn { get; set; }
    [Networked, Capacity(50)] string characterPath { get; set; } = "ScriptableObjects/Characters/Army Vet";
    [Networked] NetworkButtons previousButtons { get; set; }
    [Networked] private NetworkObject carriedObject { get; set; }
    [Networked, OnChangedRender(nameof(OnCarryingChanged)), HideInInspector] public bool isCarrying { get; set; }
    [Networked] bool isMoving { get; set; }
    [Networked] bool isDashing { get; set; }
    [Networked] float dashTimer { get; set; }
    [Networked] float dashCooldownTimer { get; set; }
    [Networked] float dashSpeed { get; set; }
    [Networked] float dashDuration { get; set; }
    [Networked] float dashCooldown { get; set; }
    [Networked] public float aoeDamage { get; set; }
    [Networked] public float aoeCooldown { get; set; }
    [Networked] public float aoeDuration { get; set; }
    [Networked] public float aoeCooldownTimer { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    public Animator animator;
    [SerializeField] Image mainHealthBar;
    [SerializeField] Image teamHealthBar;
    [SerializeField] Image enemyHealthBar;
    [SerializeField] PopUpText popUpText;
    [SerializeField] cooldownHandler dashCDHandler;
    [SerializeField] cooldownHandler reloadHandler;
    [SerializeField] cooldownHandler aoeHandler;
    [SerializeField] Image reloadIcon;
    [SerializeField] Image reloadIconLayer;
    [SerializeField] Image aoeIcon;
    [SerializeField] Image aoeIconLayer;
    [SerializeField] GameObject escapeMenu;
    Image healthBar;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI timeLeftText;
    [HideInInspector] public Transform holdPosition;
    GameController gameController;
    [SerializeField] GameObject deathOverlay;
    [SerializeField] TextMeshProUGUI respawnTimerTxt;
    [SerializeField] FlagIndicator flagIndicator;

    // Player intialisation (called from game controller on server when creating the player)
    public void OnCreated(string characterPath, Vector3 respawnPoint, int team)
    {
        Character character = Resources.Load(characterPath) as Character;
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        fireRate = character.FireRate;
        dashSpeed = character.DashSpeed;
        dashDuration = character.DashDuration;
        dashCooldown = character.DashCooldown;
        aoeDamage = character.AoeDamage;
        aoeCooldown = character.AoeCooldown;
        aoeDuration = character.AoeDuration;

        this.respawnPoint = respawnPoint;
        this.team = team;
        this.characterPath = characterPath;
        points = 100;
        reloadTime = 3.0f;
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

        // Find game controller component (Fusion creates copies of the game controller object so we need to choose the correct one)
        if (GameObject.Find("Host") != null)
            gameController = GameObject.Find("Host").GetComponent<GameController>();
        else
            gameController = GameObject.Find("Client A").GetComponent<GameController>();

        // Add this player to game controller player list
        gameController.RegisterPlayer(this);

        // Get components
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        animator = gameObject.GetComponent<Animator>();

        // Set sprite from resource path
        Character character = Resources.Load(characterPath) as Character;
        spriteRenderer.sprite = character.Sprite;

        Player localPlayer = Runner.GetPlayerObject(Runner.LocalPlayer)?.GetComponent<Player>();
        int localPlayerTeam = localPlayer.GetTeam();

        // If client controls this player then use main health bar
        if (HasInputAuthority)
        {
            healthBar = mainHealthBar;
            teamHealthBar.transform.parent.gameObject.SetActive(false);
            enemyHealthBar.transform.parent.gameObject.SetActive(false);
        }
        // If this player is on the other team to the client's player then use small health bar
        else if (localPlayerTeam != team)
        {
            healthBar = enemyHealthBar;
            mainHealthBar.transform.parent.gameObject.SetActive(false);
            teamHealthBar.transform.parent.gameObject.SetActive(false);
        }
        // If this player is on the same team to the client's player then use no health bar
        else
        {
            healthBar = teamHealthBar;
            mainHealthBar.transform.parent.gameObject.SetActive(false);
            enemyHealthBar.transform.parent.gameObject.SetActive(false);
        }

        // Set the health bar
        UpdateHealthBar(currentHealth);

        if (deathOverlay != null)
        {
            deathOverlay.SetActive(false);
        }

        // Set the ammo counter
        ammoText.text = "Bullets: " + currentAmmo;

        // Pass the local player's team to the flag indicator
        flagIndicator.SetLocalPlayerTeam(localPlayerTeam);

        // Set the initial flag indicator visibility
        OnCarryingChanged();
    }

    // Called on each client and server when player is despawned from network
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Remove this player from game controller player list
        gameController.UnregisterPlayer(this);
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
        gameController.RegisterAlivePlayer(this);

        // Disable the death overlay
        if (HasInputAuthority && deathOverlay != null)
        {
            deathOverlay.SetActive(false);
        }
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

            // Enable the death overlay and update the respawn timer text
            if (HasInputAuthority) // Only show for the local player
            {
                if (deathOverlay != null)
                {
                    deathOverlay.SetActive(true); // Enable the overlay
                }

                if (respawnTimerTxt != null)
                {
                    // Calculate the remaining respawn time
                    float remainingTime = respawnTime - currentRespawn;
                    respawnTimerTxt.text = $"Respawning in {Mathf.CeilToInt(remainingTime)}s";
                }
            }
            return;
        }
        else
        {
            if (HasInputAuthority && deathOverlay != null)
            {
                deathOverlay.SetActive(false);
            }
        }

        // Decrease bullet timer and clamp to 0 if below 0
        timeToWaitForBullet = (timeToWaitForBullet > 0) ? timeToWaitForBullet - Runner.DeltaTime : 0;

        // Handle reloading
        if (reloadTimer > 0)
        {
            reloadTimer -= Runner.DeltaTime;
            if (reloadTimer <= 0)
            {
                // Reloading is complete, update ammo
                currentAmmo = maxAmmo;
                ammoText.text = "Bullets: " + currentAmmo;
                reloadIcon.enabled = false;
                reloadIconLayer.enabled = false;
            }
        }

        // Handle dash cooldown
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Runner.DeltaTime;
        }

        // Handle dash duration
        if (isDashing)
        {
            dashTimer -= Runner.DeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false; // End dash
            }
        }

        // Handle AoE cooldown
        if (aoeCooldownTimer > 0)
        {
            aoeCooldownTimer -= Runner.DeltaTime;
            if (aoeCooldownTimer <= 0)
            {
                aoeIcon.enabled = false;
                aoeIconLayer.enabled = false;
            }
        }

        // auto reload 
        if (currentAmmo == 0 && reloadTimer <= 0) 
        {
            Reload();
        }

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData input))
        {
            //If menu is not active
            if (!escapeMenu.activeSelf){
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

                // Drop object
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

                // Dash with 'space'
                if (input.buttons.WasPressed(previousButtons, InputButtons.Dash))
                {
                    Dash(input.moveDirection);
                }
            }
            

            // Activate Menu
            if (input.buttons.WasPressed(previousButtons, InputButtons.Menu)){
                escapeMenu.SetActive(!escapeMenu.gameObject.activeSelf);
            }

            // Activate AoE skill with 'T'
            if (input.buttons.WasPressed(previousButtons, InputButtons.AoE))
            {
                ActivateAoE(input.cursorWorldPoint);
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

        // Update the time left in the UI if the client controls this player
        if (HasInputAuthority)
        {
            float timeLeft = gameController.maxTime - gameController.currentTime;
            int secondsLeft = (int) Mathf.Ceil(timeLeft);
            int mins = secondsLeft / 60;
            int secs = secondsLeft % 60;
            timeLeftText.text = "Time Left: " + mins + ":" + secs.ToString("00");
        }
    }

    // Player moves according to key presses and player speed
    void PlayerMovement(Vector2 moveDirection)
    {
        // If dashing, use dash speed; otherwise, use normal speed
        float currentSpeed = isDashing ? speed * dashSpeed : speed;
        // Move the player by setting the velocity using the supplied movement direction vector
        Vector2 velocity = moveDirection.normalized * currentSpeed;
        rb.linearVelocity = velocity;

        isMoving = velocity.x != 0 || velocity.y != 0;
    }

    // Dash mechanic
    void Dash(Vector2 moveDirection)
    {
        if (dashCooldownTimer <= 0) // Only allow dash if cooldown is over
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashCDHandler.StartCooldown(dashCooldown);
        }
        else
        {
            ShowMessage("Dash in cooldown", 0.2f, Color.white);
        }
    }

    // Activate AoE skill
    void ActivateAoE(Vector2 cursorWorldPoint)
    {
        if (aoeCooldownTimer <= 0) // Only allow AoE if cooldown is over
        {
            // Spawn AoE effect (only the server can do this)
            if (HasStateAuthority)
            {
                GameObject aoeEffect = Resources.Load("Prefabs/AoE1") as GameObject;
                // Spawn the AoE prefab
                NetworkObject aoeObject = Runner.Spawn(aoeEffect, cursorWorldPoint, Quaternion.identity, null, (runner, networkObject) =>
                {
                    AoESpell aoeSpell = networkObject.GetComponent<AoESpell>();
                    if (aoeSpell != null)
                    {
                        aoeSpell.OnCreated(aoeDamage, team, aoeDuration); 
                    }
                });
            }
            // Start cooldown
            aoeCooldownTimer = aoeCooldown;
            ShowMessage("AoE Skill Used", 0.5f, Color.white);
            aoeIcon.enabled = true;
            aoeIconLayer.enabled = true;
            aoeHandler.StartCooldown(aoeCooldown);
        }
        else
        {
            ShowMessage("AoE Skill in Cooldown", 0.5f, Color.white);
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
                    PrefabFactory.SpawnBullet(Runner, Object.InputAuthority, bulletPrefab, gameObject.transform.position, aimDirection, 40.0f, damage, team);
                }
                currentAmmo--;
                ammoText.text = "Bullets: " + currentAmmo;
            }
        }
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage)
    {
        float newHealth = currentHealth - damage;

        if (HasStateAuthority)
            currentHealth = newHealth;

        UpdateHealthBar(newHealth);

        //Play hurt animation and sounds
        HurtEffects();

        if (newHealth <= 0.0f) {
            Die();
        }
    }

    void HurtEffects(){
        animator.SetTrigger("Damaged");
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount)
    {
        float newHealth = currentHealth + amount;

        if (HasStateAuthority)
            currentHealth = newHealth;

        if (currentHealth >= maxHealth) {
            currentHealth = maxHealth;
        }

        UpdateHealthBar(newHealth);
    }

    public void GainPoints(int amount)
    {
        points += amount;
    }

    public void SpendPoints(int amount)
    {
        if(amount > points)
        {
            Debug.Log("You don't have enough points");
        }
        points -= amount;
    }

    void Die()
    {
        isAlive = false;

        // Ensure health bar is empty
        UpdateHealthBar(0.0f);

        // Disable the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = false;
        gameController.UnregisterAlivePlayer(this);

        if (isCarrying)
        {
            // Player will drop the flag if they died
            DropObject();
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

    void Reload()
    {
        if (currentAmmo >= maxAmmo)
        {
            ShowMessage("Ammo is full!", 0.1f, Color.white);
            return; 
        }
        if (reloadTimer <= 0)
        {
            ShowMessage("Reloading", 0.3f, Color.green);
            missingAmmo = maxAmmo - currentAmmo;
            reloadFraction = (float)missingAmmo / maxAmmo;
            reloadTimer = reloadTime * reloadFraction;
            timeToWaitForBullet = reloadTimer;
            reloadIcon.enabled = true;
            reloadIconLayer.enabled = true;
            reloadHandler.StartCooldown(reloadTimer);
        }
        else
        {
            ShowMessage("still reloading", 0.3f, Color.white);
        }
    }

    public void CarryObject(NetworkObject networkObject)
    {
        if (carriedObject == null)
        {
            carriedObject = networkObject;
            isCarrying = true;
            speed /= 2;
            PickupFlag flag = carriedObject.GetComponent<PickupFlag>();
            gameController.BroadcastCarryFlag(team, flag.team);
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
            gameController.CheckForWinCondition();
            gameController.BroadcastDropFlag(team, flag.team);
        }
    }

    void OnCarryingChanged()
    {
        flagIndicator.GetComponent<Image>().enabled = isCarrying;
        if (carriedObject != null)
        {
            PickupFlag flag = carriedObject.GetComponent<PickupFlag>();
            flagIndicator.SetColour(flag.team);
        }
    }

    public void ShowMessage(string message, float speed, Color color) {
        if (HasInputAuthority) {
            popUpText.MakePopupText(message, speed, color);
        }
    }

    public void Quit()
    {
        Debug.Log("Exiting");
        Application.Quit();
    }
    
    // Only server can call this RPC, and it will run only on the client that controls this player
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.InputAuthority)]
    public void RPC_ShowMessage(string message, float speed, Color color)
    {
        ShowMessage(message, speed, color);
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

    public int GetPoints()
    {
        return points;
    }
}
