using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Runtime
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class SimpleEnemy : Enemy, ISkillAbility
    {
        
        private enum SimpleEnemyState
        {
            Idle,
            Walk,
            Attack1,
            Skill,
            Hurt,
            Die
        }

        public override string Tag => "SimpleEnemy";

        private EasyFSM<SimpleEnemyState> fsm;
        
        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;
        
        public Transform view;

        // Patrol settings
        public float patrolDistance = 5f;
        public float patrolSpeed = 2f;
        private Vector2 patrolStartPos;
        private int patrolDir = -1;
        private float idleEndTime;
        public float idleTimeMin = 1f;
        public float idleTimeMax = 3f;

        // Detection
        public float detectDistance = 5f;
        public LayerMask detectLayer;
        public Transform leftCheckPoint;
        public Transform rightCheckPoint;

        // Attack1 settings
        public Transform attackPoint;
        public float attackRadius = 1f;
        public LayerMask targetLayer;
        public float attackHitTime = 0.3f;
        public float attackDuration = 0.8f;

        // Skill toggle (testing)
        public bool useSkill;
        public int skillId;
        public Transform skillCasterPoint;

        // Hurt timing
        public float hurtDuration = 0.5f;

        // State timers
        private float stateTimer;
        private bool hasHit;
        [SerializeField] private Animator animator;
        
        [SerializeField]public float attackRange = 1.5f;      // 近身触发Attack1的距离
        [SerializeField] public float attackInterval = 2f;     // 两次Attack1之间的间隔
        private float nextAttackTime = 0f;    // 下次允许Attack1的时间
        
        public SkillSystem SkillSystem { set; get; }



        public override void OnStartServer()
        {
            base.OnStartServer();
            SkillSystem = new SkillSystem(gameObject);

            patrolStartPos = transform.position;
            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);

            InitFSM();
            fsm.StartState(SimpleEnemyState.Idle);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            UpdateAI();
            fsm.ServerUpdate();
        }

        private void InitFSM()
        {
            fsm = new EasyFSM<SimpleEnemyState>();

            // Idle state
            fsm.State(SimpleEnemyState.Idle)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                    // idleEndTime already set in UpdateAI when transitioning
                })
                .OnServerUpdate(() => { /* no internal logic */ })
                .OnExit(() => animator.SetBool("Idle", false));

            // Walk state
            fsm.State(SimpleEnemyState.Walk)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Walk", true);
                })
                .OnServerUpdate(() => { /* movement in UpdateAI */ })
                .OnExit(() => animator.SetBool("Walk", false));

            // Attack1 state
            fsm.State(SimpleEnemyState.Attack1)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Attack", true);
                    stateTimer = 0f;
                    hasHit = false;
                })
                .OnServerUpdate(() =>
                {
                    stateTimer += Time.deltaTime;
                    if (!hasHit && stateTimer >= attackHitTime)
                    {
                        hasHit = true;
                        PerformAttackHit();
                    }
                    if (stateTimer >= attackDuration)
                    {
                        animator.SetBool("Attack", false);
                        // return to idle
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(SimpleEnemyState.Idle);
                    }
                })
                .OnExit(() => animator.SetBool("Attack", false));

            // Skill state
            fsm.State(SimpleEnemyState.Skill)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Attack", true);
                    SkillSystem.PlaySkill(skillId);
                })
                .OnServerUpdate(() =>
                {
                    // immediately return to idle after playing
                    fsm.ChangeState(SimpleEnemyState.Idle);
                })
                .OnExit(() => animator.SetBool("Attack", false));

            // Hurt state
            fsm.State(SimpleEnemyState.Hurt)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("GetHit", true);
                    stateTimer = 0f;
                })
                .OnServerUpdate(() =>
                {
                    stateTimer += Time.deltaTime;
                    if (stateTimer >= hurtDuration)
                    {
                        animator.SetBool("GetHit", false);
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(SimpleEnemyState.Idle);
                    }
                })
                .OnExit(() => animator.SetBool("GetHit", false));

            // Die state
            fsm.State(SimpleEnemyState.Die)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Dead", true);
                    Destroy(gameObject, 1f);
                });
        }
        

private void UpdateAI()
{
    if (fsm.CurrentStateId == SimpleEnemyState.Hurt || fsm.CurrentStateId == SimpleEnemyState.Die) return;
    // —— 1️⃣ 检测玩家 —— 
    RaycastHit2D hit = Physics2D.Raycast(leftCheckPoint.position, Vector2.left,  detectDistance, detectLayer);
    if (!hit.collider)
        hit = Physics2D.Raycast(rightCheckPoint.position, Vector2.right, detectDistance, detectLayer);

    if (hit.collider)
    {
        Vector3 playerPos = hit.collider.transform.position;
        // 朝向玩家
        var face = playerPos.x > transform.position.x ? FaceTo.Right : FaceTo.Left;
        SetFaceTo(face);

        float dist = Vector2.Distance(transform.position, playerPos);

        // —— 2️⃣ 如果要测试施法 —— 
        if (useSkill)
        {
            if (fsm.CurrentStateId != SimpleEnemyState.Skill)
                fsm.ChangeState(SimpleEnemyState.Skill);
        }
        else
        {
            // —— 3️⃣ 否则走向 & 近战 —— 
            if (dist > attackRange)
            {
                if (fsm.CurrentStateId == SimpleEnemyState.Attack1) return;
                // 距离太远：走过去
                if (fsm.CurrentStateId != SimpleEnemyState.Walk)
                    fsm.ChangeState(SimpleEnemyState.Walk);

                Vector2 dir = (playerPos.x > transform.position.x) ? Vector2.right : Vector2.left;
                transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
            }
            else if (Time.time >= nextAttackTime)
            {
                // 到了近战范围并且间隔到了：发动 Attack1
                nextAttackTime = Time.time + attackInterval;
                fsm.ChangeState(SimpleEnemyState.Attack1);
            }
        }
        return;
    }

    // —— 4️⃣ 没看到玩家，继续原有巡逻 —— 
    if (fsm.CurrentStateId == SimpleEnemyState.Idle)
    {
        if (Time.time >= idleEndTime)
        {
            patrolStartPos = transform.position;
            patrolDir *= -1;
            fsm.ChangeState(SimpleEnemyState.Walk);
        }
    }
    else if (fsm.CurrentStateId == SimpleEnemyState.Walk)
    {
        Vector2 target = patrolStartPos + Vector2.right * patrolDistance * patrolDir;
        Vector2 dirVec = patrolDir == 1 ? Vector2.right : Vector2.left;
        SetFaceTo(patrolDir == 1 ? FaceTo.Right : FaceTo.Left);
        transform.Translate(dirVec * patrolSpeed * Time.deltaTime, Space.World);
        if (Mathf.Abs(transform.position.x - target.x) < 0.1f)
        {
            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
            fsm.ChangeState(SimpleEnemyState.Idle);
        }
    }
}


        private void PerformAttackHit()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, targetLayer);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Player")) continue;
                var belong = col.GetComponent<Belong>();
                if (belong == null) continue;
                var defender = belong.Owner.GetComponent<IChaAttr>();
                if (defender == null) continue;
                DamageMgr.ProcessDamage(this, defender, this.Attack, false);
            }
        }

        public override void BeHurt(IChaAttr attacker, float damage)
        {
            base.BeHurt(attacker, damage);
            if (currentHealth <= 0)
            {
                //NetworkServer.Destroy(this.gameObject);
                fsm.ChangeState(SimpleEnemyState.Die);
            }
            else
            {
                fsm.ChangeState(SimpleEnemyState.Hurt);
            }
        }

        private void ResetBools()
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", false);
            animator.SetBool("Attack", false);
            animator.SetBool("GetHit", false);
            animator.SetBool("Dead", false);
        }

        private void SetFaceTo(FaceTo dir)
        {
            if (faceTo == dir) return;
            faceTo = dir;  // 服务端修改faceTo会自动触发OnFaceChanged回调到客户端
            UpdateFaceTo(dir);  // 服务端直接更新视觉，客户端通过Hook更新
        }

        private void OnFaceChanged(FaceTo oldDir, FaceTo newDir)
        {
            UpdateFaceTo(newDir);
        }

        private void UpdateFaceTo(FaceTo dir)
        {
            view.localScale = dir == FaceTo.Left ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.DrawLine(leftCheckPoint.position, leftCheckPoint.position + Vector3.left * detectDistance);
            Gizmos.DrawLine(rightCheckPoint.position, rightCheckPoint.position + Vector3.right * detectDistance);
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
#endif
        }



    }
}