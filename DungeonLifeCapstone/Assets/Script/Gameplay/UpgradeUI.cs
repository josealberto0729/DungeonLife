using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public List<UpgradeSO> allUpgrades;
    private PlayerStatsHandler player;
    void Start()
    {
        player = GameManager.Instance.player;
        gameObject.SetActive(false); // hidden by default
        Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Clear old buttons
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        // Pick 3 random upgrades
        List<UpgradeSO> chosen = new List<UpgradeSO>();
        while (chosen.Count < 3)
        {
            UpgradeSO candidate = allUpgrades[Random.Range(0, allUpgrades.Count)];
            if (!chosen.Contains(candidate))
                chosen.Add(candidate);
        }

        // Spawn buttons
        foreach (var upgrade in chosen)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonParent);
            Button btn = btnObj.GetComponent<Button>();

            // update visuals
            //btnObj.GetComponentInChildren<TMP_Text>().text = upgrade.upgradeName;
            btnObj.GetComponentInChildren<Image>().sprite = upgrade.icon;

            // assign click action
            btn.onClick.AddListener(() =>
            {
                ApplyUpgrade(upgrade);
                gameObject.SetActive(false); // hide after selection
            });
        }
    }
    private void ApplyUpgrade(UpgradeSO upgrade)
    {
        player = GameManager.Instance.player;
        switch (upgrade.type)
        {
            case UpgradeSO.UpgradeType.MaxHealth:
                player.stats.maxHealth *= (1f + upgrade.value / 100f);
                //player.stats.currentHealth = player.stats.maxHealth;
                MenuController.Instance.ShowIngameView();
                MenuController.Instance.UpdateUI(player.stats.currentHealth);
                break;
            case UpgradeSO.UpgradeType.Damage:
                player.stats.meleeDamage *= (1f + upgrade.value / 100f);
                player.stats.rangedDamage *= (1f + upgrade.value / 100f);
                MenuController.Instance.ShowIngameView();
                break;
            case UpgradeSO.UpgradeType.AttackSpeed:
                player.stats.attackCooldown *= (1f - upgrade.value / 100f);
                player.stats.attackCooldown = Mathf.Max(0.1f, player.stats.attackCooldown);
                MenuController.Instance.ShowIngameView();
                break;
            case UpgradeSO.UpgradeType.Heal:
                player.stats.currentHealth = player.stats.maxHealth;
                MenuController.Instance.ShowIngameView();
                MenuController.Instance.UpdateUI(player.stats.currentHealth);
                break;
        }

        Debug.Log($"Applied upgrade: {upgrade.upgradeName}");
    }

    //public void Hide()
    //{
    //    panel.SetActive(false);
    //    Time.timeScale = 1f;
    //}

    //public void AddMaxHealth()
    //{
    //    playerStats.maxHealth += 20f;
    //    playerStats.currentHealth = playerStats.maxHealth; 
    //    Hide();
    //}

    //public void OnDamage()
    //{
    //    playerStats.meleeDamage += 5f;
    //    playerStats.rangedDamage += 5f;
    //    Hide();
    //}

    //public void OnAttackSpeed()
    //{
    //    playerStats.attackCooldown = Mathf.Max(0.1f, playerStats.attackCooldown * 0.9f);
    //    Hide();
    //}

    //public void OnHeal()
    //{
    //    playerStats.currentHealth = playerStats.maxHealth;
    //    Hide();
    //}
}
