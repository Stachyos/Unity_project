using UnityEditor;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in Unity
    /// </summary>
 
    [CreateAssetMenu(fileName = "SkillData")]
    public class SkillDataSo : ScriptableObject
    {
        public int skillID;
        public string skillName;
        public float hpCost; 
        public float mpCost; 
        public float attack; 
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
