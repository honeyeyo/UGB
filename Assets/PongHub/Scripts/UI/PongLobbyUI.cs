// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using PongHub.Arena.Services;
using PongHub.Arena.Gameplay;

namespace PongHub.UI
{
    /// <summary>
    /// 乒乓球大厅UI管理器
    /// 提供完整的大厅界面功能，包括模式选择、队伍显示、准备系统
    /// 专门为VR环境优化的UI布局和交互
    /// </summary>
    public class PongLobbyUI : MonoBehaviour
    {
        #region UI References - Main Panels
        [Header("主要面板")]
        [SerializeField]
        [Tooltip("Lobby Panel / 大厅面板 - Main lobby interface panel")]
        private GameObject m_lobbyPanel;

        [SerializeField]
        [Tooltip("Waiting Panel / 等待面板 - Panel for waiting room interface")]
        private GameObject m_waitingPanel;

        [SerializeField]
        [Tooltip("Game Mode Panel / 游戏模式面板 - Panel for game mode selection")]
        private GameObject m_gameModePanel;

        [SerializeField]
        [Tooltip("Team Panel / 队伍面板 - Panel for team assignment interface")]
        private GameObject m_teamPanel;

        [SerializeField]
        [Tooltip("Ready Panel / 准备面板 - Panel for ready check interface")]
        private GameObject m_readyPanel;

        [SerializeField]
        [Tooltip("Game Panel / 游戏面板 - Panel for in-game interface")]
        private GameObject m_gamePanel;
        #endregion

        #region UI References - Waiting Panel
        [Header("等待面板")]
        [SerializeField]
        [Tooltip("Waiting Status Text / 等待状态文本 - Text for displaying waiting status")]
        private TextMeshProUGUI m_waitingStatusText;

        [SerializeField]
        [Tooltip("Player Count Text / 玩家数量文本 - Text for displaying player count")]
        private TextMeshProUGUI m_playerCountText;

        [SerializeField]
        [Tooltip("Join As Player Button / 加入为玩家按钮 - Button for joining as player")]
        private Button m_joinAsPlayerButton;

        [SerializeField]
        [Tooltip("Join As Spectator Button / 加入为观众按钮 - Button for joining as spectator")]
        private Button m_joinAsSpectatorButton;

        [SerializeField]
        [Tooltip("Host Controls Toggle / 主机控制开关 - Toggle for host controls")]
        private Toggle m_hostControlsToggle;
        #endregion

        #region UI References - Game Mode Panel
        [Header("游戏模式面板")]
        [SerializeField]
        [Tooltip("Host Mode Selection / 主机模式选择 - Container for host mode selection")]
        private GameObject m_hostModeSelection;

        [SerializeField]
        [Tooltip("Singles Button / 单打按钮 - Button for selecting singles mode")]
        private Button m_singlesButton;

        [SerializeField]
        [Tooltip("Doubles Button / 双打按钮 - Button for selecting doubles mode")]
        private Button m_doublesButton;

        [SerializeField]
        [Tooltip("Auto Mode Button / 自动模式按钮 - Button for selecting auto mode")]
        private Button m_autoModeButton;

        [SerializeField]
        [Tooltip("Mode Description Text / 模式描述文本 - Text for mode description")]
        private TextMeshProUGUI m_modeDescriptionText;

        [SerializeField]
        [Tooltip("Mode Requirement Text / 模式要求文本 - Text for mode requirements")]
        private TextMeshProUGUI m_modeRequirementText;
        #endregion

        #region UI References - Team Panel
        [Header("队伍面板")]
        [SerializeField]
        [Tooltip("Team A Container / A队容器 - Container for team A players")]
        private Transform m_teamAContainer;

        [SerializeField]
        [Tooltip("Team B Container / B队容器 - Container for team B players")]
        private Transform m_teamBContainer;

        [SerializeField]
        [Tooltip("Spectators Container / 观众容器 - Container for spectators")]
        private Transform m_spectatorsContainer;

        [SerializeField]
        [Tooltip("Player Slot Prefab / 玩家槽位预制体 - Prefab for player slot")]
        private GameObject m_playerSlotPrefab;

        [SerializeField]
        [Tooltip("Team Balance Text / 队伍平衡文本 - Text for team balance information")]
        private TextMeshProUGUI m_teamBalanceText;
        #endregion

        #region UI References - Ready Panel
        [Header("准备面板")]
        [SerializeField]
        [Tooltip("Ready Button / 准备按钮 - Button for setting ready status")]
        private Button m_readyButton;

        [SerializeField]
        [Tooltip("Not Ready Button / 未准备按钮 - Button for setting not ready status")]
        private Button m_notReadyButton;

        [SerializeField]
        [Tooltip("Ready Status Text / 准备状态文本 - Text for ready status")]
        private TextMeshProUGUI m_readyStatusText;

        [SerializeField]
        [Tooltip("Ready Timer Text / 准备计时器文本 - Text for ready timer")]
        private TextMeshProUGUI m_readyTimerText;

        [SerializeField]
        [Tooltip("Start Game Button / 开始游戏按钮 - Button for starting the game")]
        private Button m_startGameButton;

        [SerializeField]
        [Tooltip("Ready Progress Bar / 准备进度条 - Progress bar for ready check")]
        private Slider m_readyProgressBar;
        #endregion

        #region UI References - Spectator Panel
        [Header("观众面板")]
        [SerializeField]
        [Tooltip("Spectator Panel / 观众面板 - Panel for spectator interface")]
        private GameObject m_spectatorPanel;

        [SerializeField]
        [Tooltip("Switch To Player Button / 切换到玩家按钮 - Button for switching to player")]
        private Button m_switchToPlayerButton;

        [SerializeField]
        [Tooltip("Spectate Team A Button / 观看A队按钮 - Button for spectating team A")]
        private Button m_spectateTeamAButton;

        [SerializeField]
        [Tooltip("Spectate Team B Button / 观看B队按钮 - Button for spectating team B")]
        private Button m_spectateTeamBButton;

        [SerializeField]
        [Tooltip("Spectator Info Text / 观众信息文本 - Text for spectator information")]
        private TextMeshProUGUI m_spectatorInfoText;
        #endregion

        #region Private Fields
        private readonly Dictionary<string, GameObject> m_playerSlots = new();
        private bool m_isLocalPlayerReady = false;
        private float m_readyCheckStartTime = 0f;
        private const float READY_CHECK_TIMEOUT = 30f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupUIReferences();
            InitializeButtons();
        }

        private void Start()
        {
            SubscribeToEvents();
            RefreshUI();
        }

        private void Update()
        {
            UpdateReadyTimer();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        private void SetupUIReferences()
        {
            // 确保所有UI引用都已正确设置
            if (m_lobbyPanel == null)
                Debug.LogError("[PongLobbyUI] 缺少大厅面板引用");

            if (m_playerSlotPrefab == null)
                Debug.LogError("[PongLobbyUI] 缺少玩家槽位预制体引用");
        }

        private void InitializeButtons()
        {
            // 初始化所有按钮事件
            m_joinAsPlayerButton?.onClick.AddListener(OnJoinAsPlayer);
            m_joinAsSpectatorButton?.onClick.AddListener(OnJoinAsSpectator);

            m_singlesButton?.onClick.AddListener(() => OnSelectGameMode(MatchmakingStrategy.ForceSingles));
            m_doublesButton?.onClick.AddListener(() => OnSelectGameMode(MatchmakingStrategy.ForceDoubles));
            m_autoModeButton?.onClick.AddListener(() => OnSelectGameMode(MatchmakingStrategy.Auto));

            m_readyButton?.onClick.AddListener(() => OnSetReady(true));
            m_notReadyButton?.onClick.AddListener(() => OnSetReady(false));
            m_startGameButton?.onClick.AddListener(OnStartGame);

            m_switchToPlayerButton?.onClick.AddListener(OnSwitchToPlayer);
            m_spectateTeamAButton?.onClick.AddListener(() => OnSpectateTeam(NetworkedTeam.Team.TeamA));
            m_spectateTeamBButton?.onClick.AddListener(() => OnSpectateTeam(NetworkedTeam.Team.TeamB));
        }

        private void SubscribeToEvents()
        {
            // 订阅乒乓球游戏事件
            PongGameEvents.OnLobbyStateChanged += OnLobbyStateChanged;
            PongGameEvents.OnGameModeChanged += OnGameModeChanged;
            PongGameEvents.OnPlayerCountChanged += OnPlayerCountChanged;
            PongGameEvents.OnPlayerJoined += OnPlayerJoined;
            PongGameEvents.OnPlayerLeft += OnPlayerLeft;
            PongGameEvents.OnPlayerTeamAssigned += OnPlayerTeamAssigned;
            PongGameEvents.OnPlayerReadyStateChanged += OnPlayerReadyStateChanged;
            PongGameEvents.OnRoomHostChanged += OnRoomHostChanged;
        }

        private void UnsubscribeFromEvents()
        {
            // 取消订阅事件
            PongGameEvents.OnLobbyStateChanged -= OnLobbyStateChanged;
            PongGameEvents.OnGameModeChanged -= OnGameModeChanged;
            PongGameEvents.OnPlayerCountChanged -= OnPlayerCountChanged;
            PongGameEvents.OnPlayerJoined -= OnPlayerJoined;
            PongGameEvents.OnPlayerLeft -= OnPlayerLeft;
            PongGameEvents.OnPlayerTeamAssigned -= OnPlayerTeamAssigned;
            PongGameEvents.OnPlayerReadyStateChanged -= OnPlayerReadyStateChanged;
            PongGameEvents.OnRoomHostChanged -= OnRoomHostChanged;
        }
        #endregion

        #region Event Handlers
        private void OnLobbyStateChanged(GameLobbyState newState)
        {
            switch (newState)
            {
                case GameLobbyState.WaitingForPlayers:
                    ShowWaitingPanel();
                    break;
                case GameLobbyState.ModeSelection:
                    ShowGameModePanel();
                    break;
                case GameLobbyState.TeamBalancing:
                    ShowTeamPanel();
                    break;
                case GameLobbyState.ReadyCheck:
                    ShowReadyPanel();
                    m_readyCheckStartTime = Time.time;
                    break;
                case GameLobbyState.GameStarting:
                case GameLobbyState.InGame:
                    ShowGamePanel();
                    break;
                case GameLobbyState.PostGame:
                    ShowWaitingPanel();
                    break;
            }
        }

        private void OnGameModeChanged(PongGameMode newMode)
        {
            UpdateGameModeUI(newMode);
            UpdateModeRequirements(newMode);
        }

        private void OnPlayerCountChanged(int newCount)
        {
            UpdatePlayerCountDisplay(newCount);
            UpdateButtonStates();
        }

        private void OnPlayerJoined(string playerId)
        {
            CreatePlayerSlot(playerId);
        }

        private void OnPlayerLeft(string playerId)
        {
            RemovePlayerSlot(playerId);
        }

        private void OnPlayerTeamAssigned(string playerId, NetworkedTeam.Team team)
        {
            UpdatePlayerSlotTeam(playerId, team);
        }

        private void OnPlayerReadyStateChanged(string playerId, bool isReady)
        {
            UpdatePlayerReadyStatus(playerId, isReady);
            UpdateReadyProgress();
        }

        private void OnRoomHostChanged(string newHostPlayerId)
        {
            UpdateHostUI(newHostPlayerId);
        }
        #endregion

        #region Button Handlers
        private void OnJoinAsPlayer()
        {
            // 请求作为玩家加入
            if (PongSessionManager.Instance != null)
            {
                var clientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
                var playerId = $"Player_{clientId}";
                PongSessionManager.Instance.SetupPlayerData(clientId, playerId, false, false);
            }
        }

        private void OnJoinAsSpectator()
        {
            // 请求作为观众加入
            if (PongSessionManager.Instance != null)
            {
                var clientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
                var playerId = $"Spectator_{clientId}";
                PongSessionManager.Instance.SetupPlayerData(clientId, playerId, true, false);
            }
        }

        private void OnSelectGameMode(MatchmakingStrategy strategy)
        {
            // 只有房主可以选择游戏模式
            if (PongSessionManager.Instance.IsRoomHost)
            {
                PongSessionManager.Instance.SetMatchmakingStrategyServerRpc(strategy);
            }
        }

        private void OnSetReady(bool isReady)
        {
            m_isLocalPlayerReady = isReady;
            PongSessionManager.Instance.SetPlayerReadyServerRpc(isReady);

            // 更新本地UI
            UpdateLocalReadyState(isReady);
        }

        private void OnStartGame()
        {
            // 只有房主可以开始游戏
            if (PongSessionManager.Instance.IsRoomHost && PongSessionManager.Instance.CanStartGame)
            {
                var serverHandler = FindObjectOfType<PongServerHandler>();
                serverHandler?.RequestStartGameServerRpc();
            }
        }

        private void OnSwitchToPlayer()
        {
            var spawningManager = FindObjectOfType<PongPlayerSpawningManager>();
            spawningManager?.SwitchToPlayerServerRpc();
        }

        private void OnSpectateTeam(NetworkedTeam.Team team)
        {
            var spawningManager = FindObjectOfType<PongPlayerSpawningManager>();
            spawningManager?.SwitchToSpectatorServerRpc(team);
        }
        #endregion

        #region UI Panel Management
        private void ShowWaitingPanel()
        {
            SetPanelActive(m_waitingPanel, true);
            SetPanelActive(m_gameModePanel, false);
            SetPanelActive(m_teamPanel, false);
            SetPanelActive(m_readyPanel, false);
            SetPanelActive(m_gamePanel, false);
        }

        private void ShowGameModePanel()
        {
            SetPanelActive(m_waitingPanel, false);
            SetPanelActive(m_gameModePanel, true);
            SetPanelActive(m_teamPanel, false);
            SetPanelActive(m_readyPanel, false);
            SetPanelActive(m_gamePanel, false);

            // 只有房主显示模式选择
            SetPanelActive(m_hostModeSelection, PongSessionManager.Instance.IsRoomHost);
        }

        private void ShowTeamPanel()
        {
            SetPanelActive(m_waitingPanel, false);
            SetPanelActive(m_gameModePanel, false);
            SetPanelActive(m_teamPanel, true);
            SetPanelActive(m_readyPanel, false);
            SetPanelActive(m_gamePanel, false);

            RefreshTeamDisplay();
        }

        private void ShowReadyPanel()
        {
            SetPanelActive(m_waitingPanel, false);
            SetPanelActive(m_gameModePanel, false);
            SetPanelActive(m_teamPanel, true);
            SetPanelActive(m_readyPanel, true);
            SetPanelActive(m_gamePanel, false);
        }

        private void ShowGamePanel()
        {
            SetPanelActive(m_waitingPanel, false);
            SetPanelActive(m_gameModePanel, false);
            SetPanelActive(m_teamPanel, false);
            SetPanelActive(m_readyPanel, false);
            SetPanelActive(m_gamePanel, true);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            panel?.SetActive(active);
        }
        #endregion

        #region UI Updates
        private void RefreshUI()
        {
            if (PongSessionManager.Instance == null) return;

            OnLobbyStateChanged(PongSessionManager.Instance.CurrentLobbyState);
            OnGameModeChanged(PongSessionManager.Instance.CurrentGameMode);
            UpdatePlayerCountDisplay(PongSessionManager.Instance.ActivePlayerCount);
        }

        private void UpdateGameModeUI(PongGameMode mode)
        {
            if (m_modeDescriptionText == null) return;

            m_modeDescriptionText.text = mode switch
            {
                PongGameMode.Singles => "单打模式 - 1对1经典对战",
                PongGameMode.Doubles => "双打模式 - 2对2团队合作",
                PongGameMode.Waiting => "等待更多玩家加入...",
                PongGameMode.Spectator => "观众模式",
                _ => "未知模式"
            };
        }

        private void UpdateModeRequirements(PongGameMode mode)
        {
            if (m_modeRequirementText == null) return;

            m_modeRequirementText.text = mode switch
            {
                PongGameMode.Singles => "需要2名玩家",
                PongGameMode.Doubles => "需要4名玩家",
                _ => ""
            };
        }

        private void UpdatePlayerCountDisplay(int count)
        {
            if (m_playerCountText != null)
            {
                m_playerCountText.text = $"当前玩家: {count}";
            }

            if (m_waitingStatusText != null)
            {
                m_waitingStatusText.text = count switch
                {
                    0 => "等待玩家加入...",
                    1 => "等待更多玩家加入...",
                    >= 2 => "准备开始游戏！",
                    _ => "等待中..."
                };
            }
        }

        private void UpdateButtonStates()
        {
            var playerCount = PongSessionManager.Instance?.ActivePlayerCount ?? 0;

            // 更新模式选择按钮状态
            if (m_singlesButton != null)
                m_singlesButton.interactable = playerCount >= 2;

            if (m_doublesButton != null)
                m_doublesButton.interactable = playerCount >= 4;

            // 更新开始游戏按钮状态
            if (m_startGameButton != null)
            {
                m_startGameButton.interactable = PongSessionManager.Instance?.CanStartGame ?? false;
            }
        }

        private void UpdateHostUI(string hostPlayerId)
        {
            // 更新房主相关UI显示
            var isLocalHost = PongSessionManager.Instance?.IsRoomHost ?? false;

            SetPanelActive(m_hostModeSelection, isLocalHost);

            if (m_hostControlsToggle != null)
            {
                m_hostControlsToggle.isOn = isLocalHost;
            }
        }

        private void UpdateLocalReadyState(bool isReady)
        {
            if (m_readyButton != null)
                m_readyButton.gameObject.SetActive(!isReady);

            if (m_notReadyButton != null)
                m_notReadyButton.gameObject.SetActive(isReady);

            if (m_readyStatusText != null)
            {
                m_readyStatusText.text = isReady ? "已准备" : "未准备";
                m_readyStatusText.color = isReady ? Color.green : Color.red;
            }
        }

        private void UpdateReadyProgress()
        {
            if (m_readyProgressBar == null) return;

            var activePlayers = PongSessionManager.Instance?.ActivePlayerCount ?? 0;
            if (activePlayers == 0)
            {
                m_readyProgressBar.value = 0f;
                return;
            }

            // 计算准备进度（这里需要实际实现获取准备玩家数量的逻辑）
            var readyPlayers = 0; // TODO: 从SessionManager获取准备玩家数量
            m_readyProgressBar.value = (float)readyPlayers / activePlayers;
        }

        private void UpdateReadyTimer()
        {
            if (PongSessionManager.Instance?.CurrentLobbyState != GameLobbyState.ReadyCheck)
                return;

            if (m_readyTimerText == null) return;

            var timeLeft = READY_CHECK_TIMEOUT - (Time.time - m_readyCheckStartTime);
            if (timeLeft > 0)
            {
                m_readyTimerText.text = $"准备时间: {timeLeft:F0}秒";
            }
            else
            {
                m_readyTimerText.text = "准备时间已到";
            }
        }
        #endregion

        #region Player Slot Management
        private void CreatePlayerSlot(string playerId)
        {
            if (m_playerSlots.ContainsKey(playerId) || m_playerSlotPrefab == null)
                return;

            var slotObject = Instantiate(m_playerSlotPrefab);
            var slotText = slotObject.GetComponentInChildren<TextMeshProUGUI>();
            if (slotText != null)
            {
                slotText.text = playerId;
            }

            m_playerSlots[playerId] = slotObject;

            // 初始时放在等待区域
            if (m_spectatorsContainer != null)
            {
                slotObject.transform.SetParent(m_spectatorsContainer);
            }
        }

        private void RemovePlayerSlot(string playerId)
        {
            if (m_playerSlots.TryGetValue(playerId, out var slotObject))
            {
                Destroy(slotObject);
                m_playerSlots.Remove(playerId);
            }
        }

        private void UpdatePlayerSlotTeam(string playerId, NetworkedTeam.Team team)
        {
            if (!m_playerSlots.TryGetValue(playerId, out var slotObject))
                return;

            Transform targetContainer = team switch
            {
                NetworkedTeam.Team.TeamA => m_teamAContainer,
                NetworkedTeam.Team.TeamB => m_teamBContainer,
                _ => m_spectatorsContainer
            };

            if (targetContainer != null)
            {
                slotObject.transform.SetParent(targetContainer);
            }
        }

        private void UpdatePlayerReadyStatus(string playerId, bool isReady)
        {
            if (!m_playerSlots.TryGetValue(playerId, out var slotObject))
                return;

            var slotText = slotObject.GetComponentInChildren<TextMeshProUGUI>();
            if (slotText != null)
            {
                slotText.color = isReady ? Color.green : Color.white;
                slotText.text = $"{playerId} {(isReady ? "✅" : "⏳")}";
            }
        }

        private void RefreshTeamDisplay()
        {
            // 清空所有容器
            ClearContainer(m_teamAContainer);
            ClearContainer(m_teamBContainer);
            ClearContainer(m_spectatorsContainer);

            // 重新分配所有玩家槽位
            foreach (var kvp in m_playerSlots)
            {
                var playerId = kvp.Key;
                var playerData = PongSessionManager.Instance?.GetPlayerData(playerId);
                if (playerData.HasValue)
                {
                    UpdatePlayerSlotTeam(playerId, playerData.Value.SelectedTeam);
                }
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                child.SetParent(null);
            }
        }
        #endregion
    }
}