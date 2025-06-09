using UnityEngine;

[CreateAssetMenu(menuName = "Character/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    public int playerID;
    [Header("Progression")]
    public float curExp;
    public int level;
    [Header("Health")]
    public float maxHealth;
    public float currentHealth;
    [Header("Movement")]
    public float moveSpeed;
    [Header("Combat")]
    public float meleeDamage;
    public float rangedDamage;
    public float attackCooldown;

}
