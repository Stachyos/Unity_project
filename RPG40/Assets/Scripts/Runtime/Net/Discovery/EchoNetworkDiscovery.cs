using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Mirror, I override part of its function.
    /// </summary>
    
    public struct EchoServerRequest : NetworkMessage
    {
        
    }
    
    public struct EchoServerResponse : NetworkMessage
    {
        public string serverName;
        public IPEndPoint endPoint{get;set;}
        public Uri uri;
        public long serverId;
        public DateTime lastSeen;
    }
    
    public class EchoNetworkDiscovery : NetworkDiscoveryBase<EchoServerRequest, EchoServerResponse>
    {
        private Dictionary<long, EchoServerResponse> discoveredServers = new ();
        
        public event Action<EchoServerResponse> OnServerAdded;
        public event Action<EchoServerResponse> OnServerRemoved;
        
        [Header("Settings")]
        
        public float serverTimeout = 5f;
        
        public float checkInterval = 2f;
        
        protected override EchoServerResponse ProcessRequest(EchoServerRequest request, IPEndPoint endpoint)
        {
            return new EchoServerResponse()
            {
                serverName = GameGlobalData.serverName,
                uri = transport.ServerUri(),
                serverId = ServerId,
            };
        }

        protected override void ProcessResponse(EchoServerResponse response, IPEndPoint endpoint)
        {
            response.endPoint = endpoint;
            UriBuilder realUri = new UriBuilder(response.uri)
            {
                Host = response.endPoint.Address.ToString(),
            };
            response.uri = realUri.Uri;
            
            response.lastSeen = DateTime.Now;
            // Update or add the server
            if (discoveredServers.ContainsKey(response.serverId))
            {
                var serverInfo = discoveredServers[response.serverId];
                discoveredServers[response.serverId] = serverInfo;
            }
            else
            {
                discoveredServers.Add(response.serverId, response);
                OnServerAdded?.Invoke(response);
            }
        }

        public override void Start()
        {
            base.Start();
            StartCoroutine(CheckServerTimeouts());
        }
        
        private IEnumerator CheckServerTimeouts()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);
                if(discoveredServers.Count == 0)
                    continue;
                
                var expiredServers = discoveredServers
                    .Where(pair => (DateTime.Now - pair.Value.lastSeen).TotalSeconds > serverTimeout)
                    .ToList();

                foreach (var server in expiredServers)
                {
                    discoveredServers.Remove(server.Key);
                    OnServerRemoved?.Invoke(server.Value);
                }
            }
        }
        
        public List<EchoServerResponse> GetServerList()
        {
            return discoveredServers.Values.ToList();
        }
    }
}