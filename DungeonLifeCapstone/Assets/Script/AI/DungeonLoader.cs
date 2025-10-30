using UnityEngine;
using Newtonsoft.Json;

public class DungeonLoader : MonoBehaviour
{
    public TextAsset dungeonJson; 
    private DungeonData runtimeData;

    public void SetDungeonData(DungeonData data)
    {
        runtimeData = data;
    }

    private DungeonData LoadFromJSON()
    {
        if (dungeonJson == null)
        {
            Debug.LogError("Dungeon JSON not assigned!");
            return null;
        }

        string jsonData = dungeonJson.text;
        Debug.Log("Cur JSON = " + jsonData);

        DungeonData data = null;
        try
        {
            data = JsonConvert.DeserializeObject<DungeonData>(jsonData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to deserialize dungeon JSON: {ex.Message}");
            return null;
        }

        if (data == null)
        {
            Debug.LogWarning("Deserialized DungeonData is null.");
            return null;
        }

        // Cache the loaded data so runtimeData is available to callers
        runtimeData = data;
        return runtimeData;
    }

    public DungeonData GetDungeonData(bool forceReload = false)
    {
        if (!forceReload && runtimeData != null)
            return runtimeData;

        return LoadFromJSON();
    }
}
