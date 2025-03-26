using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MeleeHitbox : NetworkBehaviour
{
    [Networked] private float damage { get; set; }

    private Player player;

    public override void Spawned()
    {
        player = GetComponentInParent<Player>();
        damage = player.GetDamage();
    }

    public void CheckForHit()
    {
        // Lag-compensated hit detection
        int layerMask = LayerMask.GetMask("Default"); // Only register collisions with colliders and hitboxes on the "Default" layer
        HitOptions options = HitOptions.IgnoreInputAuthority;
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
        if (Runner.LagCompensation.OverlapSphere(transform.position, 0.3f, Object.InputAuthority, hits, layerMask, options) != 0)
        {
            // Resolve collision
            OnCollisionHitbox(hits[0].Hitbox);
        }
    }

    private void OnCollisionHitbox(Hitbox hitbox)
    {
        int team = player.GetTeam();
        if (hitbox.CompareTag("Player"))
        {
            Player hitPlayer = hitbox.GetComponent<Player>();
            if (hitPlayer != null && hitPlayer.GetTeam() != team)
            {
                hitPlayer.TakeDamage(damage, player.Object.InputAuthority);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
