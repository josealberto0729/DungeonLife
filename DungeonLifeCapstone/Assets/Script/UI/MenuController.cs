using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum MenuIndex
{
    MainMenu = 0,
    Ingame = 1,
    Gameover = 2,
    RoomReward = 3
}

public class MenuController : MonoBehaviour
{
    [SerializeField] GameoverView gameoverView;
    [SerializeField] RoomRewardView roomRewardView;
    [SerializeField] IngameView ingameView;
    public static MenuController Instance { get; private set; }

    [SerializeField] List<GameObject> menuList = new List<GameObject>();

    public void SwitchMenu(MenuIndex index)
    {
        foreach(GameObject menu in menuList)
        {
            menu.SetActive(false);
        }
        menuList[(int)index].SetActive(true);
    }

    private void Start()
    {
        ShowMainMenuView();
    }





    public void UpdateUI(float percentage)
    {
        ingameView.UpdateHealth(percentage);
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowRoomReward()
    {
        SwitchMenu(MenuIndex.RoomReward);
    }
    public void ShowIngameView()
    {
        SwitchMenu(MenuIndex.Ingame);
    }
    public void ShowGameOverView()
    {
        SwitchMenu(MenuIndex.Gameover);
    }
    public void ShowMainMenuView()
    {
        SwitchMenu(MenuIndex.MainMenu);
    }
}
