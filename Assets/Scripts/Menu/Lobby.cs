using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam1PlayersChanged))] private NetworkDictionary<PlayerRef, string> team1Players { get; }
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam2PlayersChanged))] private NetworkDictionary<PlayerRef, string> team2Players { get; }

    private GameController gameController;
    private Button team1Button;
    private Button team2Button;
    private Button knightButton;
    private Button wizardButton;
    private Button readyButton;
    private Button startGameButton;
    private GameObject team1List;
    private GameObject team2List;
    private TextMeshProUGUI playerCounter;
    private TextMeshProUGUI readyCounter;
    private GameObject playerCardPrefab;
    private int team = 0;
    private string characterName = "";
    private bool playerIsReady = false;

    public override void Spawned()
    {
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
        team1Button = GameObject.Find("Team 1 Button").GetComponent<Button>();
        team2Button = GameObject.Find("Team 2 Button").GetComponent<Button>();
        knightButton = GameObject.Find("Knight Button").GetComponent<Button>();
        wizardButton = GameObject.Find("Wizard Button").GetComponent<Button>();
        readyButton = GameObject.Find("Ready Button").GetComponent<Button>();
        readyButton.interactable = false;
        startGameButton = GameObject.Find("Start Game Button").GetComponent<Button>();
        startGameButton.interactable = false;
        team1List = GameObject.Find("Team 1 List");
        team2List = GameObject.Find("Team 2 List");
        playerCounter = GameObject.Find("Player Counter").GetComponent<TextMeshProUGUI>();
        readyCounter = GameObject.Find("Ready Counter").GetComponent<TextMeshProUGUI>();
        playerCardPrefab = Resources.Load("Prefabs/Lobby/PlayerCard") as GameObject;

        // Hide start game button if not the host
        if (!HasStateAuthority)
            startGameButton.gameObject.SetActive(false);

        // Populate team lists
        AddPlayerCardsToTeamList(team1List, team1Players);
        AddPlayerCardsToTeamList(team2List, team2Players);
    }

    public void Update()
    {
        // Make ready button interactable once team and character have been selected
        if (!playerIsReady && team != 0 && characterName != "")
            readyButton.interactable = true;

        if (Runner != null)
        {
            // Get number of players in lobby and number of players who are ready
            int numPlayersInLobby = Runner.ActivePlayers.Count();
            int numPlayersReady = team1Players.Count() + team2Players.Count();

            // Update player and ready counters
            UpdatePlayerCounter(numPlayersInLobby);
            UpdateReadyCounter(numPlayersReady);

            // Enable start game button if all players in lobby are ready, else disable it
            bool allPlayersAreReady = numPlayersReady == numPlayersInLobby;
            startGameButton.interactable = allPlayersAreReady;
        }
    }

    public void SelectTeam1()
    {
        team = 1;
        team1Button.interactable = false;
        team2Button.interactable = true;
    }

    public void SelectTeam2()
    {
        team = 2;
        team1Button.interactable = true;
        team2Button.interactable = false;
    }

    public void SelectKnight()
    {
        characterName = "Knight";
        knightButton.interactable = false;
        wizardButton.interactable = true;
    }

    public void SelectWizard()
    {
        characterName = "Wizard";
        knightButton.interactable = true;
        wizardButton.interactable = false;
    }

    public void PlayerReady()
    {
        playerIsReady = true;
        readyButton.interactable = false;
        RPC_PlayerReady(team, characterName);
    }

    public void StartGame()
    {
        if (HasStateAuthority)
        {
            // TODO
            Debug.Log("Starting game...");
        }
    }

    // Anyone can call this RPC, and it will run only on the server
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_PlayerReady(int team, string characterName, RpcInfo info = default)
    {
        PlayerRef player = info.Source;

        // Add player to chosen team if they aren't already in a team
        if (!team1Players.ContainsKey(player) && !team2Players.ContainsKey(player))
        {
            var dict = (team == 1) ? team1Players : team2Players;
            dict.Add(player, characterName);
        }
    }

    // Called when the team1Players networked property changes
    void OnTeam1PlayersChanged()
    {
        Debug.Log("Team 1 players changed");

        // Clear team list
        ClearPlayerCardsFromTeamList(team1List);

        // Populate team list based on new player info
        AddPlayerCardsToTeamList(team1List, team1Players);
    }

    // Called when the team2Players networked property changes
    void OnTeam2PlayersChanged()
    {
        Debug.Log("Team 2 players changed");

        // Clear team list
        ClearPlayerCardsFromTeamList(team2List);

        // Populate team list based on new player info
        AddPlayerCardsToTeamList(team2List, team2Players);
    }

    void UpdatePlayerCounter(int numPlayersInLobby)
    {
        playerCounter.text = "Players in lobby: " + numPlayersInLobby;
    }

    void UpdateReadyCounter(int numPlayersReady)
    {
        readyCounter.text = "Players ready: " + numPlayersReady;
    }

    void ClearPlayerCardsFromTeamList(GameObject teamList)
    {
        foreach (Transform child in teamList.transform)
        {
            if (child.name.Contains("Player Card"))
            {
                Destroy(child);
            }
        }
    }

    void AddPlayerCardsToTeamList(GameObject teamList, NetworkDictionary<PlayerRef, string> players)
    {
        // Get card and list height
        float cardHeight = playerCardPrefab.GetComponent<RectTransform>().rect.height;
        float listHeight = teamList.GetComponent<RectTransform>().rect.height;

        // Calculate position of first card relative to the list
        float cardX = 0;
        float cardY = listHeight / 2 - cardHeight / 2;
        Vector3 cardPosition = new Vector3(cardX, cardY, 0);

        // Add each player to team list
        foreach (KeyValuePair<PlayerRef, string> item in players)
        {
            PlayerRef playerRef = item.Key;
            string characterName = item.Value;

            // Create and position the player card relative to the list
            GameObject playerCard = Instantiate(playerCardPrefab, new Vector3(0, 0, 0), Quaternion.identity, teamList.transform);
            playerCard.transform.localPosition = cardPosition;

            // Set card text to character name, with an indicator to show if the player is the client's own player
            TextMeshProUGUI cardText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            cardText.text = Runner.LocalPlayer.Equals(playerRef) ? characterName + " (You)" : characterName;

            // Set position for next card
            cardPosition.y -= cardHeight;
        }
    }
}
