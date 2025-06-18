using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class OpenAIDungeonGenerator : MonoBehaviour
{
    public DungeonLoader loader;
    public string apiKey = "";
    public string model = "gpt-3.5-turbo";

    [TextArea(3, 10)]
    public string openAiPrompt = "Given the dungeon format below, generate a new variation with similar structure and randomized content. Return ONLY valid JSON with 'rooms' and 'connections'. No explanation, no markdown.";

    public DungeonData generatedDungeon;

    void Awake()
    {
        if (loader == null)
        {
            loader = FindObjectOfType<DungeonLoader>();
        }
    }

    void Start()
    {
        StartCoroutine(GenerateDungeonFromOpenAI());
    }

    public IEnumerator GenerateDungeonFromOpenAI()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader is not assigned!");
            yield break;
        }

        string baseJson = loader.dungeonJson != null ? loader.dungeonJson.text : null;

        string prompt = string.IsNullOrEmpty(baseJson)
            ? $"{openAiPrompt} Output only JSON."
            : $"{openAiPrompt}\n\nBase JSON:\n{baseJson}\n\nOnly return JSON with the fields: rooms[], connections[]. No extra text.";

        var requestData = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "Given a dungeon JSON format with rooms that have x, y, width, height, type, enemies (type, patrolRadius), powerups (type, x, y), treasures (type, x, y), and connections between rooms, generate a new, unique dungeon map with randomized values and a fully connected layout.\r\n\r\nRooms must not overlap. Each room's position and size must be chosen so no two rooms share any grid space.\r\n\r\nReturn ONLY valid JSON with 'rooms' and 'connections' arrays. Do NOT repeat the example values.\r\n" },
                new { role = "user", content = prompt }
            },
            temperature = 1.0
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OpenAI request failed: " + request.error);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                var chatResponse = JsonConvert.DeserializeObject<OpenAIChatResponse>(responseText);
                string rawContent = chatResponse.choices[0].message.content.Trim();

                // Remove markdown code fences or surrounding text if present
                string cleanedJson = CleanJson(rawContent);

                try
                {
                    generatedDungeon = JsonConvert.DeserializeObject<DungeonData>(cleanedJson);
                    loader.SetDungeonData(generatedDungeon);
                    Debug.Log("Dungeon successfully generated and loaded.");
                    Debug.Log("OpenAI JSON response:\n" + rawContent);
                    Debug.Log("Cleaned JSON to save:\n" + cleanedJson);
                    // Save JSON to Assets/Resources folder
                    string path = Path.Combine(Application.dataPath, "Resources/GeneratedDungeon.json");
                    File.WriteAllText(path, cleanedJson);
                    Debug.Log($"Dungeon JSON saved to: {path}");

#if UNITY_EDITOR
                    AssetDatabase.Refresh();
#endif
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse dungeon JSON: " + e.Message + "\nRaw content:\n" + rawContent);
                }
            }
        }
    }

    private string CleanJson(string input)
    {
        // Remove markdown code blocks if present
        input = Regex.Replace(input, @"^```json|```$", "", RegexOptions.Multiline).Trim();

        // Extract JSON object if wrapped in explanations or surrounding text
        int firstBrace = input.IndexOf('{');
        int lastBrace = input.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return input.Substring(firstBrace, lastBrace - firstBrace + 1);
        }
        return input; // fallback
    }
}

[System.Serializable]
public class OpenAIChatResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string content;
}
