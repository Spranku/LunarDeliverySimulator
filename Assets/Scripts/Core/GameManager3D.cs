using UnityEngine;
using UnityEngine.UI;
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

    [Header("UI")]
    public OrderPanelUI orderPanel;

    [Header("Day Cycle")]
    public float dayDuration = 60f;
    public int minActiveOrders = 3;
    public int maxActiveOrders = 5;
    public float orderGenerationInterval = 10f;

    [Header("UI Panels")]
    public GameObject gameResultPanel;
    public Text infoText;        
    public Text messageText;     
    public Button restartButton;
    public Button exitButton;

    [Header("Base")]
    public GameObject basePrefab;
    public MoonBasePanelUI basePanel;  
    private MoonBasePoint3D currentBasePoint;

    private List<OrderPoint3D> orderPoints = new List<OrderPoint3D>();
    private List<RoverVisual> roverVisuals = new List<RoverVisual>();
    private BasePoint currentBase;

    private float dayTimer = 0f;
    private float generationTimer = 0f;
    private bool isDayEnding = false;

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

        CheckGameState();
    }

    void Start()
    {
        if (gameResultPanel != null)
            gameResultPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
    {
        if (isDayEnding) return;

        dayTimer -= Time.deltaTime;

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

    #region BASE SETUP

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

    public void SelectBase(MoonBasePoint3D basePoint)
    {
        Debug.Log($"Selected base: {basePoint.baseName}");

        if (basePanel != null)
        {
            basePanel.ShowBase(basePoint);
        }
    }

    public void VisualizeBase()
    {
        if (basePrefab == null || moonSurface == null) return;

        Vector3 baseDirection = new Vector3(0, 1, 0).normalized;
        float radius = moonSurface.localScale.x * 0.5f;
        float offset = 0.15f;
        Vector3 basePosition = moonSurface.position + baseDirection * (radius + offset);

        GameObject baseObject = Instantiate(basePrefab, moonSurface);
        baseObject.transform.position = basePosition;
        baseObject.transform.LookAt(moonSurface.position);

        currentBasePoint = baseObject.GetComponent<MoonBasePoint3D>();
        if (currentBasePoint == null)
            currentBasePoint = baseObject.AddComponent<MoonBasePoint3D>();
    }

    #endregion

    #region ROVERS

    public void VisualizeRovers()
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
            roverGO.SetActive(false);
        }
    }

    #endregion

    #region ORDERS

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
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null) continue;

            OrderData order = orderPoints[i].Order;
            if (order == null) continue;

            if (order.IsCompleted || order.IsFailed)
            {
                Destroy(orderPoints[i].gameObject);
                orderPoints.RemoveAt(i);
            }
        }

        foreach (var order in Progress.Orders)
        {
            if (order.IsCompleted || order.IsFailed) continue;

            bool exists = orderPoints.Exists(p => p.Order == order);
            if (exists) continue;

            GameObject point = Instantiate(orderPointPrefab3D, moonSurface);
            OrderPoint3D pointScript = point.GetComponent<OrderPoint3D>();
            pointScript.Initialize(order, moonSurface);

            if (order.IsBusy)
            {
                pointScript.SetColor(Color.gray);
            }

            orderPoints.Add(pointScript);
        }
    }

    void UpdateOrderVisuals()
    {
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null) continue;

            OrderPoint3D point = orderPoints[i];
            if (point != null && point.Order != null)
            {
                if (point.Order.IsCompleted || point.Order.IsFailed)
                {
                    Destroy(point.gameObject);
                    orderPoints.RemoveAt(i);
                }
                else if (point.Order.IsBusy)
                {
                    point.SetColor(Color.gray);
                }
            }
        }
    }

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

    #endregion

    #region DAY CYCLE
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

        yield return null;
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

    #endregion

    #region GAME STATE
    public void CheckGameState()
    {
        int aliveRovers = 0;
        foreach (var rover in Progress.Rovers)
        {
            if (!rover.IsDestroyed) aliveRovers++;
        }

        if (aliveRovers == 0)
        {
            Debug.Log("💀 GAME OVER: All rovers destroyed!");
            ShowGameOver("All rovers have been destroyed!\nBase operations failed.");
            return;
        }

        if (Progress.BaseRating <= 0)
        {
            Debug.Log("💀 GAME OVER: Base rating reached 0!");
            ShowGameOver("Base rating dropped to 0!\nThe base has fallen.");
            return;
        }

        if (Progress.Money < 0)
        {
            Debug.Log("💀 GAME OVER: Money is negative!");
            ShowGameOver("Bankruptcy!\nNo funds to operate.");
            return;
        }

        if (Progress.BaseRating >= 100f)
        {
            Debug.Log("🎉 VICTORY: Base rating reached 100%!");
            ShowVictory("Base rating reached 100%!\nYou are the best lunar commander!");
            return;
        }

        if (Progress.Money >= 10000)
        {
            Debug.Log("🎉 VICTORY: Money reached 10000 credits!");
            ShowVictory("You earned 10000 credits!\nA true lunar tycoon!");
            return;
        }
    }

    void ShowGameOver(string message)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
            if (infoText != null)
                infoText.text = "💀 GAME OVER";
            if (messageText != null)
                messageText.text = message;
        }

        Time.timeScale = 0f;
    }

    void ShowVictory(string message)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
            if (infoText != null)
                infoText.text = "🎉 VICTORY!";
            if (messageText != null)
                messageText.text = message;
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (gameResultPanel != null)
            gameResultPanel.SetActive(false);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("Back to menu");
    }

    #endregion

    /* ===== EXIT ===== */
    public void ExitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Exit game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    /* ===== SELECT ORDER ===== */

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
        }
    }

    /* ===== START DELIVERY ===== */

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

        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;
        order.IsBusy = true;

        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            if (cam.IsFocusing()) cam.ExitFocusMode();
            cam.isUIActive = false;
        }

        if (orderPanel != null && orderPanel.IsPanelOpen())
        {
            orderPanel.ClosePanel();
        }

        if (targetPoint != null)
        {
            targetPoint.SetColor(Color.gray);
        }

        roverVis.onRoverDestroyed = () => {
            Progress.ChangeRating(-10f);
            Progress.TotalDeliveriesFailed++;

            /* Change order status */
            order.IsBusy = false;
            order.IsFailed = true;

            if (targetPoint != null)
            {
                targetPoint.SetColor(GetOrderColor(order));
            }

            /* Update UI */
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateUI();
                if (HUDManager.Instance.IsOrdersPanelOpen())
                {
                    HUDManager.Instance.UpdateOrdersPanel();
                }
            }

            SaveManager.Save(Progress);
            Debug.Log($"💀 Rover lost! Rating -10");

            CheckGameState();
        };

        roverVis.onDeliveryComplete = () => {
            if (!rover.IsDestroyed)
            {
                Progress.AddMoney(order.Reward);
                Progress.TotalDeliveriesCompleted++;
                Progress.ChangeRating(2f);

                order.IsCompleted = true;
                order.IsBusy = false;

                if (targetPoint != null)
                {
                    Destroy(targetPoint.gameObject);
                    orderPoints.Remove(targetPoint);
                }

                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.UpdateUI();
                    if (HUDManager.Instance.IsOrdersPanelOpen())
                    {
                        HUDManager.Instance.UpdateOrdersPanel();
                    }
                }

                SaveManager.Save(Progress);
                Debug.Log($"✅ Delivery complete! +{order.Reward} credits, +2 rating");
            }
        };

        roverVis.MoveTo(targetPoint.transform, order.Weight);
        SaveManager.Save(Progress);
    }

    /* ===== REGISTER BASE ===== */

    public void RegisterBase(BasePoint basePoint)
    {
        currentBase = basePoint;
    }

    /* ===== GET PROGRESS ===== */

    public GameProgress GetProgress()
    {
        return Progress;
    }
}