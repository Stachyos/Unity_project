using JKFrame;
using TMPro;

namespace GameLogic.Runtime.UI.AchievementUI
{
    public class AchievementCellView : CellView
    {
        public TMP_Text desc;
        public TMP_Text effectDesc;

        public override void SetData(CellData data)
        {
            base.SetData(data);
            
            AchievementCellData achievementCellData = data as AchievementCellData;
            var id = achievementCellData.id;
            var dataSo = ResSystem.LoadAsset<AchievementDataSo>($"Assets/_EchoAddressable/DataSo/AchievementDataSo_{id}.asset");
            this.desc.text = dataSo.desc;
            this.effectDesc.text = dataSo.effectDesc;
        }
    }
}