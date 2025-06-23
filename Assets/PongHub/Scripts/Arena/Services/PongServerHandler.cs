// Copyright (c) MagnusLab Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球服务器处理器
    /// 处理客户端连接、断开和网络事件
    /// 专门为乒乓球VR游戏优化
    /// </summary>
    public class PongServerHandler : NetworkBehaviour
    {
        #region Network Events
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // 注册网络管理器事件
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                // 注册乒乓球游戏事件
                PongGameEvents.OnGameStartRequested += HandleGameStart;
                PongGameEvents.OnGameEndRequested += HandleGameEnd;

                Debug.Log("[PongServerHandler] 服务器处理器已启动");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null)
            {
                // 清理网络管理器事件
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            // 清理乒乓球游戏事件
            PongGameEvents.OnGameStartRequested -= HandleGameStart;
            PongGameEvents.OnGameEndRequested -= HandleGameEnd;

            Debug.Log("[PongServerHandler] 服务器处理器已关闭");
        }
        #endregion

        #region Client Connection Handling
        /// <summary>
        /// 处理客户端连接
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[PongServerHandler] 客户端连接: {clientId}");

            // 通知客户端当前游戏状态
            NotifyClientOfGameStateClientRpc(
                PongSessionManager.Instance.CurrentGameMode,
                PongSessionManager.Instance.CurrentLobbyState,
                PongSessionManager.Instance.ActivePlayerCount,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId }
                    }
                }
            );
        }

        /// <summary>
        /// 处理客户端断开连接
        /// </summary>
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[PongServerHandler] 客户端断开: {clientId}");

            // 防止处理自己的断开连接
            if (clientId == OwnerClientId)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                return;
            }

            // 通知会话管理器处理断开连接
            PongSessionManager.Instance.DisconnectClient(clientId);

            // 检查是否需要暂停或结束游戏
            CheckGameContinuity();
        }
        #endregion

        #region Game Flow Handling
        /// <summary>
        /// 处理游戏开始请求
        /// </summary>
        private void HandleGameStart()
        {
            Debug.Log("[PongServerHandler] 处理游戏开始请求");

            // 验证游戏可以开始
            if (!PongSessionManager.Instance.CanStartGame)
            {
                Debug.LogWarning("[PongServerHandler] 游戏无法开始：条件不满足");
                return;
            }

            // 通知所有客户端游戏开始
            NotifyGameStartClientRpc();

            // 记录游戏开始时间
            Debug.Log($"[PongServerHandler] 游戏开始 - 模式: {PongSessionManager.Instance.CurrentGameMode}");
        }

        /// <summary>
        /// 处理游戏结束请求
        /// </summary>
        private void HandleGameEnd()
        {
            Debug.Log("[PongServerHandler] 处理游戏结束请求");

            // 通知所有客户端游戏结束
            NotifyGameEndClientRpc();

            // 重置游戏状态
            ResetGameState();
        }

        /// <summary>
        /// 检查游戏连续性
        /// </summary>
        private void CheckGameContinuity()
        {
            var activePlayerCount = PongSessionManager.Instance.ActivePlayerCount;
            var currentMode = PongSessionManager.Instance.CurrentGameMode;
            var currentState = PongSessionManager.Instance.CurrentLobbyState;

            // 如果游戏正在进行且玩家数量不足，暂停游戏
            if (currentState == GameLobbyState.InGame)
            {
                bool shouldPause = false;

                switch (currentMode)
                {
                    case PongGameMode.Singles when activePlayerCount < 2:
                        shouldPause = true;
                        break;
                    case PongGameMode.Doubles when activePlayerCount < 4:
                        shouldPause = true;
                        break;
                }

                if (shouldPause)
                {
                    Debug.Log("[PongServerHandler] 玩家数量不足，暂停游戏");
                    PauseGameDueToInsufficientPlayersClientRpc();
                }
            }
        }

        /// <summary>
        /// 重置游戏状态
        /// </summary>
        private void ResetGameState()
        {
            // 这里可以添加重置游戏状态的逻辑
            // 例如重置分数、重置球的位置等
            Debug.Log("[PongServerHandler] 游戏状态已重置");
        }
        #endregion

        #region Client RPCs
        /// <summary>
        /// 通知客户端当前游戏状态
        /// </summary>
        [ClientRpc]
        private void NotifyClientOfGameStateClientRpc(
            PongGameMode gameMode,
            GameLobbyState lobbyState,
            int playerCount,
            ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[PongServerHandler] 收到游戏状态更新 - 模式: {gameMode}, 状态: {lobbyState}, 玩家: {playerCount}");

            // 客户端可以根据这些信息更新UI
            PongGameEvents.OnGameModeChanged?.Invoke(gameMode);
            PongGameEvents.OnLobbyStateChanged?.Invoke(lobbyState);
            PongGameEvents.OnPlayerCountChanged?.Invoke(playerCount);
        }

        /// <summary>
        /// 通知客户端游戏开始
        /// </summary>
        [ClientRpc]
        private void NotifyGameStartClientRpc()
        {
            Debug.Log("[PongServerHandler] 收到游戏开始通知");

            // 客户端处理游戏开始逻辑
            // 例如隐藏大厅UI，显示游戏UI，启用游戏控制等
            HandleClientGameStart();
        }

        /// <summary>
        /// 通知客户端游戏结束
        /// </summary>
        [ClientRpc]
        private void NotifyGameEndClientRpc()
        {
            Debug.Log("[PongServerHandler] 收到游戏结束通知");

            // 客户端处理游戏结束逻辑
            HandleClientGameEnd();
        }

        /// <summary>
        /// 通知客户端由于玩家不足暂停游戏
        /// </summary>
        [ClientRpc]
        private void PauseGameDueToInsufficientPlayersClientRpc()
        {
            Debug.Log("[PongServerHandler] 由于玩家数量不足，游戏已暂停");

            // 客户端显示暂停信息
            HandleClientGamePause("等待玩家重新连接...");
        }
        #endregion

        #region Client-Side Handlers
        /// <summary>
        /// 客户端处理游戏开始
        /// </summary>
        private void HandleClientGameStart()
        {
            // 隐藏大厅UI
            // 显示游戏内UI
            // 启用游戏控制
            // 播放开始音效等

            PongGameEvents.OnGameStartRequested?.Invoke();
        }

        /// <summary>
        /// 客户端处理游戏结束
        /// </summary>
        private void HandleClientGameEnd()
        {
            // 显示游戏结果
            // 返回大厅UI
            // 禁用游戏控制
            // 播放结束音效等

            PongGameEvents.OnGameEndRequested?.Invoke();
        }

        /// <summary>
        /// 客户端处理游戏暂停
        /// </summary>
        private void HandleClientGamePause(string reason)
        {
            // 显示暂停信息
            // 暂停游戏逻辑
            // 显示等待界面

            Debug.Log($"[PongServerHandler] 游戏暂停: {reason}");
        }
        #endregion

        #region Server RPCs
        /// <summary>
        /// 请求开始游戏（客户端调用）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;

            // 只有房主可以开始游戏
            if (clientId == PongSessionManager.Instance.RoomHostClientId)
            {
                Debug.Log($"[PongServerHandler] 房主 {clientId} 请求开始游戏");
                PongGameEvents.OnGameStartRequested?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[PongServerHandler] 非房主客户端 {clientId} 尝试开始游戏");
            }
        }

        /// <summary>
        /// 请求结束游戏（客户端调用）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestEndGameServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;

            // 任何玩家都可以请求结束游戏（例如投降）
            Debug.Log($"[PongServerHandler] 客户端 {clientId} 请求结束游戏");
            PongGameEvents.OnGameEndRequested?.Invoke();
        }

        /// <summary>
        /// 玩家请求重新连接到游戏
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestReconnectToGameServerRpc(string playerId, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;

            Debug.Log($"[PongServerHandler] 客户端 {clientId} 请求重连，玩家ID: {playerId}");

            // 尝试重连玩家
            PongSessionManager.Instance.SetupPlayerData(clientId, playerId);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 获取连接的客户端数量
        /// </summary>
        public int GetConnectedClientCount()
        {
            return NetworkManager.Singleton.ConnectedClientsList.Count;
        }

        /// <summary>
        /// 检查客户端是否已连接
        /// </summary>
        public bool IsClientConnected(ulong clientId)
        {
            return NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId);
        }

        /// <summary>
        /// 强制断开客户端连接
        /// </summary>
        public void DisconnectClient(ulong clientId, string reason = "")
        {
            if (IsServer)
            {
                Debug.Log($"[PongServerHandler] 强制断开客户端 {clientId}, 原因: {reason}");
                NetworkManager.Singleton.DisconnectClient(clientId, reason);
            }
        }
        #endregion
    }
}