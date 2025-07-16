using UnityEngine;

public class IdleState : EnemyState
{
    float idleTime;
    float elapsedTime;

    public override void EnterState(EnemyAI enemy)
    {
        idleTime = Random.Range(1f, 3f);
        elapsedTime = 0f;
        enemy.OnIdleEnter?.Invoke();
    }

    public override void UpdateState(EnemyAI enemy)
    {
        elapsedTime += Time.deltaTime;

        if (enemy.IsPlayerInRange(enemy.detectionRange))
        {
            enemy.TransitionToState(new ChaseState());
        }
        else if (elapsedTime >= idleTime)
        {
            enemy.TransitionToState(new PatrolState());
        }
    }

    public override void FixedUpdateState(EnemyAI enemy) { }

    public override void ExitState(EnemyAI enemy) { }
}
