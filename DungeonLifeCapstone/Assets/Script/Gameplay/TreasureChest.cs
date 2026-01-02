using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    public bool isOpen = false;
    private bool playerInRange;
    public UpgradeUI upgradeUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (upgradeUI == null)
            upgradeUI = GameManager.Instance.upgrade;
    }
    void Start()
    {
        //if (upgradeUI != null)
        //    upgradeUI.Show();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInRange && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            OpenChest();
        }
    }
    void OpenChest()
    {
        isOpen = true;

        MenuController.Instance.ShowRoomReward();
        upgradeUI.Show();

        Debug.Log("Chest opened!");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
