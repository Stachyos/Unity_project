using UnityEngine;

public class EnemyAnimationTriggers : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    // 在攻击动画的关键帧（出手时）调用：
    public void PerformAttackEvent()
    {
        enemy.PerformAttack();
    }

    // 在攻击动画末尾帧调用，标记动画播完，切状态：
    public void AnimationTrigger()
    {
        enemy.AnimationTrigger();
    }
}