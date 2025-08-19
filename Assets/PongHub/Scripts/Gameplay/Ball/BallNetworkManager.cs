using UnityEngine;
using Unity.Netcode;
using PongHub.Core;
using PongHub.Core.Audio;
using System.Collections.Generic;

namespace PongHub.Gameplay.Ball
{
    /// <summary>
    /// 网络模式球管理器
    /// 管理多人游戏中的球生成、同步、销毁等功能
    /// </summary>
    public class BallNetworkManager : NetworkBehaviour, IGameModeComponent
    {
        [Header("球预制体")]
        [SerializeField]
        [Tooltip("Ball Prefab / 球预制体 - Network ball prefab with NetworkObject")]
        private GameObject m_networkBallPrefab;
        
        [SerializeField]
        [Tooltip("Spawn Point / 生成点 - Transform for ball spawn position")]
        private Transform m_spawnPoint;
        
        [SerializeField]
        [Tooltip("Ball Container / 球容器 - Parent transform for spawned balls")]
        private Transform m_ballContainer;

        [Header("网络配置")]
        [SerializeField]
        [Tooltip("Authority Mode / 权限模式 - Ball spawning authority mode")]
        private BallAuthorityMode m_authorityMode = BallAuthorityMode.ServerOnly;
        
        [SerializeField]
        [Tooltip("Max Balls / 最大球数 - Maximum number of balls in network game")]
        private int m_maxNetworkBalls = 1;
        
        [SerializeField]
        [Tooltip("Sync Frequency / 同步频率 - Ball state synchronization frequency")]
        private float m_syncFrequency = 30f;

        [Header("重置配置")]
        [SerializeField]
        [Tooltip("Auto Reset / 自动重置 - Whether to auto reset ball on miss")]
        private bool m_autoResetOnMiss = true;
        
        [SerializeField]
        [Tooltip("Reset Delay / 重置延迟 - Delay before auto reset")]
        private float m_resetDelay = 2f;
        
        [SerializeField]
        [Tooltip("Default Spawn Position / 默认生成位置 - Default ball spawn position")]
        private Vector3 m_defaultSpawnPosition = Vector3.zero;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Enable Debug Log / 启用调试日志 - Whether to show debug logs")]
        private bool m_enableDebugLog = false;
        
        [SerializeField]
        [Tooltip("Show Network Gizmos / 显示网络Gizmos - Whether to show network gizmos")]
        private bool m_showNetworkGizmos = false;

        // 网络变量
        private NetworkVariable<int> m_activeBallCount = new NetworkVariable<int>(0);
        private NetworkVariable<bool> m_gameInProgress = new NetworkVariable<bool>(false);

        // 本地管理
        private Dictionary<ulong, GameObject> m_networkBalls = new Dictionary<ulong, GameObject>();
        private GameObject m_currentNetworkBall;
        private float m_lastResetTime = 0f;

        // 统计数据
        private int m_totalNetworkBallsSpawned = 0;
        private int m_ballsNetworkReset = 0;

        // 状态
        private bool m_isInitialized = false;
        private bool m_isSpawning = false;

        public enum BallAuthorityMode
        {
            ServerOnly,
            HostOnly,
            AnyClient
        }

        #region NetworkBehaviour Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeNetworkManager();
            
            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            // 注销GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
            
            CleanupNetworkBalls();
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned || !m_isInitialized) return;

            UpdateNetworkManager();
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
        private void InitializeNetworkManager()
        {
            // 确保有生成点
            if (m_spawnPoint == null)
            {
                var spawnGO = new GameObject("NetworkBallSpawnPoint");
                spawnGO.transform.SetParent(transform);
                spawnGO.transform.localPosition = m_defaultSpawnPosition;
                m_spawnPoint = spawnGO.transform;
            }

            // 确保有球容器
            if (m_ballContainer == null)
            {
                var containerGO = new GameObject("NetworkBallContainer");
                containerGO.transform.SetParent(transform);
                m_ballContainer = containerGO.transform;
            }

            // 验证网络球预制体
            if (m_networkBallPrefab == null)
            {
                LogError("Network ball prefab is not assigned!");
                return;
            }

            // 验证NetworkObject组件
            var networkObject = m_networkBallPrefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                LogError("Network ball prefab must have NetworkObject component!");
                return;
            }

            // 设置默认生成位置
            if (m_defaultSpawnPosition == Vector3.zero)
            {
                m_defaultSpawnPosition = m_spawnPoint.position;
            }

            m_isInitialized = true;

            LogDebug("BallNetworkManager initialized successfully");
        }

        private void EnableNetworkMode()
        {
            if (!IsSpawned) return;

            // 如果是服务器，自动生成网络球
            if (IsServer && m_networkBalls.Count == 0)
            {
                SpawnNetworkBallServerRpc();
            }

            LogDebug("Network mode enabled");
        }

        private void DisableNetworkMode()
        {
            // 清理网络球（只有服务器可以销毁）
            if (IsServer)
            {
                CleanupNetworkBalls();
            }

            LogDebug("Network mode disabled");
        }
        #endregion

        #region Network Ball Management
        private void UpdateNetworkManager()
        {
            // 清理已销毁的网络球
            CleanupDestroyedNetworkBalls();

            // 服务器检查是否需要自动重置
            if (IsServer && m_autoResetOnMiss && m_networkBalls.Count == 0 &&
                m_gameInProgress.Value && Time.time - m_lastResetTime > m_resetDelay)
            {
                SpawnNetworkBallServerRpc();
            }
        }

        private void CleanupDestroyedNetworkBalls()
        {
            var keysToRemove = new List<ulong>();
            
            foreach (var kvp in m_networkBalls)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                m_networkBalls.Remove(key);
            }

            // 更新网络变量
            if (IsServer)
            {
                m_activeBallCount.Value = m_networkBalls.Count;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnNetworkBallServerRpc()
        {
            SpawnNetworkBallServerRpc(m_spawnPoint.position, Vector3.zero);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnNetworkBallServerRpc(Vector3 position, Vector3 velocity)
        {
            if (!CanSpawnNetworkBall())
            {
                LogDebug("Cannot spawn network ball: limit reached or conditions not met");
                return;
            }

            m_isSpawning = true;

            try
            {
                // 创建网络球实例
                GameObject networkBall = Instantiate(m_networkBallPrefab, position, Quaternion.identity, m_ballContainer);
                
                // 获取NetworkObject并生成
                var networkObject = networkBall.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();

                    // 添加到网络球字典
                    m_networkBalls[networkObject.NetworkObjectId] = networkBall;

                    // 设置当前球
                    if (m_currentNetworkBall == null)
                    {
                        m_currentNetworkBall = networkBall;
                    }

                    // 设置初始速度
                    if (velocity != Vector3.zero)
                    {
                        SetBallVelocityClientRpc(networkObject.NetworkObjectId, velocity);
                    }

                    // 更新统计
                    m_totalNetworkBallsSpawned++;
                    m_activeBallCount.Value = m_networkBalls.Count;

                    LogDebug($"Network ball spawned at {position}. Total active: {m_networkBalls.Count}");
                }
                else
                {
                    LogError("Failed to get NetworkObject from spawned ball");
                    Destroy(networkBall);
                }
            }
            catch (System.Exception e)
            {
                LogError($"Failed to spawn network ball: {e.Message}");
            }
            finally
            {
                m_isSpawning = false;
            }
        }

        [ClientRpc]
        private void SetBallVelocityClientRpc(ulong ballNetworkId, Vector3 velocity)
        {
            if (m_networkBalls.TryGetValue(ballNetworkId, out GameObject ball))
            {
                var ballPhysics = ball.GetComponent<BallPhysics>();
                if (ballPhysics != null)
                {
                    ballPhysics.SetVelocity(velocity);
                }
            }
        }

        private bool CanSpawnNetworkBall()
        {
            if (!IsServer) return false;
            if (m_isSpawning) return false;
            if (m_networkBalls.Count >= m_maxNetworkBalls) return false;
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetNetworkBallServerRpc()
        {
            if (m_currentNetworkBall != null)
            {
                ResetNetworkBallServerRpc(m_currentNetworkBall.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetNetworkBallServerRpc(ulong ballNetworkId)
        {
            if (m_networkBalls.TryGetValue(ballNetworkId, out GameObject ball))
            {
                // 重置球的位置
                ball.transform.position = m_spawnPoint.position;

                // 通知所有客户端重置球
                ResetBallClientRpc(ballNetworkId, m_spawnPoint.position);

                m_ballsNetworkReset++;
                m_lastResetTime = Time.time;

                LogDebug($"Network ball reset. Total resets: {m_ballsNetworkReset}");
            }
        }

        [ClientRpc]
        private void ResetBallClientRpc(ulong ballNetworkId, Vector3 position)
        {
            if (m_networkBalls.TryGetValue(ballNetworkId, out GameObject ball))
            {
                ball.transform.position = position;
                
                var ballPhysics = ball.GetComponent<BallPhysics>();
                if (ballPhysics != null)
                {
                    ballPhysics.ResetBall();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyNetworkBallServerRpc(ulong ballNetworkId)
        {
            if (m_networkBalls.TryGetValue(ballNetworkId, out GameObject ball))
            {
                var networkObject = ball.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }

                m_networkBalls.Remove(ballNetworkId);

                // 如果是当前球，清空引用
                if (m_currentNetworkBall == ball)
                {
                    m_currentNetworkBall = null;
                }

                m_activeBallCount.Value = m_networkBalls.Count;

                LogDebug($"Network ball destroyed. Remaining: {m_networkBalls.Count}");
            }
        }

        private void CleanupNetworkBalls()
        {
            if (!IsServer) return;

            foreach (var kvp in m_networkBalls)
            {
                if (kvp.Value != null)
                {
                    var networkObject = kvp.Value.GetComponent<NetworkObject>();
                    if (networkObject != null && networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                }
            }

            m_networkBalls.Clear();
            m_currentNetworkBall = null;
            m_activeBallCount.Value = 0;

            LogDebug("All network balls cleaned up");
        }
        #endregion

        #region Game Control
        [ServerRpc(RequireOwnership = false)]
        public void StartNetworkGameServerRpc()
        {
            m_gameInProgress.Value = true;
            
            // 如果没有球，生成一个
            if (m_networkBalls.Count == 0)
            {
                SpawnNetworkBallServerRpc();
            }

            LogDebug("Network game started");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndNetworkGameServerRpc()
        {
            m_gameInProgress.Value = false;
            CleanupNetworkBalls();

            LogDebug("Network game ended");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServeBallServerRpc(Vector3 direction, float force)
        {
            if (m_currentNetworkBall != null)
            {
                Vector3 serveVelocity = direction.normalized * force;
                var networkId = m_currentNetworkBall.GetComponent<NetworkObject>().NetworkObjectId;
                SetBallVelocityClientRpc(networkId, serveVelocity);

                LogDebug($"Network ball served with force {force} in direction {direction}");
            }
        }
        #endregion

        #region Properties and Getters
        public GameObject CurrentNetworkBall => m_currentNetworkBall;
        public Dictionary<ulong, GameObject> NetworkBalls => new Dictionary<ulong, GameObject>(m_networkBalls);
        public int ActiveNetworkBallCount => m_activeBallCount.Value;
        public bool IsGameInProgress => m_gameInProgress.Value;
        public bool IsInitialized => m_isInitialized;
        public bool IsSpawning => m_isSpawning;

        // 统计属性
        public int TotalNetworkBallsSpawned => m_totalNetworkBallsSpawned;
        public int BallsNetworkReset => m_ballsNetworkReset;
        #endregion

        #region Configuration
        public void SetMaxNetworkBalls(int maxBalls)
        {
            m_maxNetworkBalls = Mathf.Max(1, maxBalls);
        }

        public void SetAuthorityMode(BallAuthorityMode mode)
        {
            m_authorityMode = mode;
        }

        public void SetAutoReset(bool autoReset)
        {
            m_autoResetOnMiss = autoReset;
        }

        public void SetNetworkBallPrefab(GameObject prefab)
        {
            m_networkBallPrefab = prefab;
        }
        #endregion

        #region Debug and Visualization
        private void DrawNetworkGizmos()
        {
            if (m_spawnPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(m_spawnPoint.position, 0.05f);

                // 显示网络球数量
                Gizmos.color = Color.cyan;
                foreach (var ball in m_networkBalls.Values)
                {
                    if (ball != null)
                    {
                        Gizmos.DrawWireSphere(ball.transform.position, 0.03f);
                    }
                }
            }
        }

        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[BallNetworkManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[BallNetworkManager] {message}");
        }
        #endregion
    }
}