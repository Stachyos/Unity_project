using System.Collections.Generic;
using JKFrame;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in JKFrame, I override part of its function.
    /// </summary>
 
    [UIWindowData(typeof(WinUI),false,"Assets/Addressable/UI/WinUI.prefab",0)]
    public class WinUI : UI_WindowBase
    {
        public Button quitBtn;
        public SimpleScrollerDelegate scrollDelegate;

        public override void Init()
        {
            base.Init();
            //acquire achienement
            //UISystem.AddTips("make achievements");
            StringEventSystem.Global.Send(EventKey.AddAchievement, 1001);
            
            quitBtn.onClick.AddListener(() =>
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    NetworkManager.singleton.StopHost();
                }else if (NetworkClient.isConnected)
                {
                    NetworkManager.singleton.StopClient();
                }

                
                
                UISystem.CloseAllWindow();
                
            });
            
            var achievementData = GameHub.Interface.GetModel<CacheModel>().GetAchievement();
            List<AchievementCellData> achievementCellDatas = new List<AchievementCellData>();
            foreach (var dataID in achievementData)
            {
                var cellData = new AchievementCellData();
                cellData.id = dataID;
                achievementCellDatas.Add(cellData);
            }
            scrollDelegate.AddCellRange(achievementCellDatas);
            scrollDelegate.ReloadData();
        }
    }
}