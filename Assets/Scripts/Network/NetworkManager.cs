using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : SimulationBehaviour, INetworkRunnerCallbacks
{
    // Store shutdown reason as static variable so that it persists network runner reloads
    // Note: Fusion will reload the main menu scene when fail to host or join game, which means
    // that the network runner is reloaded, so static is necessary for persistence
    public static ShutdownReason shutdownReason;

    private NetworkRunner runner;

    // For the game controller to use for spawning players based on lobby selections
    public Dictionary<PlayerRef, Lobby.PlayerInfo> team1Players;
    public Dictionary<PlayerRef, Lobby.PlayerInfo> team2Players;

    // For the leaderboard to use for showing player stats
    public List<Leaderboard.PlayerInfo> team1PlayerStats;
    public List<Leaderboard.PlayerInfo> team2PlayerStats;

    // For the leaderboard to use to know who the host is
    public PlayerRef hostPlayerRef;

    async public void StartGame(GameMode mode, string sessionName)
    {
        runner = gameObject.GetComponent<NetworkRunner>();
        runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the next scene (the lobby scene)
        SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex + 1);
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            PlayerCount = 12,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // Set static variable for shutdown reason (for main menu to use for error message)
        NetworkManager.shutdownReason = shutdownReason;

        // Go to main menu if the network runner is shutdown
        SceneManager.LoadScene(0);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
