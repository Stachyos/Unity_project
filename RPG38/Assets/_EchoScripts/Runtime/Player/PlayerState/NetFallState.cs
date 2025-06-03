using UnityEngine;

namespace GameLogic.Runtime
{
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
            // 掉到地面则切 Idle/Move
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