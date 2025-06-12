using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{

    public class Buff_1011 : Buff
    {
        private float mpMaxModify = 2F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            mpMaxModify = (float)(attr.MaxMp * 0.1);
            attr.AddMaxMp(mpMaxModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-mpMaxModify);
        }
    }
}