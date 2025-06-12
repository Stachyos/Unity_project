using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Skill_2001 : NetSkillBehaviour
    {
        public float flySpeed = 1.5f;
        private Vector2 flyDir = Vector2.right;
        public override void OnStartServer()
        {
            base.OnStartServer();

            var owner = this.skillCaster.GetComponent<SimpleEnemy>();
            if (owner != null)
            {
                this.transform.position = owner.skillCasterPoint.position;
            
                //Determine the launch direction, based on the character's current orientation.
                if (owner.faceTo == FaceTo.Left)
                    flyDir = Vector2.left;
            
                Invoke(nameof(DestroySelf),5);
            }
            else
            {
                var owner2 = this.skillCaster.GetComponent<Enemy3>();
                if (owner2 == null) return;
                
                this.transform.position = owner2.skillCasterPoint.position;
            
                //Determine the launch direction, based on the character's current orientation.
                if (owner2.faceTo == FaceTo.Left)
                    flyDir = Vector2.left;
            
                Invoke(nameof(DestroySelf),5);
            }
            
           
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
            
            if (other.CompareTag("Player") && other.GetComponent<Belong>())
            {
                IChaAttr attacker = null;
                if (this.skillCaster != null)
                {
                    attacker = this.skillCaster.GetComponent<IChaAttr>();
                }
                var defender = other.GetComponent<Belong>().Owner.GetComponent<IChaAttr>();
                if (attacker != null)
                    DamageMgr.ProcessDamage(attacker, defender, (float)(attacker.Attack * 0.6), false);
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            NetworkServer.Destroy(this.gameObject);
        }
    }
}