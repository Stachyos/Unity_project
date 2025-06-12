using System.Collections.Generic;
using UnityEngine;


namespace GameLogic.Runtime
{

    
    public class Buff_1001 : Buff
    {
        private float mpMaxModify = 20F;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var attr = target.GetComponent<IChaAttr>();
            attr.AddMp(mpMaxModify);
            attr.AddHealth(mpMaxModify);
        }

        public override void Remove()
        {
            // var attr = Target.GetComponent<IChaAttr>();
            // attr.AddAttack(-mpMaxModify);
        }
    }
}