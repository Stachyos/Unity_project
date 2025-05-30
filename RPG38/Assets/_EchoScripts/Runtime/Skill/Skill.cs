using System.Collections.Generic;
using System.Linq;
using JKFrame;
using Mirror;
using UnityEngine;

namespace GameLogic.Runtime
{
    //极简技能释放 这里设计思路是联网的，只new一个联网技能实体 所有操作均在服务器，注意写法，不要有客户端相关内容
    public class SkillSystem
    {
        public GameObject owner; //技能系统宿主
        private Dictionary<int,SkillData> _Skills = new Dictionary<int,SkillData>();
        private Dictionary<int,float> skillCd = new Dictionary<int,float>(); //计算cd

        public SkillSystem(GameObject owner)
        {
            this.owner = owner;
        }
        
        public void LearnSkill(int skillID)
        {
            if(skillID<1000)
                return;
            
            //加载对应skillData数据       
            //搜索对应路径下的so 将so加载进来转为runtime SkillData
            var skillDataSo = ResSystem.LoadAsset<SkillDataSo>($"Assets/_EchoAddressable/DataSo/SkillData_{skillID}.asset");
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
                
                //还在cd不允许释放
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
        public float hpCost; //消耗血量
        public float mpCost; //消耗蓝量
        public float attack; //伤害值
        public float cd; //cd
        public GameObject skillPrefab;
    }
}