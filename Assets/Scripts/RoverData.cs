using System;
using UnityEngine;

[Serializable]
public class RoverData
{
    public string Id;                /* Uniq ID */
    public string Name;              /* Name of machine */
    public float CurrentBattery;     /* Current battery level */
    public float MaxBattery;         /* Max battery */
    public float CargoCapacity;      /* How much things can travel */
    public float Speed;              /* Speed of delivery */
    public bool IsBusy;              /* Busy flag */
    public bool IsDestroyed;         /* Destroyed flag */
    public Vector2 CurrentPosition;  /* Current position for anims */

    /* Constructor for create new rover */
    public RoverData(string name, float maxBattery, float cargoCapacity, float speed)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        MaxBattery = maxBattery;
        CurrentBattery = maxBattery; 
        CargoCapacity = cargoCapacity;
        Speed = speed;
        IsBusy = false;
        IsDestroyed = false;
        CurrentPosition = Vector2.zero;
    }

    /* Helpers methods */
    public float BatteryPercentage => CurrentBattery / MaxBattery * 100f;

    public bool CanDeliver(float orderWeight, float requiredBattery)
    {
        return !IsBusy &&
               !IsDestroyed &&
               CurrentBattery >= requiredBattery &&
               CargoCapacity >= orderWeight;
    }

    public void UseBattery(float amount)
    {
        CurrentBattery = Mathf.Max(0, CurrentBattery - amount);
        if (CurrentBattery <= 0)
        {
            IsDestroyed = true; /* Destroy if current battery is null */
        }
    }

    public void ChargeBattery(float amount)
    {
        CurrentBattery = Mathf.Min(MaxBattery, CurrentBattery + amount);
    }
}
