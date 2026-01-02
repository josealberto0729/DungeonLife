using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public float temperature;
}
[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class OpenAIChatResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public ChatMessage message;
}
public class LLMJsonCreator : MonoBehaviour
{
    public string apiKey;
    public string model = "gpt-4.1-mini";
    public UnityEvent onJsonGenerated;
    public UnityEvent JsonGenerationStarted;
    private bool isGenerating = false;
    [Header("Generation Settings")]
    [Tooltip("Number of rounds to generate in one go")]
    public int numberOfRounds = 1;
    private int currentRound = 1;
    public string promptFileName = "DungeonPrompt";
    private string loadedPrompt;
    private bool hasStarted = false;

    public static LLMJsonCreator Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        // Load the OpenAI config (only once)
        if (!OpenAIConfig.LoadConfig())
        {
            Debug.LogError("Failed to load OpenAI config. Check your JSON file in StreamingAssets.");
            return;
        }

        apiKey = OpenAIConfig.ApiKey;
        model = OpenAIConfig.Model;
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
        Debug.Log("OpenAI configuration loaded successfully.");

    }
    public void StartJsonGeneration()
    {
        if (isGenerating)
        {
            Debug.LogWarning("JSON generation is already in progress.");
            return;
        }
        JsonGenerationStarted?.Invoke();
        Debug.Log("Starting JSON generation...");
        StartCoroutine(GenerateGridMapJson());
    }
    private IEnumerator GenerateGridMapJson()
    {
        isGenerating = true;

        // Make sure the folder exists
        string LevelFilePath = Path.Combine(Application.persistentDataPath, "Level");
        if (!Directory.Exists(LevelFilePath))
            Directory.CreateDirectory(LevelFilePath);
        for (int i = 0; i < numberOfRounds; i++)
        {
            Task<string> Task = GenerateJsonFromLLM();
            yield return new WaitUntil(() => Task.IsCompleted);

            string generatedJson = Task.Result;

            if (string.IsNullOrEmpty(generatedJson))
            {
                Debug.LogError("Generated JSON was null or empty.");
                isGenerating = false;
                yield break;
            }

            // Count existing round files to determine the next number
            string[] existingFiles = Directory.GetFiles(LevelFilePath, "Round_*.json");
            int nextRoundNumber = existingFiles.Length + 1;
            currentRound = existingFiles.Length + 1;

            // Format the filename: Round_1.json, Round_2.json, etc.
            string fileName = $"Round_{nextRoundNumber}.json";
            string filePath = Path.Combine(LevelFilePath, fileName);

            // Save the JSON
            File.WriteAllText(filePath, generatedJson);

            Debug.Log("JSON saved to: " + filePath);
            // Small delay to avoid overwhelming the API
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("All requested map JSON files generated!");
        isGenerating = false;
        if(!hasStarted)
        {   
            hasStarted = true;
            onJsonGenerated?.Invoke();
        }   


    }
    private async Task<string> GenerateJsonFromLLM()
    {
        string endpoint = "https://api.openai.com/v1/chat/completions";
        string userPrompt;
        userPrompt = "\nGenerate a new, unique, random dungeon layout";
        var requestData = new ChatRequest
        {
            model = model,
            messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                role = "system",
                content =   "You are an expert procedural dungeon generator. " +
                "RULES: " +
                "Each room must have a unique (x, y) coordinate pair. " +
                "Exactly one room must be type 'spawn'. " +
                "One room must be type 'boss'. " +
                "One room must be type 'treasure'. " +
                "Every room must have between 8 and 10 enemies (except spawn, treasure, and boss rooms). " +
                "Ensure the grid is compact with no overlapping rooms. " +
                "Always connect rooms with valid adjacency so the dungeon is traversable. " +
                "Always output VALID JSON ONLY (no text, no explanations, no code blocks). " +
                "Each generation must be unique and must NOT copy previous examples." +
                "No two rooms can share the same x and y values." +
                "Make all room has a connections"+
                loadedPrompt

            },
            new ChatMessage
            {
                role = "user",
                content =userPrompt
            }
        },
            temperature = 0.7f
        };
        string jsonBody = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request failed: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            try
            {
                // Deserialize the response
                OpenAIChatResponse response = JsonConvert.DeserializeObject<OpenAIChatResponse>(request.downloadHandler.text);

                // Extract the JSON content (the actual skill)
                string jsonContent = response?.choices?[0]?.message?.content?.Trim();

                if (!string.IsNullOrEmpty(jsonContent))
                {
                    jsonContent = Regex.Replace(jsonContent, @"^```(json)?|```$", "", RegexOptions.Multiline).Trim();
                }

                return jsonContent;
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON parse error: {e.Message}\nResponse: {request.downloadHandler.text}");
                return null;
            }
        }


    }
}
