using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class OpenAIDungeonGenerator : MonoBehaviour
{
    public DungeonLoader loader;
    public string apiKey;
    public string model = "gpt-4o";
    public int minRoom;
    public int maxRoom;

    //[TextArea(3, 10)]
    //public string openAiPrompt = "Generate a randomized dungeon as a valid JSON object with the following fixed structure:\r\n\r\nTop-level keys: \"rooms\", \"connections\", and \"objectives\".\r\n\r\nROOMS\r\n\"rooms\" is an array of 8 to 12 rooms. Each room must include:\r\n\r\njson\r\nCopy\r\nEdit\r\n{\r\n  \"x\": number,\r\n  \"y\": number,\r\n  \"width\": 1,\r\n  \"height\": 1,\r\n  \"type\": \"spawn\" | \"boss\" | \"treasure\" | \"normal\",\r\n  \"enemies\": [ { \"type\": \"melee\" | \"ranged\" | \"boss\" } ],\r\n  \"powerups\": [ { \"type\": \"health\" | \"damage\" | \"speed\", \"x\": 0, \"y\": 0 } ],\r\n  \"treasures\": [ { \"type\": \"gold\", \"x\": 0, \"y\": 0 } ]\r\n}\r\nAll rooms must have unique (x, y) positions on a 2D grid.\r\n\r\nAll rooms must be connected via adjacency (no diagonals or isolated rooms).\r\n\r\nspawn room: no enemies, powerups, or treasures.\r\n\r\nboss room: exactly one enemy of type \"boss\", no powerups or treasures.\r\n\r\ntreasure room: exactly one treasure of type \"gold\", no enemies or powerups.\r\n\r\nAll other rooms are \"normal\" and must:\r\n\r\nContain at least one \"melee\" or \"ranged\" enemy.\r\n\r\nHave a 50% chance to include one powerup.\r\n\r\nHave no treasures.\r\n\r\nCONNECTIONS\r\n\"connections\" is an array of objects:\r\n\r\njson\r\nCopy\r\nEdit\r\n{ \"fromX\": number, \"fromY\": number, \"toX\": number, \"toY\": number }\r\nEach connection must link two adjacent rooms (difference of 1 in x or y).\r\n\r\nAll rooms must be fully reachable (no isolated rooms).\r\n\r\nOBJECTIVES\r\n\"objectives\" is an array of 3 unique strings describing player goals, like:\r\n\r\n\"Defeat all enemies\"\r\n\r\n\"Collect the treasure\"\r\n\r\n\"Defeat the boss\"\r\n\r\n✅ Output valid JSON only, no explanation, and use the exact field names and structure shown above.";
    [Tooltip("Name of the .txt prompt file in Resources (without extension)")]
    public string promptFileName = "DungeonPrompt";
    private string loadedPrompt;

    public bool useBaseJsonAsExample = false;
    public DungeonData generatedDungeon;

    public static OpenAIDungeonGenerator Instance { get; private set; }


    public UnityEvent onJsonGenerated;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (loader == null)
            loader = FindFirstObjectByType<DungeonLoader>();

        // Load prompt from Resources/DungeonPrompt.txt
        TextAsset promptFile = Resources.Load<TextAsset>(promptFileName);
        if (promptFile != null)
        {
            loadedPrompt = promptFile.text;
        }
        else
        {
            Debug.LogError("Prompt file not found in Resources!");
            loadedPrompt = "";
        }
        apiKey = ApiKeyLoader.LoadApiKey();
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
            userPrompt = loadedPrompt + "\nGenerate a new, unique, random dungeon layout";
            Debug.Log(userPrompt);
            //userPrompt = $"{openAiPrompt}\n\n" +
            //             "BASE JSON FORMAT EXAMPLE (FOR FORMATTING *ONLY*, DO NOT COPY ANY DATA):\n" +
            //             baseJson +
            //             "\n\nNow, generate an ALL NEW, fully random dungeon layout in this format. Change ALL positions, room types, connections, monsters, powerups, and treasures. " +
            //             "Do not copy or mutate the input, generate a different, unique, and RANDOM dungeon. " +
            //             "Output ONLY valid JSON with the fields: 'rooms', 'connections', and 'objectives'. Do NOT explain or use markdown formatting. Minimize similarity to the input.";
        }
        else
        {
            userPrompt = loadedPrompt + "\nGenerate a new, unique, random dungeon layout as valid JSON, following the last rules (and format, if shown above).";

            //userPrompt = openAiPrompt + "\nGenerate a new, unique, random dungeon layout as valid JSON, following the last rules (and format, if shown above).";
        }

        var requestData = new
        {
            model = model,
            messages = new[]
            {
        new {
            role = "system",
            content =
            "You are an expert procedural dungeon generator. " +
            "RULES: " +
            "- Always generate between 15 and 18 total rooms. " +
            "- Each room must have a unique (x, y) coordinate pair. No two rooms may share the same (x, y)." +
            "- Exactly one room must be type 'spawn'. " +
            "- one room must be type 'boss'. " +
            "- one room must be type 'treasure'. " +
            "- Every room must have between 8 and 10 enemies (except spawn, treasure, and boss rooms which may have special rules). " +
            "- If the dungeon has fewer than 15 rooms or more than 18, the output is INVALID. " +
            "- Ensure the grid is compact with no overlapping rooms. " +
            "- Always connect rooms with valid adjacency so the dungeon is traversable. " +
            "- Always output VALID JSON ONLY (no text, no explanations, no code blocks). " +
            "- Each generation must be unique and must NOT copy previous examples."
                },
                new { role = "user", content = userPrompt }
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
                    onJsonGenerated?.Invoke();
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