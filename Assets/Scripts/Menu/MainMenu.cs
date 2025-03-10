using UnityEngine;
using Fusion;

public class MainMenu : MonoBehaviour
{
    private Runner runner;
    private string sessionName;

    public void OnEnable() {
        runner = GameObject.Find("Network Runner").GetComponent<Runner>();
    }

    public void OnSessionNameChanged(string newSessionName) {
        sessionName = newSessionName;
    }

    public void HostGame()
    {
        Debug.Log($"Hosting game with session name '{sessionName}'...");
        runner.StartGame(GameMode.Host, sessionName);
    }

    public void JoinGame()
    {
        Debug.Log($"Joining game with session name '{sessionName}'...");
        runner.StartGame(GameMode.Client, sessionName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
