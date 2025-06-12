using UnityEngine;
using Mirror;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>

    public class NetAttack1State : AbstractState<NetPlayerState, EchoNetPlayerCtrl>
    {
        private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
        private float _timer;
        private bool _hasHit;

        public NetAttack1State(EasyFSM<NetPlayerState> fsm, EchoNetPlayerCtrl target)
            : base(fsm, target) { }

        protected override void OnEnter()
        {
            base.OnEnter();
            _target.ResetBools();
            _timer  = 0f;
            _hasHit = false;
            _target.aniCtrl.SetBool(Attack1Hash, true);
            _target.PlayAttackSoundOnAll(_target.characterID);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _timer += Time.deltaTime;

            // Judgment frame: Executed only once
            if (!_hasHit && _timer >= _target.attack1HitTime)
            {
                _hasHit = true;

                // 1. First, make sure the direction of the brush is correct.
                float h = _target.axisInput.x;
                if (h != 0)
                    _target.HandleFlip(h);
                // Otherwise, keep the current "faceTo"

                // 2. Select all Colliders within the circles
                Vector2 center = _target.attackPoint.position;
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    center,
                    _target.attackRadius,
                    _target.enemyLayer
                );

                
                foreach (var col in hits)
                {
                    // 3. Only process the objects that are marked as "Enemy"
                    if (!col.CompareTag("Enemy"))
                        continue;
                    
                    // 4. Take Belong, then take IChaAttr
                    var belong = col.GetComponent<Belong>();
                    if (belong == null) 
                    {

                        continue;
                    }

                    var defender = belong.Owner.GetComponent<IChaAttr>();
                    if (defender == null)
                    {

                        continue;
                    }

                    // 5. Call the DamageMgr, which is the same as Skill_1001
                    DamageMgr.ProcessDamage(
                        attacker: _target,
                        defender: defender,
                        damage:  _target.Attack,
                        //damage:  _target.attack,
                        forceCrit:  false
                    );
                }
            }

            // After the animation is over, switch to the status screen
            if (_timer >= _target.attack1Duration)
            {
                _target.aniCtrl.SetBool(Attack1Hash, false);
                if (!_target.IsGround)
                    _fsm.ChangeState(NetPlayerState.Fall);
                else
                    _fsm.ChangeState(_target.HasInput ? NetPlayerState.Move : NetPlayerState.Idle);
            }
        }

        protected override void OnExit()
        {
            base.OnExit();
            _target.aniCtrl.SetBool(Attack1Hash, false);
        }
    }
}
