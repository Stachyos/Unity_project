using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Runtime
{
    //提升移速
    public class Buff_1002 : Buff
    {
        private float modifierSpeed = 1f;
        public override void Apply(GameObject target)
        {
            base.Apply(target);
            var netPlayerCtrl = target.GetComponent<EchoNetPlayerCtrl>();
            netPlayerCtrl.horMoveSpeed += modifierSpeed;
        }

        public override void Remove()
        {
            // var netPlayerCtrl = Target.GetComponent<EchoNetPlayerCtrl>();
            // netPlayerCtrl.horMoveSpeed -= modifierSpeed;
        }   
    }
}