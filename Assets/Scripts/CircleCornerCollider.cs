using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CircleCornerCollider : MonoBehaviour
{
    public bool isOccupied = false;
    private GameObject occupyingPlayer;
    private Shape parentShape;

    void Start()
    {
        parentShape = GetComponentInParent<Shape>(true);
        if (parentShape == null) Debug.LogError("CircleCornerCollider's parent doesn't have a component of type Shape.");
        detectPlayersOnInstantiation();
    }

    private void detectPlayersOnInstantiation()
    {
        List<Collider2D> results = new List<Collider2D>();
        int numberOfCollisions = Physics2D.OverlapCollider(GetComponent<CircleCollider2D>(), results);
        foreach (Collider2D coll in results)
        {
            if (coll.CompareTag("Player") && !isOccupied)
            {
                isOccupied = true;
                occupyingPlayer = coll.gameObject;
                Debug.Log("Player " + coll.gameObject.name + " is on collider");

                parentShape.CheckCorners();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player") && !isOccupied)
        {
            isOccupied = true;
            occupyingPlayer = collider.gameObject;
            Debug.Log("Player " + collider.gameObject.name + " entered collider");

            parentShape.CheckCorners();
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject == occupyingPlayer)
        {
            isOccupied = false;
            occupyingPlayer = null;

            Debug.Log("Player " + collider.gameObject.name + " exited");
        }
    }
}
