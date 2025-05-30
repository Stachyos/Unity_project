using UnityEngine;
using UnityEngine.Serialization;

namespace GameLogic.Runtime
{
    [CreateAssetMenu(fileName = "RewardDataSo")]
    public class RewardDataSo : ScriptableObject
    {
        public int rewardID;
        public string rewardName;
        public string rewardDesc;
        public int itemID;
    }



    public enum ItemType
    {
        Equipment,
    }
}