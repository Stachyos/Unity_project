using UnityEngine;

namespace GameLogic.Runtime
{
    [CreateAssetMenu(fileName = "BuffDataSo")]
    public class BuffDataSo : ScriptableObject
    {
        public int Id;
        public string Name;
        public float Duration;
        //tick 间隔
        public float TickInterval;
        //是否是永久buff
        public bool IsPersistent;
    }
}