using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class Leaderboard : NetworkBehaviour
{
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam1PlayerStatsChanged))] private NetworkLinkedList<PlayerInfo> team1PlayerStats { get; }
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam2PlayerStatsChanged))] private NetworkLinkedList<PlayerInfo> team2PlayerStats { get; }
    [Networked, OnChangedRender(nameof(OnTeam1PointsChanged))] float team1Points { get; set; }
    [Networked, OnChangedRender(nameof(OnTeam2PointsChanged))] float team2Points { get; set; }
    [Networked] PlayerRef hostPlayerRef { get; set; }

    private NetworkManager networkManager;
    private GameObject team1List;
    private GameObject team2List;
    private TextMeshProUGUI team1Title;
    private TextMeshProUGUI team2Title;
    private TextMeshProUGUI team1PointsText;
    private TextMeshProUGUI team2PointsText;
    private TextMeshProUGUI teamWinner;
    private GameObject playerCardPrefab;

    public override void Spawned()
    {
        networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();
        team1List = GameObject.Find("Team 1 List");
        team2List = GameObject.Find("Team 2 List");
        team1Title = GameObject.Find("Team 1 Title").GetComponent<TextMeshProUGUI>();
        team2Title = GameObject.Find("Team 2 Title").GetComponent<TextMeshProUGUI>();
        team1PointsText = GameObject.Find("Team 1 Points").GetComponent<TextMeshProUGUI>();
        team2PointsText = GameObject.Find("Team 2 Points").GetComponent<TextMeshProUGUI>();
        teamWinner = GameObject.Find("Team Winner").GetComponent<TextMeshProUGUI>();
        playerCardPrefab = Resources.Load("Prefabs/Leaderboard/PlayerCard") as GameObject;

        // If this is the host, get the player stats from the network manager and set the networked
        // properties so all clients can show them
        if (HasStateAuthority)
        {
            // Set team 1 player stats
            var team1Players = team1PlayerStats;
            foreach (PlayerInfo player in networkManager.team1PlayerStats)
            {
                team1Players.Add(player);
            }

            // Set team 2 player stats
            var team2Players = team2PlayerStats;
            foreach (PlayerInfo player in networkManager.team2PlayerStats)
            {
                team2Players.Add(player);
            }
            
            // Set points for both teams
            team1Points = networkManager.team1Points;
            team2Points = networkManager.team2Points;

            // Set host player
            hostPlayerRef = networkManager.hostPlayerRef;
        }

        ShowTeamPointsAndWinner();

        // Populate team lists
        AddPlayerCardsToTeamList(team1List, team1PlayerStats);
        AddPlayerCardsToTeamList(team2List, team2PlayerStats);
    }

    public void LeaveLeaderboard()
    {
        // Shut down the network runner, which will cause the game to return to the main menu
        Runner.Shutdown();
    }
    
    // Called when the team1Points networked property changes
    void OnTeam1PointsChanged()
    {
        ShowTeamPointsAndWinner();
    }
    
    // Called when the team2Points networked property changes
    void OnTeam2PointsChanged()
    {
        ShowTeamPointsAndWinner();
    }

    void ShowTeamPointsAndWinner()
    {
        // Show points
        team1PointsText.text = "Total points: " + team1Points;
        team2PointsText.text = "Total points: " + team2Points;

        // Show winner
        if (team1Points > team2Points)
        {
            teamWinner.text = "Team 1 wins!";
            team1Title.color = Color.yellow;
        }
        else if (team2Points > team1Points)
        {
            teamWinner.text = "Team 2 wins!";
            team2Title.color = Color.yellow;
        }
        else
        {
            teamWinner.text = "Draw!";
        }
    }

    // Called when the team1PlayerStats networked property changes
    void OnTeam1PlayerStatsChanged()
    {
        // Clear team list
        ClearPlayerCardsFromTeamList(team1List);

        // Populate team list based on new player info
        AddPlayerCardsToTeamList(team1List, team1PlayerStats);
    }

    // Called when the team2PlayerStats networked property changes
    void OnTeam2PlayerStatsChanged()
    {
        // Clear team list
        ClearPlayerCardsFromTeamList(team2List);

        // Populate team list based on new player info
        AddPlayerCardsToTeamList(team2List, team2PlayerStats);
    }

    void ClearPlayerCardsFromTeamList(GameObject teamList)
    {
        foreach (Transform child in teamList.transform)
        {
            if (child.name.Contains("PlayerCard"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    void AddPlayerCardsToTeamList(GameObject teamList, NetworkLinkedList<PlayerInfo> players)
    {
        // Get card and list height
        float cardHeight = playerCardPrefab.GetComponent<RectTransform>().rect.height;
        float listHeight = teamList.GetComponent<RectTransform>().rect.height;

        // Calculate position of first card relative to the list
        float cardX = 0;
        float cardY = listHeight / 2 - cardHeight / 2;
        Vector3 cardPosition = new Vector3(cardX, cardY, 0);

        // Add each player to team list
        foreach (PlayerInfo playerInfo in players)
        {
            PlayerRef playerRef = playerInfo.playerRef;
            string displayname = (string)playerInfo.displayName;
            string characterName = (string)playerInfo.characterName;
            int kills = playerInfo.totalKills;
            int deaths = playerInfo.totalDeaths;
            float killDeathRatio = (float)kills / deaths;
            float damage = playerInfo.totalDamageDealt;
            int flags = playerInfo.totalFlagsCaptured;

            // Create and position the player card relative to the list
            GameObject playerCard = Instantiate(playerCardPrefab, new Vector3(0, 0, 0), Quaternion.identity, teamList.transform);
            playerCard.transform.localPosition = cardPosition;

            // Set display name, with green colour to show if the player is the client's own player
            TextMeshProUGUI displayNameText = playerCard.transform.Find("Display Name").GetComponent<TextMeshProUGUI>();
            displayNameText.text = displayname;
            displayNameText.color = Runner.LocalPlayer.Equals(playerRef) ? Color.green : Color.white;

            // Set card character image based on chosen character
            GameObject knightImage = playerCard.transform.Find("Knight Image").gameObject;
            GameObject wizardImage = playerCard.transform.Find("Wizard Image").gameObject;
            if (characterName == "Knight") wizardImage.SetActive(false);
            else knightImage.SetActive(false);

            // Puts a nice star next to the host
            GameObject starImage = playerCard.transform.Find("Star Image").gameObject;
            if (hostPlayerRef.Equals(playerRef)) starImage.SetActive(true);

            // Get stat text components
            TextMeshProUGUI killsText = playerCard.transform.Find("Kills").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI deathsText = playerCard.transform.Find("Deaths").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI killDeathRatioText = playerCard.transform.Find("KD").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI damageText = playerCard.transform.Find("Damage").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI flagsText = playerCard.transform.Find("Flags").GetComponent<TextMeshProUGUI>();

            // Set player stats
            killsText.text = kills.ToString();
            deathsText.text = deaths.ToString();
            killDeathRatioText.text = (deaths == 0) ? "-" : killDeathRatio.ToString("0.00");
            damageText.text = Mathf.RoundToInt(damage).ToString();
            flagsText.text = flags.ToString();

            // Make stats green colour if player is the client's own player
            if (Runner.LocalPlayer.Equals(playerRef))
            {
                killsText.color = Color.green;
                deathsText.color = Color.green;
                killDeathRatioText.color = Color.green;
                damageText.color = Color.green;
                flagsText.color = Color.green;
            }

            // Set position for next card
            cardPosition.y -= cardHeight;
        }
    }

    public struct PlayerInfo : INetworkStruct
    {
        public PlayerRef playerRef;
        public NetworkString<_16> displayName;
        public int team;
        public NetworkString<_16> characterName;
        public int totalKills;
        public int totalDeaths;
        public float totalDamageDealt;
        public int totalFlagsCaptured;

        public PlayerInfo(Player player)
        {
            playerRef = player.Object.InputAuthority;
            displayName = player.GetDisplayName();
            team = player.GetTeam();
            characterName = player.GetCharacterName();
            totalKills = player.GetTotalKills();
            totalDeaths = player.GetTotalDeaths();
            totalDamageDealt = player.GetTotalDamageDealt();
            totalFlagsCaptured = player.GetTotalFlagsCaptured();
        }
    }
}
