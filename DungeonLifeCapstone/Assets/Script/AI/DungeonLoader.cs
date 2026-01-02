using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class DungeonLoader : MonoBehaviour
{
    public string levelFolderName = "Level"; // folder inside persistentDataPath
    private List<DungeonData> allDungeons = new List<DungeonData>();
    private int currentDungeonIndex = 0;
    private bool isDungeonGenerating = false;
    private DungeonData runtimeData;
    public static DungeonLoader Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        LoadAllDungeons();
    }

    private void Start()
    {
        LLMJsonCreator.Instance.onJsonGenerated.AddListener(OnJsonReady);
    }
    void OnJsonReady()
    {
        Debug.Log("DungeonLoader detected new dungeon JSON generated.");
        isDungeonGenerating = false;
        LoadAllDungeons();
    }

    private void LoadAllDungeons()
    {
        allDungeons.Clear();

        string folderPath = Path.Combine(Application.persistentDataPath, levelFolderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"Created dungeon folder at: {folderPath}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
        if (jsonFiles.Length == 0)
        {
            Debug.Log($"No JSON files found in folder: {folderPath}");
            return;
        }

        foreach (string filePath in jsonFiles)
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                DungeonData data = JsonConvert.DeserializeObject<DungeonData>(jsonText);
                if (data != null)
                {
                    allDungeons.Add(data);
                }
                else
                {
                    Debug.LogWarning($"Dungeon JSON '{filePath}' is null after deserialization.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to read or deserialize '{filePath}': {ex.Message}");
            }
        }

    }

    public DungeonData GetDungeonData(bool forceReload = false)
    {
        if (!forceReload && runtimeData != null)
            return runtimeData;

        if (allDungeons.Count == 0)
        {
            Debug.LogError("No dungeons loaded!");
            return null;
        }

        runtimeData = allDungeons[currentDungeonIndex];
        return runtimeData;
    }
    public void SetDungeonData(DungeonData data)
    {
        runtimeData = data;
    }

    public void CheckToGenerateNewDungeons()
    {
        if(currentDungeonIndex > allDungeons.Count/2 && !isDungeonGenerating)
        {
            Debug.Log("DungeonLoader is now calling llm to generate new maps");
            isDungeonGenerating = true;
            LLMJsonCreator.Instance.StartJsonGeneration();
        }
    }

    public void LoadNextDungeon()
    {
        if (allDungeons.Count == 0)
        {
            Debug.LogError("No dungeons loaded!");
            return;
        }

        currentDungeonIndex++;
        if (currentDungeonIndex >= allDungeons.Count)
            currentDungeonIndex = 0; // loop back to first level

        runtimeData = allDungeons[currentDungeonIndex];
        Debug.Log($"Loaded dungeon: {runtimeData}");
    }

    public void LoadDungeonByIndex(int index)
    {
        if (index < 0 || index >= allDungeons.Count)
        {
            Debug.LogError($"Dungeon index {index} is out of range!");
            return;
        }

        currentDungeonIndex = index;
        runtimeData = allDungeons[index];
        Debug.Log($"Loaded dungeon: {runtimeData}");
    }
}
