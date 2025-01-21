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
    [SerializeField] Character character;
    Camera cam;
    float currentHealth;
    public bool isAlive;
    float respawnTime = 10.0f;
    float currentRespawn = 0.0f;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;

    //For the prefab factory (For when we have multiple players), to be called on instantiation of the prefab
    public void OnCreated(Character character){
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        cam = Camera.main;
        spriteRenderer.sprite = character.Sprite;
    }
    
    //Player initialisation (Also used for respawning)
    public void PlayerStart(Vector3Int spawnPoint)
    {
        gameObject.transform.position = spawnPoint;

        //player gets stats from its character
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        damage = character.Damage;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = character.Sprite;

        currentHealth = maxHealth;
        isAlive = true;

        rb = gameObject.GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    //Update function, called from the game controller, returns a bullet if one is fired
    public Bullet PlayerUpdate()
    {
        Bullet bullet = null;
        if (isAlive) {
            PlayerMovement();
            if (Input.GetMouseButtonDown(0)){
                bullet = ShootBullet();
            }
        }
        else {
            //Respawn timer
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
    }

    //Shoots a bullet by spawning the prefab
    Bullet ShootBullet()
    {
        GameObject bulletPrefab = Resources.Load("Prefabs/Bullet") as GameObject;
        Vector3 direction = CalculateDirectionFromMousePos();
        Bullet bullet = PrefabFactory.SpawnBullet(bulletPrefab, gameObject.transform.position, direction, 10.0f, damage);
        return bullet;
    }

    Vector3 CalculateDirectionFromMousePos(){
        Vector2 mousePos = Input.mousePosition;
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        Vector3 direction = (worldPoint - gameObject.transform.position).normalized;
        return direction;
    }
}
