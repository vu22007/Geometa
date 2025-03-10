using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Runner : SimulationBehaviour
{
    private NetworkRunner runner;

    // For the game controller to use for spawning players based on lobby selections
    public Dictionary<PlayerRef, string> team1Players;
    public Dictionary<PlayerRef, string> team2Players;

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
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
}
