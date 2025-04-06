using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private NetworkManager networkManager;
    private string sessionName = "";

    public void OnEnable() {
        networkManager = GameObject.Find("Network Runner").GetComponent<NetworkManager>();

        // Update the error message using the shutdown reason provided in the network manager,
        // in case we are back to the main menu due to an error
        UpdateErrorMessage(NetworkManager.shutdownReason);
    }

    public void OnSessionNameChanged(string newSessionName) {
        sessionName = newSessionName;
    }

    public void HostGame()
    {
        DisableMenu();
        UpdateErrorMessage(ShutdownReason.Ok); // Clear error message
        networkManager.StartGame(GameMode.Host, sessionName.ToLower());
    }

    public void JoinGame()
    {
        DisableMenu();
        UpdateErrorMessage(ShutdownReason.Ok); // Clear error message
        networkManager.StartGame(GameMode.Client, sessionName.ToLower());
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

    private void UpdateErrorMessage(ShutdownReason shutdownReason)
    {
        TextMeshProUGUI errorMessage = GameObject.Find("ErrorMessage").GetComponent<TextMeshProUGUI>();

        switch (shutdownReason)
        {
            case ShutdownReason.Ok:
                errorMessage.text = ""; break;
            case ShutdownReason.AlreadyRunning:
                errorMessage.text = "Already hosting/joining a game"; break;
            case ShutdownReason.AuthenticationTicketExpired:
                errorMessage.text = "Authentication ticket expired"; break;
            case ShutdownReason.ConnectionRefused:
                errorMessage.text = "Connection refused"; break;
            case ShutdownReason.ConnectionTimeout:
                errorMessage.text = "Connection timeout"; break;
            case ShutdownReason.CustomAuthenticationFailed:
                errorMessage.text = "Custom authentication failed"; break;
            case ShutdownReason.DisconnectedByPluginLogic:
                errorMessage.text = "Host has left"; break;
            case ShutdownReason.Error:
                errorMessage.text = "Internal error"; break;
            case ShutdownReason.GameClosed:
                errorMessage.text = "Game has already started"; break;
            case ShutdownReason.GameIdAlreadyExists:
                errorMessage.text = "Game already exists"; break;
            case ShutdownReason.GameIsFull:
                errorMessage.text = "Game is full"; break;
            case ShutdownReason.GameNotFound:
                errorMessage.text = "Game does not exist"; break;
            case ShutdownReason.HostMigration:
                errorMessage.text = "Host migration about to happen"; break;
            case ShutdownReason.IncompatibleConfiguration:
                errorMessage.text = "Incompatible configuration"; break;
            case ShutdownReason.InvalidArguments:
                errorMessage.text = "Invalid arguments"; break;
            case ShutdownReason.InvalidAuthentication:
                errorMessage.text = "Invalid authentication"; break;
            case ShutdownReason.InvalidRegion:
                errorMessage.text = "Region is unavailable or non-existent"; break;
            case ShutdownReason.MaxCcuReached:
                errorMessage.text = "Max CCU has been reached"; break;
            case ShutdownReason.OperationCanceled:
                errorMessage.text = "Operation canceled"; break;
            case ShutdownReason.OperationTimeout:
                errorMessage.text = "Operation timeout"; break;
            case ShutdownReason.PhotonCloudTimeout:
                errorMessage.text = "Photon Cloud timeout"; break;
            case ShutdownReason.ServerInRoom:
                errorMessage.text = "Game already has a host"; break;
            default:
                errorMessage.text = shutdownReason.ToString(); break;
        }
    }
}
