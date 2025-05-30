using UnityEngine;
using UnityEngine.Events;

public class LevelManager : MonoBehaviour
{
    [Header("玩家生成配置")]
    [Tooltip("Player 预制体，必须包含 Player 组件")]
    [SerializeField] private GameObject playerPrefab;
    [Tooltip("玩家出生点")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("敌人生成配置")]
    [Tooltip("Enemy 预制体，必须包含 Enemy + Health 组件")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("敌人出生点列表")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("本关要生成的敌人总数")]
    [SerializeField] private int totalEnemyCount = 5;

    [Header("关卡事件")]
    [Tooltip("所有敌人被击败后触发——胜利")]
    public UnityEvent onVictory;
    [Tooltip("玩家死亡时触发——失败")]
    public UnityEvent onDefeat;

    private int aliveEnemies = 0;
    private GameObject playerInstance;
    

    private void OnEnable()
    {
        Enemy.OnEnemyDeathGlobal += HandleEnemyDeath;
        PlayerCtr.OnPlayerDeathGlobal += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDeathGlobal -= HandleEnemyDeath;
        PlayerCtr.OnPlayerDeathGlobal -= HandlePlayerDeath;
    }

    private void Start()
    {
        SpawnPlayer();
        SpawnAllEnemies();
    }

    private void SpawnPlayer()
    {
        // if (playerPrefab == null || playerSpawnPoint == null)
        // {
        //     Debug.LogWarning("Player prefab 或 spawn point 未设置！");
        //     return;
        // }
        // playerInstance = Instantiate(playerPrefab,
        //                              playerSpawnPoint.position,
        //                              playerSpawnPoint.rotation);
    }

    private void SpawnAllEnemies()
    {
        aliveEnemies = 0;
        if (enemyPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Enemy prefab 或 spawn points 未设置！");
            return;
        }

        for (int i = 0; i < totalEnemyCount; i++)
        {
            Transform sp = spawnPoints[i % spawnPoints.Length];
            // 生成
            GameObject go = Instantiate(enemyPrefab, sp.position, sp.rotation);
            aliveEnemies++;

            // **关键：把玩家 Transform 赋给 Enemy**
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null && playerInstance != null)
            {
                enemy.SetTarget(playerInstance.transform);
            }
        }
    }

    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        aliveEnemies--;
        Debug.Log($"敌人被击败，剩余：{aliveEnemies}");
        if (aliveEnemies <= 0)
            Victory();
    }

    private void HandlePlayerDeath(PlayerCtr deadPlayerCtr)
    {
        Debug.Log("玩家死亡，失败！");
        onDefeat?.Invoke();
    }

    private void Victory()
    {
        Debug.Log("所有敌人被击败！胜利！");
        onVictory?.Invoke();
    }
}
