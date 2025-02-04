using UnityEngine;

public class PentagonShape : Shape
{
    private int nCorners = 5;
    [SerializeField] float radius = 3.5f;
    [SerializeField] float cooldown = 7;

    // Awake is used because with Start runs before the whole object is initialised. 
    void Awake()
    {
        // eulerAngles represent rotations relative to world coordinates
        CalculateTriangleCorners(transform.position, radius, transform.rotation.eulerAngles.z, nCorners, transform);
    }

    public override float Cooldown()
    {
        return cooldown;
    }

    public override void ActivateBuff()
    {
        buffActivated = true;
        Debug.Log("Buff Activated");
    }
}
