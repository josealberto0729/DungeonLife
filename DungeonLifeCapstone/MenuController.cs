using Unity.VisualScripting;
using UnityEngine;

public class MenuController : Singleton<MenuController>
{
    [SerializeField] GameoverView gameoverView;
    [SerializeField] RoomRewardView roomRewardView;
    [SerializeField] IngameView ingameView;

    public void UpdateU(float percentage)
    {
        ingameView.UpdateHealth(percentage);
    }

}
