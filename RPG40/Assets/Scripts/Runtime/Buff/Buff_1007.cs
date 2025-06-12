using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Buff_1007 : Buff
    {
        private float hpMaxModify = 50F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            attr.AddMaxHealth(-hpMaxModify);
            attr.AddHealth(-hpMaxModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-hpMaxModify);
        }
    }
}