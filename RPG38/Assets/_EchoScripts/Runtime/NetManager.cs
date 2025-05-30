using System;
using System.Collections.Generic;
using JKFrame;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetManager : MonoBehaviour
    {
        public EchoNetworkDiscovery networkDiscovery;
        public NetworkManager networkManager;
        public Transport transport;

        public long LocalClientId;
        public NetworkIdentity identity;
        public EchoNetworkRoomPlayer net;
        public Dictionary<long,string> ClientInfos = new Dictionary<long, string>();
        
        public static NetManager Instance;
        private void Awake()
        {
            Instance = this;
        }
    }

}