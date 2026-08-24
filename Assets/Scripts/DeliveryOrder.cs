using System;

[Serializable]
public class DeliveryReport
{
    public string OrderId;
    public string RoverId;
    public bool IsSuccess;
    public int EarnedMoney;
    public float BatteryUsed;
    public float TimeSpent;          /* Time in game days or seconds */ 
    public string FailureReason;     /* Why delivery is failed */ 

    public DeliveryReport(string orderId, string roverId)
    {
        OrderId = orderId;
        RoverId = roverId;
        IsSuccess = false;
        EarnedMoney = 0;
        BatteryUsed = 0;
        TimeSpent = 0;
        FailureReason = "";
    }
}