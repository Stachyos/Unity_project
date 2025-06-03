namespace GameLogic.Runtime
{
    //事件处理中心，为了防止事件逻辑满天飞，统一在同一个地方处理
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
            //历史没有获得才允许添加到cache
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