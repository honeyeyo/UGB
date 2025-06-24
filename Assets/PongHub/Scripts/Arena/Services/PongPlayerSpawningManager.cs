// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using Unity.Netcode;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Spectator;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球玩家生成管理器
    /// 专门为VR乒乓球游戏优化的生成系统
    /// 管理球桌周围的精确定位和VR空间安全
    /// </summary>
    public class PongPlayerSpawningManager : SpawningManagerBase
    {
        #region Serialized Fields
        [Header("生成配置")]
        [SerializeField] private PongSpawnConfiguration m_spawnConfig;

        [Header("VR传送效果")]
        [SerializeField] private GameObject m_teleportEffectPrefab;
        [SerializeField] private AudioClip m_teleportSound;
        [SerializeField] private float m_teleportDuration = 1.0f;

        [Header("调试设置")]
        [SerializeField] private bool m_enableDebugVisualization = true;
        [SerializeField] private bool m_logSpawnEvents = true;
        #endregion

        #region Private Fields
        private AudioSource m_audioSource;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // 调用基类的Awake
            base.Awake();

            // 获取或添加音频源组件
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 确保生成配置存在
            if (m_spawnConfig == null)
            {
                m_spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
                if (m_spawnConfig == null)
                {
                    Debug.LogError("[PongPlayerSpawningManager] 未找到PongSpawnConfiguration组件！");
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // 注册乒乓球游戏事件
                PongGameEvents.OnPlayerJoined += OnPlayerJoined;
                PongGameEvents.OnPlayerLeft += OnPlayerLeft;
                PongGameEvents.OnGameModeChanged += OnGameModeChanged;

                Debug.Log("[PongPlayerSpawningManager] 服务器端生成管理器已启动");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // 清理事件订阅
                PongGameEvents.OnPlayerJoined -= OnPlayerJoined;
                PongGameEvents.OnPlayerLeft -= OnPlayerLeft;
                PongGameEvents.OnGameModeChanged -= OnGameModeChanged;
            }
        }
        #endregion

        #region Event Handlers
        private void OnPlayerJoined(string playerId)
        {
            if (IsServer)
            {
                var playerData = PongSessionManager.Instance.GetPlayerData(playerId);
                if (playerData.HasValue)
                {
                    SchedulePlayerSpawn(playerData.Value);
                }
            }
        }

        private void OnPlayerLeft(string playerId)
        {
            if (IsServer)
            {
                var playerData = PongSessionManager.Instance.GetPlayerData(playerId);
                if (playerData.HasValue && playerData.Value.SpawnPointIndex >= 0)
                {
                    ReleasePlayerSpawnPoint(playerData.Value);
                }
            }
        }

        private void OnGameModeChanged(PongGameMode newMode)
        {
            if (IsServer && newMode != PongGameMode.Waiting)
            {
                RespawnAllPlayers();
            }
        }
        #endregion

        #region Player Spawning
        /// <summary>
        /// 安排玩家生成
        /// </summary>
        private void SchedulePlayerSpawn(PongPlayerData playerData)
        {
            StartCoroutine(SpawnPlayerCoroutine(playerData));
        }

        /// <summary>
        /// 生成玩家协程
        /// </summary>
        private System.Collections.IEnumerator SpawnPlayerCoroutine(PongPlayerData playerData)
        {
            // 等待一帧确保网络数据同步
            yield return null;

            Transform spawnPoint = null;

            if (playerData.IsSpectator)
            {
                spawnPoint = m_spawnConfig.GetSpectatorSpawnPoint(playerData.SelectedTeam);
            }
            else
            {
                var currentMode = PongSessionManager.Instance.CurrentGameMode;
                spawnPoint = m_spawnConfig.GetPlayerSpawnPoint(currentMode, playerData.SelectedTeam, playerData.TeamPosition);
            }

            if (spawnPoint != null && m_spawnConfig.OccupySpawnPoint(spawnPoint))
            {
                // 更新玩家数据中的生成点信息
                var updatedData = playerData;
                updatedData.SpawnPointIndex = GetSpawnPointIndex(spawnPoint);
                PongSessionManager.Instance.SetPlayerData(playerData.ClientId, updatedData);

                // 执行生成
                SpawnPlayerAtPosition(playerData.ClientId, spawnPoint.position, spawnPoint.rotation);

                if (m_logSpawnEvents)
                {
                    Debug.Log($"[PongPlayerSpawningManager] 玩家 {playerData.PlayerId} 生成在 {spawnPoint.name}");
                }
            }
            else
            {
                Debug.LogError($"[PongPlayerSpawningManager] 无法为玩家 {playerData.PlayerId} 找到合适的生成点");
            }
        }

        /// <summary>
        /// 在指定位置生成玩家
        /// </summary>
        private void SpawnPlayerAtPosition(ulong clientId, Vector3 position, Quaternion rotation)
        {
            // 通知客户端进行传送
            TeleportPlayerClientRpc(position, rotation, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
        }

        /// <summary>
        /// 重新生成所有玩家
        /// </summary>
        private void RespawnAllPlayers()
        {
            // 清空所有生成点
            m_spawnConfig.ResetAllSpawnPoints();

            // 重新生成所有连接的玩家
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                var playerData = PongSessionManager.Instance.GetPlayerData(clientId);
                if (playerData.HasValue && playerData.Value.IsConnected)
                {
                    SchedulePlayerSpawn(playerData.Value);
                }
            }
        }

        /// <summary>
        /// 释放玩家生成点
        /// </summary>
        private void ReleasePlayerSpawnPoint(PongPlayerData playerData)
        {
            if (playerData.SpawnPointIndex >= 0)
            {
                var spawnPoint = GetSpawnPointByIndex(playerData.SpawnPointIndex);
                if (spawnPoint != null)
                {
                    m_spawnConfig.ReleaseSpawnPoint(spawnPoint);
                }
            }
        }

        /// <summary>
        /// 传送玩家的客户端RPC
        /// </summary>
        [ClientRpc]
        private void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            StartCoroutine(PerformTeleportEffect(position, rotation));
        }

        /// <summary>
        /// 执行传送特效
        /// </summary>
        private System.Collections.IEnumerator PerformTeleportEffect(Vector3 targetPosition, Quaternion targetRotation)
        {
            // 开始淡出效果
            yield return StartCoroutine(FadeOut());

            // 传送本地玩家
            TeleportLocalPlayer(targetPosition, targetRotation);

            // 播放传送特效
            if (m_teleportEffectPrefab != null)
            {
                var effect = Instantiate(m_teleportEffectPrefab, targetPosition, targetRotation);
                Destroy(effect, 2.0f);
            }

            // 播放传送音效
            if (m_teleportSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_teleportSound);
            }

            // 开始淡入效果
            yield return StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 传送本地玩家
        /// </summary>
        private void TeleportLocalPlayer(Vector3 position, Quaternion rotation)
        {
            var playerController = FindLocalPlayerController();
            if (playerController != null)
            {
                // 对于VR，需要考虑到TrackingSpace
                var trackingSpace = playerController.transform.Find("TrackingSpace");
                if (trackingSpace != null)
                {
                    // VR模式：调整TrackingSpace
                    trackingSpace.position = position;
                    trackingSpace.rotation = rotation;
                }
                else
                {
                    // 非VR模式：直接设置玩家位置
                    playerController.transform.position = position;
                    playerController.transform.rotation = rotation;
                }

                Debug.Log($"[PongPlayerSpawningManager] 本地玩家已传送到 {position}");
            }
            else
            {
                Debug.LogWarning("[PongPlayerSpawningManager] 未找到本地玩家控制器");
            }
        }

        /// <summary>
        /// 查找本地玩家控制器
        /// </summary>
        private GameObject FindLocalPlayerController()
        {
            // 尝试通过常见标签查找
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                return playerObject;

            // 尝试查找VR相机装备
            var vrCamera = Camera.main;
            if (vrCamera != null)
                return vrCamera.transform.root.gameObject;

            // 尝试查找OVR相机装备
#if UNITY_ANDROID && !UNITY_EDITOR
            var ovrCameraRig = FindObjectOfType<OVRCameraRig>();
            if (ovrCameraRig != null)
                return ovrCameraRig.gameObject;
#endif

            return null;
        }
        #endregion

        #region Spectator Management
        /// <summary>
        /// 将玩家切换为观众
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SwitchToSpectatorServerRpc(NetworkedTeam.Team preferredSide, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            var playerData = PongSessionManager.Instance.GetPlayerData(clientId);

            if (playerData.HasValue && !playerData.Value.IsSpectator)
            {
                // 释放当前生成点
                ReleasePlayerSpawnPoint(playerData.Value);

                // 更新为观众
                var updatedData = playerData.Value;
                updatedData.IsSpectator = true;
                updatedData.SelectedTeam = preferredSide;
                updatedData.IsReady = true; // 观众总是准备就绪

                PongSessionManager.Instance.SetPlayerData(clientId, updatedData);

                // 重新生成为观众
                SchedulePlayerSpawn(updatedData);
            }
        }

        /// <summary>
        /// 将观众切换为玩家
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SwitchToPlayerServerRpc(ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            var playerData = PongSessionManager.Instance.GetPlayerData(clientId);

            if (playerData.HasValue && playerData.Value.IsSpectator)
            {
                // 检查是否有可用的玩家位置
                var currentMode = PongSessionManager.Instance.CurrentGameMode;
                var availableSlots = m_spawnConfig.GetAvailablePlayerSlots(currentMode);
                var currentPlayers = PongSessionManager.Instance.ActivePlayerCount;

                if (currentPlayers < availableSlots)
                {
                    // 释放观众位置
                    ReleasePlayerSpawnPoint(playerData.Value);

                    // 更新为玩家
                    var updatedData = playerData.Value;
                    updatedData.IsSpectator = false;
                    updatedData.IsReady = false; // 需要重新准备

                    PongSessionManager.Instance.SetPlayerData(clientId, updatedData);

                    // 触发队伍重新平衡
                    PongGameEvents.OnPlayerCountChanged?.Invoke(PongSessionManager.Instance.ActivePlayerCount);
                }
                else
                {
                    Debug.Log($"[PongPlayerSpawningManager] 玩家位置已满，无法将观众转为玩家");
                }
            }
        }

        /// <summary>
        /// 切换观众席位 - 为SpectatorNetwork提供的接口
        /// </summary>
        public Transform SwitchSpectatorSide(ulong clientId, SpectatorNetwork spectatorNetwork)
        {
            var sessionManager = PongSessionManager.Instance;
            var spawnConfig = m_spawnConfig;

            if (sessionManager == null || spawnConfig == null)
            {
                Debug.LogWarning("[PongPlayerSpawningManager] 未找到会话管理器或生成配置");
                return null;
            }

            var playerData = sessionManager.GetPlayerData(clientId);
            if (!playerData.HasValue || !playerData.Value.IsSpectator)
            {
                Debug.LogWarning($"[PongPlayerSpawningManager] 客户端 {clientId} 不是观众或数据无效");
                return null;
            }

            // 释放当前观众席位
            var currentSpawnPoint = GetCurrentSpectatorSpawnPoint(playerData.Value);
            if (currentSpawnPoint != null)
            {
                spawnConfig.ReleaseSpawnPoint(currentSpawnPoint);
            }

            // 切换到对面队伍的观众席
            var newTeam = playerData.Value.SelectedTeam == NetworkedTeam.Team.TeamA
                ? NetworkedTeam.Team.TeamB
                : NetworkedTeam.Team.TeamA;

            // 获取新的观众席位
            var newSpawnPoint = spawnConfig.GetSpectatorSpawnPoint(newTeam);
            if (newSpawnPoint != null && spawnConfig.OccupySpawnPoint(newSpawnPoint))
            {
                // 更新玩家数据
                var updatedData = playerData.Value;
                updatedData.SelectedTeam = newTeam;
                updatedData.SpawnPointIndex = GetSpawnPointIndex(newSpawnPoint);
                sessionManager.SetPlayerData(clientId, updatedData);

                // 更新观众颜色 - 使用正确的TeamColorProfiles接口
                var teamColor = GetTeamColorForSide(newTeam);
                spectatorNetwork.TeamSideColor = teamColor;

                if (m_logSpawnEvents)
                {
                    Debug.Log($"[PongPlayerSpawningManager] 观众 {clientId} 从 {playerData.Value.SelectedTeam} 切换到 {newTeam}");
                }
                return newSpawnPoint;
            }
            else
            {
                Debug.LogWarning($"[PongPlayerSpawningManager] 无法为观众 {clientId} 找到 {newTeam} 队的空闲观众席");

                // 重新占用原位置
                if (currentSpawnPoint != null)
                {
                    spawnConfig.OccupySpawnPoint(currentSpawnPoint);
                }
                return null;
            }
        }

        /// <summary>
        /// 根据队伍获取对应的颜色
        /// </summary>
        private TeamColor GetTeamColorForSide(NetworkedTeam.Team team)
        {
            // 获取当前的队伍颜色配置
            TeamColorProfiles.Instance.GetRandomProfile(out var teamColorA, out var teamColorB);

            // 根据队伍返回对应的颜色
            return team == NetworkedTeam.Team.TeamA ? teamColorA : teamColorB;
        }

        /// <summary>
        /// 重置游戏中的生成点 - 为GameManager提供的接口
        /// </summary>
        public void ResetInGameSpawnPoints()
        {
            if (m_spawnConfig != null)
            {
                m_spawnConfig.ResetAllSpawnPoints();

                if (m_logSpawnEvents)
                {
                    Debug.Log("[PongPlayerSpawningManager] 已重置游戏中的生成点");
                }
            }
            else
            {
                Debug.LogWarning("[PongPlayerSpawningManager] 未找到生成配置组件");
            }
        }

        /// <summary>
        /// 重置赛后的生成点 - 为GameManager提供的接口
        /// </summary>
        public void ResetPostGameSpawnPoints()
        {
            if (m_spawnConfig != null)
            {
                m_spawnConfig.ResetAllSpawnPoints();

                if (m_logSpawnEvents)
                {
                    Debug.Log("[PongPlayerSpawningManager] 已重置赛后的生成点");
                }
            }
            else
            {
                Debug.LogWarning("[PongPlayerSpawningManager] 未找到生成配置组件");
            }
        }
        #endregion

        #region Visual Effects
        /// <summary>
        /// 淡出效果
        /// </summary>
        private System.Collections.IEnumerator FadeOut()
        {
            // VR渐变效果实现
            yield return new WaitForSeconds(m_teleportDuration * 0.3f);
        }

        /// <summary>
        /// 淡入效果
        /// </summary>
        private System.Collections.IEnumerator FadeIn()
        {
            // VR渐变效果实现
            yield return new WaitForSeconds(m_teleportDuration * 0.3f);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 获取生成点索引
        /// </summary>
        private int GetSpawnPointIndex(Transform spawnPoint)
        {
            if (m_spawnConfig == null) return -1;

            // 在A队观众席中查找
            for (int i = 0; i < m_spawnConfig.spectatorA_Positions.Length; i++)
            {
                if (m_spawnConfig.spectatorA_Positions[i] == spawnPoint)
                    return i;
            }

            // 在B队观众席中查找
            for (int i = 0; i < m_spawnConfig.spectatorB_Positions.Length; i++)
            {
                if (m_spawnConfig.spectatorB_Positions[i] == spawnPoint)
                    return i;
            }

            // 在玩家生成点中查找
            if (spawnPoint == m_spawnConfig.teamA_Position1) return 1000;
            if (spawnPoint == m_spawnConfig.teamA_Position2) return 1001;
            if (spawnPoint == m_spawnConfig.teamB_Position1) return 1002;
            if (spawnPoint == m_spawnConfig.teamB_Position2) return 1003;

            return -1;
        }

        /// <summary>
        /// 根据索引获取生成点
        /// </summary>
        private Transform GetSpawnPointByIndex(int index)
        {
            if (m_spawnConfig == null) return null;

            // 玩家生成点
            switch (index)
            {
                case 1000: return m_spawnConfig.teamA_Position1;
                case 1001: return m_spawnConfig.teamA_Position2;
                case 1002: return m_spawnConfig.teamB_Position1;
                case 1003: return m_spawnConfig.teamB_Position2;
            }

            // A队观众席
            if (index >= 0 && index < m_spawnConfig.spectatorA_Positions.Length)
                return m_spawnConfig.spectatorA_Positions[index];

            // B队观众席（偏移处理）
            int bOffset = m_spawnConfig.spectatorA_Positions.Length;
            if (index >= bOffset && index < bOffset + m_spawnConfig.spectatorB_Positions.Length)
                return m_spawnConfig.spectatorB_Positions[index - bOffset];

            return null;
        }

        /// <summary>
        /// 获取当前观众的生成点
        /// </summary>
        private Transform GetCurrentSpectatorSpawnPoint(PongPlayerData playerData)
        {
            if (m_spawnConfig == null) return null;

            // 根据当前队伍和生成点索引找到对应的Transform
            var spectatorPositions = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_spawnConfig.spectatorA_Positions
                : m_spawnConfig.spectatorB_Positions;

            if (playerData.SpawnPointIndex >= 0 && playerData.SpawnPointIndex < spectatorPositions.Length)
            {
                return spectatorPositions[playerData.SpawnPointIndex];
            }

            return null;
        }

        /// <summary>
        /// 验证生成位置安全性
        /// </summary>
        private bool IsSpawnPositionSafe(Vector3 position)
        {
            return m_spawnConfig.IsPositionSafe(position);
        }
        #endregion

        #region Debug Visualization
        private void OnDrawGizmos()
        {
            if (!m_enableDebugVisualization || m_spawnConfig == null)
                return;

            // 绘制安全区域
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, m_spawnConfig.playerSafeRadius);

            // 绘制生成点
            DrawSpawnPoint(m_spawnConfig.teamA_Position1, Color.blue, "A1");
            DrawSpawnPoint(m_spawnConfig.teamA_Position2, Color.blue, "A2");
            DrawSpawnPoint(m_spawnConfig.teamB_Position1, Color.red, "B1");
            DrawSpawnPoint(m_spawnConfig.teamB_Position2, Color.red, "B2");
        }

        private void DrawSpawnPoint(Transform spawnPoint, Color color, string label)
        {
            if (spawnPoint == null) return;

            Gizmos.color = color;
            Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.5f);
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 1.0f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 0.5f, label);
#endif
        }
        #endregion

        #region Override Abstract Methods
        // 实现抽象基类的方法
        public override NetworkObject SpawnPlayer(ulong clientId, string playerId, bool isSpectator, Vector3 playerPos)
        {
            // 使用现有的协程生成系统
            var playerData = PongSessionManager.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                SchedulePlayerSpawn(playerData.Value);
            }

            // 这里返回null，因为实际生成是异步的
            // 如果需要同步返回NetworkObject，需要重构生成逻辑
            return null;
        }

        public override void GetRespawnPoint(ulong clientId, NetworkedTeam.Team team,
            out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            var sessionManager = PongSessionManager.Instance;

            if (m_spawnConfig == null || sessionManager == null)
            {
                Debug.LogWarning("[PongPlayerSpawningManager] 未找到生成配置或会话管理器");
                return;
            }

            var playerData = sessionManager.GetPlayerData(clientId);
            if (!playerData.HasValue)
            {
                Debug.LogWarning($"[PongPlayerSpawningManager] 未找到客户端 {clientId} 的玩家数据");
                return;
            }

            Transform spawnPoint = null;
            var currentMode = sessionManager.CurrentGameMode;

            if (playerData.Value.IsSpectator)
            {
                // 观众生成点
                spawnPoint = m_spawnConfig.GetSpectatorSpawnPoint(team);
            }
            else
            {
                // 玩家生成点
                spawnPoint = m_spawnConfig.GetPlayerSpawnPoint(currentMode, team, playerData.Value.TeamPosition);
            }

            if (spawnPoint != null)
            {
                position = spawnPoint.position;
                rotation = spawnPoint.rotation;
            }
            else
            {
                // 回退到队伍中心位置
                position = m_spawnConfig.GetTeamCenter(team);
                rotation = Quaternion.LookRotation(team == NetworkedTeam.Team.TeamA ? Vector3.forward : Vector3.back);
                Debug.LogWarning($"[PongPlayerSpawningManager] 未找到合适的生成点，使用队伍中心位置");
            }
        }
        #endregion
    }
}