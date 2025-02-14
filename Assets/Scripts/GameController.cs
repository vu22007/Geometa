using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameController : SimulationBehaviour, IPlayerJoined, IPlayerLeft, ISceneLoadStart
{
    [SerializeField] Vector3 respawnPoint1;
    [SerializeField] Vector3 respawnPoint2;

    public float maxTime = 480.0f; // 8 minute games
    [HideInInspector] public float currentTime = 0.0f;

    private List<Bullet> bullets;
    private List<Player> players;

    private List<Pickup> pickups;

    // For server to use only so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // For server to use to keep track of what team to assign to a new player when they join
    int nextTeam = 1;

    // Flags
    private PickupFlag team1Flag;
    private PickupFlag team2Flag;

    // Initialisation
    void Start()
    {
        bullets = new List<Bullet>();
        players = new List<Player>();
        pickups = new List<Pickup>();
    }

    // Scene initialisation
    public void SceneLoadStart(SceneRef sceneRef)
    {
        // Spawn initial items (only the server can do this)
        if (Runner.IsServer)
        {
            // Spawn pickup
            GameObject pickupPrefab = Resources.Load("Prefabs/Pickup") as GameObject;
            PrefabFactory.SpawnPickup(Runner, pickupPrefab, new Vector3(5f, 5f, 0f), 0, 20);
            
            // Spawn team 1 flag
            GameObject flag1Prefab = Resources.Load("Prefabs/Flag1") as GameObject;
            NetworkObject flag1Obj = PrefabFactory.SpawnFlag(Runner, flag1Prefab, respawnPoint1 + new Vector3(0, -5, 0), 1);
            team1Flag = flag1Obj.GetComponent<PickupFlag>();

            // Spawn team 2 flag
            GameObject flag2Prefab = Resources.Load("Prefabs/Flag2") as GameObject;
            NetworkObject flag2Obj = PrefabFactory.SpawnFlag(Runner, flag2Prefab, respawnPoint2 + new Vector3(0, 5, 0), 2);
            team2Flag = flag2Obj.GetComponent<PickupFlag>();
        }
    }

    // Update for every server simulation tick
    public override void FixedUpdateNetwork()
    {
        currentTime = Runner.Tick * Runner.DeltaTime;
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

                if (Runner.IsServer)
                {
                    // Despawn bullet from network (only the server can do this)
                    Runner.Despawn(bullet.GetComponent<NetworkObject>());
                }
                else
                {
                    // Disable the bullet locally so that it isn't frozen while the client waits for the server to despawn it
                    bullet.gameObject.SetActive(false);
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
            int team = nextTeam;
            Vector3 respawnPoint = (team == 1) ? respawnPoint1 : respawnPoint2;
            NetworkObject networkPlayerObject = PrefabFactory.SpawnPlayer(Runner, player, playerPrefab, respawnPoint, characterPath, team);

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

    // Check if two flags are near each other
    public void CheckForWinCondition()
    {
        if (Runner.IsServer)
        {
            // Max distance for flags to be from a base to count as a win
            float maxDistance = 8.0f;

            // If team 2's flag is close enough to team 1's base, then team 1 wins
            if (Vector2.Distance(team2Flag.transform.position, respawnPoint1) <= maxDistance)
            {
                Debug.Log("Team 1 wins!");
            }
            // If team 1's flag is close enough to team 2's base, then team 2 wins
            else if (Vector2.Distance(team1Flag.transform.position, respawnPoint2) <= maxDistance)
            {
                Debug.Log("Team 2 wins!");
            }
        }
    }

}
