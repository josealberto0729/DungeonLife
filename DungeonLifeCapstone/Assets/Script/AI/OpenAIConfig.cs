using System;
using System.IO;
using UnityEngine;

[Serializable]
public class OpenAIConfigData
{
    public string part1;
    public string part2;
    public string model;
}

public static class OpenAIConfig
{
    public static string ApiKey { get; private set; }
    public static string Model { get; private set; }

    public static bool LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "openai_config.json");

        if (!File.Exists(path))
        {
            Debug.LogError($"Missing config file: {path}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            OpenAIConfigData data = JsonUtility.FromJson<OpenAIConfigData>(json);

            if (string.IsNullOrEmpty(data.part1) || string.IsNullOrEmpty(data.part2))
            {
                Debug.LogError("API key parts are missing or empty!");
                return false;
            }

            ApiKey = data.part1 + data.part2;
            Model = data.model;

            Debug.Log("OpenAI config loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load OpenAI config: {ex.Message}");
            return false;
        }
    }
}
