using UnityEngine;

public class DeadState : EnemyState
{
    public override void EnterState(EnemyAI enemy)
    {
        enemy.OnDeadEnter?.Invoke();
        Debug.Log("Enemy died.");
        GameObject.Destroy(enemy.gameObject);
    }

    public override void UpdateState(EnemyAI enemy) { }

    public override void FixedUpdateState(EnemyAI enemy) { }

    public override void ExitState(EnemyAI enemy) { }
}
