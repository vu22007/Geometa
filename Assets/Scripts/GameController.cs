using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameController : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag1;
    //[SerializeField] CaptureFlag flag2;

    [SerializeField] float maxTime = 480.0f; //8 minute games
    [SerializeField] float currentTime = 0.0f;

    private List<Bullet> bullets;
    private List<Player> players;

    // For server to use only so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // For server to use to keep track of what team to assign to a new player when they join
    int nextTeam = 1;

    // Initialisation
    void Start()
    {
        bullets = new List<Bullet>();
        players = new List<Player>();
    }

    // Update for every server simulation tick
    public override void FixedUpdateNetwork()
    {
        currentTime += Runner.DeltaTime;
        if (currentTime >= maxTime)
        {
            //end game
        }

        foreach (Player player in players)
        {
            player.PlayerUpdate();

            // If player is dead and respawn timer is done then respawn player
            if (!player.IsAlive() && player.RespawnTimerDone())
            {
                player.Respawn();
            }
        }

        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet bullet = bullets[i];
            bullet.BulletUpdate();

            if (bullet.done)
            {
                bullets.Remove(bullet);

                // Despawn bullet from network (only the server can do this)
                if (Runner.IsServer)
                {
                    Runner.Despawn(bullet.GetComponent<NetworkObject>());
                }
            }
        }
    }

    public void RegisterPlayer(Player player)
    {
        players.Add(player);
    }

    public void UnregisterPlayer(Player player)
    {
        players.Remove(player);
    }

    public void RegisterBullet(Bullet bullet)
    {
        bullets.Add(bullet);
    }

    public void UnregisterBullet(Bullet bullet)
    {
        bullets.Remove(bullet);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            // Load prefabs
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
            Character armyVet = Resources.Load("ScriptableObjects/Characters/Army Vet") as Character;

            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1, Quaternion.identity, player, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player playerObject = networkObject.GetComponent<Player>();
                playerObject.OnCreated(armyVet, respawnPoint1, nextTeam);
                nextTeam = (nextTeam == 1) ? 2 : 1; // Flip the next team so the next player to join will be on the other team
                Runner.SetPlayerObject(player, networkObject);
            });

            // Add player network object to dictionary
            spawnedPlayers.Add(player, networkPlayerObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            if (spawnedPlayers.TryGetValue(player, out NetworkObject networkPlayerObject))
            {
                // Despawn the network object and remove from dictionary
                Runner.Despawn(networkPlayerObject);
                spawnedPlayers.Remove(player);
            }
        }
    }
}
