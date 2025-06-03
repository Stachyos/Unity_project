using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    //atk+1
    public class Buff_1004 : Buff
    {
        private float atkModify = 5F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            attr.AddAttack(atkModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-atkModify);
        }
    }
}