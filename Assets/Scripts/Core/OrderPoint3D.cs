using UnityEngine;

public class OrderPoint3D : MonoBehaviour
{
    public OrderData Order { get; private set; }
    private SphereCollider sphereCollider;
    private Renderer myRenderer;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        myRenderer = GetComponent<Renderer>();

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 0.3f;
    }

    public void Initialize(OrderData order, Transform moonSurface)
    {
        Order = order;

        /* Position on the sphere */
        Vector3 direction = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        float radius = moonSurface.localScale.x * 0.5f;
        transform.position = moonSurface.position + direction * (radius/* + 0.3f*/);

        /* Rotate to sphere */
        transform.LookAt(moonSurface.position);

        /* Color of zone */
        if (myRenderer != null)
        {
            switch (Order.ZoneType)
            {
                case "Low": myRenderer.material.color = Color.green; break;
                case "Medium": myRenderer.material.color = Color.yellow; break;
                case "High": myRenderer.material.color = Color.red; break;
            }
        }
    }

    void OnMouseDown()
    {
        var gm = FindFirstObjectByType<GameManager3D>();
        if (gm != null)
        {
            gm.SelectOrder(Order);
        }
    }
}