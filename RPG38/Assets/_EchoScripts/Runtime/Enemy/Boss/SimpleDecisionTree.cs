using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// 简单决策树：根据距离和冷却状态快速选普通攻击、位移或 Idle。
    /// </summary>
    public class SimpleDecisionTree
    {
        public enum ActionType
        {
            None,
            Attack1,
            Attack2,
            Skill,
            WalkTowards,
            WalkAway,
            Idle
        }

        /// <summary>
        /// 基于距离和各技能冷却，返回一种快速决策。
        /// </summary>
        /// <param name="distance">Boss 与玩家距离</param>
        /// <param name="bossHpPct">Boss 血量百分比</param>
        /// <param name="nextA1Cd">下一次 Attack1 冷却剩余</param>
        /// <param name="nextA2Cd">下一次 Attack2 冷却剩余</param>
        /// <param name="nextSkillCd">下一次 Skill 冷却剩余</param>
        public ActionType Decide(float distance, float bossHpPct,
            float nextA1Cd, float nextA2Cd, float nextSkillCd)
        {
            
            // 如果距离中等且 Attack2 已就绪
            if (distance <= 3f && nextA2Cd <= 0f && bossHpPct >= 0.3f)
                return ActionType.Attack2;
            // 如果距离非常近且 Attack1 已就绪
            if (distance <= 3f && nextA1Cd <= 0f)
                return ActionType.Attack1;
            //
            // // 如果血量过低，尝试远离玩家
            // if (bossHpPct < 0.3f)
            //     return ActionType.WalkAway;

            // 如果距离过远，靠近玩家
            if (distance > 3f)
                return ActionType.WalkTowards;

            // 其他情况保持 Idle
            return ActionType.Idle;
        }
    }
}