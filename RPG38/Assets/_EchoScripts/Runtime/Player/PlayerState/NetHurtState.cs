using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetHurtState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int HurtHash = Animator.StringToHash("GetHit");
        private float _timer;

        public NetHurtState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target) 
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            Debug.Log($"[Hurt] OnEnter at {_timer}s, currentState={_fsm.CurrentState}");
            base.OnEnter();
            _target.ResetBools();
            _timer = 0f;
            _target.aniCtrl.SetBool(HurtHash, true);

        }

        protected override void OnServerUpdate()
        {
            Debug.Log($"[Hurt] OnUpdate at {_timer}s, currentState={_fsm.CurrentState}");
            base.OnServerUpdate();
            _timer += Time.deltaTime;
            
            if (_timer >= _target.hurtDuration)
            {
                Debug.Log("[Hurt] OnServerUpdate end");

                _target.aniCtrl.SetBool(HurtHash, false);
                if (!_target.IsGround)
                    _fsm.ChangeState(NetPlayerState.Fall);
                else
                    _fsm.ChangeState(NetPlayerState.Idle);
            }
        }

        protected override void OnExit()
        {
            Debug.Log($"[Hurt] OnExit at {_timer}s, nextState={_fsm.CurrentState}");
            base.OnExit();
            _target.aniCtrl.SetBool(HurtHash, false);
        }
    }
}