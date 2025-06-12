using System;
using Mirror;
using TMPro;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in EnhancedScroller v2, I override part of its function.
    /// </summary>
    public class RoomItem : CellView
    {
        public Button ready, cancel;
        public TMP_Text readyText, notReadyText;
        public TMP_Text address;

        private void Start()
        {
            ready.onClick.AddListener(() =>
            {
                RoomItemData roomItemData = this.cellData as RoomItemData;
                NetManager.Instance.net.CmdChangeReadyState(true);
            });
            cancel.onClick.AddListener(() =>
            {
                RoomItemData roomItemData = this.cellData as RoomItemData;
                NetManager.Instance.net.CmdChangeReadyState(false);
            });
        }

        public override void SetData(CellData data)
        {
            base.SetData(data);
            RoomItemData roomItemData = data as RoomItemData;
            
            address.text = roomItemData.readyInfo.nickName;
            if (NetworkClient.active && roomItemData.readyInfo.clientId == NetManager.Instance.LocalClientId)
            {
                readyText.gameObject.SetActive(false);
                notReadyText.gameObject.SetActive(false);
                if (roomItemData.readyInfo.ready)
                {
                    ready.gameObject.SetActive(false);
                    cancel.gameObject.SetActive(true);
                }
                else
                {
                    ready.gameObject.SetActive(true);
                    cancel.gameObject.SetActive(false);
                }
            }
            else
            {
                ready.gameObject.SetActive(false);
                cancel.gameObject.SetActive(false);

                if (roomItemData.readyInfo.ready)
                {
                    readyText.gameObject.SetActive(true);
                    notReadyText.gameObject.SetActive(false);
                }
                else
                {
                    readyText.gameObject.SetActive(false);
                    notReadyText.gameObject.SetActive(true);
                }
            }
        }
    }
}