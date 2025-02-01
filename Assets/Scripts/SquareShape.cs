using System.Collections.Generic;
using UnityEngine;

public class SquareShape : Shape
{
    private int nCorners = 4;
    // public GameObject circleColliderPrefab;
    private float radius = 4.5f;
    // private bool buffActivated = false;
    // private Dictionary<GameObject, GameObject> playersAtCorners = new Dictionary<GameObject, GameObject>();

    // Awake is used because with Start runs before the whole object is initialised. 
    void Awake()
    {
        // eulerAngles represent rotations relative to world coordinates
        CalculateTriangleCorners(transform.position, radius, transform.rotation.eulerAngles.z + 45, nCorners, transform);
    }

    //public override void CheckCorners()
    //{
    //    foreach (var corner in corners)
    //    {
    //        CircleCornerCollider coll = corner.GetComponent<CircleCornerCollider>();
    //        if (!coll.isOccupied)
    //        {
    //            return;
    //        }
    //    }

    //    if (!buffActivated)
    //    {
    //        ActivateBuff();
    //    }
    //}

    public override void ActivateBuff()
    {
        buffActivated = true;
        Debug.Log("Buff Activated");
    }
}
