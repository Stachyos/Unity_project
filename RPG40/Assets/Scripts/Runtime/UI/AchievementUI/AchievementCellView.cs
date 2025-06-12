using JKFrame;
using TMPro;

namespace GameLogic.Runtime.UI.AchievementUI
{
    /// <summary>
    /// This class inherits a class in EnhancedScroller v2, I override part of its function.
    /// </summary>
    public class AchievementCellView : CellView
    {
        public TMP_Text desc;
        public TMP_Text effectDesc;

        public override void SetData(CellData data)
        {
            base.SetData(data);
            
            AchievementCellData achievementCellData = data as AchievementCellData;
            var id = achievementCellData.id;
            var dataSo = ResSystem.LoadAsset<AchievementDataSo>($"Assets/Addressable/DataSo/AchievementDataSo_{id}.asset");
            this.desc.text = dataSo.desc;
            this.effectDesc.text = dataSo.effectDesc;
        }
    }
}