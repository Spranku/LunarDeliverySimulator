using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 8f;

    [Header("Rotation")]
    public float rotationSpeed = 2f;
    private float currentX = 0f;
    private float currentY = 20f;

    [Header("Zoom")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float zoomSpeed = 2f;

    [Header("Sensitivity")]
    public float sensitivity = 0.5f;

    private bool isDragging = false;
    private Vector2 lastMousePosition;

    void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<MoonRotator>()?.transform;

        if (target == null)
            Debug.LogWarning("Target not set!");
    }

    void Update()
    {
        if (target == null) return;

        /* Rotate of mouse */
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            currentX += delta.x * rotationSpeed * sensitivity;
            currentY -= delta.y * rotationSpeed * sensitivity;
            currentY = Mathf.Clamp(currentY, -80f, 80f);
        }

        /* Rotate of fingers */
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                currentX += delta.x * rotationSpeed * sensitivity * 0.1f;
                currentY -= delta.y * rotationSpeed * sensitivity * 0.1f;
                currentY = Mathf.Clamp(currentY, -80f, 80f);
            }
        }

        /* Zoom */
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /* Zoom of fingers */
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
            Vector2 prevPos2 = touch2.position - touch2.deltaPosition;

            float prevDistance = Vector2.Distance(prevPos1, prevPos2);
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            float deltaDistance = currentDistance - prevDistance;
            distance -= deltaDistance * zoomSpeed * 0.01f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /* Camera update */
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = target.position - rotation * Vector3.forward * distance;

        transform.position = position;
        transform.LookAt(target.position);
    }
}