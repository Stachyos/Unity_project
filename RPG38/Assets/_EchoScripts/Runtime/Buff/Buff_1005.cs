using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    //max Hp + 10
    public class Buff_1005 : Buff
    {
        private float maxHpModify = 10F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            attr.AddMaxHealth(maxHpModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddMaxHealth(-maxHpModify);
        }
    }
}