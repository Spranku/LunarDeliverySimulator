using System;
using UnityEngine;

[Serializable]
public class OrderData
{
    public string Id;                /* Uniq ID */
    public string Title;             /* Name of order */
    public float Weight;             /* Kgs of order */
    public int Reward;               /* Bonus (credits) */
    public int Urgency;              /* Level of time order*/
    public float Risk;               /* Level of dangerous */
    public Vector2 TargetPosition;   /* Position on map */ 
    public string ZoneType;          /* Type of zone ("Low", "Medium", "High")*/
    public bool IsCompleted;         /* Is order success */
    public bool IsFailed;            /* Is order failed */
    public int DayCreated;           /* Day of order created */
    public int DayDeadline;          /* Deadline */
    public bool IsBusy;

    public OrderData(string title, float weight, int reward, int urgency,
                     Vector2 targetPosition, string zoneType, float risk, int dayCreated)
    {
        Id = Guid.NewGuid().ToString();
        Title = title;
        Weight = weight;
        Reward = reward;
        Urgency = Mathf.Clamp(urgency, 1, 5);
        TargetPosition = targetPosition;
        ZoneType = zoneType;
        Risk = Mathf.Clamp01(risk);
        IsCompleted = false;
        IsFailed = false;
        DayCreated = dayCreated;
        DayDeadline = dayCreated + urgency; /* if level of order high - deadline is less */
        IsBusy = false;
    }

    /* Check dedaline */
    public bool IsOverdue(int currentDay)
    {
        return !IsCompleted && !IsFailed && currentDay > DayDeadline;
    }

    /* Debuff by deadline */
       public int GetLatePenalty()
    {
        return Mathf.RoundToInt(Reward * 0.5f); // Øענאפ 50% מע םאדנאהû
    }
}