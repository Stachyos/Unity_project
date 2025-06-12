using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetHurtState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int HurtHash = Animator.StringToHash("GetHit");
        private float _timer;

        public NetHurtState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target) 
            : base(fsm, target) { }

        protected override void OnEnter()
        {

            base.OnEnter();
            _target.ResetBools();
            _timer = 0f;
            _target.aniCtrl.SetBool(HurtHash, true);

        }

        protected override void OnServerUpdate()
        {

            base.OnServerUpdate();
            _timer += Time.deltaTime;
            
            if (_timer >= _target.hurtDuration)
            {

                _target.aniCtrl.SetBool(HurtHash, false);
                if (!_target.IsGround)
                    _fsm.ChangeState(NetPlayerState.Fall);
                else
                    _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
            }
        }

        protected override void OnExit()
        {

            base.OnExit();
            _target.aniCtrl.SetBool(HurtHash, false);
        }
    }
}