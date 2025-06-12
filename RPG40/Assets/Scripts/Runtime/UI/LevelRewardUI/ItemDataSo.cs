using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Unity
    /// </summary>

    [CreateAssetMenu(fileName = "ItemDataSo")]
    public class ItemDataSo : ScriptableObject
    {
        public int itemID;
        public string itemName;
        public Sprite icon;
        public string itemDesc;
        public int buffId;
        public ItemType itemType;
    }
}