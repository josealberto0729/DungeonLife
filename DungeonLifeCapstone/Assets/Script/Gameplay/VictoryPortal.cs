using UnityEngine;

public class VictoryPortal : MonoBehaviour
{
    private bool isActive = false;
    private bool hasActivated = false;

    private void Start()
    {
        isActive = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || hasActivated) return;

        if (other.CompareTag("Player"))
        {
            hasActivated = true; // prevent multiple triggers
            Debug.Log("Player entered Victory Portal → Loading new dungeon");
            //DungeonData newDungeon = OpenAIDungeonGenerator.Instance.generatedDungeon;

            if (DungeonSpawner.Instance != null)
            {
                DungeonSpawner.Instance.GenerateNextLevel();
                Destroy(this.gameObject);
            }
            else
            {
                Debug.LogError("No generated dungeon found!");
            }
        }
    }

}
