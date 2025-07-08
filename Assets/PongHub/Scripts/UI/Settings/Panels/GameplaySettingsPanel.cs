using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 游戏玩法设置面板
    /// Gameplay settings panel for game mechanics and preferences
    /// </summary>
    public class GameplaySettingsPanel : MonoBehaviour
    {
        [Header("游戏难度设置")]
        [SerializeField] private SettingDropdown difficultyDropdown;
        [SerializeField] private SettingSlider aiDifficultySlider;
        [SerializeField] private SettingSlider gameSpeedSlider;
        [SerializeField] private SettingSlider ballSpeedSlider;

        [Header("辅助功能设置")]
        [SerializeField] private SettingToggle autoAimToggle;
        [SerializeField] private SettingToggle ballTrailToggle;
        [SerializeField] private SettingToggle paddleVibrateToggle;
        [SerializeField] private SettingToggle slowMotionToggle;
        [SerializeField] private SettingSlider assistLevelSlider;

        [Header("游戏界面设置")]
        [SerializeField] private SettingToggle showUIToggle;
        [SerializeField] private SettingToggle showStatsToggle;
        [SerializeField] private SettingSlider uiScaleSlider;
        [SerializeField] private SettingToggle showScoreToggle;
        [SerializeField] private SettingToggle showTimerToggle;

        [Header("音效设置")]
        [SerializeField] private SettingToggle gameplaySoundsToggle;
        [SerializeField] private SettingToggle commentaryToggle;
        [SerializeField] private SettingSlider commentaryVolumeSlider;

        [Header("比赛设置")]
        [SerializeField] private SettingDropdown matchTypeDropdown;
        [SerializeField] private SettingSlider matchDurationSlider;
        [SerializeField] private SettingSlider scoreToWinSlider;
        [SerializeField] private SettingToggle suddenDeathToggle;

        private SettingsManager settingsManager;
        private VRHapticFeedback hapticFeedback;

        private void Awake()
        {
            settingsManager = SettingsManager.Instance;
            hapticFeedback = FindObjectOfType<VRHapticFeedback>();
        }

        private void Start()
        {
            SetupComponents();
            RefreshPanel();
        }

        private void SetupComponents()
        {
            // 难度设置事件
            difficultyDropdown?.OnValueChanged.AddListener(OnDifficultyChanged);
            aiDifficultySlider?.OnValueChanged.AddListener(OnAIDifficultyChanged);
            gameSpeedSlider?.OnValueChanged.AddListener(OnGameSpeedChanged);
            ballSpeedSlider?.OnValueChanged.AddListener(OnBallSpeedChanged);

            // 辅助功能事件
            autoAimToggle?.OnValueChanged.AddListener(OnAutoAimChanged);
            ballTrailToggle?.OnValueChanged.AddListener(OnBallTrailChanged);
            paddleVibrateToggle?.OnValueChanged.AddListener(OnPaddleVibrateChanged);
            slowMotionToggle?.OnValueChanged.AddListener(OnSlowMotionChanged);
            assistLevelSlider?.OnValueChanged.AddListener(OnAssistLevelChanged);

            // UI设置事件
            showUIToggle?.OnValueChanged.AddListener(OnShowUIChanged);
            showStatsToggle?.OnValueChanged.AddListener(OnShowStatsChanged);
            uiScaleSlider?.OnValueChanged.AddListener(OnUIScaleChanged);
            showScoreToggle?.OnValueChanged.AddListener(OnShowScoreChanged);
            showTimerToggle?.OnValueChanged.AddListener(OnShowTimerChanged);

            // 音效设置事件
            gameplaySoundsToggle?.OnValueChanged.AddListener(OnGameplaySoundsChanged);
            commentaryToggle?.OnValueChanged.AddListener(OnCommentaryChanged);
            commentaryVolumeSlider?.OnValueChanged.AddListener(OnCommentaryVolumeChanged);

            // 比赛设置事件
            matchTypeDropdown?.OnValueChanged.AddListener(OnMatchTypeChanged);
            matchDurationSlider?.OnValueChanged.AddListener(OnMatchDurationChanged);
            scoreToWinSlider?.OnValueChanged.AddListener(OnScoreToWinChanged);
            suddenDeathToggle?.OnValueChanged.AddListener(OnSuddenDeathChanged);
        }

        #region 事件处理

        private void OnDifficultyChanged(int difficulty)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.difficulty = (GameDifficulty)difficulty;
                settingsManager.UpdateGameplaySettings(gameplaySettings);

                // 根据难度自动调整相关设置
                ApplyDifficultyPreset((GameDifficulty)difficulty);
            }
        }

        private void OnAIDifficultyChanged(float difficulty)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.aiDifficulty = difficulty;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnGameSpeedChanged(float speed)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.gameSpeed = speed;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnBallSpeedChanged(float speed)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.ballSpeed = speed;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnAutoAimChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.autoAim = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnBallTrailChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.ballTrail = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnPaddleVibrateChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.paddleVibrate = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnSlowMotionChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.slowMotion = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnAssistLevelChanged(float level)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.assistLevel = level;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnShowUIChanged(bool show)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.showUI = show;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnShowStatsChanged(bool show)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.showStats = show;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnUIScaleChanged(float scale)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.uiScale = scale;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnShowScoreChanged(bool show)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.showScore = show;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnShowTimerChanged(bool show)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.showTimer = show;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnGameplaySoundsChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.gameplaySounds = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnCommentaryChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.commentary = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnCommentaryVolumeChanged(float volume)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.commentaryVolume = volume;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnMatchTypeChanged(int type)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.matchType = (MatchType)type;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnMatchDurationChanged(float duration)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.matchDuration = duration;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnScoreToWinChanged(float score)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.scoreToWin = (int)score;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        private void OnSuddenDeathChanged(bool enabled)
        {
            if (settingsManager != null)
            {
                var gameplaySettings = settingsManager.GetGameplaySettings();
                gameplaySettings.suddenDeath = enabled;
                settingsManager.UpdateGameplaySettings(gameplaySettings);
            }
        }

        #endregion

        #region 设置应用

        /// <summary>
        /// 应用难度预设
        /// </summary>
        private void ApplyDifficultyPreset(GameDifficulty difficulty)
        {
            var gameplaySettings = settingsManager.GetGameplaySettings();

            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    gameplaySettings.aiDifficulty = 0.3f;
                    gameplaySettings.gameSpeed = 0.8f;
                    gameplaySettings.ballSpeed = 0.7f;
                    gameplaySettings.autoAim = true;
                    gameplaySettings.assistLevel = 0.7f;
                    break;

                case GameDifficulty.Normal:
                    gameplaySettings.aiDifficulty = 0.5f;
                    gameplaySettings.gameSpeed = 1.0f;
                    gameplaySettings.ballSpeed = 1.0f;
                    gameplaySettings.autoAim = false;
                    gameplaySettings.assistLevel = 0.3f;
                    break;

                case GameDifficulty.Hard:
                    gameplaySettings.aiDifficulty = 0.7f;
                    gameplaySettings.gameSpeed = 1.2f;
                    gameplaySettings.ballSpeed = 1.3f;
                    gameplaySettings.autoAim = false;
                    gameplaySettings.assistLevel = 0.1f;
                    break;

                case GameDifficulty.Expert:
                    gameplaySettings.aiDifficulty = 0.9f;
                    gameplaySettings.gameSpeed = 1.5f;
                    gameplaySettings.ballSpeed = 1.5f;
                    gameplaySettings.autoAim = false;
                    gameplaySettings.assistLevel = 0.0f;
                    break;
            }

            settingsManager.UpdateGameplaySettings(gameplaySettings);
            RefreshPanel();

            // 触觉反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
            }
        }

        #endregion

        public void RefreshPanel()
        {
            if (settingsManager == null) return;

            var gameplaySettings = settingsManager.GetGameplaySettings();

            // 更新难度设置
            difficultyDropdown?.SetValue((int)gameplaySettings.difficulty);
            aiDifficultySlider?.SetValue(gameplaySettings.aiDifficulty);
            gameSpeedSlider?.SetValue(gameplaySettings.gameSpeed);
            ballSpeedSlider?.SetValue(gameplaySettings.ballSpeed);

            // 更新辅助功能
            autoAimToggle?.SetValue(gameplaySettings.autoAim);
            ballTrailToggle?.SetValue(gameplaySettings.ballTrail);
            paddleVibrateToggle?.SetValue(gameplaySettings.paddleVibrate);
            slowMotionToggle?.SetValue(gameplaySettings.slowMotion);
            assistLevelSlider?.SetValue(gameplaySettings.assistLevel);

            // 更新UI设置
            showUIToggle?.SetValue(gameplaySettings.showUI);
            showStatsToggle?.SetValue(gameplaySettings.showStats);
            uiScaleSlider?.SetValue(gameplaySettings.uiScale);
            showScoreToggle?.SetValue(gameplaySettings.showScore);
            showTimerToggle?.SetValue(gameplaySettings.showTimer);

            // 更新音效设置
            gameplaySoundsToggle?.SetValue(gameplaySettings.gameplaySounds);
            commentaryToggle?.SetValue(gameplaySettings.commentary);
            commentaryVolumeSlider?.SetValue(gameplaySettings.commentaryVolume);

            // 更新比赛设置
            matchTypeDropdown?.SetValue((int)gameplaySettings.matchType);
            matchDurationSlider?.SetValue(gameplaySettings.matchDuration);
            scoreToWinSlider?.SetValue(gameplaySettings.scoreToWin);
            suddenDeathToggle?.SetValue(gameplaySettings.suddenDeath);
        }

        private void OnDestroy()
        {
            // 清理事件监听
            difficultyDropdown?.OnValueChanged.RemoveAllListeners();
            aiDifficultySlider?.OnValueChanged.RemoveAllListeners();
            // ... 其他清理
        }
    }
}