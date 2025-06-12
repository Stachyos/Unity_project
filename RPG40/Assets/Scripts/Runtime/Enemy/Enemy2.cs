using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>

    [RequireComponent(typeof(NetworkIdentity))]
    public class Enemy2 : Enemy
    {
        private enum Enemy2State
        {
            Idle,
            Move,
            Attack,
            Hurt,
            Die
        }

        public override string Tag => "Enemy";

        //EasyFSM is a Tool in QFramework, I use it with a reference in Readme
        private EasyFSM<Enemy2State> fsm;

        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;

        public Transform view;

        // Prefab for the two smaller enemies spawned on death
        [Header("Spawn on Death")]
        [Tooltip("Assign the small enemy prefab (must be a registered Network prefab).")]
        public GameObject smallEnemyPrefab;
        [Tooltip("Horizontal offset for each spawned small enemy.")]
        public float spawnOffsetX = 0.5f;

        // Knockback settings
        [Header("Knockback on Hit")]
        [Tooltip("Force applied to the player on successful melee hit.")]
        public float knockbackForce = 5f;

        // Patrol & Idle settings
        [Header("Patrol & Idle")]
        public float patrolDistance = 5f;
        public float patrolSpeed = 2f;
        private Vector2 patrolStartPos;
        private int patrolDir = -1;
        private float idleEndTime;
        public float idleTimeMin = 1f;
        public float idleTimeMax = 3f;

        // Detection
        [Header("Detection")]
        public float detectDistance = 5f;
        public LayerMask detectLayer;
        public Transform leftCheckPoint;
        public Transform rightCheckPoint;

        // Attack settings
        [Header("Attack")]
        public Transform attackPoint;
        public float attackRadius = 1f;
        public LayerMask targetLayer;
        public float attackHitTime = 0.3f;
        public float attackDuration = 0.8f;
        [SerializeField] public float attackRange = 1.5f;   // melee trigger distance
        [SerializeField] public float attackInterval = 2f;  // time between attacks
        private float nextAttackTime = 0f;

        // Hurt timing
        [Header("Hurt")]
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

            if (smallEnemyPrefab == null)
            {
                Debug.LogError("[Enemy2] smallEnemyPrefab is not assigned in inspector!");
            }

            // Initialize patrol start
            patrolStartPos = transform.position;
            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);

            InitFSM();
            fsm.StartState(Enemy2State.Idle);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            UpdateAI();
            fsm.ServerUpdate();
        }

        private void InitFSM()
        {
            fsm = new EasyFSM<Enemy2State>();

            // Idle state
            fsm.State(Enemy2State.Idle)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                    // idleEndTime was set when transitioning in UpdateAI
                })
                .OnServerUpdate(() => { /* no internal logic; AI drives transitions */ })
                .OnExit(() => animator.SetBool("Idle", false));

            // Move state (uses same animation flag as Idle)
            fsm.State(Enemy2State.Move)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                })
                .OnServerUpdate(() => { /* movement done in UpdateAI */ })
                .OnExit(() => animator.SetBool("Idle", false));

            // Attack state
            fsm.State(Enemy2State.Attack)
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
                        fsm.ChangeState(Enemy2State.Move);
                    }
                })
                .OnExit(() => animator.SetBool("Attack", false));

            // Hurt state
            fsm.State(Enemy2State.Hurt)
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
                        fsm.ChangeState(Enemy2State.Move);
                    }
                })
                .OnExit(() => animator.SetBool("GetHit", false));

            // Die state
            fsm.State(Enemy2State.Die)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Dead", true);
                    
                    SpawnSmallEnemies();
                    
                    Destroy(gameObject, 1f);
                });
        }
        
        private Transform GetNearestPlayerByRays()
        {
            var detected = new List<Transform>();
    
           
            var hitsL = Physics2D.RaycastAll(
                leftCheckPoint.position,
                Vector2.left,
                detectDistance,
                detectLayer
            );
            foreach (var h in hitsL)
            {
                if (h.collider.CompareTag("Player"))
                    detected.Add(h.collider.transform);
            }

            var hitsR = Physics2D.RaycastAll(
                rightCheckPoint.position,
                Vector2.right,
                detectDistance,
                detectLayer
            );
            foreach (var h in hitsR)
            {
                if (h.collider.CompareTag("Player"))
                    detected.Add(h.collider.transform);
            }

            if (detected.Count == 0)
                return null;

           
            var uniq = new HashSet<Transform>(detected);

         
            Transform nearest = null;
            float minD = float.MaxValue;
            Vector2 pos = transform.position;
            foreach (var t in uniq)
            {
                float d = Vector2.Distance(pos, t.position);
                if (d < minD)
                {
                    minD = d;
                    nearest = t;
                }
            }
            return nearest;
        }

        private void UpdateAI()
        {
            // Do nothing if Hurt, Die or already Attacking
            if (fsm.CurrentStateId == Enemy2State.Hurt 
                || fsm.CurrentStateId == Enemy2State.Die 
                || fsm.CurrentStateId == Enemy2State.Attack)
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

                // Face the player
                var face = playerPos.x > transform.position.x ? FaceTo.Right : FaceTo.Left;
                SetFaceTo(face);

                float dist = Vector2.Distance(transform.position, playerPos);

                // 2️ Determine speed multiplier (double if health ≤ 50%)
                float speedMultiplier = (currentHealth <= maxHealth * 0.5f) ? 2f : 1f;

                // 3️ Chase or attack
                if (dist > attackRange)
                {
                    if (fsm.CurrentStateId != Enemy2State.Move)
                        fsm.ChangeState(Enemy2State.Move);

                    Vector2 dir = (playerPos.x > transform.position.x) ? Vector2.right : Vector2.left;
                    transform.Translate(dir * patrolSpeed * speedMultiplier * Time.deltaTime, Space.World);
                }
                else if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackInterval;
                    fsm.ChangeState(Enemy2State.Attack);
                }

                return;
            }

            // 4️ No player detected → patrol between Idle and Move
            if (fsm.CurrentStateId == Enemy2State.Idle)
            {
                if (Time.time >= idleEndTime)
                {
                    patrolStartPos = transform.position;
                    patrolDir *= -1;
                    fsm.ChangeState(Enemy2State.Move);
                }
            }
            else if (fsm.CurrentStateId == Enemy2State.Move)
            {
                float speedMultiplier = (currentHealth <= maxHealth * 0.5f) ? 2f : 1f;
                Vector2 target = patrolStartPos + Vector2.right * patrolDistance * patrolDir;
                Vector2 dirVec = patrolDir == 1 ? Vector2.right : Vector2.left;
                SetFaceTo(patrolDir == 1 ? FaceTo.Right : FaceTo.Left);
                transform.Translate(dirVec * patrolSpeed * speedMultiplier * Time.deltaTime, Space.World);
                if (Mathf.Abs(transform.position.x - target.x) < 0.1f)
                {
                    idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                    fsm.ChangeState(Enemy2State.Idle);
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
                var playerObj = belong.Owner;
                var defender = playerObj.GetComponent<IChaAttr>();
                if (defender == null) continue;
                
                float damageToDeal = Attack;
                if (currentHealth <= maxHealth * 0.5f)
                    damageToDeal *= 2f;

                DamageMgr.ProcessDamage(this, defender, damageToDeal, false);
                Debug.Log("[Enemy2] Performed circle hit", this);
                Debug.Log(Environment.StackTrace);
                
                var playerRb = playerObj.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockDir = (playerObj.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }

        public override void BeHurt(IChaAttr attacker, float damage)
        {
            if (fsm.CurrentStateId == Enemy2State.Die) return;

            base.BeHurt(attacker, damage);

            if (currentHealth <= 0)
            {
                fsm.ChangeState(Enemy2State.Die);
            }
            else
            {
                if (fsm.CurrentStateId == Enemy2State.Hurt) return;
                fsm.ChangeState(Enemy2State.Hurt);
            }
        }

        /// <summary>
        /// Generate two small enemies at the current location (called only on the server).
        /// </summary>
        [Server]
        private void SpawnSmallEnemies()
        {
            if (smallEnemyPrefab == null) return;

      
            Vector3 deathPos = transform.position;

            
            for (int i = 0; i < 2; i++)
            {
                float offsetX = (i == 0) ? -spawnOffsetX : spawnOffsetX;
                Vector3 spawnPos = new Vector3(deathPos.x + offsetX, deathPos.y, deathPos.z);

                GameObject small = Instantiate(smallEnemyPrefab, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(small);
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
            faceTo = dir; 
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
