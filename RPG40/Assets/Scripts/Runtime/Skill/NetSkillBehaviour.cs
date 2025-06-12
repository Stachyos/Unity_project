using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    //All initialization and other operations are carried out on the server. Remember not to have any client-related operations.
    
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