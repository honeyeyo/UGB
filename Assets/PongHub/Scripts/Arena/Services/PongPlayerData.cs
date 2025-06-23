// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using UnityEngine;
using PongHub.Arena.Gameplay;
using Unity.Netcode;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球游戏模式枚举
    /// </summary>
    public enum PongGameMode
    {
        Waiting,        // 等待玩家
        Singles,        // 单打 (1v1)
        Doubles,        // 双打 (2v2)
        Spectator       // 观众模式
    }

    /// <summary>
    /// 匹配策略枚举
    /// </summary>
    public enum MatchmakingStrategy
    {
        Auto,           // 自动根据人数决定
        ForceSingles,   // 强制单打
        ForceDoubles    // 强制双打
    }

    /// <summary>
    /// 游戏大厅状态枚举
    /// </summary>
    public enum GameLobbyState
    {
        WaitingForPlayers,      // 等待玩家加入
        ModeSelection,          // 模式选择阶段
        TeamBalancing,          // 队伍平衡
        ReadyCheck,             // 准备确认
        GameStarting,           // 游戏开始
        InGame,                 // 游戏中
        PostGame               // 游戏结束
    }

    /// <summary>
    /// 乒乓球玩家数据结构
    /// 扩展原有ArenaPlayerData以支持乒乓球特殊功能
    /// </summary>
    [Serializable]
    public struct PongPlayerData : INetworkSerializable
    {
        #region Core Data
        public ulong ClientId;              // 客户端ID
        public string PlayerId;             // 玩家ID
        public bool IsConnected;            // 是否连接
        public bool IsSpectator;            // 是否为观众
        #endregion

        #region Team Data
        public NetworkedTeam.Team SelectedTeam;    // 选择的队伍
        public int TeamPosition;                   // 队伍内位置 (0=主位, 1=副位)
        public bool IsReady;                       // 是否准备就绪
        public bool IsRoomHost;                    // 是否为房主
        #endregion

        #region Spawn Data
        public int SpawnPointIndex;               // 生成点索引
        public Vector3 LastPosition;              // 最后位置 (用于重连)
        public bool PostGameWinnerSide;           // 游戏结束后是否在胜利方
        #endregion

        #region Game Data
        public PongGameMode PreferredMode;        // 偏好游戏模式
        public float SkillRating;                 // 技能评级 (用于平衡)
        public int WinCount;                      // 胜利次数
        public int LossCount;                     // 失败次数
        #endregion

        #region Constructors
        /// <summary>
        /// 普通玩家构造函数
        /// </summary>
        public PongPlayerData(ulong clientId, string playerId, bool isRoomHost = false)
        {
            ClientId = clientId;
            PlayerId = playerId;
            IsConnected = true;
            IsSpectator = false;
            SelectedTeam = NetworkedTeam.Team.NoTeam;
            TeamPosition = 0;
            IsReady = false;
            IsRoomHost = isRoomHost;
            SpawnPointIndex = -1;
            LastPosition = Vector3.zero;
            PostGameWinnerSide = false;
            PreferredMode = PongGameMode.Singles;
            SkillRating = 1000f; // 默认ELO评级
            WinCount = 0;
            LossCount = 0;
        }

        /// <summary>
        /// 观众构造函数
        /// </summary>
        public PongPlayerData(ulong clientId, string playerId, bool isSpectator, NetworkedTeam.Team preferredSide)
        {
            ClientId = clientId;
            PlayerId = playerId;
            IsConnected = true;
            IsSpectator = isSpectator;
            SelectedTeam = preferredSide;
            TeamPosition = 0;
            IsReady = true; // 观众总是准备就绪
            IsRoomHost = false;
            SpawnPointIndex = -1;
            LastPosition = Vector3.zero;
            PostGameWinnerSide = false;
            PreferredMode = PongGameMode.Spectator;
            SkillRating = 0f;
            WinCount = 0;
            LossCount = 0;
        }
        #endregion

        #region Network Serialization
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref IsConnected);
            serializer.SerializeValue(ref IsSpectator);
            serializer.SerializeValue(ref SelectedTeam);
            serializer.SerializeValue(ref TeamPosition);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref IsRoomHost);
            serializer.SerializeValue(ref SpawnPointIndex);
            serializer.SerializeValue(ref LastPosition);
            serializer.SerializeValue(ref PostGameWinnerSide);
            serializer.SerializeValue(ref PreferredMode);
            serializer.SerializeValue(ref SkillRating);
            serializer.SerializeValue(ref WinCount);
            serializer.SerializeValue(ref LossCount);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            var hostIndicator = IsRoomHost ? "👑 " : "";
            var readyIndicator = IsReady ? "✅" : "⏳";
            return $"{hostIndicator}{PlayerId} {readyIndicator}";
        }

        /// <summary>
        /// 获取技能等级
        /// </summary>
        public string GetSkillLevel()
        {
            return SkillRating switch
            {
                < 800 => "新手",
                < 1200 => "业余",
                < 1600 => "高级",
                < 2000 => "专家",
                _ => "大师"
            };
        }

        /// <summary>
        /// 获取胜率
        /// </summary>
        public float GetWinRate()
        {
            var totalGames = WinCount + LossCount;
            return totalGames > 0 ? (float)WinCount / totalGames : 0f;
        }

        /// <summary>
        /// 是否为活跃玩家（非观众且已连接）
        /// </summary>
        public bool IsActivePlayer => IsConnected && !IsSpectator;

        /// <summary>
        /// 是否可以开始游戏（已准备且为活跃玩家）
        /// </summary>
        public bool CanStartGame => IsActivePlayer && IsReady;
        #endregion
    }

    /// <summary>
    /// 乒乓球游戏事件系统
    /// </summary>
    public static class PongGameEvents
    {
        #region Game Mode Events
        public static Action<PongGameMode> OnGameModeChanged;
        public static Action<int> OnPlayerCountChanged;
        public static Action<MatchmakingStrategy> OnMatchmakingStrategyChanged;
        #endregion

        #region Team Events
        public static Action<string, NetworkedTeam.Team> OnPlayerTeamAssigned;
        public static Action<NetworkedTeam.Team> OnTeamReadyStateChanged;
        public static Action<string, bool> OnPlayerReadyStateChanged;
        #endregion

        #region Lobby Events
        public static Action<GameLobbyState> OnLobbyStateChanged;
        public static Action OnGameStartRequested;
        public static Action OnGameEndRequested;
        public static Action<string> OnPlayerJoined;
        public static Action<string> OnPlayerLeft;
        #endregion

        #region Host Events
        public static Action<string> OnRoomHostChanged;
        public static Action<MatchmakingStrategy> OnHostStrategyChanged;
        #endregion

        /// <summary>
        /// 清理所有事件订阅（防止内存泄漏）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnGameModeChanged = null;
            OnPlayerCountChanged = null;
            OnMatchmakingStrategyChanged = null;
            OnPlayerTeamAssigned = null;
            OnTeamReadyStateChanged = null;
            OnPlayerReadyStateChanged = null;
            OnLobbyStateChanged = null;
            OnGameStartRequested = null;
            OnGameEndRequested = null;
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnRoomHostChanged = null;
            OnHostStrategyChanged = null;
        }
    }
}