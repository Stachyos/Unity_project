using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetBehaviour : NetworkBehaviour
    {
        protected virtual void Update()
        {
            if(IsServer())
                OnServerUpdate();
            
            if(IsOwnerClient())
                OnClientUpdate();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(this.IsServer())
                OnServerTriggerEnter2D(other);
            
            if(IsOwnerClient())
                OnClientTriggerEnter2D(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if(this.IsServer())
                OnServerTriggerExit2D(other);
            
            if(IsOwnerClient())
                OnClientTriggerExit2D(other);
        }

        protected virtual void OnServerUpdate()
        {
            
        }

        protected virtual void OnClientUpdate()
        {
            
        }

        protected virtual void OnServerTriggerEnter2D(Collider2D other)
        {
            
        }
        
        protected virtual void OnServerTriggerExit2D(Collider2D other)
        {
            
        }
        
        protected virtual void OnClientTriggerEnter2D(Collider2D other)
        {
            
        }
        
        protected virtual void OnClientTriggerExit2D(Collider2D other)
        {
            
        }

        protected virtual bool IsServer()
        {
            return NetworkServer.active;
        }
        
        protected virtual bool IsOwnerClient()
        {
            return NetworkClient.active && isClient && NetworkClient.isConnected && this.isOwned && NetworkClient.ready;
        }
    }
}