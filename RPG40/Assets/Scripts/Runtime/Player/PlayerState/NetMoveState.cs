using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetMoveState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int MoveHash = Animator.StringToHash("Move");

        public NetMoveState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _target.aniCtrl.SetBool(MoveHash, true);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _target.aniCtrl.SetBool(MoveHash, true);
            
            if (!_target.IsGround)
            {
                _fsm.ChangeState(NetPlayerState.Fall);
                return;
            }

            // Mobility & Transformation
            float h = _target.axisInput.x;
            _target.HandleFlip(h);
            _target.rb2D.velocity = new Vector2(h * _target.horMoveSpeed, _target.rb2D.velocity.y);

            // Input has disappeared and returned to Idle
            if (!_target.HasInput)
                _fsm.ChangeState(NetPlayerState.Idle);
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(MoveHash, false);
        }
    }

}