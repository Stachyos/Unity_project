using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    //每tick增加10点血量
    public class Buff_1001 : Buff
    {
        public override void Remove()
        {
            
        }

        public override void OnTick()
        {
            base.OnTick();

            var attr = Target.GetComponent<IChaAttr>();
            if (attr != null)
            {
                attr.AddHealth(10);
                attr.AddMp(10);
            }
        }
    }
}