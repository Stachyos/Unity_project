using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>

    [RequireComponent(typeof(NetworkIdentity))]
    public class Enemy3 : Enemy, ISkillAbility
    {
        private enum State { IdleSkill, Die }

        public override string Tag => "Enemy3";

        //EasyFSM is a Tool in QFramework, I use it with a reference in Readme
        private EasyFSM<State> fsm;

        [SyncVar(hook = nameof(OnFaceChanged))]
        public FaceTo faceTo;
        public Transform view;

        [Header("Detection")]
        public Transform leftCheckPoint;
        public Transform rightCheckPoint;
        public float detectDistance = 5f;
        public LayerMask detectLayer;

        [Header("Skill")] 
        public int skillId;
        public float skillInterval = 2f;
        private float nextSkillTime;
        public Transform skillCasterPoint;

        [Header("Flicker on Hurt")]
        public SpriteRenderer[] renderers;
        public float flickerDuration = 0.5f;
        public float flickerSpeed = 10f;

        [Header("Fade on Death")]
        public float deathFadeDuration = 1f;

        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SkillSystem = new SkillSystem(gameObject);

            SkillSystem.LearnSkill(skillId);
            InitFSM();
            fsm.StartState(State.IdleSkill);
        }

        protected override void OnServerUpdate()
        {
            base.OnServerUpdate();
            fsm.ServerUpdate();
        }

        private void InitFSM()
        {
            fsm = new EasyFSM<State>();

           
            fsm.State(State.IdleSkill)
                .OnEnter(() =>
                {
                    animator.SetBool("Idle", true);
                })
                .OnServerUpdate(() =>
                {
                  
                    bool playerDetected = false;
                    Vector3 targetPos = Vector3.zero;

                    var hitsL = Physics2D.RaycastAll(leftCheckPoint.position, Vector2.left, detectDistance, detectLayer);
                    var hitsR = Physics2D.RaycastAll(rightCheckPoint.position, Vector2.right, detectDistance, detectLayer);
                    foreach (var h in hitsL.Concat(hitsR))
                    {
                        if (h.collider != null && h.collider.CompareTag("Player"))
                        {
                            playerDetected = true;
                            targetPos = h.collider.transform.position;
                            break;
                        }
                    }

                    if (playerDetected)
                    {
                                   
                        var desiredFace = targetPos.x > transform.position.x 
                                                              ? FaceTo.Right: FaceTo.Left;
                        SetFaceTo(desiredFace);

                        if (Time.time >= nextSkillTime)
                        {
                            nextSkillTime = Time.time + skillInterval;
                            SkillSystem.PlaySkill(skillId);
                        }
                    }
                })
                .OnExit(() =>
                {
                    animator.SetBool("Idle", false);
                });

          
            fsm.State(State.Die)
                .OnEnter(() =>
                {
                    animator.SetBool("Idle", false);
                    StartCoroutine(FadeAndDestroy());
                });
        }


        public override void BeHurt(IChaAttr attacker, float damage)
        {
            if (currentHealth <= 0) return;
            base.BeHurt(attacker, damage);

      
            RpcFlicker();

        
            if (currentHealth <= 0)
                fsm.ChangeState(State.Die);
        }

        [ClientRpc]
        private void RpcFlicker()
        {
            StartCoroutine(FlickerCoroutine());
        }

        private IEnumerator FlickerCoroutine()
        {
            float timer = 0f;
            while (timer < flickerDuration)
            {
                foreach (var r in renderers)
                    r.enabled = !r.enabled;
                timer += Time.deltaTime * flickerSpeed;
                yield return null;
            }
      
            foreach (var r in renderers) r.enabled = true;
        }

        private IEnumerator FadeAndDestroy()
        {
            float elapsed = 0f;
            var mats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                mats[i] = renderers[i].material;

            while (elapsed < deathFadeDuration)
            {
                float a = 1f - (elapsed / deathFadeDuration);
                foreach (var m in mats)
                {
                    var c = m.color;
                    c.a = a;
                    m.color = c;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            NetworkServer.Destroy(gameObject);
        }

        private void SetFaceTo(FaceTo dir)
        {
            if (faceTo == dir) return;
            faceTo = dir;
            UpdateFace(dir);
        }

        private void OnFaceChanged(FaceTo oldDir, FaceTo newDir)
        {
            UpdateFace(newDir);
        }

        private void UpdateFace(FaceTo dir)
        {
            view.localScale = dir == FaceTo.Left
                ? new Vector3(1, 1, 1)
                : new Vector3(-1, 1, 1);
        }

        public SkillSystem SkillSystem { set; get; }
    }
}
