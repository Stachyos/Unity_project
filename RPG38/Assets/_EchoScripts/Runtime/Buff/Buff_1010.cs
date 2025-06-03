using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    
    public class Buff_1010 : Buff
    {
        private float mpMaxModify = 10F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            attr.AddMaxMp(mpMaxModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-mpMaxModify);
        }
    }
}