using UnityEngine;

public class Lobby : MonoBehaviour
{
    private GameController gameController;

    public void OnEnable()
    {
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
    }
}
