using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameProgress Progress;

    void Awake()
    {
        Progress = SaveManager.Load();

        /* Create rover if havent */
        if (Progress.Rovers.Count == 0)
        {
            Progress.Rovers.Add(new RoverData("Lunar-1", 100f, 50f, 1f));
            Progress.Rovers.Add(new RoverData("Lunar-2", 80f, 30f, 1.5f));
            SaveManager.Save(Progress);
        }

        /* Create an order if havent */
        if (Progress.Orders.Count == 0)
        {
            CreateTestOrders();
            SaveManager.Save(Progress);
        }

        /* Debug only */
        Debug.Log($"Day: {Progress.Day}, Money: {Progress.Money}, Raiting: {Progress.BaseRating}");
        Debug.Log($"Rovers: {Progress.Rovers.Count}, Orders: {Progress.Orders.Count}");
    }

    void CreateTestOrders()
    {
        /* Create 3 texts order on different zones */
        Progress.Orders.Add(new OrderData(
            "Food for base",
            20f,
            100,
            2,
            new Vector2(-3, 2),
            "Low",
            0.1f,
            1
        ));

        Progress.Orders.Add(new OrderData(
            "Barrels",
            40f,
            200,
            3,
            new Vector2(4, -1),
            "Medium",
            0.4f,
            1
        ));

        Progress.Orders.Add(new OrderData(
            "Hard machines",
            80f,
            300,
            1,
            new Vector2(-2, -3),
            "High",
            0.8f,
            1
        ));
    }
}