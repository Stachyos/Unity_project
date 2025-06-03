using UnityEngine;

namespace GameLogic.Runtime
{
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

            // —— 空中流转 —— 
            if (!_target.IsGround)
            {
                _fsm.ChangeState(NetPlayerState.Fall);
                return;
            }

            // —— 移动 & 翻转 —— 
            float h = _target.axisInput.x;
            _target.HandleFlip(h);
            _target.rb2D.velocity = new Vector2(h * _target.horMoveSpeed, _target.rb2D.velocity.y);

            // —— 输入消失回 Idle —— 
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