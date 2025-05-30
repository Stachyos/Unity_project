using UnityEngine;

namespace GameLogic.Runtime
{
    public class DamageMgr
    {
        /// <summary>
        /// 处理伤害
        /// </summary>
        /// <param name="attacker">攻击者</param>
        /// <param name="defender">防御者</param>
        /// <param name="damage">伤害值</param>
        /// <param name="forceCrit">是否强制暴击</param>
        public static void ProcessDamage(IChaAttr attacker, IChaAttr defender,float damage,bool forceCrit)
        {
            if (forceCrit)
                damage *= 1.5f;
            if(defender != null && defender.IsDead == false)
                defender.BeHurt(attacker, damage);
        }
        
        public struct DamageInfo
        {
            public GameObject attacker;
            public float damage;
        }
    }
}