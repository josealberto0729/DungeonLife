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
    public string openAiPrompt = "You are a game level generator. Given an example dungeon JSON, generate an ALL NEW layout and content in the same format. DO NOT copy, repeat, or minimally change the input. Change all coordinates, room types, connections, and contents. Output ONLY valid JSON (no explanation, no markdown) with only the fields: 'rooms', 'connections', and optionally 'objectives'.";

    public bool useBaseJsonAsExample = false;  // Toggle whether to send the base JSON (schema) for few-shot, or not.
    public DungeonData generatedDungeon;

    void Awake()
    {
        if (loader == null)
            loader = FindObjectOfType<DungeonLoader>();
    }

    /// <summary>
    /// Call this to generate a new random dungeon via OpenAI.
    /// </summary>
    public void CallGenerateNewJson()
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
        string userPrompt;

        if (useBaseJsonAsExample && !string.IsNullOrEmpty(baseJson))
        {
            userPrompt = $"{openAiPrompt}\n\n" +
                         "BASE JSON FORMAT EXAMPLE (FOR FORMATTING *ONLY*, DO NOT COPY ANY DATA):\n" +
                         baseJson +
                         "\n\nNow, generate an ALL NEW, fully random dungeon layout in this format. Change ALL positions, room types, connections, monsters, powerups, and treasures. " +
                         "Do not copy or mutate the input, generate a different, unique, and RANDOM dungeon. " +
                         "Output ONLY valid JSON with the fields: 'rooms', 'connections', and 'objectives'. Do NOT explain or use markdown formatting. Minimize similarity to the input.";
        }
        else
        {
            userPrompt = openAiPrompt + "\nGenerate a new, unique, random dungeon layout as valid JSON, following the last rules (and format, if shown above).";
        }

        var requestData = new
        {
            model = model,
            messages = new[]
            {
                new {
                    role = "system",
                    content =
                    "You are an expert procedural generator. " +
                    "When prompted, you must generate a random dungeon as valid JSON ONLY, with a compact grid, non-overlapping rooms, and each room's content, type, and position randomized every request. " +
                    "NEVER repeat or copy previous examples. " +
                    "ALWAYS make each generation unique and all values newly chosen."
                },
                new { role = "user", content = userPrompt }
            },
            temperature = 1.0 // More random!
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
        return input.Trim(); // fallback
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