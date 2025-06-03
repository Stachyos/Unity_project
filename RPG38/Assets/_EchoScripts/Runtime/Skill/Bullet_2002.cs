using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    ///  2002 号技能的单个子弹脚本，
    ///  和 Skill_2001 里原本的子弹逻辑一模一样，
    ///  但把飞行方向改为从外部传入 (SyncVar)。
    /// </summary>
    public class Bullet_2002 : NetSkillBehaviour
    {
        // 飞行速度（和原来 Skill_2001 保持一致）
        public float flySpeed = 1.5f;

        // 由 Skill_2002 在生成时赋值
        [SyncVar]
        public Vector2 flyDir = Vector2.right;

        public override void OnStartServer()
        {
            base.OnStartServer();

            // 5 秒后自动销毁
            Invoke(nameof(DestroySelf), 5f);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            // 每帧沿着 flyDir 方向以 flySpeed 速度移动
            transform.Translate(flyDir * Time.deltaTime * flySpeed, Space.World);
        }

        protected override void OnServerTriggerEnter2D(Collider2D other)
        {
            base.OnServerTriggerEnter2D(other);

            // 如果撞到地面（Ground）就直接销毁
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                DestroySelf();
                return;
            }

            // 如果撞到玩家，就根据原来逻辑处理伤害并销毁
            if (other.CompareTag("Player") && other.GetComponent<Belong>() != null)
            {
                IChaAttr attacker = null;
                if (this.skillCaster != null)
                    attacker = this.skillCaster.GetComponent<IChaAttr>();

                var defender = other.GetComponent<Belong>().Owner.GetComponent<IChaAttr>();
                // 伤害计算：0.7 倍攻击力
                DamageMgr.ProcessDamage(attacker, defender, 10, false);
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