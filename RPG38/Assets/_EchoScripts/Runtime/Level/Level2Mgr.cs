using System;
using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Runtime.Level
{
    public class Level2Mgr : EchoLevelMgr
    {
        public List<Transform> spawnPoints;
        public GameObject simpleEnemyPrefab;

        private int dieCount;

        public override void OnStartServer()
        {
            base.OnStartServer();
            foreach (var point in spawnPoints)
            {
                var obj = Object.Instantiate(simpleEnemyPrefab);
                obj.transform.position = point.position;
                NetworkServer.Spawn(obj);
            }
            
            Enemy.OnEnemyDeath += EnemyOnOnEnemyDeath;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            
            Enemy.OnEnemyDeath -= EnemyOnOnEnemyDeath;
        }
        
        private void EnemyOnOnEnemyDeath(Enemy obj)
        {
            dieCount++;
            if (dieCount == spawnPoints.Count)
            {
                JKLog.Log("win");
                foreach (var conn in NetworkServer.connections.Values)
                {
                    conn.identity.GetComponent<EchoNetPlayerCtrl>().canControl = false;
                }
                StringEventSystem.Global.Send(EventKey.Passed);
                RpcWin();
            }
        }

        [ClientRpc]
        private void RpcWin()
        {
            UISystem.Show<WinUI>();
        }
    }
}