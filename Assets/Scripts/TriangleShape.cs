using System.Collections.Generic;
using UnityEngine;

public class TriangleShape : MonoBehaviour
{
    public GameObject circleColliderPrefab;
    private GameObject[] corners = new GameObject[3];
    private float radius = 4.5f;
    private bool buffActivated = false;
    private Dictionary<GameObject, GameObject> playersAtCorners = new Dictionary<GameObject, GameObject>();

    // Awake is used because with Start runs before the whole object is initialised. 
    void Awake()
    {
        // eulerAngles represent rotations relative to world coordinates
        CalculateTriangleCorners(transform.position, radius, transform.rotation.eulerAngles.z);
    }

    void CalculateTriangleCorners(Vector3 center, float radius, float rotationAngle)
    {
        for (int i = 0; i < 3; i++)
        {
            // (120deg = 2pi/3 radians) + the rotation angle in radians
            float angle = i * 2 * Mathf.PI / 3 - rotationAngle * Mathf.Deg2Rad;

            float x = center.x + radius * Mathf.Sin(angle);
            float y = center.y + radius * Mathf.Cos(angle);

            // create a new circle collider for every corner position. Quaternion.identity means no rotation, transform is the parent position
            corners[i] = Instantiate(circleColliderPrefab, new Vector3(x, y, center.z), Quaternion.identity, transform);
            // initialise no player at each corner
            playersAtCorners[corners[i]] = null;
        }
    }

    public void CheckCorners()
    {
        foreach (var corner in corners) {
            CircleCornerCollider coll = corner.GetComponent<CircleCornerCollider>();
            if (!coll.isOccupied)
            {
                return;
            }
        }

        if(!buffActivated) {
            ActivateBuff(); 
        }
    }

    void ActivateBuff()
    {
        buffActivated = true;
        Debug.Log("Buff Activated");
    }
}
