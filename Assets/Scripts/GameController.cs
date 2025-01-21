using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] List<Player> players;
    [SerializeField] Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag1;
    //[SerializeField] CaptureFlag flag2;

    [SerializeField] float maxTime = 480.0f; //8 minute games
    [SerializeField] float currentTime = 0.0f;

    List<Bullet> bullets;

    //Initialisation
    void Start()
    {
        foreach (Player player in players)
        {
            player.PlayerStart(respawnPoint1);
        }
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
            Bullet newbullet = player.PlayerUpdate();
            if(newbullet != null){
                bullets.Add(newbullet);
            }
        }
        foreach (Bullet bullet in bullets)
        {
            bullet.BulletUpdate();
        }
    }
}
