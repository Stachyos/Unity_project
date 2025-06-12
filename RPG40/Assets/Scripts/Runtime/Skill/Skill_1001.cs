using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Skill_1001 : NetSkillBehaviour
    {
        public float flySpeed = 1.5f;
        private Vector2 flyDir = Vector2.right;
        public override void OnStartServer()
        {
            base.OnStartServer();

            var owner = this.skillCaster.GetComponent<EchoNetPlayerCtrl>();
            this.transform.position = owner.skillCasterPoints[0].position;
            
            //Determine the launch direction, based on the character's current orientation.
            if (owner.faceTo == FaceTo.Left)
                flyDir = Vector2.left;
            
            Invoke(nameof(DestroySelf),5);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            transform.Translate(flyDir*Time.deltaTime*flySpeed, Space.World);
        }

        protected override void OnServerTriggerEnter2D(Collider2D other)
        {
            base.OnServerTriggerEnter2D(other);
            
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                DestroySelf();
                return;
            }
            
            if (other.CompareTag("Enemy") && other.GetComponent<Belong>())
            {
                var attacker = this.skillCaster?.GetComponent<EchoNetPlayerCtrl>();
                var defender = other.GetComponent<Belong>().Owner.GetComponent<IChaAttr>();
                if (attacker != null)
                {
                    DamageMgr.ProcessDamage(attacker, defender, (float)(10 + 0.5 * attacker.Attack), false);
                    attacker.PlaySkillSoundOnAll();
                }

                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            NetworkServer.Destroy(this.gameObject);
        }
    }
}