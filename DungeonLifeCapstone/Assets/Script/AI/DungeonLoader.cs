using UnityEngine;
using Newtonsoft.Json;

public class DungeonLoader : MonoBehaviour
{
    public TextAsset dungeonJson; 
    private DungeonData runtimeData; 

    // Called by ProceduralDungeonGenerator
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
        DungeonData data = JsonConvert.DeserializeObject<DungeonData>(jsonData);
        return data;
    }

    public DungeonData GetDungeonData()
    {
        // Use procedural data if available
        //if (runtimeData != null)
        //    return runtimeData;

        // Fallback to static JSON
        return LoadFromJSON();
    }
}
