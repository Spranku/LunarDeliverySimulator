using UnityEngine;

public class ZoneDetector : MonoBehaviour
{
    public static ZoneDetector Instance;

    [Header("Zone References")]
    public Collider2D lowZone;
    public Collider2D mediumZone;
    public Collider2D highZone;

    void Awake()
    {
        Instance = this;
    }

    public string GetZoneType(Vector2 position)
    {
        if (lowZone != null && lowZone.OverlapPoint(position)) return "Low";

        if (mediumZone != null && mediumZone.OverlapPoint(position)) return "Medium";

        if (highZone != null && highZone.OverlapPoint(position)) return "High";

        return "Low"; /* Default */
    }

    public float GetZoneRisk(string zoneType)
    {
        switch (zoneType)
        {
            case "Low": return 0.1f;
            case "Medium": return 0.4f;
            case "High": return 0.8f;
            default: return 0.1f;
        }
    }
}