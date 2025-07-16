using UnityEngine;

public class PatrolState : EnemyState
{
    public override void EnterState(EnemyAI enemy) 
    {
        enemy.OnPatrolEnter?.Invoke();
    }

    public override void UpdateState(EnemyAI enemy)
    {
        if (enemy.patrolPoints.Length == 0) return;

        Vector2 target = enemy.patrolPoints[enemy.currentPatrolIndex];
        if (Vector2.Distance(enemy.transform.position, target) < 0.1f)
        {
            enemy.currentPatrolIndex = (enemy.currentPatrolIndex + 1) % enemy.patrolPoints.Length;
        }

        if (enemy.IsPlayerInRange(enemy.detectionRange))
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    public override void FixedUpdateState(EnemyAI enemy)
    {
        Vector2 target = enemy.patrolPoints[enemy.currentPatrolIndex];
        Vector2 direction = (target - (Vector2)enemy.transform.position).normalized;
        enemy.rb.MovePosition(enemy.rb.position + direction * enemy.moveSpeed * Time.fixedDeltaTime);
    }

    public override void ExitState(EnemyAI enemy) { }
}
