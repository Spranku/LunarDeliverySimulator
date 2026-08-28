using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OrderPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panel;
    public GameObject overlay;
    public float animationDuration = 0.3f;

    [Header("Content")]
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
    private HUDManager hudManager;
    private List<GameObject> roverButtons = new List<GameObject>();

    private RectTransform panelRect;
    private Vector2 closedPos;
    private Vector2 openPos;
    private Coroutine animCoroutine;
    private bool isOpen = false;
    private float openTime = 0f;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager3D>();
        cameraController = FindFirstObjectByType<CameraController>();
        hudManager = FindFirstObjectByType<HUDManager>();

        if (panel != null)
        {
            panelRect = panel.GetComponent<RectTransform>();
            float width = 350f;
            if (panelRect != null && panelRect.rect.width > 0)
                width = panelRect.rect.width;

            closedPos = new Vector2(-width, 0);
            openPos = Vector2.zero;
            panelRect.anchoredPosition = closedPos;
            panel.SetActive(false);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (deliverButton != null)
            deliverButton.onClick.AddListener(OnDeliverClicked);

        isOpen = false;
    }

    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void ShowOrder(OrderData order)
    {
        if (order == null)
        {
            Debug.LogError("Order is null!");
            return;
        }

        /* ===== Dont show order if in progress ===== */
        if (order.IsCompleted || order.IsBusy)
        {
            Debug.Log($"Order {order.Title} is already completed or in progress!");

            if (IsPanelOpen())
            {
                ClosePanel();
            }
            return;
        }

        currentOrder = order;
        selectedRover = null;

        if (cameraController != null)
            cameraController.isUIActive = true;

        if (!isOpen)
        {
            OpenPanel();
        }

        if (titleText != null)
            titleText.text = order.Title;

        if (infoText != null)
        {
            infoText.text = $"Weight: {order.Weight:F1} kg\n" +
                            $"Reward: {order.Reward} credits\n" +
                            $"Risk: {order.Risk * 100:F0}%\n" +
                            $"Zone: {order.ZoneType}\n" +
                            $"Urgency: {order.Urgency}/5\n" +
                            $"Deadline: day {order.DayDeadline}";
        }

        if (orderStatusText != null)
        {
            if (order.IsCompleted)
                orderStatusText.text = "✅ COMPLETED";
            else if (order.IsFailed)
                orderStatusText.text = "❌ FAILED";
            else
                orderStatusText.text = "🔄 ACTIVE";
        }

        RefreshRoversList();

        if (deliverButton != null)
            deliverButton.interactable = false;
    }

    string GetOrderStatus(OrderData order)
    {
        if (order.IsCompleted) return "completed";
        if (order.IsFailed) return "failed";
        if (order.IsBusy) return "in progress";
        return "active";
    }

    void OpenPanel()
    {
        if (isOpen) return;
        isOpen = true;
        openTime = Time.time;

        if (hudManager != null)
            hudManager.ShowOverlay();

        if (panel != null)
        {
            panel.SetActive(true);
            if (panelRect != null)
            {
                panelRect.anchoredPosition = closedPos;
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(AnimatePanel(closedPos, openPos));
            }
        }
    }

    public void ClosePanel()
    {
        if (!isOpen) return;

        if (Time.time - openTime < 0.5f)
        {
            return;
        }

        isOpen = false;

        if (cameraController != null)
        {
            if (cameraController.IsFocusing())
                cameraController.ExitFocusMode();
            cameraController.isUIActive = false;
        }

        if (panelRect != null)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimatePanel(panelRect.anchoredPosition, closedPos, true));
        }
        else
        {
            HidePanelImmediate();
        }
    }

    IEnumerator AnimatePanel(Vector2 start, Vector2 end, bool hideOnComplete = false)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = t * t * (3f - 2f * t);

            if (panelRect != null)
                panelRect.anchoredPosition = Vector2.Lerp(start, end, smoothT);

            yield return null;
        }

        if (panelRect != null)
            panelRect.anchoredPosition = end;

        if (hideOnComplete)
            HidePanelImmediate();

        animCoroutine = null;
    }

    void HidePanelImmediate()
    {
        Debug.Log("HidePanelImmediate");

        if (panel != null)
            panel.SetActive(false);

        if (hudManager != null)
            hudManager.HideOverlay();

        currentOrder = null;
        selectedRover = null;
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
        if (progress == null) return;

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
                reason = $"Too heavy! (max: {rover.CargoCapacity} kg)";
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

    public bool IsPanelOpen()
    {
        return isOpen;
    }
}