using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameProgress
{
    public int Money;                           // Current money
    public int Day;                             // Current day
    public float BaseRating;                    // Raiting of base (0-100)
    public int TotalDeliveriesCompleted;        // Total success deliveries
    public int TotalDeliveriesFailed;           // Total failed deliveries

    public List<RoverData> Rovers;              // List of all rovers
    public List<OrderData> Orders;              // List of all rovers
    public List<DeliveryReport> DeliveryHistory; // History of deliveries

    public GameProgress()
    {
        Money = 500;                // First cash
        Day = 1;
        BaseRating = 50f;           // Some raiting
        TotalDeliveriesCompleted = 0;
        TotalDeliveriesFailed = 0;
        Rovers = new List<RoverData>();
        Orders = new List<OrderData>();
        DeliveryHistory = new List<DeliveryReport>();
    }

    // Helpres methods
    public void AddMoney(int amount)
    {
        Money = Mathf.Max(0, Money + amount);
    }

    public void ChangeRating(float delta)
    {
        BaseRating = Mathf.Clamp(BaseRating + delta, 0f, 100f);
    }

    public bool IsGameOver()
    {
        return BaseRating <= 0 || Money < 0;
    }

    public bool IsVictory()
    {
        return BaseRating >= 100f || Money >= 10000;
    }
}