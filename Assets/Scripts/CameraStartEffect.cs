using System.Collections;
using UnityEngine;

public class CameraStartEffect : MonoBehaviour
{
    // So far this code doesn't do much but this can be modify later to have a proper start behaviour
    Vector3 moveToPosition; // This is where the camera will move after the start
    float speed = 2f; // this is the speed at which the camera moves
    bool started = false; // stops the movement until we want it

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveToPosition = new Vector3(0, 2, -0.01f); // 2 meters above/ 0.01 meters behind
        // The following function decides how long to stare at the player before moving
        LookAtPlayerFor(3.5f); // waits for 3.5 seconds then starts 
    }

    // Update is called once per frame
    void Update()
    {
        if (!started)
            return;

        // Move the camera into position
        transform.position = Vector3.Lerp(transform.position, moveToPosition, speed);

        // Ensure the camera always looks at the player
        transform.LookAt(transform.parent);
    }

    IEnumerator LookAtPlayerFor(float time)
    {
        yield return new WaitForSeconds(time);
        started = true;
    }
}
