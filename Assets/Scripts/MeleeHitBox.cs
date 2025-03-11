using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    public float damage = 5f;
    public Player player;

    private void Start()
    {
        player = GetComponentInParent<Player>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int team = player.GetTeam();
        if (other.CompareTag("Player")) // Check if it hits a player
        {
            Player hitplayer = other.GetComponentInParent<Player>();
            if (hitplayer != null && hitplayer.GetTeam() != team)
            {
                hitplayer.TakeDamage(damage, player.Object.InputAuthority);
            }
        }
    }
}
