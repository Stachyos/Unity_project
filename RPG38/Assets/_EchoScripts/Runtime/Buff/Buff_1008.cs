using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    
    public class Buff_1008 : Buff
    {
        private float atkModify = 2F;
        

        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();

            atkModify = (float)(attr.Attack * 0.3);
            attr.AddAttack(atkModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-atkModify);
        }
    }
}