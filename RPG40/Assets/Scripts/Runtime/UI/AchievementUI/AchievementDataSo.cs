using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Unity
    /// </summary>

    [CreateAssetMenu(fileName = "AchievementDataSo")]
    public class AchievementDataSo : ScriptableObject
    {
        public string id;
        public string desc;
        public string effectDesc;
        public int buffId;
    }
}