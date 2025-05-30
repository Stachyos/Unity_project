using JKFrame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    [UIWindowData(typeof(BattleUI),false,"Assets/_EchoAddressable/UI/BattleUI.prefab",0)]
    public class BattleUI : UI_WindowBase
    {
        public TMP_Text nameText;
        public TMP_Text goldText;
        public Image hpBar;
        public Image  mpBar;
        public Button shopBtn;
        public Button shopCloseBtn;
        public Transform shopView;
        public ShopItem shopItem1;
        public ShopItem shopItem2;

        public override void Init()
        {
            base.Init();
            shopBtn.onClick.AddListener(() =>
            {
                shopView.gameObject.SetActive(true);
            });
            shopCloseBtn.onClick.AddListener(() =>
            {
                shopView.gameObject.SetActive(false);
            });
        }

        public void RefreshUI(float currentHealth,float maxHealth,float currentMp,float maxMp,int goldNumber)
        {
            hpBar.fillAmount = currentHealth / maxHealth;
            mpBar.fillAmount = currentMp / maxMp;
            goldText.text = "money:"+goldNumber.ToString();
            nameText.text = GameHub.Interface.GetModel<UserModel>().userName;
        }

        public void CloseShopView()
        {
            shopView.gameObject.SetActive(false);
        }

        public void InjectShopItemData(ShopItemDataSo data1,ShopItemDataSo data2)
        {
            shopItem1.Init(data1);
            shopItem2.Init(data2);
        }
    }
}