using System.Collections.Generic;

namespace GameLogic.Runtime
{
    //存储本关卡的缓存数据
    public class CacheModel : AbstractModel
    {
        private List<int> achievementCache = new List<int>(); //存储本轮获取的成就id数据
        protected override void OnInit()
        {
            
        }

        public void AddAchievement(int achievementID)
        {
            if (achievementCache.Contains(achievementID) == false)
            {
                achievementCache.Add(achievementID);
            }
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
            achievementCache.Clear();
        }

        public List<int> GetAchievement()
        {
            return achievementCache;
        }
    }
}