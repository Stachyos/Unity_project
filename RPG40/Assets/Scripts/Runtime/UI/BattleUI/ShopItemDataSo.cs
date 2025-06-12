using System;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Unity
    /// </summary>

  
    [CreateAssetMenu(fileName = "ShopItemDataSo")]
    public class ShopItemDataSo : ScriptableObject
    {
        public int shopItemId;
        public string itemName;
        public int goldCost;
        public int buffId;
        public int demand = 0;
        public int cost;

        private void OnValidate()
        {
            if (this.name != $"ShopItemDataSo_{shopItemId}")
            {
                Debug.LogError("ShopItemDataSo name is wrong"+this.name);
            }
        }
    }
}