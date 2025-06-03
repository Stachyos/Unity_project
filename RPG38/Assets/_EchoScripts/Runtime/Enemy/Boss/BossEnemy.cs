using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;         // 用于 Image、Text
using TMPro;                  // 如果使用 TextMeshProUGUI
using Random = UnityEngine.Random;

namespace GameLogic.Runtime
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class BossEnemy : Enemy, ISkillAbility
    {
        private enum BossState
        {
            Idle,
            Walk,
            Attack1,
            Attack2,
            Skill,
            Hurt,
            Die
        }

        public override string Tag => "BossEnemy";

        private EasyFSM<BossState> fsm;

        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;
        public Transform view;

        // Movement
        public float patrolSpeed = 2f;
        private float idleEndTime;
        public float idleTimeMin = 1f;
        public float idleTimeMax = 3f;

        // Detection (圆形范围)
        public float detectRadius = 8f;
        public LayerMask detectLayer;

        // Attack1 (近战)
        public Transform attackPoint1;
        public float attack1Radius = 1.5f;
        public LayerMask targetLayer;
        public float attack1HitTime = 0.3f;
        public float attack1Duration = 0.8f;
        public float attack1Cooldown = 1.5f;
        private float nextAttack1Time = 0f;

        // Attack2 (中距离)
        public Transform attackPoint2;
        public float attack2Radius = 3f;
        
        // 攻击2 的三次命中偏移（单位：秒），你可以根据动画帧长调整这三个值
        [SerializeField] private float[] attack2HitOffsets = new float[3] { 0.2f, 0.5f, 0.8f };

        // 标志位：是否已经触发了第 i 次命中
        private bool[] attack2HasHit = new bool[3];
        public float attack2Duration = 1.0f;
        public float attack2Cooldown = 3f;
        private float nextAttack2Time = 0f;

        // Skill (大招)
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

        // MCTS & 简单决策树 引用
        private MCTSEngine mctsEngine;
        private SimpleDecisionTree decisionTree;
        private MCTSEngine.ActionType nextAction = MCTSEngine.ActionType.None;
        private bool isInMCTS = false;
        // A* 寻路脚本引用，需要在 OnStartServer 中通过 FindObjectOfType<AStarPathfinder>() 拿到
        private AStarPathfinder pathfinder;

        // 当前寻路得到的世界坐标列表（按顺序从起点到终点）
        private List<Vector3> currentPath = null;

        // currentPath 中，下一个要飞向的节点索引
        private int currentPathIndex = 0;

        // 下一次允许重新计算路径的时间（Time.time）
        private float nextPathRecalcTime = 0f;

        // 每隔多长时间重新计算一次路径（秒）
        private float pathRecalcInterval = 0.5f;
        
        [Header("—— 血条 UI 引用 ——")]
        [SerializeField] private Canvas healthBarCanvas;        // World Space Canvas
        [SerializeField] private Image healthBarFill;           // 红色填充 Image，类型为 Filled → Horizontal
        //[SerializeField] private Text healthBarText;           // 如果你用 UnityEngine.UI.Text
        [SerializeField] private TextMeshProUGUI healthBarText; // 如果你用 TextMeshPro

        

        public SkillSystem SkillSystem { set; get; }

        public override void OnStartServer()
        {
            base.OnStartServer();

            // 初始化 MCTS 引擎和决策树
            mctsEngine = new MCTSEngine();
            decisionTree = new SimpleDecisionTree();
            // 找到场景里挂了 AStarPathfinder 的那个对象
            pathfinder = FindObjectOfType<AStarPathfinder>();
            if (pathfinder == null)
            {
                Debug.LogError("[Boss] OnStartServer: 找不到 AStarPathfinder，pathfinder 为 null！");
            }
            else
            {
                Debug.Log($"[Boss] OnStartServer: 成功获取到 AStarPathfinder ({pathfinder.gameObject.name})");
            }
            
            if (healthBarCanvas != null)
            {
                // 确保 worldCamera 指向本地的 Main Camera
                if (Camera.main != null)
                    healthBarCanvas.worldCamera = Camera.main;
                else
                    Debug.LogWarning("[BossEnemy] OnStartClient: 找不到 MainCamera，请检查场景中是否有 Camera 且 Tag 为 MainCamera。");
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
            // 确保 World Space Canvas 的 Camera 在客户端也指向主摄像机
            if (healthBarCanvas != null)
            {
                if (Camera.main != null)
                    healthBarCanvas.worldCamera = Camera.main;
                else
                    Debug.LogWarning("[BossEnemy] OnStartClient: 找不到 MainCamera");

                // 第一次进来先把一次UI画面刷新
                UpdateHealthUI();
            }
        }
        
        // private void LateUpdate()
        // {
        //     UpdateHealthUI();
        //     FaceCanvasToCamera();
        // }

        /// <summary>
        /// 根据 currentHealth / maxHealth 更新血条填充和文字
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
                // 如果 currentHealth、maxHealth 是 float，可以取整或保留一位小数
                int curHpInt = Mathf.CeilToInt(currentHealth);
                int maxHpInt = Mathf.CeilToInt(maxHealth);
                healthBarText.text = $"{curHpInt}/{maxHpInt}";
            }
        }
        
        /// <summary>
        /// 让 World Space Canvas 朝向摄像机正面（可选，如果想让血条始终正对玩家视角）
        /// </summary>
        private void FaceCanvasToCamera()
        {
            if (healthBarCanvas != null && healthBarCanvas.worldCamera != null)
            {
                // 强制让 Canvas 的 forward 朝向摄像机的反方向
                Transform camT = healthBarCanvas.worldCamera.transform;
                // 注：如果 Canvas 的初始 Rotation 是 (90,0,0)，你可能需要微调。下面示例采用 billboard 方式：
                healthBarCanvas.transform.forward = camT.forward * -1f;
                // 或者：
                // healthBarCanvas.transform.rotation = Quaternion.LookRotation(healthBarCanvas.transform.position - camT.position);
            }
        }
        private void LateUpdate()
        {
            //if (!isClient) return;   // 只在客户端执行
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
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Idle", true);
                })
                .OnServerUpdate(() => { animator.SetBool("Idle", true);})
                .OnExit(() => animator.SetBool("Idle", false));

            // Walk
            fsm.State(BossState.Walk)
                .OnEnter(() =>
                {
                    ResetBools();
                    animator.SetBool("Walk", true);
                })
                .OnServerUpdate(() => {animator.SetBool("Walk", true); })
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
                         // 进入 Attack2 状态时，把三次命中标记都重置为“尚未命中”
                             for (int i = 0; i < attack2HasHit.Length; i++)
                                 attack2HasHit[i] = false;
                     })
                 .OnServerUpdate(() =>
                     {
                         // 记录状态持续时间
                             stateTimer += Time.deltaTime;
                
                             // 对于三次命中偏移，每到一个时刻且尚未命中，就调用 PerformAttack2()
                                for (int i = 0; i < attack2HitOffsets.Length; i++)
                             {
                                 if (!attack2HasHit[i] && stateTimer >= attack2HitOffsets[i])
                                 {
                                         attack2HasHit[i] = true;
                                         PerformAttack2();
                                 }
                             }
                
                             // 当总时长超过 attack2Duration，切回 Idle
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
                    hasHit = false;
                   
                })
                .OnServerUpdate(() =>
                {
                    stateTimer += Time.deltaTime;
                    if (!hasHit)
                    {
                        hasHit = true;
                        
                        SkillSystem.PlaySkill(skillId);
                    }
                    if (stateTimer >= 2f)
                    {
                        animator.SetBool("Idle", false);
                        // return to idle
                        idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                        fsm.ChangeState(BossState.Walk);
                    }
                })
                .OnExit(() => animator.SetBool("Idle", false));

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

        private void UpdateAI()
        {
            if (fsm.CurrentStateId == BossState.Hurt || fsm.CurrentStateId == BossState.Die || fsm.CurrentStateId == BossState.Attack1 || 
                fsm.CurrentStateId == BossState.Attack2 || fsm.CurrentStateId == BossState.Skill)
                return;

            // 1. 大范围圆形探测玩家
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, detectLayer);
            Transform nearestPlayer = null;
            float nearestDist = float.MaxValue;
            foreach (var col in hits)
            {
                if (col.CompareTag("Player"))
                {
                    float d = Vector2.Distance(transform.position, col.transform.position);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearestPlayer = col.transform;
                    }
                }
            }

            if (nearestPlayer != null)
            {
                Vector3 playerPos = nearestPlayer.position;
                var face = playerPos.x > transform.position.x ? FaceTo.Right : FaceTo.Left;
                SetFaceTo(face);

                float dist = nearestDist;
                float bossHpPct = currentHealth / (float)maxHealth;
                var playerAttr = nearestPlayer.GetComponentInParent<IChaAttr>();
                float playerHpPct = 0f;
                if (playerAttr != null)
                    playerHpPct = playerAttr.CurrentHealth / (float)playerAttr.MaxHealth;

                // 2. 如果 Skill（大招）可用且未触发 MCTS，则启动 MCTS 决策
                if (Time.time >= nextSkillTime && !isInMCTS)
                {
                    isInMCTS = true;
                    // 构建简化的 GameState
                    MCTSEngine.GameState gs = new MCTSEngine.GameState(
                        bossHpPct, 
                        playerHpPct, 
                        dist,
                        Mathf.Max(0f, nextAttack1Time - Time.time),
                        Mathf.Max(0f, nextAttack2Time - Time.time),
                        Mathf.Max(0f, nextSkillTime - Time.time)
                    );
                    // 运行 MCTS
                    MCTSEngine.ActionType best = mctsEngine.RunMCTS(gs);
                    nextAction = best;
                    isInMCTS = false;
                }
                
                // 3. 如果 MCTS 已经有结果，执行该动作
                if (nextAction != MCTSEngine.ActionType.None)
                {
                    Debug.Log("ExecuteAction: "+nextAction);
                    ExecuteAction(nextAction, nearestPlayer, dist);
                    nextAction = MCTSEngine.ActionType.None;
                    return;
                }
                // if (Time.time >= nextSkillTime)
                // {
                //     Debug.Log("1. exe");
                //     ExecuteAction(MCTSEngine.ActionType.Skill, nearestPlayer, dist);
                //     return;
                // }
                // else
                // {
                //     if (fsm.CurrentStateId != BossState.Walk)
                //         fsm.ChangeState(BossState.Walk);
                //     MoveToward(nearestPlayer.position);
                // }

                // 4. 大招冷却期，使用简单决策树
                SimpleDecisionTree.ActionType simple = decisionTree.Decide(
                    dist, 
                    bossHpPct, 
                    Mathf.Max(0f, nextAttack1Time - Time.time),
                    Mathf.Max(0f, nextAttack2Time - Time.time),
                    Mathf.Max(0f, nextSkillTime - Time.time)
                );
                // 转换为 MCTSEngine.ActionType
                MCTSEngine.ActionType action = ConvertSimpleToMCTS(simple);
                ExecuteAction(action, nearestPlayer, dist);
                return;
            }

            // 5. 巡逻逻辑：没有检测到玩家
            if (fsm.CurrentStateId == BossState.Idle)
            {
                if (Time.time >= idleEndTime)
                {
                    fsm.ChangeState(BossState.Walk);
                }
            }
            else if (fsm.CurrentStateId == BossState.Walk)
            {
                // 随机或停留
                if (Time.time >= idleEndTime)
                {
                    idleEndTime = Time.time + Random.Range(idleTimeMin, idleTimeMax);
                    fsm.ChangeState(BossState.Idle);
                }
            }
        }

        /// <summary>
        /// 将 SimpleDecisionTree.ActionType 转换为 MCTSEngine.ActionType。
        /// </summary>
        private MCTSEngine.ActionType ConvertSimpleToMCTS(SimpleDecisionTree.ActionType simple)
        {
            switch (simple)
            {
                case SimpleDecisionTree.ActionType.Attack1:
                    return MCTSEngine.ActionType.Attack1;
                case SimpleDecisionTree.ActionType.Attack2:
                    return MCTSEngine.ActionType.Attack2;
                case SimpleDecisionTree.ActionType.WalkTowards:
                    return MCTSEngine.ActionType.WalkTowards;
                case SimpleDecisionTree.ActionType.WalkAway:
                    return MCTSEngine.ActionType.WalkAway;
                case SimpleDecisionTree.ActionType.Idle:
                    return MCTSEngine.ActionType.Idle;
                case SimpleDecisionTree.ActionType.Skill:
                    return MCTSEngine.ActionType.Skill;
                default:
                    return MCTSEngine.ActionType.None;
            }
        }

        /// <summary>
        /// 根据 actionType 切换状态并执行移动或攻击。
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
                        MoveToward(player.position);
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
                        MoveToward(player.position);
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
                        MoveToward(player.position);
                    }
                    break;

                case MCTSEngine.ActionType.WalkTowards:
                    if (fsm.CurrentStateId != BossState.Walk)
                        fsm.ChangeState(BossState.Walk);
                    MoveToward(player.position);
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

        /// <summary>
        /// “飞行”向目标点移动：内部先做 A* 寻路，取得一条路径后逐点移动；
        /// 如果路径为空或不可行，退化为直接直线飞过去。
        /// </summary>
        /// <param name="target">玩家或其他目标的世界坐标</param>
        private void MoveToward(Vector3 target)
        {
            Debug.Log($"[Boss] MoveToward 被调用，目标 = {target}");
            if (pathfinder == null)
            {
                // 退化：直线飞
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

                if (currentPath == null)
                    Debug.Log($"[Boss] A* 找不到路径，退化为直飞");
                else
                    Debug.Log($"[Boss] 找到路径，共 {currentPath.Count} 个节点");
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
                // 退化：直线飞
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
                DamageMgr.ProcessDamage(this, defender, this.Attack * 0.6f, false);
            }
        }

        public override void BeHurt(IChaAttr attacker, float damage)
        {
            if (fsm.CurrentStateId == BossState.Die) return;
            base.BeHurt(attacker, damage);
            UpdateHealthUI();

            if (currentHealth <= 0)
                fsm.ChangeState(BossState.Die);
            else if (fsm.CurrentStateId != BossState.Hurt)
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
