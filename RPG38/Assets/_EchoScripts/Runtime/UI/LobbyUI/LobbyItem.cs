using System;
using JKFrame;
using TMPro;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    public class LobbyItem : CellView
    {
        public TMP_Text addressText;
        public Button joinBtn;

        private void Start()
        {
            joinBtn.onClick.AddListener(() =>
            {
                var window = UISystem.GetWindow<LobbyUI>();
                if (window != null)
                {
                    LobbyItemData lobbyItemData = this.cellData as LobbyItemData;
                    window.Connect(lobbyItemData.response);
                    UISystem.Close<LobbyUI>();
                    UISystem.Show<RoomUI>();
                    
                    NetManager.Instance.networkDiscovery.StopDiscovery();
                }
            });
        }

        public override void SetData(CellData data)
        {
            base.SetData(data);
            LobbyItemData lobbyItemData = data as LobbyItemData;
            addressText.text = lobbyItemData.response.serverName;
        }
    }
}