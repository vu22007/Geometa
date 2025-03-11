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
    private Animator shockwaveAnimator;

    public override void Spawned()
    {
        slowedPlayers = new List<Player>();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.enabled = false;

        shockwaveAnimator = GetComponentInChildren<Animator>();
        // Change the localScale to change the size of the animation
        shockwaveAnimator.transform.localScale = new Vector3(5f, 5f, 5f);
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
            shockwaveAnimator.Play("ShockWave", 0, 0f);
            shockwaveAnimator.SetBool("Play", true);
            // Called so there is time for the animator to realise it is true
            StartCoroutine(DelayDisableAnimation(0.1f));
    }
    
    IEnumerator DelayDisableAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        shockwaveAnimator.SetBool("Play", false);
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
