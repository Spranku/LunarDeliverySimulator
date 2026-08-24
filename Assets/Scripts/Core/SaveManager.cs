using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public void Awake()
    {
        GameProgress progress = SaveManager.Load();
        /* Check rovers */
        if (progress.Rovers.Count == 0)
        {
            /* Create start rovers */
            progress.Rovers.Add(new RoverData("Ћуноход-1", 100f, 50f, 1f));
            progress.Rovers.Add(new RoverData("Ћуноход-2", 80f, 30f, 1.5f));
            progress.Rovers.Add(new RoverData("“€гач", 150f, 100f, 0.7f));
            SaveManager.Save(progress);
        }
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "lunar_save.json");

    public static void Save(GameProgress progress)
    {
        try
        {
            string json = JsonUtility.ToJson(progress, true); // pretty print
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game saved to {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public static GameProgress Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                GameProgress progress = JsonUtility.FromJson<GameProgress>(json);
                Debug.Log("Game loaded successfully");
                return progress;
            }
            else
            {
                Debug.Log("No save file found, creating new game");
                return new GameProgress();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return new GameProgress();
        }
    }

    public static bool SaveExists()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save deleted");
        }
    }
}