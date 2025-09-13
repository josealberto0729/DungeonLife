using UnityEngine;

public class AttackState : EnemyState
{
    float lastAttackTime = 0f;

    public override void EnterState(EnemyAI enemy)
    {
        enemy.OnAttackEnter?.Invoke();
    }

    public override void UpdateState(EnemyAI enemy)
    {
        if (!enemy.IsPlayerInRange(enemy.attackRange))
        {
            enemy.TransitionToState(new ChaseState());
        }
        else if (Time.time - lastAttackTime >= enemy.stats.attackCooldown)
        {
            if (enemy.isRanged)
            {
                if (!enemy.IsPlayerInRange(enemy.firingRange))
                {
                    enemy.TransitionToState(new ChaseState());
                    return;
                }
            }
            else
            {
                if (!enemy.IsPlayerInRange(enemy.attackRange))
                {
                    enemy.TransitionToState(new ChaseState());
                    return;
                }
            }
            if (enemy.isRanged)
            {
                PerformRangedAttack(enemy);
            }
            else
            {
                PerformMeleeAttack(enemy);
            }

            lastAttackTime = Time.time;
            enemy.OnAttackPerformed?.Invoke();
        }
    }

    public override void FixedUpdateState(EnemyAI enemy) { }

    public override void ExitState(EnemyAI enemy) { }

    void PerformMeleeAttack(EnemyAI enemy)
    {
        if (enemy.player != null)
        {
            PlayerStatsHandler playerStats = enemy.player.GetComponent<PlayerStatsHandler>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(enemy.stats.meleeDamage);
                Debug.Log($"{enemy.name} hit {enemy.player.name} for {enemy.stats.meleeDamage} melee damage!");
            }
        }
    }

    void PerformRangedAttack(EnemyAI enemy)
    {
        if (enemy.player == null || enemy.projectilePrefab == null || enemy.firePoint == null)
            return;

        // Direction to player
        Vector3 direction = (enemy.player.position - enemy.firePoint.position).normalized;

        // Spawn projectile
        GameObject proj = Object.Instantiate(enemy.projectilePrefab, enemy.firePoint.position, Quaternion.identity);

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * enemy.projectileSpeed;
        }

        // Assign owner
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.owner = enemy.gameObject;
            projectile.baseDamage = enemy.stats.rangeAttackDamage;
        }

        Debug.Log($"{enemy.name} shot a projectile at {enemy.player.name}");
    }
}
