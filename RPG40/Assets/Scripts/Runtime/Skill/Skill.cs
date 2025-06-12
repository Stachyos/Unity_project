using System.Collections.Generic;
using System.Linq;
using JKFrame;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{

    //The design concept is to be networked. Just create one networked entity and all operations will be conducted on the server.
    public class SkillSystem
    {
        public GameObject owner; //Skill System Host
        private Dictionary<int,SkillData> _Skills = new Dictionary<int,SkillData>();
        private Dictionary<int,float> skillCd = new Dictionary<int,float>(); 

        public SkillSystem(GameObject owner)
        {
            this.owner = owner;
        }
        
        public void LearnSkill(int skillID)
        {
            if(skillID<1000)
                return;
            
            // Load the corresponding skillData data
// Search for the corresponding .so file in the specified path and load it into the system as a runtime SkillData
            var skillDataSo = ResSystem.LoadAsset<SkillDataSo>($"Assets/Addressable/DataSo/SkillData_{skillID}.asset");
            var skillData = skillDataSo.NewSkillData();
            _Skills.TryAdd(skillID, skillData);
            skillCd.TryAdd(skillID, 0);
        }

        public void PlaySkill(int skillID)
        {
            if(NetworkServer.active == false)
                return;
            
            if (_Skills.TryGetValue(skillID, out SkillData skillData))
            {
                var health = owner.GetComponent<IChaAttr>();
                
                //It is still not allowed to release on CD.
                if(skillCd[skillID] > 0)
                    return;
                if(health.CurrentHealth < skillData.hpCost)
                    return;
                if(health.CurrentMp < skillData.mpCost)
                    return;
                
                skillCd[skillID] = skillData.cd;
                health.AddHealth((int)-skillData.hpCost);
                health.AddMp((int)-skillData.mpCost);

                var obj = Object.Instantiate(skillData.skillPrefab);
                obj.GetComponent<NetSkillBehaviour>().SetSkillData(this.owner, skillData);
                NetworkServer.Spawn(obj);
            }
        }

        public void UpdateCd(float time)
        {
            var keys = skillCd.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                skillCd[keys[i]] -= time;
            }
        }
    }

    public class SkillData
    {
        public int skillID;
        public string skillName;
        public float hpCost; 
        public float mpCost; 
        public float attack; 
        public float cd; 
        public GameObject skillPrefab;
    }
}