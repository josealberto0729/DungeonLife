using UnityEngine;

public class MainMenuView : MonoBehaviour
{
    public void GameStart()
    {
        MenuController.Instance.SwitchMenu(MenuIndex.Ingame);
    }
}
