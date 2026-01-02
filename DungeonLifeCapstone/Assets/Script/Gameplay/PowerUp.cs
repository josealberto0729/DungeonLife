using UnityEngine;

public class PowerUp : MonoBehaviour
{
    private PlayerStatsHandler player;
    public UpgradeSO upgrade;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameManager.Instance.player;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void ApplyPowerUp(UpgradeSO upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeSO.UpgradeType.MaxHealth:
                player.stats.maxHealth *= (1f + upgrade.value / 100f);
                MenuController.Instance.UpdateUI(player.stats.currentHealth);
                break;
            case UpgradeSO.UpgradeType.Damage:
                player.stats.meleeDamage *= (1f + upgrade.value / 100f);
                player.stats.rangedDamage *= (1f + upgrade.value / 100f);
                break;
            case UpgradeSO.UpgradeType.AttackSpeed:
                player.stats.attackCooldown *= (1f - upgrade.value / 100f);
                player.stats.attackCooldown = Mathf.Max(0.1f, player.stats.attackCooldown);
                break;
            case UpgradeSO.UpgradeType.Heal:
                player.stats.currentHealth = player.stats.maxHealth;
                MenuController.Instance.UpdateUI(player.stats.currentHealth);
                break;
        }

        Debug.Log($"Applied upgrade: {upgrade.upgradeName}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            //ApplyPowerUp(upgrade);
            Destroy(gameObject);
        }
    }

}
