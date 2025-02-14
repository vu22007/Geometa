using System.Collections;
using TMPro;
using UnityEngine;

public class PopUpText : MonoBehaviour
{
    public GameObject popUpTextPrefab;
    public TMP_Text popUpText;
    bool popUpPlaying;

    IEnumerator MakePopupText(string message, float speed, Color textColor){
        popUpPlaying = true;
        popUpText.text = message;
        popUpText.color = textColor;

        GameObject popUp = Instantiate(popUpTextPrefab, transform);

        Animator animator = popUp.GetComponent<Animator>();
        animator.speed = speed;

        yield return new WaitForSeconds(1f);
        popUpPlaying = false; 
    }

    public IEnumerator QueuePopUp(string message, float speed, Color textColor){
        while (popUpPlaying){
            yield return new WaitForSeconds(0.1f);
        }

        StartCoroutine(MakePopupText(message, speed, textColor));
    }
}