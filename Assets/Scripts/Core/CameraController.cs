using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 8f;

    [Header("Start Auto Swipe")]
    public bool playStartSwipe = true;
    public Vector2 startSwipeDelta = new Vector2(300f, -50f);
    public float startSwipeInertiaMultiplier = 0.8f;

    [Header("Start Auto Zoom")]
    public bool playStartZoom = true;
    public float startZoomFrom = 5f;
    public float startZoomTo = 10f;
    public float startZoomInertiaMultiplier = 0.3f;

    private Vector2 startSwipeVelocity = Vector2.zero;
    private float startZoomVelocity = 0f;
    private bool isStartZoomActive = false;

    [Header("Rotation")]
    public float rotationSpeed = 2f;
    private float currentX = 0f;
    private float currentY = 20f;

    [Header("Inertia Rotation")]
    public float inertiaDuration = 1.5f;
    public float inertiaMultiplier = 0.5f;
    private Vector2 velocity = Vector2.zero;
    private bool isDragging = false;
    private Vector2 lastMousePosition;

    [Header("Inertia Zoom")]
    public float zoomInertiaDuration = 1f;
    public float zoomInertiaMultiplier = 0.3f;
    public float zoomSmoothSpeed = 0.1f;
    private float zoomVelocity = 0f;
    private float targetDistance = 8f;
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

        targetDistance = distance;

        /* ===== START SWIPE ===== */
        if (playStartSwipe)
        {
            /* Calculate rotation delta from swipe */
            float deltaX = startSwipeDelta.x * rotationSpeed * sensitivity * 0.01f;
            float deltaY = -startSwipeDelta.y * rotationSpeed * sensitivity * 0.01f;

            /* Apply rotation immediately */
            currentX += deltaX;
            currentY += deltaY;
            currentY = Mathf.Clamp(currentY, -80f, 80f);

            /* Set rotation inertia velocity */
            startSwipeVelocity = new Vector2(
                startSwipeDelta.x * 0.01f * startSwipeInertiaMultiplier,
                startSwipeDelta.y * 0.01f * startSwipeInertiaMultiplier
            );
            velocity = startSwipeVelocity;

            Debug.Log($"Swipe applied! Rotation: ({currentX}, {currentY})");
        }

        /* ===== START ZOOM ===== */
        if (playStartZoom)
        {
            /* Start from startZoomFrom */
            distance = startZoomFrom;
            targetDistance = startZoomFrom;

            /* Set velocity to move from startZoomFrom to startZoomTo */
            startZoomVelocity = (startZoomTo - startZoomFrom) * 0.5f * startZoomInertiaMultiplier;
            isStartZoomActive = true;

            Debug.Log($"Zoom started! From: {startZoomFrom}, To: {startZoomTo}, Velocity: {startZoomVelocity}");
        }

        UpdateCameraPosition();
        Debug.Log("Start animation complete! Both swipe and zoom applied simultaneously!");
    }

    void Update()
    {
        if (target == null) return;

        /* ===== START ZOOM WITH INERTIA ===== */
        if (isStartZoomActive && Mathf.Abs(startZoomVelocity) > 0.001f)
        {
            /* Decrease velocity */
            startZoomVelocity = Mathf.Lerp(startZoomVelocity, 0f, Time.deltaTime / zoomInertiaDuration);

            if (Mathf.Abs(startZoomVelocity) < 0.001f)
            {
                startZoomVelocity = 0f;
                isStartZoomActive = false;
                Debug.Log("Zoom inertia finished!");
            }

            /* Apply zoom */
            targetDistance += startZoomVelocity * Time.deltaTime * 10f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            distance = Mathf.Lerp(distance, targetDistance, zoomSmoothSpeed);

            Debug.Log($"Zoom: distance = {distance}, targetDistance = {targetDistance}, velocity = {startZoomVelocity}");
        }

        /* ===== NORMAL INPUT ===== */

        /* Mouse */
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
            velocity = Vector2.zero;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            var delta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            velocity = delta * rotationSpeed * sensitivity * 0.1f;

            currentX += delta.x * rotationSpeed * sensitivity;
            currentY -= delta.y * rotationSpeed * sensitivity;
            currentY = Mathf.Clamp(currentY, -80f, 80f);
        }

        /* Inertion rotation */
        if (!isDragging)
        {
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime / inertiaDuration);

            if (velocity.magnitude < 0.001f)
                velocity = Vector2.zero;

            currentX += velocity.x * inertiaMultiplier;
            currentY += velocity.y * inertiaMultiplier;
            currentY = Mathf.Clamp(currentY, -80f, 80f);
        }

        /* Fingers (mobile) */
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

            zoomVelocity = -deltaDistance * zoomSpeed * 0.01f;

            targetDistance += deltaDistance * zoomSpeed * 0.01f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            isZooming = true;
        }
        else
        {
            isZooming = false;
        }

        /* Inertion zoom */
        if (!isZooming && Mathf.Abs(zoomVelocity) > 0.001f)
        {
            zoomVelocity = Mathf.Lerp(zoomVelocity, 0f, Time.deltaTime / zoomInertiaDuration);

            if (Mathf.Abs(zoomVelocity) < 0.001f)
                zoomVelocity = 0f;

            targetDistance -= zoomVelocity * zoomInertiaMultiplier;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        /* Zoom of mouse */
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            zoomVelocity = scroll * zoomSpeed;

            targetDistance += scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        /* Smooth zoom */
        distance = Mathf.Lerp(distance, targetDistance, zoomSmoothSpeed);

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