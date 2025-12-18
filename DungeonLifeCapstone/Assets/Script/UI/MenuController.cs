using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static Cinemachine.DocumentationSortingAttribute;

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

    public Button startButton;  

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
        LLMJsonCreator.Instance.onJsonGenerated.AddListener(StartButtonActivate);
        ShowMainMenuView();
        CheckLevel();
    }

    void StartButtonActivate()
    {
        startButton.interactable = true;
    }
    public void CheckLevel()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Level");
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Dungeon folder not found at: {folderPath}");
            return;
        }
        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
        if (jsonFiles.Length == 0)
        {
            startButton.interactable = false;
            Debug.Log($"No JSON files found in folder: {folderPath}");
            LLMJsonCreator.Instance.StartJsonGeneration();
            return;
        }
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
