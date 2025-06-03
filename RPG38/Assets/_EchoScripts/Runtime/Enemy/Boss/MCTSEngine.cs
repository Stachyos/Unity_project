using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace GameLogic.Runtime
{
    /// <summary>
    /// 蒙特卡洛树搜索引擎。提供一个入口方法 RunMCTS，
    /// 根据当前 GameState 返回下一步最佳 ActionType。
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

        /// <summary>
        /// 运行 MCTS 并返回根状态下得分最高的动作。
        /// </summary>
        /// <param name="initialState">当前游戏简化状态</param>
        /// <param name="iterations">模拟迭代次数</param>
        public ActionType RunMCTS(GameState initialState, int iterations = DEFAULT_ITERATIONS)
        {
            MCTSNode root = new MCTSNode(initialState, null, ActionType.None);

            // 迭代建树
            for (int i = 0; i < iterations; i++)
                SingleIteration(root);

            MCTSNode bestChild = root.BestChild();
            return bestChild != null ? bestChild.FromParentAction : ActionType.None;
        }

        /// <summary>
        /// 执行一次 MCTS 迭代：选择 → 扩展 → 模拟 → 回溯
        /// </summary>
        private void SingleIteration(MCTSNode root)
        {
            MCTSNode node = root;

            // 1. 选择：一路 UCT 选到未完全扩展的节点
            while (node.IsFullyExpanded())
            {
                node = node.UCTSelectChild();
                if (node == null) break;
            }

            // 2. 扩展：如果该节点未满，则拓展一个子节点
            if (node != null && !node.IsFullyExpanded())
            {
                MCTSNode child = node.Expand();
                if (child != null)
                    node = child;
            }

            // 3. 模拟：从 node 状态进行 Rollout
            float reward = node != null ? node.Rollout() : 0f;

            // 4. 回溯：将 reward 反向传播
            if (node != null)
                node.Backpropagate(reward);
        }

        #region —— GameState & MCTSNode 内部定义 —— 

        /// <summary>
        /// MCTS 用到的游戏简化状态。
        /// 包含 BossHpPct、PlayerHpPct、Distance、各类冷却剩余。
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
            /// 如果所有合法动作都已被扩展过，返回 true。
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
            /// 选择子节点：UCT公式挑最大值。
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
            /// 扩展尚未创建子节点的第一个合法动作。
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
            /// 对当前节点状态进行 Rollout（随机模拟到固定深度），返回一个 reward。
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

                // 最终奖励：血量差值
                score += simState.BossHpPct - simState.PlayerHpPct;
                return score;
            }

            /// <summary>
            /// 回溯更新 visits 和 totalScore。
            /// </summary>
            public void Backpropagate(float result)
            {
                Visits++;
                TotalScore += result;
                if (Parent != null)
                    Parent.Backpropagate(result);
            }

            /// <summary>
            /// 判断一个动作在给定 state 是否合法。
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
            /// 根据当前模拟 state 返回所有合法动作列表。
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
            /// 根据简化逻辑模拟执行一个动作，返回下一个 GameState。
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
                // 冷却递减
                next.NextA1Cd = Mathf.Max(0f, next.NextA1Cd - 1f);
                next.NextA2Cd = Mathf.Max(0f, next.NextA2Cd - 1f);
                next.NextSkillCd = Mathf.Max(0f, next.NextSkillCd - 1f);
                return next;
            }

            /// <summary>
            /// 从所有 children 中选出 averageScore（TotalScore/Visits）最高的子节点。
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
