using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetJumpState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        public NetJumpState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target) 
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            // Eliminate horizontal speed and maintain inertia
            var v = _target.rb2D.velocity;
            _target.rb2D.velocity = new Vector2(v.x, _target.jumpForce);
            _target.aniCtrl.SetBool(JumpHash, true);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            
               float vx = _target.axisInput.x * _target.horMoveSpeed;
               _target.rb2D.velocity = new Vector2(vx, _target.rb2D.velocity.y);
               _target.HandleFlip(_target.axisInput.x);
            // Once you start going down, switch to "Fall"
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