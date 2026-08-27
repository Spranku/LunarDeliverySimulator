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
    private CameraController cameraController;
    private List<GameObject> roverButtons = new List<GameObject>();

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager3D>();
        cameraController = FindFirstObjectByType<CameraController>();

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
            return;
        }

        currentOrder = order;
        selectedRover = null;
        panel.SetActive(true);

        if (cameraController != null)
            cameraController.isUIActive = true;

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
        foreach (var btn in roverButtons)
        {
            if (btn != null) Destroy(btn);
        }
        roverButtons.Clear();

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
            return;
        }

        foreach (var rover in progress.Rovers)
        {
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

        if (!canDeliver && infoText != null && currentOrder != null)
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

        /* Exit focus with close panel */
        if (cameraController != null)
        {
            if (cameraController.IsFocusing())
            {
                cameraController.ExitFocusMode();
            }
            cameraController.isUIActive = false;
        }

        currentOrder = null;
        selectedRover = null;
    }

    public bool IsPanelOpen()
    {
        return panel != null && panel.activeSelf;
    }

    void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (deliverButton != null)
            deliverButton.onClick.RemoveListener(OnDeliverClicked);
    }
}