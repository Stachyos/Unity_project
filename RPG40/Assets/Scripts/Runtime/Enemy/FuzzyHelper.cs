using System;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>


    /// <summary>
    /// Using fuzzy logic, based on the input of "enemy health percentage", "player health percentage" and "distance between the two", an output decision is made: either Attack1 (close combat) or Skill (ranged/ability).
    /// </summary>
    public class FuzzyHelper
    {
        // Enumeration of decision outcomes
        public enum ActionType
        {
            Attack1,
            Skill
        }

        #region Fuzzy

        /// <summary>
        /// Calculate the membership degree ([0,1]) of "blood percentage" in the Low set.
        /// Here is an example: hpPct ∈ [0,1], the closer hpPct is to 0, the closer Low is to 1; when hpPct ≥ 0.5, Low = 0.
        /// Triangular/trapezoidal membership functions can also be used, but here it is a simple linear decline.
        /// </summary>
        private float HealthLow(float hpPct)
        {
            if (hpPct <= 0f) return 1f;
            if (hpPct >= 0.5f) return 0f;
            return (0.5f - hpPct) / 0.5f;  
        }

        /// <summary>
        /// Calculate the membership degree of "blood percentage" in the Medium (medium) category.
        /// When hpPct is in the range of [0.25, 0.5], the membership degree goes from 0 to 1; when it is in the range of [0.5, 0.75], the membership degree goes from 1 to 0; in all other areas, it is 0.
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
        /// Calculate the membership degree of "blood percentage" in the "High" set.
        /// When hpPct ≤ 0.5, High = 0; when hpPct ∈ [0.5, 1], High linearly increases from 0 to 1.
        /// </summary>
        private float HealthHigh(float hpPct)
        {
            if (hpPct <= 0.5f) return 0f;
            if (hpPct >= 1f) return 1f;
            return (hpPct - 0.5f) / (0.5f); // 0.5→1: high 0→1
        }

        /// <summary>
        /// Calculate the membership degree of "distance" in the "Near" set.
        /// The closer the distance (<= nearMax), the membership degree is 1; when distance is within the range [nearMax, mid], the membership degree decreases from 1 to 0; beyond mid, it is 0.
        /// </summary>
        private float DistanceNear(float distance, float nearMax, float mid)
        {
            if (distance <= nearMax) return 1f;
            if (distance >= mid) return 0f;
            // nearMax→mid: 1→0
            return (mid - distance) / (mid - nearMax);
        }

        /// <summary>
        /// Calculate the membership degree of "distance" in the Medium (medium) set.
        /// The distance ranges from [nearMid to farMid], and the membership degree goes from 0 to 1 to 0, using triangular membership function.
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
        /// Calculate the membership degree of "distance" in the "Far" set.
        /// When distance ≤ farMin, the membership degree is 0; when distance ∈ [farMin, farMax], the membership degree ranges from 0 to 1; when distance ≥ farMax: 1.
        /// </summary>
        private float DistanceFar(float distance, float farMin, float farMax)
        {
            if (distance <= farMin) return 0f;
            if (distance >= farMax) return 1f;
            return (distance - farMin) / (farMax - farMin); // farMin→farMax: 0→1
        }

        #endregion

        #region Rule Evaluation

        /// <summary>
        /// Based on several rules, calculate "AttackDesire" (close combat inclination) and "SkillDesire" (long-range skill inclination).
        /// Finally, select the one with the higher inclination value as the decision. /// </summary>
        /// <param name="enemyHpPct">Current health percentage of the enemy (0 - 1)</param>
        /// <param name="playerHpPct">Current health percentage of the player (0 - 1)</param>
        /// <param name="distance">Actual distance between the enemy and the player (same as scene coordinates unit)</param>
        public ActionType Decide(float enemyHpPct, float playerHpPct, float distance, float attackRange)
        {
            // 1. Calculate the various fuzzy membership degrees
            float eLow   = HealthLow(enemyHpPct);
            float eMed   = HealthMedium(enemyHpPct);
            float eHigh  = HealthHigh(enemyHpPct);
            float pLow   = HealthLow(playerHpPct);
            float pMed   = HealthMedium(playerHpPct);
            float pHigh  = HealthHigh(playerHpPct);

            // set "nearMax = attackRange", mid = attackRange * 1.5f, farMin = attackRange * 1.5f, and farMax = attackRange * 3"
            float nearMax = attackRange;
            float mid     = attackRange * 1.5f;
            float farMin  = attackRange * 1.5f;
            float farMax  = attackRange * 3f;

            float dNear   = DistanceNear(distance, nearMax, mid);
            float dMed    = DistanceMedium(distance, nearMax, farMax);
            float dFar    = DistanceFar(distance, farMin, farMax);

            // 2. Based on several rules, calculate "AttackDesire" and "SkillDesire"
//  The following rules are for illustration purposes only. The project can be expanded or modified according to your own optimization needs.
//  Rule 1: (Enemy health high & Player health low & Close distance) ⇒ Strong melee preference
            float r1 = Mathf.Min(eHigh, pLow, dNear);
            // Rule 2: (Enemy health high & Player health low & Distance medium) ⇒ Moderate melee preference
            float r2 = Mathf.Min(eHigh, pLow, dMed);
            // Rule 3: (Enemy health low & Player health low & Close distance) ⇒ Moderate melee preference
            float r3 = Mathf.Min(eMed, pLow, dNear);
            // Rule 4: (Player health high & Enemy health low) ⇒ Preference for long-range skills (regardless of distance)
            float r4 = Mathf.Min(pHigh, eLow);
            // Rule 5: (Long distance & Enemy HP is high) ⇒ Long-range skill
            float r5 = Mathf.Min(dFar, Mathf.Max(eMed, eHigh));
            // Rule 6: (Player health low & Enemy health low & Distance far) ⇒ Strong ranged skill
            float r6 = Mathf.Min(pMed, eLow, dFar);
            

            // 3. Aggregate all the rules onto two "tendency values"
// Assumption: AttackDesire is composed of r1, r2, and r3; SkillDesire is composed of r4, r5, and r6
            float attackDesire = Mathf.Max(r1, r2, r3);
            float skillDesire  = Mathf.Max(r4, r5, r6);
            

            // 4. Compare which one is larger. If they are equal, then melee combat takes precedence.
            
            if(attackDesire <= skillDesire || distance >= 4f) return ActionType.Skill;
            else return ActionType.Attack1;
        }

        #endregion
    }
}
