using System;
using Fusion;
using UnityEngine;
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
    private int team = 0;
    private string characterName = "";

    public void OnEnable()
    {
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
        team1Button = GameObject.Find("Team 1 Button").GetComponent<Button>();
        team2Button = GameObject.Find("Team 2 Button").GetComponent<Button>();
        knightButton = GameObject.Find("Knight Button").GetComponent<Button>();
        wizardButton = GameObject.Find("Wizard Button").GetComponent<Button>();
        readyButton = GameObject.Find("Ready Button").GetComponent<Button>();
        readyButton.interactable = false;
    }

    public void Update()
    {
        // Make ready button interactable once team and character have been selected
        if (team != 0 && characterName != "")
        {
            readyButton.interactable = true;
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
        Debug.Log("Calling RPC...");
        RPC_PlayerReady(team, characterName);
    }

    // Anyone can call this RPC, and it will run only on the server
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPC_PlayerReady(int team, string characterName, RpcInfo info = default)
    {
        PlayerRef player = info.Source;

        var dict = (team == 1) ? team1Players : team2Players;
        dict.Add(player, characterName);
    }

    // Called when the team1Players networked property changes
    void OnTeam1PlayersChanged()
    {
        Debug.Log("Team 1 players changed");
    }

    // Called when the team2Players networked property changes
    void OnTeam2PlayersChanged()
    {
        Debug.Log("Team 2 players changed");
    }
}
