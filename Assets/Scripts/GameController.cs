using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameController : SimulationBehaviour, IPlayerJoined, IPlayerLeft, ISceneLoadStart
{
    [SerializeField] Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag1;
    //[SerializeField] CaptureFlag flag2;

    [SerializeField] float maxTime = 480.0f; //8 minute games
    [SerializeField] float currentTime = 0.0f;

    private List<Bullet> bullets;
    private List<Player> players;

    private List<Pickup> pickups;


    // For server to use only so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // For server to use to keep track of what team to assign to a new player when they join
    int nextTeam = 1;

    // Initialisation
    void Start()
    {
        bullets = new List<Bullet>();
        players = new List<Player>();
        pickups = new List<Pickup>();
    }

    public void SceneLoadStart(SceneRef sceneRef)
    {
        // Spawn a pickup (only the server can do this)
        if (Runner.IsServer)
        {
            GameObject pickupPrefab = Resources.Load("Prefabs/Pickup") as GameObject;
            PrefabFactory.SpawnPickup(Runner, pickupPrefab, new Vector3(5f, 5f, 0f), 0, 20);
            
            GameObject flagPrefab = Resources.Load("Prefabs/Flag1") as GameObject;
            if (flagPrefab != null)
            {
                PrefabFactory.SpawnFlag(Runner, flagPrefab, new Vector3(20f, 20f, 0f));
                Debug.Log("Flag spawned successfully!");
            }
        }
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

    public void RegisterPickup(Pickup pickup)
    {
        pickups.Add(pickup);
    }

    public void UnregisterPickup(Pickup pickup)
    {
        pickups.Remove(pickup);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            // Load prefab
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
            string characterPath = "ScriptableObjects/Characters/Army Vet";

            // Spawn the player network object
            NetworkObject networkPlayerObject = PrefabFactory.SpawnPlayer(Runner, player, playerPrefab, respawnPoint1, characterPath, nextTeam);

            // Flip the next team so the next player to join will be on the other team
            nextTeam = (nextTeam == 1) ? 2 : 1;

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
