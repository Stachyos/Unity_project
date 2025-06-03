using System;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// 使用模糊逻辑，根据「敌人血量百分比」「玩家血量百分比」「二者距离」三项输入，
    /// 输出一个决策：要么 Attack1（近战），要么 Skill（远程/技能）。
    /// </summary>
    public class FuzzyHelper
    {
        // 决策结果枚举
        public enum ActionType
        {
            Attack1,
            Skill
        }

        #region —— Fuzzy 隶属度函数 —— 

        /// <summary>
        /// 计算「血量百分比」在 Low 集合的隶属度（[0,1]）。
        /// 这里示例：hpPct ∈ [0,1]，hpPct 越接近 0，Low 越接近 1；hpPct ≥ 0.5 后，Low = 0。
        /// 用三角形/梯形隶属度也可以，这里是一个简单的线性下降。
        /// </summary>
        private float HealthLow(float hpPct)
        {
            if (hpPct <= 0f) return 1f;
            if (hpPct >= 0.5f) return 0f;
            return (0.5f - hpPct) / 0.5f;  // hpPct 从 0→0.5 时，low 从 1→0
        }

        /// <summary>
        /// 计算「血量百分比」在 Medium（中等）集合的隶属度。
        /// hpPct 在 [0.25, 0.5] 时，隶属度从 0→1；在 [0.5, 0.75] 时，隶属度从 1→0；其余区域为 0。
        /// </summary>
        private float HealthMedium(float hpPct)
        {
            if (hpPct <= 0.25f || hpPct >= 0.75f) return 0f;
            if (hpPct > 0.25f && hpPct < 0.5f)
                return (hpPct - 0.25f) / (0.25f); // 0.25→0.5: medium 0→1
            if (hpPct >= 0.5f && hpPct < 0.75f)
                return (0.75f - hpPct) / (0.25f); // 0.5→0.75: medium 1→0
            return 0f;
        }

        /// <summary>
        /// 计算「血量百分比」在 High（高）集合的隶属度。
        /// hpPct ≤ 0.5 时，High=0；hpPct ∈ [0.5,1] 时，High 从 0→1 线性上升。
        /// </summary>
        private float HealthHigh(float hpPct)
        {
            if (hpPct <= 0.5f) return 0f;
            if (hpPct >= 1f) return 1f;
            return (hpPct - 0.5f) / (0.5f); // 0.5→1: high 0→1
        }

        /// <summary>
        /// 计算「距离」在 Near（近）集合的隶属度。
        /// 距离越小（<= nearMax），隶属度为 1；distance ∈ [nearMax, mid], 隶属度从 1→0；超出 mid 则 0。
        /// </summary>
        private float DistanceNear(float distance, float nearMax, float mid)
        {
            if (distance <= nearMax) return 1f;
            if (distance >= mid) return 0f;
            // 线性下降：nearMax→mid: 1→0
            return (mid - distance) / (mid - nearMax);
        }

        /// <summary>
        /// 计算「距离」在 Medium（中等）集合的隶属度。
        /// distance ∈ [nearMid, farMid], 隶属度从 0→1→0，采用三角形隶属度。
        /// </summary>
        private float DistanceMedium(float distance, float nearMid, float farMid)
        {
            if (distance <= nearMid || distance >= farMid) return 0f;
            float center = (nearMid + farMid) / 2f;
            if (distance > nearMid && distance <= center)
                return (distance - nearMid) / (center - nearMid); // 0→1
            else // distance ∈ [center, farMid]
                return (farMid - distance) / (farMid - center);   // 1→0
        }

        /// <summary>
        /// 计算「距离」在 Far（远）集合的隶属度。
        /// distance ≤ farMin 时，隶属度为 0；distance ∈ [farMin, farMax]，隶属度从 0→1；distance >= farMax: 1。
        /// </summary>
        private float DistanceFar(float distance, float farMin, float farMax)
        {
            if (distance <= farMin) return 0f;
            if (distance >= farMax) return 1f;
            return (distance - farMin) / (farMax - farMin); // farMin→farMax: 0→1
        }

        #endregion

        #region —— Rule Evaluation （规则评估） —— 

        /// <summary>
        /// 根据若干规则，计算“AttackDesire”（近战倾向）和“SkillDesire”（远程技能倾向）。
        /// 最后取倾向值更高者作为决策。
        /// </summary>
        /// <param name="enemyHpPct">敌人当前血量百分比（0~1）</param>
        /// <param name="playerHpPct">玩家当前血量百分比（0~1）</param>
        /// <param name="distance">敌人与玩家的实际距离（单位同场景坐标）</param>
        /// <param name="attackRange">近战触发的“最近距离”（attackRange）</param>
        public ActionType Decide(float enemyHpPct, float playerHpPct, float distance, float attackRange)
        {
            // 1. 计算各个模糊隶属度
            float eLow   = HealthLow(enemyHpPct);
            float eMed   = HealthMedium(enemyHpPct);
            float eHigh  = HealthHigh(enemyHpPct);
            float pLow   = HealthLow(playerHpPct);
            float pMed   = HealthMedium(playerHpPct);
            float pHigh  = HealthHigh(playerHpPct);

            // 简单地把“nearMax = attackRange”、mid = attackRange * 1.5f、farMin = attackRange * 1.5f、farMax = attackRange * 3”
            float nearMax = attackRange;
            float mid     = attackRange * 1.5f;
            float farMin  = attackRange * 1.5f;
            float farMax  = attackRange * 3f;

            float dNear   = DistanceNear(distance, nearMax, mid);
            float dMed    = DistanceMedium(distance, nearMax, farMax);
            float dFar    = DistanceFar(distance, farMin, farMax);

            // 2. 根据若干规则，计算“AttackDesire”与“SkillDesire”
            //    下面规则仅作示例，项目里可以根据自己的调优再扩展或改动。
            //    规则一：（敌人血高 & 玩家血低 & 距离近）⇒ 强烈近战倾向
            float r1 = Mathf.Min(eHigh, pLow, dNear);
            //    规则二：（敌人血高 & 玩家血低 & 距离中）⇒ 中等近战倾向
            float r2 = Mathf.Min(eHigh, pLow, dMed);
            //    规则三：（敌人血中 & 玩家血低 & 距离近）⇒ 中等近战倾向
            float r3 = Mathf.Min(eMed, pLow, dNear);
            //    规则四：（玩家血高 & 敌人血低）⇒ 远程技能倾向（不管距离）
            float r4 = Mathf.Min(pHigh, eLow);
            //    规则五：（距离远 & 敌人血中/高）⇒ 远程技能  
            float r5 = Mathf.Min(dFar, Mathf.Max(eMed, eHigh));
            //    规则六：（玩家血中 & 敌人血低 & 距离远）⇒ 强烈远程技能
            float r6 = Mathf.Min(pMed, eLow, dFar);

            // 可以根据需要再加其他规则……

            // 3. 聚合各个规则到两个“倾向值”上
            //    假设：AttackDesire 由 r1、r2、r3 组成；SkillDesire 由 r4、r5、r6 组成
            float attackDesire = Mathf.Max(r1, r2, r3);
            float skillDesire  = Mathf.Max(r4, r5, r6);
            
            // —— 给 SkillDesire 加一个“全局权重” 1.2f，使其更容易超过 attackDesire —— 
            skillDesire *= 1.7f;

            // 4. 比较哪个更大，若相等则默认近战优先
            if (attackDesire >= skillDesire) return ActionType.Attack1;
            else                            return ActionType.Skill;
        }

        #endregion
    }
}
