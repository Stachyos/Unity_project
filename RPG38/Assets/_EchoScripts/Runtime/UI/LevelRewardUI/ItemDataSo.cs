using UnityEngine;

namespace GameLogic.Runtime
{
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