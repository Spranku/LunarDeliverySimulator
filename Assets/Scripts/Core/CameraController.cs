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

    [Header("Inertia Rotation")]
    public float inertiaDuration = 1.5f;        /* Time of end*/
    public float inertiaMultiplier = 0.5f;      /* Multiplier */
    private Vector2 velocity = Vector2.zero;    /* Current rotation speed */
    private bool isDragging = false;
    private Vector2 lastMousePosition;

    [Header("Inertia Zoom")]
    public float zoomInertiaDuration = 1f;          /* Time of end */
    public float zoomInertiaMultiplier = 0.3f;      /* Multiplier */
    public float zoomSmoothSpeed = 0.1f;            /* Smoothing speed */
    private float zoomVelocity = 0f;                /* Current zoom speed */
    private float targetDistance = 8f;              /* Target distance for smoothing */
    private bool isZooming = false;

    [Header("Zoom")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float zoomSpeed = 2f;

    [Header("Sensitivity")]
    public float sensitivity = 0.5f;

    void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<MoonRotator>()?.transform;

        if (target == null)
            Debug.LogWarning("Target not set!");

        targetDistance = distance; /* Init target distance */
    }

    void Update()
    {
        if (target == null) return;

        /* Mouse */
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
            velocity = Vector2.zero; /* Reset velocity */
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            var delta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            /* Save velocity */
            velocity = delta * rotationSpeed * sensitivity * 0.1f;

            currentX += delta.x * rotationSpeed * sensitivity;
            currentY -= delta.y * rotationSpeed * sensitivity;
            currentY = Mathf.Clamp(currentY, -80f, 80f);
        }

        /* Inertion rotation (after end touch)*/
        if (!isDragging)
        {
            /* Easy decrement velocity */
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime / inertiaDuration);

            /* If velocity is very low - stop */
            if (velocity.magnitude < 0.001f)
                velocity = Vector2.zero;

            /* Attempt inertion */
            currentX += velocity.x * inertiaMultiplier;
            currentY += velocity.y * inertiaMultiplier;
            currentY = Mathf.Clamp(currentY, -80f, 80f);
        }

        /* Fingers (mobile)
         One finger - rotation with inertion */
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                velocity = Vector2.zero;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                var delta = touch.deltaPosition;
                velocity = delta * rotationSpeed * sensitivity * 0.05f;

                currentX += delta.x * rotationSpeed * sensitivity * 0.1f;
                currentY -= delta.y * rotationSpeed * sensitivity * 0.1f;
                currentY = Mathf.Clamp(currentY, -80f, 80f);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }

        /* Zoom by two fingers */
        if (Input.touchCount == 2)
        {
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            var prevPos1 = touch1.position - touch1.deltaPosition;
            var prevPos2 = touch2.position - touch2.deltaPosition;

            float prevDistance = Vector2.Distance(prevPos1, prevPos2);
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            float deltaDistance = currentDistance - prevDistance;

            /* Save zoom velocity */
            zoomVelocity = -deltaDistance * zoomSpeed * 0.01f;

            targetDistance += deltaDistance * zoomSpeed * 0.01f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            isZooming = true;
        }
        else
        {
            isZooming = false;
        }

        /* Inertion zoom (after two fingers release) */
        if (!isZooming && Mathf.Abs(zoomVelocity) > 0.001f)
        {
            /* Easy decrement zoom velocity */
            zoomVelocity = Mathf.Lerp(zoomVelocity, 0f, Time.deltaTime / zoomInertiaDuration);

            /* If velocity is very low - stop */
            if (Mathf.Abs(zoomVelocity) < 0.001f)
                zoomVelocity = 0f;

            /* Attempt inertion zoom */
            targetDistance -= zoomVelocity * zoomInertiaMultiplier;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        /* Zoom of mouse */
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            /* Save zoom velocity */
            zoomVelocity = scroll * zoomSpeed;

            targetDistance += scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        /* Smooth zoom (eliminate jerking) */
        distance = Mathf.Lerp(distance, targetDistance, zoomSmoothSpeed);

        /* Camera update */
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

        var rotation = Quaternion.Euler(currentY, currentX, 0);
        var position = target.position - rotation * Vector3.forward * distance;

        transform.position = position;
        transform.LookAt(target.position);
    }
}