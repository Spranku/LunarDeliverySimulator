using UnityEngine;

public class OrderPoint : MonoBehaviour
{
    public OrderData Order { get; private set; }
    private SpriteRenderer myRenderer;

    void Awake()
    {
        myRenderer = GetComponent<SpriteRenderer>();

        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;
        }
    }

    public void Initialize(OrderData order)
    {
        Order = order;
        transform.position = order.TargetPosition;

        if (myRenderer != null)
        {
            switch (order.ZoneType)
            {
                case "Low": myRenderer.color = Color.green; break;
                case "Medium": myRenderer.color = Color.yellow; break;
                case "High": myRenderer.color = Color.red; break;
                default: myRenderer.color = Color.white; break;
            }
        }
    }

    public void OnClick()
    {
        if (Order == null)
        {
            Debug.LogWarning("Клик по пустой точке!");
            return;
        }

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.SelectOrder(Order);
        }
    }
}