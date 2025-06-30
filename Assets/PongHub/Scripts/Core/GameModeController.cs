using UnityEngine;
using Unity.Netcode;
using System;
using PongHub.Arena.Services;

namespace PongHub.Core
{
    /// <summary>
    /// 游戏模式枚举
    /// </summary>
    public enum GameMode
    {
        Auto,           // 自动检测（优先离线）
        ForceOffline,   // 强制离线模式
        ForceOnline,    // 强制在线模式
        Hybrid          // 混合模式（可切换）
    }

    /// <summary>
    /// 网络连接状态枚举
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,   // 未连接
        Connecting,     // 连接中
        Connected,      // 已连接
        Reconnecting,   // 重连中
        Failed          // 连接失败
    }

    /// <summary>
    /// 游戏模式控制器
    /// 专注于管理离线/在线模式切换，与现有的GameManager、MatchManager、ScoreManager协作
    /// </summary>
    public class GameModeController : MonoBehaviour
    {
        #region 设置
        [Header("模式设置")]
        [SerializeField] private GameMode gameMode = GameMode.Auto;
        [SerializeField] private bool preferOfflineMode = true;        // 优先使用离线模式
        [SerializeField] private float networkTimeoutSeconds = 10f;    // 网络连接超时时间
        [SerializeField] private float reconnectInterval = 5f;         // 重连间隔

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoSwitchOnNetworkChange = true;
        #endregion

        #region 私有变量
        private ConnectionState connectionState = ConnectionState.Disconnected;
        private bool isInitialized = false;
        private float lastConnectionAttempt = 0f;
        private int reconnectAttempts = 0;
        private const int maxReconnectAttempts = 3;

        // 引用现有管理器
        private MatchManager matchManager;
        private ScoreManager scoreManager;
        #endregion

        #region 事件系统
        public static event Action<GameMode> OnGameModeChanged;
        public static event Action<ConnectionState> OnConnectionStateChanged;
        public static event Action<bool> OnModeTransition; // true = 切换到在线, false = 切换到离线
        public static event Action<string> OnModeTransitionFailed;
        #endregion

        #region 属性
        public GameMode CurrentGameMode => gameMode;
        public ConnectionState CurrentConnectionState => connectionState;
        public bool IsOnlineMode => IsNetworkConnected();
        public bool IsOfflineMode => !IsNetworkConnected();
        public bool IsInitialized => isInitialized;
        public bool CanPause => IsOfflineMode; // 只有离线模式支持暂停
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializeReferences();
        }

        private void Start()
        {
            InitializeController();
        }

        private void Update()
        {
            UpdateConnectionState();
            HandleAutoModeSwitch();
        }

        private void OnDestroy()
        {
            CleanupEvents();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeReferences()
        {
            // 获取现有管理器的引用
            matchManager = MatchManager.Instance;
            scoreManager = ScoreManager.Instance;
        }

        /// <summary>
        /// 初始化控制器
        /// </summary>
        private void InitializeController()
        {
            RegisterNetworkEvents();
            DetermineInitialMode();
            isInitialized = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 初始化完成 - 模式: {gameMode}, 状态: {(IsOfflineMode ? "离线" : "在线")}");
            }
        }

        /// <summary>
        /// 注册网络事件
        /// </summary>
        private void RegisterNetworkEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        /// <summary>
        /// 确定初始模式
        /// </summary>
        private void DetermineInitialMode()
        {
            switch (gameMode)
            {
                case GameMode.Auto:
                    SwitchToMode(preferOfflineMode ? GameMode.ForceOffline : GameMode.ForceOnline);
                    break;

                case GameMode.ForceOffline:
                    EnableOfflineMode();
                    break;

                case GameMode.ForceOnline:
                    EnableNetworkMode();
                    break;

                case GameMode.Hybrid:
                    // 混合模式初始化 - 根据当前网络状态设置
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[GameModeController] 初始化混合模式 - 当前: {(IsOfflineMode ? "离线" : "在线")}");
                    }
                    break;
            }
        }
        #endregion

        #region 模式切换
        /// <summary>
        /// 切换到指定模式
        /// </summary>
        public void SwitchToMode(GameMode newMode)
        {
            if (gameMode == newMode) return;

            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 切换模式: {gameMode} -> {newMode}");
            }

            var previousMode = gameMode;
            gameMode = newMode;

            try
            {
                switch (newMode)
                {
                    case GameMode.ForceOffline:
                        EnableOfflineMode();
                        break;

                    case GameMode.ForceOnline:
                        EnableNetworkMode();
                        break;

                    case GameMode.Hybrid:
                        // 混合模式只需要记录日志，实际网络状态由自动切换处理
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[GameModeController] 已启用混合模式 - 当前: {(IsOfflineMode ? "离线" : "在线")}");
                        }
                        break;

                    case GameMode.Auto:
                        EnableAutoMode();
                        break;
                }

                OnGameModeChanged?.Invoke(newMode);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameModeController] 模式切换失败: {e.Message}");
                gameMode = previousMode; // 回滚
                OnModeTransitionFailed?.Invoke($"模式切换失败: {e.Message}");
            }
        }

        /// <summary>
        /// 启用离线模式
        /// </summary>
        private void EnableOfflineMode()
        {
            if (IsNetworkConnected())
            {
                DisconnectFromNetwork();
            }

            OnModeTransition?.Invoke(false);

            if (enableDebugLogs)
            {
                Debug.Log("[GameModeController] 已启用离线模式");
            }
        }

        /// <summary>
        /// 启用网络模式
        /// </summary>
        private void EnableNetworkMode()
        {
            if (!IsNetworkConnected())
            {
                ConnectToNetwork();
            }

            if (IsNetworkConnected())
            {
                OnModeTransition?.Invoke(true);
            }
            else
            {
                // 网络连接失败，回退到离线模式
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[GameModeController] 网络连接失败，回退到离线模式");
                }
                OnModeTransitionFailed?.Invoke("网络连接失败");
            }
        }

        /// <summary>
        /// 启用自动模式
        /// </summary>
        private void EnableAutoMode()
        {
            // 自动模式优先选择偏好的模式
            if (preferOfflineMode)
            {
                EnableOfflineMode();
            }
            else
            {
                EnableNetworkMode();
            }
        }
        #endregion

        #region 网络管理
        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionState()
        {
            var newState = GetCurrentConnectionState();
            if (newState != connectionState)
            {
                var previousState = connectionState;
                connectionState = newState;
                OnConnectionStateChanged?.Invoke(newState);

                if (enableDebugLogs)
                {
                    Debug.Log($"[GameModeController] 网络状态变化: {previousState} -> {newState}");
                }
            }
        }

        /// <summary>
        /// 获取当前网络状态
        /// </summary>
        private ConnectionState GetCurrentConnectionState()
        {
            if (NetworkManager.Singleton == null)
            {
                return ConnectionState.Disconnected;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                return ConnectionState.Connected;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                return ConnectionState.Connected;
            }

            // 检查是否正在连接
            if (Time.time - lastConnectionAttempt < networkTimeoutSeconds)
            {
                return ConnectionState.Connecting;
            }

            return ConnectionState.Disconnected;
        }

        /// <summary>
        /// 连接到网络
        /// </summary>
        private void ConnectToNetwork()
        {
            if (NetworkManager.Singleton == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[GameModeController] NetworkManager未找到");
                }
                return;
            }

            lastConnectionAttempt = Time.time;
            connectionState = ConnectionState.Connecting;

            // 尝试作为客户端连接
            if (!NetworkManager.Singleton.StartClient())
            {
                // 如果客户端连接失败，尝试作为主机
                if (!NetworkManager.Singleton.StartHost())
                {
                    connectionState = ConnectionState.Failed;
                    if (enableDebugLogs)
                    {
                        Debug.LogError("[GameModeController] 网络连接失败");
                    }
                }
            }
        }

        /// <summary>
        /// 断开网络连接
        /// </summary>
        private void DisconnectFromNetwork()
        {
            if (NetworkManager.Singleton != null && IsNetworkConnected())
            {
                NetworkManager.Singleton.Shutdown();
                connectionState = ConnectionState.Disconnected;

                if (enableDebugLogs)
                {
                    Debug.Log("[GameModeController] 已断开网络连接");
                }
            }
        }

        /// <summary>
        /// 检查是否已连接到网络
        /// </summary>
        private bool IsNetworkConnected()
        {
            return NetworkManager.Singleton != null &&
                   (NetworkManager.Singleton.IsConnectedClient ||
                    NetworkManager.Singleton.IsHost ||
                    NetworkManager.Singleton.IsServer);
        }

        /// <summary>
        /// 处理自动模式切换
        /// </summary>
        private void HandleAutoModeSwitch()
        {
            if (!autoSwitchOnNetworkChange || gameMode != GameMode.Hybrid) return;

            // 在混合模式下，根据网络状态自动切换
            bool shouldUseOnline = IsNetworkConnected();
            bool currentlyOnline = IsOnlineMode;

            if (shouldUseOnline != currentlyOnline)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[GameModeController] 混合模式自动切换到: {(shouldUseOnline ? "在线" : "离线")}");
                }
            }
        }
        #endregion

        #region 网络事件处理
        /// <summary>
        /// 客户端连接回调
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 客户端连接: {clientId}");
            }

            connectionState = ConnectionState.Connected;
            reconnectAttempts = 0;
        }

        /// <summary>
        /// 客户端断开回调
        /// </summary>
        private void OnClientDisconnected(ulong clientId)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 客户端断开: {clientId}");
            }

            connectionState = ConnectionState.Disconnected;

            // 尝试重连（如果不是强制离线模式）
            if (gameMode != GameMode.ForceOffline && reconnectAttempts < maxReconnectAttempts)
            {
                StartReconnect();
            }
        }

        /// <summary>
        /// 开始重连
        /// </summary>
        private void StartReconnect()
        {
            reconnectAttempts++;
            connectionState = ConnectionState.Reconnecting;

            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 开始重连 (尝试 {reconnectAttempts}/{maxReconnectAttempts})");
            }

            Invoke(nameof(AttemptReconnect), reconnectInterval);
        }

        /// <summary>
        /// 尝试重连
        /// </summary>
        private void AttemptReconnect()
        {
            if (gameMode == GameMode.ForceOffline) return;

            ConnectToNetwork();
        }
        #endregion

        #region 暂停控制
        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (!CanPause)
            {
                Debug.LogWarning("[GameModeController] 当前模式不支持暂停");
                return;
            }

            bool isPaused = Time.timeScale == 0f;
            Time.timeScale = isPaused ? 1f : 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"[GameModeController] 暂停状态: {(Time.timeScale == 0f ? "已暂停" : "已恢复")}");
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void Pause()
        {
            if (!CanPause) return;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void Resume()
        {
            Time.timeScale = 1f;
        }
        #endregion

        #region 公共接口
        /// <summary>
        /// 强制切换到离线模式
        /// </summary>
        public void ForceOfflineMode()
        {
            SwitchToMode(GameMode.ForceOffline);
        }

        /// <summary>
        /// 强制切换到在线模式
        /// </summary>
        public void ForceOnlineMode()
        {
            SwitchToMode(GameMode.ForceOnline);
        }

        /// <summary>
        /// 启用混合模式
        /// </summary>
        public void EnableHybridMode()
        {
            SwitchToMode(GameMode.Hybrid);
        }

        /// <summary>
        /// 设置网络偏好
        /// </summary>
        public void SetNetworkPreference(bool preferOffline)
        {
            preferOfflineMode = preferOffline;

            if (gameMode == GameMode.Auto)
            {
                EnableAutoMode();
            }
        }

        /// <summary>
        /// 获取当前模式信息
        /// </summary>
        public string GetModeInfo()
        {
            return $"模式: {gameMode}\n" +
                   $"网络状态: {connectionState}\n" +
                   $"当前状态: {(IsOfflineMode ? "离线" : "在线")}\n" +
                   $"可暂停: {CanPause}\n" +
                   $"重连次数: {reconnectAttempts}/{maxReconnectAttempts}";
        }

        /// <summary>
        /// 重置连接状态
        /// </summary>
        public void ResetConnectionState()
        {
            reconnectAttempts = 0;
            connectionState = ConnectionState.Disconnected;

            if (enableDebugLogs)
            {
                Debug.Log("[GameModeController] 连接状态已重置");
            }
        }
        #endregion

        #region 清理
        /// <summary>
        /// 清理事件订阅
        /// </summary>
        private void CleanupEvents()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            OnGameModeChanged = null;
            OnConnectionStateChanged = null;
            OnModeTransition = null;
            OnModeTransitionFailed = null;
        }
        #endregion
    }
}