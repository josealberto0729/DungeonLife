using UnityEngine;

public class ChaseState : EnemyState
{
    public override void EnterState(EnemyAI enemy)
    {
        enemy.OnChaseEnter?.Invoke();
    }

    public override void UpdateState(EnemyAI enemy)
    {
        //if (!enemy.IsPlayerInRange(enemy.detectionRange))
        //{
        //    enemy.TransitionToState(new PatrolState());
        //}
        if (enemy.IsPlayerInRange(enemy.attackRange))
        {
            enemy.TransitionToState(new AttackState());
        }
    }

    public override void FixedUpdateState(EnemyAI enemy)
    {
        Vector2 direction = (enemy.player.position - enemy.transform.position).normalized;
        enemy.rb.MovePosition(enemy.rb.position + direction * enemy.moveSpeed * Time.fixedDeltaTime);
    }

    public override void ExitState(EnemyAI enemy) { }
}
