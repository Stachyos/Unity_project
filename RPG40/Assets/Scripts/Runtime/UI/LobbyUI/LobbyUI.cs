using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using GameLogic.Runtime.UI.AchievementUI;
using JKFrame;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in JKFrame, I override part of its function.
    /// </summary>

    [UIWindowData(typeof(LobbyUI),false,"Assets/Addressable/UI/LobbyUI.prefab",0)]
    public class LobbyUI : UI_WindowBase
    {
        public SimpleScrollerDelegate scrollerDelegate;
        
        public Button hostBtn;
        public TMP_InputField ipInputField;
        public TMP_InputField portInputField;
        public TMP_InputField usernameInputField;

        public Button achievementBtn;
        public override void Init()
        {
            base.Init();
            NetManager.Instance.networkDiscovery.StartDiscovery();
            Debug.Log("start discovery");
            
            usernameInputField.onValueChanged.AddListener((value) =>
            {
                GameHub.Interface.GetModel<UserModel>().userName = value;
            });
            usernameInputField.onEndEdit.AddListener((value) =>
            {
                GameGlobalData.serverName = value;
            });
            
            hostBtn.onClick.AddListener(() =>
            {
                if (ipInputField.text != "" && portInputField.text != "")
                {
                    if (ValidateIPAddress(ipInputField.text) && ValidatePort(portInputField.text))
                    {
                        NetworkManager.singleton.networkAddress = ipInputField.text;
                        var portTransport = NetworkManager.singleton.transport as PortTransport;
                        ushort.TryParse(portInputField.text,out var result);
                        portTransport.Port = result;

                        NetworkManager.singleton.StartHost();
                        NetManager.Instance.networkDiscovery.AdvertiseServer();
                        
                        UISystem.Close<LobbyUI>();
                        UISystem.Show<RoomUI>();
                    }
                }
            });
            
            achievementBtn.onClick.AddListener(()=>UISystem.Show<AchievementUI>());
            
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipInputField.text = ip.ToString();
                    break;
                }
            }
        }

        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            
            NetManager.Instance.networkDiscovery.OnServerAdded += RefreshDiscoveredServer;
            NetManager.Instance.networkDiscovery.OnServerRemoved += RefreshDiscoveredServer;
        }

        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            
            NetManager.Instance.networkDiscovery.OnServerAdded -= RefreshDiscoveredServer;
            NetManager.Instance.networkDiscovery.OnServerRemoved -= RefreshDiscoveredServer;
        }

        public void Connect(EchoServerResponse info)
        {
            NetManager.Instance.networkDiscovery.StopDiscovery();
            NetworkManager.singleton.StartClient(info.uri);
        }

        private void RefreshDiscoveredServer(EchoServerResponse info)
        {
            var serverList = NetManager.Instance.networkDiscovery.GetServerList();
            this.scrollerDelegate.ClearAllCells();
            var cellDatum = serverList.Select((item) => new LobbyItemData()
            {
                response = item
            });
            this.scrollerDelegate.AddCellRange(cellDatum);
            this.scrollerDelegate.ReloadData();
        }
        private static bool ValidateIPAddress(string ipAddress)
        {
            Regex validipregex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return (ipAddress != "" && validipregex.IsMatch(ipAddress.Trim()));
        }

        private static bool ValidatePort(string portStr)
        {
            if (int.TryParse(portStr, out int port))
            {
                if (port >= 0 && port <= 65535)
                {
                    return true;
                }
            }
            return false;
        }
    }
}