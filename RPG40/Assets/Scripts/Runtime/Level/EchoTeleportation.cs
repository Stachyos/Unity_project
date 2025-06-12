using Mirror;
using UnityEngine;

namespace GameLogic.Runtime.Level
{

    public class EchoTeleportation : NetBehaviour
    {
        protected override void OnServerTriggerEnter2D(Collider2D other)
        {
            base.OnServerTriggerEnter2D(other);

            if (other.CompareTag("Player"))
            {
                var mgr = Object.FindObjectOfType<EchoLevelMgr>();
                mgr.RpcShowLevelRewardUI();
                NetworkServer.Destroy(this.gameObject);
            }
        }
    }
}