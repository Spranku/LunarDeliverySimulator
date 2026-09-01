using UnityEngine;

public class MoonBasePoint3D : MonoBehaviour
{
    [Header("Base Settings")]
    public string baseName = "Lunar Base";
    public int level = 1;

    [Header("Upgrade Costs")]
    public int roverCost = 500;
    public int batteryUpgradeCost = 800;
    public int capacityUpgradeCost = 1000;
    public int speedUpgradeCost = 600;
    public int expandBaseCost = 1200;

    [Header("Upgrade Values")]
    public float batteryBonus = 0.2f;
    public float capacityBonus = 10f;
    public float speedBonus = 0.2f;
    public int extraOrders = 2;

    private GameManager3D gameManager;
    private SphereCollider sphereCollider;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = 1f;
        sphereCollider.isTrigger = true;

        gameManager = FindFirstObjectByType<GameManager3D>();
    }

    void OnMouseDown()
    {
        MoonBasePanelUI panel = FindFirstObjectByType<MoonBasePanelUI>();
        bool isPanelOpen = panel != null && panel.panel != null && panel.panel.activeSelf;

        if (isPanelOpen)
        {
            panel.ClosePanel();
            return;
        }

        if (gameManager != null)
            gameManager.SelectBase(this);
    }

    public bool CanAfford(int cost)
    {
        var progress = gameManager?.GetProgress();
        return progress != null && progress.Money >= cost;
    }

    public void BuyRover()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null || !CanAfford(roverCost)) return;

        progress.AddMoney(-roverCost);

        string[] names = { "Explorer", "Pioneer", "Voyager", "Ranger", "Nomad" };
        string newName = names[Random.Range(0, names.Length)] + "-" + (progress.Rovers.Count + 1);

        float baseBattery = 100f * (1f + batteryBonus);
        float baseCapacity = 50f + capacityBonus;
        float baseSpeed = 1f * (1f + speedBonus);

        progress.Rovers.Add(new RoverData(newName, baseBattery, baseCapacity, baseSpeed));

        gameManager.VisualizeRovers();
        SaveManager.Save(progress);
    }

    public void UpgradeBattery()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null || !CanAfford(batteryUpgradeCost)) return;

        progress.AddMoney(-batteryUpgradeCost);
        batteryBonus += 0.1f;

        foreach (var rover in progress.Rovers)
        {
            rover.MaxBattery *= 1.1f;
            rover.CurrentBattery = rover.MaxBattery;
        }

        SaveManager.Save(progress);
    }

    public void UpgradeCapacity()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null || !CanAfford(capacityUpgradeCost)) return;

        progress.AddMoney(-capacityUpgradeCost);
        capacityBonus += 5f;

        foreach (var rover in progress.Rovers)
        {
            rover.CargoCapacity += 5f;
        }

        SaveManager.Save(progress);
    }

    public void UpgradeSpeed()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null || !CanAfford(speedUpgradeCost)) return;

        progress.AddMoney(-speedUpgradeCost);
        speedBonus += 0.1f;

        SaveManager.Save(progress);
    }

    public void ExpandBase()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null || !CanAfford(expandBaseCost)) return;

        progress.AddMoney(-expandBaseCost);
        level++;

        gameManager.minActiveOrders += extraOrders;
        gameManager.maxActiveOrders += extraOrders;

        SaveManager.Save(progress);
    }

    public BaseStats GetStats()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return new BaseStats();

        return new BaseStats
        {
            level = level,
            roverCount = progress.Rovers.Count,
            money = progress.Money,
            batteryBonus = batteryBonus * 100,
            capacityBonus = capacityBonus,
            speedBonus = speedBonus * 100,
            minOrders = gameManager.minActiveOrders,
            maxOrders = gameManager.maxActiveOrders
        };
    }
}

public struct BaseStats
{
    public int level;
    public int roverCount;
    public int money;
    public float batteryBonus;
    public float capacityBonus;
    public float speedBonus;
    public int minOrders;
    public int maxOrders;
}