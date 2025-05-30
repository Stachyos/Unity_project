using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime.Level
{
    //api只允许服务器上调用
    public class EchoLevelMgr : NetBehaviour
    {
        public string nextSceneName;
        public List<ShopItemDataSo> shopItems;
        public List<RewardDataSo> rewards;
        
        private int playerDieCount;

        public override void OnStartServer()
        {
            base.OnStartServer();
            EchoNetPlayerCtrl.OnDeath += EchoNetPlayerCtrlOnOnDeath;
            NetworkServer.RegisterHandler<LevelReadyMessage>(OnLevelReadyMessage);
        }

        private int readyCount = 0;
        protected virtual void OnLevelReadyMessage(NetworkConnectionToClient conn, LevelReadyMessage message)
        {
            readyCount++;
            if (readyCount == NetworkServer.connections.Count)
            {
                OnAllLevelReady();
            }
        }

        protected virtual void OnAllLevelReady()
        {
            RpcCloseRewardUI();
            GoNextLevel();
        }

        public void InjectShopItemData()
        {
            if (shopItems.Count >= 2)
            {
                var battleUI = UISystem.GetWindow<BattleUI>();
                if (battleUI != null)
                {
                    var r1 = Random.Range(0, shopItems.Count);
                    var data1 = shopItems[r1];
                    shopItems.RemoveAt(r1);
                    var r2 = Random.Range(0, shopItems.Count);
                    var data2 = shopItems[r2];
                    battleUI.InjectShopItemData(data1, data2);
                }
            }
        }
        


        private void EchoNetPlayerCtrlOnOnDeath(IChaAttr obj)
        {
            playerDieCount++;
            //所有连接的玩家死亡
            if (playerDieCount == NetworkServer.connections.Count)
            {
                JKLog.Log("all player is die");
                RpcFail();
            }
        }

        [ClientRpc]
        private void RpcCloseRewardUI()
        {
            UISystem.Close<LevelRewardUI>();
        }

        [ClientRpc]
        private void RpcFail()
        {
            UISystem.Show<FailUI>();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            EchoNetPlayerCtrl.OnDeath -= EchoNetPlayerCtrlOnOnDeath;
            NetworkServer.UnregisterHandler<LevelReadyMessage>();
        }

        //进入下一关前要做的事情，比如召唤传送阵等
        public virtual void PreNextLevel()
        {
            
        }
        
        //实际调用的转换场景的逻辑
        public virtual void GoNextLevel()
        {
            //在场景切换前，禁用所有玩家的操作控制权限
            foreach (var connectionToClient in NetworkServer.connections.Values)
            {
                connectionToClient.identity.GetComponent<EchoNetPlayerCtrl>().canControl = false;
            }
            NetworkManager.singleton.ServerChangeScene(nextSceneName);
        }

        public virtual void AfterNextLevel()
        {
            
        }
        
        [ClientRpc]
        public void RpcShowLevelRewardUI()
        {
            var window = UISystem.Show<LevelRewardUI>();
            window.InitRewardItem(rewards[0],rewards[1],rewards[2]);
        }
    }

    public struct LevelReadyMessage : NetworkMessage
    {
        
    }
}