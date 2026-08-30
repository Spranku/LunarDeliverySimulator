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

    [Header("Day Cycle")]
    public float dayDuration = 60f;          
    public int minActiveOrders = 3;          
    public int maxActiveOrders = 5;          
    public float orderGenerationInterval = 10f; 

    private float dayTimer = 0f;
    private float generationTimer = 0f;
    private bool isDayEnding = false;

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

        dayTimer = dayDuration;
        generationTimer = 0f;
    }

    void Update()
    {
        if (isDayEnding) return;

        dayTimer -= Time.deltaTime;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDayTimer(dayTimer);
        }

        generationTimer += Time.deltaTime;
        if (generationTimer >= orderGenerationInterval)
        {
            generationTimer = 0f;
            CheckAndGenerateOrders();
        }

        if (dayTimer <= 0f)
        {
            StartCoroutine(EndDay());
        }
    }

    void CheckAndGenerateOrders()
    {
        int activeOrders = 0;
        foreach (var order in Progress.Orders)
        {
            if (!order.IsCompleted && !order.IsFailed && !order.IsBusy)
            {
                activeOrders++;
            }
        }

        if (activeOrders < minActiveOrders)
        {
            int ordersToGenerate = Random.Range(1, maxActiveOrders - activeOrders + 1);
            if (ordersToGenerate < 1) ordersToGenerate = 1;

            Debug.Log($"📦 Generating {ordersToGenerate} new orders (active: {activeOrders}/{minActiveOrders})");
            GenerateOrders(ordersToGenerate);
            VisualizeOrders();
        }
    }

    void CheckOverdueOrders()
    {
        int overdueCount = 0;

        for (int i = Progress.Orders.Count - 1; i >= 0; i--)
        {
            OrderData order = Progress.Orders[i];

            if (order.IsCompleted || order.IsFailed) continue;
            if (order.IsBusy) continue;

            if (order.IsOverdue(Progress.Day))
            {
                overdueCount++;
                order.IsFailed = true;

                int penalty = Mathf.RoundToInt(order.Reward * 0.2f);
                Progress.AddMoney(-penalty);
                Progress.ChangeRating(-5f);

                OrderPoint3D point = orderPoints.Find(p => p.Order == order);
                if (point != null)
                {
                    point.SetColor(Color.gray);
                }

                Debug.Log($"Order '{order.Title}' overdue! Penalty: -{penalty} credits, -5 rating");
            }
        }

        if (overdueCount > 0)
        {
            Debug.Log($"Total overdue orders: {overdueCount}");
        }
    }

    void ChargeRovers()
    {
        foreach (var rover in Progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            float chargeAmount = rover.MaxBattery * 0.2f;
            rover.ChargeBattery(chargeAmount);
            rover.IsBusy = false;

            Debug.Log($"Rover {rover.Name} charged to {rover.CurrentBattery:F0}/{rover.MaxBattery:F0}");
        }
    }

    int GetActiveOrdersCount()
    {
        int count = 0;
        foreach (var order in Progress.Orders)
        {
            if (!order.IsCompleted && !order.IsFailed && !order.IsBusy)
            {
                count++;
            }
        }
        return count;
    }

    void CheckGameState()
    {
        if (Progress.BaseRating <= 0)
        {
            Debug.Log("💀 GAME OVER: Base rating reached 0!");
            /* TODO:  Game Over */
        }
        else if (Progress.Money < 0)
        {
            Debug.Log("💀 GAME OVER: Money is negative!");
            /* TODO:  Game Over */
        }
        else if (Progress.BaseRating >= 100f)
        {
            Debug.Log("🎉 VICTORY: Base rating reached 100%!");
            /* TODO:  Victory */
        }
        else if (Progress.Money >= 10000)
        {
            Debug.Log("🎉 VICTORY: Money reached 10000 credits!");
            /* TODO:  Victory */
        }
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
            if (rover.IsDestroyed) continue;

            GameObject roverGO = Instantiate(roverPrefab, roverParent);
            RoverVisual visual = roverGO.GetComponent<RoverVisual>();

            if (visual == null)
                visual = roverGO.AddComponent<RoverVisual>();

            
            rover.CurrentPosition = basePos;
            visual.Initialize(rover, moonSurface, basePos);
            roverVisuals.Add(visual);

            /* Hidden rover on the moon base */
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
            /* ===== dont spawn competed orders ===== */
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

        /* Focus camera on the order point */
        OrderPoint3D targetPoint = orderPoints.Find(p => p.Order == order);
        if (targetPoint != null)
        {
            CameraController cam = FindFirstObjectByType<CameraController>();
            if (cam != null)
            {
                cam.FocusOnPoint(targetPoint.transform);
            }
        }

        /* Show order panel */
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
        /* Check order complete */
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

        /* battery */
        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;

        /* mark as complete order */
        order.IsCompleted = true;
        order.IsBusy = true;

        /* exit focus */
        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            if (cam.IsFocusing()) cam.ExitFocusMode();
            cam.isUIActive = false;
        }

        /* close panel */
        if (orderPanel != null && orderPanel.IsPanelOpen())
        {
            orderPanel.ClosePanel();
        }

        /* ===== change color for processing order ===== */
        if (targetPoint != null)
        {
            targetPoint.SetColor(Color.gray);
        }

        roverVis.onDeliveryComplete = () => {
            Progress.AddMoney(order.Reward);
            Progress.TotalDeliveriesCompleted++;
            Progress.ChangeRating(2f);

            /* ===== Delete point  ===== */
            if (targetPoint != null)
            {
                Destroy(targetPoint.gameObject);
                orderPoints.Remove(targetPoint);
            }

            SaveManager.Save(Progress);
            Debug.Log($"✅ Delivery complete! +{order.Reward} credits, +2 rating");
        };

        roverVis.MoveTo(targetPoint.transform);

        SaveManager.Save(Progress);
        Debug.Log($"🚀 Rover {rover.Name} sent to {order.Title}!");
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

    IEnumerator EndDay()
    {
        isDayEnding = true;

        Debug.Log($"=== DAY {Progress.Day} ENDING ===");

        CheckOverdueOrders();

        Progress.Day++;

        ChargeRovers();

        CheckGameState();

        SaveManager.Save(Progress);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateUI();
        }

        dayTimer = dayDuration;
        generationTimer = 0f;
        isDayEnding = false;

        Debug.Log($"=== DAY {Progress.Day} STARTED ===");
        Debug.Log($"Active orders: {GetActiveOrdersCount()}, Money: {Progress.Money}, Rating: {Progress.BaseRating:F0}%");

        yield return null;
    }
}