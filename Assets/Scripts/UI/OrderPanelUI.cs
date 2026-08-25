using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OrderPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panel;
    public Text titleText;
    public Text infoText;
    public Text orderStatusText;
    public Transform roversListParent;
    public GameObject roverButtonPrefab;
    public Button deliverButton;
    public Button closeButton;

    [Header("Rover Button")]
    public Color availableColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    public Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    private OrderData currentOrder;
    private RoverData selectedRover;
    private GameManager3D gameManager;
    private List<GameObject> roverButtons = new List<GameObject>();

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager3D>();

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (deliverButton != null)
            deliverButton.onClick.AddListener(OnDeliverClicked);

        if (panel != null)
            panel.SetActive(false);
    }

    public void ShowOrder(OrderData order)
    {
        if (order == null)
        {
            Debug.LogError("Order is null!");
            return;
        }

        currentOrder = order;
        selectedRover = null;
        panel.SetActive(true);

        /* Load info */
        if (titleText != null)
            titleText.text = order.Title;

        if (infoText != null)
        {
            infoText.text = $"Weight: {order.Weight:F1} kg\n" +
                            $"Reward: {order.Reward} \n" +
                            $"Risk: {order.Risk * 100:F0}%\n" +
                            $"Zone: {order.ZoneType}\n" +
                            $"Urgency: {order.Urgency}/5\n" +
                            $"Deadline: day {order.DayDeadline}";
        }

        if (orderStatusText != null)
        {
            if (order.IsCompleted)
                orderStatusText.text = "Successed";
            else if (order.IsFailed)
                orderStatusText.text = "Unsuccessed";
            else
                orderStatusText.text = "Active";
        }

        RefreshRoversList();

        if (deliverButton != null)
            deliverButton.interactable = false;
    }

    void RefreshRoversList()
    {
        /* Удаляем старые кнопки */
        foreach (var btn in roverButtons)
        {
            if (btn != null) Destroy(btn);
        }
        roverButtons.Clear();

        /* Очищаем контейнер */
        if (roversListParent != null)
        {
            foreach (Transform child in roversListParent)
            {
                Destroy(child.gameObject);
            }
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager3D>();
            if (gameManager == null) return;
        }

        var progress = gameManager.GetProgress();
        if (progress == null)
        {
            Debug.LogError("Progress is null!");
            return;
        }

        Debug.Log($"Found {progress.Rovers.Count} rovers");

        foreach (var rover in progress.Rovers)
        {
            Debug.Log($"Rover: {rover.Name}, Destroyed: {rover.IsDestroyed}, Busy: {rover.IsBusy}");

            if (rover.IsDestroyed) continue;
            if (rover.IsBusy) continue;

            GameObject btn = Instantiate(roverButtonPrefab, roversListParent);
            RoverButtonUI buttonUI = btn.GetComponent<RoverButtonUI>();

            if (buttonUI != null)
            {
                buttonUI.Initialize(rover, OnRoverSelected);
                bool canDeliver = CanRoverDeliver(rover, currentOrder);
                buttonUI.SetAvailable(canDeliver);
            }

            roverButtons.Add(btn);
        }
    }

    bool CanRoverDeliver(RoverData rover, OrderData order)
    {
        if (rover == null || order == null) return false;
        if (rover.IsBusy || rover.IsDestroyed) return false;
        if (rover.CurrentBattery < order.Weight * 0.5f) return false;
        if (rover.CargoCapacity < order.Weight) return false;
        if (order.Risk > 0.7f) return false;
        return true;
    }

    void OnRoverSelected(RoverData rover)
    {
        selectedRover = rover;
        bool canDeliver = CanRoverDeliver(rover, currentOrder);

        if (deliverButton != null)
            deliverButton.interactable = canDeliver;

        /* Show error */
        if (!canDeliver && infoText != null)
        {
            string reason = "";
            if (rover.IsBusy) reason = "Rover busy!";
            else if (rover.IsDestroyed) reason = "Rover broken!";
            else if (rover.CurrentBattery < currentOrder.Weight * 0.5f)
                reason = $"Low battery! (needs: {currentOrder.Weight * 0.5f:F0})";
            else if (rover.CargoCapacity < currentOrder.Weight)
                reason = $"Too hard! (max: {rover.CargoCapacity} kg)";
            else if (currentOrder.Risk > 0.7f)
                reason = "Too dangerous! (risk > 70%)";

            infoText.text += $"\n\n❌ {reason}";
        }
    }

    void OnDeliverClicked()
    {
        if (currentOrder == null || selectedRover == null) return;

        if (gameManager != null)
        {
            gameManager.StartDelivery(selectedRover, currentOrder);
        }

        ClosePanel();
    }

    void ClosePanel()
    {
        if (panel != null)
            panel.SetActive(false);

        currentOrder = null;
        selectedRover = null;
    }

    void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (deliverButton != null)
            deliverButton.onClick.RemoveListener(OnDeliverClicked);
    }
}