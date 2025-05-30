using System.Collections.Generic;
using JKFrame;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    [UIWindowData(typeof(AchievementUI),false,"Assets/_EchoAddressable/UI/AchievementUI.prefab",0)]
    public class AchievementUI : UI_WindowBase
    {
        public AchievementScrollDelegate scrollDelegate;
        public Button closeButton;
        public override void Init()
        {
            base.Init();

            var achievementData = GameHub.Interface.GetModel<AchievementModel>().GetAchievement();
            List<AchievementCellData> achievementCellDatas = new List<AchievementCellData>();
            foreach (var dataID in achievementData.ids)
            {
                var cellData = new AchievementCellData();
                cellData.id = dataID;
                achievementCellDatas.Add(cellData);
            }
            scrollDelegate.AddCellRange(achievementCellDatas);
            scrollDelegate.ReloadData();
            
            closeButton.onClick.AddListener(UISystem.Close<AchievementUI>);
        }
    }
}