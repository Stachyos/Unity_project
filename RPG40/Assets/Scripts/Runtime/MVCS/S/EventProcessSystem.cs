namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    //The event handling center aims to prevent the event logic from being scattered all over the place and instead handles it uniformly in one central location.
    public class EventProcessSystem : AbstractSystem
    {
        protected override void OnInit()
        {
            StringEventSystem.Global.Register<int, int>(EventKey.GoldNumberChanged, OnGoldNumberChanged);
            StringEventSystem.Global.Register<float,float>(EventKey.HpMaxNumberChanged, OnHpMaxNumberChanged);
            StringEventSystem.Global.Register<int>(EventKey.AddAchievement, OnAddAchievement);
            StringEventSystem.Global.Register(EventKey.Passed, OnPassed);
        }

        private void OnAddAchievement(int achievementId)
        {
            //History cannot be added to the cache unless it has been verified.
            if (this.GetModel<AchievementModel>().HasAchievement(achievementId) == false) 
            {
                this.GetModel<CacheModel>().AddAchievement(achievementId);
            }
                
            this.GetModel<AchievementModel>().AddAchievement(achievementId);
        }

        private void OnPassed()
        {
                StringEventSystem.Global.Send(EventKey.AddAchievement,1001);
        }
        private void OnGoldNumberChanged(int oldValue, int newValue)
        {
            if (newValue >= 1000)
            {
                StringEventSystem.Global.Send(EventKey.AddAchievement,1003);
            }
        }
        
        private void OnHpMaxNumberChanged(float oldValue, float newValue)
        {
            if (newValue >= 150)
            {
                StringEventSystem.Global.Send(EventKey.AddAchievement,1002);
            }
        }

        protected override void OnDeinit()
        {
            base.OnDeinit();
            
            StringEventSystem.Global.UnRegister<int, int>(EventKey.GoldNumberChanged, OnGoldNumberChanged);
            StringEventSystem.Global.UnRegister<int>(EventKey.AddAchievement, OnAddAchievement);
        }
    }
}