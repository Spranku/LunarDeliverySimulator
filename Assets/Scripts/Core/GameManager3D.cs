using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager3D : MonoBehaviour
{
    public GameProgress Progress;

    [Header("Moon")]
    public Transform moonSurface;

    [Header("Order Points")]
    public GameObject orderPointPrefab3D;

    [Header("Rovers")]
    public GameObject roverPrefab;
    public Transform roverParent;

    [Header("Base")]
    public GameObject basePrefab;

    [Header("UI")]
    public OrderPanelUI orderPanel;

    private List<OrderPoint3D> orderPoints = new List<OrderPoint3D>();
    private List<RoverVisual> roverVisuals = new List<RoverVisual>();
    private BasePoint currentBase;

    void Awake()
    {
        Progress = new GameProgress();

        Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
        Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
        Progress.Rovers.Add(new RoverData("Bigfoot", 150f, 100f, 0.7f));

        if (Progress.Orders.Count == 0)
        {
            GenerateOrders(5);
        }

        SetupBase();
        VisualizeOrders();
        VisualizeRovers();
    }

    void SetupBase()
    {
        if (basePrefab == null || moonSurface == null) return;

        Vector3 baseDirection = new Vector3(0, 1, 0).normalized;
        float radius = moonSurface.localScale.x * 0.5f;
        float offset = 0.02f;
        Vector3 basePosition = moonSurface.position + baseDirection * (radius + offset);

        GameObject baseObject = Instantiate(basePrefab, moonSurface);
        baseObject.transform.position = basePosition;
        baseObject.transform.LookAt(moonSurface.position);

        currentBase = baseObject.GetComponent<BasePoint>();
        if (currentBase == null)
            currentBase = baseObject.AddComponent<BasePoint>();
    }

    void VisualizeRovers()
    {
        foreach (var visual in roverVisuals)
        {
            if (visual != null) Destroy(visual.gameObject);
        }
        roverVisuals.Clear();

        if (currentBase == null)
        {
            Debug.LogError("No base found!");
            return;
        }

        Vector3 basePos = currentBase.transform.position;

        foreach (var rover in Progress.Rovers)
        {
            /* Skip destroyed rovers */
            if (rover.IsDestroyed)
            {
                continue;
            }

            GameObject roverGO = Instantiate(roverPrefab, roverParent);
            RoverVisual visual = roverGO.GetComponent<RoverVisual>();

            if (visual == null)
                visual = roverGO.AddComponent<RoverVisual>();

            rover.CurrentPosition = basePos;
            visual.Initialize(rover, moonSurface, basePos);
            roverVisuals.Add(visual);
            roverGO.SetActive(false);
        }
    }

    void GenerateOrders(int count)
    {
        string[] titles = { "Food", "CO2", "Machines", "Materials", "Medkits" };
        string[] zones = { "Low", "Medium", "High" };

        for (int i = 0; i < count; i++)
        {
            string zone = zones[Random.Range(0, zones.Length)];
            float risk = zone == "Low" ? 0.01f : (zone == "Medium" ? 0.3f : 0.7f);

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
            /* Dont spawn completed/ busy orders */
            if (!order.IsCompleted && !order.IsFailed && !order.IsBusy)
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
        Debug.Log($"Order selected: {order.Title}");

        OrderPoint3D targetPoint = orderPoints.Find(p => p.Order == order);
        if (targetPoint != null)
        {
            CameraController cam = FindFirstObjectByType<CameraController>();
            if (cam != null)
            {
                cam.FocusOnPoint(targetPoint.transform);
            }
        }

        if (orderPanel != null)
        {
            orderPanel.ShowOrder(order);
            Debug.Log("Order panel ShowOrder called!");
        }
        else
        {
            Debug.LogError("OrderPanel is not assigned in GameManager3D!");
        }
    }

    public void StartDelivery(RoverData rover, OrderData order)
    {
        if (order.IsCompleted || order.IsBusy)
        {
            Debug.LogWarning($"Order {order.Title} is already in progress or completed!");
            return;
        }

        OrderPoint3D targetPoint = orderPoints.Find(p => p.Order == order);
        if (targetPoint == null)
        {
            Debug.LogError("Target point not found!");
            return;
        }

        RoverVisual roverVis = roverVisuals.Find(r => r.data == rover);
        if (roverVis == null)
        {
            Debug.LogError("Rover visual not found!");
            return;
        }

        /* Battery usage */
        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;
        order.IsBusy = true;

        /* Exit focus */
        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            if (cam.IsFocusing()) cam.ExitFocusMode();
            cam.isUIActive = false;
        }

        /* Close panel */
        if (orderPanel != null && orderPanel.IsPanelOpen())
        {
            orderPanel.ClosePanel();
        }

        /* Change color to gray */
        if (targetPoint != null)
        {
            targetPoint.SetColor(Color.gray);
        }

        /* ===== BIND TO ROVER DESTROY ===== */
        roverVis.onRoverDestroyed = () => {
            Progress.ChangeRating(-10f);
            Progress.TotalDeliveriesFailed++;

            /* Order still active (can send other rover) */
            order.IsBusy = false;

            /* Return color to point */
            if (targetPoint != null)
            {
                targetPoint.SetColor(GetOrderColor(order));
            }

            SaveManager.Save(Progress);
            Debug.Log($"💀 Rover lost! Rating -10");
        };

        /* ===== BIND TO SUCCESS DELIVERY ===== */
        roverVis.onDeliveryComplete = () => {
            if (!rover.IsDestroyed)
            {
                Progress.AddMoney(order.Reward);
                Progress.TotalDeliveriesCompleted++;
                Progress.ChangeRating(2f);

                /* Delete order point after rover returned */
                if (targetPoint != null)
                {
                    Destroy(targetPoint.gameObject);
                    orderPoints.Remove(targetPoint);
                }

                SaveManager.Save(Progress);
                Debug.Log($"✅ Delivery complete! +{order.Reward} credits, +2 rating");
            }
        };

        roverVis.MoveTo(targetPoint.transform);

        SaveManager.Save(Progress);
        Debug.Log($"🚀 Rover {rover.Name} sent to {order.Title}!");
    }

    /* Helper method for color */
    Color GetOrderColor(OrderData order)
    {
        switch (order.ZoneType)
        {
            case "Low": return Color.green;
            case "Medium": return Color.yellow;
            case "High": return Color.red;
            default: return Color.white;
        }
    }

    void UpdateOrderVisuals()
    {
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null) continue;

            OrderPoint3D point = orderPoints[i];
            if (point != null && point.Order != null &&
                (point.Order.IsCompleted || point.Order.IsBusy))
            {
                Destroy(point.gameObject);
                orderPoints.RemoveAt(i);
            }
        }
    }

    public void RegisterBase(BasePoint basePoint)
    {
        currentBase = basePoint;
    }

    public GameProgress GetProgress()
    {
        return Progress;
    }
}