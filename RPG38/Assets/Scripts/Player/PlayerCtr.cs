using System;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(NetworkTransformUnreliable))]
public class PlayerCtr : NetworkBehaviour
{
    //—— Components —————————————————————————————————————————
    private Rigidbody2D rb;
    private Animator anim;

    [Header("UI")] [SerializeField] private TextMesh stateText;

    //—— Config ————————————————————————————————————————————
    [Header("Move info")] [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [Header("Dash info")] [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashSpeed = 25f;

    [Header("Ground Check")] [SerializeField]
    private Transform groundCheck;

    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask whatIsGround;
    [Header("Attack Settings")] public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;
    public int attackDamage = 10;

    [Header("Combo Attack Duration")] [SerializeField]
    private float attack1Duration = 0.3f;

    [SerializeField] private float attack2Duration = 0.4f;
    [SerializeField] private float attack3Duration = 0.5f;

    [Header("Extra Jumps")] [SerializeField]
    private int extraJumpCount = 1;

    public static event Action<PlayerCtr> OnPlayerDeathGlobal;

    private enum State
    {
        Idle,
        Move,
        Jump,
        Fall,
        Dash,
        Attack,
        GetHit,
        Dead
    }

    //—— SyncVars ————————————————————————————————————————
    [SyncVar(hook = nameof(OnServerStateChanged))]
    private State currentState = State.Idle;

    [SyncVar] private int extraJumps;
    [SyncVar] private float dashTimer;
    [SyncVar] private float stateTimer;
    [SyncVar] private float dashDir;
    [SyncVar] private int comboCounter;
    [SyncVar] private float lastAttackTime;

    [SyncVar(hook = nameof(OnFacingChanged))]
    private int facingDir = 1;

    [SyncVar] private bool triggerAttack;
    [SyncVar] private bool triggerHit;

    private const float comboWindow = 2f;

    //—— Cached client → server inputs ——————————————————————
    private float inputH;
    private bool inputJump;
    private bool inputAttack;
    private bool inputDash;

    public override void OnStartServer()
    {
        extraJumps = extraJumpCount;
        dashTimer = 0f;
        stateTimer = 0f;
        comboCounter = 0;
        lastAttackTime = 0f;
        triggerAttack = triggerHit = false;
        currentState = State.Idle;
        facingDir = 1;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isOwned)
        {
            inputH = Input.GetAxisRaw("Horizontal");
            inputJump = Input.GetKeyDown(KeyCode.Space);
            inputAttack = Input.GetKeyDown(KeyCode.Mouse0);
            inputDash = Input.GetKeyDown(KeyCode.LeftShift);
            CmdSetInput(inputH, inputJump, inputAttack, inputDash);
        }

        // if (isServer || isLocalPlayer)
        //     ServerStateMachine();

        if (this.isServer)
        {
            ServerStateMachine();
        }
    }

    [Command]
    private void CmdSetInput(float h, bool j, bool a, bool d)
    {
        inputH = h;
        inputJump = j;
        inputAttack = a;
        inputDash = d;
    }

    private void ServerStateMachine()
    {
        if (Mathf.Abs(inputH) > 0.1f && Mathf.Sign(inputH) != facingDir)
            facingDir = (int)Mathf.Sign(inputH);

        dashTimer -= Time.deltaTime;
        if (currentState != State.Dead && currentState != State.GetHit && inputDash && dashTimer <= 0f)
        {
            dashTimer = dashCooldown;
            dashDir = Mathf.Abs(inputH) > 0.1f ? inputH : facingDir;
            ChangeServerFsmState(State.Dash);
            return;
        }

        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Move: UpdateMove(); break;
            case State.Jump: UpdateJump(); break;
            case State.Fall: UpdateFall(); break;
            case State.Dash: UpdateDash(); break;
            case State.Attack: UpdateAttack(); break;
            case State.GetHit: UpdateGetHit(); break;
            case State.Dead: break;
        }
    }

    private void UpdateIdle()
    {
        if (inputJump && IsGround())
        {
            ChangeServerFsmState(State.Jump);
            return;
        }

        if (inputAttack)
        {
            ChangeServerFsmState(State.Attack);
            return;
        }

        if (Mathf.Abs(inputH) > 0.1f)
        {
            ChangeServerFsmState(State.Move);
            return;
        }
    }

    private void UpdateMove()
    {
        if (inputAttack)
        {
            ChangeServerFsmState(State.Attack);
            return;
        }

        rb.velocity = new Vector2(inputH * moveSpeed, rb.velocity.y);
        if (Mathf.Abs(inputH) < 0.1f)
        {
            ChangeServerFsmState(State.Idle);
            return;
        }

        if (inputJump && IsGround())
        {
            ChangeServerFsmState(State.Jump);
            return;
        }
    }

    private void UpdateJump()
    {
        if (rb.velocity.y < 0f)
            ChangeServerFsmState(State.Fall);
    }

    private void UpdateFall()
    {
        if (inputJump && extraJumps > 0)
        {
            extraJumps--;
            ChangeServerFsmState(State.Jump);
            return;
        }

        if (IsGround())
        {
            rb.velocity = Vector2.zero;
            ChangeServerFsmState(State.Idle);
            return;
        }

        if (Mathf.Abs(inputH) > 0.1f)
            rb.velocity = new Vector2(inputH * moveSpeed * 0.8f, rb.velocity.y);
    }

    private void UpdateDash()
    {
        stateTimer -= Time.deltaTime;
        rb.velocity = new Vector2(dashSpeed * dashDir, 0f);
        if (stateTimer <= 0f)
            ChangeServerFsmState(IsGround() ? State.Idle : State.Fall);
    }

    private void UpdateAttack()
    {
        rb.velocity = new Vector2(inputH * moveSpeed, rb.velocity.y);
        stateTimer -= Time.deltaTime;
        if (triggerAttack || stateTimer <= 0f)
        {
            ChangeServerFsmState(IsGround() ? State.Idle : State.Fall);
            return;
        }
    }

    private void UpdateGetHit()
    {
        stateTimer -= Time.deltaTime;
        if (triggerHit || stateTimer <= 0f)
        {
            ChangeServerFsmState(State.Idle);
            return;
        }
    }

    private void ChangeServerFsmState(State next)
    {
        ExitServerFsmState(currentState);
        currentState = next;
        EnterServerFsmState(next);
    }

    private void EnterServerFsmState(State s)
    {
        triggerAttack = triggerHit = false;
        switch (s)
        {
            case State.Idle:
                extraJumps = extraJumpCount;
                rb.velocity = Vector2.zero;
                break;
            case State.Jump:
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                break;
            case State.Fall:
                // no init
                break;
            case State.Dash:
                stateTimer = dashDuration;
                break;
            case State.Attack:
                float dur = comboCounter == 0 ? attack1Duration
                    : comboCounter == 1 ? attack2Duration
                    : attack3Duration;
                stateTimer = dur;
                if (comboCounter > 2 || Time.time >= lastAttackTime + comboWindow)
                    comboCounter = 0;
                lastAttackTime = Time.time;
                break;
            case State.GetHit:
                stateTimer = 0.3f;
                break;
        }
    }

    private void ExitServerFsmState(State s)
    {
        if (s == State.Attack)
            comboCounter++;
    }

    private void OnServerStateChanged(State oldS, State newS)
    {
        ClientFsmStateExit(oldS);
        ClientFsmEnter(newS);
        if (stateText)
            stateText.text = newS.ToString();
    }

    private void ClientFsmEnter(State s)
    {
        switch (s)
        {
            case State.Idle: anim.SetBool("Idle", true); break;
            case State.Move: anim.SetBool("Move", true); break;
            case State.Jump: anim.SetBool("Jump", true); break;
            case State.Fall: anim.SetBool("Fall", true); break;
            case State.Dash: anim.SetBool("Dash", true); break;
            case State.Attack:
                anim.SetBool("Attack", true);
                anim.SetInteger("ComboCounter", comboCounter);
                break;
            case State.GetHit: anim.SetBool("GetHit", true); break;
            case State.Dead: anim.SetBool("Dead", true); break;
        }
    }

    private void ClientFsmStateExit(State s)
    {
        switch (s)
        {
            case State.Idle: anim.SetBool("Idle", false); break;
            case State.Move: anim.SetBool("Move", false); break;
            case State.Jump: anim.SetBool("Jump", false); break;
            case State.Fall: anim.SetBool("Fall", false); break;
            case State.Dash: anim.SetBool("Dash", false); break;
            case State.Attack: anim.SetBool("Attack", false); break;
            case State.GetHit: anim.SetBool("GetHit", false); break;
            case State.Dead: anim.SetBool("Dead", false); break;
        }
    }

    private void OnFacingChanged(int oldDir, int newDir)
    {
        transform.localScale = new Vector3(newDir, 1, 1);
    }

    private bool IsGround() =>
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround) != null;

    public void PerformAttack()
    {
        if (!isServer) return;
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (var c in hits)
            c.GetComponent<Health>()?.TakeDamage(attackDamage);
    }

    [Command]
    public void CmdAttackComplete() => triggerAttack = true;

    [Command]
    public void CmdHitComplete() => triggerHit = true;

    public void OnDeath()
    {
        ChangeServerFsmState(State.Dead);
        OnPlayerDeathGlobal?.Invoke(this);
    }
}