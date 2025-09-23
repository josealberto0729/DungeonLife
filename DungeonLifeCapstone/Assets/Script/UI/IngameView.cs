using UnityEngine;
using UnityEngine.UI;

public class IngameView : MonoBehaviour
{

    [SerializeField] Image healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public void UpdateHealth(float percentage)
    {
        Debug.Log("health bar update :" + percentage);
        healthBar.fillAmount = percentage / 100;
    }
}
