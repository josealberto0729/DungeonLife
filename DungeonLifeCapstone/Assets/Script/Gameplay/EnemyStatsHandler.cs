using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStatsHandler : MonoBehaviour
{
    public EnemyStatsSO stats;
    public UnityEvent onTakeDamage;
    public UnityEvent onDeath;
    public RoomManager roomManager;

    private float lastAttackTime;
    private void Awake()
    {
        // clone so each enemy has its own runtime stats
        stats = Instantiate(stats);
        stats.currentHealth = stats.maxHealth;
    }
    public void TakeDamage(float amount)
    {
        onTakeDamage.Invoke();
        stats.currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {stats.currentHealth}");

        if (stats.currentHealth <= 0)
        {
            Debug.Log("dying");
            Die();
        }
    }
    private void Die()
    {
        //roomManager = gameObject.GetComponentInParent<RoomManager>();
        //roomManager.CheckEnemies();
        //Debug.Log($"{gameObject.name} has died!");
        //onDeath.Invoke();
        //Destroy(gameObject); // or trigger loot drop, respawn, etc.
        if (roomManager == null)
            roomManager = GetComponentInParent<RoomManager>();
        if (roomManager != null)
        {
            Debug.Log("cleaning list");
            roomManager.OnEnemyDied(this); 
        }
            

        Debug.Log($"{gameObject.name} has died!");
        onDeath.Invoke();

        Destroy(gameObject);
    }

    public void DealMeleeDamage(GameObject target)
    {
        if (Time.time - lastAttackTime < stats.attackCooldown)
            return; // still on cooldown

        lastAttackTime = Time.time;

        // Try to damage a player
        PlayerStatsHandler player = target.GetComponent<PlayerStatsHandler>();
        if (player != null)
        {
            player.TakeDamage(stats.meleeDamage);
            Debug.Log($"{gameObject.name} hit {target.name} for {stats.meleeDamage} damage");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
