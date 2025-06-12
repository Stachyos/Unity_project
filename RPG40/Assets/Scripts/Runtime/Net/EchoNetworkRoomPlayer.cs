using System.Collections;
using System.Collections.Generic;
using GameLogic;
using GameLogic.Runtime;
using JKFrame;
using UnityEngine;
using Mirror;
using Mirror.Examples.PickupsDropsChilds;

/// <summary>
/// This class inherits a class in Mirror, I override part of its function.
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
