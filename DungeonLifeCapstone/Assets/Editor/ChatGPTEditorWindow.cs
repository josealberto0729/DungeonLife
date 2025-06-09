using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Newtonsoft.Json;

[System.Serializable]
public class Message
{
    public string role;
    public string content;

    public Message(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[System.Serializable]
public class ChatGPTRequest
{
    public string model = "gpt-3.5-turbo";
    public Message[] messages;

    public ChatGPTRequest(string userPrompt)
    {
        messages = new Message[]
        {
        new Message("system", "You are a procedural dungeon generator."),
        new Message("user", userPrompt)
        };
    }
}

[System.Serializable]
public class ChatGPTChoice
{
    public Message message;
}

[System.Serializable]
public class ChatGPTResponse
{
    public ChatGPTChoice[] choices;
}

public class ChatGPTEditorWindow : EditorWindow
{
    private string prompt = "Generate a procedural dungeon layout in JSON.";
    private string result = "Response will appear here...";
    private string apiKey = "";
    private bool isWaiting = false;

    [MenuItem("Tools/ChatGPT Assistant")]
    public static void ShowWindow()
    {
        GetWindow<ChatGPTEditorWindow>("ChatGPT Assistant");
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString("OpenAI_API_Key", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("ChatGPT Prompt", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(100));

        GUILayout.Space(10);
        GUILayout.Label("OpenAI API Key", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.PasswordField("API Key", apiKey);

        if (GUILayout.Button("Save API Key"))
        {
            EditorPrefs.SetString("OpenAI_API_Key", apiKey);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Send Prompt") && !isWaiting)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorUtility.DisplayDialog("Missing API Key", "Please enter and save your OpenAI API key.", "OK");
                return;
            }

            isWaiting = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(SendPromptToChatGPT(prompt));
        }

        GUILayout.Space(10);
        GUILayout.Label("ChatGPT Response", EditorStyles.boldLabel);
        GUILayout.TextArea(result, GUILayout.Height(200));
    }

    IEnumerator SendPromptToChatGPT(string userPrompt)
    {
        string apiUrl = "https://api.openai.com/v1/chat/completions";
        ChatGPTRequest chatRequest = new ChatGPTRequest(userPrompt);
        string requestBody = JsonConvert.SerializeObject(chatRequest);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();
        isWaiting = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            result = ExtractMessage(request.downloadHandler.text);
            string path = "Assets/Resources/GeneratedDungeonSavedByChat.json";
            System.IO.File.WriteAllText(path, result);
            AssetDatabase.Refresh();
        }
        else
        {
            result = "Error: " + request.error + "\n" + request.downloadHandler.text;
        }

        Repaint();
    }

    private string ExtractMessage(string json)
    {
        try
        {
            ChatGPTResponse response = JsonConvert.DeserializeObject<ChatGPTResponse>(json);
            return response.choices.Length > 0 ? response.choices[0].message.content : "No response.";
        }
        catch
        {
            return "Failed to parse ChatGPT response.";
        }
    }
}
