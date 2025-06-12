using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetSkillState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private float _timer;

        public NetSkillState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            // Trigger the activation of the skill
            _target.skillSystem.PlaySkill(_target.learnSkillId);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _timer += Time.deltaTime;
            if (_timer >= _target.skillDelay)
            {
                // After the playback is completed, switch according to the input status.
                if (!_target.IsGround)
                    _fsm.ChangeState(NetPlayerState.Fall);
                else
                    _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
            }
        }
    }
}