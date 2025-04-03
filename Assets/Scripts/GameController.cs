using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameController : NetworkBehaviour, IPlayerLeft
{
    [Networked] private bool gameOver { get; set; }
    [Networked] private float pointsTopupCooldownMax { get; set; }
    [Networked] private TickTimer pointsTopupTimer { get; set; }
    [Networked] private TickTimer gameTimer { get; set; }
    [Networked, Capacity(12)] public NetworkDictionary<PlayerRef, int> playersToTeams { get; }
    [Networked] private int pointsTeam1 { get; set; }
    [Networked] private int pointsTeam2 { get; set; }
    [Networked] private bool min2Message { get; set; }
    [Networked] private bool sec30Message { get; set; }

    [SerializeField] private Vector3 respawnPoint1;
    [SerializeField] private Vector3 respawnPoint2;
    [SerializeField] private List<Vector3> pickupLocations;
    [SerializeField] private float maxTime;

    private List<Player> players = new List<Player>();
    private List<Player> alivePlayers = new List<Player>();

    // For only the server/host to use so that it can manage the spawn and despawn of players
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // Flags
    private PickupFlag team1Flag;
    private PickupFlag team2Flag;
    
    // Initialisation
    public override void Spawned()
    {
        gameOver = false;
        pointsTopupCooldownMax = 10f;
        pointsTeam1 = 0;
        pointsTeam2 = 0;
        pointsTopupTimer = TickTimer.CreateFromSeconds(Runner, pointsTopupCooldownMax);
        gameTimer = TickTimer.CreateFromSeconds(Runner, maxTime);
        min2Message = false;
        sec30Message = false;

        //if (CoordinatesDataHolder.Instance != null)
        //{
        //    respawnPoint1 = new Vector3(CoordinatesDataHolder.Instance.respawnP1lat, CoordinatesDataHolder.Instance.respawnP1lon, 0f);
        //    respawnPoint2 = new Vector3(CoordinatesDataHolder.Instance.respawnP2lat, CoordinatesDataHolder.Instance.respawnP2lon, 0f);
        //}

        if (HasStateAuthority)
        {
            CreatePlayersToTeams();
            SpawnPlayers();
            SpawnItems();
        }
    }

    void SpawnItems()
    {
        // Spawn initial items (only the server can do this)
        if (HasStateAuthority)
        {
            SpawnPickups();
            
            // Spawn team 1 flag
            GameObject flagPrefab = Resources.Load("Prefabs/Flag") as GameObject;
            NetworkObject flag1Obj = PrefabFactory.SpawnFlag(Runner, flagPrefab, respawnPoint1 + new Vector3(0, -5, 0), 1);
            team1Flag = flag1Obj.GetComponent<PickupFlag>();

            // Spawn team 2 flag
            NetworkObject flag2Obj = PrefabFactory.SpawnFlag(Runner, flagPrefab, respawnPoint2 + new Vector3(0, 5, 0), 2);
            team2Flag = flag2Obj.GetComponent<PickupFlag>();

            // Spawn NPCs for testing
            // SpawnPlayersForTesting(3, 3, true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (gameOver) return;

        float timeLeft = gameTimer.RemainingTime(Runner).GetValueOrDefault();

        // End game if game timer has finished
        if (gameTimer.Expired(Runner))
        {
            gameOver = true;
            GoToLeaderboard();
        }

        // Topup players points by 1 every 10 seconds
        if (pointsTopupTimer.Expired(Runner))
        {
            foreach (Player player in players)
            {
                player.GainMana(1);
            }
            pointsTopupTimer = TickTimer.CreateFromSeconds(Runner, pointsTopupCooldownMax);
        }

        if (timeLeft <= 120.5f && timeLeft >= 119.5f && !min2Message)
        {
            BroadcastMessageToAll("2 Minutes left!!!", 0.3f, Color.red);
            min2Message = true;
        }

        if (timeLeft <= 30.5f && timeLeft >= 29.5f && !sec30Message)
        {
            BroadcastMessageToAll("30 Seconds left!!!", 0.3f, Color.red);
            sec30Message = true;
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

    // Create mapping between PlayerRefs and teams
    public void CreatePlayersToTeams()
    {
        if (HasStateAuthority)
        {
            NetworkManager networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();

            foreach (KeyValuePair<PlayerRef, Lobby.PlayerInfo> item in networkManager.team1Players)
                playersToTeams.Add(item.Key, 1);

            foreach (KeyValuePair<PlayerRef, Lobby.PlayerInfo> item in networkManager.team2Players)
                playersToTeams.Add(item.Key, 2);
        }
    }

    public void SpawnPlayers()
    {
        if (HasStateAuthority)
        {
            NetworkManager networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();

            // Create team 1 players
            foreach (KeyValuePair<PlayerRef, Lobby.PlayerInfo> item in networkManager.team1Players)
            {
                PlayerRef playerRef = item.Key;
                Lobby.PlayerInfo playerInfo = item.Value;
                SpawnPlayer(playerRef, (string)playerInfo.displayName, (string)playerInfo.characterName, 1);
            }

            // Create team 2 players
            foreach (KeyValuePair<PlayerRef, Lobby.PlayerInfo> item in networkManager.team2Players)
            {
                PlayerRef playerRef = item.Key;
                Lobby.PlayerInfo playerInfo = item.Value;
                SpawnPlayer(playerRef, (string)playerInfo.displayName, (string)playerInfo.characterName, 2);
            }
        }
    }

    void SpawnPlayer(PlayerRef player, string displayName, string characterName, int team)
    {
        if (HasStateAuthority)
        {
            // Load prefab
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;

            // Choose respawn point based on team
            Vector3 respawnPoint = (team == 1) ? respawnPoint1 : respawnPoint2;

            // Spawn the player network object
            NetworkObject networkPlayerObject = PrefabFactory.SpawnPlayer(Runner, player, playerPrefab, respawnPoint, displayName, characterName, team);

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

    public int CheckForPoints()
    {
        if (!HasStateAuthority) return 0;

        // Max distance for flags to be from a base to count as a win
        float maxDistance = 8.0f;

        Vector3 flag1RespawnPoint = respawnPoint1 + new Vector3(0, 3, 0);
        Vector3 flag2RespawnPoint = respawnPoint2 + new Vector3(0, 3, 0);

        // If team 2's flag is close enough to team 1's base, then team 1 wins
        if (Vector2.Distance(team2Flag.transform.position, respawnPoint1) <= maxDistance)
        {
            pointsTeam1 += 10;
            team2Flag.transform.position = flag2RespawnPoint;
            return pointsTeam1;
        }
        // If team 1's flag is close enough to team 2's base, then team 2 wins
        else if (Vector2.Distance(team1Flag.transform.position, respawnPoint2) <= maxDistance)
        {
            pointsTeam2 += 10;
            team1Flag.transform.position = flag1RespawnPoint;
            return pointsTeam2;
        }

        return 0;
    }

    public int GetTeamPoints(int team)
    {
        if (team == 1)
        {
            return pointsTeam1;
        }
        else if (team == 2)
        {
            return pointsTeam2;
        }

        return 0;
    }

    public void AddKillPoints(int team)
    {
        if (team == 1)
        {
            pointsTeam1 += 2;
        }
        else if (team == 2)
        {
            pointsTeam2 += 2;
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

    void SpawnPlayersForTesting(int allies, int enemies, bool testing)
    {
        GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
        string characterName = "Knight";

        for (int i = 0; i < allies; i++)
        {
            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1 + new Vector3(0f, 5f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
            {
                // Initialise the player (this is called before the player is spawned)
                Player player = networkObject.GetComponent<Player>();
                player.OnCreated("NPC", characterName, respawnPoint1, 1);
            });
        }

        Vector3 enemyRespawnPoint;

        for (int i = 0; i < enemies; i++)
        {
            if (testing)
            {
                enemyRespawnPoint = respawnPoint1;
            } else
            {
                enemyRespawnPoint = respawnPoint2;
            }
                // Spawn the player network object
                NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, enemyRespawnPoint + new Vector3(2f, 2f, 0f) * i, Quaternion.identity, null, (runner, networkObject) =>
                {
                    // Initialise the player (this is called before the player is spawned)
                    Player player = networkObject.GetComponent<Player>();
                    player.OnCreated("NPC", characterName, respawnPoint1, 2);
                });
        }
    }

    void SpawnPickups()
    {
        GameObject pickupPrefab = Resources.Load("Prefabs/Pickup") as GameObject;
        int pickupType = 0;

        foreach (Vector3 location in pickupLocations)
        {
            int value = 0;

            switch (pickupType)
            {
                //health
                case 0:
                    value = 20;
                    break;
                //mana
                case 1:
                    value = 10;
                    break;
                //speed
                case 2:
                    value = 5;
                    break;
                default:
                    break;
            }

            PrefabFactory.SpawnPickup(Runner, pickupPrefab, location, pickupType, value);
            
            pickupType ++;
            if(pickupType > 2){
                pickupType = 0;
            }
        }
    }

    void GoToLeaderboard()
    {
        if (!HasStateAuthority) return;

        // Give team and player stats to network manager for leaderboard to use
        NetworkManager networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();
        networkManager.team1PlayerStats = GetPlayerStats(1);
        networkManager.team2PlayerStats = GetPlayerStats(2);
        networkManager.team1Points = pointsTeam1;
        networkManager.team2Points = pointsTeam2;

        // Switch to leaderboard scene, and the leaderboard will fetch the player stats from the network manager
        Runner.LoadScene(SceneRef.FromIndex(4));
    }

    List<Leaderboard.PlayerInfo> GetPlayerStats(int team)
    {
        List<Leaderboard.PlayerInfo> playerList = new List<Leaderboard.PlayerInfo>();

        foreach (Player player in players)
        {
            if (player.GetTeam() == team)
            {
                Leaderboard.PlayerInfo playerInfo = new Leaderboard.PlayerInfo(player);
                playerList.Add(playerInfo);
            }
        }

        return playerList;
    }

    public float GetTimeLeft()
    {
        return gameTimer.RemainingTime(Runner).GetValueOrDefault();
    }
}
