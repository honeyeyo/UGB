// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using PongHub.Arena.Crowd;
using PongHub.Arena.Player;
using PongHub.Arena.Services;
using PongHub.Arena.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Spectator
{
    /// <summary>
    /// Network representation of the spectator. It chooses a random body form and spectators can selection which item
    /// their spectator representation shows, which is propagated through the network. It also handles the firework
    /// launching.
    /// </summary>
    public class SpectatorNetwork : NetworkBehaviour
    {
        private const float ITEM_CHANGE_PROPAGATION_DELAY = 1f;
        [SerializeField] private CrowdNPC[] m_spectatorPrefabs;
        [SerializeField] private Color m_bodyColor;

        [SerializeField] private SpectatorItem[] m_itemPrefabs;

        [SerializeField] private FireworkLauncherItem m_fireworkLauncher;

        private NetworkVariable<int> m_prefabIndex = new(-1);
        private NetworkVariable<TeamColor> m_teamColor = new();

        private NetworkVariable<int> m_itemIndexNet = new(-1, writePerm: NetworkVariableWritePermission.Owner);

        private SpectatorItem[] m_items;
        private int m_itemIndex = 0;

        private CrowdNPC m_spectator;

        private bool m_willSendItemChange = false;

        public TeamColor TeamSideColor
        {
            set => m_teamColor.Value = value;
        }

        private void Awake()
        {
            m_prefabIndex.OnValueChanged += OnPrefabIndexChanged;
            m_teamColor.OnValueChanged += OnTeamColorChanged;
        }

        public override void OnDestroy()
        {
            if (m_items != null)
            {
                foreach (var item in m_items)
                {
                    if (item)
                    {
                        Destroy(item.gameObject);
                    }
                }
            }

            if (m_fireworkLauncher != null)
            {
                Destroy(m_fireworkLauncher.gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                PlayerMovement.Instance.SnapPositionToTransform(transform);
                OVRScreenFade.instance.FadeIn();
                PlayerInputController.Instance.SetSpectatorMode(this);

                var cameraRig = FindObjectOfType<OVRCameraRig>();
                m_items = new SpectatorItem[m_itemPrefabs.Length];
                m_itemIndex = Random.Range(0, m_itemPrefabs.Length);
                m_itemIndexNet.Value = m_itemIndex;
                for (var i = 0; i < m_itemPrefabs.Length; ++i)
                {
                    m_items[i] = Instantiate(m_itemPrefabs[i], cameraRig.leftControllerAnchor, false);
                    m_items[i].gameObject.SetActive(i == m_itemIndex);
                }

                m_fireworkLauncher.gameObject.SetActive(true);
                m_fireworkLauncher.transform.SetParent(cameraRig.rightControllerAnchor, false);
                m_fireworkLauncher.OnLaunch += OnFireworkLaunchServerRPC;
                OnTeamColorChanged(m_teamColor.Value, m_teamColor.Value);
            }
            else
            {
                m_itemIndexNet.OnValueChanged += OnItemChanged;
                enabled = false;
            }

            if (IsServer)
            {
                m_prefabIndex.Value = Random.Range(0, m_spectatorPrefabs.Length);
            }
            else
            {
                OnPrefabIndexChanged(m_prefabIndex.Value, m_prefabIndex.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                PlayerInputController.Instance.SetSpectatorMode(null);
            }
        }

        private void OnPrefabIndexChanged(int previousvalue, int newvalue)
        {
            // don't spawn for owner
            if (!IsOwner)
            {
                if (newvalue > -1 && m_spectator == null)
                {
                    var prefab = m_spectatorPrefabs[newvalue];
                    m_spectator = Instantiate(prefab, transform, false);
                    m_spectator.SetBodyColor(m_bodyColor);
                    OnTeamColorChanged(m_teamColor.Value, m_teamColor.Value);
                }
            }
        }

        private void OnTeamColorChanged(TeamColor previousvalue, TeamColor newvalue)
        {
            if (m_spectator)
            {
                var color = TeamColorProfiles.Instance.GetColorForKey(newvalue);
                m_spectator.SetColor(color);
            }

            if (IsOwner)
            {
                var color = TeamColorProfiles.Instance.GetColorForKey(newvalue);
                foreach (var item in m_items)
                {
                    item.SetColor(color);
                }

                m_fireworkLauncher.SetColor(color);
            }
        }

        private void OnItemChanged(int previousvalue, int newvalue)
        {
            if (m_spectator)
            {
                m_spectator.ChangeItem(newvalue);
            }
        }

        public void RequestSwitchSide()
        {
            RequestSwitchSideServerRPC();
        }

        [ServerRpc]
        private void RequestSwitchSideServerRPC()
        {
            var newLocation = SwitchSpectatorSide(OwnerClientId, this);

            if (newLocation)
            {
                OnSideChangedClientRPC(newLocation.position, newLocation.rotation);
            }
        }

        /// <summary>
        /// 使用新的乒乓球系统切换观众席位
        /// </summary>
        private Transform SwitchSpectatorSide(ulong clientId, SpectatorNetwork spectatorNetwork)
        {
            var sessionManager = PongSessionManager.Instance;
            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();

            if (sessionManager == null || spawnConfig == null)
            {
                Debug.LogWarning("[SpectatorNetwork] 未找到会话管理器或生成配置");
                return null;
            }

            var playerData = sessionManager.GetPlayerData(clientId);
            if (!playerData.HasValue || !playerData.Value.IsSpectator)
            {
                Debug.LogWarning($"[SpectatorNetwork] 客户端 {clientId} 不是观众或数据无效");
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

                Debug.Log($"[SpectatorNetwork] 观众 {clientId} 从 {playerData.Value.SelectedTeam} 切换到 {newTeam}");
                return newSpawnPoint;
            }
            else
            {
                Debug.LogWarning($"[SpectatorNetwork] 无法为观众 {clientId} 找到 {newTeam} 队的空闲观众席");

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
        /// 获取当前观众的生成点
        /// </summary>
        private Transform GetCurrentSpectatorSpawnPoint(PongPlayerData playerData)
        {
            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
            if (spawnConfig == null) return null;

            // 根据当前队伍和生成点索引找到对应的Transform
            var spectatorPositions = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? spawnConfig.spectatorA_Positions
                : spawnConfig.spectatorB_Positions;

            if (playerData.SpawnPointIndex >= 0 && playerData.SpawnPointIndex < spectatorPositions.Length)
            {
                return spectatorPositions[playerData.SpawnPointIndex];
            }

            return null;
        }

        /// <summary>
        /// 获取生成点在数组中的索引
        /// </summary>
        private int GetSpawnPointIndex(Transform spawnPoint)
        {
            var spawnConfig = FindObjectOfType<PongSpawnConfiguration>();
            if (spawnConfig == null) return -1;

            // 在A队观众席中查找
            for (int i = 0; i < spawnConfig.spectatorA_Positions.Length; i++)
            {
                if (spawnConfig.spectatorA_Positions[i] == spawnPoint)
                    return i;
            }

            // 在B队观众席中查找
            for (int i = 0; i < spawnConfig.spectatorB_Positions.Length; i++)
            {
                if (spawnConfig.spectatorB_Positions[i] == spawnPoint)
                    return i;
            }

            return -1;
        }

        [ClientRpc]
        public void OnSideChangedClientRPC(Vector3 newPos, Quaternion newRotation)
        {
            var thisTrans = transform;
            thisTrans.position = newPos;
            thisTrans.rotation = newRotation;
            if (IsOwner)
            {
                PlayerMovement.Instance.SnapPosition(newPos, newRotation);
            }
        }

        public void TriggerLeftAction()
        {
            if (m_items is { Length: > 0 })
            {
                m_items[m_itemIndex].gameObject.SetActive(false);
                m_itemIndex = ++m_itemIndex % m_items.Length;
                m_items[m_itemIndex].gameObject.SetActive(true);
                if (!m_willSendItemChange)
                {
                    _ = StartCoroutine(DelayItemChangePropagation());
                }
            }
        }

        public void TriggerRightAction()
        {
            m_fireworkLauncher.TryLaunch();
        }

        [ServerRpc]
        private void OnFireworkLaunchServerRPC(Vector3 destination, float travelTime)
        {
            OnFireworkLaunchClientRpc(destination, travelTime);
        }

        [ClientRpc]
        private void OnFireworkLaunchClientRpc(Vector3 destination, float travelTime)
        {
            if (!IsOwner)
            {
                SpectatorFireworkController.Instance.DelayFireworkAt(destination, travelTime);
            }
        }

        private IEnumerator DelayItemChangePropagation()
        {
            if (m_willSendItemChange)
            {
                yield break;
            }

            m_willSendItemChange = true;

            var timer = Time.deltaTime;
            while (timer < ITEM_CHANGE_PROPAGATION_DELAY)
            {
                yield return null;
                timer += Time.deltaTime;
            }

            m_itemIndexNet.Value = m_itemIndex;

            m_willSendItemChange = false;
        }
    }
}