using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Physics;

public class Player : NetworkBehaviour
{
    [Networked] string displayName { get; set; }
    [Networked] float speed { get; set; }
    [Networked] float maxHealth { get; set; }
    [Networked] float maxMana { get; set; }
    [Networked] float damage { get; set; }
    [Networked] int maxAmmo { get; set; }
    [Networked, OnChangedRender(nameof(OnCurrentAmmoChanged))] int currentAmmo { get; set; }
    [Networked] int missingAmmo { get; set; }
    [Networked] float attackRate { get; set; }
    [Networked] float reloadTime { get; set; }
    [Networked] bool alreadyReloading { get; set; }
    [Networked, OnChangedRender(nameof(OnReloadTimerChanged))] TickTimer reloadTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnManaChanged))] float mana { get; set; }
    [Networked] TickTimer attackWaitTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnHealthChanged))] float currentHealth { get; set; }
    [Networked] int team { get; set; }
    [Networked] Vector3 respawnPoint { get; set; }
    [Networked, OnChangedRender(nameof(OnIsAliveChanged))] bool isAlive { get; set; }
    [Networked] float respawnTime { get; set; }
    [Networked] TickTimer respawnTimer { get; set; }
    [Networked, Capacity(30)] string characterName { get; set; }
    [Networked] NetworkButtons previousButtons { get; set; }
    [Networked] private NetworkObject carriedObject { get; set; }
    [Networked, OnChangedRender(nameof(OnCarryingChanged)), HideInInspector] public bool isCarrying { get; set; }
    [Networked, OnChangedRender(nameof(OnIsMovingChanged))] bool isMoving { get; set; }
    [Networked, OnChangedRender(nameof(OnIsAttackingChanged))] bool isAttacking { get; set; }
    [Networked, OnChangedRender(nameof(OnIsDashingChanged))] bool isDashing { get; set; }
    [Networked] TickTimer dashTimer { get; set; }
    [Networked] TickTimer dashCooldownTimer { get; set; }
    [Networked] float dashSpeed { get; set; }
    [Networked] float dashDuration { get; set; }
    [Networked] float dashCooldown { get; set; }
    [Networked] bool alreadyDashing { get; set; }
    [Networked] public float aoeDamage { get; set; }
    [Networked] public float aoeDuration { get; set; }
    [Networked, OnChangedRender(nameof(OnAoEEnabledChanged))] public bool isAoEEnabled { get; set; }
    [Networked] private bool isAoEUsed { get; set; }
    [Networked] private bool normalShoot { get; set; }
    [Networked] private TickTimer aoeEnabledTimer { get; set; }
    [Networked] float slowedAmount { get; set; }
    [Networked] TickTimer getSlowedTimer { get; set; }
    [Networked] float speedIncrease { get; set; }
    [Networked] TickTimer speedIncreaseTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnInvinsibleChanged))] bool invinsible { get; set; }
    [Networked] TickTimer invinsibleTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnGamePausedChanged))] private bool gamePaused { get; set; }
    [Networked, OnChangedRender(nameof(MeleeAttackRender))] private int meleeAttacked { get; set; }
    [Networked, OnChangedRender(nameof(ShootRender))] private int bulletFired { get; set; }
    [Networked, OnChangedRender(nameof(ShootAoERender))] private int aoeFired { get; set; }
    [Networked, OnChangedRender(nameof(ReloadRender))] private int reloadPerformed { get; set; }
    [Networked, OnChangedRender(nameof(DashRender))] private int dashPerformed { get; set; }
    [Networked] int circleSegments { get; set; }
    [Networked] float circleRadius { get; set; }
    [Networked] float totalDamageDealt { get; set; }
    [Networked] int totalKills { get; set; }
    [Networked] int totalDeaths { get; set; }
    [Networked] int totalFlagsCaptured { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Animator animator;
    [SerializeField] Image mainHealthBar;
    [SerializeField] Image teamHealthBar;
    [SerializeField] Image mainManaBar;
    [SerializeField] Image minimapIndicator;
    [SerializeField] Image enemyHealthBar;
    [SerializeField] UIController uIController;
    [SerializeField] cooldownHandler dashCDHandler;
    [SerializeField] cooldownHandler reloadHandler;
    [SerializeField] cooldownHandler squareHandler;
    [SerializeField] cooldownHandler triangleHandler;
    [SerializeField] Image reloadIcon;
    [SerializeField] Image reloadIconLayer;
    [SerializeField] Image aoeIcon;
    [SerializeField] Image aoeIconLayer;
    [SerializeField] GameObject escapeMenu;
    [SerializeField] Minimap minimap;
    Image healthBar;
    public TextMeshProUGUI ammoText;
    GameController gameController;
    [SerializeField] GameObject deathOverlay;
    [SerializeField] TextMeshProUGUI respawnTimerTxt;
    [SerializeField] FlagIndicator flagIndicator;
    [SerializeField] TextMeshProUGUI displayNameText;
    [SerializeField] GameObject invinsibleImage;
    private AudioClip shootSound;
    private AudioClip dyingSound;
    private AudioClip dashSound;
    private AudioClip reloadSound;
    private AudioClip knightSwordSound;
    private AudioSource audioSource;
    private AudioSource reloadAudioSource;
    [SerializeField] Image bulletIcon;
    [SerializeField] GameObject mainbulletIcon;
    [SerializeField] MeleeHitbox meleeHitbox;
    public LineRenderer circleRenderer;
    [SerializeField] Transform pointer;

    // Player intialisation (called from game controller on server when creating the player)
    public void OnCreated(string displayName, string characterName, Vector3 respawnPoint, int team)
    {
        Character character = Resources.Load($"ScriptableObjects/Characters/{characterName}") as Character;
        maxHealth = character.MaxHealth;
        maxMana = 30f;
        speed = character.Speed;
        damage = character.Damage;
        maxAmmo = character.MaxAmmo;
        attackRate = character.AttackRate;
        dashSpeed = character.DashSpeed;
        dashDuration = character.DashDuration;
        dashCooldown = character.DashCooldown;
        characterName = character.name;

        this.displayName = displayName;
        this.respawnPoint = respawnPoint;
        this.team = team;
        this.characterName = characterName;
        mana = 30f;
        reloadTime = 3.0f;
        respawnTime = 10.0f;
        aoeDamage = 5;
        aoeDuration = 5;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        isAlive = true;
        isCarrying = false;
        isAoEEnabled = false;
        isAoEUsed = false;
        normalShoot = true;
        gamePaused = false;
        invinsible = false;

        totalDamageDealt = 0;
        totalKills = 0;
        totalDeaths = 0;
        totalFlagsCaptured = 0;
    }

    // Player initialisation (called on each client and server when player is spawned on network)
    public override void Spawned()
    {
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
        animator.runtimeAnimatorController = Resources.Load("Animations/" + character.name) as RuntimeAnimatorController;

        int localPlayerTeam = gameController.playersToTeams[Runner.LocalPlayer];

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
        UpdateHealthBar();

        //Set the points bar
        UpdateManaBar();

        // Disable the death overlay
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
        reloadSound = Resources.Load<AudioClip>("Sounds/WizardReload");
        knightSwordSound = Resources.Load<AudioClip>("Sounds/KnightSword");

        // Create separate audio source just for reload sounds, so the pitch can be changed without affecting other sounds
        reloadAudioSource = gameObject.AddComponent<AudioSource>();

        // Set the initial flag indicator visibility
        OnCarryingChanged();

        // Set display name text
        displayNameText.text = displayName;

        // Disable display name text if client controls this player
        if (HasInputAuthority)
            displayNameText.gameObject.SetActive(false);

        // Disable ammo indicator for knight
        if (characterName == "Knight")
        {
            mainbulletIcon.SetActive(false);
        }

        circleSegments = 128;
        circleRadius = 8f;
        circleRenderer.positionCount = circleSegments + 1;
        circleRenderer.loop = true;
        circleRenderer.startWidth = 0.3f;
        circleRenderer.endWidth = 0.3f;
        circleRenderer.material = new Material(Shader.Find("Unlit/Color"));
        circleRenderer.material.color = Color.red;
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
        // Teleport player to respawn point
        gameObject.GetComponent<NetworkRigidbody2D>().Teleport(respawnPoint);

        // Reset state
        isAlive = true;
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;
        respawnTimer = TickTimer.None;
        attackWaitTimer = TickTimer.None;

        // Activate the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = true;

        // Enable the hitbox
        gameObject.GetComponent<HitboxRoot>().HitboxRootActive = true;

        // Make invinsible (Spawn protection)
        invinsible = true;
        invinsibleTimer = TickTimer.CreateFromSeconds(Runner, 4f);
    }

    public override void Render()
    {
        if (!isAlive)
        {
            // Update the respawn timer text
            if (respawnTimerTxt != null)
            {
                // Calculate the remaining respawn time
                float remainingTime = respawnTimer.RemainingTime(Runner).GetValueOrDefault();
                respawnTimerTxt.text = $"Respawning in {Mathf.CeilToInt(remainingTime)}";
            }
        }

        if (isCarrying)
        {
            UpdatePointer();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Check if player is dead
        if (!isAlive)
        {
            // Respawn player if timer is over
            if (respawnTimer.Expired(Runner))
            {
                Respawn();
            }

            // Stop player movement and prevent player from infinitely sliding when pushed by another player
            rb.linearVelocity = new Vector2(0, 0);

            return;
        }

        // Handle reloading
        if (reloadTimer.Expired(Runner))
        {
            // Reloading is complete, update ammo
            currentAmmo = maxAmmo;
            reloadTimer = TickTimer.None;
        }

        // Handle dash duration
        if (isDashing)
        {
            if (dashTimer.Expired(Runner))
            {
                isDashing = false; // End dash
            }
        }

        // Auto reload 
        if (currentAmmo == 0 && !reloadTimer.IsRunning)
        {
            Reload();
        }

        // AoE timer
        if (aoeEnabledTimer.Expired(Runner))
        {
            if (!isAoEUsed)
            {
                // Disable AoE
                isAoEEnabled = false;
                normalShoot = true;
            }

            // Reset timer
            aoeEnabledTimer = TickTimer.None;
        }

        // Get slowed timer
        if (getSlowedTimer.Expired(Runner))
        {
            // Restore speed
            speed += slowedAmount;
            slowedAmount = 0;

            // Reset timer
            getSlowedTimer = TickTimer.None;
        }

        // Increase speed timer
        if (speedIncreaseTimer.Expired(Runner))
        {
            // Restore speed
            speed -= speedIncrease;
            speedIncrease = 0;

            // Reset timer
            speedIncreaseTimer = TickTimer.None;
        }

        // Spawn protection timer
        if (invinsibleTimer.Expired(Runner))
        {
            //Make damageable
            invinsible = false;

            //Reset timer
            speedIncreaseTimer = TickTimer.None;
        }

        // GetInput will return true on the StateAuthority (the server) and the InputAuthority (the client who controls this player)
        // So the following is ran for just the server and the client who controls this player
        if (GetInput(out NetworkInputData input))
        {
            // If game is not paused
            if (!gamePaused)
            {
                // WASD movement
                PlayerMovement(input.moveDirection);

                // Firing the weapon
                if (characterName == "Knight")
                {
                    if (input.buttons.WasPressed(previousButtons, InputButtons.Shoot))
                    {
                        isAttacking = !isAttacking;
                        MeleeAttack();
                    }
                }

                else
                {
                    if (input.buttons.IsSet(InputButtons.Shoot))
                    {
                        if (normalShoot && !isAoEEnabled)
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

                if (input.buttons.WasPressed(previousButtons, InputButtons.AoE))
                {
                    if (isAoEEnabled && !normalShoot)
                    {
                        ShootAoE(input.aimDirection, input.cursorWorldPoint);
                        isAttacking = true;
                    }
                    else
                    {
                        isAttacking = false;
                    }
                }

                // Reloading
                if (input.buttons.WasPressed(previousButtons, InputButtons.Reload) && characterName != "Knight")
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
        if (dashCooldownTimer.Expired(Runner) || !dashCooldownTimer.IsRunning) // Only allow dash if cooldown is over
        {
            isDashing = true;
            dashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
            dashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
            alreadyDashing = false;
        }
        else
        {
            alreadyDashing = true;
        }

        // Signal that the dash was performed for DashRender to be called
        dashPerformed++;
    }

    void DashRender() {
        if (alreadyDashing)
        {
            ShowMessage("Dash in cooldown", 0.2f, Color.white);
        }
    }

    void OnIsDashingChanged()
    {
        if (isDashing && HasInputAuthority)
        {
            dashCDHandler.StartCooldown(dashCooldown);
            audioSource.PlayOneShot(dashSound);
        }
    }

    void MeleeAttack()
    {
        if (attackWaitTimer.Expired(Runner) || !attackWaitTimer.IsRunning)
        {
            attackWaitTimer = TickTimer.CreateFromSeconds(Runner, 1 / attackRate);
            meleeHitbox.CheckForHit();

            // Signal that the melee attack was performed for MeleeAttackRender to be called
            meleeAttacked++;
        }
    }

    void MeleeAttackRender()
    {
        // Just the player that does the melee attack listens to the sound
        if (HasInputAuthority)
        {
            audioSource.PlayOneShot(knightSwordSound);
        }
    }

    // Shoots a bullet by spawning the prefab on the network
    void Shoot(Vector2 aimDirection)
    {
        if (attackWaitTimer.Expired(Runner) || !attackWaitTimer.IsRunning)
        {
            attackWaitTimer = TickTimer.CreateFromSeconds(Runner, 1 / attackRate);
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

                currentAmmo--;

                // Signal that bullet was fired for ShootRender to be called
                bulletFired++;
            }
        }
    }

    void ShootRender()
    {
        // Just the player that shoots listens to the sound
        if (HasInputAuthority)
        {
            PlayShootSound();
        }

        OnCurrentAmmoChanged();
    }

    void OnCurrentAmmoChanged()
    {
        // Update ammo indicator to new value
        ammoText.text = currentAmmo.ToString();
        bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
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

        isAoEEnabled = false;
        normalShoot = true;

        // Signal that AoE was fired for ShootAoERender to be called
        aoeFired++;
    }

    void ShootAoERender()
    {
        // Just the player that shoot listens to the sound
        if (HasInputAuthority)
        {
            PlayShootSound();
        }
    }

    void OnAoEEnabledChanged()
    {
        aoeIcon.enabled = isAoEEnabled;
    }

    public void PlayShootSound()
    {
        audioSource.PlayOneShot(shootSound);
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage, PlayerRef damageDealer)
    {
        if (!invinsible)
        {
            currentHealth -= damage;

            // Add damage to damage dealer's total damage dealt counter
            if (Runner.TryGetPlayerObject(damageDealer, out NetworkObject networkPlayerObject))
            {
                Player player = networkPlayerObject.GetComponent<Player>();
                if (player.team != team)
                {
                    player.IncreaseDamageDealtCounter(damage);
                }
            }

            if (currentHealth <= 0.0f)
            {
                Die(damageDealer);
            }
        }
    }

    void HurtEffects(float damage) {
        animator.SetTrigger("Damaged");
        ShowDamagePopup(damage);
    }

    void ShowDamagePopup(float damage)
    {
        GameObject damagePopupPrefab = Resources.Load("Prefabs/DamagePopup") as GameObject;
        PrefabFactory.SpawnDamagePopup(damagePopupPrefab, (int)damage, team, transform.position);
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount)
    {
        currentHealth += amount;

        if (currentHealth >= maxHealth) {
            currentHealth = maxHealth;
        }
    }

    public void GainMana(int amount)
    {
        mana += amount;
        if (mana > maxMana) {
            mana = maxMana;
        }
    }

    public void SpendMana(int amount)
    {
        if (amount <= mana)
        {
            mana -= amount;
        }
    }

    void Die(PlayerRef killer)
    {
        if (!isAlive) return;

        isAlive = false;
        totalDeaths++;

        respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);

        // Disable the shape controller
        gameObject.GetComponentInChildren<ShapeController>().isActive = false;

        // Disable the hitbox
        gameObject.GetComponent<HitboxRoot>().HitboxRootActive = false;

        // Player will drop the flag if they died
        if (isCarrying)
        {
            DropObject();
        }

        // Award Mana to killer and increment their kill count
        if (Runner.TryGetPlayerObject(killer, out NetworkObject networkPlayerObject))
        {
            Player player = networkPlayerObject.GetComponent<Player>();
            if (player.team != team)
            {
                player.GainMana(10);
                player.IncrementKillCount();
            }
        }
    }

    void OnIsAliveChanged()
    {
        // Ensure health bar is updated
        UpdateHealthBar();

        // Toggle the death overlay
        if (HasInputAuthority && deathOverlay != null)
        {
            deathOverlay.SetActive(!isAlive);
        }

        // Toggle player visibility
        SetPlayerEnabled(isAlive);

        // Play death sound if dead
        if (!isAlive)
        {
            PlayDyingSound(transform.position);
        }

        // Register/unregister player with game controller depending on if alive or dead
        if (isAlive) gameController.RegisterAlivePlayer(this);
        else gameController.UnregisterAlivePlayer(this);
    }

    void SetPlayerEnabled(bool enabled)
    {
        GetComponent<SpriteRenderer>().enabled = enabled;
        transform.Find("Collider").gameObject.SetActive(enabled);
        transform.Find("Overhead UI").gameObject.SetActive(enabled);
        transform.Find("MinimapIndicator").gameObject.SetActive(enabled);
    }

    public void PlayDyingSound(Vector3 pos)
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

    void IncrementKillCount()
    {
        totalKills++;
    }

    void IncreaseDamageDealtCounter(float damage)
    {
        totalDamageDealt += damage;
    }

    void OnHealthChanged(NetworkBehaviourBuffer previous)
    {
        float previousHealth = GetPropertyReader<float>(nameof(currentHealth)).Read(previous);

        UpdateHealthBar();

        if (currentHealth < previousHealth)
        {
            // Player took damage so show hurt effects
            float damage = previousHealth - currentHealth;
            HurtEffects(damage);
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }
    }

    void OnManaChanged()
    {
        UpdateManaBar();
    }

    void UpdateManaBar() {
        mainManaBar.fillAmount = mana / maxMana; ;
    }

    void Reload()
    {
        bool manaFull = currentAmmo >= maxAmmo;

        if (!manaFull)
        {
            if (!reloadTimer.IsRunning)
            {
                missingAmmo = maxAmmo - currentAmmo;
                float reloadFraction = (float)missingAmmo / maxAmmo;
                float time = reloadTime * reloadFraction;
                reloadTimer = TickTimer.CreateFromSeconds(Runner, time);
                attackWaitTimer = TickTimer.CreateFromSeconds(Runner, time);
                alreadyReloading = false;
            }
            else
            {
                alreadyReloading = true;
            }
        }

        // Signal that the reload was performed for ReloadRender to be called
        reloadPerformed++;
    }

    void ReloadRender()
    {
        if (currentAmmo >= maxAmmo)
        {
            ShowMessage("Energy is full!", 0.1f, Color.white);
        }
        else if (alreadyReloading)
        {
            ShowMessage("Still gathering energy", 0.3f, Color.white);
        }
    }

    void OnReloadTimerChanged()
    {
        if (HasInputAuthority)
        {
            // Reload has started
            if (reloadTimer.IsRunning)
            {
                ShowMessage("Gathering Energy", 0.3f, Color.green);

                // Play sound (use separate reload audio source so that the pitch can be adjusted without affecting other sounds)
                reloadAudioSource.pitch = 2.7f / missingAmmo;
                reloadAudioSource.PlayOneShot(reloadSound);

                // Update icon
                float time = reloadTimer.RemainingTime(Runner).GetValueOrDefault();
                reloadIcon.enabled = true;
                reloadIconLayer.enabled = true;
                reloadHandler.StartCooldown(time);
            }

            // Reload has finished
            else
            {
                ammoText.text = maxAmmo.ToString();
                bulletIcon.fillAmount = 1;
                reloadIcon.enabled = false;
                reloadIconLayer.enabled = false;
            }
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
            if (!flag.IsInsideCollider())
            {
                flag.Drop();
                carriedObject = null;
                isCarrying = false;
                speed *= 2;
                gameController.CheckForWinCondition();
                gameController.BroadcastDropFlag(team, flag.team);
            }
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

        if (HasInputAuthority)
        {
            if (isCarrying)
            {
                circleRenderer.enabled = true;
                pointer.gameObject.SetActive(true);
                UpdatePointer();
                DrawCircle(respawnPoint, circleRadius);
            }
            else
            {
                circleRenderer.enabled = false;
                pointer.gameObject.SetActive(false);
            }
        }
    }

    void DrawCircle(Vector3 center, float radius)
    {
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            Vector3 point = center + new Vector3(x, y, 0);
            circleRenderer.SetPosition(i, point);
        }
    }

    void UpdatePointer()
    {
        if (pointer == null) return;

        Vector3 direction = (respawnPoint - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);
        pointer.rotation = rotation;
    }

    public void ShowMessage(string message, float speed, Color color)
    {
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

    public void ActivateTri(bool tri)
    {
        if (tri)
        {
            isAoEEnabled = true; // Enable AoE
            normalShoot = false;
            isAoEUsed = false;
            aoeEnabledTimer = TickTimer.CreateFromSeconds(Runner, 5f);
        } 
    }

    public void ActivateTriCD(float triCD)
    {
        triangleHandler.StartCooldown(triCD);
    }

    public void ActivateSqCD(float sqCD)
    {
        squareHandler.StartCooldown(sqCD);
    }

    public float GetMana()
    {
        return mana;
    }

    public void IncreaseSpeed(float amount, float time){
        if(speedIncrease == 0){
            speed += amount;
            speedIncrease += amount;
        }

        speedIncreaseTimer = TickTimer.CreateFromSeconds(Runner, time);
    }
    public void GetSlowed(float amount, float time)
    {
        // If not already slowed, slow the player
        if (slowedAmount == 0)
        {
            speed -= amount;
            slowedAmount = amount;
        }

        // Set timer until the player's speed returns to normal
        // Note: If they are already slowed, this resets the timer so they have to wait longer, but the above
        // prevents their speed from getting even slower
        getSlowedTimer = TickTimer.CreateFromSeconds(Runner, time);
    }

    void OnIsMovingChanged()
    {
        // Play idle or walking animation
        if (isMoving)
            animator.SetFloat("Speed", 0.02f);
        else
            animator.SetFloat("Speed", 0f);
    }

    void OnIsAttackingChanged()
    {
        if (characterName == "Knight")
        {
            // Regardless of value, trigger animation when attacking property is toggled
            animator.SetTrigger("Attack");
        }
        else
        {
            // Only when attacking property is set to true, trigger animation
            if (isAttacking)
            {
                animator.SetTrigger("Attack");
            }
        }
    }

    void OnGamePausedChanged() {
        escapeMenu.SetActive(gamePaused);
    }

    void OnInvinsibleChanged() {
        invinsibleImage.SetActive(invinsible);
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public int GetTeam()
    {
        return team;
    }

    public float GetDamage()
    {
        return damage;
    }

    public string GetDisplayName()
    {
        return displayName;
    }

    public string GetCharacterName()
    {
        return characterName;
    }

    public int GetTotalKills()
    {
        return totalKills;
    }

    public int GetTotalDeaths()
    {
        return totalDeaths;
    }

    public float GetTotalDamageDealt()
    {
        return totalDamageDealt;
    }

    public int GetTotalFlagsCaptured()
    {
        return totalFlagsCaptured;
    }
}
