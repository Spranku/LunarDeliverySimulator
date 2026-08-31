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

    private MoonBasePoint3D currentBase;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (buyRoverButton != null)
            buyRoverButton.onClick.AddListener(() => { currentBase?.BuyRover(); UpdateUI(); });

        if (upgradeBatteryButton != null)
            upgradeBatteryButton.onClick.AddListener(() => { currentBase?.UpgradeBattery(); UpdateUI(); });

        if (upgradeCapacityButton != null)
            upgradeCapacityButton.onClick.AddListener(() => { currentBase?.UpgradeCapacity(); UpdateUI(); });

        if (upgradeSpeedButton != null)
            upgradeSpeedButton.onClick.AddListener(() => { currentBase?.UpgradeSpeed(); UpdateUI(); });

        if (expandBaseButton != null)
            expandBaseButton.onClick.AddListener(() => { currentBase?.ExpandBase(); UpdateUI(); });

        panel.SetActive(false);
    }

    public void ShowBase(MoonBasePoint3D basePoint)
    {
        currentBase = basePoint;
        panel.SetActive(true);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (currentBase == null) return;

        var stats = currentBase.GetStats();

        if (titleText != null)
            titleText.text = $"🏗️ {currentBase.baseName} (Lv.{stats.level})";

        if (statsText != null)
        {
            statsText.text = $"🚀 Rovers: {stats.roverCount}\n" +
                             $"🔋 Battery Bonus: +{stats.batteryBonus:F0}%\n" +
                             $"📦 Capacity Bonus: +{stats.capacityBonus:F0} kg\n" +
                             $"⚡ Speed Bonus: +{stats.speedBonus:F0}%\n" +
                             $"📋 Orders: {stats.minOrders}-{stats.maxOrders}";
        }

        if (moneyText != null)
            moneyText.text = $"💰 {stats.money} credits";

        if (buyRoverButton != null)
            buyRoverButton.interactable = currentBase.CanAfford(currentBase.roverCost);

        if (upgradeBatteryButton != null)
            upgradeBatteryButton.interactable = currentBase.CanAfford(currentBase.batteryUpgradeCost);

        if (upgradeCapacityButton != null)
            upgradeCapacityButton.interactable = currentBase.CanAfford(currentBase.capacityUpgradeCost);

        if (upgradeSpeedButton != null)
            upgradeSpeedButton.interactable = currentBase.CanAfford(currentBase.speedUpgradeCost);

        if (expandBaseButton != null)
            expandBaseButton.interactable = currentBase.CanAfford(currentBase.expandBaseCost);
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        currentBase = null;
    }
}