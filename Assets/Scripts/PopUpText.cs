using System.Collections;
using TMPro;
using UnityEngine;

public class PopUpText : MonoBehaviour
{
    public GameObject popUpTextPrefab;
    public TMP_Text popUpText;
    public Player player;

    public void MakePopupText(string message, float speed, Color textColor){
        popUpText.text = message;
        popUpText.color = textColor;

        GameObject popUp = Instantiate(popUpTextPrefab, transform);

        Animator animator = popUp.GetComponent<Animator>();
        animator.speed = speed;
    }

    public void SetPlayer(Player player)
    {
        this.player = player;
    }
}