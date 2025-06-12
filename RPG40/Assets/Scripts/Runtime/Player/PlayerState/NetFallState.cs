using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetFallState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int FallHash = Animator.StringToHash("Fall");

        public NetFallState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target) 
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _target.aniCtrl.SetBool(FallHash, true);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
               float vx = _target.axisInput.x * _target.horMoveSpeed;
               _target.rb2D.velocity = new Vector2(vx, _target.rb2D.velocity.y);
               _target.HandleFlip(_target.axisInput.x);
            // If it falls to the ground, then switch to Idle/Move mode.
            if (_target.IsGround)
                _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(FallHash, false);
        }
    }
}