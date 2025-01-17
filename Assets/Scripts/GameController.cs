using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] List<Player> players;
    [SerializeField] Vector3Int respawnPoint1;
    //[SerializeField] Vector3Int respawnPoint2;
    [SerializeField] CaptureFlag flag;
    [SerializeField] float maxTime;
    [SerializeField] float currentTime;

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
        foreach (Player player in players)
        {
            player.PlayerUpdate();
        }
    }
}
