using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;         
using TMPro;                  
using Random = UnityEngine.Random;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>

    [RequireComponent(typeof(NetworkIdentity))]
    public class BossEnemy : Enemy, ISkillAbility
    {
        private enum BossState
        {
            Idle,
            Patrol, Walk,
            Attack1,
            Attack2,
            Skill,
            Hurt,
            Die
        }

        public override string Tag => "BossEnemy";

        //EasyFSM is a Tool in QFramework, I use it with a reference in Readme
        private EasyFSM<BossState> fsm;
        [SerializeField] private SpriteRenderer visualRenderer;
        
        private bool skillPlayed;
        private float flickerTimer;
        private const float flickerDuration = 2f;
        private const float flickerSpeed = 5f;
        private const float skillDuration = 0.2f;
        
        public float stopRadius = 1.2f;

        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;
        public Transform view;

        // Movement
        public float patrolSpeed = 2f;
        private float idleEndTime;
        public float idleTimeMin = 1f;
        public float idleTimeMax = 3f;

        // Detection 
        public float detectRadius = 8f;
        public LayerMask detectLayer;

        // Attack1 
        public Transform attackPoint1;
        public float attack1Radius = 1.5f;
        public LayerMask targetLayer;
        public float attack1HitTime = 0.3f;
        public float attack1Duration = 0.8f;
        public float attack1Cooldown = 1.5f;
        private float nextAttack1Time = 0f;

        // Attack2
        public Transform attackPoint2;
        public float attack2Radius = 3f;
        
        [SerializeField] private float[] attack2HitOffsets = new float[3] { 0.2f, 0.5f, 0.8f };

        // Flag bit: Indicates whether the i-th hit has been triggered
        private bool[] attack2HasHit = new bool[3];
        public float attack2Duration = 1.0f;
        public float attack2Cooldown = 3f;
        private float nextAttack2Time = 0f;

        // Skill 
        public int skillId;
        public Transform skillCasterPoint;
        public float skillCooldown = 10f;
        private float nextSkillTime = 0f;

        // Hurt
        public float hurtDuration = 0.5f;

        // Animator
        [SerializeField] private Animator animator;

        // State timers
        private float stateTimer;
        private bool hasHit;

        // MCTS & Simple Decision Tree References
        private MCTSEngine mctsEngine;
        private SimpleDecisionTree decisionTree;
        private MCTSEngine.ActionType nextAction = MCTSEngine.ActionType.None;
        private bool isInMCTS = false;
        // The A* pathfinding script reference needs to be obtained in the OnStartServer method by using FindObjectOfType<AStarPathfinder>().
        private AStarPathfinder pathfinder;

        // The current list of world coordinates obtained by pathfinding (in sequential order from the starting point to the destination)
        private List<Vector3> currentPath = null;

        // The index of the next node that needs to be flown towards in the currentPath.
        private int currentPathIndex = 0;

        // The next time when recalculating the path is allowed (Time.time)
        private float nextPathRecalcTime = 0f;

        // Re-calculate the path every (number of) seconds.
        private float pathRecalcInterval = 0.5f;
        
        

        [SerializeField] private Canvas healthBarCanvas;        
        [SerializeField] private Image healthBarFill;          
        //[SerializeField] private Text healthBarText;           
        [SerializeField] private TextMeshProUGUI healthBarText; 
        
        private int patrolDir = 1;           
        public float patrolDistance = 5f;     
        private Vector3 patrolCenter;        

        

        public SkillSystem SkillSystem { set; get; }

        public override void OnStartServer()
        {
            base.OnStartServer();

            //Initialize the MCTS engine and the decision tree
            mctsEngine = new MCTSEngine();
            decisionTree = new SimpleDecisionTree();
            // Locate the object in the scene where AStarPathfinder is installed.
            pathfinder = FindObjectOfType<AStarPathfinder>();
            if (pathfinder == null)
            {
                Debug.LogError("[Boss] OnStartServer: no AStarPathfinder，pathfinder is null！");
            }
            else
            {
                Debug.Log($"[Boss] OnStartServer: found AStarPathfinder ({pathfinder.gameObject.name})");
            }
            
            if (healthBarCanvas != null)
            {
                // Make sure that worldCamera points to the local Main Camera
                if (Camera.main != null)
                    healthBarCanvas.worldCamera = Camera.main;
                else
                    Debug.LogWarning("[BossEnemy] OnStartClient: Couldn't find MainCamera. Please check if there is a Camera in the scene and its Tag is set to \"MainCamera\".");
            }

            SkillSystem = new SkillSystem(gameObject);
            SkillSystem.LearnSkill(skillId);

            idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);

            InitFSM();
            fsm.StartState(BossState.Idle);
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            // Make sure that the Camera of the World Space Canvas also points to the main camera on the client side.
            if (healthBarCanvas != null)
            {
                if (Camera.main != null)
                    healthBarCanvas.worldCamera = Camera.main;
                else
                    Debug.LogWarning("[BossEnemy] OnStartClient: Unable to find MainCamera");
                
                OnHealthChangedEvent += (_, __) => UpdateHealthUI();

                // When entering for the first time, refresh the UI screen once.
                UpdateHealthUI();
            }
        }
        
        // private void LateUpdate()
        // {
        //     UpdateHealthUI();
        //     FaceCanvasToCamera();
        // }

        /// <summary>
        /// Update the health bar filling and text based on currentHealth / maxHealth
        /// </summary>
        private void UpdateHealthUI()
        {
            if (healthBarFill != null)
            {
                float hpPct = currentHealth / (float)maxHealth;
                hpPct = Mathf.Clamp01(hpPct);
                healthBarFill.fillAmount = hpPct;
            }

            if (healthBarText != null)
            {
                // If currentHealth and maxHealth are of type float, they can be rounded or kept with one decimal place.
                int curHpInt = Mathf.CeilToInt(currentHealth);
                int maxHpInt = Mathf.CeilToInt(maxHealth);
                healthBarText.text = $"{curHpInt}/{maxHpInt}";
            }
        }
        
        /// <summary>
        /// Make the World Space Canvas face the front of the camera 
        /// </summary>
        private void FaceCanvasToCamera()
        {
            if (healthBarCanvas != null && healthBarCanvas.worldCamera != null)
            {
                // Force the forward direction of the Canvas to be opposite to the direction of the camera.
                Transform camT = healthBarCanvas.worldCamera.transform;
                // Note: If the initial rotation of the Canvas is (90, 0, 0), you may need to make some adjustments. The following example uses the billboard method:
                healthBarCanvas.transform.forward = camT.forward * -1f;
                // healthBarCanvas.transform.rotation = Quaternion.LookRotation(healthBarCanvas.transform.position - camT.position);
            }
        }
        private void LateUpdate()
        {
            //if (!isClient) return;
            UpdateHealthUI();
            //FaceCanvasToCamera();
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            UpdateAI();
            fsm.ServerUpdate();
            //UpdateHealthUI();
            //FaceCanvasToCamera();
        }

        private void InitFSM()
        {
            fsm = new EasyFSM<BossState>();

            // Idle
            fsm.State(BossState.Idle)
                .OnEnter(() => {
                    ResetBools();
                    animator.SetBool("Idle", true);
                    idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);

                    
                    patrolCenter = transform.position;
                    patrolDir = Random.value < 0.5f ? 1 : -1;
                })
                .OnServerUpdate(() => {})
                .OnExit(() => animator.SetBool("Idle", false));
            
            fsm.State(BossState.Walk)
                .OnEnter(() => {
                    ResetBools();
                    animator.SetBool("Walk", true);
                })
                .OnServerUpdate(() => {})
                .OnExit(() => animator.SetBool("Walk", false));
            

            fsm.State(BossState.Patrol)
                .OnEnter(() => {
                    ResetBools();
                    animator.SetBool("Walk", true);
                    
                })
                .OnServerUpdate(() => {
                   
                    Vector3 target = patrolCenter + Vector3.right * patrolDistance * patrolDir;
                    Vector3 delta = target - transform.position;
                    float step = patrolSpeed * Time.deltaTime;

                    if (Mathf.Abs(delta.x) <= 0.1f)
                    {
                    
                        fsm.ChangeState(BossState.Idle);
                    }
                    else
                    {
                        float dir = Mathf.Sign(delta.x);
                        view.localScale = dir > 0 ? new Vector3(-1,1,1) : new Vector3(1,1,1);
                        transform.Translate(Vector3.right * dir * step, Space.World);
                    }
                })
                .OnExit(() => animator.SetBool("Walk", false));
            
            // Attack1
            fsm.State(BossState.Attack1)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Attack1", true);
                    stateTimer = 0f;
                    hasHit = false;
                })
                .OnServerUpdate(() =>
                {
                    stateTimer += Time.deltaTime;
                    if (!hasHit && stateTimer >= attack1HitTime)
                    {
                        hasHit = true;
                        PerformAttack1();
                    }
                    if (stateTimer >= attack1Duration)
                    {
                        animator.SetBool("Attack1", false);
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(BossState.Walk);
                    }
                })
                .OnExit(() => animator.SetBool("Attack1", false));

            // Attack2
             fsm.State(BossState.Attack2)
                     .OnEnter(() =>
                     {
                         ResetBools();
                         animator.SetBool("Attack2", true);
                         stateTimer = 0f;
                         // When entering the "Attack2" state, reset all three hit marks to "Not Hit Yet"
                             for (int i = 0; i < attack2HasHit.Length; i++)
                                 attack2HasHit[i] = false;
                     })
                 .OnServerUpdate(() =>
                     {
                         // Recorded status duration
                             stateTimer += Time.deltaTime;
                
                             // For the three-hit offset, whenever a certain moment arrives and the attack has not yet been successful, the PerformAttack2() function is called.
                                for (int i = 0; i < attack2HitOffsets.Length; i++)
                             {
                                 if (!attack2HasHit[i] && stateTimer >= attack2HitOffsets[i])
                                 {
                                         attack2HasHit[i] = true;
                                         PerformAttack2();
                                 }
                             }
                
                             // When the total duration exceeds attack2Duration, switch back to Idle state.
                                 if (stateTimer >= attack2Duration)
                             {
                                 animator.SetBool("Attack2", false);
                                 idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                                 fsm.ChangeState(BossState.Walk);
                             }
                     })
                 .OnExit(() => animator.SetBool("Attack2", false));

            // Skill
            fsm.State(BossState.Skill)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);

                    stateTimer = 0f;
                    skillPlayed = false;
                    flickerTimer = 0f;
                })
                .OnServerUpdate(() =>
                {
                    stateTimer += Time.deltaTime;

                  
                    if (stateTimer < flickerDuration)
                    {
                        flickerTimer += Time.deltaTime;
                        float t = Mathf.PingPong(flickerTimer * flickerSpeed, 1f);
                        visualRenderer.color = Color.Lerp(Color.white, Color.red, t);

                       
                        RpcFlickerEffect(true);
                        return;
                    }

                   
                    visualRenderer.color = Color.white;
                    if (!skillPlayed)
                    {
                        skillPlayed = true;
                        SkillSystem.PlaySkill(skillId);
                    }

                   
                    if (stateTimer >= flickerDuration + skillDuration)
                    {
                        animator.SetBool("Idle", false);
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(BossState.Walk);
                    }

                  
                    RpcFlickerEffect(false);
                })
                .OnExit(() =>
                    {
                      
                        visualRenderer.color = Color.white;
                        animator.SetBool("Idle", false);
                    });

            // Hurt
            fsm.State(BossState.Hurt)
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
                        fsm.ChangeState(BossState.Idle);
                    }
                })
                .OnExit(() => animator.SetBool("GetHit", false));

            // Die
            fsm.State(BossState.Die)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Dead", true);
                    Destroy(gameObject, 1f);
                });
        }
        
        [ClientRpc]
        private void RpcFlickerEffect(bool flicker)
        {
            if (flicker)
            {
                flickerTimer += Time.deltaTime;
                float t = Mathf.PingPong(flickerTimer * flickerSpeed, 1f);
                visualRenderer.color = Color.Lerp(Color.white, Color.red, t);
            }
            else
            {
                visualRenderer.color = Color.white;
            }
        }

        private void UpdateAI()
        {
           
            if (fsm.CurrentStateId == BossState.Hurt
                || fsm.CurrentStateId == BossState.Die
                || fsm.CurrentStateId == BossState.Attack1
                || fsm.CurrentStateId == BossState.Attack2
                || fsm.CurrentStateId == BossState.Skill)
                return;

         
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, detectLayer);
            Transform nearest = FindNearest(hits);
            if (nearest == null)
            {
               
                fsm.ChangeState(BossState.Idle);
                return;
            }

            float dist = Mathf.Abs(transform.position.x- nearest.position.x);

         
            SetFaceTo(nearest.position.x > transform.position.x
                ? FaceTo.Right : FaceTo.Left);

           
            float x1 = attackPoint1.transform.position.x;
            float x2 = this.transform.position.x;
            float farside = Mathf.Abs(x1 - x2)+stopRadius;
            float closeside = Mathf.Abs(x1 - x2)-stopRadius;
            
            //stopRadius;
            if (dist > farside)
            {
               
                if (fsm.CurrentStateId != BossState.Walk)
                    fsm.ChangeState(BossState.Walk);
                ChasePlayer(nearest);
                return;
            }else if (dist < closeside)
            {
              
                if (fsm.CurrentStateId != BossState.Walk)
                    fsm.ChangeState(BossState.Walk);
                MoveAwayPlayer(nearest);
                return;
                
            }


            
            if (Time.time >= nextSkillTime)
            {

        var best = mctsEngine.RunMCTS(
            new MCTSEngine.GameState(
                currentHealth / maxHealth,
                nearest.GetComponentInParent<IChaAttr>().CurrentHealth / nearest.GetComponentInParent<IChaAttr>().MaxHealth,
                dist,
                Mathf.Max(0, nextAttack1Time - Time.time),
                Mathf.Max(0, nextAttack2Time - Time.time),
                Mathf.Max(0, nextSkillTime   - Time.time)
            )
        );

      
        if (best == MCTSEngine.ActionType.Skill)
        {
            nextSkillTime = Time.time + skillCooldown;
            fsm.ChangeState(BossState.Skill);
        }
        else if (best == MCTSEngine.ActionType.Attack1)
        {
            nextAttack1Time = Time.time + attack1Cooldown;
            fsm.ChangeState(BossState.Attack1);
        }
        else if (best == MCTSEngine.ActionType.Attack2)
        {
            nextAttack2Time = Time.time + attack2Cooldown;
            fsm.ChangeState(BossState.Attack2);
        }
        
        return;
    }
            
    var simple = decisionTree.Decide(
        dist,
        currentHealth / maxHealth,
        Mathf.Max(0, nextAttack1Time - Time.time),
        Mathf.Max(0, nextAttack2Time - Time.time),
        Mathf.Max(0, nextSkillTime   - Time.time)
    );
    var action = ConvertSimpleToMCTS(simple);
    switch (action)
    {
        case MCTSEngine.ActionType.Attack1:
            if (Time.time >= nextAttack1Time)
            {
                nextAttack1Time = Time.time + attack1Cooldown;
                fsm.ChangeState(BossState.Attack1);
            }
            break;
        case MCTSEngine.ActionType.Attack2:
            if (Time.time >= nextAttack2Time)
            {
                nextAttack2Time = Time.time + attack2Cooldown;
                fsm.ChangeState(BossState.Attack2);
            }
            break;
        case MCTSEngine.ActionType.Skill:
           
            break;
        default:
            
            break;
    }
        }
        
        
        private Transform FindNearest(Collider2D[] hits)
        {
            Transform best = null;
            float minD = float.MaxValue;
            foreach (var c in hits)
            {
                if (!c.CompareTag("Player") ) continue;
                var cha = c.GetComponentInParent<IChaAttr>();
                if (cha == null || cha.CurrentHealth <= 0) 
                    continue;    
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < minD)
                {
                    minD = d;
                    best = c.transform;
                }
            }
            return best;
        }

        private void ChasePlayer(Transform player)
        {
           
            List<Vector3> path = pathfinder?.FindPath(transform.position, player.position);
            if (path != null && path.Count > 0)
            {
                MoveAlongPath(path);
            }
            else
            {
                
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                Vector3 delta = new Vector3(dir * patrolSpeed * Time.deltaTime, 0f, 0f);
                transform.Translate(delta, Space.World);
            }
        }
        
        private void MoveAwayPlayer(Transform player)
        {
            
                
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                Vector3 delta = new Vector3(-1*dir * patrolSpeed * Time.deltaTime, 0f, 0f);
                transform.Translate(delta, Space.World);
            
        }



        /// <summary>
        /// Convert SimpleDecisionTree.ActionType to MCTSEngine.ActionType.
        /// </summary>
        private MCTSEngine.ActionType ConvertSimpleToMCTS(SimpleDecisionTree.ActionType simple)
        {
            switch (simple)
            {
                case SimpleDecisionTree.ActionType.Attack1:
                    return MCTSEngine.ActionType.Attack1;
                case SimpleDecisionTree.ActionType.Attack2:
                    return MCTSEngine.ActionType.Attack2;
                case SimpleDecisionTree.ActionType.Skill:
                    return MCTSEngine.ActionType.Skill;
                default:
                    return MCTSEngine.ActionType.None;
            }
        }

        /// <summary>
        /// Switch the state based on the actionType and perform movement or attack.
        /// </summary>
        private void ExecuteAction(MCTSEngine.ActionType action, Transform player, float dist)
        {
            switch (action)
            {
                case MCTSEngine.ActionType.Attack1:
                    if (Time.time >= nextAttack1Time)
                    {
                        nextAttack1Time = Time.time + attack1Cooldown;
                        fsm.ChangeState(BossState.Attack1);
                    }
                    else
                    {
                        if (fsm.CurrentStateId != BossState.Walk)
                            fsm.ChangeState(BossState.Walk);
                        ChasePlayer(player);
                    }
                    break;

                case MCTSEngine.ActionType.Attack2:
                    if (Time.time >= nextAttack2Time)
                    {
                        nextAttack2Time = Time.time + attack2Cooldown;
                        fsm.ChangeState(BossState.Attack2);
                    }
                    else
                    {
                        if (fsm.CurrentStateId != BossState.Walk)
                            fsm.ChangeState(BossState.Walk);
                        ChasePlayer(player);
                    }
                    break;

                case MCTSEngine.ActionType.Skill:
                    
                    if (Time.time >= nextSkillTime)
                    {
                        
                        nextSkillTime = Time.time + skillCooldown;
                        fsm.ChangeState(BossState.Skill);
                    }
                    else
                    {
                        if (fsm.CurrentStateId != BossState.Walk)
                            fsm.ChangeState(BossState.Walk);
                        ChasePlayer(player);
                    }
                    break;

                case MCTSEngine.ActionType.WalkTowards:
                    if (fsm.CurrentStateId != BossState.Walk)
                        fsm.ChangeState(BossState.Walk);
                    ChasePlayer(player);
                    break;

                case MCTSEngine.ActionType.WalkAway:
                    if (fsm.CurrentStateId != BossState.Walk)
                        fsm.ChangeState(BossState.Walk);
                    MoveAwayFrom(player.position);
                    break;

                case MCTSEngine.ActionType.Idle:
                    if (fsm.CurrentStateId != BossState.Idle)
                        fsm.ChangeState(BossState.Idle);
                    break;

                case MCTSEngine.ActionType.None:
                    break;
            }
        }
        
        
        private void MoveAlongPath(List<Vector3> path)
        {
            if (currentPath != path)
            {
                currentPath = path;
                currentPathIndex = 0;
            }

            if (currentPathIndex < currentPath.Count)
            {
                Vector3 nextNode = currentPath[currentPathIndex];
                if (Vector3.Distance(transform.position, nextNode) < 0.1f)
                {
                    currentPathIndex++;
                }
                else
                {
                    Vector3 dir = (nextNode - transform.position).normalized;
                    transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
                }
            }
            else
            {
              
                fsm.ChangeState(BossState.Walk);
            }
        }
        

        /// <summary>
        /// "Flying" moves towards the target point: First, perform A* pathfinding internally to obtain a path, and then move point by point;
        /// If the path is empty or infeasible, it degenerates into a direct straight-line flight.
        /// </summary>
        /// <param name="target">The world coordinates of the player or other targets</param>
        private void MoveToward(Vector3 target)
        {
            if (pathfinder == null)
            {
                Vector3 dir = (target - transform.position).normalized;
                dir.y = 0;
                transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
                return;
            }

            bool needRecalc = false;
            if (currentPath == null || currentPathIndex >= (currentPath?.Count ?? 0))
                needRecalc = true;
            else if (Time.time >= nextPathRecalcTime)
                needRecalc = true;

            if (needRecalc)
            {
                nextPathRecalcTime = Time.time + pathRecalcInterval;
                currentPath = pathfinder.FindPath(transform.position, target);
                currentPathIndex = 0;
                
            }

            if (currentPath != null && currentPathIndex < currentPath.Count)
            {
                Vector3 nextNode = currentPath[currentPathIndex];
                float distToNode = Vector3.Distance(transform.position, nextNode);
                if (distToNode < 0.1f)
                {
                    currentPathIndex++;
                }
                else
                {
                    Vector3 dir = (nextNode - transform.position).normalized;
                    transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
                }
            }
            else
            {
                Vector3 dir = (target - transform.position).normalized;
                transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
            }
        }

        private void MoveAwayFrom(Vector3 target)
        {
            Vector2 dir = (target.x > transform.position.x) ? Vector2.left : Vector2.right;
            transform.Translate(dir * patrolSpeed * Time.deltaTime, Space.World);
        }

        private void PerformAttack1()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint1.position, attack1Radius, targetLayer);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Player")) continue;
                var defender = col.GetComponentInParent<IChaAttr>();
                if (defender == null) continue;
                DamageMgr.ProcessDamage(this, defender, this.Attack, false);
            }
        }

        private void PerformAttack2()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint2.position, attack2Radius, targetLayer);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Player")) continue;
                var defender = col.GetComponentInParent<IChaAttr>();
                if (defender == null) continue;
                DamageMgr.ProcessDamage(this, defender, this.Attack * 0.5f, false);
            }
        }

        public override void BeHurt(IChaAttr attacker, float damage)
        {
            if (fsm.CurrentStateId == BossState.Die) return;
            base.BeHurt(attacker, damage);
            UpdateHealthUI();

            
            if (currentHealth <= 0)
                fsm.ChangeState(BossState.Die);
            else if (fsm.CurrentStateId != BossState.Hurt && fsm.CurrentStateId != BossState.Attack1 && fsm.CurrentStateId != BossState.Skill && fsm.CurrentStateId != BossState.Attack2)
                fsm.ChangeState(BossState.Hurt);
        }

        private void ResetBools()
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", false);
            animator.SetBool("Attack1", false);
            animator.SetBool("Attack2", false);
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
            view.localScale = dir == FaceTo.Right ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint1.position, attack1Radius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint2.position, attack2Radius);
#endif
        }
    }
}
