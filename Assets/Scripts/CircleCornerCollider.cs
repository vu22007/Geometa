using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCornerCollider : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] float score { get; set; }
    private CircleCollider2D circleCollider;
    private List<Player> slowedPlayers;

    public GameObject shockwavePrefab;

    public override void Spawned()

    {
        slowedPlayers = new List<Player>();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.enabled = false;
    }

    public void ActivateCollider(Vector3 pos, float score)
    {
        this.score = score;
        
        circleCollider.transform.position = pos;
        circleCollider.enabled = true;

        // Trigger animation
        TriggerShockwave(pos);


        StartCoroutine(DelayDisable(0.1f));
    }

    void TriggerShockwave(Vector3 position)
    {
        if (shockwavePrefab != null)
        {
            if (!Runner.IsRunning)
            {
                Debug.LogWarning("Fusion is not running! Cannot spawn shockwave.");
                return;
            }

            // Spawn with correct ownership (Prevent owner-prediction issues)
            NetworkObject shockwaveObject = Runner.Spawn(shockwavePrefab, position, Quaternion.identity, Object.InputAuthority);

            Debug.Log($"Shockwave spawned at {position}");

            Animator shockwaveAnimator = shockwaveObject.GetComponent<Animator>();
            float animLength = (shockwaveAnimator != null) ? shockwaveAnimator.GetCurrentAnimatorStateInfo(0).length : 1.5f;

            Debug.Log($"Shockwave animation length: {animLength}");

            StartCoroutine(DespawnShockwave(shockwaveObject, animLength));
        }
        else
        {
            Debug.LogWarning("Shockwave prefab not assigned!");
        }
    }

   IEnumerator DespawnShockwave(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null && obj.IsValid)
        {
            Debug.Log($"Despawning shockwave after {delay} seconds.");

            Runner.Despawn(obj); // Fusion despawn
            Destroy(obj.gameObject); // Forcefully remove from scene
        }
        else
        {
            Debug.LogWarning("Shockwave object is already destroyed or invalid.");
        }
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        circleCollider.enabled = false;
        // Set an empty list when ability ends
        slowedPlayers = new List<Player>();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        // Prevent running if not fully spawned in the network
        if (!Object || !HasStateAuthority || !Runner.IsRunning) return;

        if (collider.gameObject.tag == "Player")
        {
            Player player = collider.GetComponentInParent<Player>();

            // Ensure 'team' is accessed only after Spawned() is called
            if (player != null && Object.IsValid && player.GetTeam() != team && !slowedPlayers.Contains(player))
            {
                Debug.Log("Enemy slowed");
                player.GetSlowed(2f * score, 1f);
                slowedPlayers.Add(player);
            }
        }
    }
}
