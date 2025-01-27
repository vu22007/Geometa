using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class Player : MonoBehaviour
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
        this.team = team;
        fireRate = 0.25f;
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
            timeToWaitForBullet = (timeToWaitForBullet > 0) ? timeToWaitForBullet - Time.deltaTime : 0;

            // Firing the weapon
            if (Input.GetMouseButton(0)) {
                if (timeToWaitForBullet <= 0) {
                    timeToWaitForBullet = fireRate;
                    if (currentAmmo != 0) {
                        bullet = ShootBullet();
                    }
                    else {
                        Debug.Log("Press R to reload!!");
                    }
                }
            }

            // Reloading
            if (Input.GetKeyDown(KeyCode.R)) {
                Debug.Log("Reloading");
                timeToWaitForBullet = reloadTime;
                Reload();
            }
        }
        else {
            currentRespawn += Time.deltaTime;
        }
        return bullet;
    }

    //player moves according to key presses and player speed
    void PlayerMovement()
    {
        // Allow player to move
        float speedX = Input.GetAxisRaw("Horizontal");
        float speedY = Input.GetAxisRaw("Vertical");
        rb.linearVelocity = new Vector2(speedX, speedY).normalized * speed;

        // Flip sprite to face direction the player is moving in
        if (speedX < 0) {
            spriteRenderer.flipX = true;
        } else if (speedX > 0) {
            spriteRenderer.flipX = false;
        }
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
