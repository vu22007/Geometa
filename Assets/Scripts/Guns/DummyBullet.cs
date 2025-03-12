using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DummyBullet : MonoBehaviour
{
    Vector3 velocity;
    int team;
    Tick initialTick;

    // Bullet intialisation
    public void OnCreated(Vector2 position, Vector2 direction, Quaternion rotation, float speed, int team, Tick initialTick)
    {
        Vector2 velocity2D = direction.normalized * speed;
        velocity = new Vector3(velocity2D.x, velocity2D.y);
        this.team = team;
        this.initialTick = initialTick;

        // Set initial position of bullet
        transform.position = position;

        // Rotate the bullet into the correct orientation
        transform.rotation = rotation;
    }

    // Update function
    public void Update()
    {
        // Update current position of bullet
        transform.position += velocity * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                // Check if player is from the enemy team
                if (player.GetTeam() != team)
                {
                    Destroy(gameObject);
                }
            }
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    public Tick getInitialTick()
    {
        return initialTick;
    }
}
