using System.Collections.Generic;
using JKFrame;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    [UIWindowData(typeof(FailUI),false,"Assets/_EchoAddressable/UI/FailUI.prefab",0)]
    public class FailUI : UI_WindowBase
    {
        public Button quitBtn;
        public SimpleScrollerDelegate scrollDelegate;

        public override void Init()
        {
            base.Init();
            
            quitBtn.onClick.AddListener(() =>
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                    NetworkManager.singleton.StopHost();
                else if (NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();
                
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