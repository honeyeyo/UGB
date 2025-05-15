// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using Unity.Netcode;

namespace UltimateGloveBall.Arena.Gameplay
{
    /// <summary>
    /// 网络化分数系统
    /// 用于在多个客户端之间同步游戏分数
    /// 通过注册OnScoreUpdated事件来监听分数变化
    /// </summary>
    public class NetworkedScore : NetworkBehaviour
    {
        /// <summary>
        /// A队分数网络变量
        /// </summary>
        private NetworkVariable<int> m_teamAScore = new();

        /// <summary>
        /// B队分数网络变量
        /// </summary>
        private NetworkVariable<int> m_teamBScore = new();

        /// <summary>
        /// 分数更新事件
        /// 参数为(A队分数, B队分数)
        /// </summary>
        public Action<int, int> OnScoreUpdated;

        /// <summary>
        /// 获取当前获胜队伍
        /// </summary>
        /// <returns>获胜队伍枚举值</returns>
        public NetworkedTeam.Team GetWinningTeam()
        {
            if (m_teamAScore.Value > m_teamBScore.Value)
            {
                return NetworkedTeam.Team.TeamA;
            }
            else if (m_teamAScore.Value < m_teamBScore.Value)
            {
                return NetworkedTeam.Team.TeamB;
            }

            return NetworkedTeam.Team.NoTeam;
        }

        /// <summary>
        /// 网络对象生成时的回调
        /// 非服务器端注册分数变化事件
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_teamAScore.OnValueChanged += OnScoreChanged;
                m_teamBScore.OnValueChanged += OnScoreChanged;
            }
        }

        /// <summary>
        /// 分数变化时的回调
        /// 触发分数更新事件
        /// </summary>
        /// <param name="previousvalue">变化前的分数</param>
        /// <param name="newvalue">变化后的分数</param>
        private void OnScoreChanged(int previousvalue, int newvalue)
        {
            OnScoreUpdated?.Invoke(m_teamAScore.Value, m_teamBScore.Value);
        }

        /// <summary>
        /// 更新指定队伍的分数
        /// </summary>
        /// <param name="team">目标队伍</param>
        /// <param name="inc">分数增量</param>
        public void UpdateScore(NetworkedTeam.Team team, int inc)
        {
            switch (team)
            {
                case NetworkedTeam.Team.TeamA:
                    m_teamAScore.Value += inc;
                    break;
                case NetworkedTeam.Team.TeamB:
                    m_teamBScore.Value += inc;
                    break;
                case NetworkedTeam.Team.NoTeam:
                    break;
                default:
                    break;
            }

            OnScoreUpdated?.Invoke(m_teamAScore.Value, m_teamBScore.Value);
        }

        /// <summary>
        /// 重置两队分数为0
        /// </summary>
        public void Reset()
        {
            m_teamAScore.Value = 0;
            m_teamBScore.Value = 0;
            OnScoreUpdated?.Invoke(m_teamAScore.Value, m_teamBScore.Value);
        }
    }
}