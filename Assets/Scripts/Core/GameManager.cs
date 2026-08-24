using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameProgress Progress;

    [Header("Visuals")]
    public Transform ordersParent;
    public GameObject orderPointPrefab;

    [Header("UI")]
    [SerializeField] private OrderPanelUI orderPanel;

    private List<GameObject> orderVisuals = new List<GameObject>();

    public void SelectOrder(OrderData order)
    {
        Debug.Log($"Выбран заказ: {order.Title}, Вес: {order.Weight}кг, Награда: {order.Reward} кредитов");

        // Показываем панель
        if (orderPanel != null)
        {
            orderPanel.ShowOrder(order);
        }
        else
        {
            Debug.Log("orderPanel=null");
        }
    }

    public void StartDelivery(RoverData rover, OrderData order)
    {
        // Расход батареи
        float batteryUsed = order.Weight * 0.5f;
        rover.UseBattery(batteryUsed);
        rover.IsBusy = true;

        // Начисляем награду
        Progress.AddMoney(order.Reward);
        order.IsCompleted = true;

        // Сохраняем
        SaveManager.Save(Progress);

        // Обновляем визуалы (удаляем точку с карты)
        UpdateOrderVisuals();

        Debug.Log($"✅ Доставка завершена! +{order.Reward} кредитов");
    }

    void UpdateOrderVisuals()
    {
        for (int i = orderVisuals.Count - 1; i >= 0; i--)
        {
            if (orderVisuals[i] == null) continue;

            OrderPoint point = orderVisuals[i].GetComponent<OrderPoint>();
            if (point != null && point.Order != null && point.Order.IsCompleted)
            {
                Destroy(orderVisuals[i]);
                orderVisuals.RemoveAt(i);
            }
        }
    }

    public GameProgress GetProgress()
    {
        return Progress;
    }


    public void Awake()
    {
        // Полностью очищаем контейнер
        if (ordersParent != null)
        {
            for (int i = ordersParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(ordersParent.GetChild(i).gameObject);
            }
        }
        orderVisuals.Clear();

        // Удаляем сохранение и создаем новые данные
        SaveManager.DeleteSave();
        Progress = SaveManager.Load();

        // Создаем роверы
        if (Progress.Rovers.Count == 0)
        {
            Progress.Rovers.Add(new RoverData("Луноход-1", 100f, 50f, 1f));
            Progress.Rovers.Add(new RoverData("Луноход-2", 80f, 30f, 1.5f));
            Progress.Rovers.Add(new RoverData("Тягач", 150f, 100f, 0.7f));
        }

        // Очищаем старые заказы
        Progress.Orders.Clear();

        // Создаем новые случайные заказы
        GenerateRandomOrders(5);

        // Сохраняем
        SaveManager.Save(Progress);

        // Визуализируем
        VisualizeOrders();
    }

    Vector2 GetRandomPositionInZone(string zoneType)
    {
        float x, y;

        switch (zoneType)
        {
            case "Low":
                // Low зона: позиция (0, 0), размер (8, 4)
                // Границы: x от -4 до 4, y от -2 до 2
                x = Random.Range(-4f, 4f);
                y = Random.Range(-2f, 2f);
                break;

            case "Medium":
                // Medium зона: позиция (6, 3.4), размер (4, 3)
                // Границы: x от 4 до 8, y от 1.9 до 4.9
                x = Random.Range(4f, 8f);
                y = Random.Range(1.9f, 4.9f);
                break;

            case "High":
                // High зона: позиция (-5.5, -3.4), размер (3, 3)
                // Границы: x от -7 до -4, y от -4.9 до -1.9
                x = Random.Range(-7f, -4f);
                y = Random.Range(-4.9f, -1.9f);
                break;

            default:
                x = Random.Range(-5f, 5f);
                y = Random.Range(-3f, 3f);
                break;
        }

        return new Vector2(x, y);
    }

    void GenerateRandomOrders(int count)
    {
        string[] titles = { "Пайки", "Кислород", "Оборудование", "Материалы", "Медикаменты" };
        string[] zones = { "Low", "Medium", "High" };

        for (int i = 0; i < count; i++)
        {
            string zone = zones[Random.Range(0, zones.Length)];
            Vector2 pos = GetRandomPositionInZone(zone);
            float risk = zone == "Low" ? 0.1f : (zone == "Medium" ? 0.4f : 0.8f);

            Progress.Orders.Add(new OrderData(
                titles[Random.Range(0, titles.Length)],
                Random.Range(10f, 80f),
                Random.Range(50, 300),
                Random.Range(1, 5),
                pos,
                zone,
                risk,
                Progress.Day
            ));
        }
    }

    void VisualizeOrders()
    {
        // Очищаем контейнер
        if (ordersParent != null)
        {
            for (int i = ordersParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(ordersParent.GetChild(i).gameObject);
            }
        }
        orderVisuals.Clear();

        int createdCount = 0;

        foreach (var order in Progress.Orders)
        {
            if (!order.IsCompleted && !order.IsFailed && order != null)
            {
                GameObject point = Instantiate(orderPointPrefab, ordersParent);
                var pointScript = point.GetComponent<OrderPoint>();

                if (pointScript != null)
                {
                    pointScript.Initialize(order);
                    orderVisuals.Add(point);
                    createdCount++;
                    Debug.Log($"Создана точка: {order.Title} на {order.TargetPosition}");
                }
            }
        }

        Debug.Log($"Всего создано точек: {createdCount}");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                OrderPoint point = hit.collider.GetComponent<OrderPoint>();
                if (point != null)
                {
                    point.OnClick();
                }
            }
        }
    }
}