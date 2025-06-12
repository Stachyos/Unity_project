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
    public class SmallEnemy : Enemy
    {
        private enum SmallEnemyState
        {
            Idle,
            Move,
            Attack,
            Hurt,
            Die
        }

        public override string Tag => "Enemy";

        //EasyFSM is a Tool in QFramework, I use it with a reference in Readme
        private EasyFSM<SmallEnemyState> fsm;

        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;

        public Transform view;

        // Patrol & Idle settings
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

        // Attack settings
        public Transform attackPoint;
        public float attackRadius = 1f;
        public LayerMask targetLayer;
        public float attackHitTime = 0.3f;
        public float attackDuration = 0.8f;
        [SerializeField] public float attackRange = 1.5f;   // melee trigger distance
        [SerializeField] public float attackInterval = 2f;  // time between attacks
        private float nextAttackTime = 0f;

        // Hurt timing
        public float hurtDuration = 0.5f;

        // State timers & flags
        private float stateTimer;
        private bool hasHit;

        [SerializeField] private Animator animator;

        private void Awake()
        {
            // Ensure animator reference is set
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Initialize patrol start
            patrolStartPos = transform.position;
            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);

            InitFSM();
            fsm.StartState(SmallEnemyState.Idle);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            UpdateAI();
            fsm.ServerUpdate();
        }

        private void InitFSM()
        {
            fsm = new EasyFSM<SmallEnemyState>();

            // Idle state
            fsm.State(SmallEnemyState.Idle)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                    // idleEndTime was set when transitioning in UpdateAI
                })
                .OnServerUpdate(() => { /* no internal logic; AI drives transitions */ })
                .OnExit(() => animator.SetBool("Idle", false));

            // Move state (uses same animation flag as Idle)
            fsm.State(SmallEnemyState.Move)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                })
                .OnServerUpdate(() => { /* movement done in UpdateAI */ })
                .OnExit(() => animator.SetBool("Idle", false));

            // Attack state
            fsm.State(SmallEnemyState.Attack)
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
                        // return to idle after attack
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(SmallEnemyState.Move);
                    }
                })
                .OnExit(() => animator.SetBool("Attack", false));

            // Hurt state
            fsm.State(SmallEnemyState.Hurt)
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
                        fsm.ChangeState(SmallEnemyState.Move);
                    }
                })
                .OnExit(() => animator.SetBool("GetHit", false));

            // Die state
            fsm.State(SmallEnemyState.Die)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Dead", true);
                    Destroy(gameObject, 1f);
                });
        }

       private void UpdateAI()
{
    if (fsm.CurrentStateId == SmallEnemyState.Hurt 
        || fsm.CurrentStateId == SmallEnemyState.Die 
        || fsm.CurrentStateId == SmallEnemyState.Attack)
        return;

 
    var leftHits  = Physics2D.RaycastAll(leftCheckPoint.position,  Vector2.left,  detectDistance, detectLayer);
    var rightHits = Physics2D.RaycastAll(rightCheckPoint.position, Vector2.right, detectDistance, detectLayer);

   
    var allHits = leftHits.Concat(rightHits)
                          .Where(h => h.collider != null)
                          .ToList();

   
    if (allHits.Count > 0)
    {
        var nearestHit = allHits
            .OrderBy(h => Vector2.Distance(transform.position, h.collider.transform.position))
            .First();

        Vector3 playerPos = nearestHit.collider.transform.position;

       
        var face = playerPos.x > transform.position.x ? FaceTo.Right : FaceTo.Left;
        SetFaceTo(face);

        float dist = Vector2.Distance(transform.position, playerPos);

    
        float speedMultiplier = (currentHealth <= maxHealth * 0.5f) ? 2f : 1f;

      
        if (dist > attackRange)
        {
            if (fsm.CurrentStateId != SmallEnemyState.Move)
                fsm.ChangeState(SmallEnemyState.Move);

            Vector2 dir = (playerPos.x > transform.position.x) ? Vector2.right : Vector2.left;
            transform.Translate(dir * patrolSpeed * speedMultiplier * Time.deltaTime, Space.World);
        }
        else if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            fsm.ChangeState(SmallEnemyState.Attack);
        }

        return;
    }

    
    if (fsm.CurrentStateId == SmallEnemyState.Idle)
    {
        if (Time.time >= idleEndTime)
        {
            patrolStartPos = transform.position;
            patrolDir *= -1;
            fsm.ChangeState(SmallEnemyState.Move);
        }
    }
    else if (fsm.CurrentStateId == SmallEnemyState.Move)
    {
        float speedMultiplier = (currentHealth <= maxHealth * 0.5f) ? 2f : 1f;
        Vector2 target = patrolStartPos + Vector2.right * patrolDistance * patrolDir;
        Vector2 dirVec = patrolDir == 1 ? Vector2.right : Vector2.left;
        SetFaceTo(patrolDir == 1 ? FaceTo.Right : FaceTo.Left);
        transform.Translate(dirVec * patrolSpeed * speedMultiplier * Time.deltaTime, Space.World);

        if (Mathf.Abs(transform.position.x - target.x) < 0.1f)
        {
            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
            fsm.ChangeState(SmallEnemyState.Idle);
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

                // 5️ Double damage if health ≤ 50%
                float damageToDeal = Attack;
                

                DamageMgr.ProcessDamage(this, defender, damageToDeal, false);
            }
        }

        public override void BeHurt(IChaAttr attacker, float damage)
        {
            if (fsm.CurrentStateId == SmallEnemyState.Die) return;

            base.BeHurt(attacker, damage);

            if (currentHealth <= 0)
            {
                fsm.ChangeState(SmallEnemyState.Die);
            }
            else
            {
                if (fsm.CurrentStateId == SmallEnemyState.Hurt) return;
                fsm.ChangeState(SmallEnemyState.Hurt);
            }
        }

        private void ResetBools()
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Attack", false);
            animator.SetBool("GetHit", false);
            animator.SetBool("Dead", false);
        }

        private void SetFaceTo(FaceTo dir)
        {
            if (faceTo == dir) return;
            faceTo = dir; // Server updates SyncVar, hook calls client update
            UpdateFaceTo(dir);
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
            // Draw detection rays
            Gizmos.DrawLine(leftCheckPoint.position, leftCheckPoint.position + Vector3.left * detectDistance);
            Gizmos.DrawLine(rightCheckPoint.position, rightCheckPoint.position + Vector3.right * detectDistance);
            // Draw attack radius
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
#endif
        }
    }
}
