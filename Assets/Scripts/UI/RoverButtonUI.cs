using UnityEngine;
using UnityEngine.UI;

public class RoverButtonUI : MonoBehaviour
{
    private Text nameText;
    private Text batteryText;
    private Text capacityText;
    private Button button;

    public RoverData Rover { get; private set; }
    private System.Action<RoverData> onSelected;

    void Awake()
    {
        
        Text[] texts = GetComponentsInChildren<Text>();

        if (texts.Length >= 3)
        {
            nameText = texts[0];
            batteryText = texts[1];
            capacityText = texts[2];
        }
        else
        {
            foreach (Text t in texts)
            {
                if (t.gameObject.name == "NameText")
                    nameText = t;
                else if (t.gameObject.name == "BatteryText")
                    batteryText = t;
                else if (t.gameObject.name == "CapacityText")
                    capacityText = t;
            }
        }

        button = GetComponent<Button>();
    }

    public void Initialize(RoverData rover, System.Action<RoverData> onSelectedCallback)
    {
        Rover = rover;
        onSelected = onSelectedCallback;

        if (nameText != null)
            nameText.text = rover.Name;
        if (batteryText != null)
            batteryText.text = $" {rover.CurrentBattery:F0}/{rover.MaxBattery:F0}";
        if (capacityText != null)
            capacityText.text = $" {rover.CargoCapacity} kg";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(Rover));
        }
    }

    public void SetAvailable(bool isAvailable)
    {
        if (button == null) return;
        button.interactable = isAvailable;
    }
}