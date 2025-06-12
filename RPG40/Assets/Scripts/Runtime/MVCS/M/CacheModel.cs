using System.Collections.Generic;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>

    public class CacheModel : AbstractModel
    {
        private List<int> achievementCache = new List<int>(); 
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