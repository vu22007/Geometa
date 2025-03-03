using TMPro;
using UnityEngine;
using Fusion;

public class DamagePopup : MonoBehaviour
{
    TextMeshPro textMesh;
    float destructTimeMax = 1.5f;
    float destructTime;
    Vector3 moveVector;
    public int team;


    private void Awake(){
        textMesh = gameObject.GetComponent<TextMeshPro>();
    }

    public void Setup(int damageAmount, int team){
        textMesh.SetText(damageAmount.ToString());
        this.team = team;
        destructTime = destructTimeMax;

        float randomXDir = Random.Range(-0.4f, 0.4f);
        moveVector = new Vector3(randomXDir, 1) * 20f;
    }

    void Update(){
        //moving
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        destructTime -= Time.deltaTime;

        //effects
        if (destructTime > destructTimeMax * 0.5f){
            transform.localScale += 0.7f * Vector3.one * Time.deltaTime;
        }
        else{
            transform.localScale -= 0.7f * Vector3.one * Time.deltaTime;
        }


        if (destructTime < 0){
            Destroy(gameObject);
        }
    }
}
