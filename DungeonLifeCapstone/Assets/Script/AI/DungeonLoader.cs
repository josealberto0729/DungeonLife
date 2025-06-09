using UnityEngine;
using Newtonsoft.Json;

public class DungeonLoader : MonoBehaviour
{
    public TextAsset dungeonJson; 

    private DungeonData LoadDungeonData()
    {
        string jsonData = dungeonJson.text;
        DungeonData data = JsonConvert.DeserializeObject<DungeonData>(jsonData);
        return data;
    }

    public DungeonData GetDungeonData()
    {
        return LoadDungeonData();
    }
}
