using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;
using PongHub.UI.Localization;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 多人模式详情面板
    /// 显示多人模式的详细信息和房间设置选项
    /// </summary>
    public class MultiplayerModePanel : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private Image m_backgroundImage;
        [SerializeField] private Image m_modeIcon;
        [SerializeField] private TextMeshProUGUI m_modeTitleText;
        [SerializeField] private TextMeshProUGUI m_modeDescriptionText;

        [Header("房间设置")]
        [SerializeField] private InputField m_roomNameInput;
        [SerializeField] private Dropdown m_maxPlayersDropdown;
        [SerializeField] private Toggle m_privateRoomToggle;
        [SerializeField] private InputField m_passwordInput;
        [SerializeField] private Toggle m_allowSpectatorsToggle;

        [Header("游戏设置")]
        [SerializeField] private Dropdown m_gameRulesDropdown;
        [SerializeField] private Slider m_gameDurationSlider;
        [SerializeField] private TextMeshProUGUI m_gameDurationText;
        [SerializeField] private Dropdown m_ballSpeedDropdown;
        [SerializeField] private Toggle m_enablePowerupsToggle;

        [Header("房间列表")]
        [SerializeField] private ScrollRect m_roomListScrollRect;
        [SerializeField] private Transform m_roomListContainer;
        [SerializeField] private Button m_refreshRoomsButton;
        [SerializeField] private TextMeshProUGUI m_roomCountText;

        [Header("快速匹配")]
        [SerializeField] private Button m_quickMatchButton;
        [SerializeField] private TextMeshProUGUI m_estimatedWaitText;
        [SerializeField] private Slider m_skillRangeSlider;
        [SerializeField] private TextMeshProUGUI m_skillRangeText;

        [Header("统计信息")]
        [SerializeField] private TextMeshProUGUI m_onlinePlayersText;
        [SerializeField] private TextMeshProUGUI m_activeRoomsText;
        [SerializeField] private TextMeshProUGUI m_averageWaitTimeText;
        [SerializeField] private TextMeshProUGUI m_playerRankText;

        [Header("按钮")]
        [SerializeField] private Button m_createRoomButton;
        [SerializeField] private Button m_joinRoomButton;
        [SerializeField] private Button m_backButton;

        [Header("预制件")]
        [SerializeField] private GameObject m_roomItemPrefab;

        [Header("动画")]
        [SerializeField] private float m_showAnimDuration = 0.3f;
        [SerializeField] private AnimationCurve m_animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("事件")]
        public UnityEvent<GameModeInfo, MultiplayerSettings> OnCreateRoom;
        public UnityEvent<string> OnJoinRoom;
        public UnityEvent<GameModeInfo> OnQuickMatch;
        public UnityEvent OnBackClicked;

        // 私有变量
        private GameModeInfo m_currentMode;
        private LocalizationManager m_localizationManager;
        private Coroutine m_animationCoroutine;
        private List<RoomItemUI> m_roomItems = new List<RoomItemUI>();
        private string m_selectedRoomId;

        #region 属性

        public GameModeInfo CurrentMode => m_currentMode;
        public bool IsShowing => gameObject.activeInHierarchy && m_canvasGroup.alpha > 0;
        public string SelectedRoomId => m_selectedRoomId;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            m_localizationManager = FindObjectOfType<LocalizationManager>();

            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            BindEvents();
            InitializeDropdowns();
            RefreshRoomList();
        }

        private void OnDestroy()
        {
            UnbindEvents();

            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示多人模式详情
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        public void ShowModeDetails(GameModeInfo modeInfo)
        {
            m_currentMode = modeInfo;
            RefreshContent();
            Show();
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show()
        {
            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);

            gameObject.SetActive(true);
            m_animationCoroutine = StartCoroutine(ShowAnimation());
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);

            m_animationCoroutine = StartCoroutine(HideAnimation());
        }

        /// <summary>
        /// 刷新内容
        /// </summary>
        public void RefreshContent()
        {
            if (m_currentMode == null)
                return;

            UpdateModeInfo();
            UpdateStatistics();
            UpdateSettings();
            UpdateLocalizedTexts();
            RefreshRoomList();
        }

        /// <summary>
        /// 刷新房间列表
        /// </summary>
        public void RefreshRoomList()
        {
            StartCoroutine(RefreshRoomListCoroutine());
        }

        /// <summary>
        /// 选择房间
        /// </summary>
        /// <param name="roomId">房间ID</param>
        public void SelectRoom(string roomId)
        {
            // 取消之前选择的房间
            foreach (var item in m_roomItems)
            {
                item.SetSelected(item.RoomId == roomId);
            }

            m_selectedRoomId = roomId;
            UpdateJoinButton();
        }

        #endregion

        #region 私有方法

        private void BindEvents()
        {
            if (m_createRoomButton != null)
                m_createRoomButton.onClick.AddListener(HandleCreateRoomClicked);

            if (m_joinRoomButton != null)
                m_joinRoomButton.onClick.AddListener(HandleJoinRoomClicked);

            if (m_backButton != null)
                m_backButton.onClick.AddListener(HandleBackButtonClicked);

            if (m_quickMatchButton != null)
                m_quickMatchButton.onClick.AddListener(HandleQuickMatchClicked);

            if (m_refreshRoomsButton != null)
                m_refreshRoomsButton.onClick.AddListener(HandleRefreshRoomsClicked);

            if (m_gameDurationSlider != null)
                m_gameDurationSlider.onValueChanged.AddListener(HandleGameDurationChanged);

            if (m_skillRangeSlider != null)
                m_skillRangeSlider.onValueChanged.AddListener(HandleSkillRangeChanged);

            if (m_privateRoomToggle != null)
                m_privateRoomToggle.onValueChanged.AddListener(HandlePrivateRoomToggled);
        }

        private void UnbindEvents()
        {
            if (m_createRoomButton != null)
                m_createRoomButton.onClick.RemoveListener(HandleCreateRoomClicked);

            if (m_joinRoomButton != null)
                m_joinRoomButton.onClick.RemoveListener(HandleJoinRoomClicked);

            if (m_backButton != null)
                m_backButton.onClick.RemoveListener(HandleBackButtonClicked);

            if (m_quickMatchButton != null)
                m_quickMatchButton.onClick.RemoveListener(HandleQuickMatchClicked);

            if (m_refreshRoomsButton != null)
                m_refreshRoomsButton.onClick.RemoveListener(HandleRefreshRoomsClicked);

            if (m_gameDurationSlider != null)
                m_gameDurationSlider.onValueChanged.RemoveListener(HandleGameDurationChanged);

            if (m_skillRangeSlider != null)
                m_skillRangeSlider.onValueChanged.RemoveListener(HandleSkillRangeChanged);

            if (m_privateRoomToggle != null)
                m_privateRoomToggle.onValueChanged.RemoveListener(HandlePrivateRoomToggled);
        }

        private void InitializeDropdowns()
        {
            // 初始化最大玩家数下拉菜单
            if (m_maxPlayersDropdown != null)
            {
                m_maxPlayersDropdown.ClearOptions();
                var options = new List<string> { "2", "4", "6", "8" };
                m_maxPlayersDropdown.AddOptions(options);
                m_maxPlayersDropdown.value = 0; // 2 players
            }

            // 初始化游戏规则下拉菜单
            if (m_gameRulesDropdown != null)
            {
                m_gameRulesDropdown.ClearOptions();
                var options = new List<string>
                {
                    GetLocalizedText("rules_standard"),
                    GetLocalizedText("rules_tournament"),
                    GetLocalizedText("rules_casual"),
                    GetLocalizedText("rules_custom")
                };
                m_gameRulesDropdown.AddOptions(options);
                m_gameRulesDropdown.value = 0; // Standard
            }

            // 初始化球速下拉菜单
            if (m_ballSpeedDropdown != null)
            {
                m_ballSpeedDropdown.ClearOptions();
                var options = new List<string>
                {
                    GetLocalizedText("speed_slow"),
                    GetLocalizedText("speed_normal"),
                    GetLocalizedText("speed_fast"),
                    GetLocalizedText("speed_extreme")
                };
                m_ballSpeedDropdown.AddOptions(options);
                m_ballSpeedDropdown.value = 1; // Normal
            }
        }

        private void UpdateModeInfo()
        {
            if (m_modeTitleText != null)
                m_modeTitleText.text = GetLocalizedText(m_currentMode.TitleKey);

            if (m_modeDescriptionText != null)
                m_modeDescriptionText.text = GetLocalizedText(m_currentMode.DescriptionKey);

            if (m_modeIcon != null && m_currentMode.Icon != null)
                m_modeIcon.sprite = m_currentMode.Icon;
        }

        private void UpdateStatistics()
        {
            // TODO: 从网络系统获取实际数据
            if (m_onlinePlayersText != null)
                m_onlinePlayersText.text = "248";

            if (m_activeRoomsText != null)
                m_activeRoomsText.text = "37";

            if (m_averageWaitTimeText != null)
                m_averageWaitTimeText.text = "15s";

            if (m_playerRankText != null)
                m_playerRankText.text = "#1,234";
        }

        private void UpdateSettings()
        {
            // 设置默认值
            if (m_gameDurationSlider != null)
            {
                m_gameDurationSlider.value = 600f; // 10分钟
                UpdateGameDurationText(600f);
            }

            if (m_skillRangeSlider != null)
            {
                m_skillRangeSlider.value = 0.3f; // 30% range
                UpdateSkillRangeText(0.3f);
            }

            if (m_roomNameInput != null)
            {
                m_roomNameInput.text = $"{GetLocalizedText("default_room_name")} {UnityEngine.Random.Range(1000, 9999)}";
            }
        }

        private void UpdateLocalizedTexts()
        {
            UpdateGameDurationText(m_gameDurationSlider != null ? m_gameDurationSlider.value : 600f);
            UpdateSkillRangeText(m_skillRangeSlider != null ? m_skillRangeSlider.value : 0.3f);
            UpdateEstimatedWaitTime();
        }

        private void UpdateGameDurationText(float seconds)
        {
            if (m_gameDurationText != null)
            {
                int minutes = Mathf.RoundToInt(seconds / 60f);
                m_gameDurationText.text = $"{minutes} {GetLocalizedText("minutes")}";
            }
        }

        private void UpdateSkillRangeText(float range)
        {
            if (m_skillRangeText != null)
            {
                int percentage = Mathf.RoundToInt(range * 100);
                m_skillRangeText.text = $"±{percentage}%";
            }
        }

        private void UpdateEstimatedWaitTime()
        {
            if (m_estimatedWaitText != null)
            {
                // 根据技能范围和在线玩家数估算等待时间
                float skillRange = m_skillRangeSlider != null ? m_skillRangeSlider.value : 0.3f;
                int estimatedSeconds = Mathf.RoundToInt(30f / (skillRange + 0.1f));
                m_estimatedWaitText.text = $"~{estimatedSeconds}s";
            }
        }

        private void UpdateJoinButton()
        {
            if (m_joinRoomButton != null)
            {
                m_joinRoomButton.interactable = !string.IsNullOrEmpty(m_selectedRoomId);
            }
        }

        private void UpdateRoomCount(int count)
        {
            if (m_roomCountText != null)
            {
                m_roomCountText.text = $"{count} {GetLocalizedText("rooms_available")}";
            }
        }

        private MultiplayerSettings GetCurrentSettings()
        {
            var settings = new MultiplayerSettings();

            if (m_roomNameInput != null)
                settings.RoomName = m_roomNameInput.text;

            if (m_maxPlayersDropdown != null)
                settings.MaxPlayers = int.Parse(m_maxPlayersDropdown.options[m_maxPlayersDropdown.value].text);

            if (m_privateRoomToggle != null)
                settings.IsPrivate = m_privateRoomToggle.isOn;

            if (m_passwordInput != null)
                settings.Password = m_passwordInput.text;

            if (m_allowSpectatorsToggle != null)
                settings.AllowSpectators = m_allowSpectatorsToggle.isOn;

            if (m_gameRulesDropdown != null)
                settings.GameRules = (GameRules)m_gameRulesDropdown.value;

            if (m_gameDurationSlider != null)
                settings.GameDuration = m_gameDurationSlider.value;

            if (m_ballSpeedDropdown != null)
                settings.BallSpeed = (BallSpeed)m_ballSpeedDropdown.value;

            if (m_enablePowerupsToggle != null)
                settings.EnablePowerups = m_enablePowerupsToggle.isOn;

            return settings;
        }

        private string GetLocalizedText(string key)
        {
            if (m_localizationManager != null)
                return m_localizationManager.GetLocalizedText(key);
            return key;
        }

        private IEnumerator RefreshRoomListCoroutine()
        {
            // 清除现有房间列表
            ClearRoomList();

            // TODO: 从网络服务获取房间列表
            yield return new WaitForSeconds(0.5f); // 模拟网络延迟

            // 创建示例房间数据
            var sampleRooms = CreateSampleRoomData();

            foreach (var roomData in sampleRooms)
            {
                CreateRoomItem(roomData);
            }

            UpdateRoomCount(sampleRooms.Count);
        }

        private void ClearRoomList()
        {
            foreach (var item in m_roomItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            m_roomItems.Clear();
            m_selectedRoomId = null;
            UpdateJoinButton();
        }

        private void CreateRoomItem(RoomData roomData)
        {
            if (m_roomItemPrefab == null || m_roomListContainer == null)
                return;

            GameObject roomItemGO = Instantiate(m_roomItemPrefab, m_roomListContainer);
            RoomItemUI roomItem = roomItemGO.GetComponent<RoomItemUI>();

            if (roomItem != null)
            {
                roomItem.Initialize(roomData);
                roomItem.OnRoomSelected += SelectRoom;
                m_roomItems.Add(roomItem);
            }
        }

        private List<RoomData> CreateSampleRoomData()
        {
            // 创建示例房间数据
            var rooms = new List<RoomData>();

            for (int i = 0; i < 10; i++)
            {
                rooms.Add(new RoomData
                {
                    Id = $"room_{i}",
                    Name = $"Room {i + 1}",
                    PlayerCount = UnityEngine.Random.Range(1, 5),
                    MaxPlayers = 4,
                    IsPrivate = UnityEngine.Random.value > 0.7f,
                    Ping = UnityEngine.Random.Range(20, 100),
                    GameMode = m_currentMode?.ModeId ?? "standard",
                    HostName = $"Player{UnityEngine.Random.Range(1000, 9999)}"
                });
            }

            return rooms;
        }

        private IEnumerator ShowAnimation()
        {
            float elapsedTime = 0f;

            while (elapsedTime < m_showAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = m_animCurve.Evaluate(elapsedTime / m_showAnimDuration);

                if (m_canvasGroup != null)
                    m_canvasGroup.alpha = progress;

                yield return null;
            }

            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 1f;
        }

        private IEnumerator HideAnimation()
        {
            float elapsedTime = 0f;

            while (elapsedTime < m_showAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = 1f - m_animCurve.Evaluate(elapsedTime / m_showAnimDuration);

                if (m_canvasGroup != null)
                    m_canvasGroup.alpha = progress;

                yield return null;
            }

            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        #endregion

        #region 事件处理

        private void HandleCreateRoomClicked()
        {
            if (m_currentMode == null)
                return;

            var settings = GetCurrentSettings();
            OnCreateRoom?.Invoke(m_currentMode, settings);
        }

        private void HandleJoinRoomClicked()
        {
            if (string.IsNullOrEmpty(m_selectedRoomId))
                return;

            OnJoinRoom?.Invoke(m_selectedRoomId);
        }

        private void HandleBackButtonClicked()
        {
            OnBackClicked?.Invoke();
        }

        private void HandleQuickMatchClicked()
        {
            if (m_currentMode == null)
                return;

            OnQuickMatch?.Invoke(m_currentMode);
        }

        private void HandleRefreshRoomsClicked()
        {
            RefreshRoomList();
        }

        private void HandleGameDurationChanged(float value)
        {
            UpdateGameDurationText(value);
        }

        private void HandleSkillRangeChanged(float value)
        {
            UpdateSkillRangeText(value);
            UpdateEstimatedWaitTime();
        }

        private void HandlePrivateRoomToggled(bool isOn)
        {
            if (m_passwordInput != null)
            {
                m_passwordInput.gameObject.SetActive(isOn);
                if (!isOn)
                    m_passwordInput.text = "";
            }
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 多人游戏设置
    /// </summary>
    [Serializable]
    public class MultiplayerSettings
    {
        public string RoomName;
        public int MaxPlayers = 4;
        public bool IsPrivate = false;
        public string Password = "";
        public bool AllowSpectators = true;
        public GameRules GameRules = GameRules.Standard;
        public float GameDuration = 600f; // seconds
        public BallSpeed BallSpeed = BallSpeed.Normal;
        public bool EnablePowerups = true;
    }

    /// <summary>
    /// 房间数据
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public string Id;
        public string Name;
        public int PlayerCount;
        public int MaxPlayers;
        public bool IsPrivate;
        public int Ping;
        public string GameMode;
        public string HostName;
    }

    /// <summary>
    /// 游戏规则枚举
    /// </summary>
    public enum GameRules
    {
        Standard = 0,
        Tournament = 1,
        Casual = 2,
        Custom = 3
    }

    /// <summary>
    /// 球速枚举
    /// </summary>
    public enum BallSpeed
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
        Extreme = 3
    }

    /// <summary>
    /// 房间列表项UI组件
    /// </summary>
    public class RoomItemUI : MonoBehaviour
    {
        public string RoomId { get; private set; }
        public event System.Action<string> OnRoomSelected;

        public void Initialize(RoomData roomData)
        {
            RoomId = roomData.Id;
            // TODO: 设置UI显示
        }

        public void SetSelected(bool selected)
        {
            // TODO: 更新选中状态视觉效果
        }

        public void OnClick()
        {
            OnRoomSelected?.Invoke(RoomId);
        }
    }

    #endregion
}