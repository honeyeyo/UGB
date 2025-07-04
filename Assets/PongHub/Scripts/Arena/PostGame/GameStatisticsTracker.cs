// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections.Generic;
using PongHub.Arena.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.PostGame
{
    /// <summary>
    /// 游戏统计数据跟踪器
    /// 负责实时收集和跟踪比赛过程中的各种统计数据
    /// 包括得分、技术统计、回合时长等信息
    /// </summary>
    public class GameStatisticsTracker : NetworkBehaviour
    {
        [Header("跟踪设置")]
        [SerializeField]
        [Tooltip("Enable Debug Log / 启用调试日志 - Enable debug logging for statistics tracking")]
        private bool m_enableDebugLog = true;        // 启用调试日志

        // 统计数据
        private GameStatistics m_currentStats;
        private GameStatistics m_matchTotalStats;

        // 回合跟踪
        private float m_setStartTime;                    // 当前局开始时间
        private float m_rallyStartTime;                  // 当前回合开始时间
        private int m_currentRallyHits;                  // 当前回合击球次数
        private bool m_rallyInProgress;                  // 回合是否进行中
        private bool m_isInitialized;                    // 是否已初始化

        // 网络同步变量
        private NetworkVariable<NetworkedGameStatistics> m_networkStats = new();

        // 事件
        public static event Action<GameStatistics> OnStatisticsUpdated;

        // 公共属性
        public GameStatistics CurrentStatistics => m_currentStats;
        public bool IsInitialized => m_isInitialized;

        #region Unity生命周期

        private void Awake()
        {
            m_currentStats = new GameStatistics();
            m_matchTotalStats = new GameStatistics();
        }

        private void Start()
        {
            Initialize();
        }

        #endregion

        #region 网络生命周期

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                m_networkStats.OnValueChanged += OnNetworkStatsChanged;
            }

            // 监听游戏事件
            SubscribeToGameEvents();

            m_isInitialized = true;
            LogDebug("游戏统计跟踪器已初始化");
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeFromGameEvents();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            ResetMatchStatistics();
            m_setStartTime = Time.time;
        }

        private void SubscribeToGameEvents()
        {
            // 监听分数变化事件
            if (GameState.Instance?.Score != null)
            {
                GameState.Instance.Score.OnScoreUpdated += OnScoreUpdated;
            }

            // TODO: 监听球的击打事件
            // Ball.OnBallHit += OnBallHit;
            // Ball.OnBallScored += OnBallScored;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (GameState.Instance?.Score != null)
            {
                GameState.Instance.Score.OnScoreUpdated -= OnScoreUpdated;
            }

            // TODO: 取消监听球的击打事件
            // Ball.OnBallHit -= OnBallHit;
            // Ball.OnBallScored -= OnBallScored;
        }

        #endregion

        #region 统计数据管理

        /// <summary>
        /// 重置整场比赛统计
        /// </summary>
        public void ResetMatchStatistics()
        {
            m_currentStats.Reset();
            m_matchTotalStats.Reset();
            m_setStartTime = Time.time;

            if (IsServer)
            {
                m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
            }

            LogDebug("重置整场比赛统计数据");
        }

        /// <summary>
        /// 重置单局统计
        /// </summary>
        public void ResetSetStatistics()
        {
            m_currentStats.ResetSetStatistics();
            m_setStartTime = Time.time;
            m_rallyInProgress = false;
            m_currentRallyHits = 0;

            if (IsServer)
            {
                m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
            }

            LogDebug($"重置第{m_currentStats.CurrentSet}局统计数据");
        }

        /// <summary>
        /// 开始新的一局
        /// </summary>
        public void StartNewSet()
        {
            // 保存上一局的数据到总统计
            if (m_currentStats.CurrentSet > 1)
            {
                AccumulateSetStatistics();
            }

            m_currentStats.CurrentSet++;
            ResetSetStatistics();
        }

        /// <summary>
        /// 累积单局统计到总统计
        /// </summary>
        private void AccumulateSetStatistics()
        {
            m_matchTotalStats.PlayerAWinners += m_currentStats.PlayerAWinners;
            m_matchTotalStats.PlayerAErrors += m_currentStats.PlayerAErrors;
            m_matchTotalStats.PlayerAServeAces += m_currentStats.PlayerAServeAces;
            m_matchTotalStats.PlayerAReturnWins += m_currentStats.PlayerAReturnWins;

            m_matchTotalStats.PlayerBWinners += m_currentStats.PlayerBWinners;
            m_matchTotalStats.PlayerBErrors += m_currentStats.PlayerBErrors;
            m_matchTotalStats.PlayerBServeAces += m_currentStats.PlayerBServeAces;
            m_matchTotalStats.PlayerBReturnWins += m_currentStats.PlayerBReturnWins;

            m_matchTotalStats.TotalGameTime += m_currentStats.SetDuration;
            m_matchTotalStats.TotalRallies += m_currentStats.TotalRallies;

            if (m_currentStats.LongestRally > m_matchTotalStats.LongestRally)
            {
                m_matchTotalStats.LongestRally = m_currentStats.LongestRally;
            }
        }

        /// <summary>
        /// 获取当前统计数据
        /// </summary>
        public GameStatistics GetCurrentStatistics()
        {
            m_currentStats.SetDuration = Time.time - m_setStartTime;
            return m_currentStats;
        }

        /// <summary>
        /// 获取整场比赛统计数据
        /// </summary>
        public GameStatistics GetMatchStatistics()
        {
            AccumulateSetStatistics();
            return m_matchTotalStats;
        }

        /// <summary>
        /// 更新实时统计数据
        /// </summary>
        private void UpdateRealTimeStatistics()
        {
            m_currentStats.SetDuration = Time.time - m_setStartTime;
            m_currentStats.CalculateAverageRally();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 分数更新事件处理
        /// </summary>
        private void OnScoreUpdated(int teamAScore, int teamBScore)
        {
            m_currentStats.PlayerAScore = teamAScore;
            m_currentStats.PlayerBScore = teamBScore;

            // 结束当前回合
            if (m_rallyInProgress)
            {
                EndCurrentRally();
            }

            // 更新网络数据
            if (IsServer)
            {
                UpdateRealTimeStatistics();
                m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
            }

            OnStatisticsUpdated?.Invoke(m_currentStats);
            LogDebug($"分数更新: A队{teamAScore} - B队{teamBScore}");
        }

        /// <summary>
        /// 球击打事件处理
        /// </summary>
        private void OnBallHit(Vector3 hitPoint, NetworkedTeam.Team hittingTeam)
        {
            if (!m_rallyInProgress)
            {
                StartNewRally();
            }

            m_currentRallyHits++;
            LogDebug($"球被击打，当前回合击球次数: {m_currentRallyHits}");
        }

        /// <summary>
        /// 球得分事件处理
        /// </summary>
        private void OnBallScored(NetworkedTeam.Team scoringTeam, bool isWinner, bool isAce)
        {
            if (!IsServer) return;

            // 更新技术统计
            if (scoringTeam == NetworkedTeam.Team.TeamA)
            {
                if (isWinner)
                {
                    m_currentStats.PlayerAWinners++;
                    if (isAce) m_currentStats.PlayerAServeAces++;
                }
                else
                {
                    m_currentStats.PlayerBErrors++;
                }
            }
            else if (scoringTeam == NetworkedTeam.Team.TeamB)
            {
                if (isWinner)
                {
                    m_currentStats.PlayerBWinners++;
                    if (isAce) m_currentStats.PlayerBServeAces++;
                }
                else
                {
                    m_currentStats.PlayerAErrors++;
                }
            }

            // 结束回合
            EndCurrentRally();

            LogDebug($"得分统计更新: {scoringTeam} {(isWinner ? "获胜" : "失误")}");
        }

        #endregion

        #region 回合管理

        /// <summary>
        /// 开始新回合
        /// </summary>
        private void StartNewRally()
        {
            m_rallyStartTime = Time.time;
            m_rallyInProgress = true;
            m_currentRallyHits = 0;

            LogDebug("开始新回合");
        }

        /// <summary>
        /// 结束当前回合
        /// </summary>
        private void EndCurrentRally()
        {
            if (!m_rallyInProgress) return;

            m_rallyInProgress = false;
            m_currentStats.TotalRallies++;

            // 更新最长回合记录
            if (m_currentRallyHits > m_currentStats.LongestRally)
            {
                m_currentStats.LongestRally = m_currentRallyHits;
            }

            LogDebug($"回合结束，击球次数: {m_currentRallyHits}");
        }

        #endregion

        #region 局和比赛结束处理

        /// <summary>
        /// 处理局结束
        /// </summary>
        public void OnSetCompleted(NetworkedTeam.Team winner)
        {
            if (!IsServer) return;

            m_currentStats.CurrentSetWinner = winner;

            // 更新局数统计
            if (winner == NetworkedTeam.Team.TeamA)
            {
                m_currentStats.PlayerASetsWon++;
            }
            else if (winner == NetworkedTeam.Team.TeamB)
            {
                m_currentStats.PlayerBSetsWon++;
            }

            // 检查是否完成整场比赛
            int setsToWin = (m_currentStats.MaxSets / 2) + 1; // 例如5局3胜
            if (m_currentStats.PlayerASetsWon >= setsToWin)
            {
                m_currentStats.MatchWinner = NetworkedTeam.Team.TeamA;
                m_currentStats.IsMatchComplete = true;
            }
            else if (m_currentStats.PlayerBSetsWon >= setsToWin)
            {
                m_currentStats.MatchWinner = NetworkedTeam.Team.TeamB;
                m_currentStats.IsMatchComplete = true;
            }

            UpdateRealTimeStatistics();
            m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);

            LogDebug($"局结束: {winner} 获胜，当前局数比分 {m_currentStats.PlayerASetsWon}-{m_currentStats.PlayerBSetsWon}");
        }

        #endregion

        #region 网络同步

        /// <summary>
        /// 网络统计数据变化回调
        /// </summary>
        private void OnNetworkStatsChanged(NetworkedGameStatistics previousValue, NetworkedGameStatistics newValue)
        {
            m_currentStats = newValue.ToGameStatistics();
            OnStatisticsUpdated?.Invoke(m_currentStats);
        }

        #endregion

        #region 调试和日志

        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[GameStatisticsTracker] {message}");
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 手动添加制胜球统计
        /// </summary>
        public void AddWinner(NetworkedTeam.Team team)
        {
            if (!IsServer) return;

            if (team == NetworkedTeam.Team.TeamA)
                m_currentStats.PlayerAWinners++;
            else if (team == NetworkedTeam.Team.TeamB)
                m_currentStats.PlayerBWinners++;

            m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
        }

        /// <summary>
        /// 手动添加失误统计
        /// </summary>
        public void AddError(NetworkedTeam.Team team)
        {
            if (!IsServer) return;

            if (team == NetworkedTeam.Team.TeamA)
                m_currentStats.PlayerAErrors++;
            else if (team == NetworkedTeam.Team.TeamB)
                m_currentStats.PlayerBErrors++;

            m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
        }

        /// <summary>
        /// 手动设置最长回合
        /// </summary>
        public void SetLongestRally(int rallyLength)
        {
            if (!IsServer) return;

            if (rallyLength > m_currentStats.LongestRally)
            {
                m_currentStats.LongestRally = rallyLength;
                m_networkStats.Value = new NetworkedGameStatistics(m_currentStats);
            }
        }

        #endregion
    }
}