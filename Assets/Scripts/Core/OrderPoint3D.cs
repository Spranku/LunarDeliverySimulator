using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }
    private SphereCollider sphereCollider;
    private Renderer myRenderer;

    [Header("Visual")]
    public float alpha = 0.6f;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        myRenderer = GetComponent<Renderer>();

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.2f;
        sphereCollider.isTrigger = true;

        /* Создаем Unlit/Color материал */
        if (myRenderer != null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader);
                myRenderer.material = mat;
                Debug.Log("Unlit/Color material created!");
            }
            else
            {
                Debug.LogError("Unlit/Color shader not found! Using Standard.");
                Material mat = new Material(Shader.Find("Standard"));
                myRenderer.material = mat;
            }
        }
    }

    public void Initialize(OrderData order, Transform moonSurface)
    {
        Order = order;

        Vector3 direction = Random.onUnitSphere;
        float moonRadius = moonSurface.localScale.x * 0.5f;
        Vector3 worldPosition = moonSurface.position + direction * (moonRadius + 0.2f);

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

            Debug.Log($"Color set: {color} for order {Order.Title}");
        }
    }

    void OnMouseDown()
    {
        GameManager3D gm = FindFirstObjectByType<GameManager3D>();
        if (gm != null)
        {
            gm.SelectOrder(Order);
        }
    }
}