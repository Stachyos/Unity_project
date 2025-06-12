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
        [Header("Character_ID")] 
        public int characterID;
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
        
        public bool HasInput { get; protected set; }

        public EasyFSM<NetPlayerState> fsm = new();
        public BuffSystem buffSystem;
        public SkillSystem skillSystem;

        public static event Action<IChaAttr> OnDeath;

        /// <summary>
        /// Is it possible to control and grant setting permissions by the server?
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
        public float skillDelay = 0.1f; 
        
        [Header("Attack Settings")]
        public Transform attackPoint;
        public float attackRadius = 0.5f;
        public LayerMask enemyLayer;
        public float  attack1HitTime = 0.2f;  
        
        [Header("Double Jump Settings")]
// Maximum number of jumps (the number of jumps allowed before landing, 1: single jump, 2: two-jump)
        [SerializeField] private int maxJumpCount = 2;
// Current number of jumps
        [SyncVar] private int jumpCount = 0;

        [Header("Dash Cooldown Settings")]
// Sprint cooling duration (seconds)
        [SerializeField] private float dashCooldown = 1f;
// Record the time point when the last sprint was triggered
        [SyncVar] private float lastDashTime = -Mathf.Infinity;

        // 新增事件
        public event Action OnAttack1;
        public event Action OnDash;
        public event Action OnSkill;
        
        [HideInInspector] public bool facingRight = true;
        

        private bool _canUpdate = true; 
        
        /// <summary>
        /// Change the orientation of the character based on the input level.
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
        
        // Draw the attack range in the Scene window for debugging purposes
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
            //Processing state machine logic
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
            //Whether on the server side or the client side, the initial state should be set first.
            fsm.StartState(NetPlayerState.Idle);
            
            //Enable players to persistently exist and make it easier to handle the situation.
            DontDestroyOnLoad(gameObject);

            EchoNetPlayerCtrl.OnDeath += OnSelfDeath;
        }

        private void OnDestroy()
        {
            // Avoid memory leaks
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

        // The client only synchronizes the animation display and the input forwarding to the server, without performing any specific logic.
        protected override void OnClientUpdate()
        {
            if (fsm.CurrentStateId == NetPlayerState.Hurt || fsm.CurrentStateId == NetPlayerState.Die) return;
            //The better approach here is to establish an operation buffer pool instead of using variables. The operation buffer pool can prevent any omissions in the operations.
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
        private void TargetRpcRefreshUI(NetworkConnection conn,float currentHealth,float maxHealth,float currentMp,float maxMp,int goldNumber,float attack, float speed)
        {
            var window = UISystem.GetWindow<BattleUI>();
            if(window ==null)
                return;
            
            StringEventSystem.Global.Send(EventKey.GoldNumberChanged,this.goldNumber,goldNumber);
            StringEventSystem.Global.Send(EventKey.HpMaxNumberChanged,this.maxHealth,maxHealth);
            
            this.currentHealth = currentHealth;
            this.maxHealth = maxHealth;
            this.currentMp = currentMp;
            this.maxMp = maxMp;
            this.goldNumber = goldNumber;
            window.RefreshUI(currentHealth,maxHealth,currentMp,maxMp,goldNumber,attack,speed);
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
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        //Because the achievement records are all stored locally rather than on the server, you have to request the server yourself and let the server perform the bonus addition.
        [Command]
        private void CmdReqAchievementAddition(List<int> achievementIds)
        {
            var netPlayerCtrl = this.connectionToClient.identity.GetComponent<EchoNetPlayerCtrl>();
            foreach (var achievementId in achievementIds)
            {
                var dataSo = ResSystem.LoadAsset<AchievementDataSo>($"Assets/Addressable/DataSo/AchievementDataSo_{achievementId}.asset");
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
        private void CmdDashInput()
        {
            float now = Time.time;
            // If the current time has exceeded the last sprint time plus the cooldown duration, then the sprint is allowed.
            if (now - lastDashTime >= dashCooldown)
            {
                lastDashTime = now;
                OnDash?.Invoke();
            }

        }

        // Change to trigger event, and let SkillState handle the playback
        [Command]
        private void CmdSkillInput()   => OnSkill?.Invoke();

        [Command]
        private void CmdJumpInput()
        {
            // Perform the jump judgment on the server side
            if (IsGround || jumpCount < maxJumpCount)
            {
                jumpCount++;
                OnJump?.Invoke();
            }

        }


        
        [Command]
        public void CmdBuyShopItem(int id, int goldCost)
        {
            var dataSo = ResSystem.LoadAsset<ShopItemDataSo>($"Assets/Addressable/DataSo/ShopItemDataSo_{id}.asset");
            this.buffSystem.AddBuff(dataSo.buffId);
            this.AddGold(-goldCost);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
            
        }

        [Command]
        public void CmdAddGold(int amount)
        {
            this.AddGold(amount);
        }

        [Command]
        public void AddReward(int id)
        {
            var rewardDataSo = ResSystem.LoadAsset<RewardDataSo>($"Assets/Addressable/DataSo/Reward DataSo_{id}.asset");
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/Addressable/DataSo/ItemDataSo_{rewardDataSo.itemID}.asset");
            if (itemDataSo.itemType == ItemType.Equipment)
            {
                this.AddEquip(itemDataSo.itemID);
                TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
            }
            
        }

        #endregion

        #region Common

        public void AddEquip(int itemId)
        {
            // if(this.equips.Contains(itemId) == false)
            //     return;
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/Addressable/DataSo/ItemDataSo_{itemId}.asset");
            this.equips.Add(itemId);
            this.buffSystem.AddBuff(itemDataSo.buffId);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void RemoveEquip(int itemId)
        {
            if(this.equips.Contains(itemId) == false)
                return;
            var itemDataSo = ResSystem.LoadAsset<ItemDataSo>($"Assets/Addressable/DataSo/ItemDataSo_{itemId}.asset");
            this.equips.Remove(itemId);
            this.buffSystem.RemoveBuff(itemDataSo.buffId);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void PlayAni(string aniName)
        {
            this.aniCtrl.Play(aniName);
        }

        /// <summary>
        /// Trigger requires an active net broadcast.
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
        

     
        [Server]
        public void PlayAttackSoundOnAll(int characterIndex)
        {
           
            AudioManager.Instance.PlayLocalAttack(characterIndex);

            
            var msg = new PlaySoundMessage {
                eventType = SoundEvent.Attack,
                characterIndex = characterIndex
            };
            NetworkServer.SendToAll(msg);
        }
        
        [Server]
        public void PlayHitSoundOnAll(int characterIndex)
        {
           
            AudioManager.Instance.PlayLocalHit(characterIndex);

            
            var msg = new PlaySoundMessage {
                eventType = SoundEvent.Hit,
                characterIndex = characterIndex
            };
            NetworkServer.SendToAll(msg);
        }

        [Server]
        public void PlaySkillSoundOnAll()
        {
            AudioManager.Instance.PlayLocalSkill();
            NetworkServer.SendToAll(new PlaySoundMessage {
                eventType = SoundEvent.Skill
            });
        }

        
        public bool _hasDied;
        private bool _isProcessingHurt = false;
        private float _nextHurtTime = 0f;    
        private const float HurtInvincibleDuration = 0.2f;  
        public void BeHurt(IChaAttr attacker, float damage)
        {
            
            if (Time.time < _nextHurtTime ||_isProcessingHurt || _hasDied || fsm.CurrentStateId == NetPlayerState.Die)
                return;
            _isProcessingHurt = true;
            _nextHurtTime = Time.time + HurtInvincibleDuration;
            //this.currentHealth -= damage;
            this.AddHealth(-damage);
            
            PlayHitSoundOnAll(characterID);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
            if (this.currentHealth <= 0)
            {
                //Use with the rebirth buff active
                if (this.buffSystem.HasBuff(1003))
                {
                    this.Rebirth();
                    this.buffSystem.RemoveBuff(1003);
                    TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
                    _isProcessingHurt = false;
                    return;
                }
                TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
                _hasDied = true;   
                fsm.ChangeState(NetPlayerState.Die);
                OnDeath?.Invoke(this);
                _isProcessingHurt = false;
                return;
            }

            if (fsm.CurrentStateId != NetPlayerState.Hurt)
                fsm.ChangeState(NetPlayerState.Hurt);
            _isProcessingHurt = false;
        }
        
  

        public void AddMaxHealth(float amount)
        {
            this.maxHealth += amount;
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void AddMaxMp(float amount)
        {
            this.maxMp += amount;
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void AddAttack(float amount)
        {
            this.attack += amount;
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        //Set the facing direction. Since syncscale has been set in the network transform, there is no need to publish the RPC.
        public void ChangeFaceTo(FaceTo face)
        {
            //Only rotate the view. Do not rotate anything else.
            this.viewTf.localScale = face == FaceTo.Left ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
            this.faceTo = face;
        }

        private void CheckGround()
        {
            bool wasGround = IsGround;
            IsGround = Physics2D.Raycast(this.transform.position, Vector2.down, checkGroundDistance, groundMask);

            // If just landed from the air (was not on the ground before, but is now on the ground)
            if (!wasGround && IsGround)
            {
                jumpCount = 0; // Reset the jump count
            }
        }

        public void AddHealth(float amount)
        {
            this.currentHealth += amount;
            this.currentHealth = Mathf.Clamp(this.currentHealth, 0, maxHealth);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void AddMp(float amount)
        {
            this.currentMp += amount;
            this.currentMp = Mathf.Clamp(this.currentMp, 0, maxMp);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void AddGold(int amount)
        {
            this.goldNumber += amount;
            this.goldNumber = Mathf.Clamp(this.goldNumber, 0, 999999);
            TargetRpcRefreshUI(this.connectionToClient,this.currentHealth,this.maxHealth,this.currentMp,this.maxMp,this.goldNumber, Attack, horMoveSpeed);
        }

        public void Rebirth()
        {
            this.AddHealth(10000);
            this.AddMp(10000);
            this.canControl = true;
        }

        #endregion
        
        private void OnCanControlChange(bool oldValue, bool newValue)
        {
            if (newValue == false)
            {
                //Permission denied. Sending the last default input. Restoring the state.
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
        public float attack = 20;
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