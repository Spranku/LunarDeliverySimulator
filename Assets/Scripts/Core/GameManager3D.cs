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

    private List<OrderPoint3D> orderPoints = new List<OrderPoint3D>();
    private List<RoverVisual> roverVisuals = new List<RoverVisual>();
    private BasePoint currentBase;

    private float dayTimer = 0f;
    private float generationTimer = 0f;
    private bool isDayEnding = false;

    void Awake()
    {
        //SaveManager.DeleteSave(); // TEMP

        if (!SaveManager.SaveExists())
        {
            Progress = new GameProgress();
            Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
            Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
            Progress.Rovers.Add(new RoverData("Bigfoot", 150f, 100f, 0.7f));
            GenerateOrders(5);
            SaveManager.Save(Progress);
            Debug.Log("First launch: New game created!");
        }
        else
        {
            Progress = SaveManager.Load();
            Debug.Log("Game loaded!");
        }

        if (Progress.Rovers.Count == 0)
        {
            Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
            Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
            Progress.Rovers.Add(new RoverData("Bigfoot", 150f, 100f, 0.7f));
            SaveManager.Save(Progress);
        }

        
        if (Progress.Orders.Count == 0)
        {
            GenerateOrders(5);
            SaveManager.Save(Progress);
        }

        SetupBase();
        VisualizeOrders();
        VisualizeRovers();

        dayTimer = dayDuration;
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
            StartCoroutine(EndDay());
    }

    #region Base

    void SetupBase()
    {
        if (basePrefab == null || moonSurface == null) return;

        Vector3 direction = Vector3.up;
        float radius = moonSurface.localScale.x * 0.5f;
        Vector3 position = moonSurface.position + direction * (radius + 0.02f);

        GameObject baseObj = Instantiate(basePrefab, moonSurface);
        baseObj.transform.position = position;
        baseObj.transform.LookAt(moonSurface.position);

        currentBase = baseObj.GetComponent<BasePoint>();
        if (currentBase == null)
            currentBase = baseObj.AddComponent<BasePoint>();
    }

    public void SelectBase(MoonBasePoint3D basePoint)
    {
        CameraController cam = FindFirstObjectByType<CameraController>();
        cam?.FocusOnPoint(basePoint.transform);

        if (basePanel != null)
            basePanel.ShowBase(basePoint);
        else
            Debug.LogError("BasePanel is not assigned!");
    }

    #endregion

    #region Rovers

    public void VisualizeRovers()
    {
        foreach (var v in roverVisuals)
            if (v != null) Destroy(v.gameObject);

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

            GameObject go = Instantiate(roverPrefab, roverParent);
            RoverVisual visual = go.GetComponent<RoverVisual>();
            if (visual == null)
                visual = go.AddComponent<RoverVisual>();

            rover.CurrentPosition = basePos;
            visual.Initialize(rover, moonSurface, basePos);
            roverVisuals.Add(visual);
            go.SetActive(false);
        }
    }

    #endregion

    #region Orders

    void GenerateOrders(int count)
    {
        string[] titles = { "Food", "CO2", "Machines", "Materials", "Medkits" };
        string[] zones = { "Low", "Medium", "High" };

        for (int i = 0; i < count; i++)
        {
            string zone = zones[Random.Range(0, zones.Length)];
            float risk = zone == "Low" ? 0.1f : zone == "Medium" ? 0.4f : 0.8f;

            Progress.Orders.Add(new OrderData(
                titles[Random.Range(0, titles.Length)],
                Random.Range(10f, 80f),
                Random.Range(70, 400),
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
        // Remove orphaned points
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null)
            {
                orderPoints.RemoveAt(i);
                continue;
            }

            OrderData order = orderPoints[i].Order;
            if (order == null || !Progress.Orders.Contains(order))
            {
                if (orderPoints[i].gameObject != null)
                    Destroy(orderPoints[i].gameObject);
                orderPoints.RemoveAt(i);
            }
        }

        // Create new points
        foreach (var order in Progress.Orders)
        {
            if (order.IsCompleted || order.IsFailed) continue;
            if (orderPoints.Exists(p => p.Order == order)) continue;

            GameObject point = Instantiate(orderPointPrefab3D, moonSurface);
            OrderPoint3D script = point.GetComponent<OrderPoint3D>();
            script.Initialize(order, moonSurface);

            if (order.IsBusy)
                script.SetColor(Color.orange);

            orderPoints.Add(script);
        }
    }

    void UpdateOrderVisuals()
    {
        for (int i = orderPoints.Count - 1; i >= 0; i--)
        {
            if (orderPoints[i] == null) continue;

            OrderPoint3D point = orderPoints[i];
            if (point.Order == null) continue;

            if (point.Order.IsCompleted || point.Order.IsFailed)
            {
                Destroy(point.gameObject);
                orderPoints.RemoveAt(i);
            }
            else if (point.Order.IsBusy)
            {
                point.SetColor(Color.orange);
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

    public void CleanupCompletedOrders()
    {
        int removed = 0;

        for (int i = Progress.Orders.Count - 1; i >= 0; i--)
        {
            var order = Progress.Orders[i];
            if (order.IsCompleted || order.IsFailed)
            {
                Progress.Orders.RemoveAt(i);
                removed++;
            }
        }

        if (removed > 0)
        {
            SaveManager.Save(Progress);
            VisualizeOrders();
        }
    }

    #endregion

    #region Day Cycle

    void CheckAndGenerateOrders()
    {
        int activeOrders = 0;
        foreach (var order in Progress.Orders)
        {
            if (!order.IsCompleted && !order.IsFailed && !order.IsBusy)
                activeOrders++;
        }

        // Soft lock check
        if (activeOrders > 0 && !HasDeliverableOrder())
        {
            Debug.LogWarning("No deliverable orders found! Regenerating...");

            for (int i = Progress.Orders.Count - 1; i >= 0; i--)
            {
                var order = Progress.Orders[i];
                if (!order.IsCompleted && !order.IsFailed && !order.IsBusy)
                    Progress.Orders.RemoveAt(i);
            }

            GenerateOrders(minActiveOrders);
            VisualizeOrders();
            SaveManager.Save(Progress);
            return;
        }

        if (activeOrders < minActiveOrders)
        {
            int toGenerate = Random.Range(1, maxActiveOrders - activeOrders + 1);
            if (toGenerate < 1) toGenerate = 1;

            GenerateOrders(toGenerate);
            VisualizeOrders();
        }
    }

    bool HasDeliverableOrder()
    {
        foreach (var order in Progress.Orders)
        {
            if (order.IsCompleted || order.IsFailed || order.IsBusy) continue;

            foreach (var rover in Progress.Rovers)
            {
                if (rover.IsDestroyed || rover.IsBusy) continue;
                if (rover.CargoCapacity >= order.Weight && rover.CurrentBattery >= order.Weight * 0.5f)
                    return true;
            }
        }
        return false;
    }

    IEnumerator EndDay()
    {
        isDayEnding = true;

        CheckOverdueOrders();
        CleanupCompletedOrders();
        Progress.Day++;
        ChargeRovers();
        CheckGameState();

        SaveManager.Save(Progress);
        HUDManager.Instance?.UpdateUI();

        dayTimer = dayDuration;
        generationTimer = 0f;
        isDayEnding = false;

        yield return null;
    }

    void CheckOverdueOrders()
    {
        int count = 0;

        for (int i = Progress.Orders.Count - 1; i >= 0; i--)
        {
            var order = Progress.Orders[i];
            if (order.IsCompleted || order.IsFailed || order.IsBusy) continue;

            if (order.IsOverdue(Progress.Day))
            {
                count++;
                order.IsFailed = true;

                int penalty = Mathf.RoundToInt(order.Reward * 0.2f);
                Progress.AddMoney(-penalty);
                Progress.ChangeRating(-5f);

                var point = orderPoints.Find(p => p.Order == order);
                point?.SetColor(Color.orange);
            }
        }

        if (count > 0)
            Debug.Log($"Total overdue orders: {count}");
    }

    void ChargeRovers()
    {
        foreach (var rover in Progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            rover.ChargeBattery(rover.MaxBattery * 0.2f);
            rover.IsBusy = false;
        }
    }

    #endregion

    #region Game State

    public void CheckGameState()
    {
        int alive = 0;
        foreach (var rover in Progress.Rovers)
        {
            if (!rover.IsDestroyed) alive++;
        }

        if (alive == 0)
            ShowGameOver("All rovers have been destroyed!");
        else if (Progress.BaseRating <= 0)
            ShowGameOver("Base rating dropped to 0!");
        else if (Progress.Money < 0)
            ShowGameOver("Bankruptcy! No funds to operate.");
        else if (Progress.BaseRating >= 100f)
            ShowVictory("Base rating reached 100%!");
        else if (Progress.Money >= 10000)
            ShowVictory("You earned 10000 credits!");
    }

    void ShowGameOver(string msg)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
            if (infoText != null) infoText.text = "💀 GAME OVER";
            if (messageText != null) messageText.text = msg;
        }
        Time.timeScale = 0f;
    }

    void ShowVictory(string msg)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
            if (infoText != null) infoText.text = "🎉 VICTORY!";
            if (messageText != null) messageText.text = msg;
        }
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SaveManager.DeleteSave();

        if (gameResultPanel != null)
            gameResultPanel.SetActive(false);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Order Selection & Delivery

    public void SelectOrder(OrderData order)
    {
        var point = orderPoints.Find(p => p.Order == order);
        if (point != null)
        {
            CameraController cam = FindFirstObjectByType<CameraController>();
            cam?.FocusOnPoint(point.transform);
        }

        orderPanel?.ShowOrder(order);
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

        orderPanel?.ClosePanel();
        targetPoint.SetColor(Color.orange);

        roverVis.onRoverDestroyed = () =>
        {
            Progress.ChangeRating(-10f);
            Progress.TotalDeliveriesFailed++;

            order.IsBusy = false;
            order.IsFailed = true;
            targetPoint.SetColor(GetOrderColor(order));

            HUDManager.Instance?.UpdateUI();
            if (HUDManager.Instance.IsOrdersPanelOpen())
                HUDManager.Instance.UpdateOrdersPanel();

            SaveManager.Save(Progress);
            CheckGameState();
        };

        roverVis.onDeliveryComplete = () =>
        {
            if (rover.IsDestroyed) return;

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

            HUDManager.Instance?.UpdateUI();
            if (HUDManager.Instance.IsOrdersPanelOpen())
                HUDManager.Instance.UpdateOrdersPanel();

            SaveManager.Save(Progress);
        };

        roverVis.MoveTo(targetPoint.transform, order.Weight);
        SaveManager.Save(Progress);
    }

    #endregion

    #region Helpers

    public void RegisterBase(BasePoint basePoint)
    {
        currentBase = basePoint;
    }

    public GameProgress GetProgress() => Progress;

    #endregion
}