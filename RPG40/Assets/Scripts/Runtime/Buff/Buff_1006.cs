using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    

    public class Buff_1006 : Buff
    {
        private int GoldNumberModify = 100;
        public override void Apply(GameObject target)
        {
            base.Apply(target);

            var netPlayerCtrl = target.GetComponent<EchoNetPlayerCtrl>();
            netPlayerCtrl.AddGold(GoldNumberModify);
        }

        public override void Remove()
        {
        }
    }
}