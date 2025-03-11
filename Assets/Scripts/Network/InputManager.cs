using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : SimulationBehaviour, INetworkRunnerCallbacks
{
    public PlayerInputActions playerInputActions;
    private InputAction actionShoot;
    private InputAction actionReload;
    private InputAction actionPlaceShape;
    private InputAction actionTriangle;
    private InputAction actionSquare;
    private InputAction actionPentagon;
    private InputAction actionPickup;
    private InputAction actionDash;
    private InputAction actionAoE;
    private InputAction actionMenu;

    private void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInputActions();
        }

        actionShoot = playerInputActions.Player.Shoot;
        actionReload = playerInputActions.Player.Reload;
        actionPlaceShape = playerInputActions.Player.PlaceShape;
        actionTriangle = playerInputActions.Player.Triangle;
        actionSquare = playerInputActions.Player.Square;
        actionPentagon = playerInputActions.Player.Pentagon;
        actionPickup = playerInputActions.Player.Pickup;
        actionDash = playerInputActions.Player.Dash;
        actionAoE = playerInputActions.Player.AoE;
        actionMenu = playerInputActions.Player.Menu;

        actionShoot.Enable();
        actionReload.Enable();
        actionPlaceShape.Enable();
        actionTriangle.Enable();
        actionSquare.Enable();
        actionPentagon.Enable();
        actionPickup.Enable();
        actionDash.Enable();
        actionAoE.Enable();
        actionMenu.Enable();
    }

    private void OnDisable()
    {
        actionShoot.Disable();
        actionReload.Disable();
        actionPlaceShape.Disable();
        actionTriangle.Disable();
        actionSquare.Disable();
        actionPentagon.Disable();
        actionPickup.Disable();
        actionDash.Disable();
        actionAoE.Disable();
        actionMenu.Disable();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        // Set buttons
        data.buttons.Set(InputButtons.Shoot, actionShoot.IsPressed());
        data.buttons.Set(InputButtons.Reload, actionReload.IsPressed());
        data.buttons.Set(InputButtons.PlaceShape, actionPlaceShape.IsPressed());
        data.buttons.Set(InputButtons.Triangle, actionTriangle.IsPressed());
        data.buttons.Set(InputButtons.Square, actionSquare.IsPressed());
        data.buttons.Set(InputButtons.Pentagon, actionPentagon.IsPressed());
        data.buttons.Set(InputButtons.Pickup, actionPickup.IsPressed());
        data.buttons.Set(InputButtons.TakeDamage, Input.GetKey(KeyCode.J));
        data.buttons.Set(InputButtons.Dash, actionDash.IsPressed());
        data.buttons.Set(InputButtons.AoE, actionAoE.IsPressed());
        data.buttons.Set(InputButtons.Menu, actionMenu.IsPressed());

        // Set movement direction vector
        float speedX = Input.GetAxisRaw("Horizontal");
        float speedY = Input.GetAxisRaw("Vertical");
        data.moveDirection = new Vector2(speedX, speedY).normalized;

        if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject networkPlayerObject))
        {
            // Get local player and camera
            Player localPlayer = networkPlayerObject.GetComponent<Player>();
            Camera cam = localPlayer.cam;

            Vector2 mousePos = Input.mousePosition;
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            Vector3 direction = worldPoint - localPlayer.transform.position;

            // Set cursor world point vector
            data.cursorWorldPoint = new Vector2(worldPoint.x, worldPoint.y);

            // Set aim direction vector
            data.aimDirection = new Vector2(direction.x, direction.y);
        }

        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}

    public void OnConnectedToServer(NetworkRunner runner) {}

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {}

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}

    public void OnSceneLoadDone(NetworkRunner runner) {}

    public void OnSceneLoadStart(NetworkRunner runner) {}

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {}

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {}

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
}
