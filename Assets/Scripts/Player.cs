using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    float speed;
    float maxHealth;
    [SerializeField] Character character;
    SpriteRenderer spriteRenderer;
    float currentHealth;
    public bool isAlive;
    float respawnTime = 10.0f;
    float currentRespawn = 0.0f;
    
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
    }

    //Update function, called from the game controller
    public void PlayerUpdate()
    {
        if(isAlive){
            PlayerMovement();
        }
        else{
            //Respawn timer
        }
    }

    //player moves according to key presses and player speed
    void PlayerMovement(){
        //TODO
    }

    //take damage equal to input, includes check for death
    public void TakeDamage(float damage){
        currentHealth -= damage;
        if (currentHealth <= 0.0f)
        {
            Die();
        }
    }

    //heal equal to input, includes check for max health
    public void Heal(float amount){
        currentHealth += amount;
        if (currentHealth >= maxHealth){
            currentHealth = maxHealth;
        }
    }

    void Die(){
        Debug.Log("You died :((");
        isAlive = false;
    }
}
