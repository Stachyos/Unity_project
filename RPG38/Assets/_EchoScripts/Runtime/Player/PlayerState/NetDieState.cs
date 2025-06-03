using UnityEngine;

namespace GameLogic.Runtime
{
    public class NetDieState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public NetDieState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            
            
            // 2. 把根节点也切到 Ignore Raycast（以防万一）——不过真正命中的是 Collider2D，
            //    但建议先改根节点，逻辑更一致
            _target.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            // 3. 找出所有子物体里挂着 2D 碰撞器（Collider2D）的对象，一并把它们的 Layer 设为 Ignore Raycast
            //    includeInactive: true 可以处理被暂时设为 inactive 的 Collider2D
            Collider2D[] all2DColliders = _target.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D col2D in all2DColliders)
            {
                col2D.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            
            _target.ResetBools();
            _target.aniCtrl.SetBool(DeadHash, true);
            _target.canControl = false;
            
        }
        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            if (_target.canControl)
            {
                _target.canControl = false;
            }

            if (_target.aniCtrl.GetBool(DeadHash) == false )
            {
                _target.canControl = true;
                _target.ResetBools();
                _target.aniCtrl.SetBool(DeadHash, true);
                
            }

            
        }
    }
}