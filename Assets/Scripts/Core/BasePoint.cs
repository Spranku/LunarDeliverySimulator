using UnityEngine;

public class BasePoint : MonoBehaviour
{
    [Header("Base Settings")]
    public string baseName = "Main Base";
    public int level = 1;
    public int upgradeCost = 500;

    void Start()
    {
        GameManager3D gm = FindFirstObjectByType<GameManager3D>();
        if (gm != null)
        {
            gm.RegisterBase(this);
        }
    }
}