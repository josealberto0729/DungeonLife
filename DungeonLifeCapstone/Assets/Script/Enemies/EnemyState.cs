using UnityEngine;
public abstract class EnemyState
{
    public abstract void EnterState(EnemyAI enemy);
    public abstract void UpdateState(EnemyAI enemy);
    public abstract void FixedUpdateState(EnemyAI enemy);
    public abstract void ExitState(EnemyAI enemy);
}
