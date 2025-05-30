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
				netPlayer.AddHealth(1000);
				netPlayer.AddMp(1000);
				var startPosTf = GetStartPosition();
				netPlayer.transform.position = startPosTf.position;
				netPlayer.TargetRpcInjectShopItemData(conn);
				netPlayer.fsm.ChangeState(NetPlayerState.Idle);
				netPlayer.aniCtrl.SetBool("Dead",false);
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
