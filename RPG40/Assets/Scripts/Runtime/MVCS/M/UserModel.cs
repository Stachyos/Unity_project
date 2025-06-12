namespace GameLogic.Runtime
{
    /// <summary>
    /// This class inherits a class in QFramework, I override part of its function.
    /// </summary>
    public class UserModel : AbstractModel
    {
        public string userName = "Default";
        public int currentHP = 100;
        public int currentMP = 100;
        public int maxHP = 100;
        public int maxMP = 100;
        public int currentGold = 1000;
        
        protected override void OnInit()
        {
            
        }
    }
}