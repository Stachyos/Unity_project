using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Buff_1009 : Buff
    {
        private float hpMaxModify = 2F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            hpMaxModify = (float)(attr.MaxHealth * 0.1);
            attr.AddMaxHealth(hpMaxModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-hpMaxModify);
        }
    }
}