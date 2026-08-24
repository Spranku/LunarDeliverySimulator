using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OrderPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text infoText;
    [SerializeField] private Transform roversListParent;
    [SerializeField] private GameObject roverButtonPrefab;
    [SerializeField] private Button deliverButton;
    [SerializeField] private Button closeButton;

    private OrderData currentOrder;
    private RoverData selectedRover;
    private GameManager gameManager;
    private List<GameObject> roverButtons = new List<GameObject>();

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (deliverButton != null)
            deliverButton.onClick.AddListener(OnDeliverClicked);

        panel.SetActive(false);
    }

    public void ShowOrder(OrderData order)
    {
        Debug.Log("Show order");
        currentOrder = order;
        selectedRover = null;

        // Показываем панель
        panel.SetActive(true);

        // Заполняем информацию
        titleText.text = order.Title;
        infoText.text = $"⚖️ Вес: {order.Weight} кг\n" +
                        $"💰 Награда: {order.Reward} кредитов\n" +
                        $"⚠️ Риск: {order.Risk * 100:F0}%\n" +
                        $"📍 Зона: {order.ZoneType}\n" +
                        $"⏰ Срочность: {order.Urgency}/5";

        // Создаем кнопки роверов
        RefreshRoversList();

        // Блокируем кнопку доставки
        deliverButton.interactable = false;
    }

    void RefreshRoversList()
    {
        Debug.Log("RefreshRoversList вызван");

        // Удаляем старые кнопки
        foreach (var btn in roverButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        roverButtons.Clear();

        // Дополнительная очистка - удаляем все дочерние объекты в RoversList
        if (roversListParent != null)
        {
            foreach (Transform child in roversListParent)
            {
                Destroy(child.gameObject);
            }
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager не найден!");
            return;
        }

        var progress = gameManager.GetProgress();
        if (progress == null)
        {
            Debug.LogError("Progress равен null!");
            return;
        }

        foreach (var rover in progress.Rovers)
        {
            if (rover.IsDestroyed) continue;
            if (rover.IsBusy) continue; // Пропускаем занятых

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

        // Проверяем может ли доставить
        bool canDeliver = CanRoverDeliver(rover, currentOrder);
        deliverButton.interactable = canDeliver;

        // Показываем причину если нельзя
        if (!canDeliver)
        {
            string reason = "";
            if (rover.IsBusy) reason = "Ровер занят!";
            else if (rover.IsDestroyed) reason = "Ровер сломан!";
            else if (rover.CurrentBattery < currentOrder.Weight * 0.5f)
                reason = $"Не хватает батареи! (нужно: {currentOrder.Weight * 0.5f:F0})";
            else if (rover.CargoCapacity < currentOrder.Weight)
                reason = $"Слишком тяжело! (грузоподъемность: {rover.CargoCapacity} кг)";
            else if (currentOrder.Risk > 0.7f)
                reason = "Слишком опасно! (риск > 70%)";

            infoText.text += $"\n\n❌ {reason}";
        }
    }

    void OnDeliverClicked()
    {
        if (currentOrder == null || selectedRover == null) return;

        // Запускаем доставку через GameManager
        if (gameManager != null)
        {
            gameManager.StartDelivery(selectedRover, currentOrder);
        }

        ClosePanel();
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        currentOrder = null;
        selectedRover = null;
    }
}