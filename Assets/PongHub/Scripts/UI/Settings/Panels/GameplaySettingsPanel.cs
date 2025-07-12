using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 游戏设置面板
    /// </summary>
    public class GameplaySettingsPanel : MonoBehaviour
    {
        [Header("游戏难度")]
        [SerializeField] private SettingDropdown difficultyDropdown;
        [SerializeField] private SettingDropdown aiDifficultyDropdown;
        [SerializeField] private SettingSlider gameSpeedSlider;
        [SerializeField] private SettingSlider ballSpeedSlider;

        [Header("游戏辅助")]
        [SerializeField] private SettingToggle autoAimToggle;
        [SerializeField] private SettingToggle ballTrailToggle;
        [SerializeField] private SettingToggle paddleVibrateToggle;
        [SerializeField] private SettingToggle slowMotionToggle;
        [SerializeField] private SettingSlider assistLevelSlider;

        [Header("界面显示")]
        [SerializeField] private SettingToggle showUIToggle;
        [SerializeField] private SettingToggle showStatsToggle;
        [SerializeField] private SettingToggle showTutorialsToggle;
        [SerializeField] private SettingToggle showScoreToggle;
        [SerializeField] private SettingToggle showTimerToggle;

        [Header("音频")]
        [SerializeField] private SettingToggle gameplaySoundsToggle;
        [SerializeField] private SettingToggle commentaryToggle;
        [SerializeField] private SettingSlider commentaryVolumeSlider;

        [Header("比赛设置")]
        [SerializeField] private SettingDropdown matchTypeDropdown;
        [SerializeField] private SettingDropdown matchDurationDropdown;
        [SerializeField] private SettingSlider scoreToWinSlider;
        [SerializeField] private SettingToggle suddenDeathToggle;

        [Header("辅助功能")]
        [SerializeField] private SettingToggle enableAssistModeToggle;
        [SerializeField] private SettingToggle enableDebugInfoToggle;
        [SerializeField] private SettingToggle highContrastToggle;
        [SerializeField] private SettingSlider uiScaleSlider;
        [SerializeField] private SettingToggle enableSubtitlesToggle;

        [Header("本地化")]
        [SerializeField] private SettingDropdown languageDropdown;

        [Header("自动保存")]
        [SerializeField] private SettingToggle autoSaveToggle;

        private GameplaySettings currentSettings;
        private bool isUpdating = false;

        private void Start()
        {
            InitializePanel();
            SetupEventHandlers();
            LoadCurrentSettings();
        }

        private void InitializePanel()
        {
            // 初始化难度下拉框
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new[] { "简单", "普通", "困难", "专家" });

            // 初始化AI难度下拉框
            aiDifficultyDropdown.ClearOptions();
            aiDifficultyDropdown.AddOptions(new[] { "简单", "普通", "困难", "专家" });

            // 初始化比赛类型下拉框
            matchTypeDropdown.ClearOptions();
            matchTypeDropdown.AddOptions(new[] { "练习", "快速比赛", "排位赛", "锦标赛" });

            // 初始化比赛时长下拉框
            matchDurationDropdown.ClearOptions();
            matchDurationDropdown.AddOptions(new[] { "5分钟", "10分钟", "15分钟", "30分钟" });

            // 初始化语言下拉框
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new[] { "英语", "中文", "日语", "韩语", "法语", "德语", "西班牙语" });

            // 设置滑块范围
            gameSpeedSlider.SetMinMaxValues(0.5f, 2.0f);
            ballSpeedSlider.SetMinMaxValues(0.5f, 2.0f);
            assistLevelSlider.SetMinMaxValues(0f, 1f);
            commentaryVolumeSlider.SetMinMaxValues(0f, 1f);
            scoreToWinSlider.SetMinMaxValues(5f, 21f);
            uiScaleSlider.SetMinMaxValues(0.5f, 2.0f);
        }

        private void SetupEventHandlers()
        {
            // 游戏难度设置
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            aiDifficultyDropdown.onValueChanged.AddListener(OnAIDifficultyChanged);
            gameSpeedSlider.onValueChanged.AddListener(OnGameSpeedChanged);
            ballSpeedSlider.onValueChanged.AddListener(OnBallSpeedChanged);

            // 游戏辅助设置
            autoAimToggle.onValueChanged.AddListener(OnAutoAimChanged);
            ballTrailToggle.onValueChanged.AddListener(OnBallTrailChanged);
            paddleVibrateToggle.onValueChanged.AddListener(OnPaddleVibrateChanged);
            slowMotionToggle.onValueChanged.AddListener(OnSlowMotionChanged);
            assistLevelSlider.onValueChanged.AddListener(OnAssistLevelChanged);

            // 界面显示设置
            showUIToggle.onValueChanged.AddListener(OnShowUIChanged);
            showStatsToggle.onValueChanged.AddListener(OnShowStatsChanged);
            showTutorialsToggle.onValueChanged.AddListener(OnShowTutorialsChanged);
            showScoreToggle.onValueChanged.AddListener(OnShowScoreChanged);
            showTimerToggle.onValueChanged.AddListener(OnShowTimerChanged);

            // 音频设置
            gameplaySoundsToggle.onValueChanged.AddListener(OnGameplaySoundsChanged);
            commentaryToggle.onValueChanged.AddListener(OnCommentaryChanged);
            commentaryVolumeSlider.onValueChanged.AddListener(OnCommentaryVolumeChanged);

            // 比赛设置
            matchTypeDropdown.onValueChanged.AddListener(OnMatchTypeChanged);
            matchDurationDropdown.onValueChanged.AddListener(OnMatchDurationChanged);
            scoreToWinSlider.onValueChanged.AddListener(OnScoreToWinChanged);
            suddenDeathToggle.onValueChanged.AddListener(OnSuddenDeathChanged);

            // 辅助功能设置
            enableAssistModeToggle.onValueChanged.AddListener(OnEnableAssistModeChanged);
            enableDebugInfoToggle.onValueChanged.AddListener(OnEnableDebugInfoChanged);
            highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);
            uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);
            enableSubtitlesToggle.onValueChanged.AddListener(OnEnableSubtitlesChanged);

            // 本地化设置
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            // 自动保存设置
            autoSaveToggle.onValueChanged.AddListener(OnAutoSaveChanged);
        }

        private void LoadCurrentSettings()
        {
            currentSettings = SettingsManager.Instance.GetGameplaySettings();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (currentSettings == null) return;

            isUpdating = true;

            // 游戏难度设置
            difficultyDropdown.value = (int)currentSettings.defaultDifficulty;
            aiDifficultyDropdown.value = (int)currentSettings.defaultDifficulty; // 使用相同的难度
            gameSpeedSlider.value = 1.0f; // 默认值，因为GameplaySettings中没有这个字段
            ballSpeedSlider.value = 1.0f; // 默认值，因为GameplaySettings中没有这个字段

            // 游戏辅助设置
            autoAimToggle.isOn = currentSettings.enableAssistMode;
            ballTrailToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段
            paddleVibrateToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段
            slowMotionToggle.isOn = false; // 默认值，因为GameplaySettings中没有这个字段
            assistLevelSlider.value = currentSettings.enableAssistMode ? 1f : 0f;

            // 界面显示设置
            showUIToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段
            showStatsToggle.isOn = currentSettings.showStatistics;
            showTutorialsToggle.isOn = currentSettings.showTutorials;
            showScoreToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段
            showTimerToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段

            // 音频设置
            gameplaySoundsToggle.isOn = true; // 默认值，因为GameplaySettings中没有这个字段
            commentaryToggle.isOn = false; // 默认值，因为GameplaySettings中没有这个字段
            commentaryVolumeSlider.value = 0.5f; // 默认值，因为GameplaySettings中没有这个字段

            // 比赛设置
            matchTypeDropdown.value = 0; // 默认值，因为GameplaySettings中没有这个字段
            matchDurationDropdown.value = 1; // 默认值，因为GameplaySettings中没有这个字段
            scoreToWinSlider.value = 11f; // 默认值，因为GameplaySettings中没有这个字段
            suddenDeathToggle.isOn = false; // 默认值，因为GameplaySettings中没有这个字段

            // 辅助功能设置
            enableAssistModeToggle.isOn = currentSettings.enableAssistMode;
            enableDebugInfoToggle.isOn = currentSettings.enableDebugInfo;
            highContrastToggle.isOn = currentSettings.highContrast;
            uiScaleSlider.value = currentSettings.uiScale;
            enableSubtitlesToggle.isOn = currentSettings.enableSubtitles;

            // 本地化设置
            languageDropdown.value = (int)currentSettings.language;

            // 自动保存设置
            autoSaveToggle.isOn = currentSettings.autoSave;

            isUpdating = false;
        }

        #region 难度预设

        private void ApplyDifficultyPreset(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Easy:
                    aiDifficultyDropdown.value = 0;
                    gameSpeedSlider.value = 0.8f;
                    ballSpeedSlider.value = 0.8f;
                    autoAimToggle.isOn = true;
                    assistLevelSlider.value = 0.8f;
                    break;

                case DifficultyLevel.Normal:
                    aiDifficultyDropdown.value = 1;
                    gameSpeedSlider.value = 1.0f;
                    ballSpeedSlider.value = 1.0f;
                    autoAimToggle.isOn = false;
                    assistLevelSlider.value = 0.4f;
                    break;

                case DifficultyLevel.Hard:
                    aiDifficultyDropdown.value = 2;
                    gameSpeedSlider.value = 1.2f;
                    ballSpeedSlider.value = 1.2f;
                    autoAimToggle.isOn = false;
                    assistLevelSlider.value = 0.2f;
                    break;

                case DifficultyLevel.Expert:
                    aiDifficultyDropdown.value = 3;
                    gameSpeedSlider.value = 1.5f;
                    ballSpeedSlider.value = 1.5f;
                    autoAimToggle.isOn = false;
                    assistLevelSlider.value = 0.0f;
                    break;
            }
        }

        #endregion

        #region 事件处理器

        private void OnDifficultyChanged(int value)
        {
            if (isUpdating) return;
            currentSettings.defaultDifficulty = (DifficultyLevel)value;
            ApplyDifficultyPreset(currentSettings.defaultDifficulty);
            SaveSettings();
        }

        private void OnAIDifficultyChanged(int value)
        {
            if (isUpdating) return;
            // AI难度暂时映射到默认难度
            currentSettings.defaultDifficulty = (DifficultyLevel)value;
            SaveSettings();
        }

        private void OnGameSpeedChanged(float value)
        {
            if (isUpdating) return;
            // 游戏速度设置暂时没有对应字段，只打印日志
            Debug.Log($"游戏速度设置: {value}");
        }

        private void OnBallSpeedChanged(float value)
        {
            if (isUpdating) return;
            // 球速设置暂时没有对应字段，只打印日志
            Debug.Log($"球速设置: {value}");
        }

        private void OnAutoAimChanged(bool value)
        {
            if (isUpdating) return;
            // 自动瞄准映射到辅助模式
            currentSettings.enableAssistMode = value;
            SaveSettings();
        }

        private void OnBallTrailChanged(bool value)
        {
            if (isUpdating) return;
            // 球轨迹设置暂时没有对应字段，只打印日志
            Debug.Log($"球轨迹设置: {value}");
        }

        private void OnPaddleVibrateChanged(bool value)
        {
            if (isUpdating) return;
            // 球拍震动设置暂时没有对应字段，只打印日志
            Debug.Log($"球拍震动设置: {value}");
        }

        private void OnSlowMotionChanged(bool value)
        {
            if (isUpdating) return;
            // 慢动作设置暂时没有对应字段，只打印日志
            Debug.Log($"慢动作设置: {value}");
        }

        private void OnAssistLevelChanged(float value)
        {
            if (isUpdating) return;
            // 辅助级别映射到辅助模式
            currentSettings.enableAssistMode = value > 0.5f;
            SaveSettings();
        }

        private void OnShowUIChanged(bool value)
        {
            if (isUpdating) return;
            // UI显示设置暂时没有对应字段，只打印日志
            Debug.Log($"UI显示设置: {value}");
        }

        private void OnShowStatsChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.showStatistics = value;
            SaveSettings();
        }

        private void OnShowTutorialsChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.showTutorials = value;
            SaveSettings();
        }

        private void OnShowScoreChanged(bool value)
        {
            if (isUpdating) return;
            // 分数显示设置暂时没有对应字段，只打印日志
            Debug.Log($"分数显示设置: {value}");
        }

        private void OnShowTimerChanged(bool value)
        {
            if (isUpdating) return;
            // 计时器显示设置暂时没有对应字段，只打印日志
            Debug.Log($"计时器显示设置: {value}");
        }

        private void OnGameplaySoundsChanged(bool value)
        {
            if (isUpdating) return;
            // 游戏音效设置暂时没有对应字段，只打印日志
            Debug.Log($"游戏音效设置: {value}");
        }

        private void OnCommentaryChanged(bool value)
        {
            if (isUpdating) return;
            // 解说设置暂时没有对应字段，只打印日志
            Debug.Log($"解说设置: {value}");
        }

        private void OnCommentaryVolumeChanged(float value)
        {
            if (isUpdating) return;
            // 解说音量设置暂时没有对应字段，只打印日志
            Debug.Log($"解说音量设置: {value}");
        }

        private void OnMatchTypeChanged(int value)
        {
            if (isUpdating) return;
            // 比赛类型设置暂时没有对应字段，只打印日志
            Debug.Log($"比赛类型设置: {value}");
        }

        private void OnMatchDurationChanged(int value)
        {
            if (isUpdating) return;
            // 比赛时长设置暂时没有对应字段，只打印日志
            Debug.Log($"比赛时长设置: {value}");
        }

        private void OnScoreToWinChanged(float value)
        {
            if (isUpdating) return;
            // 获胜分数设置暂时没有对应字段，只打印日志
            Debug.Log($"获胜分数设置: {value}");
        }

        private void OnSuddenDeathChanged(bool value)
        {
            if (isUpdating) return;
            // 突然死亡设置暂时没有对应字段，只打印日志
            Debug.Log($"突然死亡设置: {value}");
        }

        private void OnEnableAssistModeChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.enableAssistMode = value;
            SaveSettings();
        }

        private void OnEnableDebugInfoChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.enableDebugInfo = value;
            SaveSettings();
        }

        private void OnHighContrastChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.highContrast = value;
            SaveSettings();
        }

        private void OnUIScaleChanged(float value)
        {
            if (isUpdating) return;
            currentSettings.uiScale = value;
            SaveSettings();
        }

        private void OnEnableSubtitlesChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.enableSubtitles = value;
            SaveSettings();
        }

        private void OnLanguageChanged(int value)
        {
            if (isUpdating) return;
            currentSettings.language = (LanguageCode)value;
            SaveSettings();
        }

        private void OnAutoSaveChanged(bool value)
        {
            if (isUpdating) return;
            currentSettings.autoSave = value;
            SaveSettings();
        }

        #endregion

        #region 辅助方法

        private void SaveSettings()
        {
            SettingsManager.Instance.SaveGameplaySettings(currentSettings);
        }

        public void ResetToDefaults()
        {
            currentSettings = new GameplaySettings();
            SettingsManager.Instance.SaveGameplaySettings(currentSettings);
            UpdateUI();
        }

        public void OnSettingsUpdated(GameplaySettings newSettings)
        {
            currentSettings = newSettings;
            UpdateUI();
        }

        /// <summary>
        /// 刷新面板
        /// </summary>
        public void RefreshPanel()
        {
            LoadCurrentSettings();
        }

        #endregion
    }
}