using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;  

[RequireComponent(typeof(NetworkDiscovery))]
public class CustomNetworkManager : NetworkManager
{
    [Header("自定义刷点")]
    public List<Transform> spawnPoints;      // 在 Inspector 填好
    
    [Header("Player & Scenes")]
    public GameObject playerPrefabSpawnUsed;        // 指向你的玩家预制体
    public string     gameSceneName = "GameScene";  
    
    [Header("UI 引用")]
    public Button   hostButton;
    public Button   joinButton;
    public Button   startButton;
    public TMP_Text statusText;
    public Transform serverListContent;
    public GameObject serverListItemPrefab;

    [Header("Discovery 设置")]
    [Tooltip("搜索持续时间，秒")]
    public float discoveryDuration = 5f;

    NetworkDiscovery           discovery;
    readonly List<ServerListItem> serverItems = new List<ServerListItem>();
    Coroutine discoveryCoroutine;
    private int totalPlayers = 0;


    public new void Awake()
    {
        base.Awake();                // 先让 NetworkManager 本身跑它的 Awake
        DontDestroyOnLoad(gameObject);
    }

    public override void Start()
    {
        base.Start();
        discovery = GetComponent<NetworkDiscovery>();
        discovery.OnServerFound += HandleServerFound;
        
        
        // // 调试: 预先添加几个占位空项，便于检查布局
        // for (int i = 1; i <= 3; i++)
        // {
        //     var go = Instantiate(serverListItemPrefab, serverListContent);
        //     var item = go.GetComponent<ServerListItem>();
        //     item.Setup($"Placeholder {i}", () => Debug.Log($"Clicked placeholder {i}"));
        // }
        //
        // Debug.Log($">>> After placeholders: childCount = {serverListContent.childCount}");
        // for (int i = 0; i < serverListContent.childCount; i++)
        // {
        //     var child = serverListContent.GetChild(i);
        //     Debug.Log($">>>  Child {i}: name={child.name}, activeInHierarchy={child.gameObject.activeInHierarchy}");
        // }


        hostButton.onClick.AddListener(HostServer);
        joinButton.onClick.AddListener(SearchServers);
        startButton.onClick.AddListener(StartGame);

        statusText.text = "Ready";
        startButton.gameObject.SetActive(false);
    }

    #region UI 回调
    void HostServer()
    {
        Debug.Log(">>> HostServer()");
        discovery.AdvertiseServer();
        StartHost();
        statusText.text = "Hosting… ";
    }
    
    // 当 Host 完全启动后，这个方法会被调用
    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log(">>> OnStartHost");
        // 这里把自己算作第一个玩家
        statusText.text = $"玩家：1/2";
    }

    void SearchServers()
    {
        Debug.Log(">>> SearchServers()");
        ClearServerList();
        // 开始广播/监听
        discovery.StartDiscovery();
        // 如果上一次还在跑，先停掉
        if (discoveryCoroutine != null) StopCoroutine(discoveryCoroutine);
        discoveryCoroutine = StartCoroutine(DiscoveryProgress());
    }

    void StartGame()
    {
        Debug.Log(">>> StartGame()");
        if (totalPlayers >= 2)
            ServerChangeScene(gameSceneName);
    }
    
        // 删除原来的手动 Instantiate 逻辑，改为调用 Mirror 的 AddPlayerForConnection 钩子
        public override void OnServerSceneChanged(string sceneName)
            {
                base.OnServerSceneChanged(sceneName);
                if (sceneName == gameSceneName)
                   {
                        Debug.Log(">>> OnServerSceneChanged: spawning players via OnServerAddPlayer");
                        // 让 OnServerAddPlayer 为每条连接正确 spawn
                            foreach (var conn in NetworkServer.connections.Values)
                            {
                                OnServerAddPlayer(conn);
                            }
                    }
            }

        // 重写这个方法，带位置生成并绑定 Player
            public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
            // 选一个起始点，如果你在场景里放了 StartPositions（NetworkManager 的 Start Positions 列表）
                Transform start = GetStartPosition();
            Vector3 pos = start != null ? start.position : Vector3.zero;
            Quaternion rot = start != null ? start.rotation : Quaternion.identity;
    
                // 在服务器上 Instantiate 带位置的 Player
                    var player = Instantiate(playerPrefabSpawnUsed, pos, rot);
            // 绑定给这个连接（同时在所有客户端上 Spawn 并赋予控制权）
               NetworkServer.AddPlayerForConnection(conn, player);
       }
    
    // —— 客户端切换场景后，在本地建立 Player 控制权 —— 
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        // Mirror 会自动把 server 端给的 player prefab spawning info 同步下来
        // 这里可以做一些客户端初始化之类的事
        Debug.Log(">>> OnClientSceneChanged: player should be spawned locally");
    }
    #endregion

    #region Discovery 进度 & 超时提示
    IEnumerator DiscoveryProgress()
    {
        float elapsed = 0f;
        // 每 0.5s 更新一次 UI
        while (elapsed < discoveryDuration)
        {
            statusText.text = $"searching... {elapsed:F1}s";
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        // 超时后停止搜索
        discovery.StopDiscovery();
        if (serverItems.Count > 0)
            statusText.text = $"found {serverItems.Count} servers";
        else
            statusText.text = "timeout, no servers found";

        discoveryCoroutine = null;
        Debug.Log(">>> Discovery timeout, stopped.");
    }
    #endregion

    #region 发现回调
    void HandleServerFound(DiscoveryResponse info, IPEndPoint endpoint)
    {
        // 1. 拿到真实的 IP 字符串
        string ip = endpoint.Address.ToString();

        // 2. 去重（用 ip 而不是 info.uri.Host）
        if (serverItems.Exists(x => x.Address == ip)) return;

        // 3. 实例化列表条目，用 ip 来显示和连入
        var go   = Instantiate(serverListItemPrefab, serverListContent);
        var item = go.GetComponent<ServerListItem>();
        item.Setup(ip, () =>
        {
            networkAddress = ip;
            Debug.Log($">>> StartClient to {ip}");
            StartClient();
        });
        serverItems.Add(item);

        statusText.text = $"found {serverItems.Count}: {ip}";
    }
    #endregion

    void ClearServerList()
    {
        foreach (var it in serverItems) Destroy(it.gameObject);
        serverItems.Clear();
    }

    #region 覆写连接回调，只更新 UI，不切场景
    
    // 客户端真正启动时
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log(">>> OnStartClient: 客户端已启动");
    }
    public override void OnClientConnect()
    {
        Debug.Log(">>> OnClientConnect()");
        statusText.text = $"connected，current player：{totalPlayers}/2";
    }

    public override void OnClientDisconnect()
    {
        Debug.Log(">>> OnClientDisconnect()");
        statusText.text = "disconnected";
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
                // 客户端 Count +1
                    int remote = NetworkServer.connections.Count;
                totalPlayers = remote;
                Debug.Log($">>> OnServerConnect: connections={remote}, totalPlayers={totalPlayers}");
                statusText.text = $"玩家：{totalPlayers}/2";
                startButton.gameObject.SetActive(totalPlayers >= 2);
    }

    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
                int remote = NetworkServer.connections.Count;
                totalPlayers = remote;
                Debug.Log($">>> OnServerDisconnect: connections={remote}, totalPlayers={totalPlayers}");
                statusText.text = $"玩家：{totalPlayers}/2";
                startButton.gameObject.SetActive(totalPlayers >= 2);
    }
    #endregion
}
