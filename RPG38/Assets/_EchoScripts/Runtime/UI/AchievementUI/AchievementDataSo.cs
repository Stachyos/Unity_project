using UnityEngine;

namespace GameLogic.Runtime
{
    [CreateAssetMenu(fileName = "AchievementDataSo")]
    public class AchievementDataSo : ScriptableObject
    {
        public string id;
        public string desc;
        public string effectDesc;
        public int buffId;
    }
}