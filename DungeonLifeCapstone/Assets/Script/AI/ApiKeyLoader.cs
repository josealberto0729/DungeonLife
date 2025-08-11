using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

public static class ApiKeyLoader
{
    private const string passphrase = "MyStrongPassphrase123!"; // same as in editor

    public static string LoadApiKey()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "api_key.txt");

        if (!File.Exists(filePath))
        {
            Debug.LogError("API key file not found!");
            return null;
        }

        string encryptedData = File.ReadAllText(filePath);
        return Decrypt(encryptedData, passphrase);
    }

    private static string Decrypt(string encryptedText, string passphrase)
    {
        byte[] combinedBytes = Convert.FromBase64String(encryptedText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(passphrase));

            byte[] iv = new byte[16];
            Array.Copy(combinedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            int cipherTextLength = combinedBytes.Length - iv.Length;
            byte[] cipherText = new byte[cipherTextLength];
            Array.Copy(combinedBytes, iv.Length, cipherText, 0, cipherTextLength);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
    }
}
