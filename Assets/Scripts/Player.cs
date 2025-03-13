using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Player : NetworkBehaviour
{
    [Networked] float speed { get; set; }
    [Networked] float maxHealth { get; set; }
    [Networked] float maxPoints { get; set; }
    [Networked] float damage { get; set; }
    [Networked] int maxAmmo { get; set; }
    [Networked] int currentAmmo { get; set; }
    [Networked] int missingAmmo { get; set; }
    [Networked] float fireRate { get; set; }
    [Networked] float reloadTime { get; set; }
    [Networked] float reloadTimer { get; set; }
    [Networked] float reloadFraction { get; set; }
    [Networked, OnChangedRender(nameof(OnPointsChanged))] float points { get; set; }
    [Networked] float timeToWaitForBullet { get; set; }
    [Networked, OnChangedRender(nameof(OnHealthChanged))] float currentHealth { get; set; }
    [Networked] int team { get; set; }
    [Networked] Vector3 respawnPoint { get; set; }
    [Networked] bool isAlive { get; set; }
    [Networked] float respawnTime { get; set; }
    [Networked] float currentRespawn { get; set; }
    [Networked, Capacity(30)] string characterName { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }
    [Networked] private NetworkObject carriedObject { get; set; }
    [Networked, OnChangedRender(nameof(OnCarryingChanged)), HideInInspector] public bool isCarrying { get; set; }
    [Networked] bool isMoving { get; set; }
    [Networked] bool isAttacking { get; set; }
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
    [Networked] public bool isAoEEnabled { get; set; }
    [Networked] private bool isAoEUsed { get; set; }
    [Networked] public float aoeMaxRad { get; set; }
    [Networked] private bool normalShoot { get; set; }
    [Networked] private bool gamePaused { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Animator animator;
    [SerializeField] Image mainHealthBar;
    [SerializeField] Image teamHealthBar;
    [SerializeField] Image mainPointsBar;
    [SerializeField] Image minimapIndicator;
    [SerializeField] Image enemyHealthBar;
    [SerializeField] UIController uIController;
    [SerializeField] cooldownHandler dashCDHandler;
    [SerializeField] cooldownHandler reloadHandler;
    [SerializeField] cooldownHandler aoeHandler;
    [SerializeField] Image reloadIcon;
    [SerializeField] Image reloadIconLayer;
    [SerializeField] Image aoeIcon;
    [SerializeField] Image aoeIconLayer;
    [SerializeField] GameObject escapeMenu;
    [SerializeField] Minimap minimap;
    Image healthBar;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI timeLeftText;
    [HideInInspector] public Transform holdPosition;
    GameController gameController;
    [SerializeField] GameObject deathOverlay;
    [SerializeField] TextMeshProUGUI respawnTimerTxt;
    [SerializeField] FlagIndicator flagIndicator;
    private AudioClip shootSound;
    private AudioClip dyingSound;
    private AudioClip dashSound;
    private AudioSource audioSource;
    [SerializeField] Image bulletIcon;
    [SerializeField] GameObject mainbulletIcon;
    [SerializeField] Transform meleePoint;
    [SerializeField] GameObject meleeHitbox;
    
    // Player intialisation (called from game controller on server when creating the player)
    public void OnCreated(string characterName, Vector3 respawnPoint, int team)
    {
        Character character = Resources.Load($"ScriptableObjects/Characters/{characterName}") as Character;
        maxHealth = character.MaxHealth;
        maxPoints = 30f;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        fireRate = character.FireRate;
        dashSpeed = character.DashSpeed;
        dashDuration = character.DashDuration;
        dashCooldown = character.DashCooldown;
        characterName = character.name;
        
        this.respawnPoint = respawnPoint;
        this.team = team;
        this.characterName = characterName;
        points = 30f;
        reloadTime = 3.0f;
        respawnTime = 10.0f;
        aoeDamage = 5;
        aoeCooldown = 10;
        aoeDuration = 5;
        aoeMaxRad = 10;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;
        isCarrying = false;
        isAoEEnabled = false;
        isAoEUsed = false;
        normalShoot = true;
        gamePaused = false;
    }

    // Player initialisation (called on each client and server when player is spawned on network)
    public override void Spawned()
    {
        uIController = GetComponentInChildren<UIController>();
        uIController.SetPlayer(this);
        uIController.transform.SetParent(null);
        // Disable the camera if client does not control this player
        if (!HasInputAuthority)
        {
            cam.gameObject.SetActive(false);
        }

        // Get game controller component
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();

        // Add this player to game controller player list
        gameController.RegisterPlayer(this);

        // Get components
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        animator = gameObject.GetComponent<Animator>();

        // Set sprite from resource path
        Character character = Resources.Load($"ScriptableObjects/Characters/{characterName}") as Character;
        spriteRenderer.sprite = character.Sprite;

        //Set animator controller
        animator.runtimeAnimatorController = Resources.Load("Animations/"+character.name) as RuntimeAnimatorController;

        Player localPlayer = Runner.GetPlayerObject(Runner.LocalPlayer)?.GetComponent<Player>();
        int localPlayerTeam = localPlayer.GetTeam();

        //Setup minimap
        minimap.Setup();
        minimapIndicator.gameObject.SetActive(true);

        // If client controls this player then use main health bar
        if (HasInputAuthority)
        {
            healthBar = mainHealthBar;
            teamHealthBar.transform.parent.gameObject.SetActive(false);
            enemyHealthBar.transform.parent.gameObject.SetActive(false);
            minimapIndicator.color = Color.blue;
        }
        // If this player is on the other team to the client's player then use small health bar
        else if (localPlayerTeam != team)
        {
            healthBar = enemyHealthBar;
            enemyHealthBar.transform.parent.gameObject.SetActive(true);
            mainHealthBar.transform.parent.gameObject.SetActive(false);
            teamHealthBar.transform.parent.gameObject.SetActive(false);
            minimapIndicator.color = Color.red;
        }
        // If this player is on the same team to the client's player then use no health bar
        else
        {
            healthBar = teamHealthBar;
            mainHealthBar.transform.parent.gameObject.SetActive(false);
            enemyHealthBar.transform.parent.gameObject.SetActive(false);
            minimapIndicator.color = Color.green;
        }

        // Set the health bar
        UpdateHealthBar(currentHealth);

        if (deathOverlay != null)
        {
            deathOverlay.SetActive(false);
        }

        // Set the ammo counter
        ammoText.text = currentAmmo.ToString();
        bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;

        // Pass the local player's team to the flag indicator
        flagIndicator.SetLocalPlayerTeam(localPlayerTeam);

        audioSource = GetComponent<AudioSource>();
        shootSound = Resources.Load<AudioClip>("Sounds/Shoot");
        dyingSound = Resources.Load<AudioClip>("Sounds/Dying");
        dashSound = Resources.Load<AudioClip>("Sounds/Dash");

        // Set the initial flag indicator visibility
        OnCarryingChanged();

        //Set points bar
        UpdatePointsBar();
        DisableMeleeHitbox();
        if (characterName == "Knight")
        {
            Debug.Log("disabled mana");
            mainbulletIcon.SetActive(false);
        }
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
        gameObject.SetActive(true);
        gameObject.GetComponent<NetworkRigidbody2D>().Teleport(respawnPoint);
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        currentRespawn = 0.0f;
        timeToWaitForBullet = 0.0f;

        // Refill the health bar
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
        
        if (characterName != "Knight")
        {
            // Set the ammo counter
            ammoText.text = currentAmmo.ToString();
            bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
        }
        // // Set the ammo counter
        // ammoText.text = currentAmmo.ToString();
        // bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;

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

            if (!Runner.IsResimulation)
            {
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

                // Hide the player
                gameObject.SetActive(false);
            }

            return;
        }
        else
        {
            if (!Runner.IsResimulation)
            {
                if (HasInputAuthority && deathOverlay != null)
                {
                    deathOverlay.SetActive(false);
                }

                // Show the player
                gameObject.SetActive(true);
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

                if (!Runner.IsResimulation)
                {
                    ammoText.text = currentAmmo.ToString();
                    bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
                    reloadIcon.enabled = false;
                    reloadIconLayer.enabled = false;
                }
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

        // auto reload 
        if (currentAmmo == 0 && reloadTimer <= 0) 
        {
            Reload();
        }

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData input))
        {
            // If game is not paused
            if (!gamePaused){
                // WASD movement
                PlayerMovement(input.moveDirection);

                // Firing the weapon
                if (characterName == "Knight")
                {
                    if (input.buttons.IsSet(InputButtons.Shoot))
                    {
                        isAttacking = true;
                        EnableMeleeHitbox();
                        Debug.Log("enabled");
                    }
                    else
                    {
                        isAttacking = false;
                        DisableMeleeHitbox();
                        Debug.Log("disabled");

                    }
                }

                else
                {
                    if (input.buttons.IsSet(InputButtons.Shoot))
                    {
                        if (isAoEEnabled && !normalShoot)
                        {
                            ShootAoE(input.aimDirection, input.cursorWorldPoint);
                        }
                        else if (normalShoot && !isAoEEnabled)
                        {
                            Shoot(input.aimDirection);
                        }
                        isAttacking = true;
                    }
                    else
                    {
                        isAttacking = false;
                    }
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
                    TakeDamage(10, PlayerRef.None);
                }

                // Dash with 'space'
                if (input.buttons.WasPressed(previousButtons, InputButtons.Dash))
                {
                    Dash(input.moveDirection);
                }

            }

            // Activate Menu
            if (input.buttons.WasPressed(previousButtons, InputButtons.Menu))
            {
                gamePaused = !gamePaused;

                if (!Runner.IsResimulation)
                    escapeMenu.SetActive(!escapeMenu.gameObject.activeSelf);
            }

            if (gamePaused)
            {
                // Stop player movement (decelerate to stationary)
                float acceleration = 5f;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, acceleration * Runner.DeltaTime);
            }
            
            //Character rotates to mouse position
            Vector2 lookDirection = input.aimDirection.normalized;
            Quaternion wantedRotation = Quaternion.LookRotation(transform.forward, lookDirection);
            gameObject.transform.rotation = wantedRotation;

            cam.gameObject.transform.rotation = Quaternion.identity;
            
            previousButtons = input.buttons;
        }

        if (!Runner.IsResimulation)
        {
            // Play idle or walking animation
            if (isMoving)
                animator.SetFloat("Speed", 0.02f);
            else
                animator.SetFloat("Speed", 0f);

            if (isAttacking)
            {
                animator.SetTrigger("Attack");
            }
        }

        // If carrying an object, move it to player's position
        if (isCarrying && carriedObject != null)
        {
            carriedObject.transform.position = transform.position + new Vector3(2.0f, 0, 0);
        }
    }

    // Player moves according to key presses and player speed
    void PlayerMovement(Vector2 moveDirection)
    {
        // Calculate target velocity
        float targetSpeed = isDashing ? speed * dashSpeed : speed;
        Vector2 targetVelocity = moveDirection.normalized * targetSpeed;

        // Current velocity
        Vector2 currentVelocity = rb.linearVelocity;

        // Apply acceleration to gradually reach target velocity
        float acceleration = 5f;
        Vector2 newVelocity = Vector2.Lerp(currentVelocity, targetVelocity, acceleration * Runner.DeltaTime);

        // Apply the new velocity
        rb.linearVelocity = newVelocity;

        // Check if player is moving
        float threshold = 0.1f;
        isMoving = Mathf.Abs(newVelocity.x) > threshold || Mathf.Abs(newVelocity.y) > threshold;
    }

    // Dash mechanic
    void Dash(Vector2 moveDirection)
    {
        if (dashCooldownTimer <= 0) // Only allow dash if cooldown is over
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            if (!Runner.IsResimulation)
            {
                dashCDHandler.StartCooldown(dashCooldown);
                audioSource.PlayOneShot(dashSound);
            }
        }
        else
        {
            if (!Runner.IsResimulation)
                ShowMessage("Dash in cooldown", 0.2f, Color.white);
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
                // Spawn dummy bullet
                if (HasInputAuthority && !Runner.IsResimulation)
                {
                    // Get rotation
                    Vector3 direction = new Vector3(aimDirection.x, aimDirection.y);
                    Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);

                    GameObject dummyBulletPrefab = Resources.Load("Prefabs/DummyBullet") as GameObject;
                    GameObject dummyBulletObject = Instantiate(dummyBulletPrefab);
                    DummyBullet dummyBullet = dummyBulletObject.GetComponent<DummyBullet>();
                    dummyBullet.OnCreated(gameObject.transform.position, aimDirection, rotation, 40.0f, team, Runner.Tick);
                }

                // Spawn bullet (only the server can do this)
                if (HasStateAuthority)
                {
                    GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
                    PrefabFactory.SpawnBullet(Runner, Object.InputAuthority, bulletPrefab, gameObject.transform.position, aimDirection, 40.0f, damage, team, Object.InputAuthority);
                }

                // Just the player that shoot listens to the sound
                if (HasInputAuthority && !Runner.IsResimulation)
                {
                    PlayShootSound();
                }

                currentAmmo--;

                if (!Runner.IsResimulation)
                {
                    if (characterName != "Knight")
                    {
                        ammoText.text = currentAmmo.ToString();
                        bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
                    }
                }
            }
        }
    }

    // Shoots a bullet by spawning the prefab on the network
    void ShootAoE(Vector2 aimDirection, Vector2 cursorWorldPoint)
    {
        float distance = Vector2.Distance(transform.position, cursorWorldPoint);
        if (HasStateAuthority)
        {
            GameObject aoeSpellPrefab = Resources.Load("Prefabs/AoE1") as GameObject;
            NetworkObject aoeSpellObject = Runner.Spawn(aoeSpellPrefab, transform.position, Quaternion.identity, Object.InputAuthority, (runner, networkObject) =>
            {
                AoESpell aoeSpell = networkObject.GetComponent<AoESpell>();
                if (aoeSpell != null)
                {
                    aoeSpell.OnCreated(aimDirection, 10f, distance, aoeDamage, team, aoeDuration, Object.InputAuthority);
                }
            });
        }

        // Just the player that shoot listens to the sound
        if (HasInputAuthority && !Runner.IsResimulation)
        {
            PlayShootSound();
        }

        isAoEEnabled = false;
        normalShoot = true;
        
        if (!Runner.IsResimulation)
            aoeIcon.enabled = false;
    }
    

    public void PlayShootSound()
    {
        audioSource.PlayOneShot(shootSound);
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage, PlayerRef damageDealer)
    {
        if (!isAlive) return;

        float newHealth = currentHealth - damage;

        if (HasStateAuthority)
            currentHealth = newHealth;

        if (!Runner.IsResimulation)
            UpdateHealthBar(newHealth);

        // Play hurt animation and sounds for all clients
        if (HasStateAuthority)
            RPC_HurtEffects(damage);

        if (newHealth <= 0.0f) {
            Die(damageDealer);
        }
    }
    
    void HurtEffects(float damage){
        animator.SetTrigger("Damaged");
        ShowDamagePopup(damage);
    }

    void ShowDamagePopup(float damage)
    {
        GameObject damagePopupPrefab = Resources.Load("Prefabs/DamagePopup") as GameObject;
        PrefabFactory.SpawnDamagePopup(damagePopupPrefab, (int)damage, team, transform.position);
    }

    // Only server can call this RPC, and it will run only on all clients
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
    public void RPC_HurtEffects(float damage)
    {
        HurtEffects(damage);
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

        if (!Runner.IsResimulation)
            UpdateHealthBar(newHealth);
    }

    public void GainPoints(int amount)
    {
        points += amount;
        if(points > maxPoints){
            points = maxPoints;
        }

        if (!Runner.IsResimulation)
            UpdatePointsBar();
    }

    public void SpendPoints(int amount)
    {
        if(amount > points)
        {
            Debug.Log("Not enough points");
        }
        else
        {
            points -= amount;
            if (!Runner.IsResimulation)
                UpdatePointsBar();
        }
    }

    void Die(PlayerRef killer)
    {
        isAlive = false;

        // Ensure health bar is empty
        UpdateHealthBar(0.0f);

        // Disable the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = false;
        gameController.UnregisterAlivePlayer(this);
        
        if (HasStateAuthority)
            RPC_PlayDyingSound(transform.position);

        if (isCarrying)
        {
            // Player will drop the flag if they died
            DropObject();
        }

        // Award points to killer
        if (Runner.TryGetPlayerObject(killer, out NetworkObject networkPlayerObject))
        {
            Player player = networkPlayerObject.GetComponent<Player>();
            if (player.team != team)
            {
                player.GainPoints(10);
            }
        }

        gameObject.SetActive(false);
    }

    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
    public void RPC_PlayDyingSound(Vector3 pos)
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);
        bool onScreen =
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f;

        if (onScreen)
        {
            audioSource.PlayOneShot(dyingSound, 0.7f);
        }

    }

    public void EnableMeleeHitbox()
    {
        meleeHitbox.SetActive(true);
    }

    public void DisableMeleeHitbox()
    {
        meleeHitbox.SetActive(false);
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

    void OnPointsChanged()
    {
        UpdatePointsBar();
    }

    void UpdatePointsBar(){
        float fillAmount = points/maxPoints;
        mainPointsBar.fillAmount = fillAmount;
    }

    void Reload()
    {
        if (currentAmmo >= maxAmmo)
        {
            if (!Runner.IsResimulation)
                ShowMessage("Mana is full!", 0.1f, Color.white);
            return; 
        }
        if (reloadTimer <= 0)
        {
            missingAmmo = maxAmmo - currentAmmo;
            reloadFraction = (float)missingAmmo / maxAmmo;
            reloadTimer = reloadTime * reloadFraction;
            timeToWaitForBullet = reloadTimer;

            if (!Runner.IsResimulation)
            {
                ShowMessage("Gathering Mana", 0.3f, Color.green);
                reloadIcon.enabled = true;
                reloadIconLayer.enabled = true;
                reloadHandler.StartCooldown(reloadTimer);
            }
        }
        else
        {
            if (!Runner.IsResimulation)
                ShowMessage("Still gathering mana", 0.3f, Color.white);
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
            uIController.MakePopupText(message, speed, color);
        }
    }

    public void ResumeGame()
    {
        escapeMenu.SetActive(false);

        // This ResumeGame method is called only locally via the pause menu UI, so the change
        // in the gamePaused networked property will not take place on the host, so we need to
        // call an RPC to change it
        RPC_ResumeGame();
    }

    // Only the client that controls this player can call this RPC, and it will run only on the server
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_ResumeGame()
    {
        gamePaused = false;
    }

    public void LeaveMatch()
    {
        // Shut down the network runner, which will cause the game to return to the main menu
        Runner.Shutdown();
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

    public string GetCharacterName()
    {
        return characterName;
    }

    public void ActivateTri(bool tri)
    {
        if (tri)
        {
            isAoEEnabled = true; // Enable AoE
            normalShoot = false;
            aoeIcon.enabled = true;
            isAoEUsed = false;
            StartCoroutine(EnableAoETemporarily());
        } 
    }

    public float GetPoints()
    {
        return points;
    }

    public void GetSlowed(float amount, float time)
    {
        this.speed -= amount;
        StartCoroutine(timeSlowed(amount, time));
    }

    IEnumerator timeSlowed(float amount, float time)
    {
        yield return new WaitForSeconds(time);
        this.speed += amount;
    }

    private IEnumerator EnableAoETemporarily()
    {
        yield return new WaitForSeconds(5f); 
        if (!isAoEUsed)
        {
            isAoEEnabled = false; // Disable AoE
            normalShoot = true;
            aoeIcon.enabled = false;
        }
        
    }
}
