using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }

    [Header("Colors")]
    public Color activeLowColor = new Color(0f, 1f, 0f, 0.6f);
    public Color activeMediumColor = new Color(1f, 1f, 0f, 0.6f);
    public Color activeHighColor = new Color(1f, 0f, 0f, 0.6f);
    public Color busyColor = new Color(1f, 0.5f, 0f, 0.6f);
    public Color completedColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    public Color failedColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);

    [Header("Position")]
    public float surfaceOffset = 0.3f;

    [Header("Scale by Distance")]
    public float scaleAtMaxDistance = 0.2f;
    public float scaleAtMinDistance = 0.05f;
    public float maxDistanceForScale = 10f;
    public float minDistanceForScale = 1.8f;

    private SphereCollider sphereCollider;
    private Renderer myRenderer;
    private CameraController cameraController;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = true;

        myRenderer = GetComponent<Renderer>();
        cameraController = FindFirstObjectByType<CameraController>();

        if (myRenderer != null && myRenderer.material != null)
        {
            Debug.Log($"Material: {myRenderer.material.name}, Shader: {myRenderer.material.shader.name}");
        }
    }

    public void Initialize(OrderData order, Transform moonSurface)
    {
        Order = order;

        Vector3 direction = Random.onUnitSphere;
        float moonRadius = moonSurface.localScale.x * 0.5f;
        Vector3 worldPosition = moonSurface.position + direction * (moonRadius + surfaceOffset);

        transform.position = worldPosition;
        transform.rotation = Quaternion.LookRotation(direction);

        UpdateColor();
        UpdateScale(10f);
    }

    void Update()
    {
        if (cameraController != null)
            UpdateScale(cameraController.distance);
        else
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
                UpdateScale(distance);
            }
        }
    }

    void UpdateScale(float cameraDistance)
    {
        float clampedDistance = Mathf.Clamp(cameraDistance, minDistanceForScale, maxDistanceForScale);
        float t = (clampedDistance - minDistanceForScale) / (maxDistanceForScale - minDistanceForScale);
        float scale = Mathf.Lerp(scaleAtMinDistance, scaleAtMaxDistance, t);
        transform.localScale = Vector3.one * scale;
    }

    public void SetColor(Color color)
    {
        if (myRenderer != null && myRenderer.material != null)
        {
            myRenderer.material.color = color;
        }
    }

    void UpdateColor()
    {
        if (myRenderer == null || myRenderer.material == null || Order == null) return;

        Color color = Color.white;

        if (Order.IsBusy)
            color = busyColor;
        else if (Order.IsCompleted)
            color = completedColor;
        else if (Order.IsFailed)
            color = failedColor;
        else
        {
            switch (Order.ZoneType)
            {
                case "Low": color = activeLowColor; break;
                case "Medium": color = activeMediumColor; break;
                case "High": color = activeHighColor; break;
            }
        }

        myRenderer.material.color = color;
        Debug.Log($"Point {Order.Title}: {color}");
    }

    void OnMouseDown()
    {
        if (Order == null || Order.IsCompleted || Order.IsBusy)
            return;

        GameManager3D gm = FindFirstObjectByType<GameManager3D>();
        gm?.SelectOrder(Order);
    }
}