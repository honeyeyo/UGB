// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using Unity.Netcode;
using PongHub.Arena.Gameplay;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球玩家生成管理器
    /// 专门为VR乒乓球游戏优化的生成系统
    /// 管理球桌周围的精确定位和VR空间安全
    /// </summary>
    public class PongPlayerSpawningManager : NetworkBehaviour
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
        private void Awake()
        {
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
            var spawnPoint = GetSpawnPointByIndex(playerData.SpawnPointIndex);
            if (spawnPoint != null)
            {
                m_spawnConfig.ReleaseSpawnPoint(spawnPoint);

                if (m_logSpawnEvents)
                {
                    Debug.Log($"[PongPlayerSpawningManager] 释放玩家 {playerData.PlayerId} 的生成点");
                }
            }
        }
        #endregion

        #region Client RPCs
        /// <summary>
        /// 传送玩家到指定位置
        /// </summary>
        [ClientRpc]
        private void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            StartCoroutine(PerformTeleportEffect(position, rotation));
        }

        /// <summary>
        /// 执行传送效果
        /// </summary>
        private System.Collections.IEnumerator PerformTeleportEffect(Vector3 targetPosition, Quaternion targetRotation)
        {
            // 播放传送音效
            if (m_teleportSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_teleportSound);
            }

            // 生成传送特效
            if (m_teleportEffectPrefab != null)
            {
                var effect = Instantiate(m_teleportEffectPrefab, targetPosition, targetRotation);
                Destroy(effect, m_teleportDuration * 2f);
            }

            // 淡出效果（如果有VR渐变组件）
            yield return StartCoroutine(FadeOut());

            // 移动玩家
            TeleportLocalPlayer(targetPosition, targetRotation);

            // 淡入效果
            yield return StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 传送本地玩家
        /// </summary>
        private void TeleportLocalPlayer(Vector3 position, Quaternion rotation)
        {
            // 查找玩家的角色控制器或VR装备
            var playerController = FindLocalPlayerController();
            if (playerController != null)
            {
                // 方法1: 如果有CharacterController
                var characterController = playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    playerController.transform.position = position;
                    playerController.transform.rotation = rotation;
                    characterController.enabled = true;
                }
                else
                {
                    // 方法2: 直接设置Transform
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
            // 简单实现：使用Transform的GetSiblingIndex
            return spawnPoint.GetSiblingIndex();
        }

        /// <summary>
        /// 根据索引获取生成点
        /// </summary>
        private Transform GetSpawnPointByIndex(int index)
        {
            // 这里需要根据实际的生成点管理逻辑实现
            // 简单实现：遍历所有生成点找到匹配的索引
            return null; // 暂时返回null，需要具体实现
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
    }
}