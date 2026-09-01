using UnityEngine;

public class CameraController : MonoBehaviour
{
    #region Variables

    [Header("Target")]
    public Transform target;
    public float distance = 8f;

    [Header("UI")]
    public bool isUIActive = false;

    [Header("Rotation")]
    public float rotationSpeed = 2f;
    private float horizontalAngle = 0f;
    private float verticalAngle = 20f;

    [Header("Focus")]
    public float focusDistance = 0.5f;
    public float focusLerpSpeed = 3f;
    private bool isFocusing = false;
    private Transform focusTarget = null;
    private float focusStartTime = 0f;
    private bool focusJustActivated = false;
    private bool blockCameraUpdate = false;

    [Header("Sensitivity")]
    public float sensitivityAtMaxDistance = 0.15f;
    public float sensitivityAtMinDistance = 0.005f;
    public float maxDistanceForSensitivity = 10f;
    public float minDistanceForSensitivity = 1.8f;

    [Header("Inertia Rotation")]
    public float inertiaDuration = 1.5f;
    public float inertiaMultiplier = 0.5f;
    public float inertiaStopThreshold = 0.0005f; 
    private Vector2 velocity = Vector2.zero;
    private bool isDragging = false;
    private Vector2 lastMousePosition;

    [Header("Zoom")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float zoomSpeed = 2f;
    public float zoomInertiaDuration = 1.5f;
    public float zoomInertiaMultiplier = 0.5f;
    private float zoomVelocity = 0f;
    private float targetDistance = 8f;
    private bool isZooming = false;

    [Header("Start Animation")]
    public bool playStartSwipe = true;
    public Vector2 startSwipeDelta = new Vector2(300f, -50f);
    public float startSwipeInertiaMultiplier = 0.8f;
    public bool playStartZoom = true;
    public float startZoomFrom = 5f;
    public float startZoomTo = 10f;
    public float startZoomInertiaMultiplier = 0.3f;
    private Vector2 startSwipeVelocity = Vector2.zero;
    private float startZoomVelocity = 0f;
    private bool isStartZoomActive = false;

    private float inertiaTimer = 0f;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<MoonRotator>()?.transform;

        if (target == null)
            Debug.LogWarning("Target not set!");

        targetDistance = distance;

        Vector3 relativePos = transform.position - target.position;
        if (relativePos.magnitude > 0.01f)
        {
            horizontalAngle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Asin(Mathf.Clamp(relativePos.y / relativePos.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
        }

        if (playStartSwipe)
        {
            float sensitivity = GetCurrentSensitivity();
            float deltaX = startSwipeDelta.x * rotationSpeed * sensitivity * 0.01f;
            float deltaY = -startSwipeDelta.y * rotationSpeed * sensitivity * 0.01f;
            horizontalAngle += deltaX;
            verticalAngle += deltaY;
            verticalAngle = Mathf.Clamp(verticalAngle, -80f, 80f);
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

        UpdateZoom();
        UpdateFocus();

        if (blockCameraUpdate)
        {
            blockCameraUpdate = false;
            return;
        }

        if (isUIActive)
        {
            UpdateCameraPosition();
            return;
        }

        UpdateRotationInput();
        UpdateZoomInput();
        UpdateCameraPosition();
    }

    #endregion

    #region Update Methods

    private void UpdateZoom()
    {
        if (isStartZoomActive && Mathf.Abs(startZoomVelocity) > 0.001f)
        {
            startZoomVelocity = Mathf.Lerp(startZoomVelocity, 0f, Time.deltaTime / zoomInertiaDuration);
            if (Mathf.Abs(startZoomVelocity) < 0.0005f)
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
                distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 2f);
            else
                distance = targetDistance;
        }
    }

    private void UpdateFocus()
    {
        if (!isFocusing || focusTarget == null) return;

        Vector3 dirToPoint = (focusTarget.position - target.position).normalized;
        Vector3 desiredPos = focusTarget.position + dirToPoint * focusDistance;

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * focusLerpSpeed);
        transform.LookAt(focusTarget.position);

        distance = Vector3.Distance(transform.position, target.position);
        targetDistance = distance;

        if (!focusJustActivated && !isUIActive)
        {
            if (!IsPointerOverUI() && (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.touchCount > 0))
            {
                ExitFocusMode();
            }
        }
        else
        {
            if (Time.time - focusStartTime > 0.5f)
                focusJustActivated = false;
        }
    }

    private void UpdateRotationInput()
    {
        /* Mouse */
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
            velocity = Vector2.zero;
            inertiaTimer = 0f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            inertiaTimer = 0f;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            float sensitivity = GetCurrentSensitivity();

            velocity = delta * rotationSpeed * sensitivity * 0.1f;
            horizontalAngle += delta.x * rotationSpeed * sensitivity;
            verticalAngle -= delta.y * rotationSpeed * sensitivity;
            verticalAngle = Mathf.Clamp(verticalAngle, -80f, 80f);

            inertiaTimer = 0f;
        }

        /* Smooth */
        if (!isDragging)
        {
            if (velocity.magnitude > inertiaStopThreshold)
            {
                /* exp smooth затухание */
                float decay = Mathf.Exp(-Time.deltaTime * 4f / inertiaDuration);
                velocity *= decay;

                /* Inertioin */
                horizontalAngle += velocity.x * inertiaMultiplier;
                verticalAngle += velocity.y * inertiaMultiplier;
                verticalAngle = Mathf.Clamp(verticalAngle, -80f, 80f);

                inertiaTimer += Time.deltaTime;
            }
            else
            {
                velocity = Vector2.zero;
            }
        }

        /* Mobile touch (one finger) */
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                velocity = Vector2.zero;
                inertiaTimer = 0f;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                float sensitivity = GetCurrentSensitivity();

                velocity = delta * rotationSpeed * sensitivity * 0.05f;
                horizontalAngle += delta.x * rotationSpeed * sensitivity * 0.1f;
                verticalAngle -= delta.y * rotationSpeed * sensitivity * 0.1f;
                verticalAngle = Mathf.Clamp(verticalAngle, -80f, 80f);

                inertiaTimer = 0f;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                inertiaTimer = 0f;
            }
        }
    }

    private void UpdateZoomInput()
    {
        /* Two finger zoom (mobile) */
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);
            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
            Vector2 prevPos2 = touch2.position - touch2.deltaPosition;

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

        /* Zoom inertia - плавное затухание */
        if (!isZooming && Mathf.Abs(zoomVelocity) > 0.0005f)
        {
            float decay = Mathf.Exp(-Time.deltaTime * 4f / zoomInertiaDuration);
            zoomVelocity *= decay;

            targetDistance -= zoomVelocity * zoomInertiaMultiplier * Time.deltaTime * 20f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
        else
        {
            zoomVelocity = 0f;
        }

        /* Mouse scroll wheel */
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            zoomVelocity = -scroll * zoomSpeed;  
            targetDistance -= scroll * zoomSpeed; 
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void UpdateCameraPosition()
    {
        if (target == null || isFocusing || blockCameraUpdate) return;

        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        Vector3 position = target.position - rotation * Vector3.forward * distance;

        transform.position = position;
        transform.LookAt(target.position);
    }

    #endregion

    #region Helpers

    private float GetCurrentSensitivity()
    {
        float clampedDistance = Mathf.Clamp(distance, minDistanceForSensitivity, maxDistanceForSensitivity);
        float t = (clampedDistance - minDistanceForSensitivity) / (maxDistanceForSensitivity - minDistanceForSensitivity);
        return Mathf.Lerp(sensitivityAtMinDistance, sensitivityAtMaxDistance, t);
    }

    private bool IsPointerOverUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return true;
            }
        }
        return false;
    }

    #endregion

    #region Public Methods

    public void FocusOnPoint(Transform point)
    {
        if (point == null) return;

        focusTarget = point;
        isFocusing = true;
        focusJustActivated = true;
        focusStartTime = Time.time;
        blockCameraUpdate = false;

        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null) rotator.autoRotate = false;
    }

    public void ExitFocusMode()
    {
        if (!isFocusing) return;

        isFocusing = false;
        focusTarget = null;
        focusJustActivated = false;
        blockCameraUpdate = true;

        MoonRotator rotator = target?.GetComponent<MoonRotator>();
        if (rotator != null) rotator.autoRotate = true;

        float delta = 10f - distance;
        if (Mathf.Abs(delta) > 0.1f)
        {
            targetDistance = 10f;
            startZoomVelocity = delta * 0.5f * startZoomInertiaMultiplier;
            isStartZoomActive = true;
        }
    }

    public bool IsFocusing()
    {
        return isFocusing;
    }

    #endregion
}