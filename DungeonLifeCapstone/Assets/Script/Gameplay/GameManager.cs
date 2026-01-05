using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public enum GameState
{
    GameInit,
    GameStart,
    GamePause,
    GameEnd
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public PlayerStatsHandler player;
    public PlayerStatsSO playerStatsSO;
    public UpgradeUI upgrade;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        ResetPlayerStats();
    }

    public void ResetPlayerStats()
    {
        playerStatsSO.maxHealth = 100f;
        playerStatsSO.currentHealth = playerStatsSO.maxHealth;
        playerStatsSO.moveSpeed = 5f;
        playerStatsSO.meleeDamage = 20f;
        playerStatsSO.rangedDamage = 20f;
        playerStatsSO.attackCooldown = 1f;
    }

    public void ResumeIngameView()
    {
        MenuController.Instance.SwitchMenu(MenuIndex.Ingame);
    }

    public void GameStart()
    {

    }
    public void GameRestart()
    {

    }
    public void GameEnded()
    {

    }

}
