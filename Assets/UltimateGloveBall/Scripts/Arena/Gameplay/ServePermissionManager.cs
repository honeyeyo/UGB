// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 发球权限管理器 - 管理乒乓球游戏的发球权限和轮换逻辑
    /// 功能：
    /// 1. 区分比赛状态和练习状态的权限控制
    /// 2. 发球权轮换（每两分或每局）
    /// 3. 网络同步发球权状态
    /// 4. 集成计分系统
    /// </summary>
    public class ServePermissionManager : NetworkBehaviour
    {
        #region Singleton
        public static ServePermissionManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("比赛设置")]
        [SerializeField] private int servesPerPlayer = 2;          // 每人连续发球数（预留功能）
        [SerializeField] private int serveRotationScore = 2;       // 每几分轮换发球权
        [SerializeField] private int maxScore = 11;                // 最高分数
        [SerializeField] private bool enableDeuce = true;          // 是否启用平分规则

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = true;       // 启用调试日志
        [SerializeField] private bool showPermissionUI = true;     // 显示权限UI（预留功能）
        #endregion

        #region Network Variables
        [Header("网络状态")]
        public NetworkVariable<bool> IsInMatch = new(false);                    // 是否在比赛中
        public NetworkVariable<ulong> CurrentServerPlayerId = new(ulong.MaxValue); // 当前发球方ID
        public NetworkVariable<int> Player1Score = new(0);                      // 玩家1分数
        public NetworkVariable<int> Player2Score = new(0);                      // 玩家2分数
        public NetworkVariable<int> CurrentSet = new(1);                        // 当前局数
        public NetworkVariable<int> ServeCount = new(0);                        // 当前发球计数
        public NetworkVariable<GamePhase> CurrentGamePhase = new(GamePhase.Practice); // 游戏阶段
        #endregion

        #region Events
        public static event Action<ulong> OnServeRightChanged;                  // 发球权变更
        public static event Action<int, int> OnScoreUpdated;                    // 分数更新
        public static event Action<GamePhase> OnGamePhaseChanged;               // 游戏阶段变更
        public static event Action<ulong> OnServePermissionDenied;              // 发球权限被拒绝
        public static event Action<ulong> OnMatchStarted;                       // 比赛开始
        public static event Action<ulong> OnMatchEnded;                         // 比赛结束
        #endregion

        #region Private Fields
        private List<ulong> activePlayers = new();                             // 活跃玩家列表
        private ulong player1Id = ulong.MaxValue;                              // 玩家1 ID
        private ulong player2Id = ulong.MaxValue;                              // 玩家2 ID
        private Dictionary<ulong, string> playerNames = new();                 // 玩家名称映射
        private bool isInitialized = false;
        #endregion

        #region Enums
        public enum GamePhase
        {
            Practice,       // 练习模式
            PreMatch,       // 赛前准备
            InMatch,        // 比赛中
            PostMatch,      // 赛后
            Paused          // 暂停
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            InitializeManager();
        }
        #endregion

        #region NetworkBehaviour Overrides
        public override void OnNetworkSpawn()
        {
            // 订阅网络变量变化事件
            IsInMatch.OnValueChanged += OnMatchStateChanged;
            CurrentServerPlayerId.OnValueChanged += OnServerPlayerChanged;
            Player1Score.OnValueChanged += OnPlayer1ScoreChanged;
            Player2Score.OnValueChanged += OnPlayer2ScoreChanged;
            CurrentGamePhase.OnValueChanged += OnGamePhaseChanged_Internal;

            // 如果是服务器，初始化游戏状态
            if (IsServer)
            {
                InitializeServerState();
            }

            isInitialized = true;
            LogDebug("发球权限管理器已初始化");
        }

        public override void OnNetworkDespawn()
        {
            // 取消订阅事件
            IsInMatch.OnValueChanged -= OnMatchStateChanged;
            CurrentServerPlayerId.OnValueChanged -= OnServerPlayerChanged;
            Player1Score.OnValueChanged -= OnPlayer1ScoreChanged;
            Player2Score.OnValueChanged -= OnPlayer2ScoreChanged;
            CurrentGamePhase.OnValueChanged -= OnGamePhaseChanged_Internal;
        }
        #endregion

        #region Initialization
        private void InitializeManager()
        {
            // 查找活跃玩家
            RefreshActivePlayers();

            LogDebug($"发球权限管理器初始化完成，找到 {activePlayers.Count} 个玩家");
        }

        private void InitializeServerState()
        {
            // 设置初始游戏阶段
            CurrentGamePhase.Value = GamePhase.Practice;
            IsInMatch.Value = false;

            // 重置分数
            Player1Score.Value = 0;
            Player2Score.Value = 0;
            ServeCount.Value = 0;
            CurrentSet.Value = 1;

            LogDebug("服务器状态已初始化");
        }
        #endregion

        #region Serve Permission Logic
        /// <summary>
        /// 检查玩家是否可以发球
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可以发球</returns>
        public bool CanPlayerServe(ulong playerId)
        {
            if (!isInitialized) return false;

            // 练习模式：任何人都可以发球
            if (CurrentGamePhase.Value == GamePhase.Practice)
            {
                LogDebug($"练习模式 - 玩家 {playerId} 可以发球");
                return true;
            }

            // 比赛模式：只有当前发球方可以发球
            if (CurrentGamePhase.Value == GamePhase.InMatch)
            {
                bool canServe = playerId == CurrentServerPlayerId.Value;
                LogDebug($"比赛模式 - 玩家 {playerId} {(canServe ? "可以" : "不能")}发球 (当前发球方: {CurrentServerPlayerId.Value})");
                return canServe;
            }

            // 其他状态下不允许发球
            LogDebug($"游戏阶段 {CurrentGamePhase.Value} - 玩家 {playerId} 不能发球");
            return false;
        }

        /// <summary>
        /// 请求发球权限（客户端调用）
        /// </summary>
        /// <param name="playerId">请求的玩家ID</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestServePermissionServerRpc(ulong playerId = 0, ServerRpcParams rpcParams = default)
        {
            if (playerId == 0) playerId = rpcParams.Receive.SenderClientId;

            if (CanPlayerServe(playerId))
            {
                // 权限允许
                GrantServePermissionClientRpc(playerId);
            }
            else
            {
                // 权限拒绝
                DenyServePermissionClientRpc(playerId);
            }
        }

        [ClientRpc]
        private void GrantServePermissionClientRpc(ulong playerId)
        {
            LogDebug($"发球权限已授予玩家: {playerId}");
        }

        [ClientRpc]
        private void DenyServePermissionClientRpc(ulong playerId)
        {
            LogDebug($"发球权限被拒绝 - 玩家: {playerId}");
            OnServePermissionDenied?.Invoke(playerId);
        }
        #endregion

        #region Serve Rotation Logic
        /// <summary>
        /// 轮换发球权
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RotateServeRightServerRpc()
        {
            if (!IsServer) return;

            RefreshActivePlayers();
            if (activePlayers.Count < 2)
            {
                LogDebug("玩家数量不足，无法轮换发球权");
                return;
            }

            // 找到当前发球者的索引
            int currentIndex = activePlayers.FindIndex(p => p == CurrentServerPlayerId.Value);
            if (currentIndex == -1) currentIndex = 0; // 如果没找到，从第一个开始

            // 轮换到下一个玩家
            int nextIndex = (currentIndex + 1) % activePlayers.Count;
            ulong nextServer = activePlayers[nextIndex];

            // 更新发球权
            CurrentServerPlayerId.Value = nextServer;
            ServeCount.Value = 0; // 重置发球计数

            LogDebug($"发球权已轮换：{CurrentServerPlayerId.Value} -> {nextServer}");
        }

        /// <summary>
        /// 自动检查是否需要轮换发球权
        /// </summary>
        private void CheckServeRotation()
        {
            if (!IsServer || CurrentGamePhase.Value != GamePhase.InMatch) return;

            // 计算总分数
            int totalScore = Player1Score.Value + Player2Score.Value;

            // 每两分轮换一次发球权
            if (totalScore > 0 && totalScore % serveRotationScore == 0 && ServeCount.Value == 0)
            {
                RotateServeRightServerRpc();
            }
        }
        #endregion

        #region Score Management
        /// <summary>
        /// 更新分数
        /// </summary>
        /// <param name="scoringPlayerId">得分玩家ID</param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateScoreServerRpc(ulong scoringPlayerId)
        {
            if (!IsServer || CurrentGamePhase.Value != GamePhase.InMatch) return;

            // 确定哪个玩家得分
            if (scoringPlayerId == player1Id)
            {
                Player1Score.Value++;
            }
            else if (scoringPlayerId == player2Id)
            {
                Player2Score.Value++;
            }
            else
            {
                LogDebug($"未知玩家得分: {scoringPlayerId}");
                return;
            }

            LogDebug($"分数更新: 玩家1={Player1Score.Value}, 玩家2={Player2Score.Value}");

            // 检查是否需要轮换发球权
            CheckServeRotation();

            // 检查比赛是否结束
            CheckMatchEnd();
        }

        /// <summary>
        /// 检查比赛是否结束
        /// </summary>
        private void CheckMatchEnd()
        {
            int score1 = Player1Score.Value;
            int score2 = Player2Score.Value;

            // 基本胜利条件：11分且领先2分
            bool player1Wins = score1 >= maxScore && score1 - score2 >= 2;
            bool player2Wins = score2 >= maxScore && score2 - score1 >= 2;

            // 平分规则
            if (enableDeuce && score1 >= maxScore - 1 && score2 >= maxScore - 1)
            {
                // 平分状态：必须领先2分才能获胜
                player1Wins = score1 - score2 >= 2;
                player2Wins = score2 - score1 >= 2;
            }

            if (player1Wins || player2Wins)
            {
                ulong winnerId = player1Wins ? player1Id : player2Id;
                EndMatchServerRpc(winnerId);
            }
        }
        #endregion

        #region Match Control
        /// <summary>
        /// 开始比赛
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartMatchServerRpc()
        {
            if (!IsServer) return;

            RefreshActivePlayers();
            if (activePlayers.Count < 2)
            {
                LogDebug("玩家数量不足，无法开始比赛");
                return;
            }

            // 设置玩家
            player1Id = activePlayers[0];
            player2Id = activePlayers[1];

            // 随机选择首发球员
            ulong firstServer = UnityEngine.Random.Range(0, 2) == 0 ? player1Id : player2Id;
            CurrentServerPlayerId.Value = firstServer;

            // 重置状态
            Player1Score.Value = 0;
            Player2Score.Value = 0;
            ServeCount.Value = 0;
            CurrentSet.Value = 1;

            // 更新游戏状态
            CurrentGamePhase.Value = GamePhase.InMatch;
            IsInMatch.Value = true;

            LogDebug($"比赛开始 - 首发球员: {firstServer}");
            OnMatchStarted?.Invoke(firstServer);
        }

        /// <summary>
        /// 结束比赛
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EndMatchServerRpc(ulong winnerId = ulong.MaxValue)
        {
            if (!IsServer) return;

            CurrentGamePhase.Value = GamePhase.PostMatch;
            IsInMatch.Value = false;

            LogDebug($"比赛结束 - 获胜者: {winnerId}");
            OnMatchEnded?.Invoke(winnerId);
        }

        /// <summary>
        /// 切换到练习模式
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartPracticeModeServerRpc()
        {
            if (!IsServer) return;

            CurrentGamePhase.Value = GamePhase.Practice;
            IsInMatch.Value = false;

            // 清除发球权限制
            CurrentServerPlayerId.Value = ulong.MaxValue;

            LogDebug("已切换到练习模式");
        }
        #endregion

        #region Player Management
        /// <summary>
        /// 刷新活跃玩家列表
        /// </summary>
        private void RefreshActivePlayers()
        {
            activePlayers.Clear();

            // 查找所有网络玩家
            var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
            foreach (var client in connectedClients)
            {
                activePlayers.Add(client.ClientId);
            }

            LogDebug($"刷新玩家列表，找到 {activePlayers.Count} 个玩家");
        }

        /// <summary>
        /// 获取活跃玩家列表
        /// </summary>
        /// <returns>玩家ID列表</returns>
        public List<ulong> GetActivePlayers()
        {
            RefreshActivePlayers();
            return new List<ulong>(activePlayers);
        }

        /// <summary>
        /// 获取玩家名称
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家名称</returns>
        public string GetPlayerName(ulong playerId)
        {
            if (playerNames.TryGetValue(playerId, out string name))
            {
                return name;
            }
            return $"Player {playerId}";
        }

        /// <summary>
        /// 设置玩家名称
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="name">玩家名称</param>
        public void SetPlayerName(ulong playerId, string name)
        {
            playerNames[playerId] = name;
        }
        #endregion

        #region Network Variable Callbacks
        private void OnMatchStateChanged(bool previousValue, bool newValue)
        {
            LogDebug($"比赛状态变更: {previousValue} -> {newValue}");
        }

        private void OnServerPlayerChanged(ulong previousValue, ulong newValue)
        {
            LogDebug($"发球权变更: {previousValue} -> {newValue}");
            OnServeRightChanged?.Invoke(newValue);
        }

        private void OnPlayer1ScoreChanged(int previousValue, int newValue)
        {
            OnScoreUpdated?.Invoke(newValue, Player2Score.Value);
        }

        private void OnPlayer2ScoreChanged(int previousValue, int newValue)
        {
            OnScoreUpdated?.Invoke(Player1Score.Value, newValue);
        }

        private void OnGamePhaseChanged_Internal(GamePhase previousValue, GamePhase newValue)
        {
            LogDebug($"游戏阶段变更: {previousValue} -> {newValue}");
            OnGamePhaseChanged?.Invoke(newValue);
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 获取当前比赛信息
        /// </summary>
        /// <returns>比赛信息字符串</returns>
        public string GetMatchInfo()
        {
            string serverName = GetPlayerName(CurrentServerPlayerId.Value);
            return $"游戏阶段: {CurrentGamePhase.Value}\n" +
                   $"比赛状态: {(IsInMatch.Value ? "进行中" : "未开始")}\n" +
                   $"当前发球方: {serverName}\n" +
                   $"分数: {Player1Score.Value} - {Player2Score.Value}\n" +
                   $"发球计数: {ServeCount.Value}";
        }

        /// <summary>
        /// 检查是否为有效的比赛配置
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidMatchConfiguration()
        {
            RefreshActivePlayers();
            return activePlayers.Count >= 2;
        }
        #endregion

        #region Debug
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ServePermissionManager] {message}");
            }
        }
        #endregion
    }
}