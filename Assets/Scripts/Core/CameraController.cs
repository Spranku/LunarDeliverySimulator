using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 8f;

    [Header("Orbit Settings")]
    public float rotationSpeed = 2f;
    private float currentX = 0f;
    private float currentY = 20f;

    [Header("Focus Settings")]
    public float focusDistance = 0.5f;
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
    public float zoomInertiaDuration = 1.5f; 
    public float zoomInertiaMultiplier = 0.5f; 
    public float zoomSmoothSpeed = 0.05f;
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
        }

        if (playStartZoom)
        {
            distance = startZoomFrom;
            targetDistance = startZoomFrom;

            startZoomVelocity = (startZoomTo - startZoomFrom) * 0.5f * startZoomInertiaMultiplier;
            isStartZoomActive = true;
        }

        UpdateCameraPosition();
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
            }

            targetDistance += startZoomVelocity * Time.deltaTime * 10f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 2f);
        }
        else
        {
            if (Mathf.Abs(distance - targetDistance) > 0.01f)
            {
                distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 2f);
            }
            else
            {
                distance = targetDistance;
            }
        }

        /* ===== FOCUS MODE ===== */
        if (isFocusing && focusTarget != null)
        {
            focusTargetPosition = focusTarget.position;

            Vector3 moonToPoint = focusTargetPosition - target.position;
            Vector3 directionFromMoon = moonToPoint.normalized;

            Vector3 desiredPosition = focusTargetPosition + directionFromMoon * focusDistance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * focusLerpSpeed);
            transform.LookAt(focusTargetPosition);

            distance = Vector3.Distance(transform.position, focusTargetPosition);
            targetDistance = distance;

            Vector3 relativePos = transform.position - target.position;
            float currentDist = relativePos.magnitude;
            if (currentDist > 0.01f)
            {
                float newX = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                float newY = Mathf.Asin(Mathf.Clamp(relativePos.y / currentDist, -1f, 1f)) * Mathf.Rad2Deg;
                currentX = newX;
                currentY = Mathf.Clamp(newY, -80f, 80f);
            }

            /* ¬ыход из фокуса при любом действии мыши */
            if (!focusJustActivated)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.touchCount > 0)
                {
                    ExitFocusMode();
                    return;
                }
            }
            else
            {
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

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

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

        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null)
            rotator.autoRotate = false;
    }

    void StartAutoZoomOut()
    {
        float currentDist = distance;
        float targetDist = 10f;

        float delta = targetDist - currentDist;

        if (Mathf.Abs(delta) < 0.1f)
        {
            return;
        }

        targetDistance = targetDist;
        startZoomVelocity = delta * 0.5f * startZoomInertiaMultiplier;
        isStartZoomActive = true;
    }

    void ExitFocusMode()
    {
        if (!isFocusing) return;

        Debug.Log($"=== EXITING FOCUS ===");
        Debug.Log($"Position: {transform.position}, Angles: ({currentX}, {currentY}), Distance: {distance}");

        Vector3 relativePos = transform.position - target.position;
        float currentDistance = relativePos.magnitude;

        if (currentDistance > 0.01f)
        {
            float newX = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
            float newY = Mathf.Asin(Mathf.Clamp(relativePos.y / currentDistance, -1f, 1f)) * Mathf.Rad2Deg;
            currentX = newX;
            currentY = Mathf.Clamp(newY, -80f, 80f);
        }

        isFocusing = false;
        focusTarget = null;
        focusJustActivated = false;

        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null)
            rotator.autoRotate = true;

        /* Launch auto zoom  */
        StartAutoZoomOut();
    }

    public bool IsFocusing()
    {
        return isFocusing;
    }
}