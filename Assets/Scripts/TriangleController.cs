using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class TriangleController : MonoBehaviour
{
    //private bool preview = true;
    //private PlayerInput playerInput;
    //private InputAction triangleActivation;
    public GameObject trianglePrefab;
    private GameObject previewTriangle;
    public GameObject previewTrianglePrefab;
    private bool isPlacing = false;
    [SerializeField] Camera cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void ActivateTriangle()
    {
        cam = GetComponentInChildren<Camera>();
        Debug.Log("Preview Triangle");
        if (isPlacing)
        {
            Debug.Log("Already placing a triangle");
            return;
        }

        previewTriangle = Instantiate(previewTrianglePrefab, Input.mousePosition, Quaternion.identity);
        isPlacing = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (isPlacing)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
            previewTriangle.transform.position = worldPoint;

            if (Input.GetMouseButtonDown(0))
            {
                PlaceTriangle();
            }
        }
    }

    public void PlaceTriangle()
    {
        Instantiate(trianglePrefab, previewTriangle.transform.position, Quaternion.identity);
        Debug.Log("Triangle Placed");

        Destroy(previewTriangle);
        previewTriangle = null;

        isPlacing = false;

    }
}
