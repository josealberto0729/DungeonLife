using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

public class OpenAiKeyEncryptor : EditorWindow
{
    private string apiKey = "";
    private const string passphrase = "MyStrongPassphrase123!"; // You can keep this outside GitHub

    [MenuItem("Tools/OpenAI/Encrypt API Key")]
    public static void ShowWindow()
    {
        GetWindow<OpenAiKeyEncryptor>("OpenAI Key Encryptor");
    }

    void OnGUI()
    {
        GUILayout.Label("OpenAI API Key Encryptor", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);

        if (GUILayout.Button("Encrypt & Save"))
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorUtility.DisplayDialog("Error", "Please enter an API key", "OK");
                return;
            }

            string encrypted = Encrypt(apiKey, passphrase);

            string savePath = Application.streamingAssetsPath;
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            File.WriteAllText(Path.Combine(savePath, "api_key.txt"), encrypted);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "API Key encrypted and saved to StreamingAssets/api_key.txt", "OK");
        }
    }

    private static string Encrypt(string plainText, string passphrase)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(passphrase)); // 32 bytes AES-256
            aes.GenerateIV(); // Random IV for security

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Combine IV + encrypted data
                byte[] combinedBytes = new byte[aes.IV.Length + encryptedBytes.Length];
                Array.Copy(aes.IV, 0, combinedBytes, 0, aes.IV.Length);
                Array.Copy(encryptedBytes, 0, combinedBytes, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(combinedBytes);
            }
        }
    }
}
