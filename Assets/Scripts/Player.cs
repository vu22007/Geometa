using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    float speed;
    float maxHealth;
    [SerializeField] Character character;
    float currentHealth;
    public bool isAlive;
    float respawnTime = 10.0f;
    float currentRespawn = 0.0f;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    
    //Player initialisation (Also used for respawning)
    public void PlayerStart(Vector3Int spawnPoint)
    {
        gameObject.transform.position = spawnPoint;

        //player gets stats from its character
        maxHealth = character.MaxHealth;
        speed = character.Speed;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = character.Sprite;

        currentHealth = maxHealth;
        isAlive = true;

        rb = gameObject.GetComponent<Rigidbody2D>();
    }

    //Update function, called from the game controller
    public void PlayerUpdate()
    {
        if (isAlive) {
            PlayerMovement();
        }
        else {
            //Respawn timer
        }
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
}
