using UnityEngine;
using UnityEngine.Events;

public class EnemyStatsHandler : MonoBehaviour
{
    public EnemyStatsSO stats;
    public UnityEvent onTakeDamage;
    public UnityEvent onDeath;
    public RoomManager roomManager;

    [Header("Boss Settings")]
    public bool isBoss = false;
    public UnityEvent onBossDeath;

    private bool isDead = false;
    private float lastAttackTime;
    private void Awake()
    {
        // clone so each enemy has its own runtime stats
        stats = Instantiate(stats);
        stats.currentHealth = stats.maxHealth;
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;
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
        if (isDead) return;
        isDead = true;
        if (roomManager == null)
            roomManager = GetComponentInParent<RoomManager>();
        if (roomManager != null)
        {
            Debug.Log("cleaning list");
            roomManager.OnEnemyDied(this); 
        }
        if (isBoss)
        {
            Debug.Log("Boss defeated! Triggering boss mechanics.");
            onBossDeath.Invoke();
            OpenAIDungeonGenerator.Instance.onJsonGenerated.RemoveAllListeners();
            OpenAIDungeonGenerator.Instance.onJsonGenerated.AddListener(() =>
            {
                Debug.Log("Spawning portal since JSON is ready!");
                DungeonSpawner.Instance.SpawnVictoryPortal();
            });
            OpenAIDungeonGenerator.Instance.CallGenerateNewJson();
            
        }

        Debug.Log($"{gameObject.name} has died!");
        onDeath.Invoke();

        Destroy(gameObject,0.5f);
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
