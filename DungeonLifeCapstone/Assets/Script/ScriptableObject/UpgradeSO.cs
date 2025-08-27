using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    public string upgradeName;
    public Sprite icon;
    public string description;

    public enum UpgradeType { MaxHealth, Damage, AttackSpeed, Heal }
    public UpgradeType type;
    public float value;
}
