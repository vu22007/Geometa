using UnityEngine;

public class Bullet : MonoBehaviour
{
    Vector3 velocity;
    float damage;
    
    public void OnCreated(Vector3 startDirection, float speed, float damage){
        velocity = startDirection * speed;
        velocity.z = 0;
        this.damage = damage;
    }

    //On colliding with a collider
    void OnTriggerEnter2D(Collider2D other){
        if (other.CompareTag("Player")){
            Player player = other.GetComponent<Player>();
            //Should check if player is from the enemy team
            if(player != null){
                player.TakeDamage(damage);
            }
        }
        Destroy(gameObject); //Doesn't pierce
    }

    //To be called in the game controller with the list of all currently active bullets
    public void BulletUpdate(){
        gameObject.transform.position += velocity * Time.deltaTime;
    }
}
