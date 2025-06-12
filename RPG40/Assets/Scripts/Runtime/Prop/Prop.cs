using Mirror;
using UnityEngine;

namespace GameLogic.Runtime.Prop
{

    [RequireComponent(typeof(NetworkIdentity))]
    public class Prop : NetBehaviour
    {
        public int gold;
        public int hp;
        public int mp;
        public int buffId = -1;
        public int skillId = -1;

        protected override void OnServerTriggerEnter2D(Collider2D other)
        {
            base.OnServerTriggerEnter2D(other);

            if (other.CompareTag("Player") == false)
                return;

            if(other.GetComponent<Belong>() ==null  ||  other.GetComponent<Belong>().Owner.GetComponent<EchoNetPlayerCtrl>() ==null)
                return;
            
            var netPlayer = other.GetComponent<Belong>().Owner.GetComponent<EchoNetPlayerCtrl>();
            netPlayer.AddGold(this.gold);
            netPlayer.AddHealth(this.hp);
            netPlayer.AddMp(this.mp);
            if (this.buffId != -1)
            {
                netPlayer.buffSystem.AddBuff(this.buffId);
            }

            if (this.buffId != -1)
            {
                netPlayer.skillSystem.LearnSkill(this.skillId);
            }
            NetworkServer.Destroy(this.gameObject);
        }
    }
}