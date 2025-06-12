using System;
using JKFrame;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
 
    public class ShopItem : MonoBehaviour
    {
        public TMP_Text shopNameText;
        public TMP_Text costText;
        public Button buyBtn;
        private ShopItemDataSo _dataSo;

        private void Start()
        {
            buyBtn.onClick.AddListener(() =>
            {
                if (_dataSo == null)
                    return;
                
                var netPlayer = NetworkClient.localPlayer?.GetComponent<EchoNetPlayerCtrl>();
                if (netPlayer == null || !netPlayer.isLocalPlayer)
                    return;

                if (netPlayer.goldNumber < _dataSo.goldCost)
                    return;

                netPlayer.CmdBuyShopItem(_dataSo.shopItemId, _dataSo.goldCost);
                _dataSo.demand++;
                buyBtn.interactable = false;

                var battleUI = UISystem.GetWindow<BattleUI>();
                if (battleUI != null)
                {
                    battleUI.CloseShopView();
                }
            });
        }

        public void Init(ShopItemDataSo dataSo)
        {
            buyBtn.interactable = true;
            shopNameText.text = dataSo.itemName;
            costText.text  = dataSo.goldCost.ToString();
            this._dataSo = dataSo;
        }
    }
}