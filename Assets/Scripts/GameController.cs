using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class GameController : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] Vector3 respawnPoint1;
    [SerializeField] Vector3 respawnPoint2;

    public float maxTime = 480.0f; // 8 minute games
    [HideInInspector] public float currentTime = 0.0f;

    private List<Bullet> bullets;
    private List<Player> players;
    private List<Player> alivePlayers;

    private List<Pickup> pickups;

    // For only the server to use so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // For server to use to keep track of what team to assign to a new player when they join
    int nextTeam = 1;

    // Flags
    private PickupFlag team1Flag;
    private PickupFlag team2Flag;

    // For only the server to use when broadcasting messages
    private bool gameOver = false;

    private bool spawnedItems = false;

    // Initialisation
    void Start()
    {
        bullets = new List<Bullet>();
        players = new List<Player>();
        pickups = new List<Pickup>();
        alivePlayers = new List<Player>();
    }

    void SpawnItems()
    {
        spawnedItems = true;

        // Spawn initial items (only the server can do this)
        if (Runner.IsServer)
        {
            // Spawn pickup
            GameObject pickupPrefab = Resources.Load("Prefabs/Pickup") as GameObject;
            PrefabFactory.SpawnPickup(Runner, pickupPrefab, new Vector3(5f, 5f, 0f), 0, 20);
            
            // Spawn team 1 flag
            GameObject flagPrefab = Resources.Load("Prefabs/Flag") as GameObject;
            NetworkObject flag1Obj = PrefabFactory.SpawnFlag(Runner, flagPrefab, respawnPoint1 + new Vector3(0, -5, 0), 1);
            team1Flag = flag1Obj.GetComponent<PickupFlag>();

            // Spawn team 2 flag
            NetworkObject flag2Obj = PrefabFactory.SpawnFlag(Runner, flagPrefab, respawnPoint2 + new Vector3(0, 5, 0), 2);
            team2Flag = flag2Obj.GetComponent<PickupFlag>();

            // Spawn NPCs for testing - WORKS IF YOU PLAY AS A HOST :)
            SpawnPlayersForTesting(3, 3);
        }
    }

    // Update for every server simulation tick
    public override void FixedUpdateNetwork()
    {
        if (!spawnedItems) SpawnItems();

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
        RegisterAlivePlayer(player);
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

    public void RegisterAlivePlayer(Player player)
    {
        alivePlayers.Add(player);
    }

    public void UnregisterAlivePlayer(Player player)
    {
        alivePlayers.Remove(player);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            // Load prefab
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
            string characterPath = "ScriptableObjects/Characters/Knight";

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

    public List<Player> GetAlivePlayers()
    {
        return alivePlayers;
    }

    // Check if two flags are near each other
    public void CheckForWinCondition()
    {
        if (!Runner.IsServer) return;

        // Max distance for flags to be from a base to count as a win
        float maxDistance = 8.0f;

        // If team 2's flag is close enough to team 1's base, then team 1 wins
        if (Vector2.Distance(team2Flag.transform.position, respawnPoint1) <= maxDistance)
        {
            gameOver = true;
            BroadcastMessageToTeam(1, "You win!", 0.1f, Color.green);
            BroadcastMessageToTeam(2, "You lose!", 0.1f, Color.red);
        }
        // If team 1's flag is close enough to team 2's base, then team 2 wins
        else if (Vector2.Distance(team1Flag.transform.position, respawnPoint2) <= maxDistance)
        {
            gameOver = true;
            BroadcastMessageToTeam(2, "You win!", 0.1f, Color.green);
            BroadcastMessageToTeam(1, "You lose!", 0.1f, Color.red);
        }
    }

    public void BroadcastCarryFlag(int playerTeam, int flagTeam)
    {
        if (!Runner.IsServer || gameOver) return;

        int otherTeam = playerTeam == 1 ? 2 : 1;
        if (flagTeam == playerTeam)
        {
            BroadcastMessageToTeam(playerTeam, "Your team has taken back its flag!", 0.1f, Color.green);
            BroadcastMessageToTeam(otherTeam, "The enemy has taken back their flag!", 0.1f, Color.red);
        }
        else
        {
            BroadcastMessageToTeam(playerTeam, "Your team has captured the enemy's flag!", 0.1f, Color.green);
            BroadcastMessageToTeam(otherTeam, "The enemy has captured your team's flag!", 0.1f, Color.red);
        }
    }

    public void BroadcastDropFlag(int playerTeam, int flagTeam)
    {
        if (!Runner.IsServer || gameOver) return;

        int otherTeam = playerTeam == 1 ? 2 : 1;
        if (flagTeam == playerTeam)
        {
            BroadcastMessageToTeam(playerTeam, "Your team has dropped its flag!", 0.1f, Color.white);
            BroadcastMessageToTeam(otherTeam, "The enemy has dropped their flag!", 0.1f, Color.white);
        }
        else
        {
            BroadcastMessageToTeam(playerTeam, "Your team has dropped the enemy's flag!", 0.1f, Color.white);
            BroadcastMessageToTeam(otherTeam, "The enemy has dropped your team's flag!", 0.1f, Color.white);
        }
    }

    public void BroadcastMessageToAll(string message, float speed, Color color)
    {
        if (!Runner.IsServer) return;

        foreach (Player player in players)
        {
            player.RPC_ShowMessage(message, speed, color);
        }
    }

    public void BroadcastMessageToTeam(int team, string message, float speed, Color color)
    {
        if (!Runner.IsServer) return;

        foreach (Player player in players)
        {
            if (player.GetTeam() == team){
                player.RPC_ShowMessage(message, speed, color);
            }
        }
    }

    void SpawnPlayersForTesting(int allies, int enemies)
    {
        GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
        string characterPath = "ScriptableObjects/Characters/Knight";

        for (int i = 0; i < allies; i++)
        {
            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1 + new Vector3(0f, 5f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player player = networkObject.GetComponent<Player>();
                player.OnCreated(characterPath, respawnPoint1, 1);
            });
        }

        for (int i = 0; i < enemies; i++)
        {
            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1 + new Vector3(2f, 2f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player player = networkObject.GetComponent<Player>();
                player.OnCreated(characterPath, respawnPoint1, 2);
            });
        }

    }
}
