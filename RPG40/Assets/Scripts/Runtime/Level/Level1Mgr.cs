using System;
using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Runtime.Level
{
    
    public class Level1Mgr : EchoLevelMgr
    {
        public List<Transform> spawnPoints;
        public GameObject simpleEnemyPrefab;
        public GameObject teleportationPrefab;
        public Transform teleportationPoint;

        private int dieCount;
        private int targetDieCount;

        public override void OnStartServer()
        {
            base.OnStartServer();
            dieCount = 0;
            foreach (var point in spawnPoints)
            {
                var obj = Object.Instantiate(simpleEnemyPrefab);
                obj.transform.position = point.position;
                NetworkServer.Spawn(obj);
            }
            
            Enemy.OnEnemyDeath += EnemyOnOnEnemyDeath;
            targetDieCount = spawnPoints.Count;
            var enemy2Comp = simpleEnemyPrefab.GetComponent<Enemy2>();
            if (enemy2Comp != null)
            {
                targetDieCount *= 3;
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            
            Enemy.OnEnemyDeath -= EnemyOnOnEnemyDeath;
        }
        
        private void EnemyOnOnEnemyDeath(Enemy obj)
        {
            dieCount++;
            if (dieCount>= targetDieCount)
            {
                PreNextLevel();
            }
        }

        public override void PreNextLevel()
        {
            base.PreNextLevel();
            var obj = Object.Instantiate(teleportationPrefab);
            obj.transform.position = teleportationPoint.position;
            NetworkServer.Spawn(obj);
        }

        protected override void OnAllLevelReady()
        {
            base.OnAllLevelReady();
            
            // this.GoNextLevel();
        }
    }
}