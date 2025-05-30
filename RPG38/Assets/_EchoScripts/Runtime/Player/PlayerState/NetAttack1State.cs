using UnityEngine;
using Mirror;

namespace GameLogic.Runtime
{
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
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            _timer += Time.deltaTime;

            // —— 判定帧：只执行一次 —— 
            if (!_hasHit && _timer >= _target.attack1HitTime)
            {
                _hasHit = true;

                // 1. 先把朝向刷对
                float h = _target.axisInput.x;
                if (h != 0)
                    _target.HandleFlip(h);
                // 否则保留当前 faceTo

                // 2. 拾取所有圆内 Collider
                Vector2 center = _target.attackPoint.position;
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    center,
                    _target.attackRadius,
                    _target.enemyLayer
                );

                // （可选）调试日志
                Debug.Log($"[Attack1] 判定中心 {center}，半径 {_target.attackRadius}，检测到 {hits.Length} 个 Collider");

                foreach (var col in hits)
                {
                    // 3. 只对标记为 "Enemy" 的物体做处理
                    if (!col.CompareTag("Enemy"))
                        continue;
                    
                    // 4. 拿 Belong，再拿 IChaAttr
                    var belong = col.GetComponent<Belong>();
                    if (belong == null) 
                    {
                        Debug.LogWarning($"[Attack1] {col.name} 找不到 Belong，跳过");
                        continue;
                    }

                    var defender = belong.Owner.GetComponent<IChaAttr>();
                    if (defender == null)
                    {
                        Debug.LogWarning($"[Attack1] {belong.Owner.name} 上找不到 IChaAttr，跳过");
                        continue;
                    }

                    // 5. 调用 DamageMgr，和 Skill_1001 一致
                    DamageMgr.ProcessDamage(
                        attacker: _target,
                        defender: defender,
                        damage:  10000,
                        //damage:  _target.attack,
                        forceCrit:  false
                    );
                }
            }

            // —— 动画打完后切状态 —— 
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
