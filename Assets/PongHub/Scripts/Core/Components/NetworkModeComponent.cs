using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace PongHub.Core.Components
{
    /// <summary>
    /// 网络模式组件
    /// 管理网络模式下的游戏逻辑，包括网络连接、状态同步、多人游戏逻辑等
    /// </summary>
    public class NetworkModeComponent : NetworkBehaviour, IGameModeComponent
    {
        [Header("网络模式设置")]
        [SerializeField]
        [Tooltip("Enable Network Sync / 启用网络同步 - Enable automatic network state synchronization")]
        private bool m_enableNetworkSync = true;

        [SerializeField]
        [Tooltip("Network Only Objects / 网络模式专用对象 - GameObjects that are only active in network mode")]
        private GameObject[] m_networkOnlyObjects;

        [SerializeField]
        [Tooltip("Network Only Components / 网络模式专用组件 - MonoBehaviours that are only enabled in network mode")]
        private MonoBehaviour[] m_networkOnlyComponents;

        [Header("同步设置")]
        [SerializeField]
        [Tooltip("Sync Transform / 同步变换 - Synchronize transform data across network")]
        private bool m_syncTransform = true;

        [SerializeField]
        [Tooltip("Sync Animation / 同步动画 - Synchronize animation states across network")]
        private bool m_syncAnimation = true;

        [SerializeField]
        [Tooltip("Sync Rate / 同步频率 - Network synchronization rate per second")]
        [Range(10f, 120f)]
        private float m_syncRate = 60f;

        [Header("网络连接设置")]
        [SerializeField]
        [Tooltip("Auto Connect / 自动连接 - Automatically connect to network when enabled")]
        private bool m_autoConnect = true;

        [SerializeField]
        [Tooltip("Connection Timeout / 连接超时 - Maximum time to wait for network connection")]
        private float m_connectionTimeout = 10f;

        [SerializeField]
        [Tooltip("Max Players / 最大玩家数 - Maximum number of players in network session")]
        private int m_maxPlayers = 2;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for network mode operations")]
        private bool m_debugMode = false;

        // 网络状态
        private bool m_isNetworkActive = false;
        private bool m_isConnecting = false;
        private float m_connectionStartTime = 0f;

        // 同步状态
        private Dictionary<string, object> m_syncedStates = new Dictionary<string, object>();
        private Coroutine m_syncCoroutine = null;

        #region IGameModeComponent 实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            bool shouldBeActive = IsActiveInMode(newMode);

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 模式切换: {previousMode} -> {newMode}, 激活状态: {shouldBeActive}");
            }

            // 启用/禁用网络模式专用对象
            foreach (var obj in m_networkOnlyObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(shouldBeActive);
                }
            }

            // 启用/禁用网络模式专用组件
            foreach (var component in m_networkOnlyComponents)
            {
                if (component != null)
                {
                    component.enabled = shouldBeActive;
                }
            }

            // 根据模式配置网络
            if (shouldBeActive)
            {
                EnableNetworkMode();
            }
            else
            {
                DisableNetworkMode();
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            return mode == GameMode.Network || mode == GameMode.Multiplayer;
        }

        #endregion

        #region Unity 生命周期

        private void Start()
        {
            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogWarning("[NetworkModeComponent] GameModeManager实例不存在，延迟注册");
                Invoke(nameof(RegisterWithDelay), 0.5f);
            }
        }

        private void Update()
        {
            // 检查连接超时
            if (m_isConnecting && Time.time - m_connectionStartTime > m_connectionTimeout)
            {
                if (m_debugMode)
                {
                    Debug.LogWarning("[NetworkModeComponent] 网络连接超时");
                }
                HandleConnectionTimeout();
            }
        }

        private void OnDestroy()
        {
            // 停止同步协程
            if (m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
                m_syncCoroutine = null;
            }

            // 从GameModeManager注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region NetworkBehaviour 重写

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 网络对象生成 - IsOwner: {IsOwner}, IsServer: {IsServer}");
            }

            // 网络对象生成后的初始化
            m_isNetworkActive = true;
            m_isConnecting = false;

            // 开始同步协程
            if (m_enableNetworkSync && IsOwner)
            {
                StartSyncCoroutine();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 网络对象销毁");
            }

            m_isNetworkActive = false;

            // 停止同步协程
            if (m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
                m_syncCoroutine = null;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 延迟注册到GameModeManager
        /// </summary>
        private void RegisterWithDelay()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogError("[NetworkModeComponent] 无法找到GameModeManager实例");
            }
        }

        /// <summary>
        /// 启用网络模式
        /// </summary>
        private void EnableNetworkMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 启用网络模式");
            }

            // 如果配置了自动连接，尝试建立网络连接
            if (m_autoConnect && !m_isNetworkActive)
            {
                StartNetworkConnection();
            }

            // 配置网络物理模拟
            ConfigureNetworkPhysics();

            // 初始化网络游戏玩法
            InitializeNetworkGameplay();
        }

        /// <summary>
        /// 禁用网络模式
        /// </summary>
        private void DisableNetworkMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 禁用网络模式");
            }

            // 断开网络连接
            DisconnectFromNetwork();

            // 清理网络模式状态
            CleanupNetworkGameplay();
        }

        /// <summary>
        /// 开始网络连接
        /// </summary>
        private void StartNetworkConnection()
        {
            if (m_isConnecting || m_isNetworkActive)
            {
                if (m_debugMode)
                {
                    Debug.LogWarning("[NetworkModeComponent] 网络已连接或正在连接中");
                }
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 开始建立网络连接");
            }

            m_isConnecting = true;
            m_connectionStartTime = Time.time;

            // 尝试启动网络管理器
            if (NetworkManager.Singleton != null)
            {
                // 注册网络事件
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                // 尝试作为主机启动
                if (!NetworkManager.Singleton.StartHost())
                {
                    // 如果主机启动失败，尝试作为客户端连接
                    if (!NetworkManager.Singleton.StartClient())
                    {
                        HandleConnectionFailure("无法启动网络连接");
                    }
                }
            }
            else
            {
                HandleConnectionFailure("NetworkManager不存在");
            }
        }

        /// <summary>
        /// 断开网络连接
        /// </summary>
        private void DisconnectFromNetwork()
        {
            if (NetworkManager.Singleton != null && m_isNetworkActive)
            {
                // 注销网络事件
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

                // 关闭网络连接
                NetworkManager.Singleton.Shutdown();

                if (m_debugMode)
                {
                    Debug.Log("[NetworkModeComponent] 已断开网络连接");
                }
            }

            m_isNetworkActive = false;
            m_isConnecting = false;
        }

        /// <summary>
        /// 处理连接超时
        /// </summary>
        private void HandleConnectionTimeout()
        {
            HandleConnectionFailure("连接超时");
        }

        /// <summary>
        /// 处理连接失败
        /// </summary>
        private void HandleConnectionFailure(string reason)
        {
            if (m_debugMode)
            {
                Debug.LogError($"[NetworkModeComponent] 网络连接失败: {reason}");
            }

            m_isConnecting = false;
            m_isNetworkActive = false;

            // 可以在这里实现回退到单机模式的逻辑
            // 例如：GameModeManager.Instance.SwitchToMode(GameMode.Local);
        }

        /// <summary>
        /// 配置网络物理模拟
        /// </summary>
        private void ConfigureNetworkPhysics()
        {
            // TODO: 配置网络物理参数
            // 例如：设置网络物理同步、碰撞检测、球的网络属性等
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 网络物理模拟已配置");
            }
        }

        /// <summary>
        /// 初始化网络游戏玩法
        /// </summary>
        private void InitializeNetworkGameplay()
        {
            // TODO: 初始化网络游戏特定的功能
            // 例如：同步分数、设置多人游戏规则、配置玩家角色等
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 网络游戏玩法已初始化");
            }
        }

        /// <summary>
        /// 清理网络游戏状态
        /// </summary>
        private void CleanupNetworkGameplay()
        {
            // TODO: 清理网络模式的游戏状态
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 网络游戏状态已清理");
            }
        }

        /// <summary>
        /// 开始同步协程
        /// </summary>
        private void StartSyncCoroutine()
        {
            if (m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
            }

            m_syncCoroutine = StartCoroutine(SyncStateCoroutine());
        }

        /// <summary>
        /// 状态同步协程
        /// </summary>
        private IEnumerator SyncStateCoroutine()
        {
            float syncInterval = 1f / m_syncRate;

            while (m_isNetworkActive && IsOwner)
            {
                SyncComponentState();
                yield return new WaitForSeconds(syncInterval);
            }
        }

        #endregion

        #region 网络事件处理

        /// <summary>
        /// 客户端连接成功
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 客户端连接成功: {clientId}");
            }

            m_isConnecting = false;
            m_isNetworkActive = true;
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        private void OnClientDisconnected(ulong clientId)
        {
            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 客户端断开连接: {clientId}");
            }

            // 如果是本地客户端断开，重置状态
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                m_isNetworkActive = false;
                m_isConnecting = false;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 同步组件状态
        /// </summary>
        public void SyncComponentState()
        {
            if (!m_isNetworkActive || !IsOwner)
                return;

            if (m_syncTransform)
            {
                SyncTransformData();
            }

            if (m_syncAnimation)
            {
                SyncAnimationData();
            }

            // 同步自定义状态
            SyncCustomStates();
        }

        /// <summary>
        /// 设置网络同步启用状态
        /// </summary>
        /// <param name="enabled">是否启用同步</param>
        public void SetNetworkSyncEnabled(bool enabled)
        {
            m_enableNetworkSync = enabled;

            if (enabled && m_isNetworkActive && IsOwner)
            {
                StartSyncCoroutine();
            }
            else if (m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
                m_syncCoroutine = null;
            }
        }

        /// <summary>
        /// 设置同步频率
        /// </summary>
        /// <param name="rate">同步频率（Hz）</param>
        public void SetSyncRate(float rate)
        {
            m_syncRate = Mathf.Clamp(rate, 10f, 120f);

            // 如果正在同步，重新启动协程以应用新频率
            if (m_syncCoroutine != null)
            {
                StartSyncCoroutine();
            }
        }

        /// <summary>
        /// 获取网络状态信息
        /// </summary>
        public string GetNetworkStatusInfo()
        {
            return $"网络激活: {m_isNetworkActive}\n" +
                   $"正在连接: {m_isConnecting}\n" +
                   $"同步启用: {m_enableNetworkSync}\n" +
                   $"同步频率: {m_syncRate}Hz\n" +
                   $"是否拥有者: {(IsSpawned ? IsOwner.ToString() : "未生成")}\n" +
                   $"是否服务器: {(IsSpawned ? IsServer.ToString() : "未生成")}";
        }

        /// <summary>
        /// 强制重新连接
        /// </summary>
        public void ForceReconnect()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 强制重新连接");
            }

            DisconnectFromNetwork();

            // 延迟重新连接以确保完全断开
            Invoke(nameof(StartNetworkConnection), 1f);
        }

        #endregion

        #region 私有同步方法

        /// <summary>
        /// 同步变换数据
        /// </summary>
        private void SyncTransformData()
        {
            // TODO: 实现变换数据的网络同步
            // 使用NetworkTransform或自定义RPC
        }

        /// <summary>
        /// 同步动画数据
        /// </summary>
        private void SyncAnimationData()
        {
            // TODO: 实现动画状态的网络同步
            // 使用NetworkAnimator或自定义RPC
        }

        /// <summary>
        /// 同步自定义状态
        /// </summary>
        private void SyncCustomStates()
        {
            // TODO: 实现自定义游戏状态的网络同步
            // 例如：分数、游戏进度、特殊效果等
        }

        #endregion
    }
}