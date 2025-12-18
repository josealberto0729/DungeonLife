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
            DungeonLoader.Instance.CheckToGenerateNewDungeons();
            DungeonLoader.Instance.LoadNextDungeon();
            DungeonSpawner.Instance.GenerateNextLevel();
            Destroy(this.gameObject);
        }
    }

}
