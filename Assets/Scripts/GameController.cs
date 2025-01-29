using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class GameController : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] public Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag1;
    //[SerializeField] CaptureFlag flag2;

    [SerializeField] float maxTime = 480.0f; //8 minute games
    [SerializeField] float currentTime = 0.0f;

    List<Bullet> bullets;
    List<Player> players;

    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // Initialisation
    void Start()
    {
        GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
        Character armyVet = Resources.Load("ScriptableObjects/Characters/Army Vet") as Character;

        bullets = new List<Bullet>();

        players = new List<Player>();

        //int team = 1;
        //Player playerOne = PrefabFactory.SpawnPlayer(playerPrefab, respawnPoint1, armyVet, team);
        //players.Add(playerOne);
    }

    // Update for every server simulation tick
    public override void FixedUpdateNetwork()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= maxTime)
        {
            //end game
        }

        foreach (Player player in players)
        {
            Bullet newBullet = player.PlayerUpdate();
            if (newBullet != null)
            {
                bullets.Add(newBullet);
            }
            //If player is dead and respawn timer is done then respawn player
            if (!player.isAlive && player.RespawnTimerDone())
            {
                player.Respawn();
            }
        }

        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet bullet = bullets[i];
            bullet.BulletUpdate();
            if (bullet.done)
            {
                bullets.Remove(bullet);
                bullet.DestroyBullet();
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            // Load prefabs
            GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
            Character armyVet = Resources.Load("ScriptableObjects/Characters/Army Vet") as Character;

            // Spawn the player network object
            NetworkObject networkPlayerObject = Runner.Spawn(playerPrefab, respawnPoint1, Quaternion.identity, player);

            // Initialise the player
            Player playerObject = networkPlayerObject.GetComponent<Player>();
            playerObject.OnCreated(armyVet, respawnPoint1, 1);

            // Update the player network object
            Runner.SetPlayerObject(player, networkPlayerObject);

            // Add player network object to dictionary
            spawnedPlayers.Add(player, networkPlayerObject);

            // Pass the player to the game controller
            players.Add(playerObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Run the following only on the server
        if (Runner.IsServer)
        {
            if (spawnedPlayers.TryGetValue(player, out NetworkObject networkPlayerObject))
            {
                // Despawn the network object and remove from dictionary
                Runner.Despawn(networkPlayerObject);
                spawnedPlayers.Remove(player);

                // Remove the player from the game controller
                Player playerObject = networkPlayerObject.GetComponent<Player>();
                players.Remove(playerObject);
            }
        }
    }
}
