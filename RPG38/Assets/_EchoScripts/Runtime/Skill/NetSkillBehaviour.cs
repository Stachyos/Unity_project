using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    //初始化等操作均在服务器进行，切记不要有客户端相关的操作
    
    public class NetSkillBehaviour : NetBehaviour
    {
        public GameObject skillCaster;
        public SkillData skillData;

        public BoxCollider2D mainCollider;
        public Rigidbody2D rb2d;
        public void SetSkillData(GameObject owner,SkillData skillData)
        {
            this.skillData = skillData;
            this.skillCaster = owner;
        }
    }
}