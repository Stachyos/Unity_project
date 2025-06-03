using System.Linq;
using GameLogic;
using GameLogic.Runtime;
using JKFrame;
using UnityEngine;
using Mirror;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomManager.html

	See Also: NetworkManager
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

/// <summary>
/// This is a specialized NetworkManager that includes a networked room.
/// The room has slots that track the joined players, and a maximum player count that is enforced.
/// It requires that the NetworkRoomPlayer component be on the room player objects.
/// NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class.
/// </summary>
public class EchoNetworkRoomManager : NetworkRoomManager
{
	public GameObject player1;
	public GameObject player2;

	public override void OnStartServer()
	{
		base.OnStartServer();

		if (player2 == null)
		{
			Debug.LogError($"[{nameof(EchoNetworkRoomManager)}] 无法通过 Resources.Load 找到 {player2}");
		}
		else
		{
			// 如果 spawnPrefabs 里还没有同名的 Prefab，就加入进去
			if (!spawnPrefabs.Contains(player2))
			{
				spawnPrefabs.Add(player2);
				Debug.Log(
					$"[{nameof(EchoNetworkRoomManager)}] 已经把 player2.prefab 加入到 spawnPrefabs 列表 (assetId={player2.GetComponent<NetworkIdentity>().assetId})");
			}
			else
			{
				Debug.Log($"[{nameof(EchoNetworkRoomManager)}] spawnPrefabs 中已经包含了 player2.prefab，跳过重复添加");
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
				//将血量 蓝量回满
				netPlayer.AddHealth(20);
				netPlayer.AddMp(20);
				var startPosTf = GetStartPosition();
				netPlayer.transform.position = startPosTf.position;
				netPlayer.TargetRpcInjectShopItemData(conn);
				netPlayer.fsm.ChangeState(NetPlayerState.Idle);
				netPlayer.aniCtrl.SetBool("Dead",false);
				
				
				// 2. 把根节点也切到 Ignore Raycast（以防万一）——不过真正命中的是 Collider2D，
				//    但建议先改根节点，逻辑更一致
				netPlayer.gameObject.layer = LayerMask.NameToLayer("Player");

				// 3. 找出所有子物体里挂着 2D 碰撞器（Collider2D）的对象，一并把它们的 Layer 设为 Ignore Raycast
				//    includeInactive: true 可以处理被暂时设为 inactive 的 Collider2D
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
		
		//获取一次最新位置，因为用的是udp，是不可靠的
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
		base.ReadyStatusChanged();
		RefreshRoomUINotify();
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
