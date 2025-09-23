using UnityEngine;

public class GameoverView : MonoBehaviour
{
    public GameObject retryPanel;

    public void ShowPanel()
    {
        retryPanel.SetActive(true);
    }

    public void HidePanel()
    {
        retryPanel.SetActive(false);
    }
}
