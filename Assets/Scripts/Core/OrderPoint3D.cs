using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }
    private SphereCollider sphereCollider;
    private Renderer myRenderer;

    [Header("Visual")]
    public float alpha = 0.6f;

    [Header("Position")]
    public float surfaceOffset = 0.3f;

    [Header("Scale by Distance")]
    public float scaleAtMaxDistance = 0.2f;
    public float scaleAtMinDistance = 0.05f;
    public float maxDistanceForScale = 10f;
    public float minDistanceForScale = 1.8f;

    private CameraController cameraController;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        myRenderer = GetComponent<Renderer>();

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.5f;
        sphereCollider.isTrigger = true;

        if (myRenderer != null)
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader);
                myRenderer.material = mat;
            }
            else
            {
                Material mat = new Material(Shader.Find("Standard"));
                myRenderer.material = mat;
            }
        }

        cameraController = FindFirstObjectByType<CameraController>();
    }

    public void Initialize(OrderData order, Transform moonSurface)
    {
        Order = order;
        Debug.Log($"📍 Point {order.Title}: zone={order.ZoneType}, risk={order.Risk * 100:F0}%");

        Vector3 direction = Random.onUnitSphere;
        float moonRadius = moonSurface.localScale.x * 0.5f;
        Vector3 worldPosition = moonSurface.position + direction * (moonRadius + surfaceOffset);

        transform.position = worldPosition;
        transform.rotation = Quaternion.LookRotation(direction);

        if (myRenderer != null && myRenderer.material != null)
        {
            Color color = Color.white;

            switch (Order.ZoneType)
            {
                case "Low": color = Color.green; break;
                case "Medium": color = Color.yellow; break;
                case "High": color = Color.red; break;
                default: color = Color.white; break;
            }

            color.a = alpha;
            myRenderer.material.color = color;
        }

        UpdateScale(10f);
    }

    void Update()
    {
        if (cameraController != null)
        {
            UpdateScale(cameraController.distance);
        }
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
            color.a = alpha;
            myRenderer.material.color = color;
            Debug.Log($"Point {Order?.Title} color changed to {color}");
        }
    }

    void OnMouseDown()
    {
        if (Order == null) return;

        /* ===== Dont choice competed or busy order ===== */
        if (Order.IsCompleted || Order.IsBusy)
        {
            Debug.Log($"Order {Order.Title} is already completed or in progress!");
            return;
        }

        var gm = FindFirstObjectByType<GameManager3D>();
        if (gm != null)
        {
            gm.SelectOrder(Order);
        }

        var cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            cam.FocusOnPoint(transform); 
        }
    }

    void OnDestroy()
    {
        if (myRenderer != null && myRenderer.material != null)
            Destroy(myRenderer.material);
    }
}