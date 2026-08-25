using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 8f;

    [Header("Focus")]
    public float focusDistance = 2f;
    public float focusLerpSpeed = 3f;
    private Transform focusTarget = null;
    private bool isFocusing = false;
    private Vector3 focusTargetPosition;
    private float focusStartTime = 0f;
    private bool focusJustActivated = false;

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

    [Header("Sensitivity (Zoom Dependent)")]
    public float sensitivityAtMaxDistance = 0.15f;
    public float sensitivityAtMinDistance = 0.005f;
    public float maxDistanceForSensitivity = 10f;
    public float minDistanceForSensitivity = 1.8f;

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

    void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<MoonRotator>()?.transform;

        if (target == null)
            Debug.LogWarning("Target not set!");

        targetDistance = distance;

        if (playStartSwipe)
        {
            float sensitivity = GetCurrentSensitivity();

            float deltaX = startSwipeDelta.x * rotationSpeed * sensitivity * 0.01f;
            float deltaY = -startSwipeDelta.y * rotationSpeed * sensitivity * 0.01f;

            currentX += deltaX;
            currentY += deltaY;
            currentY = Mathf.Clamp(currentY, -80f, 80f);

            startSwipeVelocity = new Vector2(
                startSwipeDelta.x * 0.01f * startSwipeInertiaMultiplier,
                startSwipeDelta.y * 0.01f * startSwipeInertiaMultiplier
            );
            velocity = startSwipeVelocity;

            Debug.Log($"Swipe applied! Rotation: ({currentX}, {currentY})");
        }

        if (playStartZoom)
        {
            distance = startZoomFrom;
            targetDistance = startZoomFrom;

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
            startZoomVelocity = Mathf.Lerp(startZoomVelocity, 0f, Time.deltaTime / zoomInertiaDuration);

            if (Mathf.Abs(startZoomVelocity) < 0.001f)
            {
                startZoomVelocity = 0f;
                isStartZoomActive = false;
                Debug.Log("Zoom inertia finished!");
            }

            targetDistance += startZoomVelocity * Time.deltaTime * 10f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            distance = Mathf.Lerp(distance, targetDistance, zoomSmoothSpeed);
        }

        /* ===== FOCUS MODE ===== */
        if (isFocusing && focusTarget != null)
        {
            /* Update target position (point moves with moon) */
            focusTargetPosition = focusTarget.position;

            /* Calculate vector from moon center to the point */
            Vector3 moonToPoint = focusTargetPosition - target.position;
            Vector3 directionFromMoon = moonToPoint.normalized;

            /* Position camera: from point outward from moon */
            Vector3 desiredPosition = focusTargetPosition + directionFromMoon * focusDistance;

            /* Smoothly move camera */
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * focusLerpSpeed);

            /* Look at focus point */
            transform.LookAt(focusTargetPosition);

            /* Update distance */
            distance = Vector3.Distance(transform.position, focusTargetPosition);

            /* Cancel focus on any input (with delay protection) */
            if (!focusJustActivated)
            {
                if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                {
                    ExitFocusMode();
                }
            }
            else
            {
                /* Reset flag after 0.5 seconds */
                if (Time.time - focusStartTime > 0.5f)
                {
                    focusJustActivated = false;
                }
            }

            return;
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

            float sensitivity = GetCurrentSensitivity();
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
                float sensitivity = GetCurrentSensitivity();
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

        /* If in focus mode, don't use this */
        if (isFocusing) return;

        var rotation = Quaternion.Euler(currentY, currentX, 0);
        var position = target.position - rotation * Vector3.forward * distance;

        transform.position = position;
        transform.LookAt(target.position);
    }

    float GetCurrentSensitivity()
    {
        float clampedDistance = Mathf.Clamp(distance, minDistanceForSensitivity, maxDistanceForSensitivity);
        float t = (clampedDistance - minDistanceForSensitivity) / (maxDistanceForSensitivity - minDistanceForSensitivity);
        return Mathf.Lerp(sensitivityAtMinDistance, sensitivityAtMaxDistance, t);
    }

    /* ===== FOCUS METHODS ===== */
    public void FocusOnPoint(Transform point)
    {
        if (point == null) return;

        focusTarget = point;
        focusTargetPosition = point.position;
        isFocusing = true;
        focusJustActivated = true;
        focusStartTime = Time.time;

        /* Disable auto rotation while focusing */
        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null)
            rotator.autoRotate = false;

        Debug.Log($"Focus on point: {point.name}");
    }

    public void ExitFocusMode()
    {
        if (!isFocusing) return;

        isFocusing = false;
        focusTarget = null;
        focusJustActivated = false;

        /* Re-enable auto rotation */
        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null)
            rotator.autoRotate = true;

        /* Reset distance to normal */
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        Debug.Log("Exit focus mode");
    }

    public bool IsFocusing()
    {
        return isFocusing;
    }
}