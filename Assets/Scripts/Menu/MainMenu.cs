using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;

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
        DisableMenu();
        runner.StartGame(GameMode.Host, sessionName);
    }

    public void JoinGame()
    {
        DisableMenu();
        runner.StartGame(GameMode.Client, sessionName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void DisableMenu()
    {
        // Disable text field
        GetComponentInChildren<TMP_InputField>().interactable = false;

        // Disable host and join buttons
        GameObject.Find("HostButton").GetComponent<Button>().interactable = false;
        GameObject.Find("JoinButton").GetComponent<Button>().interactable = false;
    }
}
