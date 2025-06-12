using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>

    /// <summary>
    /// Monte Carlo Tree Search Engine. It provides an entry method called "RunMCTS",
    /// which returns the best action type for the next step based on the current GameState.
    /// </summary>
    public class MCTSEngine
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

        private const int DEFAULT_ITERATIONS = 100;
        private const int ROLLOUT_DEPTH = 3;
        private const float UCT_C = 1.0f;

        /// Monte Carlo Tree Search Engine. It provides an entry method named "RunMCTS", which returns the best action type for the next step based on the current GameState. /// <summary>
        /// Run MCTS and return the action with the highest score in the root state. /// </summary>
        /// <param name="initialState">The simplified state of the current game</param>
        /// <param name="iterations">The number of simulation iterations</param>
        public ActionType? RunMCTS(GameState initialState, int iterations = DEFAULT_ITERATIONS)
        {
          
            MCTSNode root = new MCTSNode(initialState, null, ActionType.None);
            for (int i = 0; i < iterations; i++)
                SingleIteration(root);

           
            var candidates = new List<MCTSNode>();
            foreach (var kv in root.Children)
            {
                if (kv.Key == ActionType.Attack1 ||
                    kv.Key == ActionType.Attack2 ||
                    kv.Key == ActionType.Skill)
                {
                    candidates.Add(kv.Value);
                }
            }

       
            if (candidates.Count == 0)
                return null;

       
            MCTSNode best = null;
            float bestAvg = float.MinValue;
            foreach (var c in candidates)
            {
                float avg = c.TotalScore / (c.Visits + 1e-6f);
                if (avg > bestAvg)
                {
                    bestAvg = avg;
                    best = c;
                }
            }

         
            return best?.FromParentAction;
        }

        /// <summary>
        /// Perform one iteration of MCTS: selection → expansion → simulation → backtracking
        /// </summary>
        private void SingleIteration(MCTSNode root)
        {
            MCTSNode node = root;

            // 1. Option: Select all UCTs up to the unexpanded nodes
            while (node.IsFullyExpanded())
            {
                node = node.UCTSelectChild();
                if (node == null) break;
            }

            // 2. Expansion: If the current node is not full, then create a child node.
            if (node != null && !node.IsFullyExpanded())
            {
                MCTSNode child = node.Expand();
                if (child != null)
                    node = child;
            }

            // 3. Simulation: Perform rollout from the node state
            float reward = node != null ? node.Rollout() : 0f;

            // 4. Backpropagation: Backward propagation of the reward
            if (node != null)
                node.Backpropagate(reward);
        }

        #region GameState & MCTSNode

        /// <summary>
        /// The simplified game state used by MCTS.
        /// Includes BossHpPct, PlayerHpPct, Distance, and various remaining cooldowns.
        /// </summary>
        public class GameState
        {
            public float BossHpPct;
            public float PlayerHpPct;
            public float Distance;
            public float NextA1Cd;
            public float NextA2Cd;
            public float NextSkillCd;

            public GameState(float bossHp, float playerHp, float dist,
                             float a1Cd, float a2Cd, float skillCd)
            {
                BossHpPct = bossHp;
                PlayerHpPct = playerHp;
                Distance = dist;
                NextA1Cd = a1Cd;
                NextA2Cd = a2Cd;
                NextSkillCd = skillCd;
            }

            public GameState Clone()
            {
                return new GameState(BossHpPct, PlayerHpPct, Distance, NextA1Cd, NextA2Cd, NextSkillCd);
            }
        }

        private class MCTSNode
        {
            public GameState State;
            public MCTSNode Parent;
            public Dictionary<ActionType, MCTSNode> Children;
            public int Visits;
            public float TotalScore;
            public ActionType FromParentAction;

            public MCTSNode(GameState state, MCTSNode parent, ActionType action)
            {
                State = state;
                Parent = parent;
                FromParentAction = action;
                Children = new Dictionary<ActionType, MCTSNode>();
                Visits = 0;
                TotalScore = 0f;
            }

            /// <summary>
            /// If all the legal actions have been expanded, return true.
            /// </summary>
            public bool IsFullyExpanded()
            {
                foreach (ActionType a in Enum.GetValues(typeof(ActionType)))
                {
                    if (a == ActionType.None) continue;
                    if (!Children.ContainsKey(a) && IsActionValid(State, a))
                        return false;
                }
                return true;
            }

            /// <summary>
            /// Selecting child nodes: The UCT formula selects the maximum value.
            /// </summary>
            public MCTSNode UCTSelectChild()
            {
                MCTSNode best = null;
                float bestUCT = float.MinValue;

                foreach (var kv in Children)
                {
                    MCTSNode child = kv.Value;
                    float q = child.TotalScore / (child.Visits + 1e-6f);
                    float uct = q + UCT_C * Mathf.Sqrt(Mathf.Log(Visits + 1) / (child.Visits + 1e-6f));
                    if (uct > bestUCT)
                    {
                        bestUCT = uct;
                        best = child;
                    }
                }

                return best;
            }

            /// <summary>
            /// The first legal action that expands and has not yet created child nodes.
            /// </summary>
            public MCTSNode Expand()
            {
                foreach (ActionType a in Enum.GetValues(typeof(ActionType)))
                {
                    if (a == ActionType.None) continue;
                    if (!Children.ContainsKey(a) && IsActionValid(State, a))
                    {
                        GameState next = SimulateAction(State, a);
                        MCTSNode node = new MCTSNode(next, this, a);
                        Children[a] = node;
                        return node;
                    }
                }
                return null;
            }

            /// <summary>
            /// Perform a Rollout (random simulation up to a fixed depth) on the current node state and return a reward.
            /// </summary>
            public float Rollout()
            {
                GameState simState = State.Clone();
                float score = 0f;

                for (int depth = 0; depth < ROLLOUT_DEPTH; depth++)
                {
                    List<ActionType> actions = GetValidActions(simState);
                    if (actions.Count == 0) break;

                    ActionType a = actions[UnityEngine.Random.Range(0, actions.Count)];
                    simState = SimulateAction(simState, a);

                    if (simState.PlayerHpPct <= 0f)
                    {
                        score += 1f;
                        break;
                    }
                    if (simState.BossHpPct <= 0f)
                    {
                        score -= 1f;
                        break;
                    }
                }

                // Final reward: Blood quantity difference
                score += simState.BossHpPct - simState.PlayerHpPct;
                return score;
            }

            /// <summary>
            /// Backtrack and update the "visits" and "totalScore" values.
            /// </summary>
            public void Backpropagate(float result)
            {
                Visits++;
                TotalScore += result;
                if (Parent != null)
                    Parent.Backpropagate(result);
            }

            /// <summary>
            /// Determine whether an action is legal in a given state.
            /// </summary>
            public static bool IsActionValid(GameState s, ActionType a)
            {
                switch (a)
                {
                    case ActionType.Attack1:
                        return (s.Distance <= 3f) && (s.NextA1Cd <= 0f);
                    case ActionType.Attack2:
                        return (s.Distance <= 3f) && (s.NextA2Cd <= 0f);
                    case ActionType.Skill:
                        return s.NextSkillCd <= 0f;
                    case ActionType.WalkTowards:
                        return true;
                    case ActionType.WalkAway:
                        return true;
                    case ActionType.Idle:
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Based on the current simulation state, return the list of all valid actions.
            /// </summary>
            public static List<ActionType> GetValidActions(GameState state)
            {
                List<ActionType> ret = new List<ActionType>();
                foreach (ActionType a in Enum.GetValues(typeof(ActionType)))
                {
                    if (a == ActionType.None) continue;
                    if (IsActionValid(state, a))
                        ret.Add(a);
                }
                return ret;
            }

            /// <summary>
            /// Based on the simplified logical simulation, execute an action and return to the next GameState.
            /// </summary>
            public static GameState SimulateAction(GameState s, ActionType a)
            {
                float attack1Duration = 1.3f;
                float attack2Duration = 1.7f;
                float skillDuration = 2f;
               
                float GetHit(float duration)
                {
                    return duration / 60;
                }

                
                GameState next = s.Clone();
                switch (a)
                {
                    case ActionType.Attack1:
                        next.PlayerHpPct -= 0.1f;
                        next.BossHpPct -= GetHit(attack1Duration);
                        next.NextA1Cd = 1.5f;
                        break;
                    case ActionType.Attack2:
                        next.PlayerHpPct -= 0.18f;
                        next.BossHpPct -= GetHit(attack2Duration);
                        next.NextA2Cd = 3f;
                        break;
                    case ActionType.Skill:
                        next.PlayerHpPct -= 0.4f;
                        next.BossHpPct -= GetHit(skillDuration);
                        next.NextSkillCd = 10f;
                        break;
                    case ActionType.WalkTowards:
                        next.Distance = Mathf.Max(0f, next.Distance - 1f);
                        break;
                    case ActionType.WalkAway:
                        next.Distance += 1f;
                        break;
                    case ActionType.Idle:
                        break;
                }
                // Cooling rate reduction
                next.NextA1Cd = Mathf.Max(0f, next.NextA1Cd - 1f);
                next.NextA2Cd = Mathf.Max(0f, next.NextA2Cd - 1f);
                next.NextSkillCd = Mathf.Max(0f, next.NextSkillCd - 1f);
                return next;
            }

            /// <summary>
            /// Select the child node with the highest averageScore (TotalScore/Visits) from all the children.
            /// </summary>
            public MCTSNode BestChild()
            {
                MCTSNode best = null;
                float bestVal = float.MinValue;
                foreach (var kv in Children)
                {
                    MCTSNode child = kv.Value;
                    float avg = child.TotalScore / (child.Visits + 1e-6f);
                    if (avg > bestVal)
                    {
                        bestVal = avg;
                        best = child;
                    }
                }
                return best;
            }
        }

        #endregion
    }
}
