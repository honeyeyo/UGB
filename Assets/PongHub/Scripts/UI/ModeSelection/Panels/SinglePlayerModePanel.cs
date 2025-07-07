using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;
using PongHub.UI.Localization;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 单机模式详情面板
    /// 显示单机模式的详细信息和设置选项
    /// </summary>
    public class SinglePlayerModePanel : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private Image m_backgroundImage;
        [SerializeField] private Image m_modeIcon;
        [SerializeField] private TextMeshProUGUI m_modeTitleText;
        [SerializeField] private TextMeshProUGUI m_modeDescriptionText;

        [Header("难度设置")]
        [SerializeField] private Dropdown m_difficultyDropdown;
        [SerializeField] private TextMeshProUGUI m_difficultyDescription;
        [SerializeField] private Slider m_customDifficultySlider;
        [SerializeField] private Toggle m_adaptiveDifficultyToggle;

        [Header("游戏设置")]
        [SerializeField] private Slider m_gameDurationSlider;
        [SerializeField] private TextMeshProUGUI m_gameDurationText;
        [SerializeField] private Toggle m_enableAIOpponentToggle;
        [SerializeField] private Dropdown m_aiPersonalityDropdown;

        [Header("统计信息")]
        [SerializeField] private TextMeshProUGUI m_bestScoreText;
        [SerializeField] private TextMeshProUGUI m_timesPlayedText;
        [SerializeField] private TextMeshProUGUI m_averageScoreText;
        [SerializeField] private TextMeshProUGUI m_totalPlayTimeText;

        [Header("按钮")]
        [SerializeField] private Button m_startButton;
        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_practiceButton;

        [Header("动画")]
        [SerializeField] private float m_showAnimDuration = 0.3f;
        [SerializeField] private AnimationCurve m_animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("事件")]
        public UnityEvent<GameModeInfo, DifficultyLevel> OnStartGame;
        public UnityEvent OnBackClicked;
        public UnityEvent<GameModeInfo> OnPracticeMode;

        // 私有变量
        private GameModeInfo m_currentMode;
        private LocalizationManager m_localizationManager;
        private Coroutine m_animationCoroutine;

        #region 属性

        public GameModeInfo CurrentMode => m_currentMode;
        public bool IsShowing => gameObject.activeInHierarchy && m_canvasGroup.alpha > 0;

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
        /// 显示单机模式详情
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
        }

        #endregion

        #region 私有方法

        private void BindEvents()
        {
            if (m_startButton != null)
                m_startButton.onClick.AddListener(HandleStartButtonClicked);

            if (m_backButton != null)
                m_backButton.onClick.AddListener(HandleBackButtonClicked);

            if (m_practiceButton != null)
                m_practiceButton.onClick.AddListener(HandlePracticeButtonClicked);

            if (m_difficultyDropdown != null)
                m_difficultyDropdown.onValueChanged.AddListener(HandleDifficultyChanged);

            if (m_gameDurationSlider != null)
                m_gameDurationSlider.onValueChanged.AddListener(HandleGameDurationChanged);

            if (m_customDifficultySlider != null)
                m_customDifficultySlider.onValueChanged.AddListener(HandleCustomDifficultyChanged);
        }

        private void UnbindEvents()
        {
            if (m_startButton != null)
                m_startButton.onClick.RemoveListener(HandleStartButtonClicked);

            if (m_backButton != null)
                m_backButton.onClick.RemoveListener(HandleBackButtonClicked);

            if (m_practiceButton != null)
                m_practiceButton.onClick.RemoveListener(HandlePracticeButtonClicked);

            if (m_difficultyDropdown != null)
                m_difficultyDropdown.onValueChanged.RemoveListener(HandleDifficultyChanged);

            if (m_gameDurationSlider != null)
                m_gameDurationSlider.onValueChanged.RemoveListener(HandleGameDurationChanged);

            if (m_customDifficultySlider != null)
                m_customDifficultySlider.onValueChanged.RemoveListener(HandleCustomDifficultyChanged);
        }

        private void InitializeDropdowns()
        {
            // 初始化难度下拉菜单
            if (m_difficultyDropdown != null)
            {
                m_difficultyDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>
                {
                    GetLocalizedText("difficulty_easy"),
                    GetLocalizedText("difficulty_normal"),
                    GetLocalizedText("difficulty_hard"),
                    GetLocalizedText("difficulty_expert"),
                    GetLocalizedText("difficulty_custom")
                };
                m_difficultyDropdown.AddOptions(options);
                m_difficultyDropdown.value = 1; // Normal
            }

            // 初始化AI性格下拉菜单
            if (m_aiPersonalityDropdown != null)
            {
                m_aiPersonalityDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>
                {
                    GetLocalizedText("ai_personality_defensive"),
                    GetLocalizedText("ai_personality_balanced"),
                    GetLocalizedText("ai_personality_aggressive"),
                    GetLocalizedText("ai_personality_unpredictable")
                };
                m_aiPersonalityDropdown.AddOptions(options);
                m_aiPersonalityDropdown.value = 1; // Balanced
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
            // TODO: 从统计系统获取实际数据
            if (m_bestScoreText != null)
                m_bestScoreText.text = "150";

            if (m_timesPlayedText != null)
                m_timesPlayedText.text = "12";

            if (m_averageScoreText != null)
                m_averageScoreText.text = "98.5";

            if (m_totalPlayTimeText != null)
                m_totalPlayTimeText.text = "2h 35m";
        }

        private void UpdateSettings()
        {
            // 设置默认值
            if (m_gameDurationSlider != null)
            {
                m_gameDurationSlider.value = 300f; // 5分钟
                UpdateGameDurationText(300f);
            }

            if (m_customDifficultySlider != null)
            {
                m_customDifficultySlider.value = 0.5f;
                m_customDifficultySlider.gameObject.SetActive(false);
            }
        }

        private void UpdateLocalizedTexts()
        {
            // 更新所有本地化文本
            UpdateGameDurationText(m_gameDurationSlider != null ? m_gameDurationSlider.value : 300f);
        }

        private void UpdateGameDurationText(float seconds)
        {
            if (m_gameDurationText != null)
            {
                int minutes = Mathf.RoundToInt(seconds / 60f);
                m_gameDurationText.text = $"{minutes} {GetLocalizedText("minutes")}";
            }
        }

        private DifficultyLevel GetSelectedDifficulty()
        {
            if (m_difficultyDropdown == null)
                return DifficultyLevel.Normal;

            return (DifficultyLevel)m_difficultyDropdown.value;
        }

        private string GetLocalizedText(string key)
        {
            if (m_localizationManager != null)
                return m_localizationManager.GetLocalizedText(key);
            return key;
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

        private void HandleStartButtonClicked()
        {
            if (m_currentMode == null)
                return;

            DifficultyLevel difficulty = GetSelectedDifficulty();
            OnStartGame?.Invoke(m_currentMode, difficulty);
        }

        private void HandleBackButtonClicked()
        {
            OnBackClicked?.Invoke();
        }

        private void HandlePracticeButtonClicked()
        {
            if (m_currentMode == null)
                return;

            OnPracticeMode?.Invoke(m_currentMode);
        }

        private void HandleDifficultyChanged(int index)
        {
            bool isCustom = index == 4; // Custom difficulty index
            if (m_customDifficultySlider != null)
                m_customDifficultySlider.gameObject.SetActive(isCustom);

            // 更新难度描述
            if (m_difficultyDescription != null)
            {
                string descKey = $"difficulty_desc_{((DifficultyLevel)index).ToString().ToLower()}";
                m_difficultyDescription.text = GetLocalizedText(descKey);
            }
        }

        private void HandleGameDurationChanged(float value)
        {
            UpdateGameDurationText(value);
        }

        private void HandleCustomDifficultyChanged(float value)
        {
            // 自定义难度逻辑
        }

        #endregion
    }
}