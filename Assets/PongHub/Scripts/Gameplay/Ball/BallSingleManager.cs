using UnityEngine;
using PongHub.Core;
using PongHub.Core.Audio;
using System.Collections.Generic;

namespace PongHub.Gameplay.Ball
{
    /// <summary>
    /// 单人模式球管理器
    /// 管理单机模式下的球生成、重置、销毁等功能
    /// </summary>
    public class BallSingleManager : MonoBehaviour
    {
        [Header("球预制体")]
        [SerializeField] private Ball m_ballPrefab;
        [SerializeField] private Transform m_spawnPoint;
        [SerializeField] private Transform m_ballContainer;

        [Header("生成配置")]
        [SerializeField] private bool m_autoSpawnBall = true;
        // [SerializeField] private float m_autoSpawnDelay = 2f;
        [SerializeField] private int m_maxBalls = 1;
        [SerializeField] private bool m_enableMultiBalls = false;

        [Header("重置配置")]
        [SerializeField] private bool m_autoResetOnMiss = true;
        [SerializeField] private float m_resetDelay = 1f;
        [SerializeField] private Vector3 m_defaultSpawnPosition = Vector3.zero;
        [SerializeField] private Vector3 m_defaultSpawnVelocity = Vector3.zero;

        [Header("调试设置")]
        [SerializeField] private bool m_enableDebugLog = false;
        [SerializeField] private bool m_showSpawnGizmos = false;

        // 当前球管理
        private Ball m_currentBall;
        private List<Ball> m_activeBalls = new List<Ball>();
        private float m_lastResetTime = 0f;

        // 统计数据
        private int m_totalBallsSpawned = 0;
        private int m_ballsReset = 0;

        // 状态
        private bool m_isInitialized = false;
        private bool m_isSpawning = false;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeBallManager();
        }

        private void Update()
        {
            if (m_isInitialized)
            {
                UpdateBallManager();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_showSpawnGizmos)
            {
                DrawSpawnGizmos();
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            // 确保有生成点
            if (m_spawnPoint == null)
            {
                var spawnGO = new GameObject("BallSpawnPoint");
                spawnGO.transform.SetParent(transform);
                spawnGO.transform.localPosition = m_defaultSpawnPosition;
                m_spawnPoint = spawnGO.transform;
            }

            // 确保有球容器
            if (m_ballContainer == null)
            {
                var containerGO = new GameObject("BallContainer");
                containerGO.transform.SetParent(transform);
                m_ballContainer = containerGO.transform;
            }

            // 验证球预制体
            if (m_ballPrefab == null)
            {
                LogError("Ball prefab is not assigned!");
                return;
            }
        }

        private void InitializeBallManager()
        {
            // 初始化球列表
            m_activeBalls.Clear();

            // 设置默认生成位置
            if (m_defaultSpawnPosition == Vector3.zero)
            {
                m_defaultSpawnPosition = m_spawnPoint.position;
            }

            m_isInitialized = true;

            // 自动生成第一个球
            if (m_autoSpawnBall)
            {
                SpawnBall();
            }

            LogDebug("BallSingleManager initialized successfully");
        }
        #endregion

        #region Ball Management
        private void UpdateBallManager()
        {
            // 清理已销毁的球
            CleanupDestroyedBalls();

            // 检查是否需要自动重置
            if (m_autoResetOnMiss && m_activeBalls.Count == 0 &&
                Time.time - m_lastResetTime > m_resetDelay)
            {
                SpawnBall();
            }
        }

        private void CleanupDestroyedBalls()
        {
            for (int i = m_activeBalls.Count - 1; i >= 0; i--)
            {
                if (m_activeBalls[i] == null)
                {
                    m_activeBalls.RemoveAt(i);
                }
            }
        }

        public Ball SpawnBall()
        {
            return SpawnBall(m_spawnPoint.position, m_defaultSpawnVelocity);
        }

        public Ball SpawnBall(Vector3 position)
        {
            return SpawnBall(position, m_defaultSpawnVelocity);
        }

        public Ball SpawnBall(Vector3 position, Vector3 velocity)
        {
            // 检查是否可以生成新球
            if (!CanSpawnBall())
            {
                LogDebug("Cannot spawn ball: limit reached or conditions not met");
                return null;
            }

            m_isSpawning = true;

            try
            {
                // 创建球实例
                Ball newBall = Instantiate(m_ballPrefab, position, Quaternion.identity, m_ballContainer);

                // 移除网络组件
                RemoveNetworkComponents(newBall);

                // 设置初始速度
                if (velocity != Vector3.zero)
                {
                    var physics = newBall.GetComponent<BallPhysics>();
                    if (physics != null)
                    {
                        physics.SetVelocity(velocity);
                    }
                }

                // 添加到激活列表
                m_activeBalls.Add(newBall);

                // 设置当前球
                if (m_currentBall == null)
                {
                    m_currentBall = newBall;
                }

                // 注册球事件
                RegisterBallEvents(newBall);

                m_totalBallsSpawned++;

                LogDebug($"Ball spawned at {position}. Total active balls: {m_activeBalls.Count}");

                return newBall;
            }
            catch (System.Exception e)
            {
                LogError($"Failed to spawn ball: {e.Message}");
                return null;
            }
            finally
            {
                m_isSpawning = false;
            }
        }

        private void RemoveNetworkComponents(Ball ball)
        {
            // 移除所有网络相关组件
            var networkObject = ball.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObject != null)
            {
                DestroyImmediate(networkObject);
            }

            var ballNetworking = ball.GetComponent<BallNetworking>();
            if (ballNetworking != null)
            {
                DestroyImmediate(ballNetworking);
            }

            var ballStateSync = ball.GetComponent<BallStateSync>();
            if (ballStateSync != null)
            {
                DestroyImmediate(ballStateSync);
            }

            LogDebug("Network components removed from ball");
        }

        private void RegisterBallEvents(Ball ball)
        {
            // 注册球的事件回调
            // 例如：球出界、球击中桌子等
        }

        private bool CanSpawnBall()
        {
            if (m_isSpawning) return false;
            if (m_activeBalls.Count >= m_maxBalls && !m_enableMultiBalls) return false;
            return true;
        }

        public void ResetBall()
        {
            if (m_currentBall != null)
            {
                ResetBall(m_currentBall);
            }
        }

        public void ResetBall(Ball ball)
        {
            if (ball == null) return;

            // 重置球的位置和速度
            ball.transform.position = m_spawnPoint.position;

            var physics = ball.GetComponent<BallPhysics>();
            if (physics != null)
            {
                physics.ResetBall();
            }

            m_ballsReset++;
            m_lastResetTime = Time.time;

            LogDebug($"Ball reset to spawn position. Total resets: {m_ballsReset}");
        }

        public void DestroyBall()
        {
            if (m_currentBall != null)
            {
                DestroyBall(m_currentBall);
            }
        }

        public void DestroyBall(Ball ball)
        {
            if (ball == null) return;

            // 从激活列表中移除
            m_activeBalls.Remove(ball);

            // 如果是当前球，清空引用
            if (m_currentBall == ball)
            {
                m_currentBall = null;
            }

            // 销毁球对象
            Destroy(ball.gameObject);

            LogDebug($"Ball destroyed. Remaining active balls: {m_activeBalls.Count}");
        }

        public void DestroyAllBalls()
        {
            for (int i = m_activeBalls.Count - 1; i >= 0; i--)
            {
                if (m_activeBalls[i] != null)
                {
                    DestroyBall(m_activeBalls[i]);
                }
            }

            m_activeBalls.Clear();
            m_currentBall = null;

            LogDebug("All balls destroyed");
        }
        #endregion

        #region Practice Features
        public void StartPracticeMode()
        {
            // 清理现有球
            DestroyAllBalls();

            // 生成练习球
            var practiceBall = SpawnBall();
            if (practiceBall != null)
            {
                // 设置练习模式特有属性
                ConfigurePracticeBall(practiceBall);
            }
        }

        private void ConfigurePracticeBall(Ball ball)
        {
            // 练习模式下的特殊配置
            // 例如：启用轨迹线、调整物理参数等
        }

        public void ServeBall(Vector3 direction, float force)
        {
            if (m_currentBall == null) return;

            var physics = m_currentBall.GetComponent<BallPhysics>();
            if (physics != null)
            {
                Vector3 serveVelocity = direction.normalized * force;
                physics.SetVelocity(serveVelocity);

                LogDebug($"Ball served with force {force} in direction {direction}");
            }
        }
        #endregion

        #region Properties and Getters
        public Ball CurrentBall => m_currentBall;
        public List<Ball> ActiveBalls => new List<Ball>(m_activeBalls);
        public int ActiveBallCount => m_activeBalls.Count;
        public bool IsInitialized => m_isInitialized;
        public bool IsSpawning => m_isSpawning;

        // 统计属性
        public int TotalBallsSpawned => m_totalBallsSpawned;
        public int BallsReset => m_ballsReset;
        #endregion

        #region Configuration
        public void SetMaxBalls(int maxBalls)
        {
            m_maxBalls = Mathf.Max(1, maxBalls);
        }

        public void SetAutoSpawn(bool autoSpawn)
        {
            m_autoSpawnBall = autoSpawn;
        }

        public void SetAutoReset(bool autoReset)
        {
            m_autoResetOnMiss = autoReset;
        }

        public void SetSpawnPosition(Vector3 position)
        {
            m_defaultSpawnPosition = position;
            if (m_spawnPoint != null)
            {
                m_spawnPoint.position = position;
            }
        }

        public void SetBallPrefab(Ball prefab)
        {
            m_ballPrefab = prefab;
        }
        #endregion

        #region Debug and Visualization
        private void DrawSpawnGizmos()
        {
            if (m_spawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_spawnPoint.position, 0.05f);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(m_spawnPoint.position, m_defaultSpawnVelocity);
            }
        }

        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[BallSingleManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[BallSingleManager] {message}");
        }
        #endregion
    }
}