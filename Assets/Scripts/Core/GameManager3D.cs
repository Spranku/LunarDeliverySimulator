using UnityEngine;
using System.Collections.Generic;

public class GameManager3D : MonoBehaviour
{
    public GameProgress Progress;

    [Header("Moon")]
    public Transform moonSurface;

    [Header("Order Points")]
    public GameObject orderPointPrefab3D;

    [Header("UI")]
    public OrderPanelUI orderPanel;

    private List<OrderPoint3D> orderPoints = new List<OrderPoint3D>();

    void Awake()
    {
        /* Create test progress */
        Progress = new GameProgress();

        /* Create rovers */
        Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
        Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
        Progress.Rovers.Add(new RoverData("Bigfoot", 150f, 100f, 0.7f));

        /* Create test orders */
        if (Progress.Orders.Count == 0)
        {
            GenerateOrders(5);
        }

        VisualizeOrders();
    }

    void GenerateOrders(int count)
    {
        string[] titles = { "Food", "CO2", "Machines", "Materials", "Medkits" };
        string[] zones = { "Low", "Medium", "High" };

        for (int i = 0; i < count; i++)
        {
            string zone = zones[Random.Range(0, zones.Length)];
            float risk = zone == "Low" ? 0.1f : (zone == "Medium" ? 0.4f : 0.8f);

            Progress.Orders.Add(new OrderData(
                titles[Random.Range(0, titles.Length)],
                Random.Range(10f, 80f),
                Random.Range(50, 300),
                Random.Range(1, 5),
                Vector2.zero,
                zone,
                risk,
                Progress.Day
            ));
        }
    }

    void VisualizeOrders()
    {
        foreach (var point in orderPoints)
        {
            Destroy(point.gameObject);
        }
        orderPoints.Clear();

        foreach (var order in Progress.Orders)
        {
            if (!order.IsCompleted && !order.IsFailed)
            {
                GameObject point = Instantiate(orderPointPrefab3D, moonSurface);
                OrderPoint3D pointScript = point.GetComponent<OrderPoint3D>();
                pointScript.Initialize(order, moonSurface);
                orderPoints.Add(pointScript);
            }
        }
    }

    public void SelectOrder(OrderData order)
    {
        Debug.Log($"Choiced order: {order.Title}");

        if (orderPanel != null)
        {
            orderPanel.ShowOrder(order);
        }
        else
        {
            Debug.LogError("OrderPanel not set in GameManager3D!");
        }
    }

    public void StartDelivery(RoverData rover, OrderData order)
    {
        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;

        Progress.AddMoney(order.Reward);
        order.IsCompleted = true;

        Progress.TotalDeliveriesCompleted++;

        UpdateOrderVisuals();

        SaveManager.Save(Progress);

        /* Force exit focus, unlock camera */
        var cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            if (cam.IsFocusing())
            {
                cam.ExitFocusMode();
            }
            cam.isUIActive = false;
        }

        Debug.Log($"Delivery finished! +{order.Reward} credits");
    }

    void UpdateOrderVisuals()
    {
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null) continue;

            OrderPoint3D point = orderPoints[i];
            if (point != null && point.Order != null && point.Order.IsCompleted)
            {
                Destroy(point.gameObject);
                orderPoints.RemoveAt(i);
            }
        }
    }

    public GameProgress GetProgress()
    {
        return Progress;
    }
}