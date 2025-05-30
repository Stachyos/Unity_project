using System;
using System.Collections.Generic;
using System.Linq;
using JKFrame;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    [UIWindowData(typeof(RoomUI),false,"Assets/_EchoAddressable/UI/RoomUI.prefab",0)]
    public class RoomUI : UI_WindowBase
    {
        public Button startGame;
        public Button closeBtn;
        public RoomScrollerDelegate scrollerDelegate;
        
        public Button playerSelectBtn;
        public Button playerSelectBtn2;
        
        public override void Init()
        {
            base.Init();
            
            closeBtn.onClick.AddListener(() =>
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                    NetworkManager.singleton.StopHost();
                else if (NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();
                
                UISystem.Close<RoomUI>();
                UISystem.Show<LobbyUI>();
            });
            
            startGame.onClick.AddListener(() =>
            {
                startGame.interactable = false;
                
                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    NetManager.Instance.net.RpcStartGame();
                }
                
                NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;
                room.ServerChangeScene(room.GameplayScene);
            });

            StringEventSystem.Global.Register<List<ReadyInfo>>(EventKey.RefreshRoomUI, OnReadyStatusChanged);
            
            playerSelectBtn.onClick.AddListener(()=>SelectHeroClicked(1));
            playerSelectBtn2.onClick.AddListener(()=>SelectHeroClicked(2));
        }

        public override void OnClose()
        {
            base.OnClose();
            StringEventSystem.Global.UnRegister<List<ReadyInfo>>(EventKey.RefreshRoomUI, OnReadyStatusChanged);
        }

        private void OnReadyStatusChanged(List<ReadyInfo> readyInfos)
        {
            scrollerDelegate.ClearAllCells();
            var cellDatas = readyInfos.Select((item) => new RoomItemData()
            {
                readyInfo = item
            });
            scrollerDelegate.AddCellRange(cellDatas);
            scrollerDelegate.ReloadData();
        }

        public void SelectHeroClicked(int id)
        {
            playerSelectBtn.transform.parent.GetComponent<Image>().color = Color.white;
            playerSelectBtn2.transform.parent.GetComponent<Image>().color = Color.white;
            if (id == 1)
            {
                playerSelectBtn.transform.parent.GetComponent<Image>().color = Color.green;
            }
            else if(id==2)
            {
                playerSelectBtn2.transform.parent.GetComponent<Image>().color = Color.red;
            }
            NetManager.Instance.net.CmdChangeHeroId(id);
        }

        private void Update()
        {
            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;
            if (room)
            {
                if (!Utils.IsSceneActive(room.RoomScene))
                    return;
            }

            if (room && room.allPlayersReady)
            {
                startGame.gameObject.SetActive(true);
            }
            else
            {
                startGame.gameObject.SetActive(false);
            }
        }
    }
}