using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerStatsHandler : MonoBehaviour
{
    public PlayerStatsSO stats;
    //public static PlayerStatsHandler Instance { get; private set; }

    //public MVC mVC;


    private void Awake()
    {
        //if (Instance != null && Instance != this) Destroy(gameObject);
        //else Instance = this;
        // Ensure current health starts full
        stats.currentHealth = stats.maxHealth;
        //if (retryPanel == null)
        //{
        //    retryPanel = GameObject.Find("Retry"); 
        //}

    }
    private void OnEnable()
    {
        MenuController.Instance.ShowIngameView();
    }
    public void Start()
    {
        GameManager.Instance.player = this;
        MenuController.Instance.UpdateUI((stats.currentHealth / stats.maxHealth) * 100);
    }

    public void TakeDamage(float amount)
    {
        stats.currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {stats.currentHealth}");
        MenuController.Instance.UpdateUI(stats.currentHealth);
        if (stats.currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
        //mVC.ShowPanel();
        MenuController.Instance.SwitchMenu(MenuIndex.Gameover);
        Destroy(gameObject); 
    }

}
