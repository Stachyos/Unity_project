using UnityEditor;
using UnityEngine;

namespace GameLogic.Runtime
{
    //1001起 角色技能  2001起 怪物技能
    [CreateAssetMenu(fileName = "SkillData")]
    public class SkillDataSo : ScriptableObject
    {
        public int skillID;
        public string skillName;
        public float hpCost; //消耗血量
        public float mpCost; //消耗蓝量
        public float attack; //伤害值
        public float cd; //cd
        public GameObject skillPrefab;
        
        public SkillData NewSkillData()
        {
            return new SkillData()
            {
                skillID = this.skillID,
                skillName = this.skillName,
                hpCost = this.hpCost,
                mpCost = this.mpCost,
                attack = this.attack,
                cd = this.cd,
                skillPrefab = this.skillPrefab
            };
        }
    }
}
