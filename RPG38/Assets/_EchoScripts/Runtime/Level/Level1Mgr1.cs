using System;
using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Runtime.Level
{
    public class Level1Mgr1 : EchoLevelMgr
    {
        public List<Transform> spawnPoints;
        public GameObject simpleEnemyPrefab;
        public GameObject teleportationPrefab;
        public Transform teleportationPoint;

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