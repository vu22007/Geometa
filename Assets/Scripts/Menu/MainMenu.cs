using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class MainMenu : MonoBehaviour
{
    // Store error message as static variable so that it persists scene reloads
    // Note: Fusion will reload main menu scene when fail to host or join game, so this is necessary
    static string errorMessage = "";

    private Runner runner;
    private string sessionName;

    public void OnEnable() {
        runner = GameObject.Find("Network Runner").GetComponent<Runner>();
        UpdateErrorMessage(errorMessage);
    }

    public void OnSessionNameChanged(string newSessionName) {
        sessionName = newSessionName;
    }

    async public void HostGame()
    {
        DisableMenu();
        UpdateErrorMessage(""); // Clear error message
        StartGameResult result = await runner.StartGame(GameMode.Host, sessionName);
        errorMessage = result.ErrorMessage;
    }

    async public void JoinGame()
    {
        DisableMenu();
        UpdateErrorMessage(""); // Clear error message
        StartGameResult result = await runner.StartGame(GameMode.Client, sessionName);
        errorMessage = result.ErrorMessage;
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

    private void UpdateErrorMessage(string errorMessage)
    {
        TextMeshProUGUI errorMessageText = GameObject.Find("ErrorMessage").GetComponent<TextMeshProUGUI>();

        // If error message contains error code, remove error code starting from '(' at end of string 
        if (errorMessage.IndexOf('(') >= 0)
            errorMessage = errorMessage.Substring(0, errorMessage.IndexOf('(')).TrimEnd();

        // If error message is "Ok" then there is no error, so show nothing
        if (errorMessage == "Ok")
            errorMessageText.text = "";

        // If the string matches the below, then convert it into a clearer message
        else if (errorMessage == "Game closed")
            errorMessageText.text = "Game has already started";

        // Just show the message as it is if string does not match any of the above
        else
            errorMessageText.text = errorMessage;
    }
}
