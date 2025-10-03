using System.Linq;
using GameLogic;
using GameLogic.Runtime;
using JKFrame;
using UnityEngine;
using Mirror;

/// <summary>
/// This class inherits a class in Mirror, I override part of its function.
/// </summary>

public class EchoNetworkRoomManager : NetworkRoomManager
{
	[Header("Room Start Options")]
	[Tooltip("If true, the room can start the game when at least one player is ready. If false, uses minPlayers logic.")]
	public bool allowSinglePlayerStart = true;
	public GameObject player1;
	public GameObject player2;

	public override void OnStartServer()
	{
		base.OnStartServer();

		if (player2 == null)
		{
			
		}
		else
		{
			// If there is no Prefab with the same name in the spawnPrefabs, add it in.
			if (!spawnPrefabs.Contains(player2))
			{
				spawnPrefabs.Add(player2);
			}

		}
	}

	public override void OnRoomServerPlayersReady()
	{
		JKLog.Log("all players are ready");
	}

	private void OnServerAllReady()
	{
		if (Utils.IsSceneActive("GamePlay1")||Utils.IsSceneActive("GamePlay2")||Utils.IsSceneActive("GamePlay3")||Utils.IsSceneActive("GamePlay4")
		    ||Utils.IsSceneActive("GamePlay5")||Utils.IsSceneActive("GamePlay6")||Utils.IsSceneActive("GamePlay7"))
		{
			foreach (var conn in NetworkServer.connections.Values)
			{
				var netPlayer = conn.identity.GetComponent<EchoNetPlayerCtrl>();
				netPlayer.canControl = true;
				//Restore health and mp to full.
				netPlayer.AddHealth(20);
				netPlayer.AddMp(20);
				if (NetworkServer.connections.Values.Count == 1)
				{
					netPlayer.AddMaxHealth(20);
					netPlayer.AddHealth(20);
					netPlayer.AddMaxMp(20);
					netPlayer.AddMp(20);
					
				}

				var startPosTf = GetStartPosition();
				netPlayer.transform.position = startPosTf.position;
				netPlayer.TargetRpcInjectShopItemData(conn);
				netPlayer.fsm.ChangeState(NetPlayerState.Idle);
				netPlayer.aniCtrl.SetBool("Dead",false);
				netPlayer._hasDied = false;
				
				

				netPlayer.gameObject.layer = LayerMask.NameToLayer("Player");


				Collider2D[] all2DColliders = netPlayer.GetComponentsInChildren<Collider2D>(true);
				foreach (Collider2D col2D in all2DColliders)
				{
					col2D.gameObject.layer = LayerMask.NameToLayer("Player");
				}
				
				
				conn.identity.GetComponent<NetworkTransformUnreliable>().RpcTeleport(startPosTf.position);
			}
		}
	}
	public override void OnServerReady(NetworkConnectionToClient conn)
	{
		base.OnServerReady(conn);

		bool isAllReady = true;
		foreach (var con in NetworkServer.connections.Values)
		{
			if(con.isReady == false)
			{
				isAllReady = false;
				break;
			}
		}

		if (isAllReady)
		{
			OnServerAllReady();
		}

	}
	

	public override void OnClientSceneChanged()
	{
		base.OnClientSceneChanged();
		

	}

	public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
	{
		if (roomPlayer.GetComponent<EchoNetworkRoomPlayer>()==null)
		{
			return null;
		}

		GameObject result = null;
		var startPos = GetStartPosition();
		var netPlayer = roomPlayer.GetComponent<EchoNetworkRoomPlayer>();
		if (netPlayer.heroId == 1)
		{
			result = Instantiate(player1,startPos.position, Quaternion.identity);
		}
		else if(netPlayer.heroId == 2)
		{
			result = Instantiate(player2,startPos.position, Quaternion.identity);
		}
		return result;
	}
	
	

	public override Transform GetStartPosition()
	{
		return base.GetStartPosition();
	}

	public override void ReadyStatusChanged()
	{
		// 当允许单人启动时：只要有至少一个 ready 的玩家就开始（触发 OnRoomServerPlayersReady）
		if (allowSinglePlayerStart)
		{
			int CurrentPlayers = 0;
			int ReadyPlayers = 0;

			foreach (NetworkRoomPlayer item in roomSlots)
			{
				if (item != null)
				{
					CurrentPlayers++;
					if (item.readyToBegin)
						ReadyPlayers++;
				}
			}

			// 如果至少 1 人 ready，就认为 allPlayersReady，并触发后续逻辑
			if (ReadyPlayers >= 1 && CurrentPlayers >= 1)
			{
				// 清理 pending（保持与原有行为一致）
				pendingPlayers.Clear();

				// 通过设置 allPlayersReady 触发 OnRoomServerPlayersReady() 回调
				allPlayersReady = true;
			}
			else
			{
				allPlayersReady = false;
			}

			// 刷新 UI（保留你原来的 UI 刷新调用）
			RefreshRoomUINotify();
		}
		else
		{
			// 保留原有默认逻辑（基于 minPlayers）
			base.ReadyStatusChanged();
			RefreshRoomUINotify();
		}
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		UISystem.CloseAllWindow();
		GameHub.Interface.Deinit();
	}

	public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
	{
		base.OnRoomServerDisconnect(conn);
		
		if (NetworkClient.activeHost)
		{
			RefreshRoomUINotify();
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();

	}

	public void RefreshRoomUINotify()
	{
		var readyInfos = roomSlots.Select((item) => new ReadyInfo()
		{
			clientId = item.connectionToClient.connectionId,
			nickName = NetManager.Instance.ClientInfos[item.connectionToClient.connectionId],
			ready = item.readyToBegin
		}).ToList();
		foreach (var net in roomSlots)
		{
			var echoNet = net as EchoNetworkRoomPlayer;
			echoNet.RpcRefreshRoomUI(readyInfos);
		}
	}
}

public partial class ReadyInfo
{
	public long clientId;
	public string nickName;
	public bool ready;
}
