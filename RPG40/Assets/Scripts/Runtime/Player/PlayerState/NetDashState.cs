using UnityEngine;

namespace GameLogic.Runtime
{    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>

    public class NetDashState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private float _timer;
        private float _dir;

        public NetDashState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _timer = 0f;
            _dir = Mathf.Sign(_target.axisInput.x);
            _target.HandleFlip(_dir);

            _target.aniCtrl.SetBool(DashHash, true);

            // Set only the horizontal speed and maintain the vertical position.
            float currentY = 0;
            //float currentY = _target.rb2D.velocity.y;
            _target.rb2D.velocity = new Vector2(_dir * _target.dashSpeed, currentY);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _target.rb2D.velocity = new Vector2(_dir * _target.dashSpeed, 0);
            _timer += Time.deltaTime;
            if (_timer >= _target.dashDuration)
            {
                if (!_target.IsGround)
                    _fsm.ChangeState(NetPlayerState.Fall);
                else
                    _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
            }
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(DashHash, false);
        }
    }
}