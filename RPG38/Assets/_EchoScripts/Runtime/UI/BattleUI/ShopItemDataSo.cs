using System;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Runtime
{
    [CreateAssetMenu(fileName = "ShopItemDataSo")]
    public class ShopItemDataSo : ScriptableObject
    {
        public int shopItemId;
        public string itemName;
        public int goldCost;
        public int buffId;

        private void OnValidate()
        {
            if (this.name != $"ShopItemDataSo_{shopItemId}")
            {
                Debug.LogError("ShopItemDataSo name is wrong");
            }
        }
    }
}