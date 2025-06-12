using Mirror;

namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
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