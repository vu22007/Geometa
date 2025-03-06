using UnityEngine;
using Fusion;

public class MainMenu : MonoBehaviour
{
    private GameController gameController;
    private string sessionName;

    public void OnEnable() {
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
    }

    public void OnSessionNameChanged(string newSessionName) {
        sessionName = newSessionName;
    }

    public void HostGame()
    {
        Debug.Log($"Hosting game with session name '{sessionName}'...");
        gameController.StartGame(GameMode.Host, sessionName);
    }

    public void JoinGame()
    {
        Debug.Log($"Joining game with session name '{sessionName}'...");
        gameController.StartGame(GameMode.Client, sessionName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
