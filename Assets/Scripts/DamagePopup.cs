using TMPro;
using UnityEngine;
using Fusion;

public class DamagePopup : NetworkBehaviour
{
    TextMeshPro textMesh;
    float destructTimeMax = 3f;
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
    }

    void Update(){
        float moveYSpeed = 20f;
        transform.position += new Vector3(0, moveYSpeed) * Time.deltaTime;

        destructTime -= Runner.DeltaTime;

        //effects
        if (destructTime > destructTimeMax * 0.5f){
            transform.localScale += Vector3.one * Runner.DeltaTime;
        }
        else{
            transform.localScale -= Vector3.one * Runner.DeltaTime;
        }


        if (destructTime < 0){
            Destroy(gameObject);
        }
    }
}
