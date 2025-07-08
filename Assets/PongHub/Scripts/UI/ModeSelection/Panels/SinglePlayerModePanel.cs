using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;
using PongHub.UI.Localization;
using System.Collections.Generic;
using PongHub.Core;
using PongHub.Core.Audio;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 单机模式面板
    /// 提供练习模式选项、AI对战难度选择和个人成绩显示功能
    /// </summary>
    public class SinglePlayerModePanel : MonoBehaviour
    {
        [Header("面板配置")]
        [SerializeField] private GameObject m_panelRoot;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Transform m_modesContainer;
        [SerializeField] private GameObject m_modeButtonPrefab;
        [SerializeField] private Button m_backButton;

        [Header("练习模式配置")]
        [SerializeField] private GameObject m_practicePanel;
        [SerializeField] private Button m_freePracticeButton;
        [SerializeField] private Button m_targetPracticeButton;
        [SerializeField] private Button m_skillChallengeButton;

        [Header("AI对战配置")]
        [SerializeField] private GameObject m_aiPanel;
        [SerializeField] private Transform m_difficultyContainer;
        [SerializeField] private GameObject m_difficultyButtonPrefab;
        [SerializeField] private Slider m_difficultySlider;
        [SerializeField] private TextMeshProUGUI m_difficultyText;

        [Header("个人成绩显示")]
        [SerializeField] private GameObject m_statsPanel;
        [SerializeField] private TextMeshProUGUI m_totalGamesText;
        [SerializeField] private TextMeshProUGUI m_winRateText;
        [SerializeField] private TextMeshProUGUI m_bestScoreText;
        [SerializeField] private TextMeshProUGUI m_playTimeText;
        [SerializeField] private TextMeshProUGUI m_lastPlayedText;

        [Header("本地化键")]
        [SerializeField] private string m_titleKey = "single_player.title";
        [SerializeField] private string m_practiceKey = "single_player.practice";
        [SerializeField] private string m_aiKey = "single_player.ai_battle";
        [SerializeField] private string m_statsKey = "single_player.stats";

        // 单机模式类型
        public enum SinglePlayerModeType
        {
            FreePractice,       // 自由练习
            TargetPractice,     // 目标练习
            SkillChallenge,     // 技能挑战
            AIBattle,           // AI对战
            Stats               // 统计
        }

        // AI难度级别
        public enum AIDifficulty
        {
            Beginner = 0,       // 初级
            Easy = 1,           // 简单
            Medium = 2,         // 中等
            Hard = 3,           // 困难
            Expert = 4          // 专家
        }

        // 事件定义
        public System.Action<SinglePlayerModeType> OnModeSelected;
        public System.Action<AIDifficulty> OnDifficultySelected;
        public System.Action OnBackPressed;
        public System.Action OnBack;

        private List<Button> m_modeButtons = new List<Button>();
        private List<Button> m_difficultyButtons = new List<Button>();
        private PlayerStats m_playerStats;
        private LocalizationManager m_localizationManager;

        private bool m_isInitialized = false;
        private SinglePlayerModeType m_selectedMode = SinglePlayerModeType.FreePractice;
        private AIDifficulty m_selectedDifficulty = AIDifficulty.Easy;

        /// <summary>
        /// 初始化单机模式面板
        /// </summary>
        private void Start()
        {
            InitializeComponents();
            SetupUI();
            LoadPlayerStats();
            UpdateUI();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            if (m_isInitialized) return;

            // 获取管理器引用
            m_localizationManager = FindObjectOfType<LocalizationManager>();

            // 创建玩家统计数据
            m_playerStats = new PlayerStats();

            m_isInitialized = true;
        }

        /// <summary>
        /// 设置UI界面
        /// </summary>
        private void SetupUI()
        {
            // 设置标题
            UpdateTitle();

            // 创建模式按钮
            CreateModeButtons();

            // 创建难度按钮
            CreateDifficultyButtons();

            // 设置事件监听
            SetupEventListeners();

            // 初始化面板状态
            ShowModeSelection();
        }

        /// <summary>
        /// 更新标题文本
        /// </summary>
        private void UpdateTitle()
        {
            if (m_titleText != null && m_localizationManager != null)
            {
                m_titleText.text = m_localizationManager.GetLocalizedText(m_titleKey);
            }
        }

        /// <summary>
        /// 创建模式选择按钮
        /// </summary>
        private void CreateModeButtons()
        {
            if (m_modesContainer == null || m_modeButtonPrefab == null) return;

            // 清除现有按钮
            ClearModeButtons();

            // 创建各种模式按钮
            var modeTypes = new[]
            {
                SinglePlayerModeType.FreePractice,
                SinglePlayerModeType.TargetPractice,
                SinglePlayerModeType.SkillChallenge,
                SinglePlayerModeType.AIBattle,
                SinglePlayerModeType.Stats
            };

            foreach (var modeType in modeTypes)
            {
                CreateModeButton(modeType);
            }
        }

        /// <summary>
        /// 创建单个模式按钮
        /// </summary>
        private void CreateModeButton(SinglePlayerModeType modeType)
        {
            GameObject buttonObj = Instantiate(m_modeButtonPrefab, m_modesContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // 设置按钮文本
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetModeDisplayName(modeType);
                }

                // 设置按钮点击事件
                button.onClick.AddListener(() => OnModeButtonClicked(modeType));

                // 添加到按钮列表
                m_modeButtons.Add(button);
            }
        }

        /// <summary>
        /// 创建难度选择按钮
        /// </summary>
        private void CreateDifficultyButtons()
        {
            if (m_difficultyContainer == null || m_difficultyButtonPrefab == null) return;

            // 清除现有按钮
            ClearDifficultyButtons();

            // 创建各难度级别按钮
            for (int i = 0; i < 5; i++)
            {
                AIDifficulty difficulty = (AIDifficulty)i;
                CreateDifficultyButton(difficulty);
            }

            // 设置难度滑块
            if (m_difficultySlider != null)
            {
                m_difficultySlider.minValue = 0;
                m_difficultySlider.maxValue = 4;
                m_difficultySlider.value = 1; // 默认简单难度
                m_difficultySlider.onValueChanged.AddListener(OnDifficultySliderChanged);
            }
        }

        /// <summary>
        /// 创建单个难度按钮
        /// </summary>
        private void CreateDifficultyButton(AIDifficulty difficulty)
        {
            GameObject buttonObj = Instantiate(m_difficultyButtonPrefab, m_difficultyContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button != null)
            {
                // 设置按钮文本
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetDifficultyDisplayName(difficulty);
                }

                // 设置按钮点击事件
                button.onClick.AddListener(() => OnDifficultyButtonClicked(difficulty));

                // 添加到按钮列表
                m_difficultyButtons.Add(button);
            }
        }

        /// <summary>
        /// 设置事件监听器
        /// </summary>
        private void SetupEventListeners()
        {
            // 返回按钮
            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(OnBackButtonClicked);
            }

            // 练习模式按钮
            if (m_freePracticeButton != null)
            {
                m_freePracticeButton.onClick.AddListener(() => StartPracticeMode(SinglePlayerModeType.FreePractice));
            }

            if (m_targetPracticeButton != null)
            {
                m_targetPracticeButton.onClick.AddListener(() => StartPracticeMode(SinglePlayerModeType.TargetPractice));
            }

            if (m_skillChallengeButton != null)
            {
                m_skillChallengeButton.onClick.AddListener(() => StartPracticeMode(SinglePlayerModeType.SkillChallenge));
            }
        }

        /// <summary>
        /// 加载玩家统计数据
        /// </summary>
        private void LoadPlayerStats()
        {
            // 从PlayerPrefs或存档系统加载统计数据
            m_playerStats.totalGames = PlayerPrefs.GetInt("SP_TotalGames", 0);
            m_playerStats.totalWins = PlayerPrefs.GetInt("SP_TotalWins", 0);
            m_playerStats.bestScore = PlayerPrefs.GetFloat("SP_BestScore", 0f);
            m_playerStats.totalPlayTime = PlayerPrefs.GetFloat("SP_TotalPlayTime", 0f);
            m_playerStats.lastPlayedTime = PlayerPrefs.GetString("SP_LastPlayed", "");

            // 计算胜率
            if (m_playerStats.totalGames > 0)
            {
                m_playerStats.winRate = (float)m_playerStats.totalWins / m_playerStats.totalGames;
            }
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            UpdateStatsDisplay();
            UpdateDifficultyDisplay();
        }

        /// <summary>
        /// 更新统计数据显示
        /// </summary>
        private void UpdateStatsDisplay()
        {
            if (m_totalGamesText != null)
            {
                m_totalGamesText.text = $"总场次: {m_playerStats.totalGames}";
            }

            if (m_winRateText != null)
            {
                m_winRateText.text = $"胜率: {m_playerStats.winRate:P1}";
            }

            if (m_bestScoreText != null)
            {
                m_bestScoreText.text = $"最高分: {m_playerStats.bestScore:F0}";
            }

            if (m_playTimeText != null)
            {
                int hours = Mathf.FloorToInt(m_playerStats.totalPlayTime / 3600f);
                int minutes = Mathf.FloorToInt((m_playerStats.totalPlayTime % 3600f) / 60f);
                m_playTimeText.text = $"游戏时间: {hours}h {minutes}m";
            }

            if (m_lastPlayedText != null)
            {
                m_lastPlayedText.text = $"上次游戏: {m_playerStats.lastPlayedTime}";
            }
        }

        /// <summary>
        /// 更新难度显示
        /// </summary>
        private void UpdateDifficultyDisplay()
        {
            if (m_difficultyText != null && m_difficultySlider != null)
            {
                AIDifficulty currentDifficulty = (AIDifficulty)Mathf.RoundToInt(m_difficultySlider.value);
                m_difficultyText.text = GetDifficultyDisplayName(currentDifficulty);
            }
        }

        /// <summary>
        /// 显示模式选择界面
        /// </summary>
        public void ShowModeSelection()
        {
            SetPanelActive(m_panelRoot, true);
            SetPanelActive(m_practicePanel, false);
            SetPanelActive(m_aiPanel, false);
            SetPanelActive(m_statsPanel, false);
        }

        /// <summary>
        /// 显示练习模式界面
        /// </summary>
        public void ShowPracticeMode()
        {
            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_practicePanel, true);
            SetPanelActive(m_aiPanel, false);
            SetPanelActive(m_statsPanel, false);
        }

        /// <summary>
        /// 显示AI对战界面
        /// </summary>
        public void ShowAIBattleMode()
        {
            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_practicePanel, false);
            SetPanelActive(m_aiPanel, true);
            SetPanelActive(m_statsPanel, false);
        }

        /// <summary>
        /// 显示统计界面
        /// </summary>
        public void ShowStats()
        {
            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_practicePanel, false);
            SetPanelActive(m_aiPanel, false);
            SetPanelActive(m_statsPanel, true);

            // 刷新统计数据
            LoadPlayerStats();
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 模式按钮点击处理
        /// </summary>
        private void OnModeButtonClicked(SinglePlayerModeType modeType)
        {
            PlayButtonClickSound();

            switch (modeType)
            {
                case SinglePlayerModeType.FreePractice:
                case SinglePlayerModeType.TargetPractice:
                case SinglePlayerModeType.SkillChallenge:
                    ShowPracticeMode();
                    break;
                case SinglePlayerModeType.AIBattle:
                    ShowAIBattleMode();
                    break;
                case SinglePlayerModeType.Stats:
                    ShowStats();
                    break;
            }

            OnModeSelected?.Invoke(modeType);
        }

        /// <summary>
        /// 难度按钮点击处理
        /// </summary>
        private void OnDifficultyButtonClicked(AIDifficulty difficulty)
        {
            PlayButtonClickSound();

            // 更新滑块值
            if (m_difficultySlider != null)
            {
                m_difficultySlider.value = (int)difficulty;
            }

            // 更新显示
            UpdateDifficultyDisplay();

            OnDifficultySelected?.Invoke(difficulty);
        }

        /// <summary>
        /// 难度滑块值改变处理
        /// </summary>
        private void OnDifficultySliderChanged(float value)
        {
            UpdateDifficultyDisplay();

            AIDifficulty difficulty = (AIDifficulty)Mathf.RoundToInt(value);
            OnDifficultySelected?.Invoke(difficulty);
        }

        /// <summary>
        /// 返回按钮点击处理
        /// </summary>
        private void OnBackButtonClicked()
        {
            PlayButtonClickSound();
            OnBackPressed?.Invoke();
        }

        /// <summary>
        /// 开始练习模式
        /// </summary>
        private void StartPracticeMode(SinglePlayerModeType modeType)
        {
            PlayButtonClickSound();

            // 记录开始时间
            m_playerStats.lastPlayedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SavePlayerStats();

            OnModeSelected?.Invoke(modeType);
        }

        /// <summary>
        /// 获取模式显示名称
        /// </summary>
        private string GetModeDisplayName(SinglePlayerModeType modeType)
        {
            switch (modeType)
            {
                case SinglePlayerModeType.FreePractice:
                    return m_localizationManager?.GetLocalizedText("single_player.free_practice") ?? "自由练习";
                case SinglePlayerModeType.TargetPractice:
                    return m_localizationManager?.GetLocalizedText("single_player.target_practice") ?? "目标练习";
                case SinglePlayerModeType.SkillChallenge:
                    return m_localizationManager?.GetLocalizedText("single_player.skill_challenge") ?? "技能挑战";
                case SinglePlayerModeType.AIBattle:
                    return m_localizationManager?.GetLocalizedText("single_player.ai_battle") ?? "AI对战";
                case SinglePlayerModeType.Stats:
                    return m_localizationManager?.GetLocalizedText("single_player.stats") ?? "个人统计";
                default:
                    return "未知模式";
            }
        }

        /// <summary>
        /// 获取难度显示名称
        /// </summary>
        private string GetDifficultyDisplayName(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Beginner:
                    return m_localizationManager?.GetLocalizedText("difficulty.beginner") ?? "初学者";
                case AIDifficulty.Easy:
                    return m_localizationManager?.GetLocalizedText("difficulty.easy") ?? "简单";
                case AIDifficulty.Medium:
                    return m_localizationManager?.GetLocalizedText("difficulty.medium") ?? "中等";
                case AIDifficulty.Hard:
                    return m_localizationManager?.GetLocalizedText("difficulty.hard") ?? "困难";
                case AIDifficulty.Expert:
                    return m_localizationManager?.GetLocalizedText("difficulty.expert") ?? "专家";
                default:
                    return "未知难度";
            }
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        private void PlayButtonClickSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound("button_click");
            }
        }

        /// <summary>
        /// 开始游戏回调
        /// </summary>
        public void OnStartGame()
        {
            PlayButtonClickSound();

            // 根据当前选择的模式类型开始相应的游戏
            switch (m_selectedMode)
            {
                case SinglePlayerModeType.FreePractice:
                    StartFreePractice();
                    break;
                case SinglePlayerModeType.TargetPractice:
                    StartTargetPractice();
                    break;
                case SinglePlayerModeType.SkillChallenge:
                    StartSkillChallenge();
                    break;
                case SinglePlayerModeType.AIBattle:
                    StartAIBattle();
                    break;
                default:
                    Debug.LogWarning($"未知的游戏模式: {m_selectedMode}");
                    break;
            }
        }

        /// <summary>
        /// 返回按钮点击回调
        /// </summary>
        public void OnBackClicked()
        {
            PlayButtonClickSound();

            // 触发返回事件
            OnBack?.Invoke();
        }

        /// <summary>
        /// 显示模式详情
        /// </summary>
        public void ShowModeDetails(SinglePlayerModeType modeType)
        {
            m_selectedMode = modeType;

            // 更新UI显示
            UpdateModeDisplay();

            // 如果有统计按钮，更新统计显示
            if (modeType == SinglePlayerModeType.Stats)
            {
                UpdateStatsDisplay();
            }
        }

        /// <summary>
        /// 更新模式显示
        /// </summary>
        private void UpdateModeDisplay()
        {
            // 根据选择的模式显示相应的面板
            switch (m_selectedMode)
            {
                case SinglePlayerModeType.FreePractice:
                case SinglePlayerModeType.TargetPractice:
                case SinglePlayerModeType.SkillChallenge:
                    ShowPracticeMode();
                    break;
                case SinglePlayerModeType.AIBattle:
                    ShowAIBattleMode();
                    break;
                case SinglePlayerModeType.Stats:
                    ShowStats();
                    break;
                default:
                    ShowModeSelection();
                    break;
            }
        }

        /// <summary>
        /// 开始自由练习
        /// </summary>
        private void StartFreePractice()
        {
            // 设置游戏模式为本地模式
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(PongHub.Core.GameMode.Local);
            }

            // 加载练习场景
            LoadGameScene();
        }

        /// <summary>
        /// 开始目标练习
        /// </summary>
        private void StartTargetPractice()
        {
            // 设置游戏模式为本地模式
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(PongHub.Core.GameMode.Local);
                // 可以设置特定的练习参数
            }

            // 加载练习场景
            LoadGameScene();
        }

        /// <summary>
        /// 开始技能挑战
        /// </summary>
        private void StartSkillChallenge()
        {
            // 设置游戏模式为本地模式
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(PongHub.Core.GameMode.Local);
            }

            // 加载挑战场景
            LoadGameScene();
        }

        /// <summary>
        /// 开始AI对战
        /// </summary>
        private void StartAIBattle()
        {
            // 设置游戏模式为本地模式
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(PongHub.Core.GameMode.Local);
            }

            // 尝试设置AI难度（如果有相关管理器）
            var modeSingleManager = FindObjectOfType<ModeSingleManager>();
            if (modeSingleManager != null)
            {
                // 将AIDifficulty转换为float值
                float difficultyValue = ConvertAIDifficultyToFloat(m_selectedDifficulty);
                modeSingleManager.SetAIDifficulty(difficultyValue);
            }

            // 加载对战场景
            LoadGameScene();
        }

        /// <summary>
        /// 将AI难度枚举转换为浮点数值
        /// </summary>
        private float ConvertAIDifficultyToFloat(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Beginner:
                    return 0.2f;
                case AIDifficulty.Easy:
                    return 0.4f;
                case AIDifficulty.Medium:
                    return 0.6f;
                case AIDifficulty.Hard:
                    return 0.8f;
                case AIDifficulty.Expert:
                    return 1.0f;
                default:
                    return 0.6f; // 默认中等难度
            }
        }

        /// <summary>
        /// 加载游戏场景
        /// </summary>
        private void LoadGameScene()
        {
            // 这里应该加载游戏场景
            // 临时实现，实际项目中应该有场景管理器
            Debug.Log($"加载游戏场景: {m_selectedMode}, 难度: {m_selectedDifficulty}");

            // 可以使用SceneManager.LoadScene来加载场景
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        /// <summary>
        /// 设置面板激活状态
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        /// <summary>
        /// 清除模式按钮
        /// </summary>
        private void ClearModeButtons()
        {
            foreach (var button in m_modeButtons)
            {
                if (button != null)
                {
                    DestroyImmediate(button.gameObject);
                }
            }
            m_modeButtons.Clear();
        }

        /// <summary>
        /// 清除难度按钮
        /// </summary>
        private void ClearDifficultyButtons()
        {
            foreach (var button in m_difficultyButtons)
            {
                if (button != null)
                {
                    DestroyImmediate(button.gameObject);
                }
            }
            m_difficultyButtons.Clear();
        }

        /// <summary>
        /// 保存玩家统计数据
        /// </summary>
        private void SavePlayerStats()
        {
            PlayerPrefs.SetInt("SP_TotalGames", m_playerStats.totalGames);
            PlayerPrefs.SetInt("SP_TotalWins", m_playerStats.totalWins);
            PlayerPrefs.SetFloat("SP_BestScore", m_playerStats.bestScore);
            PlayerPrefs.SetFloat("SP_TotalPlayTime", m_playerStats.totalPlayTime);
            PlayerPrefs.SetString("SP_LastPlayed", m_playerStats.lastPlayedTime);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 更新游戏统计数据
        /// </summary>
        public void UpdateGameStats(bool won, float score, float playTime)
        {
            m_playerStats.totalGames++;
            if (won)
            {
                m_playerStats.totalWins++;
            }

            if (score > m_playerStats.bestScore)
            {
                m_playerStats.bestScore = score;
            }

            m_playerStats.totalPlayTime += playTime;
            m_playerStats.lastPlayedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 重新计算胜率
            m_playerStats.winRate = (float)m_playerStats.totalWins / m_playerStats.totalGames;

            SavePlayerStats();
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStats()
        {
            m_playerStats = new PlayerStats();
            SavePlayerStats();
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 销毁时清理
        /// </summary>
        private void OnDestroy()
        {
            ClearModeButtons();
            ClearDifficultyButtons();
        }
    }

    /// <summary>
    /// 玩家统计数据
    /// </summary>
    [System.Serializable]
    public class PlayerStats
    {
        public int totalGames = 0;              // 总场次
        public int totalWins = 0;               // 总胜利次数
        public float winRate = 0f;              // 胜率
        public float bestScore = 0f;            // 最高分
        public float totalPlayTime = 0f;        // 总游戏时间(秒)
        public string lastPlayedTime = "";      // 上次游戏时间
    }
}