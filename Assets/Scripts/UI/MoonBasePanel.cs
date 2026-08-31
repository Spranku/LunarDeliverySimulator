using UnityEngine;
using UnityEngine.UI;

public class MoonBasePanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panel;
    public Text titleText;
    public Text statsText;
    public Text moneyText;

    [Header("Buttons")]
    public Button buyRoverButton;
    public Button upgradeBatteryButton;
    public Button upgradeCapacityButton;
    public Button upgradeSpeedButton;
    public Button expandBaseButton;
    public Button closeButton;

    [Header("Button Labels")]
    public Text buyRoverLabel;
    public Text upgradeBatteryLabel;
    public Text upgradeCapacityLabel;
    public Text upgradeSpeedLabel;
    public Text expandBaseLabel;

    private MoonBasePoint3D currentBase;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (buyRoverButton != null)
            buyRoverButton.onClick.AddListener(() => {
                currentBase?.BuyRover();
                UpdateUI();
            });

        if (upgradeBatteryButton != null)
            upgradeBatteryButton.onClick.AddListener(() => {
                currentBase?.UpgradeBattery();
                UpdateUI();
            });

        if (upgradeCapacityButton != null)
            upgradeCapacityButton.onClick.AddListener(() => {
                currentBase?.UpgradeCapacity();
                UpdateUI();
            });

        if (upgradeSpeedButton != null)
            upgradeSpeedButton.onClick.AddListener(() => {
                currentBase?.UpgradeSpeed();
                UpdateUI();
            });

        if (expandBaseButton != null)
            expandBaseButton.onClick.AddListener(() => {
                currentBase?.ExpandBase();
                UpdateUI();
            });

        if (panel != null)
            panel.SetActive(false);
    }

    public void ShowBase(MoonBasePoint3D basePoint)
    {
        currentBase = basePoint;
        panel.SetActive(true);
        UpdateUI();

        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
            cam.isUIActive = true;
    }

    void UpdateUI()
    {
        if (currentBase == null) return;

        var stats = currentBase.GetStats();

        if (titleText != null)
            titleText.text = $"🏗️ {currentBase.baseName} (Lv.{stats.level})";

        if (statsText != null)
        {
            statsText.text = $"Rovers: {stats.roverCount}\n" +
                             $"Battery: +{stats.batteryBonus:F0}%\n" +
                             $"Capacity: +{stats.capacityBonus:F0} kg\n" +
                             $"Speed: +{stats.speedBonus:F0}%\n" +
                             $"Orders: {stats.minOrders}-{stats.maxOrders}";
        }

        /* Money */
        if (moneyText != null)
            moneyText.text = $"{stats.money} credits";

        bool canAffordRover = currentBase.CanAfford(currentBase.roverCost);
        bool canAffordBattery = currentBase.CanAfford(currentBase.batteryUpgradeCost);
        bool canAffordCapacity = currentBase.CanAfford(currentBase.capacityUpgradeCost);
        bool canAffordSpeed = currentBase.CanAfford(currentBase.speedUpgradeCost);
        bool canAffordExpand = currentBase.CanAfford(currentBase.expandBaseCost);

        if (buyRoverButton != null)
        {
            buyRoverButton.interactable = canAffordRover;
            if (buyRoverLabel != null)
                buyRoverLabel.text = $"Buy Rover\n{currentBase.roverCost}";
        }

        if (upgradeBatteryButton != null)
        {
            upgradeBatteryButton.interactable = canAffordBattery;
            if (upgradeBatteryLabel != null)
                upgradeBatteryLabel.text = $"Upgrade Battery\n{currentBase.batteryUpgradeCost}";
        }

        if (upgradeCapacityButton != null)
        {
            upgradeCapacityButton.interactable = canAffordCapacity;
            if (upgradeCapacityLabel != null)
                upgradeCapacityLabel.text = $"Upgrade Capacity\n{currentBase.capacityUpgradeCost}";
        }

        if (upgradeSpeedButton != null)
        {
            upgradeSpeedButton.interactable = canAffordSpeed;
            if (upgradeSpeedLabel != null)
                upgradeSpeedLabel.text = $"Upgrade Speed\n{currentBase.speedUpgradeCost}";
        }

        if (expandBaseButton != null)
        {
            expandBaseButton.interactable = canAffordExpand;
            if (expandBaseLabel != null)
                expandBaseLabel.text = $"Expand Base\n{currentBase.expandBaseCost}";
        }
    }

    public void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
            cam.isUIActive = false;

        currentBase = null;
    }
}