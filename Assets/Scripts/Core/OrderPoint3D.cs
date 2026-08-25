using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }
    private SphereCollider sphereCollider;
    private Renderer myRenderer;

    [Header("Visual")]
    public float alpha = 0.6f;

    [Header("Position")]
    public float surfaceOffset = 0.3f;             /* Offset above moon surface */

    [Header("Scale by Distance")]
    public float scaleAtMaxDistance = 0.2f;
    public float scaleAtMinDistance = 0.05f;
    public float maxDistanceForScale = 10f;
    public float minDistanceForScale = 1.8f;

    private Vector3 baseScale;
    private CameraController cameraController;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        myRenderer = GetComponent<Renderer>();

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.2f;
        sphereCollider.isTrigger = true;

        baseScale = transform.localScale;

        if (myRenderer != null)
        {
            Shader shader = Shader.Find("Unlit/Color");
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

        /* Random direction on sphere surface */
        Vector3 direction = Random.onUnitSphere;

        /* Calculate radius from moon scale */
        float moonRadius = moonSurface.localScale.x * 0.5f;

        /* Position on surface + offset */
        Vector3 worldPosition = moonSurface.position + direction * (moonRadius + surfaceOffset);

        transform.position = worldPosition;
        transform.rotation = Quaternion.LookRotation(direction);

        /* Color of zone with alpha */
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

        /* Apply initial scale */
        UpdateScale(10f);
    }

    void Update()
    {
        /* Update scale based on camera distance */
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

    void OnMouseDown()
    {
        GameManager3D gm = FindFirstObjectByType<GameManager3D>();
        if (gm != null)
        {
            gm.SelectOrder(Order);
        }
    }

    void OnDestroy()
    {
        if (myRenderer != null && myRenderer.material != null)
            Destroy(myRenderer.material);
    }
}