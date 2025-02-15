using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float destructTime;
    float timer;
    void Start()
    {
        timer = destructTime;
    }

    void Update()
    {
        timer = timer - Time.deltaTime;
        if(timer <= 0){
            Destroy(gameObject);
        }
    }
}
