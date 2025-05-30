using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetDieState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public NetDieState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _target.aniCtrl.SetBool(DeadHash, true);
            _target.canControl = false;
        }
    }
}