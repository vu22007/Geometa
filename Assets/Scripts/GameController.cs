using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameController : NetworkBehaviour, IPlayerLeft
{
    [Networked, HideInInspector] public float currentTime { get; set; }
    [Networked] private float pointsTopupCooldownMax { get; set; }
    [Networked] private float pointsTopupCooldownCurrent { get; set; }
    [Networked] private int gameStartTick { get; set; }

    [SerializeField] private Vector3 respawnPoint1;
    [SerializeField] private Vector3 respawnPoint2;
    [SerializeField] private float maxTime;

    private List<Player> players;
    private List<Player> alivePlayers;

    // For only the server/host to use so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // For only the server/host to use when broadcasting messages
    private bool gameOver = false;

    // Flags
    private PickupFlag team1Flag;
    private PickupFlag team2Flag;

    // Initialisation
    public override void Spawned()
    {
        players = new List<Player>();
        alivePlayers = new List<Player>();
        currentTime = 0.0f;
        pointsTopupCooldownMax = 10f;
        pointsTopupCooldownCurrent = pointsTopupCooldownMax;
        gameStartTick = Runner.Tick;

        if (HasStateAuthority)
        {
            SpawnPlayers();
            SpawnItems();
        }
    }

    void SpawnItems()
    {
        // Spawn initial items (only the server can do this)
        if (HasStateAuthority)
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

            // Spawn NPCs for testing
            SpawnPlayersForTesting(3, 3);
        }
    }

    // Update for every server simulation tick
    public override void FixedUpdateNetwork()
    {
        currentTime = (Runner.Tick - gameStartTick) * Runner.DeltaTime;

        if (currentTime >= maxTime)
        {
            //end game
        }

        // Topup players points by 5 every 10 seconds
        pointsTopupCooldownCurrent -= Runner.DeltaTime;
        if (pointsTopupCooldownCurrent < 0){
            foreach (Player player in players)
            {
                player.GainPoints(5);
            }
            pointsTopupCooldownCurrent = pointsTopupCooldownMax;
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
        UnregisterAlivePlayer(player);
    }

    public void RegisterAlivePlayer(Player player)
    {
        alivePlayers.Add(player);
    }

    public void UnregisterAlivePlayer(Player player)
    {
        alivePlayers.Remove(player);
    }

    public void SpawnPlayers()
    {
        if (HasStateAuthority)
        {
            NetworkManager networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();

            // Create team 1 players
            foreach (KeyValuePair<PlayerRef, string> item in networkManager.team1Players)
            {
                PlayerRef playerRef = item.Key;
                string characterName = item.Value;
                SpawnPlayer(playerRef, characterName, 1);
            }

            // Create team 2 players
            foreach (KeyValuePair<PlayerRef, string> item in networkManager.team2Players)
            {
                PlayerRef playerRef = item.Key;
                string characterName = item.Value;
                SpawnPlayer(playerRef, characterName, 2);
            }
        }
    }

    void SpawnPlayer(PlayerRef player, string characterName, int team)
    {
        if (HasStateAuthority)
        {
            // Load prefab
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;

            // Choose respawn point based on team
            Vector3 respawnPoint = (team == 1) ? respawnPoint1 : respawnPoint2;

            // Spawn the player network object
            NetworkObject networkPlayerObject = PrefabFactory.SpawnPlayer(Runner, player, playerPrefab, respawnPoint, characterName, team);

            // Add player network object to dictionary
            spawnedPlayers.Add(player, networkPlayerObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Run the following only on the server
        if (HasStateAuthority)
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
        if (!HasStateAuthority) return;

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
        if (!HasStateAuthority || gameOver) return;

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
        if (!HasStateAuthority || gameOver) return;

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
        if (!HasStateAuthority) return;

        foreach (Player player in players)
        {
            player.RPC_ShowMessage(message, speed, color);
        }
    }

    public void BroadcastMessageToTeam(int team, string message, float speed, Color color)
    {
        if (!HasStateAuthority) return;

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
        string characterName = "Wizard";

        for (int i = 0; i < allies; i++)
        {
            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1 + new Vector3(0f, 5f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player player = networkObject.GetComponent<Player>();
                player.OnCreated(characterName, respawnPoint1, 1);
            });
        }

        for (int i = 0; i < enemies; i++)
        {
            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint2 + new Vector3(2f, 2f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player player = networkObject.GetComponent<Player>();
                player.OnCreated(characterName, respawnPoint1, 2);
            });
        }
    }

    public float GetMaxTime()
    {
        return maxTime;
    }
}
