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
    public float batteryBonus = 0.2f;   // +20%
    public float capacityBonus = 10f;   // +10 kg
    public float speedBonus = 0.2f;     // +20%
    public int extraOrders = 2;         // +2 orders

    private GameManager3D gameManager;
    private SphereCollider sphereCollider;
    private Renderer myRenderer;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = 1f;
        sphereCollider.isTrigger = true;

        myRenderer = GetComponent<Renderer>();
        gameManager = FindFirstObjectByType<GameManager3D>();
    }

    void OnMouseDown()
    {
        Debug.Log($"Base clicked: {baseName}");

        if (gameManager != null)
        {
            gameManager.SelectBase(this);
        }

        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            cam.FocusOnPoint(transform);
        }
    }

    /* ===== UPGRADE METHODS ===== */

    public bool CanAfford(int cost)
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return false;
        return progress.Money >= cost;
    }

    public void BuyRover()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return;

        if (!CanAfford(roverCost))
        {
            Debug.Log("Not enough credits to buy rover!");
            return;
        }

        progress.AddMoney(-roverCost);

        /* Create new rover */
        string[] names = { "Explorer", "Pioneer", "Voyager", "Ranger", "Nomad" };
        string newName = names[Random.Range(0, names.Length)] + "-" + (progress.Rovers.Count + 1);

        float baseBattery = 100f;
        float baseCapacity = 50f;
        float baseSpeed = 1f;

        /* Apply buffs */
        baseBattery *= (1f + batteryBonus);
        baseCapacity += capacityBonus;
        baseSpeed *= (1f + speedBonus);

        progress.Rovers.Add(new RoverData(newName, baseBattery, baseCapacity, baseSpeed));

        gameManager.VisualizeRovers();
        SaveManager.Save(progress);

        Debug.Log($"🚀 New rover purchased: {newName}!");
    }

    public void UpgradeBattery()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return;

        if (!CanAfford(batteryUpgradeCost))
        {
            Debug.Log("Not enough credits to upgrade battery!");
            return;
        }

        progress.AddMoney(-batteryUpgradeCost);
        batteryBonus += 0.1f; // ++bonus

        /* For all rovers */
        foreach (var rover in progress.Rovers)
        {
            rover.MaxBattery *= (1f + 0.1f);
            rover.CurrentBattery = rover.MaxBattery;
        }

        SaveManager.Save(progress);
        Debug.Log($"🔋 Battery upgrade complete! +10% to all rovers");
    }

    public void UpgradeCapacity()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return;

        if (!CanAfford(capacityUpgradeCost))
        {
            Debug.Log("Not enough credits to upgrade capacity!");
            return;
        }

        progress.AddMoney(-capacityUpgradeCost);
        capacityBonus += 5f;

        foreach (var rover in progress.Rovers)
        {
            rover.CargoCapacity += 5f;
        }

        SaveManager.Save(progress);
        Debug.Log($"📦 Capacity upgrade complete! +5kg to all rovers");
    }

    public void UpgradeSpeed()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return;

        if (!CanAfford(speedUpgradeCost))
        {
            Debug.Log("Not enough credits to upgrade speed!");
            return;
        }

        progress.AddMoney(-speedUpgradeCost);
        speedBonus += 0.1f;

        SaveManager.Save(progress);
        Debug.Log($"⚡ Speed upgrade complete! +10% speed to all rovers");
    }

    public void ExpandBase()
    {
        var progress = gameManager?.GetProgress();
        if (progress == null) return;

        if (!CanAfford(expandBaseCost))
        {
            Debug.Log("Not enough credits to expand base!");
            return;
        }

        progress.AddMoney(-expandBaseCost);
        level++;
        gameManager.minActiveOrders += extraOrders;
        gameManager.maxActiveOrders += extraOrders;

        SaveManager.Save(progress);
        Debug.Log($"🏗️ Base expanded to level {level}! More orders available");
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