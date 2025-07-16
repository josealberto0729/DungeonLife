using UnityEngine;

public class AttackState : EnemyState
{
    float attackCooldown = 1f;
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
        else if (Time.time - lastAttackTime >= attackCooldown)
        {
            // TODO: Implement actual damage logic
            Debug.Log("Enemy attacks player!");
            lastAttackTime = Time.time;
            enemy.OnAttackPerformed?.Invoke();
        }
    }

    public override void FixedUpdateState(EnemyAI enemy) { }

    public override void ExitState(EnemyAI enemy) { }
}
