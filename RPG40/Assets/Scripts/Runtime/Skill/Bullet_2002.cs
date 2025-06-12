using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    /// <summary>
    /// The individual bullet script for Skill 2002
    /// </summary>
    public class Bullet_2002 : NetSkillBehaviour
    {
        public float flySpeed = 1.5f;

        // Assigned during generation by Skill_2002
        [SyncVar]
        public Vector2 flyDir = Vector2.right;

        public override void OnStartServer()
        {
            base.OnStartServer();

            // It will be automatically destroyed after 5 seconds.
            Invoke(nameof(DestroySelf), 5f);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            // Each frame moves in the direction of flyDir at a speed of flySpeed.
            transform.Translate(flyDir * Time.deltaTime * flySpeed, Space.World);
        }

        protected override void OnServerTriggerEnter2D(Collider2D other)
        {
            base.OnServerTriggerEnter2D(other);

            // If it hits the ground, it will be immediately destroyed and stop moving.
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                DestroySelf();
                return;
            }

            // If it collides with a player, handle the damage according to the original logic and destroy it.
            if (other.CompareTag("Player") && other.GetComponent<Belong>() != null)
            {
                IChaAttr attacker = null;
                if (this.skillCaster != null)
                    attacker = this.skillCaster.GetComponent<IChaAttr>();

                var defender = other.GetComponent<Belong>().Owner.GetComponent<IChaAttr>();
                // Damage calculation: 0.7 times attack power
                if (attacker != null) DamageMgr.ProcessDamage(attacker, defender, attacker.Attack, false);
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            if (isServer)
                NetworkServer.Destroy(this.gameObject);
        }
    }
}