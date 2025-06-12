using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>


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
        
        [SerializeField]public float attackRange = 1.5f;      
        [SerializeField] public float attackInterval = 2f;     
        private float nextAttackTime = 0f;   
        
        public SkillSystem SkillSystem { set; get; }
        private FuzzyHelper fuzzyHelper;



        public override void OnStartServer()
        {
            base.OnStartServer();
            fuzzyHelper = new FuzzyHelper();
            SkillSystem = new SkillSystem(gameObject);

            SkillSystem.LearnSkill(skillId);

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
            //EasyFSM is a Tool in QFramework, I use it with a reference in Readme
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
                        fsm.ChangeState(SimpleEnemyState.Walk);
                    }
                })
                .OnExit(() => animator.SetBool("Attack", false));

            // Skill state
            fsm.State(SimpleEnemyState.Skill)
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
                    if (!hasHit && stateTimer >= 0.1f)
                    {
                        hasHit = true;
                        SkillSystem.PlaySkill(skillId);
                    }
                    if (stateTimer >= attackDuration)
                    {
                        animator.SetBool("Attack", false);
                        // return to idle
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(SimpleEnemyState.Walk);
                    }
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
                        fsm.ChangeState(SimpleEnemyState.Walk);
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
    // Skip if in Hurt, Die, Attack1 or Skill state
    if (fsm.CurrentStateId == SimpleEnemyState.Hurt 
        || fsm.CurrentStateId == SimpleEnemyState.Die 
        || fsm.CurrentStateId == SimpleEnemyState.Attack1 
        || fsm.CurrentStateId == SimpleEnemyState.Skill)
        return;

    // 1. Cast rays on both sides and collect all hits
    var leftHits  = Physics2D.RaycastAll(leftCheckPoint.position,  Vector2.left,  detectDistance, detectLayer);
    var rightHits = Physics2D.RaycastAll(rightCheckPoint.position, Vector2.right, detectDistance, detectLayer);

    // 2. Merge and filter valid hits
    var allHits = leftHits.Concat(rightHits)
                          .Where(h => h.collider != null)
                          .ToList();

    // 3. If any player detected, pick the nearest one
    if (allHits.Count > 0)
    {
        var nearestHit = allHits
            .OrderBy(h => Vector2.Distance(transform.position, h.collider.transform.position))
            .First();

        Vector3 playerPos = nearestHit.collider.transform.position;

        // Facing the player
        var face = playerPos.x > transform.position.x ? FaceTo.Right : FaceTo.Left;
        SetFaceTo(face);

        float dist = Vector2.Distance(transform.position, playerPos);

        // Calculate health ratios
        float enemyHpPct  = currentHealth / (float)maxHealth;
        var playerAttr    = nearestHit.collider.GetComponentInParent<IChaAttr>();
        float playerHpPct = (playerAttr != null)
            ? playerAttr.CurrentHealth / (float)playerAttr.MaxHealth
            : 0f;

        // Fuzzy decision
        var decision = fuzzyHelper.Decide(enemyHpPct, playerHpPct, dist, detectDistance);

        if (decision == FuzzyHelper.ActionType.Skill)
        {
            // Skill action
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackInterval * 1.3f;
                if (fsm.CurrentStateId != SimpleEnemyState.Skill)
                    fsm.ChangeState(SimpleEnemyState.Skill);
            }
        }
        else
        {
            // Melee combat (Attack1 or Walk)
            if (dist > attackRange)
            {
                // Too far: walk towards player
                if (fsm.CurrentStateId != SimpleEnemyState.Walk)
                    fsm.ChangeState(SimpleEnemyState.Walk);

                Vector2 dir = (playerPos.x > transform.position.x) ? Vector2.right : Vector2.left;
                transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
            }
            else if (Time.time >= nextAttackTime)
            {
                // In range: Attack1
                nextAttackTime = Time.time + attackInterval;
                fsm.ChangeState(SimpleEnemyState.Attack1);
            }
        }

        return;
    }

    // 4. No player detected: original patrol logic
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
        Vector2 dirVec = (patrolDir == 1) ? Vector2.right : Vector2.left;
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
            if(fsm.CurrentStateId == SimpleEnemyState.Die) return;
            base.BeHurt(attacker, damage);
            
            if (currentHealth <= 0)
            {
                //NetworkServer.Destroy(this.gameObject);
                fsm.ChangeState(SimpleEnemyState.Die);
            }
            else
            {
                if(fsm.CurrentStateId == SimpleEnemyState.Hurt || fsm.CurrentStateId == SimpleEnemyState.Attack1 || fsm.CurrentStateId == SimpleEnemyState.Skill) return;
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
            faceTo = dir;  // When the server modifies the "faceTo" property, it will automatically trigger the "OnFaceChanged" callback to the client side.
            UpdateFaceTo(dir);  // The server directly updates the visuals, while the client updates them through a Hook.
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