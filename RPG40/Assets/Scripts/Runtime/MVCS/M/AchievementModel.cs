using System.Collections.Generic;
using System.IO;
using JKFrame;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>

    public class AchievementModel : AbstractModel
    {
        private string savePath = Application.persistentDataPath + "/Achievements.json";
        private AchievementData achievementData;
        protected override void OnInit()
        {
            if(File.Exists(savePath) == false)
                achievementData = new AchievementData();
            else
            {
                achievementData = JsonConvert.DeserializeObject<AchievementData>(File.ReadAllText(savePath));
            }
        }

        public bool HasAchievement(int achievementID)
        {
            return achievementData.ids.FindIndex((item => item == achievementID)) != -1;
        }

        public void AddAchievement(int  achievementId)
        {
            if(HasAchievement(achievementId))
                return;
            
            achievementData.ids.Add(achievementId);
            var json = JsonConvert.SerializeObject(achievementData);
            File.WriteAllText(savePath, json);
        }

        public AchievementData GetAchievement()
        {
            return achievementData;
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
        }
    }

    public class AchievementData
    {
        public List<int> ids = new List<int>(); 
    }
}