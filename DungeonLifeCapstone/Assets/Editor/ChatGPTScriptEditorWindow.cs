using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Newtonsoft.Json;
using System.IO;
using System;

public class ChatGPTScriptEditorWindow : EditorWindow
{
    private string apiKey = "";
    private string prompt = "";
    private string result = "Response will appear here...";
    private string draggedFilePath = "";
    private string fileContent = "";
    private bool isWaiting = false;

    [MenuItem("Tools/ChatGPT Script Refactorer")]
    public static void ShowWindow()
    {
        GetWindow<ChatGPTScriptEditorWindow>("ChatGPT Script Refactorer");
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString("OpenAI_API_Key", "");
    }

    private void OnGUI()
    {
        GUILayout.Label("ChatGPT Refactor / Generator", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.PasswordField("OpenAI API Key", apiKey);
        if (GUILayout.Button("Save API Key"))
            EditorPrefs.SetString("OpenAI_API_Key", apiKey);

        GUILayout.Space(10);
        GUILayout.Label("Drag and drop a .cs or .json file here:", EditorStyles.helpBox, GUILayout.Height(40));
        HandleDragAndDrop();

        if (!string.IsNullOrEmpty(fileContent))
        {
            GUILayout.Label("Prompt Preview (editable):", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(100));

            if (GUILayout.Button("Send to ChatGPT") && !isWaiting)
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
            GUILayout.Label("ChatGPT Response:", EditorStyles.boldLabel);
            result = EditorGUILayout.TextArea(result, GUILayout.Height(200));

            if (!string.IsNullOrEmpty(result))
            {
                if (GUILayout.Button("Overwrite Original File"))
                {
                    File.WriteAllText(draggedFilePath, result);
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Save as New Script"))
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(draggedFilePath), "Generated_" + Path.GetFileNameWithoutExtension(draggedFilePath) + ".cs");
                    File.WriteAllText(newPath, result);
                    AssetDatabase.Refresh();
                }
            }
        }

        if (isWaiting)
        {
            EditorGUILayout.HelpBox("Waiting for ChatGPT response...", MessageType.Info);
        }
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (DragAndDrop.paths.Length > 0)
                {
                    draggedFilePath = DragAndDrop.paths[0];
                    string extension = Path.GetExtension(draggedFilePath);

                    if (extension == ".cs")
                    {
                        fileContent = File.ReadAllText(draggedFilePath);
                        prompt = $"Refactor and improve the following Unity C# script:\n\n{fileContent}";
                    }
                    else if (extension == ".json")
                    {
                        fileContent = File.ReadAllText(draggedFilePath);
                        prompt = $"Create a Unity C# script that parses and represents the following JSON data structure. Include relevant classes and logic for loading the JSON at runtime:\n\n{fileContent}";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid File", "Only .cs and .json files are supported.", "OK");
                        return;
                    }

                    Repaint();
                }
            }

            evt.Use();
        }
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

    [Serializable]
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

    [Serializable]
    public class ChatGPTRequest
    {
        public string model = "gpt-4o";
        public Message[] messages;

        public ChatGPTRequest(string userPrompt)
        {
            messages = new Message[] { new Message("user", userPrompt) };
        }
    }

    [Serializable]
    public class ChatGPTChoice
    {
        public Message message;
    }

    [Serializable]
    public class ChatGPTResponse
    {
        public ChatGPTChoice[] choices;
    }
}
