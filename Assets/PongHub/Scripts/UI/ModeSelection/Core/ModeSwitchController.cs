using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PongHub.UI.ModeSelection;
using PongHub.UI.Localization;
using PongHub.Core;
using PongHub.Core.Components;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式切换状态
    /// </summary>
    public enum ModeSwitchState
    {
        Inactive,           // 非活跃状态
        ShowingModeList,    // 显示模式列表
        ShowingSinglePlayer,// 显示单机模式详情
        ShowingMultiplayer, // 显示多人模式详情
        Transitioning,      // 切换中
        Starting           // 启动游戏中
    }



    /// <summary>
    /// 模式切换控制器
    /// 管理整个模式选择和切换流程
    /// </summary>
    public class ModeSwitchController : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private ModeSelectionConfig m_config;
        [SerializeField] private bool m_autoInitialize = true;

        [Header("面板引用")]
        [SerializeField] private ModeSelectionPanel m_modeSelectionPanel;
        [SerializeField] private SinglePlayerModePanel m_singlePlayerPanel;
        [SerializeField] private MultiplayerModePanel m_multiplayerPanel;
        [SerializeField] private GameObject m_loadingPanel;

        [Header("动画和效果")]
        [SerializeField] private ModeTransitionEffect m_transitionEffect;
        [SerializeField] private CanvasGroup m_mainCanvasGroup;

        [Header("音频")]
        [SerializeField] private AudioSource m_audioSource;

        [Header("事件")]
        public UnityEvent<GameModeInfo> OnModeSelected;
        public UnityEvent<ModeSwitchState> OnStateChanged;
        public UnityEvent OnModeSwitchStarted;
        public UnityEvent OnModeSwitchCompleted;

        // 私有变量
        private ModeSwitchState m_currentState = ModeSwitchState.Inactive;
        private GameModeInfo m_selectedMode = null;
        private GameModeInfo m_lastPlayedMode = null;
        private Dictionary<string, ModeStatsData> m_modeStats = new Dictionary<string, ModeStatsData>();

        // 系统引用
        private GameModeManager m_gameModeManager;
        private LocalizationManager m_localizationManager;
        private TableMenuSystem m_tableMenuSystem;

        // 状态管理
        private bool m_isInitialized = false;

        #region 属性

        public ModeSwitchState CurrentState => m_currentState;
        public GameModeInfo SelectedMode => m_selectedMode;
        public GameModeInfo LastPlayedMode => m_lastPlayedMode;
        public ModeSelectionConfig Config => m_config;
        public bool IsInitialized => m_isInitialized;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 查找系统引用
            FindSystemReferences();

            // 设置音频源
            if (m_audioSource == null)
                m_audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (m_autoInitialize)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            // 保存统计数据
            SaveModeStats();

            // 清理事件
            if (m_modeSelectionPanel != null)
            {
                m_modeSelectionPanel.OnModeCardClicked.RemoveListener(HandleModeCardClicked);
                m_modeSelectionPanel.OnQuickStartClicked.RemoveListener(HandleQuickStartClicked);
                m_modeSelectionPanel.OnRandomModeClicked.RemoveListener(HandleRandomModeClicked);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public void Initialize()
        {
            if (m_isInitialized)
                return;

            // 验证配置
            if (m_config == null)
            {
                Debug.LogError("ModeSwitchController: ModeSelectionConfig is not assigned!");
                return;
            }

            // 加载模式统计数据
            LoadModeStats();

            // 初始化面板
            InitializePanels();

            // 设置初始状态
            SetState(ModeSwitchState.Inactive);

            m_isInitialized = true;

            Debug.Log("ModeSwitchController initialized successfully");
        }

        /// <summary>
        /// 显示模式选择界面
        /// </summary>
        public void ShowModeSelection()
        {
            if (!m_isInitialized)
            {
                Debug.LogWarning("ModeSwitchController not initialized!");
                return;
            }

            StartCoroutine(ShowModeSelectionCoroutine());
        }

        /// <summary>
        /// 隐藏模式选择界面
        /// </summary>
        public void HideModeSelection()
        {
            StartCoroutine(HideModeSelectionCoroutine());
        }

        /// <summary>
        /// 选择指定模式
        /// </summary>
        /// <param name="modeId">模式ID</param>
        public void SelectMode(string modeId)
        {
            GameModeInfo modeInfo = m_config.GetModeInfo(modeId);
            if (modeInfo == null)
            {
                Debug.LogWarning($"Mode with ID '{modeId}' not found!");
                return;
            }

            SelectMode(modeInfo);
        }

        /// <summary>
        /// 选择指定模式
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        public void SelectMode(GameModeInfo modeInfo)
        {
            if (modeInfo == null || !modeInfo.CheckAvailability())
            {
                PlaySound(m_config.ModeUnavailableSound);
                return;
            }

            m_selectedMode = modeInfo;
            PlaySound(m_config.ModeSelectSound);

            // 触发事件
            OnModeSelected?.Invoke(modeInfo);

            // 根据模式类型显示相应界面
            ShowModeDetails(modeInfo);
        }

        /// <summary>
        /// 快速开始上次游戏模式
        /// </summary>
        public void QuickStart()
        {
            GameModeInfo modeToStart = m_lastPlayedMode ?? m_config.GetDefaultMode();
            if (modeToStart != null && modeToStart.CheckAvailability())
            {
                StartGameMode(modeToStart);
            }
            else
            {
                ShowModeSelection();
            }
        }

        /// <summary>
        /// 随机选择模式
        /// </summary>
        public void RandomMode()
        {
            GameModeInfo randomMode = m_config.GetRandomMode();
            if (randomMode != null)
            {
                SelectMode(randomMode);
            }
        }

        /// <summary>
        /// 启动游戏模式
        /// </summary>
        /// <param name="modeInfo">要启动的模式</param>
        /// <param name="difficulty">难度（可选）</param>
        public void StartGameMode(GameModeInfo modeInfo, DifficultyLevel difficulty = DifficultyLevel.Normal)
        {
            if (modeInfo == null || !modeInfo.CheckAvailability())
            {
                Debug.LogWarning("Cannot start unavailable mode!");
                return;
            }

            StartCoroutine(StartGameModeCoroutine(modeInfo, difficulty));
        }

        /// <summary>
        /// 返回到模式选择
        /// </summary>
        public void BackToModeSelection()
        {
            if (m_currentState == ModeSwitchState.ShowingModeList)
                return;

            SetState(ModeSwitchState.ShowingModeList);
            ShowModeSelectionPanel();
        }

        /// <summary>
        /// 获取模式统计数据
        /// </summary>
        /// <param name="modeId">模式ID</param>
        /// <returns>统计数据</returns>
        public ModeStatsData GetModeStats(string modeId)
        {
            m_modeStats.TryGetValue(modeId, out ModeStatsData stats);
            return stats;
        }

        /// <summary>
        /// 更新模式统计数据
        /// </summary>
        /// <param name="modeId">模式ID</param>
        /// <param name="score">得分</param>
        /// <param name="playTime">游戏时间</param>
        public void UpdateModeStats(string modeId, float score, float playTime)
        {
            GameModeInfo modeInfo = m_config.GetModeInfo(modeId);
            if (modeInfo != null)
            {
                modeInfo.UpdateStats(score, playTime);
                m_modeStats[modeId] = modeInfo.SaveStats();
                m_lastPlayedMode = modeInfo;

                // 保存到持久化存储
                SaveModeStats();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 查找系统引用
        /// </summary>
        private void FindSystemReferences()
        {
            if (m_gameModeManager == null)
                m_gameModeManager = FindObjectOfType<GameModeManager>();

            if (m_localizationManager == null)
                m_localizationManager = FindObjectOfType<LocalizationManager>();

            if (m_tableMenuSystem == null)
                m_tableMenuSystem = FindObjectOfType<TableMenuSystem>();
        }

        /// <summary>
        /// 初始化面板
        /// </summary>
        private void InitializePanels()
        {
            // 绑定面板事件
            if (m_modeSelectionPanel != null)
            {
                m_modeSelectionPanel.Initialize(this, m_config);
                m_modeSelectionPanel.OnModeCardClicked.AddListener(HandleModeCardClicked);
                m_modeSelectionPanel.OnQuickStartClicked.AddListener(HandleQuickStartClicked);
                m_modeSelectionPanel.OnRandomModeClicked.AddListener(HandleRandomModeClicked);
            }

            // 初始化单机模式面板
            if (m_singlePlayerPanel != null)
            {
                m_singlePlayerPanel.OnModeSelected += (mode) => HandleSinglePlayerModeSelected(mode);
                m_singlePlayerPanel.OnBackPressed += HandleBackToModeSelection;
            }

            // 初始化多人模式面板
            if (m_multiplayerPanel != null)
            {
                m_multiplayerPanel.OnModeSelected += (mode) => HandleMultiplayerModeSelected(mode);
                m_multiplayerPanel.OnBackPressed += HandleBackToModeSelection;
            }

            // 隐藏所有面板
            HideAllPanels();
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="newState">新状态</param>
        private void SetState(ModeSwitchState newState)
        {
            if (m_currentState == newState)
                return;

            ModeSwitchState oldState = m_currentState;
            m_currentState = newState;

            Debug.Log($"ModeSwitchController state changed: {oldState} -> {newState}");

            // 触发状态变化事件
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// 显示模式选择协程
        /// </summary>
        private IEnumerator ShowModeSelectionCoroutine()
        {
            SetState(ModeSwitchState.Transitioning);
            OnModeSwitchStarted?.Invoke();

            // 播放过渡效果
            if (m_transitionEffect != null)
            {
                // 使用现有的过渡效果方法
                m_transitionEffect.TriggerTransition(null, m_modeSelectionPanel.gameObject);
                yield return new WaitForSeconds(0.3f); // 等待过渡效果
            }

            // 显示模式选择面板
            ShowModeSelectionPanel();
            SetState(ModeSwitchState.ShowingModeList);

            OnModeSwitchCompleted?.Invoke();
        }

        /// <summary>
        /// 隐藏模式选择协程
        /// </summary>
        private IEnumerator HideModeSelectionCoroutine()
        {
            SetState(ModeSwitchState.Transitioning);

            // 播放过渡效果
            if (m_transitionEffect != null)
            {
                // 使用现有的过渡效果方法
                m_transitionEffect.TriggerTransition(m_modeSelectionPanel.gameObject, null);
                yield return new WaitForSeconds(0.3f); // 等待过渡效果
            }

            // 隐藏所有面板
            HideAllPanels();
            SetState(ModeSwitchState.Inactive);
        }

        /// <summary>
        /// 启动游戏模式协程
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        /// <param name="difficulty">难度</param>
        private IEnumerator StartGameModeCoroutine(GameModeInfo modeInfo, DifficultyLevel difficulty)
        {
            SetState(ModeSwitchState.Starting);

            // 显示加载界面
            if (m_loadingPanel != null)
                m_loadingPanel.SetActive(true);

            // 播放确认音效
            PlaySound(m_config.ModeConfirmSound);

            // 等待一帧确保UI更新
            yield return null;

            // 通过GameModeManager启动模式
            if (m_gameModeManager != null)
            {
                if (modeInfo.ModeType == GameModeType.Practice || modeInfo.ModeType == GameModeType.AIBattle)
                {
                    m_gameModeManager.SwitchToMode(GameMode.Local);
                }
                else if (modeInfo.ModeType == GameModeType.LocalMultiplayer)
                {
                    m_gameModeManager.SwitchToMode(GameMode.Local);
                }
                else if (modeInfo.ModeType == GameModeType.OnlineMultiplayer)
                {
                    m_gameModeManager.SwitchToMode(GameMode.Network);
                }

                // 更新最后游戏模式
                m_lastPlayedMode = modeInfo;

                // 隐藏模式选择界面
                yield return StartCoroutine(HideModeSelectionCoroutine());
            }
            else
            {
                Debug.LogError("GameModeManager not found!");
                SetState(ModeSwitchState.ShowingModeList);
            }

            // 隐藏加载界面
            if (m_loadingPanel != null)
                m_loadingPanel.SetActive(false);
        }

        /// <summary>
        /// 显示模式详情
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        private void ShowModeDetails(GameModeInfo modeInfo)
        {
            HideAllPanels();

            switch (modeInfo.ModeType)
            {
                case GameModeType.Practice:
                case GameModeType.AIBattle:
                case GameModeType.Training:
                    if (m_singlePlayerPanel != null)
                    {
                        var singlePlayerMode = ConvertToSinglePlayerMode(modeInfo);
                        m_singlePlayerPanel.ShowModeDetails(singlePlayerMode);
                        SetState(ModeSwitchState.ShowingSinglePlayer);
                    }
                    break;

                case GameModeType.OnlineMultiplayer:
                case GameModeType.LocalMultiplayer:
                    if (m_multiplayerPanel != null)
                    {
                        var multiplayerMode = ConvertToMultiplayerMode(modeInfo);
                        m_multiplayerPanel.ShowModeDetails(multiplayerMode);
                        SetState(ModeSwitchState.ShowingMultiplayer);
                    }
                    break;

                default:
                    // 直接启动其他类型的模式
                    StartGameMode(modeInfo);
                    break;
            }
        }

        /// <summary>
        /// 显示模式选择面板
        /// </summary>
        private void ShowModeSelectionPanel()
        {
            HideAllPanels();

            if (m_modeSelectionPanel != null)
            {
                m_modeSelectionPanel.gameObject.SetActive(true);
                m_modeSelectionPanel.RefreshModeList();
            }
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        private void HideAllPanels()
        {
            if (m_modeSelectionPanel != null)
                m_modeSelectionPanel.gameObject.SetActive(false);

            if (m_singlePlayerPanel != null)
                m_singlePlayerPanel.gameObject.SetActive(false);

            if (m_multiplayerPanel != null)
                m_multiplayerPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="clip">音频片段</param>
        private void PlaySound(AudioClip clip)
        {
            if (m_audioSource != null && clip != null)
            {
                m_audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 加载模式统计数据
        /// </summary>
        private void LoadModeStats()
        {
            // TODO: 从PlayerPrefs或其他持久化存储加载
            string statsJson = PlayerPrefs.GetString("ModeStats", "{}");
            try
            {
                // 简单的JSON解析实现
                // 在实际项目中可能需要使用更完善的JSON库
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load mode stats: {e.Message}");
            }

            // 加载最后游戏模式
            string lastModeId = PlayerPrefs.GetString("LastPlayedMode", "");
            if (!string.IsNullOrEmpty(lastModeId))
            {
                m_lastPlayedMode = m_config.GetModeInfo(lastModeId);
            }
        }

        /// <summary>
        /// 保存模式统计数据
        /// </summary>
        private void SaveModeStats()
        {
            try
            {
                // TODO: 保存到PlayerPrefs或其他持久化存储
                // 在实际项目中可能需要使用更完善的序列化方式

                // 保存最后游戏模式
                if (m_lastPlayedMode != null)
                {
                    PlayerPrefs.SetString("LastPlayedMode", m_lastPlayedMode.ModeId);
                }

                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save mode stats: {e.Message}");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理模式卡片点击
        /// </summary>
        /// <param name="modeInfo">点击的模式</param>
        private void HandleModeCardClicked(GameModeInfo modeInfo)
        {
            SelectMode(modeInfo);
        }

        /// <summary>
        /// 处理快速开始点击
        /// </summary>
        private void HandleQuickStartClicked()
        {
            QuickStart();
        }

        /// <summary>
        /// 处理随机模式点击
        /// </summary>
        private void HandleRandomModeClicked()
        {
            RandomMode();
        }

        /// <summary>
        /// 处理模式启动
        /// </summary>
        /// <param name="modeInfo">启动的模式</param>
        /// <param name="difficulty">选择的难度</param>
        private void HandleModeStarted(GameModeInfo modeInfo, DifficultyLevel difficulty)
        {
            StartGameMode(modeInfo, difficulty);
        }

        private void HandleBackToModeSelection()
        {
            BackToModeSelection();
        }

        /// <summary>
        /// 处理单机模式选择
        /// </summary>
        /// <param name="modeType">选择的单机模式类型</param>
        private void HandleSinglePlayerModeSelected(SinglePlayerModePanel.SinglePlayerModeType modeType)
        {
            // 根据单机模式类型启动相应的游戏模式
            GameModeInfo gameMode = null;

            switch (modeType)
            {
                case SinglePlayerModePanel.SinglePlayerModeType.FreePractice:
                case SinglePlayerModePanel.SinglePlayerModeType.TargetPractice:
                case SinglePlayerModePanel.SinglePlayerModeType.SkillChallenge:
                    gameMode = m_config.GetModeInfo("practice");
                    break;
                case SinglePlayerModePanel.SinglePlayerModeType.AIBattle:
                    gameMode = m_config.GetModeInfo("ai_battle");
                    break;
            }

            if (gameMode != null)
            {
                StartGameMode(gameMode);
            }
        }

        /// <summary>
        /// 处理多人模式选择
        /// </summary>
        /// <param name="modeType">选择的多人模式类型</param>
        private void HandleMultiplayerModeSelected(MultiplayerModePanel.MultiplayerModeType modeType)
        {
            // 根据多人模式类型启动相应的游戏模式
            GameModeInfo gameMode = null;

            switch (modeType)
            {
                case MultiplayerModePanel.MultiplayerModeType.CreateRoom:
                case MultiplayerModePanel.MultiplayerModeType.JoinRoom:
                case MultiplayerModePanel.MultiplayerModeType.QuickMatch:
                    gameMode = m_config.GetModeInfo("online_multiplayer");
                    break;
                case MultiplayerModePanel.MultiplayerModeType.Friends:
                    gameMode = m_config.GetModeInfo("friends");
                    break;
            }

            if (gameMode != null)
            {
                StartGameMode(gameMode);
            }
        }

        /// <summary>
        /// 将GameModeInfo转换为SinglePlayerModeType
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        /// <returns>单机模式类型</returns>
        private SinglePlayerModePanel.SinglePlayerModeType ConvertToSinglePlayerMode(GameModeInfo modeInfo)
        {
            switch (modeInfo.ModeType)
            {
                case GameModeType.Practice:
                    return SinglePlayerModePanel.SinglePlayerModeType.FreePractice;
                case GameModeType.AIBattle:
                    return SinglePlayerModePanel.SinglePlayerModeType.AIBattle;
                case GameModeType.Training:
                    return SinglePlayerModePanel.SinglePlayerModeType.SkillChallenge;
                default:
                    return SinglePlayerModePanel.SinglePlayerModeType.FreePractice;
            }
        }

        /// <summary>
        /// 将GameModeInfo转换为MultiplayerModeType
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        /// <returns>多人模式类型</returns>
        private MultiplayerModePanel.MultiplayerModeType ConvertToMultiplayerMode(GameModeInfo modeInfo)
        {
            switch (modeInfo.ModeType)
            {
                case GameModeType.OnlineMultiplayer:
                    return MultiplayerModePanel.MultiplayerModeType.CreateRoom;
                case GameModeType.LocalMultiplayer:
                    return MultiplayerModePanel.MultiplayerModeType.JoinRoom;
                default:
                    return MultiplayerModePanel.MultiplayerModeType.CreateRoom;
            }
        }

        #endregion
    }
}