using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class ApiKeyData
{
    public string apiKey;
}

public static class ApiKeyLoader
{
    public static string LoadApiKey(string fileName = "openai_key")
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null)
        {
            Debug.LogError($"API key file '{fileName}.json' not found in Resources!");
            return "";
        }

        try
        {
            ApiKeyData data = JsonConvert.DeserializeObject<ApiKeyData>(jsonFile.text);
            return data.apiKey;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse API key JSON: {e.Message}");
            return "";
        }
    }
}
