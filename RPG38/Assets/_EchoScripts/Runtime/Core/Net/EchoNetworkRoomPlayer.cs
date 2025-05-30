using System.Collections;
using System.Collections.Generic;
using GameLogic;
using GameLogic.Runtime;
using JKFrame;
using UnityEngine;
using Mirror;
using Mirror.Examples.PickupsDropsChilds;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-player
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomPlayer.html
*/

/// <summary>
/// This component works in conjunction with the NetworkRoomManager to make up the multiplayer room system.
/// The RoomPrefab object of the NetworkRoomManager must have this component on it.
/// This component holds basic room player data required for the room to function.
/// Game specific data for room players can be put in other components on the RoomPrefab or in scripts derived from NetworkRoomPlayer.
/// </summary>
public class EchoNetworkRoomPlayer : NetworkRoomPlayer
{
	public override void OnStartClient()
	{
		base.OnStartClient();

		if (isOwned)
		{
			CmdSubmitUserInfo(GameHub.Interface.GetModel<UserModel>().userName);
		}
	}

	public override void OnClientEnterRoom()
	{
		base.OnClientEnterRoom();
	}

	[Command]
	private void CmdSubmitUserInfo(string nickName)
	{
		NetManager.Instance.ClientInfos[connectionToClient.connectionId] = nickName;
		TargetRpcSubmitUserInfoResp(connectionToClient,connectionToClient.connectionId);

		var room = NetManager.Instance.networkManager as EchoNetworkRoomManager;
		room.RefreshRoomUINotify();
	}

	[TargetRpc]
	private void TargetRpcSubmitUserInfoResp(NetworkConnection connection,long clientId)
	{
		NetManager.Instance.LocalClientId = clientId;
		NetManager.Instance.identity = this.GetComponent<NetworkIdentity>();
		NetManager.Instance.net = this;
		
		var window = UISystem.GetWindow<RoomUI>();
		if (window!=null)
		{
			window.SelectHeroClicked(1);
		}
	}

	[ClientRpc]
	public void RpcRefreshRoomUI(List<ReadyInfo> readyInfos)
	{ 
		StringEventSystem.Global.Send(EventKey.RefreshRoomUI,readyInfos);
	}

	[Command]
	public void CmdStartGame()
	{
		RpcStartGame();
	}

	[ClientRpc]
	public void RpcStartGame()
	{
		Debug.Log("RpcStartGame");
		UISystem.CloseAllWindow();
		NetManager.Instance.networkDiscovery.StopDiscovery();
	}

	[SyncVar] public int heroId = 1;

	[Command]
	public void CmdChangeHeroId(int newHeroId)
	{
		this.heroId = newHeroId;
	}


}
