using GameLogic.Runtime.Level;
using JKFrame;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    [UIWindowData(typeof(LevelRewardUI),false,"Assets/_EchoAddressable/UI/LevelRewardUI.prefab",0)]
    public class LevelRewardUI : UI_WindowBase
    {
        public RewardItem rewardItem;
        public RewardItem rewardItem2;
        public RewardItem rewardItem3;
        
        public Button sureBtn;

        public override void Init()
        {
            base.Init();
            
            sureBtn.onClick.AddListener(() =>
            {
                sureBtn.interactable = false;
                
                NetworkClient.Send(new LevelReadyMessage());
            });
            sureBtn.interactable = false;
        }

        public void InitRewardItem(RewardDataSo rewardData,RewardDataSo rewardData2,RewardDataSo rewardData3)
        {
            rewardItem.Init(rewardData);
            rewardItem2.Init(rewardData2);
            rewardItem3.Init(rewardData3);
        }

        public void CloseAllItemClick()
        {
            rewardItem.choseBtn.interactable = false;
            rewardItem2.choseBtn.interactable = false;
            rewardItem3.choseBtn.interactable = false;
            sureBtn.interactable = true;
        }
    }
}