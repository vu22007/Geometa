using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CircleCornerCollider : NetworkBehaviour
{
    [Networked] public int team { get; set; }
    [Networked] float score { get; set; }
    [Networked] public PlayerRef parentPlayerRef { get; set; }
    [Networked] private TickTimer disableTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnColliderActivated))] private int activatedCollider { get; set; }

    private CircleCollider2D circleCollider;
    private List<Player> slowedPlayers;
    private Animator shockwaveAnimator;
    private Vector3 defaultScale;
    private float defaultRadius;

    public void OnCreated(int team, PlayerRef parentPlayerRef)
    {
        this.team = team;
        this.parentPlayerRef = parentPlayerRef;
    }

    public override void Spawned()
    {
        slowedPlayers = new List<Player>();
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.enabled = false;

        shockwaveAnimator = GetComponent<Animator>();
        // Change the localScale to change the size of the animation
        defaultScale = new Vector3(16f, 16f, 16f);
        defaultRadius = 0.33f;

        circleCollider.radius = defaultRadius;

        // Register with the collider's associated SquareShape object
        if (Runner.TryGetPlayerObject(parentPlayerRef, out NetworkObject playerNetworkObject))
        {
            Transform shapeController = playerNetworkObject.transform.Find("ShapeController");
            SquareShape squareShape = shapeController.Find("SquareShape").GetComponent<SquareShape>();
            squareShape.RegisterCircleCornerCollider(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (disableTimer.Expired(Runner))
        {
            circleCollider.enabled = false;
            slowedPlayers = new List<Player>();
            disableTimer = TickTimer.None;
        }
    }

    public void ActivateCollider(Vector3 pos, float score)
    {
        this.score = score;
        transform.localScale = defaultScale * score;
        transform.position = pos;
        circleCollider.enabled = true;
        disableTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);

        // Signal that the collider was activated for OnColliderActivated to be called
        activatedCollider++;
    }

    void OnColliderActivated()
    {
        // Show shockwave animation on all clients and host
        TriggerShockwave();
    }

    void TriggerShockwave()
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

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            Player player = collider.GetComponentInParent<Player>();

            if (player.GetTeam() != team && !slowedPlayers.Contains(player))
            {
                player.GetSlowed(2f * score, 3f);
                player.TakeDamage(2.5f * score, Object.InputAuthority);
                slowedPlayers.Add(player);
            }
        }
    }
}