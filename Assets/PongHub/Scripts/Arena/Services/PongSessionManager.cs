// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using Meta.Utilities;
using UnityEngine;
using Unity.Netcode;
using PongHub.Arena.Gameplay;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球竞技场会话管理器
    /// 专门为乒乓球VR游戏设计，支持单打(1v1)和双打(2v2)模式
    /// 管理玩家匹配、队伍分配和游戏状态转换
    /// </summary>
    public class PongSessionManager : Singleton<PongSessionManager>
    {
        #region Network Variables
        [Header("网络同步数据")]
        private NetworkVariable<PongGameMode> m_currentGameMode = new(PongGameMode.Waiting);
        private NetworkVariable<MatchmakingStrategy> m_matchmakingStrategy = new(MatchmakingStrategy.Auto);
        private NetworkVariable<GameLobbyState> m_lobbyState = new(GameLobbyState.WaitingForPlayers);
        private NetworkVariable<ulong> m_roomHostClientId = new(0);
        private NetworkVariable<int> m_activePlayerCount = new(0);
        #endregion

        #region Private Fields
        /// <summary>
        /// 玩家数据字典 - 按玩家ID索引
        /// </summary>
        private readonly Dictionary<string, PongPlayerData> m_playerDataDict = new();

        /// <summary>
        /// 客户端ID到玩家ID的映射
        /// </summary>
        private readonly Dictionary<ulong, string> m_clientIdToPlayerId = new();

        /// <summary>
        /// 队伍A的玩家列表 (按位置排序)
        /// </summary>
        private readonly List<string> m_teamAPlayers = new();

        /// <summary>
        /// 队伍B的玩家列表 (按位置排序)
        /// </summary>
        private readonly List<string> m_teamBPlayers = new();

        /// <summary>
        /// 观众列表
        /// </summary>
        private readonly List<string> m_spectators = new();

        /// <summary>
        /// 准备确认超时计时器
        /// </summary>
        private float m_readyCheckTimer = 0f;
        private const float READY_CHECK_TIMEOUT = 30f;
        #endregion

        #region Properties
        /// <summary>
        /// 当前游戏模式
        /// </summary>
        public PongGameMode CurrentGameMode => m_currentGameMode.Value;

        /// <summary>
        /// 当前匹配策略
        /// </summary>
        public MatchmakingStrategy CurrentStrategy => m_matchmakingStrategy.Value;

        /// <summary>
        /// 当前大厅状态
        /// </summary>
        public GameLobbyState CurrentLobbyState => m_lobbyState.Value;

        /// <summary>
        /// 房主客户端ID
        /// </summary>
        public ulong RoomHostClientId => m_roomHostClientId.Value;

        /// <summary>
        /// 活跃玩家数量
        /// </summary>
        public int ActivePlayerCount => m_activePlayerCount.Value;

        /// <summary>
        /// 是否为房主
        /// </summary>
        public bool IsRoomHost => NetworkManager.Singleton.LocalClientId == m_roomHostClientId.Value;

        /// <summary>
        /// 游戏是否可以开始
        /// </summary>
        public bool CanStartGame => CurrentLobbyState == GameLobbyState.ReadyCheck && AllPlayersReady();
        #endregion

        #region Unity Lifecycle
        protected override void InternalAwake()
        {
            // 注册网络变量变化回调
            m_currentGameMode.OnValueChanged += OnGameModeChanged;
            m_matchmakingStrategy.OnValueChanged += OnMatchmakingStrategyChanged;
            m_lobbyState.OnValueChanged += OnLobbyStateChanged;
            m_roomHostClientId.OnValueChanged += OnRoomHostChanged;
            m_activePlayerCount.OnValueChanged += OnActivePlayerCountChanged;
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                UpdateServerLogic();
            }
        }

        private void OnDestroy()
        {
            PongGameEvents.ClearAllEvents();
        }
        #endregion

        #region Server Logic
        /// <summary>
        /// 服务器端逻辑更新
        /// </summary>
        private void UpdateServerLogic()
        {
            switch (m_lobbyState.Value)
            {
                case GameLobbyState.WaitingForPlayers:
                    CheckPlayerCountForModeSelection();
                    break;

                case GameLobbyState.ModeSelection:
                    if (ShouldAutoSelectMode())
                    {
                        AutoSelectGameMode();
                    }
                    break;

                case GameLobbyState.TeamBalancing:
                    PerformTeamBalancing();
                    break;

                case GameLobbyState.ReadyCheck:
                    UpdateReadyCheckTimer();
                    break;
            }
        }

        private void CheckPlayerCountForModeSelection()
        {
            var activePlayers = GetActivePlayerCount();

            if (activePlayers >= 2)
            {
                m_lobbyState.Value = GameLobbyState.ModeSelection;
            }
        }

        private bool ShouldAutoSelectMode()
        {
            return m_matchmakingStrategy.Value == MatchmakingStrategy.Auto ||
                   Time.time - m_readyCheckTimer > 10f; // 10秒后自动选择
        }

        private void AutoSelectGameMode()
        {
            var newMode = DetermineGameMode(GetActivePlayerCount(), m_matchmakingStrategy.Value);
            if (newMode != PongGameMode.Waiting)
            {
                m_currentGameMode.Value = newMode;
                m_lobbyState.Value = GameLobbyState.TeamBalancing;
            }
        }

        private void PerformTeamBalancing()
        {
            var activePlayers = GetActivePlayers();
            BalanceTeams(activePlayers, m_currentGameMode.Value);
            m_lobbyState.Value = GameLobbyState.ReadyCheck;
            m_readyCheckTimer = Time.time;
        }

        private void UpdateReadyCheckTimer()
        {
            if (AllPlayersReady())
            {
                m_lobbyState.Value = GameLobbyState.GameStarting;
                StartGameServerRpc();
            }
            else if (Time.time - m_readyCheckTimer > READY_CHECK_TIMEOUT)
            {
                // 超时，回到等待状态
                m_lobbyState.Value = GameLobbyState.WaitingForPlayers;
                ResetAllPlayerReadyStates();
            }
        }
        #endregion

        #region Player Management
        /// <summary>
        /// 设置玩家数据
        /// </summary>
        public void SetupPlayerData(ulong clientId, string playerId, bool isSpectator = false, bool isRoomHost = false)
        {
            if (IsDuplicateConnection(playerId))
            {
                Debug.LogError($"玩家已在游戏中: {playerId}");
                return;
            }

            var isReconnecting = false;
            PongPlayerData playerData;

            // 检查重连
            if (m_playerDataDict.ContainsKey(playerId))
            {
                if (!m_playerDataDict[playerId].IsConnected)
                {
                    isReconnecting = true;
                    playerData = m_playerDataDict[playerId];
                    playerData.ClientId = clientId;
                    playerData.IsConnected = true;
                }
                else
                {
                    Debug.LogWarning($"玩家 {playerId} 尝试重复连接");
                    return;
                }
            }
            else
            {
                // 新玩家
                if (isSpectator)
                {
                    var preferredSide = DeterminePreferredSpectatorSide();
                    playerData = new PongPlayerData(clientId, playerId, true, preferredSide);
                }
                else
                {
                    playerData = new PongPlayerData(clientId, playerId, isRoomHost);
                }

                // 设置房主
                if (isRoomHost || m_roomHostClientId.Value == 0)
                {
                    m_roomHostClientId.Value = clientId;
                    playerData.IsRoomHost = true;
                }
            }

            // 更新数据
            m_clientIdToPlayerId[clientId] = playerId;
            m_playerDataDict[playerId] = playerData;

            // 更新计数器
            UpdateActivePlayerCount();

            // 触发事件
            if (!isReconnecting)
            {
                PongGameEvents.OnPlayerJoined?.Invoke(playerId);
            }

            Debug.Log($"玩家 {playerId} {(isReconnecting ? "重连" : "加入")} 游戏 (观众: {isSpectator})");
        }

        /// <summary>
        /// 断开玩家连接
        /// </summary>
        public void DisconnectClient(ulong clientId)
        {
            if (m_clientIdToPlayerId.TryGetValue(clientId, out var playerId))
            {
                if (m_playerDataDict.TryGetValue(playerId, out var playerData))
                {
                    if (playerData.ClientId == clientId)
                    {
                        playerData.IsConnected = false;
                        m_playerDataDict[playerId] = playerData;

                        // 如果是房主断开，重新分配房主
                        if (playerData.IsRoomHost)
                        {
                            ReassignRoomHost();
                        }

                        // 从队伍中移除
                        RemovePlayerFromTeam(playerId);

                        // 更新计数器
                        UpdateActivePlayerCount();

                        // 触发事件
                        PongGameEvents.OnPlayerLeft?.Invoke(playerId);

                        Debug.Log($"玩家 {playerId} 断开连接");
                    }
                }

                m_clientIdToPlayerId.Remove(clientId);
            }
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        public PongPlayerData? GetPlayerData(string playerId)
        {
            return m_playerDataDict.TryGetValue(playerId, out var data) ? data : null;
        }

        /// <summary>
        /// 获取玩家数据（通过客户端ID）
        /// </summary>
        public PongPlayerData? GetPlayerData(ulong clientId)
        {
            var playerId = GetPlayerId(clientId);
            return playerId != null ? GetPlayerData(playerId) : null;
        }

        /// <summary>
        /// 获取玩家ID
        /// </summary>
        public string GetPlayerId(ulong clientId)
        {
            return m_clientIdToPlayerId.TryGetValue(clientId, out var playerId) ? playerId : null;
        }

        /// <summary>
        /// 设置玩家数据
        /// </summary>
        public void SetPlayerData(ulong clientId, PongPlayerData playerData)
        {
            if (m_clientIdToPlayerId.TryGetValue(clientId, out var playerId))
            {
                m_playerDataDict[playerId] = playerData;
            }
        }

        /// <summary>
        /// 检查是否为重复连接
        /// </summary>
        public bool IsDuplicateConnection(string playerId)
        {
            return m_playerDataDict.ContainsKey(playerId) && m_playerDataDict[playerId].IsConnected;
        }
        #endregion

        #region Team Management
        /// <summary>
        /// 队伍平衡算法
        /// </summary>
        private void BalanceTeams(List<PongPlayerData> players, PongGameMode mode)
        {
            ClearTeams();

            var activePlayers = players.Where(p => p.IsActivePlayer).ToList();

            switch (mode)
            {
                case PongGameMode.Singles:
                    AssignSinglesTeams(activePlayers);
                    break;

                case PongGameMode.Doubles:
                    AssignDoublesTeams(activePlayers);
                    break;
            }

            // 分配观众
            var spectators = players.Where(p => p.IsSpectator).ToList();
            foreach (var spectator in spectators)
            {
                m_spectators.Add(spectator.PlayerId);
            }
        }

        private void AssignSinglesTeams(List<PongPlayerData> players)
        {
            // 按技能评级排序，平衡分配
            players.Sort((a, b) => a.SkillRating.CompareTo(b.SkillRating));

            for (int i = 0; i < Mathf.Min(2, players.Count); i++)
            {
                var player = players[i];
                player.SelectedTeam = i == 0 ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
                player.TeamPosition = 0;
                m_playerDataDict[player.PlayerId] = player;

                if (i == 0)
                    m_teamAPlayers.Add(player.PlayerId);
                else
                    m_teamBPlayers.Add(player.PlayerId);

                PongGameEvents.OnPlayerTeamAssigned?.Invoke(player.PlayerId, player.SelectedTeam);
            }
        }

        private void AssignDoublesTeams(List<PongPlayerData> players)
        {
            // 按技能评级排序
            players.Sort((a, b) => a.SkillRating.CompareTo(b.SkillRating));

            // 分配策略：高低搭配确保平衡
            for (int i = 0; i < Mathf.Min(4, players.Count); i++)
            {
                var player = players[i];
                var teamAssignment = i switch
                {
                    0 => (NetworkedTeam.Team.TeamA, 0), // A队主位
                    1 => (NetworkedTeam.Team.TeamB, 0), // B队主位
                    2 => (NetworkedTeam.Team.TeamB, 1), // B队副位
                    3 => (NetworkedTeam.Team.TeamA, 1), // A队副位
                    _ => (NetworkedTeam.Team.NoTeam, 0)
                };

                player.SelectedTeam = teamAssignment.Item1;
                player.TeamPosition = teamAssignment.Item2;
                m_playerDataDict[player.PlayerId] = player;

                if (player.SelectedTeam == NetworkedTeam.Team.TeamA)
                    m_teamAPlayers.Add(player.PlayerId);
                else if (player.SelectedTeam == NetworkedTeam.Team.TeamB)
                    m_teamBPlayers.Add(player.PlayerId);

                PongGameEvents.OnPlayerTeamAssigned?.Invoke(player.PlayerId, player.SelectedTeam);
            }
        }

        private void ClearTeams()
        {
            m_teamAPlayers.Clear();
            m_teamBPlayers.Clear();
            m_spectators.Clear();
        }

        private void RemovePlayerFromTeam(string playerId)
        {
            m_teamAPlayers.Remove(playerId);
            m_teamBPlayers.Remove(playerId);
            m_spectators.Remove(playerId);
        }
        #endregion

        #region Ready System
        /// <summary>
        /// 设置玩家准备状态
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            var playerId = GetPlayerId(clientId);

            if (playerId != null && m_playerDataDict.TryGetValue(playerId, out var playerData))
            {
                playerData.IsReady = isReady;
                m_playerDataDict[playerId] = playerData;

                PongGameEvents.OnPlayerReadyStateChanged?.Invoke(playerId, isReady);
            }
        }

        private bool AllPlayersReady()
        {
            var activePlayers = GetActivePlayers();
            return activePlayers.Count > 0 && activePlayers.All(p => p.CanStartGame);
        }

        private void ResetAllPlayerReadyStates()
        {
            var playerIds = m_playerDataDict.Keys.ToList();
            foreach (var playerId in playerIds)
            {
                var playerData = m_playerDataDict[playerId];
                playerData.IsReady = false;
                m_playerDataDict[playerId] = playerData;
            }
        }
        #endregion

        #region Game Mode Logic
        /// <summary>
        /// 设置匹配策略（仅房主可用）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetMatchmakingStrategyServerRpc(MatchmakingStrategy strategy, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            if (clientId == m_roomHostClientId.Value)
            {
                m_matchmakingStrategy.Value = strategy;
            }
        }

        /// <summary>
        /// 根据玩家数量和策略确定游戏模式
        /// </summary>
        private PongGameMode DetermineGameMode(int playerCount, MatchmakingStrategy strategy)
        {
            return strategy switch
            {
                MatchmakingStrategy.Auto => playerCount switch
                {
                    2 => PongGameMode.Singles,
                    3 => PongGameMode.Singles, // 1人观众
                    >= 4 => PongGameMode.Doubles, // 4人双打，多余做观众
                    _ => PongGameMode.Waiting
                },
                MatchmakingStrategy.ForceSingles => playerCount >= 2
                    ? PongGameMode.Singles
                    : PongGameMode.Waiting,
                MatchmakingStrategy.ForceDoubles => playerCount >= 4
                    ? PongGameMode.Doubles
                    : PongGameMode.Waiting,
                _ => PongGameMode.Waiting
            };
        }
        #endregion

        #region Utility Methods
        private List<PongPlayerData> GetActivePlayers()
        {
            return m_playerDataDict.Values.Where(p => p.IsActivePlayer).ToList();
        }

        private int GetActivePlayerCount()
        {
            return m_playerDataDict.Values.Count(p => p.IsActivePlayer);
        }

        private void UpdateActivePlayerCount()
        {
            m_activePlayerCount.Value = GetActivePlayerCount();
        }

        private NetworkedTeam.Team DeterminePreferredSpectatorSide()
        {
            // 简单策略：选择观众较少的一边
            var aCount = m_playerDataDict.Values.Count(p => p.IsSpectator && p.SelectedTeam == NetworkedTeam.Team.TeamA);
            var bCount = m_playerDataDict.Values.Count(p => p.IsSpectator && p.SelectedTeam == NetworkedTeam.Team.TeamB);

            return aCount <= bCount ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
        }

        private void ReassignRoomHost()
        {
            var activePlayers = GetActivePlayers();
            if (activePlayers.Count > 0)
            {
                var newHost = activePlayers.First();
                newHost.IsRoomHost = true;
                m_playerDataDict[newHost.PlayerId] = newHost;
                m_roomHostClientId.Value = newHost.ClientId;

                PongGameEvents.OnRoomHostChanged?.Invoke(newHost.PlayerId);
            }
            else
            {
                m_roomHostClientId.Value = 0;
            }
        }
        #endregion

        #region Network Callbacks
        private void OnGameModeChanged(PongGameMode previous, PongGameMode current)
        {
            PongGameEvents.OnGameModeChanged?.Invoke(current);
        }

        private void OnMatchmakingStrategyChanged(MatchmakingStrategy previous, MatchmakingStrategy current)
        {
            PongGameEvents.OnMatchmakingStrategyChanged?.Invoke(current);
        }

        private void OnLobbyStateChanged(GameLobbyState previous, GameLobbyState current)
        {
            PongGameEvents.OnLobbyStateChanged?.Invoke(current);
        }

        private void OnRoomHostChanged(ulong previous, ulong current)
        {
            var playerId = GetPlayerId(current);
            if (playerId != null)
            {
                PongGameEvents.OnRoomHostChanged?.Invoke(playerId);
            }
        }

        private void OnActivePlayerCountChanged(int previous, int current)
        {
            PongGameEvents.OnPlayerCountChanged?.Invoke(current);
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc()
        {
            PongGameEvents.OnGameStartRequested?.Invoke();
        }
        #endregion
    }
}