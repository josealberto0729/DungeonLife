using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EnemyAI[] enemies = GetComponentsInChildren<EnemyAI>();
            foreach (EnemyAI enemy in enemies)
            {
                enemy.TransitionToState(new ChaseState());
                Debug.Log(enemies + "is chasing" + collision.gameObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EnemyAI[] enemies = GetComponentsInChildren<EnemyAI>();
            foreach (EnemyAI enemy in enemies)
            {
                enemy.TransitionToState(new PatrolState());
                Debug.Log(enemies + "has stop chasing" + collision.gameObject);
            }
        }
    }
}
