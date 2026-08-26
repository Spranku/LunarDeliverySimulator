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

        foreach (var rover in Progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            GameObject roverGO = Instantiate(roverPrefab, roverParent);
            RoverVisual visual = roverGO.GetComponent<RoverVisual>();

            if (visual == null)
                visual = roverGO.AddComponent<RoverVisual>();

            /* Moon base - start position */
            rover.CurrentPosition = currentBase.transform.position;
            visual.Initialize(rover, moonSurface);
            roverVisuals.Add(visual);

            /* Hidden rover on base */
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
        Debug.Log($"Order choiced: {order.Title}");

        if (orderPanel != null)
        {
            orderPanel.ShowOrder(order);
        }
    }

    public void StartDelivery(RoverData rover, OrderData order)
    {
        /* Find order point */
        OrderPoint3D targetPoint = orderPoints.Find(p => p.Order == order);
        if (targetPoint == null)
        {
            Debug.LogError("Target point not found!");
            return;
        }

        /* Find rover visual */
        RoverVisual roverVis = roverVisuals.Find(r => r.data == rover);
        if (roverVis == null)
        {
            Debug.LogError("Rover visual not found!");
            return;
        }

        /* Battery */
        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;

        // ===== Exit focus, unlock camera =====
        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            if (cam.IsFocusing())
            {
                cam.ExitFocusMode();
                Debug.Log("Force exit focus mode!");
            }
            cam.isUIActive = false;
            Debug.Log("Camera unlocked!");
        }

        /* Show rover */
        roverVis.gameObject.SetActive(true);

        /* Bind to finish event */
        roverVis.onDeliveryComplete = () => {
            order.IsCompleted = true;
            Progress.AddMoney(order.Reward);
            Progress.TotalDeliveriesCompleted++;

            UpdateOrderVisuals();
            roverVis.gameObject.SetActive(false);

            SaveManager.Save(Progress);

            Debug.Log($"Delivery finished! +{order.Reward} credits");
        };

        /* Send to point */
        roverVis.MoveTo(targetPoint.transform.position);

        SaveManager.Save(Progress);

        Debug.Log($"Rover {rover.Name} sender to order {order.Title}!");
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

    public void RegisterBase(BasePoint basePoint)
    {
        currentBase = basePoint;
    }

    public GameProgress GetProgress()
    {
        return Progress;
    }
}