using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public List<EnemyAI> enemies = new List<EnemyAI>();
    public UpgradeUI upgradeUI; 

    void Awake()
    {
        if (upgradeUI == null)
            upgradeUI = GameManager.Instance.upgrade;
    }

    public void FillEnemyList()
    {
        enemies.Clear();
        enemies.AddRange(GetComponentsInChildren<EnemyAI>());
    }
    public void CheckEnemies()
    {
        enemies.RemoveAll(e => e == null || e.Equals(null));
        if (enemies.Count == 0)
        {
            if (upgradeUI != null)
                upgradeUI.Show();
        }
    }

    public void OnEnemyDied(EnemyStatsHandler enemy)
    {
        enemies.Remove(enemy.GetComponent<EnemyAI>());
        if (enemies.Count == 0)
        {
            MenuController.Instance.ShowRoomReward();
            upgradeUI.Show();
        }
    }
}
