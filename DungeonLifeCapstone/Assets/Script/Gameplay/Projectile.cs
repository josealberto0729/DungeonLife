using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifetime = 3f;
    public GameObject owner; // who shot this projectile
    public float baseDamage = 10f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == owner)
            return;

        float damageAmount = 0f;

        // Get damage from owner's stats (if available)
        if (owner != null)
        {
            PlayerStatsHandler ownerStats = owner.GetComponent<PlayerStatsHandler>();
            if (ownerStats != null)
            {
                damageAmount = ownerStats.stats.rangedDamage;
            }

            EnemyStatsHandler enemyOwnerStats = owner.GetComponent<EnemyStatsHandler>();
            if (enemyOwnerStats != null)
            {
                damageAmount = enemyOwnerStats.stats.rangeAttackDamage;
            }
        }

        // Apply damage if the target has stats
        PlayerStatsHandler player = collision.gameObject.GetComponent<PlayerStatsHandler>();
        if (player != null)
        {
            player.TakeDamage(damageAmount);
        }

        EnemyStatsHandler enemy = collision.gameObject.GetComponent<EnemyStatsHandler>();
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
        }

        Destroy(gameObject);
    }
}
