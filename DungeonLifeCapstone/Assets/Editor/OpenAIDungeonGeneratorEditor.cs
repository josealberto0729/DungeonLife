using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OpenAIDungeonGenerator))]
public class OpenAIDungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        OpenAIDungeonGenerator generator = (OpenAIDungeonGenerator)target;

        generator.promptFileName = EditorGUILayout.TextArea(generator.promptFileName, GUILayout.Height(60));
        generator.apiKey = EditorGUILayout.PasswordField("API Key", generator.apiKey);
        generator.model = EditorGUILayout.TextField("Model", generator.model);

        if (GUILayout.Button("Generate Dungeon"))
        {
            generator.StartCoroutine(generator.GenerateDungeonFromOpenAI());
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(generator);
    }
}
