using UnityEngine;

[CreateAssetMenu(menuName = "Character/Enemy Stats")]
public class EnemyStatsSO : ScriptableObject
{
    public int enemyID;
    public string type;
    [Header("Progression")]
    public int level;
    [Header("Health")]
    public float maxHealth;
    public float currentHealth;
    [Header("Combat")]
    public float attackDamage;
    public float attackCooldown;
}