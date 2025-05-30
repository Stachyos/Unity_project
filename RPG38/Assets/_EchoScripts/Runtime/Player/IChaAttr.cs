namespace GameLogic.Runtime
{
    //属性系统
    public interface IChaAttr
    {
        public string Tag { get; }
        public float MaxHealth { get; set; }
        public float CurrentHealth {get;set;}
        public float CurrentMp { get; set; }
        public float MaxMp { get; set; }
        public float Attack{get;set;}
        public bool IsDead { get; }
        public void AddHealth(float amount);
        public void AddMp(float amount);
        public void BeHurt(IChaAttr attacker, float damage);
        public void AddMaxHealth(float amount);
        public void AddMaxMp(float amount);
        public void AddAttack(float amount);
    }
}