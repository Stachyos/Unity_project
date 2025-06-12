using System;
using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Runtime.Level
{


    // The API is only accessible for server-side calls.
    public class EchoLevelMgr : NetBehaviour
    {
        public string nextSceneName;
        public List<ShopItemDataSo> shopItems;
        public List<RewardDataSo> rewards;
        public float InflationRate =0.2f;
        public int E = 3;
        
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
                    var id1 = shopItems[r1].shopItemId;

                var data1 = shopItems[r1];
                    
                    
                    shopItems.RemoveAt(r1);
                    var r2 = Random.Range(0, shopItems.Count);
                    var data2 = shopItems[r2];
                    
                    //var dataSo1 = ResSystem.LoadAsset<ShopItemDataSo>($"Assets/_EchoAddressable/DataSo/ShopItemDataSo_{id1}.asset");
                    data1.goldCost = (int)(data1.cost * Math.Pow((1 + InflationRate), GameGlobalData.levelNumber) *
                                           Math.Pow((10 + data1.demand) / 10, E));
                    data2.goldCost = (int)(data2.cost * Math.Pow((1 + InflationRate), GameGlobalData.levelNumber) *
                                           Math.Pow((10 + data2.demand) / 10, E));

                    battleUI.InjectShopItemData(data1, data2);
                }
            }
        }
        


        private void EchoNetPlayerCtrlOnOnDeath(IChaAttr obj)
        {
            playerDieCount++;
            // All connected players have died
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

        //Things to do before moving on to the next level, such as summoning the teleportation array, etc.
        public virtual void PreNextLevel()
        {
            
        }
        
        // The logic of the actual conversion scenario being invoked
        public virtual void GoNextLevel()
        {
            // Before switching scenes, disable all players' operation control permissions
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

            // Ensure that the "rewards" list contains at least 3 items
            if (rewards.Count < 3)
            {
                return;
            }

            // Randomly select three items, without repetition.
            int r1 = Random.Range(0, rewards.Count);
            var item1 = rewards[r1];
            rewards.RemoveAt(r1);

            int r2 = Random.Range(0, rewards.Count);
            var item2 = rewards[r2];
            rewards.RemoveAt(r2);

            int r3 = Random.Range(0, rewards.Count);
            var item3 = rewards[r3];
            rewards.RemoveAt(r3);

            // Pass to the UI
            window.InitRewardItem(item1, item2, item3);
            
        }
    }

    public struct LevelReadyMessage : NetworkMessage
    {
        
    }
}