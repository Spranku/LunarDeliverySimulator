using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }

    [Header("Visual")]
    public float alpha = 0.6f;

    [Header("Position")]
    public float surfaceOffset = 0.3f;

    [Header("Scale by Distance")]
    public float scaleAtMaxDistance = 0.2f;
    public float scaleAtMinDistance = 0.05f;
    public float maxDistanceForScale = 10f;
    public float minDistanceForScale = 1.8f;

    private SphereCollider sphereCollider;
    private Renderer myRenderer;
    private Material materialInstance;
    private CameraController cameraController;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = true;

        myRenderer = GetComponent<Renderer>();
        if (myRenderer != null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            materialInstance = new Material(shader);
            myRenderer.material = materialInstance;
        }

        cameraController = FindFirstObjectByType<CameraController>();
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
        if (materialInstance != null)
        {
            color.a = alpha;
            materialInstance.color = color;
        }
    }

    void UpdateColor()
    {
        if (materialInstance == null || Order == null) return;

        Color color = Color.white;

        switch (Order.ZoneType)
        {
            case "Low": color = Color.green; break;
            case "Medium": color = Color.yellow; break;
            case "High": color = Color.red; break;
        }

        color.a = alpha;
        materialInstance.color = color;
    }

    void OnMouseDown()
    {
        if (Order == null || Order.IsCompleted || Order.IsBusy)
            return;

        GameManager3D gm = FindFirstObjectByType<GameManager3D>();
        gm?.SelectOrder(Order);
    }

    void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }
}