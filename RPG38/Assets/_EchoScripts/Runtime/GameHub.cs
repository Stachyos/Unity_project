using Mirror;

namespace GameLogic.Runtime
{
    public class GameHub : Architecture<GameHub>
    {
        protected override void Init()
        {
            this.RegisterModel(new UserModel());
            this.RegisterModel(new AchievementModel());
            this.RegisterModel(new CacheModel());

            this.RegisterSystem(new EventProcessSystem());
        }
    }
}