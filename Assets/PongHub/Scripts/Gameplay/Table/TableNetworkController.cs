using UnityEngine;
using Unity.Netcode;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Gameplay.Ball;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 网络模式球桌控制器
    /// 管理多人游戏中的球桌网络同步和交互
    /// </summary>
    [RequireComponent(typeof(Table))]
    public class TableNetworkController : NetworkBehaviour, IGameModeComponent
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Table / 球桌 - Reference to the main table component")]
        private Table m_table;

        [SerializeField]
        [Tooltip("Table Part Manager / 球桌部件管理器 - Reference to table part manager")]
        private TablePartManager m_tablePartManager;

        [Header("网络配置")]
        [SerializeField]
        [Tooltip("Sync Table State / 同步球桌状态 - Whether to sync table state across network")]
        private bool m_syncTableState = true;

        [SerializeField]
        [Tooltip("Authority Mode / 权限模式 - Table interaction authority mode")]
        private TableAuthorityMode m_authorityMode = TableAuthorityMode.ServerOnly;

        [SerializeField]
        [Tooltip("Sync Frequency / 同步频率 - Table state synchronization frequency")]
        private float m_syncFrequency = 10f;

        [Header("网络交互设置")]
        [SerializeField]
        [Tooltip("Enable Network Collision / 启用网络碰撞 - Whether to sync ball-table collisions")]
        private bool m_enableNetworkCollision = true;

        [SerializeField]
        [Tooltip("Enable Network Audio / 启用网络音频 - Whether to sync table hit sounds")]
        private bool m_enableNetworkAudio = true;

        [SerializeField]
        [Tooltip("Enable Network Effects / 启用网络特效 - Whether to sync table visual effects")]
        private bool m_enableNetworkEffects = true;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Enable Debug Log / 启用调试日志 - Whether to show debug logs")]
        private bool m_enableDebugLog = false;

        [SerializeField]
        [Tooltip("Show Network Gizmos / 显示网络Gizmos - Whether to show network gizmos")]
        private bool m_showNetworkGizmos = false;

        // 网络变量
        private NetworkVariable<Vector3> m_tablePosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> m_tableRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<bool> m_tableActive = new NetworkVariable<bool>(true);

        // 碰撞统计
        private NetworkVariable<int> m_totalHits = new NetworkVariable<int>(0);
        private NetworkVariable<int> m_surfaceHits = new NetworkVariable<int>(0);
        private NetworkVariable<int> m_netHits = new NetworkVariable<int>(0);

        // 状态
        private bool m_isInitialized = false;
        private float m_lastSyncTime = 0f;

        public enum TableAuthorityMode
        {
            ServerOnly,
            HostOnly,
            AllClients
        }

        #region NetworkBehaviour Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeNetworkController();

            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }

            // 注册网络变量变化事件
            m_tablePosition.OnValueChanged += OnTablePositionChanged;
            m_tableRotation.OnValueChanged += OnTableRotationChanged;
            m_tableActive.OnValueChanged += OnTableActiveChanged;
        }

        public override void OnNetworkDespawn()
        {
            // 注销GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }

            // 注销网络变量事件
            m_tablePosition.OnValueChanged -= OnTablePositionChanged;
            m_tableRotation.OnValueChanged -= OnTableRotationChanged;
            m_tableActive.OnValueChanged -= OnTableActiveChanged;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned || !m_isInitialized) return;

            UpdateNetworkController();
        }

        private void OnDrawGizmos()
        {
            if (m_showNetworkGizmos)
            {
                DrawNetworkGizmos();
            }
        }
        #endregion

        #region IGameModeComponent Implementation
        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            switch (newMode)
            {
                case GameMode.Network:
                    EnableNetworkMode();
                    break;
                case GameMode.Local:
                case GameMode.Menu:
                    DisableNetworkMode();
                    break;
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            return mode == GameMode.Network;
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 获取Table组件
            if (m_table == null)
                m_table = GetComponent<Table>();

            // 获取TablePartManager组件
            if (m_tablePartManager == null)
                m_tablePartManager = GetComponent<TablePartManager>();

            if (m_table == null)
            {
                LogError("Table component is required but not found!");
                return;
            }
        }

        private void InitializeNetworkController()
        {
            if (m_table == null)
            {
                LogError("Cannot initialize: Table component missing");
                return;
            }

            // 设置初始网络状态
            if (IsServer)
            {
                m_tablePosition.Value = transform.position;
                m_tableRotation.Value = transform.rotation;
                m_tableActive.Value = gameObject.activeInHierarchy;
            }

            // 注册TablePart事件
            if (m_tablePartManager != null)
            {
                RegisterTablePartEvents();
            }

            m_isInitialized = true;

            LogDebug("TableNetworkController initialized successfully");
        }

        private void RegisterTablePartEvents()
        {
            // 注册球桌部件的碰撞事件，用于网络同步
            foreach (var tablePart in m_tablePartManager.TableParts)
            {
                if (tablePart != null)
                {
                    // 这里可以注册TablePart的碰撞事件
                    // tablePart.OnBallHit += OnTablePartHit;
                }
            }
        }

        private void EnableNetworkMode()
        {
            if (!IsSpawned) return;

            // 启用网络同步
            if (IsServer)
            {
                SyncTableStateClientRpc(transform.position, transform.rotation, true);
            }

            LogDebug("Network mode enabled for table");
        }

        private void DisableNetworkMode()
        {
            // 禁用网络功能
            LogDebug("Network mode disabled for table");
        }
        #endregion

        #region Network Synchronization
        private void UpdateNetworkController()
        {
            // 服务器定期同步球桌状态
            if (IsServer && m_syncTableState && Time.time - m_lastSyncTime > 1f / m_syncFrequency)
            {
                SyncTableState();
                m_lastSyncTime = Time.time;
            }
        }

        private void SyncTableState()
        {
            // 检查位置是否发生变化
            if (Vector3.Distance(m_tablePosition.Value, transform.position) > 0.01f)
            {
                m_tablePosition.Value = transform.position;
            }

            // 检查旋转是否发生变化
            if (Quaternion.Angle(m_tableRotation.Value, transform.rotation) > 0.1f)
            {
                m_tableRotation.Value = transform.rotation;
            }

            // 检查激活状态是否发生变化
            bool currentActive = gameObject.activeInHierarchy;
            if (m_tableActive.Value != currentActive)
            {
                m_tableActive.Value = currentActive;
            }
        }

        [ClientRpc]
        private void SyncTableStateClientRpc(Vector3 position, Quaternion rotation, bool active)
        {
            if (IsServer) return; // 服务器不需要应用自己发送的状态

            transform.position = position;
            transform.rotation = rotation;
            gameObject.SetActive(active);
        }

        // 网络变量变化回调
        private void OnTablePositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsServer)
            {
                transform.position = newValue;
            }
        }

        private void OnTableRotationChanged(Quaternion previousValue, Quaternion newValue)
        {
            if (!IsServer)
            {
                transform.rotation = newValue;
            }
        }

        private void OnTableActiveChanged(bool previousValue, bool newValue)
        {
            if (!IsServer)
            {
                gameObject.SetActive(newValue);
            }
        }
        #endregion

        #region Network Collision Handling
        [ServerRpc(RequireOwnership = false)]
        public void OnBallHitTableServerRpc(Vector3 hitPoint, Vector3 hitNormal, float hitForce, int partType)
        {
            // 更新碰撞统计
            m_totalHits.Value++;
            
            switch ((TablePartType)partType)
            {
                case TablePartType.Surface:
                    m_surfaceHits.Value++;
                    break;
                case TablePartType.Net:
                    m_netHits.Value++;
                    break;
            }

            // 通知所有客户端播放效果
            PlayTableHitEffectClientRpc(hitPoint, hitNormal, hitForce, partType);

            LogDebug($"Ball hit table part {(TablePartType)partType} at {hitPoint} with force {hitForce}");
        }

        [ClientRpc]
        private void PlayTableHitEffectClientRpc(Vector3 hitPoint, Vector3 hitNormal, float hitForce, int partType)
        {
            // 播放碰撞音效
            if (m_enableNetworkAudio && AudioManager.Instance != null)
            {
                // 根据不同的TablePartType播放不同的音效
                switch ((TablePartType)partType)
                {
                    case TablePartType.Surface:
                        AudioManager.Instance.PlayTableHit(hitPoint, hitForce * 0.5f);
                        break;
                    case TablePartType.Net:
                        AudioManager.Instance.PlayNetHit(hitPoint, hitForce * 0.3f);
                        break;
                    default:
                        AudioManager.Instance.PlayTableHit(hitPoint, hitForce * 0.4f);
                        break;
                }
            }

            // 播放视觉效果
            if (m_enableNetworkEffects)
            {
                PlayHitVisualEffect(hitPoint, hitNormal, (TablePartType)partType);
            }
        }

        private void PlayHitVisualEffect(Vector3 hitPoint, Vector3 hitNormal, TablePartType partType)
        {
            // 这里可以播放粒子效果、闪光等视觉效果
            // 例如：在击中点生成粒子效果
            LogDebug($"Playing visual effect for {partType} hit at {hitPoint}");
        }
        #endregion

        #region Table Control
        [ServerRpc(RequireOwnership = false)]
        public void SetTableActiveServerRpc(bool active)
        {
            m_tableActive.Value = active;
            gameObject.SetActive(active);

            LogDebug($"Table active state set to {active}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetTableServerRpc()
        {
            // 重置球桌到初始状态
            if (m_table != null)
            {
                m_table.ResetTable();
            }

            // 重置统计数据
            m_totalHits.Value = 0;
            m_surfaceHits.Value = 0;
            m_netHits.Value = 0;

            // 通知客户端重置
            ResetTableClientRpc();

            LogDebug("Table reset completed");
        }

        [ClientRpc]
        private void ResetTableClientRpc()
        {
            if (m_table != null)
            {
                m_table.ResetTable();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTablePositionServerRpc(Vector3 position, Quaternion rotation)
        {
            m_tablePosition.Value = position;
            m_tableRotation.Value = rotation;
            
            transform.position = position;
            transform.rotation = rotation;

            LogDebug($"Table position set to {position}, rotation {rotation.eulerAngles}");
        }
        #endregion

        #region Properties and Getters
        public Table Table => m_table;
        public TablePartManager TablePartManager => m_tablePartManager;
        public bool IsInitialized => m_isInitialized;
        public TableAuthorityMode AuthorityMode => m_authorityMode;

        // 网络状态
        public Vector3 NetworkPosition => m_tablePosition.Value;
        public Quaternion NetworkRotation => m_tableRotation.Value;
        public bool IsNetworkActive => m_tableActive.Value;

        // 碰撞统计
        public int TotalHits => m_totalHits.Value;
        public int SurfaceHits => m_surfaceHits.Value;
        public int NetHits => m_netHits.Value;
        #endregion

        #region Configuration
        public void SetAuthorityMode(TableAuthorityMode mode)
        {
            m_authorityMode = mode;
        }

        public void SetSyncFrequency(float frequency)
        {
            m_syncFrequency = Mathf.Max(1f, frequency);
        }

        public void SetNetworkAudio(bool enabled)
        {
            m_enableNetworkAudio = enabled;
        }

        public void SetNetworkEffects(bool enabled)
        {
            m_enableNetworkEffects = enabled;
        }
        #endregion

        #region Debug and Visualization
        private void DrawNetworkGizmos()
        {
            // 绘制网络同步状态
            if (IsSpawned)
            {
                Gizmos.color = IsServer ? Color.green : Color.blue;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);

                // 显示网络位置
                if (!IsServer && Vector3.Distance(transform.position, m_tablePosition.Value) > 0.01f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, m_tablePosition.Value);
                }
            }
        }

        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[TableNetworkController] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TableNetworkController] {message}");
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        [ContextMenu("Test Network Table Hit")]
        private void TestNetworkTableHit()
        {
            if (Application.isPlaying && IsSpawned)
            {
                OnBallHitTableServerRpc(
                    transform.position + Vector3.up,
                    Vector3.up,
                    5f,
                    (int)TablePartType.Surface
                );
            }
        }

        [ContextMenu("Reset Network Statistics")]
        private void ResetNetworkStatistics()
        {
            if (Application.isPlaying && IsServer)
            {
                m_totalHits.Value = 0;
                m_surfaceHits.Value = 0;
                m_netHits.Value = 0;
            }
        }
#endif
        #endregion
    }
}