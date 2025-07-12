using UnityEngine;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 设置验证器 - 验证设置数据的有效性
    /// Settings Validator - Validates settings data integrity
    /// </summary>
    public class SettingsValidator : MonoBehaviour
    {
        [Header("验证配置")]
        [SerializeField]
        [Tooltip("启用详细日志")]
        private bool enableVerboseLogging = false;

        /// <summary>
        /// 验证完整的游戏设置
        /// </summary>
        public bool ValidateGameSettings(GameSettings settings)
        {
            if (settings == null)
            {
                LogValidationError("GameSettings is null");
                return false;
            }

            bool isValid = ValidateAudioSettings(settings.audioSettings) &&
                          ValidateVideoSettings(settings.videoSettings) &&
                          ValidateControlSettings(settings.controlSettings) &&
                          ValidateGameplaySettings(settings.gameplaySettings) &&
                          ValidateUserProfile(settings.userProfile);

            if (enableVerboseLogging)
            {
                Debug.Log($"GameSettings validation result: {isValid}");
            }

            return isValid;
        }

        /// <summary>
        /// 验证音频设置
        /// </summary>
        public bool ValidateAudioSettings(AudioSettings settings)
        {
            if (settings == null)
            {
                LogValidationError("AudioSettings is null");
                return false;
            }

            // 验证音量范围
            if (!IsInRange(settings.masterVolume, 0f, 1f, "Master Volume"))
                return false;
            if (!IsInRange(settings.musicVolume, 0f, 1f, "Music Volume"))
                return false;
            if (!IsInRange(settings.sfxVolume, 0f, 1f, "SFX Volume"))
                return false;
            if (!IsInRange(settings.voiceVolume, 0f, 1f, "Voice Volume"))
                return false;

            // 验证音频范围
            if (!IsInRange(settings.audioRange, 0f, 10f, "Audio Range"))
                return false;

            // 验证枚举值
            if (!IsValidEnum<AudioQuality>(settings.audioQuality, "Audio Quality"))
                return false;

            // 验证音频设备名称
            if (string.IsNullOrEmpty(settings.audioDevice))
            {
                LogValidationError("Audio device name cannot be empty");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证视频设置
        /// </summary>
        public bool ValidateVideoSettings(VideoSettings settings)
        {
            if (settings == null)
            {
                LogValidationError("VideoSettings is null");
                return false;
            }

            // 验证渲染缩放
            if (!IsInRange(settings.renderScale, 0.5f, 2.0f, "Render Scale"))
                return false;

            // 验证目标帧率
            if (!IsInRange(settings.targetFrameRate, 72, 120, "Target Frame Rate"))
                return false;

            // 验证枚举值
            if (!IsValidEnum<RenderQuality>(settings.renderQuality, "Render Quality"))
                return false;
            if (!IsValidEnum<AntiAliasing>(settings.antiAliasing, "Anti Aliasing"))
                return false;
            if (!IsValidEnum<ShadowQualityLevel>(settings.shadowQuality, "Shadow Quality"))
                return false;

            // 验证舒适设置
            if (!ValidateComfortSettings(settings.comfortSettings))
                return false;

            return true;
        }

        /// <summary>
        /// 验证舒适设置
        /// </summary>
        public bool ValidateComfortSettings(ComfortSettings settings)
        {
            if (settings == null)
            {
                LogValidationError("ComfortSettings is null");
                return false;
            }

            // 验证舒适度级别
            if (!IsInRange(settings.comfortLevel, 0f, 1f, "Comfort Level"))
                return false;

            // 验证瞬移角度
            if (!IsInRange(settings.snapTurnAngle, 15f, 90f, "Snap Turn Angle"))
                return false;

            return true;
        }

        /// <summary>
        /// 验证控制设置
        /// </summary>
        public bool ValidateControlSettings(ControlSettings settings)
        {
            if (settings == null)
            {
                LogValidationError("ControlSettings is null");
                return false;
            }

            // 验证灵敏度
            if (!IsInRange(settings.mouseSensitivity, 0.1f, 3.0f, "Mouse Sensitivity"))
                return false;
            if (!IsInRange(settings.vrControllerSensitivity, 0.1f, 3.0f, "VR Controller Sensitivity"))
                return false;

            // 验证死区
            if (!IsInRange(settings.deadZone, 0f, 0.5f, "Dead Zone"))
                return false;

            // 验证触觉反馈强度
            if (!IsInRange(settings.hapticIntensity, 0f, 1f, "Haptic Intensity"))
                return false;

            // 验证手部跟踪精度
            if (!IsInRange(settings.handTrackingAccuracy, 0.1f, 2.0f, "Hand Tracking Accuracy"))
                return false;

            // 验证枚举值
            if (!IsValidEnum<ControlScheme>(settings.controlScheme, "Control Scheme"))
                return false;
            if (!IsValidEnum<HandPreference>(settings.dominantHand, "Dominant Hand"))
                return false;

            // 验证按键绑定
            if (settings.keyBindings == null)
            {
                LogValidationError("Key bindings dictionary is null");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证游戏设置
        /// </summary>
        public bool ValidateGameplaySettings(GameplaySettings settings)
        {
            if (settings == null)
            {
                LogValidationError("GameplaySettings is null");
                return false;
            }

            // 验证UI缩放
            if (!IsInRange(settings.uiScale, 0.5f, 2.0f, "UI Scale"))
                return false;

            // 验证枚举值
            if (!IsValidEnum<DifficultyLevel>(settings.defaultDifficulty, "Default Difficulty"))
                return false;
            if (!IsValidEnum<LanguageCode>(settings.language, "Language"))
                return false;

            return true;
        }

        /// <summary>
        /// 验证用户资料
        /// </summary>
        public bool ValidateUserProfile(UserProfile profile)
        {
            if (profile == null)
            {
                LogValidationError("UserProfile is null");
                return false;
            }

            // 验证玩家姓名
            if (string.IsNullOrEmpty(profile.playerName))
            {
                LogValidationError("Player name cannot be empty");
                return false;
            }

            if (profile.playerName.Length > 50)
            {
                LogValidationError("Player name is too long (max 50 characters)");
                return false;
            }

            // 验证身高
            if (!IsInRange(profile.heightCm, 120f, 220f, "Height"))
                return false;

            // 验证统计数据
            if (profile.totalPlayTime < 0)
            {
                LogValidationError("Total play time cannot be negative");
                return false;
            }

            if (profile.totalMatches < 0)
            {
                LogValidationError("Total matches cannot be negative");
                return false;
            }

            if (profile.wins < 0 || profile.wins > profile.totalMatches)
            {
                LogValidationError("Wins must be between 0 and total matches");
                return false;
            }

            // 验证枚举值
            if (!IsValidEnum<HandPreference>(profile.handPreference, "Hand Preference"))
                return false;
            if (!IsValidEnum<ExperienceLevel>(profile.experience, "Experience Level"))
                return false;

            // 验证成就列表
            if (profile.achievements == null)
            {
                LogValidationError("Achievements list is null");
                return false;
            }

            // 验证偏好颜色
            if (string.IsNullOrEmpty(profile.preferredPaddleColor))
            {
                LogValidationError("Preferred paddle color cannot be empty");
                return false;
            }

            return true;
        }

        #region 验证辅助方法

        /// <summary>
        /// 验证数值是否在指定范围内
        /// </summary>
        private bool IsInRange(float value, float min, float max, string fieldName)
        {
            if (value < min || value > max)
            {
                LogValidationError($"{fieldName} ({value}) is out of range [{min}, {max}]");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证数值是否在指定范围内（整数）
        /// </summary>
        private bool IsInRange(int value, int min, int max, string fieldName)
        {
            if (value < min || value > max)
            {
                LogValidationError($"{fieldName} ({value}) is out of range [{min}, {max}]");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证枚举值是否有效
        /// </summary>
        private bool IsValidEnum<T>(T value, string fieldName) where T : System.Enum
        {
            if (!System.Enum.IsDefined(typeof(T), value))
            {
                LogValidationError($"{fieldName} ({value}) is not a valid enum value");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 记录验证错误
        /// </summary>
        private void LogValidationError(string message)
        {
            Debug.LogError($"[SettingsValidator] {message}");
        }

        #endregion

        #region 设置修复

        /// <summary>
        /// 修复无效设置为有效值
        /// </summary>
        public GameSettings FixInvalidSettings(GameSettings settings)
        {
            if (settings == null)
            {
                return new GameSettings();
            }

            var fixedSettings = new GameSettings();

            // 修复音频设置
            fixedSettings.audioSettings = FixAudioSettings(settings.audioSettings);

            // 修复视频设置
            fixedSettings.videoSettings = FixVideoSettings(settings.videoSettings);

            // 修复控制设置
            fixedSettings.controlSettings = FixControlSettings(settings.controlSettings);

            // 修复游戏设置
            fixedSettings.gameplaySettings = FixGameplaySettings(settings.gameplaySettings);

            // 修复用户资料
            fixedSettings.userProfile = FixUserProfile(settings.userProfile);

            return fixedSettings;
        }

        /// <summary>
        /// 修复音频设置
        /// </summary>
        private AudioSettings FixAudioSettings(AudioSettings settings)
        {
            if (settings == null) return new AudioSettings();

            settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
            settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
            settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
            settings.voiceVolume = Mathf.Clamp01(settings.voiceVolume);
            settings.audioRange = Mathf.Clamp(settings.audioRange, 0f, 10f);

            if (string.IsNullOrEmpty(settings.audioDevice))
                settings.audioDevice = "Default";

            return settings;
        }

        /// <summary>
        /// 修复视频设置
        /// </summary>
        private VideoSettings FixVideoSettings(VideoSettings settings)
        {
            if (settings == null) return new VideoSettings();

            settings.renderScale = Mathf.Clamp(settings.renderScale, 0.5f, 2.0f);
            settings.targetFrameRate = Mathf.Clamp(settings.targetFrameRate, 72, 120);

            if (settings.comfortSettings == null)
                settings.comfortSettings = new ComfortSettings();

            settings.comfortSettings.comfortLevel = Mathf.Clamp01(settings.comfortSettings.comfortLevel);
            settings.comfortSettings.snapTurnAngle = Mathf.Clamp(settings.comfortSettings.snapTurnAngle, 15f, 90f);

            return settings;
        }

        /// <summary>
        /// 修复控制设置
        /// </summary>
        private ControlSettings FixControlSettings(ControlSettings settings)
        {
            if (settings == null) return new ControlSettings();

            settings.mouseSensitivity = Mathf.Clamp(settings.mouseSensitivity, 0.1f, 3.0f);
            settings.vrControllerSensitivity = Mathf.Clamp(settings.vrControllerSensitivity, 0.1f, 3.0f);
            settings.deadZone = Mathf.Clamp(settings.deadZone, 0f, 0.5f);
            settings.hapticIntensity = Mathf.Clamp01(settings.hapticIntensity);
            settings.handTrackingAccuracy = Mathf.Clamp(settings.handTrackingAccuracy, 0.1f, 2.0f);

            if (settings.keyBindings == null)
                settings.keyBindings = new System.Collections.Generic.Dictionary<string, KeyCode>();

            return settings;
        }

        /// <summary>
        /// 修复游戏设置
        /// </summary>
        private GameplaySettings FixGameplaySettings(GameplaySettings settings)
        {
            if (settings == null) return new GameplaySettings();

            settings.uiScale = Mathf.Clamp(settings.uiScale, 0.5f, 2.0f);

            return settings;
        }

        /// <summary>
        /// 修复用户资料
        /// </summary>
        private UserProfile FixUserProfile(UserProfile profile)
        {
            if (profile == null) return new UserProfile();

            if (string.IsNullOrEmpty(profile.playerName))
                profile.playerName = "Player";

            if (profile.playerName.Length > 50)
                profile.playerName = profile.playerName.Substring(0, 50);

            profile.heightCm = Mathf.Clamp(profile.heightCm, 120f, 220f);
            profile.totalPlayTime = Mathf.Max(0, profile.totalPlayTime);
            profile.totalMatches = Mathf.Max(0, profile.totalMatches);
            profile.wins = Mathf.Clamp(profile.wins, 0, profile.totalMatches);

            if (profile.achievements == null)
                profile.achievements = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(profile.preferredPaddleColor))
                profile.preferredPaddleColor = "Blue";

            return profile;
        }

        #endregion
    }
}