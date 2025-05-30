// PlayerAnimationTriggers.cs
using UnityEngine;

public class PlayerAnimationTriggers : MonoBehaviour
{
    private PlayerCtr _playerCtr;
    private void Awake() => _playerCtr = GetComponentInParent<PlayerCtr>();

    // Animation Event: 伤害判定帧
    public void PerformAttack()    => _playerCtr.PerformAttack();

    // Animation Event: 三段攻击最后一帧调用
    public void AttackComplete()   => _playerCtr.CmdAttackComplete();

    // Animation Event: 受击动画结束时调用
    public void HitComplete()      => _playerCtr.CmdHitComplete();
}