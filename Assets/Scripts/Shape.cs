using System.Collections.Generic;
using UnityEngine;

public abstract class Shape : MonoBehaviour
{
    [SerializeField] CircleCornerCollider circleColliderPrefab;
    protected CircleCornerCollider[] corners;
    protected Dictionary<CircleCornerCollider, Player> playersAtCorners = new Dictionary<CircleCornerCollider, Player>();
    protected bool buffActivated = false;

    public abstract float Cooldown();

    public void CalculateTriangleCorners(Vector3 center, float radius, float rotationAngle, int nCorners, Transform transform)
    {
        corners = new CircleCornerCollider[nCorners];
        for (int i = 0; i < nCorners; i++)
        {
            // (120deg = 2pi/3 radians) + the rotation angle in radians
            float angle = i * 2 * Mathf.PI / nCorners - rotationAngle * Mathf.Deg2Rad;

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
        foreach (var corner in corners)
        {
            CircleCornerCollider coll = corner.GetComponent<CircleCornerCollider>();
            if (!coll.isOccupied)
            {
                return;
            }
        }

        if (!buffActivated)
        {
            ActivateBuff();
        }
    }

    public abstract void ActivateBuff();
}
