using UnityEngine;

namespace GameLogic.Runtime
{
  
    public class DamageMgr
    {
        /// <summary> Dealing with Injury /// </summary>
        /// <param name="attacker">Attacker</param>
        /// <param name="defender">Defender</param>
        /// <param name="damage">Damage value</param>
        /// <param name="forceCrit">Whether to force a critical hit</param>
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