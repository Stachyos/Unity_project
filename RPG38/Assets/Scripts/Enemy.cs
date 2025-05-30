// Enemy.cs

using System;
using UnityEngine;
using Mirror;

public class Enemy : NetworkBehaviour
{
    
    /// <summary>
    /// 敌人死亡时，全局广播此事件
    /// </summary>
    public static event Action<Enemy> OnEnemyDeathGlobal;
    //—— Components ——————————————————————————
    private Rigidbody2D rb;
    private Animator     anim;

    //—— AI Config ————————————————————————————
    [Header("AI Config")]
    [SerializeField] private Transform player;
    [SerializeField] private float     detectionRange = 10f;
    [SerializeField] private float     attackRange    = 1.5f;

    //—— Attack Settings —————————————————————————
    [Header("Attack Settings")]
    public Transform attackPoint;          // ← 在 Inspector 拖一个子物体到想要的命中中心
    [SerializeField] private float    attackCooldown = 2f;
    [SerializeField] private LayerMask playerLayer;   // 勾选 Player 所在 Layer
    [SerializeField] private int      attackDamage   = 10;
    private float                    lastAttackTime = -Mathf.Infinity;

    //—— Move Settings ——————————————————————————
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;
    
    [Header("Flip Settings")]
    [SerializeField] private float flipInterval = 2f;

    //—— State & Trigger —————————————————————————
    private enum State { Idle, Walk, Jump, Attack, GetHit, Dead }
    private State  currentState;
    private bool   triggerCalled;

    //—— Facing ——————————————————————————————
    private bool facingRight = true;
    
    
    public void SetTarget(Transform t) => player = t;

    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    private void Start()
    {
        ChangeState(State.Idle);
        // 开始每隔 flipInterval 秒调用一次 ToggleFacingDirection
        InvokeRepeating(nameof(FacePlayer), flipInterval, flipInterval);
    }
    

    private void Update()
    {
         // FacePlayer();
        switch (currentState)
        {
            case State.Idle:    UpdateIdle();    break;
            case State.Walk:    UpdateWalk();    break;
            case State.Jump:    UpdateJump();    break;
            case State.Attack:  UpdateAttack();  break;
            case State.GetHit:  UpdateGetHit();  break;
            case State.Dead:    UpdateDead();    break;
        }
    }

    // 由动画事件调用，标记动画播完
    public void AnimationTrigger() => triggerCalled = true;

    //—— PerformAttack: 基于 OverlapCircleAll 的命中判定 ——
    public void PerformAttack()
    {
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,   // ← 用 attackPoint 而不是 transform.position
            attackRange,
            playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var hp = hit.GetComponent<Health>();
                if (hp != null)
                    hp.TakeDamage(attackDamage);
            }
        }
    }

    //—— State Machine 核心 ——————————————————————
    private void ChangeState(State next)
    {
        ExitState(currentState);
        currentState = next;
        EnterState(next);
    }

    private void EnterState(State s)
    {
        triggerCalled = false;
        switch (s)
        {
            case State.Idle:
                anim.SetBool("Idle", true);
                rb.velocity = Vector2.zero;
                break;
            case State.Walk:
                anim.SetBool("Walk", true);
                break;
            case State.Jump:
                anim.SetBool("Jump", true);
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                break;
            case State.Attack:
                anim.SetBool("Attack", true);
                rb.velocity = Vector2.zero;
                break;
            case State.GetHit:
                anim.SetBool("GetHit", true);
                rb.velocity = Vector2.zero;
                break;
            case State.Dead:
                anim.SetBool("Dead", true);
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void ExitState(State s)
    {
        switch (s)
        {
            case State.Idle:   anim.SetBool("Idle", false);   break;
            case State.Walk:   anim.SetBool("Walk", false);   break;
            case State.Jump:   anim.SetBool("Jump", false);   break;
            case State.Attack: anim.SetBool("Attack", false); break;
            case State.GetHit: anim.SetBool("GetHit", false); break;
            case State.Dead:   anim.SetBool("Dead", false);   break;
        }
    }

    //—— 各状态逻辑 —————————————————————————————————
    private void UpdateIdle()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
            ChangeState(State.Walk);
    }

    private void UpdateWalk()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ChangeState(State.Attack);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void UpdateJump()
    {
        if (triggerCalled)
            ChangeState(State.Idle);
    }

    private void UpdateAttack()
    {
        if (triggerCalled)
        {
            PerformAttack();
            ChangeState(State.Idle);
        }
    }

    private void UpdateGetHit()
    {
        if (triggerCalled)
            ChangeState(State.Idle);
    }

    private void UpdateDead() { /* 不再切换 */ }

    //—— Facing / Flip ——————————————————————————
    private void FacePlayer()
    {
        if (player == null || currentState == State.Dead) return;
        bool shouldFaceRight = player.position.x < transform.position.x;
        if (shouldFaceRight != facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    //—— 受击 / 死亡 回调 —————————————————————————————————
    public void OnGetHit()
    {
        if (currentState != State.Dead)
            ChangeState(State.GetHit);
    }

    public void OnDeath()
    {
        // 切入死亡状态
        ChangeState(State.Dead);
        // 广播给战斗管理器：有一个敌人死亡了
        OnEnemyDeathGlobal?.Invoke(this);
    }

    //—— 可视化攻击范围 —————————————————————————————————
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
