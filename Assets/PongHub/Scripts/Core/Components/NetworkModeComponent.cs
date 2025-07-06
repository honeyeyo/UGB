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

        [Header("状态保持设置")]
        [SerializeField]
        [Tooltip("Save State / 保存状态 - Save and restore component state during mode switching")]
        private bool m_saveState = true;

        [SerializeField]
        [Tooltip("State Keys / 状态键 - Keys for state data that should be preserved")]
        private string[] m_stateKeys;

        [Header("过渡动画设置")]
        [SerializeField]
        [Tooltip("Transition Duration / 过渡时长 - Duration of transition animations in seconds")]
        private float m_transitionDuration = 0.5f;

        [SerializeField]
        [Tooltip("Transition Effect / 过渡效果 - Visual effect during mode transition")]
        private GameObject m_transitionEffect;

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

        // 状态保持
        private Dictionary<string, object> m_savedState = new Dictionary<string, object>();
        private bool m_hasRestoredState = false;

        // 过渡动画
        private Coroutine m_transitionCoroutine = null;

        // 事件
        public System.Action<bool> OnNetworkStatusChanged;
        public System.Action<string> OnNetworkError;
        public System.Action OnTransitionStarted;
        public System.Action OnTransitionCompleted;

        #region IGameModeComponent 实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            bool shouldBeActive = IsActiveInMode(newMode);

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 模式切换: {previousMode} -> {newMode}, 激活状态: {shouldBeActive}");
            }

            // 保存当前状态（如果从网络模式切换出去）
            if (m_saveState && previousMode == GameMode.Network && !shouldBeActive)
            {
                SaveComponentState();
            }

            // 开始过渡动画
            StartTransitionEffect(shouldBeActive);

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

            // 恢复保存的状态（如果切换到网络模式）
            if (m_saveState && newMode == GameMode.Network && shouldBeActive && !m_hasRestoredState)
            {
                RestoreComponentState();
                m_hasRestoredState = true;
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

            // 初始化过渡效果
            if (m_transitionEffect != null)
            {
                m_transitionEffect.SetActive(false);
            }
        }

        private void Update()
        {
            // 检查连接超时
            if (m_isConnecting && !m_isNetworkActive)
            {
                if (Time.time - m_connectionStartTime > m_connectionTimeout)
                {
                    Debug.LogWarning("[NetworkModeComponent] 网络连接超时");
                }
                HandleConnectionTimeout();
            }
        }

        public override void OnDestroy()
        {
            // 停止同步协程
            if (m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
                m_syncCoroutine = null;
            }

            // 停止过渡协程
            if (m_transitionCoroutine != null)
            {
                StopCoroutine(m_transitionCoroutine);
                m_transitionCoroutine = null;
            }

            // 从GameModeManager注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }

            // 调用基类的OnDestroy
            base.OnDestroy();
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

            // 通知状态变化
            OnNetworkStatusChanged?.Invoke(true);

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

            // 通知状态变化
            OnNetworkStatusChanged?.Invoke(false);

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
            if (m_isNetworkActive || m_isConnecting)
            {
                DisconnectFromNetwork();
            }

            // 清理网络游戏玩法
            CleanupNetworkGameplay();

            // 重置状态
            m_hasRestoredState = false;
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
                    Debug.Log("[NetworkModeComponent] 已经在连接或已连接到网络");
                }
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 开始网络连接");
            }

            m_isConnecting = true;
            m_connectionStartTime = Time.time;

            // 检查NetworkManager是否存在
            if (NetworkManager.Singleton == null)
            {
                HandleConnectionFailure("NetworkManager不存在");
                return;
            }

            // 设置连接批准回调来限制玩家数量
            NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
            {
                // 如果当前连接数量已经达到最大玩家数，拒绝连接
                if (NetworkManager.Singleton.ConnectedClientsIds.Count >= m_maxPlayers)
                {
                    response.Approved = false;
                    response.Reason = "服务器已满";
                }
                else
                {
                    response.Approved = true;
                }
            };

            // 注册连接事件
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 启动作为主机
            if (!NetworkManager.Singleton.StartHost())
            {
                // 如果无法作为主机启动，尝试作为客户端连接
                if (!NetworkManager.Singleton.StartClient())
                {
                    HandleConnectionFailure("无法作为主机或客户端启动");
                    return;
                }
            }
        }

        /// <summary>
        /// 断开网络连接
        /// </summary>
        private void DisconnectFromNetwork()
        {
            if (!m_isConnecting && !m_isNetworkActive)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 断开网络连接");
            }

            m_isConnecting = false;

            // 检查NetworkManager是否存在
            if (NetworkManager.Singleton != null)
            {
                // 注销连接事件
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.ConnectionApprovalCallback = null;

                // 关闭连接
                NetworkManager.Singleton.Shutdown();
            }

            m_isNetworkActive = false;
            OnNetworkStatusChanged?.Invoke(false);
        }

        /// <summary>
        /// 处理连接超时
        /// </summary>
        private void HandleConnectionTimeout()
        {
            m_isConnecting = false;
            HandleConnectionFailure("连接超时");
        }

        /// <summary>
        /// 处理连接失败
        /// </summary>
        private void HandleConnectionFailure(string reason)
        {
            m_isConnecting = false;
            m_isNetworkActive = false;

            if (m_debugMode)
            {
                Debug.LogError($"[NetworkModeComponent] 网络连接失败: {reason}");
            }

            // 通知错误
            OnNetworkError?.Invoke(reason);

            // 如果在网络模式下连接失败，切换回本地模式
            if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameMode.Network)
            {
                GameModeManager.Instance.SwitchToMode(GameMode.Local);
            }
        }

        /// <summary>
        /// 配置网络物理模拟
        /// </summary>
        private void ConfigureNetworkPhysics()
        {
            // 配置网络物理模拟
            // 这里可以根据需要调整物理设置，例如插值、预测等
            Physics.simulationMode = m_isNetworkActive ? SimulationMode.Script : SimulationMode.FixedUpdate;
        }

        /// <summary>
        /// 初始化网络游戏玩法
        /// </summary>
        private void InitializeNetworkGameplay()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 初始化网络游戏玩法");
            }

            // 初始化网络游戏玩法
            // 这里可以初始化网络游戏相关的组件和逻辑
        }

        /// <summary>
        /// 清理网络游戏玩法
        /// </summary>
        private void CleanupNetworkGameplay()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 清理网络游戏玩法");
            }

            // 清理网络游戏玩法
            // 这里可以清理网络游戏相关的组件和逻辑
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
        /// 同步状态协程
        /// </summary>
        private IEnumerator SyncStateCoroutine()
        {
            float syncInterval = 1.0f / m_syncRate;
            WaitForSeconds wait = new WaitForSeconds(syncInterval);

            while (m_isNetworkActive && m_enableNetworkSync)
            {
                // 同步变换数据
                if (m_syncTransform)
                {
                    SyncTransformData();
                }

                // 同步动画数据
                if (m_syncAnimation)
                {
                    SyncAnimationData();
                }

                // 同步自定义状态
                SyncCustomStates();

                yield return wait;
            }
        }

        /// <summary>
        /// 客户端连接回调
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 客户端已连接: {clientId}");
            }

            m_isConnecting = false;
            m_isNetworkActive = true;

            // 通知状态变化
            OnNetworkStatusChanged?.Invoke(true);
        }

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        private void OnClientDisconnected(ulong clientId)
        {
            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 客户端已断开连接: {clientId}");
            }

            // 如果是本地客户端断开连接，更新状态
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                m_isNetworkActive = false;

                // 通知状态变化
                OnNetworkStatusChanged?.Invoke(false);

                // 如果在网络模式下断开连接，切换回本地模式
                if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameMode.Network)
                {
                    GameModeManager.Instance.SwitchToMode(GameMode.Local);
                }
            }
        }

        /// <summary>
        /// 保存组件状态
        /// </summary>
        private void SaveComponentState()
        {
            if (!m_saveState)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 保存组件状态");
            }

            m_savedState.Clear();

            // 保存关键状态数据
            foreach (var key in m_stateKeys)
            {
                if (m_syncedStates.TryGetValue(key, out var value))
                {
                    m_savedState[key] = value;
                }
            }

            // 可以添加更多特定状态的保存逻辑
        }

        /// <summary>
        /// 恢复组件状态
        /// </summary>
        private void RestoreComponentState()
        {
            if (!m_saveState || m_savedState.Count == 0)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 恢复组件状态");
            }

            // 恢复关键状态数据
            foreach (var pair in m_savedState)
            {
                m_syncedStates[pair.Key] = pair.Value;
            }

            // 可以添加更多特定状态的恢复逻辑
        }

        /// <summary>
        /// 开始过渡效果
        /// </summary>
        private void StartTransitionEffect(bool activating)
        {
            if (m_transitionEffect == null || m_transitionDuration <= 0)
            {
                return;
            }

            // 停止当前过渡
            if (m_transitionCoroutine != null)
            {
                StopCoroutine(m_transitionCoroutine);
            }

            // 开始新的过渡
            m_transitionCoroutine = StartCoroutine(TransitionEffectCoroutine(activating));
        }

        /// <summary>
        /// 过渡效果协程
        /// </summary>
        private IEnumerator TransitionEffectCoroutine(bool activating)
        {
            // 通知过渡开始
            OnTransitionStarted?.Invoke();

            // 显示过渡效果
            m_transitionEffect.SetActive(true);

            // 过渡动画逻辑
            float elapsedTime = 0;
            while (elapsedTime < m_transitionDuration)
            {
                float progress = elapsedTime / m_transitionDuration;

                // 可以在这里添加更复杂的过渡动画逻辑
                // 例如调整透明度、缩放等

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 隐藏过渡效果
            m_transitionEffect.SetActive(false);

            // 通知过渡完成
            OnTransitionCompleted?.Invoke();
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 同步组件状态
        /// </summary>
        public void SyncComponentState()
        {
            if (!m_isNetworkActive || !m_enableNetworkSync)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponent] 手动同步组件状态");
            }

            // 同步变换数据
            if (m_syncTransform)
            {
                SyncTransformData();
            }

            // 同步动画数据
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
        public void SetNetworkSyncEnabled(bool enabled)
        {
            if (m_enableNetworkSync == enabled)
            {
                return;
            }

            m_enableNetworkSync = enabled;

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 设置网络同步: {enabled}");
            }

            if (m_enableNetworkSync && m_isNetworkActive)
            {
                StartSyncCoroutine();
            }
            else if (!m_enableNetworkSync && m_syncCoroutine != null)
            {
                StopCoroutine(m_syncCoroutine);
                m_syncCoroutine = null;
            }
        }

        /// <summary>
        /// 设置同步频率
        /// </summary>
        public void SetSyncRate(float rate)
        {
            if (rate < 10f)
            {
                rate = 10f;
            }
            else if (rate > 120f)
            {
                rate = 120f;
            }

            m_syncRate = rate;

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponent] 设置同步频率: {rate}");
            }

            // 重启同步协程以应用新频率
            if (m_enableNetworkSync && m_isNetworkActive)
            {
                StartSyncCoroutine();
            }
        }

        /// <summary>
        /// 获取网络状态信息
        /// </summary>
        public string GetNetworkStatusInfo()
        {
            string status = "未连接";

            if (m_isConnecting)
            {
                status = "正在连接...";
            }
            else if (m_isNetworkActive)
            {
                status = "已连接";
                if (NetworkManager.Singleton != null)
                {
                    status += $" (ID: {NetworkManager.Singleton.LocalClientId})";
                }
            }

            return status;
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

            // 断开当前连接
            DisconnectFromNetwork();

            // 短暂延迟后重新连接
            StartCoroutine(ReconnectAfterDelay());
        }

        /// <summary>
        /// 延迟重新连接协程
        /// </summary>
        private IEnumerator ReconnectAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            StartNetworkConnection();
        }

        #endregion

        #region 同步方法

        /// <summary>
        /// 同步变换数据
        /// </summary>
        private void SyncTransformData()
        {
            // 这里实现变换数据的同步逻辑
            // 例如位置、旋转、缩放等
            // 可以使用NetworkTransform组件或自定义RPC
        }

        /// <summary>
        /// 同步动画数据
        /// </summary>
        private void SyncAnimationData()
        {
            // 这里实现动画数据的同步逻辑
            // 例如动画状态、参数等
            // 可以使用NetworkAnimator组件或自定义RPC
        }

        /// <summary>
        /// 同步自定义状态
        /// </summary>
        private void SyncCustomStates()
        {
            // 这里实现自定义状态的同步逻辑
            // 例如游戏状态、玩家数据等
            // 可以使用NetworkVariable或自定义RPC
        }

        #endregion
    }
}