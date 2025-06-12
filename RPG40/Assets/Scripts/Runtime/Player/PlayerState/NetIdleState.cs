using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetIdleState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        public NetIdleState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _target.aniCtrl.SetBool(IdleHash, true);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _target.aniCtrl.SetBool(IdleHash, true);
            
            if (!_target.IsGround)
            {
                _fsm.ChangeState(NetPlayerState.Fall);
                return;
            }

            if (_target.HasInput)
                _fsm.ChangeState(NetPlayerState.Move);
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(IdleHash, false);
        }
    }
}