using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class NetDieState : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public NetDieState(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _target.aniCtrl.SetBool("Dead", true);
            

            _target.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");


            Collider2D[] all2DColliders = _target.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D col2D in all2DColliders)
            {
                col2D.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            
            _target.canControl = false;
            
        }
        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();

            // if (_target.canControl)
            // {
            //     _target.canControl = false;
            // }
            //
            // if (_target.aniCtrl.GetBool(DeadHash) == false )
            // {
            //     _target.canControl = true;
            //     _target.ResetBools();
            //     _target.aniCtrl.SetBool(DeadHash, true);
            //     
            // }

            
        }
    }
}