using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetSkillState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private float _timer;

        public NetSkillState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            // 触发技能释放
            _target.skillSystem.PlaySkill(_target.learnSkillId);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _timer += Time.deltaTime;
            if (_timer >= _target.skillDelay)
            {
                // 播放完毕后根据输入状态切换
                _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
            }
        }
    }
}