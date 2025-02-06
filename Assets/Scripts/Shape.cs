using System.Collections.Generic;
using Fusion;
using UnityEngine;

public abstract class Shape : NetworkBehaviour
{
    [SerializeField] CircleCornerCollider circleColliderPrefab;
    protected CircleCornerCollider[] corners;
    protected Dictionary<CircleCornerCollider, Player> playersAtCorners = new Dictionary<CircleCornerCollider, Player>();
    protected bool buffActivated = false;
    [SerializeField] public bool cornersInitialised = false;
    [Networked, OnChangedRender(nameof(OnIsPreviewChanged))] public bool isPreview { get; set; }

    // Shape intialisation (called from shape controller on server when creating the shape)
    public void OnCreated(bool isPreview)
    {
        this.isPreview = isPreview;
    }

    // Shape initialisation (called on each client and server when shape is spawned on network)
    public override void Spawned()
    {
        // Check if this shape is owned by this client
        if (HasInputAuthority)
        {
            // Look over all shape controllers and find the one that this client owns (the one with input authority)
            // and set its previewShape and currentShape to this shape, so that the client's copy of their shape controller
            // can control the shape for client-side prediction purposes
            foreach (GameObject shapeControllerObject in GameObject.FindGameObjectsWithTag("ShapeController"))
            {
                ShapeController shapeController = shapeControllerObject.GetComponent<ShapeController>();
                if (shapeController.HasInputAuthority)
                {
                    // This shape belongs to this shape controller for this client, so add the shape to controller
                    shapeController.previewShape = gameObject;
                    shapeController.currentShape = this;
                }
            }
        }

        OnIsPreviewChanged();
    }

    // Called when the isPreview networked property is changed
    void OnIsPreviewChanged()
    {
        // If the client does not own this shape, then make it invisible if the shape is a preview
        if (!HasInputAuthority)
        {
            bool isVisisble = !isPreview;
            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = isVisisble;
            }
        }
    }

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

        cornersInitialised = true;
    }

    public void EnableCorners()
    {
        if (cornersInitialised)
        {
            foreach(CircleCornerCollider corner in corners)
            {
                CircleCollider2D col = corner.GetComponent<CircleCollider2D>();
                if (col == null)
                {
                    Debug.LogError("Circle Corner Collider doesn't have a CircleCollider2D component");
                }
                col.enabled = true;
            }
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
