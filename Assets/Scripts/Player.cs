using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion.Addons.Physics;
using System.Collections;
using System.Collections.Generic;

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
    [Networked, Capacity(50)] string characterPath { get; set; }
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
    [Networked] string characterName { get; set; }
    [Networked] float meleeDamage { get; set; }
    [Networked] float meleeRange { get; set; }
    [Networked] float meleeRadius { get; set; }

    public Camera cam;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Animator animator;
    [SerializeField] Image mainHealthBar;
    [SerializeField] Image teamHealthBar;
    [SerializeField] Image mainPointsBar;
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
    private AudioClip shootSound;
    private AudioClip dyingSound;
    private AudioClip dashSound;
    private AudioSource audioSource;
    [SerializeField] Image bulletIcon;
    [SerializeField] Transform meleePoint;
    [SerializeField] GameObject meleeHitbox;
    
    // Player intialisation (called from game controller on server when creating the player)
    public void OnCreated(string characterPath, Vector3 respawnPoint, int team)
    {
        Character character = Resources.Load(characterPath) as Character;
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
        this.characterPath = characterPath;
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
        meleeDamage = 10.0f;
        meleeRange = 3.0f;
        meleeRadius = 0.5f;
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

        //Set animator controller
        animator.runtimeAnimatorController = Resources.Load("Animations/"+character.name) as RuntimeAnimatorController;

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
        ammoText.text = currentAmmo.ToString();
        bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;

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
                ammoText.text = currentAmmo.ToString();
                bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
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
                    // if (isAoEEnabled && !normalShoot)
                    // {
                    //     ShootAoE(input.aimDirection);
                    //     Debug.Log("Shoot aoe");
                    // }
                    // else if (normalShoot && !isAoEEnabled)
                    // {
                    //     Shoot(input.aimDirection);
                    //     Debug.Log("Shoot normal");
                    // }

                    // meleeHit();
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
                escapeMenu.SetActive(!escapeMenu.gameObject.activeSelf);
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

        if (isAttacking)
        {
            animator.SetTrigger("Attack");
        }

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

            if (mins < 0 || secs < 0)
                timeLeftText.text = "Time Left: 0:00";
            else
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
            audioSource.PlayOneShot(dashSound);
        }
        else
        {
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

                // Spawn bullet (only the server can do this)
                if (HasStateAuthority)
                {
                    GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
                    PrefabFactory.SpawnBullet(Runner, Object.InputAuthority, bulletPrefab, gameObject.transform.position, aimDirection, 40.0f, damage, team, Object.InputAuthority);
                }
                // Just the player that shoot listens to the sound
                if (HasInputAuthority)
                {
                    PlayShootSound();
                }
                currentAmmo--;
                ammoText.text = currentAmmo.ToString();
                bulletIcon.fillAmount = (float)currentAmmo / maxAmmo;
            }
        }
    }

    // Shoots a bullet by spawning the prefab on the network
    void ShootAoE(Vector2 aimDirection)
    {
        if (HasStateAuthority)
        {
            GameObject aoeSpellPrefab = Resources.Load("Prefabs/AoE1") as GameObject;
            NetworkObject aoeSpellObject = Runner.Spawn(aoeSpellPrefab, transform.position, Quaternion.identity, null, (runner, networkObject) =>
            {
                AoESpell aoeSpell = networkObject.GetComponent<AoESpell>();
                if (aoeSpell != null)
                {
                    aoeSpell.OnCreated(aimDirection, 10f, 10f, aoeDamage, team, aoeDuration, Object.InputAuthority);
                }
            });
        }
        // Just the player that shoot listens to the sound
        if (HasInputAuthority)
        {
            PlayShootSound();
        }  
        isAoEEnabled = false;
        normalShoot = true;
        aoeIcon.enabled = false;     
    }

    // void meleeHit()
    // {
    //     int layerMask = LayerMask.GetMask("Default");
    //     Vector2 attackPosition = (Vector2)transform.position + (spriteRenderer.flipX ? Vector2.left : Vector2.right) * meleeRange;
    //     Collider[] hitPlayers = Physics.OverlapBox(attackPosition, meleeRange, layerMask);

    //     foreach (Collider player in hitPlayers)
    //     {
    //         if (player.CompareTag("Player")) // Extra check for safety
    //         {
    //             player.GetComponent<Player>()?.TakeDamage(meleeDamage, Object.InputAuthority);
    //         }
    //     }
    

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

        UpdateHealthBar(newHealth);
    }

    public void GainPoints(int amount)
    {
        points += amount;
        if(points > maxPoints){
            points = maxPoints;
        }
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
            ShowMessage("Mana is full!", 0.1f, Color.white);
            return; 
        }
        if (reloadTimer <= 0)
        {
            ShowMessage("Gathering Mana", 0.3f, Color.green);
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
