// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using PongHub.App;
using PongHub.Gameplay.Ball;
using PongHub.Arena.Environment;
using PongHub.Arena.Player;
using PongHub.Arena.Services;
using PongHub.Arena.PostGame;
using Unity.Netcode;
using UnityEngine;
#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN)
using Oculus.Platform;
#endif

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 游戏管理器类,负责管理游戏的整体状态和流程
    /// 主要功能:
    /// 1. 管理游戏的不同阶段(赛前、倒计时、比赛中、赛后)
    /// 2. 在赛前阶段跟踪玩家的队伍选择
    /// 3. 根据游戏阶段设置场景
    /// 4. 随机选择游戏的颜色配置
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        // 游戏开始倒计时时间(秒)
        private const double GAME_START_COUNTDOWN_TIME_SEC = 4;
        // 游戏持续时间(秒)
        private const double GAME_DURATION_SEC = 180;

        /// <summary>
        /// 游戏阶段枚举
        /// </summary>
        public enum GamePhase
        {
            PreGame,    // 赛前准备
            CountDown,  // 倒计时
            InGame,     // 比赛中
            PostGame,   // 赛后
        }

        /// <summary>
        /// 游戏状态保存结构体
        /// </summary>
        private struct GameStateSave
        {
            public double TimeRemaining;  // 剩余时间
        }

        // 序列化字段
        [SerializeField] private GameState m_gameState;                           // 游戏状态
        [SerializeField] private GameObject m_startGameButtonContainer;           // 开始游戏按钮容器
        [SerializeField] private GameObject m_restartGameButtonContainer;         // 重新开始按钮容器
        [SerializeField] private GameObject m_inviteFriendButtonContainer;        // 邀请好友按钮容器
        [SerializeField] private BallSpawner m_ballSpawner;                      // 球体生成器

        [SerializeField] private CountdownView m_countdownView;                   // 倒计时视图

        // [SerializeField] private ObstacleManager m_obstacleManager;               // 障碍物管理器

        [SerializeField] private GameObject m_postGameView;                       // 赛后视图

        [Header("PostGame 改进组件")]
        [SerializeField] private PostGameController m_postGameController;        // PostGame控制器
        [SerializeField] private GameStatisticsTracker m_statisticsTracker;      // 统计数据跟踪器
        [SerializeField] private int m_pointsPerSet = 11;                        // 每局分数
        [SerializeField] private int m_setsToWin = 3;                            // 获胜需要的局数

        [SerializeField] private AudioSource m_courtAudioSource;                  // 场地音效源
        [SerializeField] private AudioClip m_lowCountdownBeep;                   // 低音倒计时音效
        [SerializeField] private AudioClip m_highCountdownBeep;                  // 高音倒计时音效

        // 游戏阶段监听器列表
        private readonly List<IGamePhaseListener> m_phaseListeners = new();

        // 网络同步变量
        private NetworkVariable<GamePhase> m_currentGamePhase = new(GamePhase.PreGame);  // 当前游戏阶段
        private NetworkVariable<double> m_gameStartTime = new();                          // 游戏开始时间
        private NetworkVariable<double> m_gameEndTime = new();                           // 游戏结束时间

        // 玩家队伍选择字典
        private readonly Dictionary<ulong, NetworkedTeam.Team> m_playersTeamSelection = new();

        // 队伍颜色
        private NetworkVariable<TeamColor> m_teamAColor = new(TeamColor.Profile1TeamA);   // A队颜色
        private NetworkVariable<TeamColor> m_teamBColor = new(TeamColor.Profile1TeamB);   // B队颜色
        private bool m_teamColorIsSet = false;                                            // 队伍颜色是否已设置

        // 游戏状态保存
        private GameStateSave m_gameStateSave;

        // 上一次剩余秒数
        private int m_previousSecondsLeft = int.MaxValue;

        // 公共属性
        public GamePhase CurrentPhase => m_currentGamePhase.Value;
        public TeamColor TeamAColor => m_teamAColor.Value;
        public TeamColor TeamBColor => m_teamBColor.Value;

        /// <summary>
        /// 启用时注册事件监听
        /// </summary>
        private void OnEnable()
        {
            m_currentGamePhase.OnValueChanged += OnPhaseChanged;
            m_gameStartTime.OnValueChanged += OnStartTimeChanged;
            PHApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration += OnHostMigrationStarted;

            // 注册PostGame事件监听
            if (m_postGameController != null)
            {
                PostGameController.OnNextSetRequested += OnNextSetRequested;
                PostGameController.OnSpectatorModeRequested += OnSpectatorModeRequested;
                PostGameController.OnExitRequested += OnExitRequested;
            }

            // 监听分数变化事件，用于检测局结束
            if (m_gameState?.Score != null)
            {
                m_gameState.Score.OnScoreUpdated += OnScoreUpdated;
            }
        }

        /// <summary>
        /// 游戏开始时间改变回调
        /// </summary>
        private void OnStartTimeChanged(double previousvalue, double newvalue)
        {
            if (m_currentGamePhase.Value == GamePhase.CountDown)
            {
                Debug.LogWarning($"OnStartTimeChanged: {newvalue}");
                m_countdownView.Show(newvalue);
            }
        }

        /// <summary>
        /// 主机迁移开始时的处理
        /// </summary>
        private void OnHostMigrationStarted()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)
            {
                m_gameStateSave.TimeRemaining = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
            }
        }

        /// <summary>
        /// 禁用时取消事件监听
        /// </summary>
        private void OnDisable()
        {
            m_currentGamePhase.OnValueChanged -= OnPhaseChanged;
            m_gameStartTime.OnValueChanged -= OnStartTimeChanged;
            PHApplication.Instance.NetworkLayer.OnHostLeftAndStartingMigration -= OnHostMigrationStarted;

            // 取消PostGame事件监听
            if (m_postGameController != null)
            {
                PostGameController.OnNextSetRequested -= OnNextSetRequested;
                PostGameController.OnSpectatorModeRequested -= OnSpectatorModeRequested;
                PostGameController.OnExitRequested -= OnExitRequested;
            }

            // 取消分数变化监听
            if (m_gameState?.Score != null)
            {
                m_gameState.Score.OnScoreUpdated -= OnScoreUpdated;
            }
        }

        /// <summary>
        /// 注册阶段监听器
        /// </summary>
        public void RegisterPhaseListener(IGamePhaseListener listener)
        {
            m_phaseListeners.Add(listener);
            listener.OnPhaseChanged(m_currentGamePhase.Value);
            listener.OnTeamColorUpdated(TeamAColor, TeamBColor);
        }

        /// <summary>
        /// 取消注册阶段监听器
        /// </summary>
        public void UnregisterPhaseListener(IGamePhaseListener listener)
        {
            _ = m_phaseListeners.Remove(listener);
        }

        /// <summary>
        /// 更新玩家队伍
        /// </summary>
        public void UpdatePlayerTeam(ulong clientId, NetworkedTeam.Team team)
        {
            m_playersTeamSelection[clientId] = team;
        }

        /// <summary>
        /// 获取人数最少的队伍
        /// </summary>
        public NetworkedTeam.Team GetTeamWithLeastPlayers()
        {
            var countA = 0;
            var countB = 0;
            foreach (var team in m_playersTeamSelection.Values)
            {
                if (team == NetworkedTeam.Team.TeamA)
                {
                    countA++;
                }
                else if (team == NetworkedTeam.Team.TeamB)
                {
                    countB++;
                }
            }

            return countA <= countB ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
        }

        /// <summary>
        /// 网络对象生成时的处理
        /// </summary>
        public override void OnNetworkSpawn()
        {
            var currentPhase = m_currentGamePhase.Value;
            if (IsServer)
            {
                if (!m_teamColorIsSet)
                {
                    TeamColorProfiles.Instance.GetRandomProfile(out var colorA, out var colorB);
                    m_teamAColor.Value = colorA;
                    m_teamBColor.Value = colorB;
                    m_teamColorIsSet = true;
                }
                OnColorUpdatedClientRPC(m_teamAColor.Value, m_teamBColor.Value);

                if (m_currentGamePhase.Value is GamePhase.PreGame)
                {
                    m_startGameButtonContainer.SetActive(true);
                }
                else if (m_currentGamePhase.Value is GamePhase.PostGame)
                {
                    m_restartGameButtonContainer.SetActive(true);
                }

                // m_obstacleManager.SetTeamColor(TeamAColor, TeamBColor);

                // 从主机迁移恢复时的状态处理
                if (currentPhase == GamePhase.CountDown)
                {
                    StartCountdown();
                }
                else if (currentPhase == GamePhase.InGame)
                {
                    HandleInGameHostMigration(m_gameStateSave.TimeRemaining);
                }
            }

            if (m_currentGamePhase.Value == GamePhase.PreGame)
            {
                m_inviteFriendButtonContainer.SetActive(true);
            }

            OnPhaseChanged(currentPhase, currentPhase);
            NotifyPhaseListener(m_currentGamePhase.Value);
            m_teamColorIsSet = true;
        }

        /// <summary>
        /// 游戏阶段改变时的处理
        /// </summary>
        private void OnPhaseChanged(GamePhase previousvalue, GamePhase newvalue)
        {
            if (newvalue == GamePhase.CountDown)
            {
                m_countdownView.Show(m_gameStartTime.Value);
            }

            if (newvalue == GamePhase.PostGame)
            {
                m_postGameView.SetActive(true);
            }
            else
            {
                m_postGameView.SetActive(false);
            }

            var playerCanMove = newvalue is GamePhase.InGame or GamePhase.PreGame;
            PlayerInputController.Instance.MovementEnabled = playerCanMove;
            // 仅对游戏中的玩家生效
            if (LocalPlayerEntities.Instance.Avatar != null)
            {
                m_inviteFriendButtonContainer.SetActive(newvalue == GamePhase.PreGame);
            }
            m_previousSecondsLeft = int.MaxValue;
            NotifyPhaseListener(newvalue);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (m_currentGamePhase.Value is GamePhase.PreGame or GamePhase.PostGame)
            {
                m_gameState.Score.Reset();
                _ = StartCoroutine(DeactivateStartButton());

                // 仅在初始开始游戏时检查阵营
                if (m_currentGamePhase.Value is GamePhase.PreGame)
                {
                    CheckPlayersSides();
                    LockPlayersTeams();
                }

                StartCountdown();

                // 使用新的乒乓球生成管理器
                var pongSpawningManager = GetPongSpawningManager();
                if (pongSpawningManager != null)
                {
                    pongSpawningManager.ResetInGameSpawnPoints();
                }

                RespawnAllPlayers();
            }
        }

        /// <summary>
        /// 重置游戏中的生成点
        /// </summary>
        private void ResetInGameSpawnPoints()
        {
            // 使用新的乒乓球会话管理器重置所有生成点
            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
            if (spawnConfig != null)
            {
                spawnConfig.ResetAllSpawnPoints();
                Debug.Log("[GameManager] 已重置游戏中的生成点");
            }
            else
            {
                Debug.LogWarning("[GameManager] 未找到PongSpawnConfiguration组件");
            }
        }

        /// <summary>
        /// 颜色更新的客户端RPC
        /// </summary>
        [ClientRpc]
        private void OnColorUpdatedClientRPC(TeamColor teamColorA, TeamColor teamColorB)
        {
            NotifyTeamColorListener(teamColorA, teamColorB);
            m_teamColorIsSet = true;
        }

        /// <summary>
        /// 邀请好友
        /// </summary>
        public void InviteFriend()
        {
            // 仅在赛前阶段可以邀请好友
            if (CurrentPhase != GamePhase.PreGame)
            {
                return;
            }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            Debug.Log("Invite Friends clicked");
#else
            GroupPresence.LaunchInvitePanel(new InviteOptions());
#endif
        }

        /// <summary>
        /// 禁用开始按钮的协程
        /// </summary>
        private IEnumerator DeactivateStartButton()
        {
            // 等待指针处理完成后再禁用UI
            yield return new WaitForEndOfFrame();
            m_startGameButtonContainer.SetActive(false);
            m_restartGameButtonContainer.SetActive(false);
        }

        /// <summary>
        /// 开始倒计时
        /// </summary>
        private void StartCountdown()
        {
            m_gameStartTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_START_COUNTDOWN_TIME_SEC;
            m_currentGamePhase.Value = GamePhase.CountDown;
            m_countdownView.Show(m_gameStartTime.Value, SwitchToInGame);
        }

        /// <summary>
        /// 切换到游戏中状态
        /// </summary>
        public void SwitchToInGame()
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + GAME_DURATION_SEC;
            // 在乒乓球游戏中，球由玩家手动生成，而不是自动生成
        }

        /// <summary>
        /// 进入赛后状态
        /// </summary>
        private void GoToPostGame()
        {
            m_ballSpawner.DespawnAllBalls();
            m_currentGamePhase.Value = GamePhase.PostGame;
            m_restartGameButtonContainer.SetActive(true);

            // 使用新的乒乓球生成管理器
            var pongSpawningManager = GetPongSpawningManager();
            if (pongSpawningManager != null)
            {
                pongSpawningManager.ResetPostGameSpawnPoints();
            }

            RespawnAllPlayers();
        }

        /// <summary>
        /// 重置赛后的生成点
        /// </summary>
        private void ResetPostGameSpawnPoints()
        {
            // 使用新的乒乓球会话管理器重置观众席生成点
            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
            if (spawnConfig != null)
            {
                spawnConfig.ResetAllSpawnPoints();
                Debug.Log("[GameManager] 已重置赛后的生成点");
            }
            else
            {
                Debug.LogWarning("[GameManager] 未找到PongSpawnConfiguration组件");
            }
        }

        /// <summary>
        /// 分数更新事件处理 - 检测局结束
        /// </summary>
        private void OnScoreUpdated(int teamAScore, int teamBScore)
        {
            if (m_currentGamePhase.Value != GamePhase.InGame)
                return;

            if (CheckSetComplete(teamAScore, teamBScore, out NetworkedTeam.Team winner))
            {
                OnSetCompleted(winner);
            }
        }

        /// <summary>
        /// 检查单局是否完成
        /// </summary>
        private bool CheckSetComplete(int teamAScore, int teamBScore, out NetworkedTeam.Team winner)
        {
            winner = NetworkedTeam.Team.NoTeam;

            // 检查是否有队伍达到获胜分数且领先至少2分
            if (teamAScore >= m_pointsPerSet && teamAScore >= teamBScore + 2)
            {
                winner = NetworkedTeam.Team.TeamA;
                return true;
            }
            else if (teamBScore >= m_pointsPerSet && teamBScore >= teamAScore + 2)
            {
                winner = NetworkedTeam.Team.TeamB;
                return true;
            }

            // 处理平分情况：两队都到达获胜分数但没有2分差距时，需要领先2分才能获胜
            if (teamAScore >= m_pointsPerSet && teamBScore >= m_pointsPerSet)
            {
                if (teamAScore >= teamBScore + 2)
                {
                    winner = NetworkedTeam.Team.TeamA;
                    return true;
                }
                else if (teamBScore >= teamAScore + 2)
                {
                    winner = NetworkedTeam.Team.TeamB;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 单局完成处理
        /// </summary>
        private void OnSetCompleted(NetworkedTeam.Team winner)
        {
            if (!IsServer) return;

            Debug.Log($"[GameManager] 单局完成，获胜方: {winner}");

            // 暂停球的移动
            m_ballSpawner.DespawnAllBalls();

            // 更新统计数据
            if (m_statisticsTracker != null)
            {
                m_statisticsTracker.OnSetCompleted(winner);
            }

            // 获取当前统计数据
            var currentStats = m_statisticsTracker?.GetCurrentStatistics() ?? new GameStatistics();

            // 检查是否完成整场比赛
            bool isMatchComplete = CheckMatchComplete(currentStats);

            // 显示PostGame界面
            ShowPostGameUI(currentStats, isMatchComplete);
        }

        /// <summary>
        /// 检查整场比赛是否完成
        /// </summary>
        private bool CheckMatchComplete(GameStatistics stats)
        {
            return stats.PlayerASetsWon >= m_setsToWin || stats.PlayerBSetsWon >= m_setsToWin;
        }

        /// <summary>
        /// 显示PostGame界面
        /// </summary>
        private void ShowPostGameUI(GameStatistics stats, bool isMatchComplete)
        {
            if (m_postGameController != null)
            {
                m_postGameController.ShowPostGameUI(stats, isMatchComplete);
            }
            else
            {
                // 回退到原来的PostGame逻辑
                m_currentGamePhase.Value = GamePhase.PostGame;
            }
        }

        /// <summary>
        /// 下一局请求处理
        /// </summary>
        private void OnNextSetRequested()
        {
            Debug.Log("[GameManager] 处理下一局请求");

            if (!IsServer) return;

            // 获取当前统计数据
            var currentStats = m_statisticsTracker?.GetCurrentStatistics() ?? new GameStatistics();

            if (currentStats.IsMatchComplete)
            {
                // 如果比赛完成，重新开始整场比赛
                StartNewMatch();
            }
            else
            {
                // 开始下一局
                StartNextSet();
            }
        }

        /// <summary>
        /// 开始新的比赛
        /// </summary>
        private void StartNewMatch()
        {
            Debug.Log("[GameManager] 开始新比赛");

            // 重置统计数据
            if (m_statisticsTracker != null)
            {
                m_statisticsTracker.ResetMatchStatistics();
            }

            // 重置比分
            m_gameState.Score.Reset();

            // 隐藏PostGame界面
            if (m_postGameController != null)
            {
                m_postGameController.HidePostGameUI();
            }

            // 开始新游戏
            StartGame();
        }

        /// <summary>
        /// 开始下一局
        /// </summary>
        private void StartNextSet()
        {
            Debug.Log("[GameManager] 开始下一局");

            // 重置本局统计数据
            if (m_statisticsTracker != null)
            {
                m_statisticsTracker.ResetSetStatistics();
            }

            // 重置比分
            m_gameState.Score.Reset();

            // 隐藏PostGame界面
            if (m_postGameController != null)
            {
                m_postGameController.HidePostGameUI();
            }

            // 重新生成玩家并开始倒计时
            CheckPlayersSides();
            LockPlayersTeams();
            StartCountdown();
            RespawnAllPlayers();
        }

        /// <summary>
        /// 观众模式请求处理
        /// </summary>
        private void OnSpectatorModeRequested()
        {
            Debug.Log("[GameManager] 处理观众模式请求");
            // 这个功能主要在PostGameController中处理，这里只是记录日志
        }

        /// <summary>
        /// 退出请求处理
        /// </summary>
        private void OnExitRequested()
        {
            Debug.Log("[GameManager] 处理退出请求");
            // 这个功能主要在PostGameController中处理，这里只是记录日志
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        private void Update()
        {
            if (m_currentGamePhase.Value == GamePhase.InGame)
            {
                var timeLeft = m_gameEndTime.Value - NetworkManager.Singleton.ServerTime.Time;
                UpdateTimeInPhaseListener(Math.Max(0, timeLeft));

                if (timeLeft < 11)
                {
                    var seconds = Math.Max(0, (int)Math.Floor(timeLeft));
                    if (m_previousSecondsLeft != seconds)
                    {
                        TriggerEndGameCountdownBeep(seconds);
                    }

                    m_previousSecondsLeft = seconds;

                    if (IsServer)
                    {
                        if (timeLeft < 0)
                        {
                            GoToPostGame();
                        }
                    }
                }
            }
            else if (m_currentGamePhase.Value == GamePhase.PreGame)
            {
                if (NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    CheckPlayersSides();
                }
            }
        }

        /// <summary>
        /// 触发游戏结束倒计时音效
        /// </summary>
        private void TriggerEndGameCountdownBeep(int seconds)
        {
            if (seconds == 0)
            {
                m_courtAudioSource.PlayOneShot(m_highCountdownBeep);
            }
            else
            {
                m_courtAudioSource.PlayOneShot(m_lowCountdownBeep);
            }
        }

        /// <summary>
        /// 通知阶段监听器
        /// </summary>
        private void NotifyPhaseListener(GamePhase newphase)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseChanged(newphase);
            }
        }

        /// <summary>
        /// 更新阶段时间到监听器
        /// </summary>
        private void UpdateTimeInPhaseListener(double timeLeft)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnPhaseTimeUpdate(timeLeft);
            }
        }

        /// <summary>
        /// 通知队伍颜色监听器
        /// </summary>
        private void NotifyTeamColorListener(TeamColor teamColorA, TeamColor teamColorB)
        {
            foreach (var listener in m_phaseListeners)
            {
                listener.OnTeamColorUpdated(teamColorA, teamColorB);
            }
        }

        /// <summary>
        /// 锁定玩家队伍
        /// </summary>
        private void LockPlayersTeams()
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (m_playersTeamSelection.TryGetValue(clientId, out var team))
                {
                    var avatar = LocalPlayerEntities.Instance.GetPlayerObjects(clientId).Avatar;
                    if (avatar != null)
                    {
                        avatar.GetComponent<NetworkedTeam>().MyTeam = team;

                        var playerData = PongSessionManager.Instance.GetPlayerData(clientId);
                        if (playerData.HasValue)
                        {
                            var updatedData = playerData.Value;
                            updatedData.SelectedTeam = team;
                            PongSessionManager.Instance.SetPlayerData(clientId, updatedData);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查玩家阵营
        /// </summary>
        private void CheckPlayersSides()
        {
            var clientCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (m_playersTeamSelection.Count != clientCount)
            {
                m_playersTeamSelection.Clear();
            }

            for (var i = 0; i < clientCount; ++i)
            {
                var clientId = NetworkManager.Singleton.ConnectedClientsIds[i];
                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(clientId);
                var avatar = playerObjects.Avatar;
                if (avatar != null)
                {
                    var side = avatar.transform.position.z < 0
                        ? NetworkedTeam.Team.TeamA
                        : NetworkedTeam.Team.TeamB;

                    var color = side == NetworkedTeam.Team.TeamA ? TeamAColor : TeamBColor;

                    foreach (var colorComp in playerObjects.ColoringComponents)
                    {
                        colorComp.TeamColor = color;
                    }

                    m_playersTeamSelection[clientId] = side;
                }
            }
        }

        /// <summary>
        /// 重生所有玩家
        /// </summary>
        private void RespawnAllPlayers()
        {
            foreach (var clientId in LocalPlayerEntities.Instance.PlayerIds)
            {
                var allPlayerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(clientId);
                if (allPlayerObjects.Avatar)
                {
                    // 使用新的乒乓球生成系统获取重生点
                    GetPongRespawnPoint(clientId, allPlayerObjects.Avatar.GetComponent<NetworkedTeam>().MyTeam,
                        out var position, out var rotation);

                    // 仅发送给特定客户端
                    var clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                    };
                    OnRespawnClientRpc(position, rotation, m_currentGamePhase.Value, clientRpcParams);
                }
            }
        }

        /// <summary>
        /// 获取乒乓球游戏的重生点
        /// </summary>
        private void GetPongRespawnPoint(ulong clientId, NetworkedTeam.Team team, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
            var sessionManager = PongSessionManager.Instance;

            if (spawnConfig == null || sessionManager == null)
            {
                Debug.LogWarning("[GameManager] 未找到乒乓球生成配置或会话管理器");
                return;
            }

            var playerData = sessionManager.GetPlayerData(clientId);
            if (!playerData.HasValue)
            {
                Debug.LogWarning($"[GameManager] 未找到客户端 {clientId} 的玩家数据");
                return;
            }

            Transform spawnPoint = null;
            var currentMode = sessionManager.CurrentGameMode;

            if (playerData.Value.IsSpectator)
            {
                // 观众生成点
                spawnPoint = spawnConfig.GetSpectatorSpawnPoint(team);
            }
            else
            {
                // 玩家生成点
                spawnPoint = spawnConfig.GetPlayerSpawnPoint(currentMode, team, playerData.Value.TeamPosition);
            }

            if (spawnPoint != null)
            {
                position = spawnPoint.position;
                rotation = spawnPoint.rotation;
            }
            else
            {
                // 回退到队伍中心位置
                position = spawnConfig.GetTeamCenter(team);
                rotation = Quaternion.LookRotation(team == NetworkedTeam.Team.TeamA ? Vector3.forward : Vector3.back);
                Debug.LogWarning($"[GameManager] 未找到合适的生成点，使用队伍中心位置");
            }
        }

        /// <summary>
        /// 处理游戏中的主机迁移
        /// </summary>
        private void HandleInGameHostMigration(double timeRemaining)
        {
            m_currentGamePhase.Value = GamePhase.InGame;
            m_gameEndTime.Value = NetworkManager.Singleton.ServerTime.Time + timeRemaining;
            // 在乒乓球游戏中，球通常由玩家手动生成，而不是自动生成
            // 如果需要自动生成球，可以使用 m_ballSpawner.SpawnBallAtPosition()
        }

        /// <summary>
        /// 重生的客户端RPC
        /// </summary>
        [ClientRpc]
        private void OnRespawnClientRpc(Vector3 position, Quaternion rotation, GamePhase phase, ClientRpcParams rpcParams)
        {
            if (phase is GamePhase.PostGame or GamePhase.CountDown)
            {
                PlayerInputController.Instance.MovementEnabled = false;
            }
            PlayerMovement.Instance.TeleportTo(position, rotation);
        }

        /// <summary>
        /// 获取乒乓球生成管理器
        /// </summary>
        private PongPlayerSpawningManager GetPongSpawningManager()
        {
            return FindObjectOfType<PongPlayerSpawningManager>();
        }
    }
}