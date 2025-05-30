using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetJumpState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        public NetJumpState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target) 
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            // 清除水平速度保持惯性
            var v = _target.rb2D.velocity;
            _target.rb2D.velocity = new Vector2(v.x, _target.jumpForce);
            _target.aniCtrl.SetBool(JumpHash, true);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            // 一旦向下开始，就切到 Fall
            if (_target.rb2D.velocity.y <= 0f)
                _fsm.ChangeState(NetPlayerState.Fall);
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(JumpHash, false);
        }
    }
}