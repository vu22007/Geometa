using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class Lobby : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam1PlayersChanged))] private NetworkDictionary<PlayerRef, PlayerInfo> team1Players { get; }
    [Networked, Capacity(6), OnChangedRender(nameof(OnTeam2PlayersChanged))] private NetworkDictionary<PlayerRef, PlayerInfo> team2Players { get; }
    [Networked] PlayerRef hostPlayerRef { get; set; }

    [SerializeField] GameObject buildingsGeneratorPrefab;
    private NetworkManager networkManager;
    private TMP_InputField displayNameInput;
    private TMP_InputField coordinatesInput;
    [Networked] NetworkString<_64> coordinates { get; set; }
    // Corrdinates used for generating the map, so players joined after can generate it as well
    [Networked] NetworkString<_64> mapGenerationCoordinates { get; set; }
    private Button team1Button;
    private Button team2Button;
    private Button knightButton;
    private Button wizardButton;
    private Button readyButton;
    private Button generateMapButton;
    private Button startGameButton;
    private Button selectAreaButton;
    private GameObject team1List;
    private GameObject team2List;
    private TextMeshProUGUI playerCounter;
    private TextMeshProUGUI readyCounter;
    private GameObject playerCardPrefab;
    private string displayName = "";
    private int team = 0;
    private string characterName = "";
    private bool playerIsReady = false;
    RunBlenderScript buildingsGenerator;
    CoordinatesDataHolder coordinatesDataHolder;
    [Networked] bool mapGenerated { get; set; } = false;
    [Networked, Capacity(16)] private NetworkLinkedList<PlayerRef> playersCompletedMapGen { get; }

    public override void Spawned()
    {
        coordinatesDataHolder = GameObject.Find("CoordinatesDataHolder").GetComponent<CoordinatesDataHolder>();
        buildingsGenerator = GameObject.Find("BuildingsGenerator").GetComponent<RunBlenderScript>();
        selectAreaButton = GameObject.Find("Select Area").GetComponent<Button>();
        networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();
        displayNameInput = GameObject.Find("Display Name Input").GetComponent<TMP_InputField>();
        coordinatesInput = GameObject.Find("Coordinates Input").GetComponent<TMP_InputField>();
        team1Button = GameObject.Find("Team 1 Button").GetComponent<Button>();
        team2Button = GameObject.Find("Team 2 Button").GetComponent<Button>();
        knightButton = GameObject.Find("Knight Button").GetComponent<Button>();
        wizardButton = GameObject.Find("Wizard Button").GetComponent<Button>();
        readyButton = GameObject.Find("Ready Button").GetComponent<Button>();
        readyButton.onClick.AddListener(PlayerReady);
        readyButton.interactable = false;
        startGameButton = GameObject.Find("Start Game Button").GetComponent<Button>();
        startGameButton.interactable = false;
        generateMapButton = GameObject.Find("Generate Map Button").GetComponent<Button>();
        team1List = GameObject.Find("Team 1 List");
        team2List = GameObject.Find("Team 2 List");
        playerCounter = GameObject.Find("Player Counter").GetComponent<TextMeshProUGUI>();
        readyCounter = GameObject.Find("Ready Counter").GetComponent<TextMeshProUGUI>();
        playerCardPrefab = Resources.Load("Prefabs/Lobby/PlayerCard") as GameObject;

        // Hide start game button if not the host
        if (!HasStateAuthority)
        {
            startGameButton.gameObject.SetActive(false);
            generateMapButton.gameObject.SetActive(false);
            selectAreaButton.gameObject.SetActive(false);
            coordinatesInput.gameObject.SetActive(false);
        }

        if (mapGenerated & Runner.IsClient)
        {
            Debug.Log("Started Generating Map");
            GenerateMap();
        }

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

    public void OnDisplayNameChanged(string newDisplayName)
    {
        // Limit name length
        int charLimit = 16;
        newDisplayName = new string(newDisplayName.Take(charLimit).ToArray());

        displayName = newDisplayName;
        displayNameInput.text = newDisplayName;
    }

    public void OnCoordinatesChanged(string coordinates)
    {
        this.coordinates = coordinates;
        coordinatesInput.text = coordinates;
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

        // Turn the ready button into an unready button
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Unready";
        readyButton.onClick.RemoveAllListeners();
        readyButton.onClick.AddListener(PlayerUnready);

        // Disable display name, team and character selection
        displayNameInput.interactable = false;
        team1Button.interactable = false;
        team2Button.interactable = false;
        knightButton.interactable = false;
        wizardButton.interactable = false;

        // Send selection to host
        RPC_PlayerReady(displayName, team, characterName);
    }

    public void PlayerUnready()
    {
        playerIsReady = false;

        // Turn the unready button into a ready button
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        readyButton.onClick.RemoveAllListeners();
        readyButton.onClick.AddListener(PlayerReady);

        // Enable display name, team and character selection, with the current selection applied
        displayNameInput.interactable = true;
        team1Button.interactable = team != 1;
        team2Button.interactable = team != 2;
        knightButton.interactable = characterName != "Knight";
        wizardButton.interactable = characterName != "Wizard";

        // Remove selection from host
        RPC_PlayerUnready();
    }

    public void SelectArea()
    {
        // Note: Might need changing for linux
        string localPathToHtmlFile = "\\Non-Unity\\mapCoordinates.html";
        string pathToHtmlFile = System.IO.Directory.GetCurrentDirectory() + localPathToHtmlFile;
        if (HasStateAuthority)
        {
            Application.OpenURL(pathToHtmlFile);
            // Debug.Log("HTML file path: " + pathToHtmlFile);
            // Debug.Log("Current directory: " + System.IO.Directory.GetCurrentDirectory());
        }
    }

    // Add this RPC method
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_MapGenComplete(PlayerRef player)
    {
        if (!playersCompletedMapGen.Contains(player))
        {
            playersCompletedMapGen.Add(player);
            Debug.Log($"Player {player} completed map gen. Total: {playersCompletedMapGen.Count()}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_GenerateMap()
    {
        mapGenerationCoordinates = coordinates;
        GenerateMap();
    }

    public void GenerateMap()
    {
        string[] partsArray = mapGenerationCoordinates.ToString().Split(',')
                         .Select(s => s.Trim())
                         .ToArray();

        bool allValid = true;
        double[] numbers = new double[partsArray.Length];

        for (int i = 0; i < partsArray.Length; i++)
        {
            if (double.TryParse(partsArray[i], out double number))
            {
                // Round to 4 decimals because blosm has that max accuracy
                numbers[i] = (double)System.Math.Round(number, 4);
            }
            else
            {
                allValid = false;
                Debug.LogError($"Invalid input in: {partsArray[i]}");
            }
        }

        Debug.Log("Map generation function inside");
        mapGenerated = true;

        if (allValid)
        {
            StartCoroutine(buildingsGenerator.RunBlender(numbers[0], numbers[1], numbers[2], numbers[3]));
            coordinatesDataHolder.SetCoordinates(numbers[0], numbers[1], numbers[2], numbers[3]);
        }
        else
        {
            Debug.LogError($"Invalid input for coordinates: {mapGenerationCoordinates}");
        }
    }

    public void StartGame()
    {
        if (HasStateAuthority)
        {
            Debug.Log("Game started");
            // Give the network manager the player dictionaries, but first convert the networked ones to standard ones
            networkManager.team1Players = ConvertFromNetworkDictionary(team1Players);
            networkManager.team2Players = ConvertFromNetworkDictionary(team2Players);

            if (mapGenerated)
            {
                // Check if all active players have completed generation
                bool allPlayersReady = Runner.ActivePlayers.All(p => playersCompletedMapGen.Contains(p));

                foreach (var item in Runner.ActivePlayers)
                {
                    if (!playersCompletedMapGen.Contains(item))
                    {
                        Debug.Log("Hasn't generated map: " + item);
                    }
                }

                if (allPlayersReady) 
                {
                    // Prevent new players from joining
                    Runner.SessionInfo.IsOpen = false;
                    playersCompletedMapGen.Clear();
                    // Play Scene with a pre-generated map
                    DontDestroyOnLoad(coordinatesDataHolder);
                    Runner.LoadScene(SceneRef.FromIndex(5));
                }
                else
                {
                    Debug.LogError("A player hasn't finished generating the map. Players finished: " + playersCompletedMapGen.Count() + "/" + Runner.ActivePlayers.Count());
                }
            }
            else
            {
                // Prevent new players from joining
                Runner.SessionInfo.IsOpen = false;
                // Load Scene that will use the 3D Generated buildings and generate 2D map in scene
                // Switch to map scene to start the game, and the game controller will spawn player objects using the player dicts in the network manager
                Destroy(coordinatesDataHolder);
                Runner.LoadScene(SceneRef.FromIndex(3));
            }
        }
    }

    public void LeaveLobby()
    {
        // Shut down the network runner, which will cause the game to return to the main menu
        Runner.Shutdown();
    }

    // Convert a network dictionary into a standard dictionary
    Dictionary<PlayerRef, PlayerInfo> ConvertFromNetworkDictionary(NetworkDictionary<PlayerRef, PlayerInfo> networkDictionary)
    {
        Dictionary<PlayerRef, PlayerInfo> dictionary = new Dictionary<PlayerRef, PlayerInfo>();
        foreach (KeyValuePair<PlayerRef, PlayerInfo> item in networkDictionary)
        {
            dictionary.Add(item.Key, item.Value);
        }
        return dictionary;
    }

    // Anyone can call this RPC, and it will run only on the server
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_PlayerReady(string displayName, int team, string characterName, RpcInfo info = default)
    {
        PlayerRef player = info.Source;

        // Validate player details
        bool teamIsValid = team == 1 || team == 2;
        bool characterIsValid = characterName == "Knight" || characterName == "Wizard";
        if (!teamIsValid || !characterIsValid) return;

        // Use the character name as the display name if one is not provided
        if (displayName == "") displayName = characterName;

        // Add player to chosen team if they aren't already in a team
        if (!team1Players.ContainsKey(player) && !team2Players.ContainsKey(player))
        {
            PlayerInfo playerInfo = new PlayerInfo(player, displayName, team, characterName);
            var dict = (team == 1) ? team1Players : team2Players;
            dict.Add(player, playerInfo);
        }
    }

    // Anyone can call this RPC, and it will run only on the server
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_PlayerUnready(RpcInfo info = default)
    {
        PlayerRef player = info.Source;
        RemovePlayerSelection(player);
    }

    // Called when the team1Players networked property changes
    void OnTeam1PlayersChanged()
    {
        // Clear team list
        ClearPlayerCardsFromTeamList(team1List);

        // Populate team list based on new player info
        AddPlayerCardsToTeamList(team1List, team1Players);
    }

    // Called when the team2Players networked property changes
    void OnTeam2PlayersChanged()
    {
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
            if (child.name.Contains("PlayerCard"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    void AddPlayerCardsToTeamList(GameObject teamList, NetworkDictionary<PlayerRef, PlayerInfo> players)
    {
        // Get card and list height
        float cardHeight = playerCardPrefab.GetComponent<RectTransform>().rect.height;
        float listHeight = teamList.GetComponent<RectTransform>().rect.height;

        // Calculate position of first card relative to the list
        float cardX = 0;
        float cardY = listHeight / 2 - cardHeight / 2;
        Vector3 cardPosition = new Vector3(cardX, cardY, 0);

        // Add each player to team list
        foreach (KeyValuePair<PlayerRef, PlayerInfo> item in players)
        {
            PlayerRef playerRef = item.Key;
            PlayerInfo playerInfo = item.Value;
            string displayname = (string)playerInfo.displayName;
            string characterName = (string)playerInfo.characterName;

            // Create and position the player card relative to the list
            GameObject playerCard = Instantiate(playerCardPrefab, new Vector3(0, 0, 0), Quaternion.identity, teamList.transform);
            playerCard.transform.localPosition = cardPosition;

            // Set card text to display name, with green colour to show if the player is the client's own player
            TextMeshProUGUI cardText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            cardText.text = displayname;
            cardText.color = Runner.LocalPlayer.Equals(playerRef) ? Color.green : Color.white;

            // Set card character image based on chosen character
            GameObject knightImage = playerCard.transform.Find("Knight Image").gameObject;
            GameObject wizardImage = playerCard.transform.Find("Wizard Image").gameObject;
            if (characterName == "Knight") wizardImage.SetActive(false);
            else knightImage.SetActive(false);

            // Puts a nice star next to the host
            GameObject starImage = playerCard.transform.Find("Star Image").gameObject;
            if (hostPlayerRef.Equals(playerRef)) starImage.SetActive(true);

            // Set position for next card
            cardPosition.y -= cardHeight;
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Store the PlayerRef of the host
        if (HasStateAuthority && Runner.LocalPlayer.Equals(player))
        {
            hostPlayerRef = player;
            networkManager.hostPlayerRef = player;
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Remove player from both dictionaries if they leave, so that the team lists
        // update on each client to no longer show the player
        if (HasStateAuthority)
        {
            RemovePlayerSelection(player);
        }
    }

    private void RemovePlayerSelection(PlayerRef player)
    {
        if (team1Players.ContainsKey(player))
        {
            var dict = team1Players;
            dict.Remove(player);
        }
        else if (team2Players.ContainsKey(player))
        {
            var dict = team2Players;
            dict.Remove(player);
        }
    }

    public struct PlayerInfo : INetworkStruct
    {
        public PlayerRef playerRef;
        public NetworkString<_16> displayName;
        public int team;
        public NetworkString<_16> characterName;

        public PlayerInfo(PlayerRef playerRef, NetworkString<_16> displayName, int team, NetworkString<_16> characterName)
        {
            this.playerRef = playerRef;
            this.displayName = displayName;
            this.team = team;
            this.characterName = characterName;
        }
    }
}
