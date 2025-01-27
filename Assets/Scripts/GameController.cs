using System;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag1;
    //[SerializeField] CaptureFlag flag2;

    [SerializeField] float maxTime = 480.0f; //8 minute games
    [SerializeField] float currentTime = 0.0f;

    List<Bullet> bullets;
    List<Player> players;

    //Initialisation
    void Start()
    {
        GameObject playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
        Character armyVet = Resources.Load("ScriptableObjects/Characters/Army Vet") as Character;
        GameObject pickupPrefab = Resources.Load("Prefabs/Pickup") as GameObject;

        bullets = new List<Bullet>();

        players = new List<Player>();

        int team = 1;
        Player playerOne = PrefabFactory.SpawnPlayer(playerPrefab, respawnPoint1, armyVet, team);
        players.Add(playerOne);

        PrefabFactory.SpawnPickup(pickupPrefab, new Vector3(2,2,2), 0, 10); //test pickup
    }

    //Framewise update
    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime >= maxTime){
            //end game
        }

        foreach (Player player in players)
        {
            Bullet newBullet = player.PlayerUpdate();
            if(newBullet != null){
                bullets.Add(newBullet);
            }
            //If player is dead and respawn timer is done then respawn player
            if (!player.isAlive && player.RespawnTimerDone()) {
                player.Respawn();
            }
        }

        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            Bullet bullet = bullets[i];
            bullet.BulletUpdate();
            if(bullet.done){
                bullets.Remove(bullet);
                bullet.DestroyBullet();
            }
        }
    }
}
