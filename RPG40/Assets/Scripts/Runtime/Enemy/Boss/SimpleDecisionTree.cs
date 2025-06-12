using UnityEngine;

namespace GameLogic.Runtime
{


    public class SimpleDecisionTree
    {
        public enum ActionType
        {
            None,
            Attack1,
            Attack2,
            Skill,
            Idle
        }

        /// <summary>
        /// ased on the distance and the cooldown time of each skill, a quick decision is made. /// </summary>
        /// <param name="distance">Distance between the boss and the player</param>
        /// <param name="bossHpPct">Percentage of the boss's health</param>
        /// <param name="nextA1Cd">Remaining cooldown time for the next Attack1</param>
        /// <param name="nextA2Cd">Remaining cooldown time for the next Attack2</param>
        /// <param name="nextSkillCd">Remaining cooldown time for the next Skill</param>
        public ActionType Decide(float distance, float bossHpPct,
            float nextA1Cd, float nextA2Cd, float nextSkillCd)
        {
            
            // If the distance is moderate and Attack2 is ready
            if (nextA2Cd <= 0f && bossHpPct >= 0.3f)
                return ActionType.Attack2;
            // If the distance is very close and Attack1 is ready
            if (nextA1Cd <= 0f)
                return ActionType.Attack1;
            //
            // If the health level is too low, try to stay away from the player.
            // if (bossHpPct < 0.3f)
            //     return ActionType.WalkAway;
            if (nextSkillCd <= 0f)
                return ActionType.Skill;     

            // Other situations remain in Idle state.
            return ActionType.Idle;
        }
    }
}