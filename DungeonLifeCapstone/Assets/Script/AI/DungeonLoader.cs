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
        DungeonData data = JsonConvert.DeserializeObject<DungeonData>(jsonData);
        return data;
    }

    public DungeonData GetDungeonData()
    {
        return LoadFromJSON();
    }
}
