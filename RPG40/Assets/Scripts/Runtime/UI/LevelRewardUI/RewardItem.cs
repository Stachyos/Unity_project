using System;
using JKFrame;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
   
    public class RewardItem : MonoBehaviour
    {
        public TMP_Text nameDesc;
        public TMP_Text effectDesc;
        public Button choseBtn;
        public RewardDataSo rewardData;
        private void Start()
        {
            choseBtn.onClick.AddListener(() =>
            {
                if(rewardData ==null)
                    return;

                var window = UISystem.GetWindow<LevelRewardUI>();
                if(window == null)
                    return;
                window.CloseAllItemClick();
                
                //Send the selected reward ID to have the buff activated in the server area.
                var netPlayerCtrl = NetworkClient.connection.identity.GetComponent<EchoNetPlayerCtrl>();
                netPlayerCtrl.AddReward(this.rewardData.rewardID);
            });
        }

        public void Init(RewardDataSo data)
        {
            this.rewardData = data;
            nameDesc.text = data.rewardName;
            effectDesc.text = data.rewardDesc;
        }
    }
}