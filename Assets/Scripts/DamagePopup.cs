using TMPro;
using UnityEngine;
using Fusion;

public class DamagePopup : NetworkBehaviour
{
    TextMeshPro textMesh;
    float destructTimeMax = 2f;
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

        moveVector = new Vector3(0, 1) * 20f;
    }

    void Update(){
        //moving
        transform.position += moveVector * Runner.DeltaTime;
        moveVector -= moveVector * 8f * Runner.DeltaTime;

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
