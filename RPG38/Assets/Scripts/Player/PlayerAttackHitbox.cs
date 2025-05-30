using System.Collections;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    public LayerMask enemyLayer;
    private Collider2D attackCol;
    private ContactFilter2D filter;
    private Collider2D[] results = new Collider2D[10];

    private void Awake()
    {
        attackCol = GetComponent<Collider2D>();
        filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;
    }

    // 动画事件调用
    public void PerformAttack()
    {
        int count = attackCol.OverlapCollider(filter, results);
        for (int i = 0; i < count; i++)
            results[i].GetComponent<Health>()?.TakeDamage(10);
    }
}

