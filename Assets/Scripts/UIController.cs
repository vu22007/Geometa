using Fusion;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] GameObject popUpTextPrefab;
    [SerializeField] TMP_Text popUpText;
    GameController gameController;
    [SerializeField] TextMeshProUGUI timeLeftText;

    private void Start()
    {
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
    }

    private void Update()
    {
        // Update the time left in the UI if the client controls this player
        float timeLeft = gameController.GetTimeLeft();
        int secondsLeft = (int)Mathf.Ceil(timeLeft);
        int mins = secondsLeft / 60;
        int secs = secondsLeft % 60;

        if (mins < 0 || secs < 0)
            timeLeftText.text = "Time Left: 0:00";
        else
            timeLeftText.text = "Time Left: " + mins + ":" + secs.ToString("00");
    }

    public void MakePopupText(string message, float speed, Color textColor)
    {
        popUpText.text = message;
        popUpText.color = textColor;

        GameObject popUp = Instantiate(popUpTextPrefab, transform);

        Animator animator = popUp.GetComponent<Animator>();
        animator.speed = speed;
    }
}