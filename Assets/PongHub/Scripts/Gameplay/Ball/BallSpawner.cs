// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using PongHub.Arena.Gameplay;
using PongHub.Networking.Pooling;

namespace PongHub.Gameplay.Ball
{
    /// <summary>
    /// 乒乓球生成器 - 管理乒乓球的生成、销毁和回收
    /// 与原版BallSpawner的主要区别：
    /// 1. 专门处理乒乓球的生成逻辑
    /// 2. 集成发球权限系统
    /// 3. 支持球附着到非持拍手
    /// 4. 优化的网络同步和对象池管理
    /// </summary>
    public class BallSpawner : NetworkBehaviour
    {
        #region Singleton
        public static BallSpawner Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("生成设置")]
        [SerializeField]
        [Tooltip("Pong Ball Prefab / 乒乓球预制件 - NetworkObject prefab for ping pong ball")]
        private NetworkObject pongBallPrefab;              // 乒乓球预制件
        [SerializeField]
        [Tooltip("Max Active Balls / 最大活跃球数 - Maximum number of active balls (1 for match mode)")]
        private int maxActiveBalls = 1;                    // 最大活跃球数（比赛模式为1）
        [SerializeField]
        [Tooltip("Ball Respawn Delay / 球重生延迟 - Delay before respawning ball in seconds")]
        private float ballRespawnDelay = 2f;               // 球重新生成延迟

        [Header("生成位置")]
        [SerializeField]
        [Tooltip("Ball Spawn Points / 球生成点 - Array of spawn point transforms")]
        private Transform[] ballSpawnPoints;               // 球生成点
        [SerializeField]
        [Tooltip("Default Spawn Point / 默认生成点 - Default spawn point transform")]
        private Transform defaultSpawnPoint;               // 默认生成点

        [Header("对象池")]
        [SerializeField]
        [Tooltip("Ball Pool / 球对象池 - NetworkObjectPool for ball objects")]
        private NetworkObjectPool ballPool;               // 球对象池
        [SerializeField]
        [Tooltip("Pool Initial Size / 池初始大小 - Initial size of the object pool")]
        private int poolInitialSize = 5;                  // 对象池初始大小
        // [SerializeField] private int poolMaxSize = 10;                     // 对象池最大大小（预留用于扩展）

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Enable Debug Log / 启用调试日志 - Whether to enable debug logging")]
        private bool enableDebugLog = true;               // 启用调试日志
        [SerializeField]
        [Tooltip("Auto Spawn Balls / 自动生成球 - Whether to automatically spawn balls for testing")]
        private bool autoSpawnBalls = false;              // 自动生成球（测试用）
        #endregion

        #region Private Fields
        private readonly List<BallNetworking> activeBalls = new();     // 活跃球列表
        private readonly Dictionary<ulong, BallNetworking> playerBalls = new(); // 玩家球映射
        private WaitForSeconds respawnWait;                                 // 重生等待
        private int nextSpawnPointIndex = 0;                               // 下一个生成点索引
        #endregion

        #region Events
        public static event System.Action<BallNetworking> OnBallSpawned;  // 球生成事件
        public static event System.Action<BallNetworking> OnBallDespawned; // 球销毁事件
        public static event System.Action<int> OnActiveBallCountChanged;       // 活跃球数量变化
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

            // 初始化等待时间
            respawnWait = new WaitForSeconds(ballRespawnDelay);
        }

        private void Start()
        {
            InitializeSpawner();
        }
        #endregion

        #region NetworkBehaviour Overrides
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeServerState();

                // 自动生成初始球（如果启用）
                if (autoSpawnBalls)
                {
                    StartCoroutine(AutoSpawnInitialBalls());
                }
            }

            LogDebug("乒乓球生成器已网络生成");
        }

        public override void OnNetworkDespawn()
        {
            // 清理所有球
            if (IsServer)
            {
                DespawnAllBalls();
            }
        }
        #endregion

        #region Initialization
        private void InitializeSpawner()
        {
            // 验证必需组件
            if (pongBallPrefab == null)
            {
                Debug.LogError("BallSpawner: 未设置乒乓球预制件");
                return;
            }

            if (ballPool == null)
            {
                Debug.LogWarning("BallSpawner: 未设置对象池，将使用直接实例化");
            }

            // 验证生成点
            if (ballSpawnPoints == null || ballSpawnPoints.Length == 0)
            {
                Debug.LogWarning("BallSpawner: 未设置生成点，将使用默认位置");
            }

            LogDebug("乒乓球生成器初始化完成");
        }

        private void InitializeServerState()
        {
            // 清空活跃球列表
            activeBalls.Clear();
            playerBalls.Clear();

            // 预热对象池
            if (ballPool != null)
            {
                PrewarmPool();
            }

            LogDebug("服务器状态已初始化");
        }

        private void PrewarmPool()
        {
            // 预先创建一些球对象到池中
            for (int i = 0; i < poolInitialSize; i++)
            {
                var ballObj = ballPool.GetNetworkObject(pongBallPrefab.gameObject, Vector3.zero, Quaternion.identity);
                if (ballObj != null)
                {
                    ballObj.gameObject.SetActive(false);
                    ballPool.ReturnNetworkObject(ballObj, pongBallPrefab.gameObject);
                }
            }

            LogDebug($"对象池已预热，创建了 {poolInitialSize} 个球对象");
        }
        #endregion

        #region Ball Spawning
        /// <summary>
        /// 为指定玩家生成球
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>生成的球，如果失败则返回null</returns>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnBallForPlayerServerRpc(ulong playerId = 0, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            if (playerId == 0) playerId = rpcParams.Receive.SenderClientId;

            // 检查是否可以生成球
            if (!CanSpawnBallForPlayer(playerId))
            {
                LogDebug($"无法为玩家 {playerId} 生成球");
                return;
            }

            // 生成球
            var ball = SpawnBallInternal(playerId);
            if (ball != null)
            {
                // 附着到玩家的非持拍手
                ball.AttachBallToNonPaddleHandServerRpc(playerId);
                LogDebug($"为玩家 {playerId} 生成了球");
            }
        }

        /// <summary>
        /// 在指定位置生成球
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <param name="rotation">生成旋转</param>
        /// <returns>生成的球</returns>
        public BallNetworking SpawnBallAtPosition(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) return null;

            return SpawnBallInternal(ulong.MaxValue, position, rotation);
        }

        /// <summary>
        /// 内部球生成逻辑
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="position">生成位置（可选）</param>
        /// <param name="rotation">生成旋转（可选）</param>
        /// <returns>生成的球</returns>
        private BallNetworking SpawnBallInternal(ulong playerId, Vector3? position = null, Quaternion? rotation = null)
        {
            // 确定生成位置和旋转
            Vector3 spawnPos = position ?? GetNextSpawnPosition();
            Quaternion spawnRot = rotation ?? GetSpawnRotation();

            // 从对象池获取球对象
            NetworkObject ballNetworkObject;
            if (ballPool != null)
            {
                ballNetworkObject = ballPool.GetNetworkObject(pongBallPrefab.gameObject, spawnPos, spawnRot);
            }
            else
            {
                // 直接实例化（回退方案）
                var ballGameObject = Instantiate(pongBallPrefab.gameObject, spawnPos, spawnRot);
                ballNetworkObject = ballGameObject.GetComponent<NetworkObject>();
            }

            if (ballNetworkObject == null)
            {
                Debug.LogError("BallSpawner: 无法获取球网络对象");
                return null;
            }

            // 生成网络对象
            if (!ballNetworkObject.IsSpawned)
            {
                ballNetworkObject.Spawn();
            }

            // 获取球组件
            var ballComponent = ballNetworkObject.GetComponent<BallNetworking>();
            if (ballComponent == null)
            {
                Debug.LogError("BallSpawner: 球预制件缺少 BallNetworking 组件");
                return null;
            }

            // 注册球
            RegisterBall(ballComponent, playerId);

            return ballComponent;
        }

        /// <summary>
        /// 注册球到管理系统
        /// </summary>
        /// <param name="ball">球组件</param>
        /// <param name="playerId">关联的玩家ID</param>
        private void RegisterBall(BallNetworking ball, ulong playerId)
        {
            // 添加到活跃球列表
            activeBalls.Add(ball);

            // 如果有关联玩家，添加到玩家映射
            if (playerId != ulong.MaxValue)
            {
                playerBalls[playerId] = ball;
            }

            // 订阅球事件
            ball.BallDied += OnBallDied;

            // 触发事件
            OnBallSpawned?.Invoke(ball);
            OnActiveBallCountChanged?.Invoke(activeBalls.Count);

            LogDebug($"球已注册，当前活跃球数: {activeBalls.Count}");
        }
        #endregion

        #region Ball Despawning
        /// <summary>
        /// 销毁球
        /// </summary>
        /// <param name="ball">要销毁的球</param>
        /// <param name="immediately">是否立即销毁</param>
        public void DespawnBall(BallNetworking ball, bool immediately = false)
        {
            if (!IsServer || ball == null) return;

            if (immediately)
            {
                DespawnBallNow(ball);
            }
            else
            {
                StartCoroutine(DespawnBallCoroutine(ball));
            }
        }

        /// <summary>
        /// 销毁球协程
        /// </summary>
        /// <param name="ball">要销毁的球</param>
        /// <returns>协程</returns>
        private IEnumerator DespawnBallCoroutine(BallNetworking ball)
        {
            yield return respawnWait;

            if (ball != null && ball.gameObject != null)
            {
                DespawnBallNow(ball);
            }
        }

        /// <summary>
        /// 立即销毁球
        /// </summary>
        /// <param name="ball">要销毁的球</param>
        private void DespawnBallNow(BallNetworking ball)
        {
            if (ball == null) return;

            // 从管理列表中移除
            UnregisterBall(ball);

            // 返回到对象池或销毁
            if (ballPool != null && ball.NetworkObject.IsSpawned)
            {
                ball.NetworkObject.Despawn();
                // 对象池会自动处理回收
            }
            else if (ball.gameObject != null)
            {
                Destroy(ball.gameObject);
            }
        }

        /// <summary>
        /// 销毁所有球
        /// </summary>
        public void DespawnAllBalls()
        {
            if (!IsServer) return;

            var ballsToRemove = new List<BallNetworking>(activeBalls);
            foreach (var ball in ballsToRemove)
            {
                DespawnBallNow(ball);
            }

            LogDebug("所有球已被销毁");
        }

        /// <summary>
        /// 从管理系统注销球
        /// </summary>
        /// <param name="ball">球组件</param>
        private void UnregisterBall(BallNetworking ball)
        {
            // 取消订阅事件
            ball.BallDied -= OnBallDied;

            // 从活跃列表移除
            activeBalls.Remove(ball);

            // 从玩家映射移除
            var playerToRemove = ulong.MaxValue;
            foreach (var kvp in playerBalls)
            {
                if (kvp.Value == ball)
                {
                    playerToRemove = kvp.Key;
                    break;
                }
            }
            if (playerToRemove != ulong.MaxValue)
            {
                playerBalls.Remove(playerToRemove);
            }

            // 触发事件
            OnBallDespawned?.Invoke(ball);
            OnActiveBallCountChanged?.Invoke(activeBalls.Count);

            LogDebug($"球已注销，剩余活跃球数: {activeBalls.Count}");
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 球死亡事件处理
        /// </summary>
        /// <param name="ball">死亡的球</param>
        /// <param name="immediately">是否立即处理</param>
        private void OnBallDied(BallNetworking ball, bool immediately)
        {
            LogDebug($"球已死亡，立即处理: {immediately}");
            DespawnBall(ball, immediately);

            // 在比赛模式下可能需要重新生成球
            if (ShouldRespawnBall())
            {
                StartCoroutine(RespawnBallAfterDelay());
            }
        }

        /// <summary>
        /// 延迟重新生成球
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator RespawnBallAfterDelay()
        {
            yield return respawnWait;

            // 在中性位置生成新球
            if (ballSpawnPoints != null && ballSpawnPoints.Length > 0)
            {
                var spawnPoint = ballSpawnPoints[Random.Range(0, ballSpawnPoints.Length)];
                SpawnBallAtPosition(spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                SpawnBallAtPosition(Vector3.up * 2f, Quaternion.identity);
            }

            LogDebug("新球已重新生成");
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 检查是否可以为玩家生成球
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可以生成</returns>
        private bool CanSpawnBallForPlayer(ulong playerId)
        {
            // 检查最大球数限制
            if (activeBalls.Count >= maxActiveBalls)
            {
                LogDebug($"已达到最大球数限制: {maxActiveBalls}");
                return false;
            }

            // 检查玩家是否已有球
            if (playerBalls.ContainsKey(playerId))
            {
                LogDebug($"玩家 {playerId} 已有球附着");
                return false;
            }

            // 检查发球权限
            if (ServePermissionManager.Instance != null)
            {
                bool hasPermission = ServePermissionManager.Instance.CanPlayerServe(playerId);
                if (!hasPermission)
                {
                    LogDebug($"玩家 {playerId} 没有发球权限");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否应该重新生成球
        /// </summary>
        /// <returns>是否应该重新生成</returns>
        private bool ShouldRespawnBall()
        {
            // 在练习模式下总是重新生成
            if (ServePermissionManager.Instance != null &&
                ServePermissionManager.Instance.CurrentGamePhase.Value == ServePermissionManager.GamePhase.Practice)
            {
                return true;
            }

            // 在比赛模式下，如果没有活跃球则重新生成
            return activeBalls.Count == 0;
        }

        /// <summary>
        /// 获取下一个生成位置
        /// </summary>
        /// <returns>生成位置</returns>
        private Vector3 GetNextSpawnPosition()
        {
            if (ballSpawnPoints != null && ballSpawnPoints.Length > 0)
            {
                var spawnPoint = ballSpawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % ballSpawnPoints.Length;
                return spawnPoint.position;
            }

            if (defaultSpawnPoint != null)
            {
                return defaultSpawnPoint.position;
            }

            return Vector3.up * 2f; // 默认在空中2米高度
        }

        /// <summary>
        /// 获取生成旋转
        /// </summary>
        /// <returns>生成旋转</returns>
        private Quaternion GetSpawnRotation()
        {
            if (ballSpawnPoints != null && ballSpawnPoints.Length > 0)
            {
                var currentIndex = (nextSpawnPointIndex - 1 + ballSpawnPoints.Length) % ballSpawnPoints.Length;
                return ballSpawnPoints[currentIndex].rotation;
            }

            if (defaultSpawnPoint != null)
            {
                return defaultSpawnPoint.rotation;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// 自动生成初始球（测试用）
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator AutoSpawnInitialBalls()
        {
            yield return new WaitForSeconds(1f); // 等待网络初始化

            if (ballSpawnPoints != null)
            {
                foreach (var spawnPoint in ballSpawnPoints)
                {
                    if (activeBalls.Count >= maxActiveBalls) break;

                    SpawnBallAtPosition(spawnPoint.position, spawnPoint.rotation);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                SpawnBallAtPosition(Vector3.up * 2f, Quaternion.identity);
            }

            LogDebug("自动生成初始球完成");
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 获取指定玩家的球
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家的球，如果没有则返回null</returns>
        public BallNetworking GetPlayerBall(ulong playerId)
        {
            playerBalls.TryGetValue(playerId, out var ball);
            return ball;
        }

        /// <summary>
        /// 获取所有活跃球
        /// </summary>
        /// <returns>活跃球列表副本</returns>
        public List<BallNetworking> GetActiveBalls()
        {
            return new List<BallNetworking>(activeBalls);
        }

        /// <summary>
        /// 获取活跃球数量
        /// </summary>
        /// <returns>活跃球数量</returns>
        public int GetActiveBallCount()
        {
            return activeBalls.Count;
        }

        /// <summary>
        /// 设置最大活跃球数
        /// </summary>
        /// <param name="maxBalls">最大球数</param>
        public void SetMaxActiveBalls(int maxBalls)
        {
            maxActiveBalls = Mathf.Max(1, maxBalls);
            LogDebug($"最大活跃球数设置为: {maxActiveBalls}");
        }

        /// <summary>
        /// 获取生成器状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetSpawnerInfo()
        {
            return $"活跃球数: {activeBalls.Count}/{maxActiveBalls}\n" +
                   $"玩家球数: {playerBalls.Count}\n" +
                   $"对象池: {(ballPool != null ? "已启用" : "未启用")}\n" +
                   $"生成点数: {(ballSpawnPoints?.Length ?? 0)}";
        }
        #endregion

        #region Debug
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[BallSpawner] {message}");
            }
        }

        private void OnDrawGizmos()
        {
            // 绘制生成点
            if (ballSpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var spawnPoint in ballSpawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
                        Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 0.3f);
                    }
                }
            }

            // 绘制默认生成点
            if (defaultSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(defaultSpawnPoint.position, 0.15f);
            }

            // 绘制活跃球
            Gizmos.color = Color.blue;
            foreach (var ball in activeBalls)
            {
                if (ball != null)
                {
                    Gizmos.DrawWireSphere(ball.transform.position, 0.02f);
                }
            }
        }
        #endregion
    }
}