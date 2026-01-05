using UnityEngine;
using UnityEngine.UI;

public class IngameView : MonoBehaviour
{

    [SerializeField] Image healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public void UpdateHealth(float percentage)
    {
        Debug.Log("health bar update :" + percentage);
        percentage = (percentage / GameManager.Instance.player.stats.maxHealth) * 100f;
        float normalized = Mathf.Clamp01(percentage / 100f);
        healthBar.fillAmount = normalized;
    }
}
