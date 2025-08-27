using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;

public class EnemyAI : MonoBehaviour
{
    public EnemyState currentState;
    public Transform player;
    public float detectionRange = 5f;
    public float attackRange = 1.2f;
    public float moveSpeed = 2f;
    public Rigidbody2D rb;

    public Vector2[] patrolPoints;
    public int currentPatrolIndex = 0;

    [Header("Unity Events")]
    public UnityEvent OnIdleEnter;
    public UnityEvent OnPatrolEnter;
    public UnityEvent OnChaseEnter;
    public UnityEvent OnAttackEnter;
    public UnityEvent OnDeadEnter;

    public UnityEvent OnAttackPerformed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        TransitionToState(new IdleState());
        player = DungeonSpawner.Instance.player.transform;
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.UpdateState(this);
    }
    private void FixedUpdate()
    {
        currentState?.FixedUpdateState(this);
    }
    public void TransitionToState(EnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState?.EnterState(this);
    }
    public bool IsPlayerInRange(float range)
    {
        return player != null && Vector2.Distance(transform.position, player.position) <= range;
    }
    public void SetPatrolPoints(Vector2[] points)
    {
        patrolPoints = points;
        currentPatrolIndex = 0;
    }

}
