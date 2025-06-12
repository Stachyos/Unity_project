using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Unity
    /// </summary>


    [CreateAssetMenu(fileName = "BuffDataSo")]
    public class BuffDataSo : ScriptableObject
    {
        public int Id;
        public string Name;
        public float Duration;
        //tick
        public float TickInterval;
        //buff if permanent
        public bool IsPersistent;
    }
}