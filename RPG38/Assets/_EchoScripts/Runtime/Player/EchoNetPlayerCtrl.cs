using System;
using System.Collections.Generic;
using GameLogic.Runtime.Level;
using JKFrame;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLogic.Runtime
{
    public enum FaceTo
    {
        Left,
        Right,
    }
    [RequireComponent(typeof(NetworkIdentity))]
    public class EchoNetPlayerCtrl : NetBehaviour, IChaAttr
    {
        public Animator aniCtrl;
        public Rigidbody2D rb2D;
        public Collider2D mainBody;
        public NetworkAnimator netAnim;
        public TextMesh headText;
        public Transform viewTf;

        [Header("Check Ground")] 
        public float checkGroundDistance = 0.15F;
        public LayerMask groundMask;
        
        [Header("Attribute")]
        public float jumpForce = 10;
        public float horMoveSpeed = 5;

        public bool IsGround { get; protected set; }
        
        //仅在服务器上有效
        public bool HasInput { get; protected set; }

        public EasyFSM<NetPlayerState> fsm = new();
        public BuffSystem buffSystem;
        public SkillSystem skillSystem;

        public static event Action<IChaAttr> OnDeath;

        /// <summary>
        /// 是否能够控制，由服务器赋予设置权限
        /// </summary>
        [SyncVar(hook = nameof(OnCanControlChange))] public bool canControl = true;

        [SyncVar] public Vector2 axisInput;
        public event Action OnJump;
        
        
        [Header("Health")] 
        public float maxHealth = 100;
        public float currentHealth;
        public int goldNumber;
        public float maxMp=100;
        public float currentMp;
        public int learnSkillId;
        public int learnBuffId;

        public FaceTo faceTo = FaceTo.Right;
        public List<Transform> skillCasterPoints = new();
        public List<int> equips = new();
        
        [Header("New Action Settings")]
        public float dashSpeed = 15f;
        public float dashDuration = 0.2f;
        public float attack1Duration = 0.5f;
        public float hurtDuration = 0.3f;
        public float skillDelay = 0.1f; // 技能状态停留时间
        
        [Header("Attack Settings")]
        public Transform attackPoint;
        public float attackRadius = 0.5f;
        public LayerMask enemyLayer;
        public float  attack1HitTime = 0.2f;  // 判定帧的时刻（秒）

        // 新增事件
        public event Action OnAttack1;
        public event Action OnDash;
        public event Action OnSkill;
        
        [HideInInspector] public bool facingRight = true;
        
        /// <summary>
        /// 根据水平输入翻转角色朝向
        /// </summary>
        public void HandleFlip(float horizontal)
        {
            if (horizontal >  0f)
                ChangeFaceTo(FaceTo.Right);
            else if (horizontal <  0f)
                ChangeFaceTo(FaceTo.Left);
        }

        // private void Flip()
        // {
        //     facingRight = !facingRight;
        //     var scale = transform.localScale;
        //     scale.x *= -1;
        //     transform.localScale = scale;
        // }
        
        // （可选）在 Scene 视窗画出攻击范围帮调试
        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }



#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (maxHealth < 1)
                maxHealth = 1;

            if (currentHealth <= 0)
                currentHealth = maxHealth;
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;
        }
#endif


        #region UnityCallBack

        private void Start()
        {
            //处理状态机逻辑
            fsm.AddState(NetPlayerState.Idle, new NetIdleState(fsm, this));
            fsm.AddState(NetPlayerState.Move, new NetMoveState(fsm, this));
            fsm.AddState(NetPlayerState.Die,new NetDieState(fsm, this));
            
            fsm.AddState(NetPlayerState.Jump, new NetJumpState(fsm, this));
            fsm.AddState(NetPlayerState.Fall, new NetFallState(fsm, this));
            fsm.AddState(NetPlayerState.Attack1, new NetAttack1State(fsm, this));
            fsm.AddState(NetPlayerState.Dash, new NetDashState(fsm, this));
            fsm.AddState(NetPlayerState.Hurt, new NetHurtState(fsm, this));
            fsm.AddState(NetPlayerState.Skill,   new NetSkillState(fsm, this));
            
            if (isServer)
            {
                OnJump += ()  => fsm.ChangeState(NetPlayerState.Jump);
                OnAttack1 += () => fsm.ChangeState(NetPlayerState.Attack1);
                OnDash += ()    => fsm.ChangeState(NetPlayerState.Dash);
                OnSkill   += () => fsm.ChangeState(NetPlayerState.Skill);
            }
            //不管服务端还是客户端，都先设置一下初始状态
            fsm.StartState(NetPlayerState.Idle);
            
            //让玩家持久存在，处理起来更方便
            DontDestroyOnLoad(gameObject);

            EchoNetPlayerCtrl.OnDeath += OnSelfDeath;
        }

        private void OnDestroy()
        {
            // 避免内存泄露
            OnJump     = null;
            OnAttack1  = null;
            OnDash     = null;
            OnSkill = null;
            EchoNetPlayerCtrl.OnDeath -= OnSelfDeath;
        }

        private void OnSelfDeath(IChaAttr chaAttr)
        {
            Debug.Log("self death");
        }

        protected override void Update()
        {
            if (canControl == false)
                return;
            
            base.Update();
        }

        private void FixedUpdate()
        {
            CheckGround();
        }

        #endregion

        #region Client

        // client 只同步动画表现和输入转发到服务器，不做任何具体逻辑
        protected override void OnClientUpdate()
        {
            if (fsm.CurrentStateId == NetPlayerState.Hurt || fsm.CurrentStateId == NetPlayerState.Die) return;
            //这里更好的做法 是建立一个操作缓冲池，而不是用变量，操作缓冲池可以避免漏掉操作
            var localInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            CmdSubmitInput(localInput);

            if (Input.GetKeyDown(KeyCode.Space))
                CmdJumpInput();

            if (Input.GetKeyDown(KeyCode.J))
                CmdAttack1Input();

            if (Input.GetKeyDown(KeyCode.LeftShift) && Mathf.Abs(localInput.x) > 0.1f)
                CmdDashInput();

            if (Input.GetKeyDown(KeyCode.K))
                CmdSkillInput();
        }
        
        [TargetRpc]
        private void TargetRpcRefreshUI(NetworkConnection conn,float currentHealth,float maxHealth,float currentMp,float maxMp,int goldNumber)
        {
            var window = UISystem.GetWindow<BattleUI>();
            if(window ==null)
                return;
            
            StringEventSystem.Global.Send(EventKey.GoldNumberChanged,this.goldNumber,goldNumber);
            
            this.currentHealth = currentHealth;
            this.maxHealth = maxHealth;
            this.currentMp = currentMp;
            this.maxMp = maxMp;
            this.goldNumber = goldNumber;
            window.RefreshUI(currentHealth,maxHealth,currentMp,maxMp,goldNumber);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (this.isOwned)
            {
                CmdReqUI();

                var achievementData = GameHub.Interface.GetModel<AchievementModel>().GetAchievement();
                CmdReqAchievementAddition(achievementData.ids);
            }
               
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            //Object.FindObjectOfType<CameraFollow>().target = transform;
            var camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (camFollow != null)
                camFollow.target = transform;

            UISystem.Show<BattleUI>();
            CmdReqShopItem();
        }

        [TargetRpc]
        public void TargetRpcInjectShopItemData(NetworkConnection conn)
        {
            var levelMgr = Object.FindObjectOfType<EchoLevelMgr>();
            levelMgr.InjectShopItemData();
        }

        #endregion

        #region Server

        protected override void OnServerUpdate()
        {
            if (fsm != null)
            {
                fsm.ServerUpdate();
            }
            
            buffSystem.Update(Time.deltaTime);
            skillSystem.UpdateCd(Time.deltaTime);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            this.buffSystem = new BuffSystem(this.gameObject);
            this.skillSystem = new SkillSystem(this.gameObject);
            this.skillSystem.LearnSkill(learnSkillId);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            this.buffSystem.ClearAllBuffs();
        }

        [Command]
        private void CmdReqUI()
        {
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber);
        }

        //因为成就存档都保存在各自的本地，而不是在服务器上，所以只能自己去请求服务器，让服务器去进行加成
        [Command]
        private void CmdReqAchievementAddition(List<int> achievementIds)
        {
            //获取主角
            var netPlayerCtrl = this.connectionToClient.identity.GetComponent<EchoNetPlayerCtrl>();
            foreach (var achievementId in achievementIds)
            {
                var dataSo = ResSystem.LoadAsset<AchievementDataSo>($"Assets/_EchoAddressable/DataSo/AchievementDataSo_{achievementId}.asset");
                netPlayerCtrl.buffSystem.AddBuff(dataSo.buffId);
            }
        }
        
        [Command]
        private void CmdReqShopItem()
        {
            TargetRpcInjectShopItemData(this.connectionToClient);
        }

        [Command]
        private void CmdSubmitInput(Vector2 input)
        {
            this.axisInput = input;
            this.HasInput = axisInput.magnitude > 0.1F;
        }

        [Command]
        private void CmdAttack1Input() => OnAttack1?.Invoke();

        [Command]
        private void CmdDashInput()    => OnDash?.Invoke();

        // 改为触发事件，由 SkillState 处理播放
        [Command]
        private void CmdSkillInput()   => OnSkill?.Invoke();

        [Command]
        private void CmdJumpInput()    => OnJump?.Invoke();


        
        [Command]
        public void CmdBuyShopItem(int id)
        {
            var dataSo = ResSystem.LoadAsset<ShopItemDataSo>($"Assets/_EchoAddressable/DataSo/ShopItemDataSo_{id}.asset");
            this.buffSystem.AddBuff(dataSo.buffId);
            this.AddGold(-dataSo.goldCost);
        }

        [Command]
        public void CmdAddGold(int amount)
        {
            this.AddGold(amount);
        }

        [Command]
        public void AddReward(int id)
        {
            var rewardDataSo = ResSystem.LoadAsset<RewardDataSo>($"Assets/_EchoAddressable/DataSo/Reward DataSo_{id}.asset");
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/_EchoAddressable/DataSo/ItemDataSo_{rewardDataSo.itemID}.asset");
            if (itemDataSo.itemType == ItemType.Equipment)
            {
                this.AddEquip(itemDataSo.itemID);
            }
        }

        #endregion

        #region Common

        public void AddEquip(int itemId)
        {
            if(this.equips.Contains(itemId) == false)
                return;
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/_EchoAddressable/DataSo/ItemDataSo_{itemId}.asset");
            this.equips.Add(itemId);
            this.buffSystem.AddBuff(itemDataSo.buffId);
        }

        public void RemoveEquip(int itemId)
        {
            if(this.equips.Contains(itemId) == false)
                return;
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/_EchoAddressable/DataSo/ItemDataSo_{itemId}.asset");
            this.equips.Remove(itemId);
            this.buffSystem.RemoveBuff(itemDataSo.buffId);
        }

        public void PlayAni(string aniName)
        {
            this.aniCtrl.Play(aniName);
        }

        /// <summary>
        /// Trigger需要主动net广播
        /// </summary>
        /// <param name="aniName"></param>
        public void AniSetTrigger(string aniName)
        {
            this.aniCtrl.SetTrigger(aniName);
            this.netAnim.SetTrigger(aniName);
        }

        public void SetPosition(Vector2 position)
        {
            this.transform.position = position;
        }
        

        public void BeHurt(IChaAttr attacker, float damage)
        {
            Debug.Log($"BeHurt called: dmg={damage}, curHP={currentHealth} before, state={fsm.CurrentStateId}");
            this.currentHealth -= damage;
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber);
            if (this.currentHealth <= 0)
            {
                //持有复生buff 进行使用
                if (this.buffSystem.HasBuff(1003))
                {
                    this.Rebirth();
                    this.buffSystem.RemoveBuff(1003);
                    return;
                }
                fsm.ChangeState(NetPlayerState.Die);
                OnDeath?.Invoke(this);
                return;
            }

            if (fsm.CurrentStateId != NetPlayerState.Hurt)
                fsm.ChangeState(NetPlayerState.Hurt);
        }

        public void AddMaxHealth(float amount)
        {
            this.maxHealth += amount;
        }

        public void AddMaxMp(float amount)
        {
            this.maxMp += amount;
        }

        public void AddAttack(float amount)
        {
            this.attack += amount;
        }

        //设置面朝向，由于在network transform设置了syncscale 所以不用发布rpc
        public void ChangeFaceTo(FaceTo face)
        {
            //只转View视图 其他不进行旋转
            this.viewTf.localScale = face == FaceTo.Left ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
            this.faceTo = face;
        }

        private void CheckGround()
        {
            IsGround = Physics2D.Raycast(this.transform.position, Vector2.down, checkGroundDistance, groundMask);
        }

        public void AddHealth(float amount)
        {
            this.currentHealth += amount;
            this.currentHealth = Mathf.Clamp(this.currentHealth, 0, maxHealth);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber);
        }

        public void AddMp(float amount)
        {
            this.currentMp += amount;
            this.currentMp = Mathf.Clamp(this.currentMp, 0, maxMp);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber);
        }

        public void AddGold(int amount)
        {
            this.goldNumber += amount;
            this.goldNumber = Mathf.Clamp(this.goldNumber, 0, 999999);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber);
        }

        public void Rebirth()
        {
            this.AddHealth(1000);
            this.AddMp(1000);
            this.canControl = true;
        }

        #endregion
        
        private void OnCanControlChange(bool oldValue, bool newValue)
        {
            if (newValue == false)
            {
                //权限禁止，发送最后一次默认的输入，还原状态
                CmdSubmitInput(Vector2.zero);
            }
        }
        public string Tag => "Player";

        public float MaxHealth
        {
            get => maxHealth;
            set => maxHealth = value;
        }

        public float CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = value;
        }

        public float MaxMp { get=>maxMp; set=>maxMp = value; }
        public float attack = 5;
        public float Attack { get=>attack; set=>attack=value; }
        public bool IsDead => this.currentHealth <= 0;
        public float CurrentMp {get=>currentMp; set=>currentMp = value; }

        public void ResetBools()
        {
            this.aniCtrl.SetBool("Idle", false);
            this.aniCtrl.SetBool("Move", false);
            this.aniCtrl.SetBool("Attack1", false);
            this.aniCtrl.SetBool("GetHit", false);
            this.aniCtrl.SetBool("Dead", false);
            this.aniCtrl.SetBool("Jump", false);
            this.aniCtrl.SetBool("Fall", false);
            this.aniCtrl.SetBool("Dash", false);
            
        }
    }
}