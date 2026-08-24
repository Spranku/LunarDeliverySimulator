using UnityEngine;
using System.Collections.Generic;

public class GameManager3D : MonoBehaviour
{
    public GameProgress Progress;

    [Header("Moon")]
    public Transform moonSurface;

    [Header("Order Points")]
    public GameObject orderPointPrefab3D;

    private List<OrderPoint3D> orderPoints = new List<OrderPoint3D>();

    void Awake()
    {
        Progress = SaveManager.Load();

        if (Progress.Rovers.Count == 0)
        {
            Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
            Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
            SaveManager.Save(Progress);
        }

        if (Progress.Orders.Count == 0)
        {
            GenerateOrders(5);
            SaveManager.Save(Progress);
        }

        VisualizeOrders();
    }

    void GenerateOrders(int count)
    {
        string[] titles = { "Food", "CO_2", "Machines", "Materials", "Medkits" };
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
                Vector2.zero, /* Useless on 3D */
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
                var point = Instantiate(orderPointPrefab3D);
                var pointScript = point.GetComponent<OrderPoint3D>();
                pointScript.Initialize(order, moonSurface);
                orderPoints.Add(pointScript);
            }
        }
    }

    public void SelectOrder(OrderData order)
    {
        Debug.Log($"Choice order: {order.Title}");
        // TODO: Show UI Panel
    }
}