using System;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    ///  2002 号技能：朝向周围 360° 一次性发射 36 发子弹
    ///  其余逻辑（伤害、销毁、飞行速度等）完全沿用 Bullet_2002 中的实现。
    /// </summary>
    public class Skill_2002 : NetSkillBehaviour
    {
        [Header("—— 子弹 Prefab ——")]
        [Tooltip("拖入一个带有 Bullet_2002 脚本的子弹预制体")]
        public GameObject bulletPrefab;

        public override void OnStartServer()
        {
            base.OnStartServer();

            // 拿到施法者（和 Skill_2001 中的 owner 一样）
            var ownerEnemy = this.skillCaster.GetComponent<BossEnemy>();
            Vector3 spawnPos = ownerEnemy.skillCasterPoint.position;

            // 一共 36 枚，角度间隔 10 度
            for (int i = 0; i < 36; i++)
            {
                // 计算出 0..350 度，每 10 度一个方向
                float angleInRad = i * 10f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angleInRad), Mathf.Sin(angleInRad));

                // 在 spawnPos 位置生成一颗子弹
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                
                // 拿到子弹脚本，设置它的飞行方向和 skillCaster
                var bulletScript = bulletObj.GetComponent<Bullet_2002>();
                bulletScript.flyDir = dir;
                bulletScript.skillCaster = this.skillCaster;

                // 服务端生成后需要交给 Mirror 托管
                NetworkServer.Spawn(bulletObj);
            }

            // 生成完 36 发子弹后，立刻销毁自身（这个 Skill_2002 的空 GameObject）
            NetworkServer.Destroy(this.gameObject);
        }
    }
}