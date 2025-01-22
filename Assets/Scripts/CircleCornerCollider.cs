using UnityEngine;

public class CircleCornerCollider : MonoBehaviour
{
    public bool isOccupied = false;
    private GameObject occupyingPlayer;
    // Should replace with parent shape later
    private TriangleShape parentShape;

    void Start()
    {
        parentShape = GetComponentInParent<TriangleShape>();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Debug.Log("Collision in circle occured");
        if(collider.CompareTag("Player") && !isOccupied)
        {
            isOccupied = true;
            occupyingPlayer = collider.gameObject;

            Debug.Log("Player " + collider.gameObject.name + " entered");
            // parentShape.CheckCorners(); 
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject == occupyingPlayer)
        {
            isOccupied = false;
            occupyingPlayer = null;

            // Maybe should delete this because there is no need for check when exited
            // parentShape.CheckCorners();
        }
    }

    void Update()
    {
        
    }
}
