using UnityEngine;

public class PlayerStatsHandler : MonoBehaviour
{
    public PlayerStatsSO stats;

    private void Awake()
    {
        // Ensure current health starts full
        stats.currentHealth = stats.maxHealth;
    }

    public void TakeDamage(float amount)
    {
        stats.currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {stats.currentHealth}");

        if (stats.currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
        Destroy(gameObject); // or trigger respawn, disable, etc.
    }
}
