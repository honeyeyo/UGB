using System;
using System.Collections.Generic;
using UnityEngine;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 游戏设置主数据结构
    /// Main game settings data structure
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        [Header("音频设置")]
        public AudioSettings audioSettings = new AudioSettings();

        [Header("视频设置")]
        public VideoSettings videoSettings = new VideoSettings();

        [Header("控制设置")]
        public ControlSettings controlSettings = new ControlSettings();

        [Header("游戏设置")]
        public GameplaySettings gameplaySettings = new GameplaySettings();

        [Header("用户资料")]
        public UserProfile userProfile = new UserProfile();

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            audioSettings = new AudioSettings();
            videoSettings = new VideoSettings();
            controlSettings = new ControlSettings();
            gameplaySettings = new GameplaySettings();
            userProfile = new UserProfile();
        }

        /// <summary>
        /// 验证所有设置的有效性
        /// </summary>
        public bool ValidateSettings()
        {
            return audioSettings.Validate() &&
                   videoSettings.Validate() &&
                   controlSettings.Validate() &&
                   gameplaySettings.Validate() &&
                   userProfile.Validate();
        }
    }

    /// <summary>
    /// 音频设置
    /// </summary>
    [Serializable]
    public class AudioSettings
    {
        [Header("音量控制")]
        [Range(0f, 1f)]
        [Tooltip("主音量 (0-1)")]
        public float masterVolume = 1.0f;

        [Range(0f, 1f)]
        [Tooltip("音乐音量 (0-1)")]
        public float musicVolume = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("音效音量 (0-1)")]
        public float sfxVolume = 0.9f;

        [Range(0f, 1f)]
        [Tooltip("语音音量 (0-1)")]
        public float voiceVolume = 1.0f;

        [Header("音频选项")]
        [Tooltip("失去焦点时静音")]
        public bool muteOnFocusLoss = true;

        [Tooltip("音频设备")]
        public string audioDevice = "Default";

        [Tooltip("音频质量")]
        public AudioQuality audioQuality = AudioQuality.High;

        [Header("VR特殊设置")]
        [Tooltip("空间音频")]
        public bool spatialAudio = true;

        [Range(0f, 10f)]
        [Tooltip("音频传播距离")]
        public float audioRange = 5.0f;

        public bool Validate()
        {
            return masterVolume >= 0f && masterVolume <= 1f &&
                   musicVolume >= 0f && musicVolume <= 1f &&
                   sfxVolume >= 0f && sfxVolume <= 1f &&
                   voiceVolume >= 0f && voiceVolume <= 1f &&
                   audioRange >= 0f && audioRange <= 10f;
        }
    }

    /// <summary>
    /// 视频设置
    /// </summary>
    [Serializable]
    public class VideoSettings
    {
        [Header("渲染质量")]
        [Tooltip("整体渲染质量")]
        public RenderQuality renderQuality = RenderQuality.High;

        [Tooltip("抗锯齿")]
        public AntiAliasing antiAliasing = AntiAliasing.MSAA_4x;

        [Tooltip("阴影质量")]
        public ShadowQualityLevel shadowQuality = ShadowQualityLevel.High;

        [Tooltip("启用后处理")]
        public bool enablePostProcessing = true;

        [Tooltip("垂直同步")]
        public bool enableVSync = true;

        [Header("VR特殊设置")]
        [Range(0.5f, 2.0f)]
        [Tooltip("VR渲染缩放")]
        public float renderScale = 1.0f;

        [Tooltip("舒适设置")]
        public ComfortSettings comfortSettings = new ComfortSettings();

        [Tooltip("固定注视点渲染")]
        public bool foveatedRendering = true;

        [Range(72f, 144f)]
        [Tooltip("目标帧率")]
        public int targetFrameRate = 120;

        public bool Validate()
        {
            return renderScale >= 0.5f && renderScale <= 2.0f &&
                   targetFrameRate >= 72 && targetFrameRate <= 120 &&
                   comfortSettings.Validate();
        }
    }

    /// <summary>
    /// 舒适设置
    /// </summary>
    [Serializable]
    public class ComfortSettings
    {
        [Range(0f, 1f)]
        [Tooltip("视觉舒适度 - 减少眩晕")]
        public float comfortLevel = 0.5f;

        [Tooltip("启用瞬移移动")]
        public bool snapTurn = false;

        [Range(15f, 90f)]
        [Tooltip("瞬移角度")]
        public float snapTurnAngle = 30f;

        [Tooltip("启用晕动症缓解")]
        public bool motionSicknessReduction = true;

        public bool Validate()
        {
            return comfortLevel >= 0f && comfortLevel <= 1f &&
                   snapTurnAngle >= 15f && snapTurnAngle <= 90f;
        }
    }

    /// <summary>
    /// 控制设置
    /// </summary>
    [Serializable]
    public class ControlSettings
    {
        [Header("灵敏度设置")]
        [Range(0.1f, 3.0f)]
        [Tooltip("鼠标灵敏度")]
        public float mouseSensitivity = 1.0f;

        [Range(0.1f, 3.0f)]
        [Tooltip("VR控制器灵敏度")]
        public float vrControllerSensitivity = 1.0f;

        [Header("控制选项")]
        [Tooltip("反转Y轴")]
        public bool invertY = false;

        [Range(0f, 0.5f)]
        [Tooltip("死区")]
        public float deadZone = 0.1f;

        [Tooltip("启用触觉反馈")]
        public bool hapticFeedback = true;

        [Range(0f, 1f)]
        [Tooltip("触觉反馈强度")]
        public float hapticIntensity = 1.0f;

        [Header("控制方案")]
        [Tooltip("控制方案")]
        public ControlScheme controlScheme = ControlScheme.Default;

        [Tooltip("按键绑定")]
        public Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

        [Header("VR特殊设置")]
        [Tooltip("主手偏好")]
        public HandPreference dominantHand = HandPreference.Right;

        [Range(0.1f, 2.0f)]
        [Tooltip("手部跟踪精度")]
        public float handTrackingAccuracy = 1.0f;

        public bool Validate()
        {
            return mouseSensitivity >= 0.1f && mouseSensitivity <= 3.0f &&
                   vrControllerSensitivity >= 0.1f && vrControllerSensitivity <= 3.0f &&
                   deadZone >= 0f && deadZone <= 0.5f &&
                   hapticIntensity >= 0f && hapticIntensity <= 1f &&
                   handTrackingAccuracy >= 0.1f && handTrackingAccuracy <= 2.0f;
        }
    }

    /// <summary>
    /// 游戏玩法设置
    /// </summary>
    [Serializable]
    public class GameplaySettings
    {
        [Header("游戏难度")]
        [Tooltip("默认难度级别")]
        public DifficultyLevel defaultDifficulty = DifficultyLevel.Normal;

        [Header("游戏选项")]
        [Tooltip("自动保存")]
        public bool autoSave = true;

        [Tooltip("显示教程")]
        public bool showTutorials = true;

        [Tooltip("启用辅助模式")]
        public bool enableAssistMode = false;

        [Tooltip("显示统计信息")]
        public bool showStatistics = true;

        [Tooltip("启用调试信息")]
        public bool enableDebugInfo = false;

        [Header("本地化")]
        [Tooltip("游戏语言")]
        public LanguageCode language = LanguageCode.Chinese;

        [Header("辅助功能")]
        [Tooltip("高对比度模式")]
        public bool highContrast = false;

        [Range(0.5f, 2.0f)]
        [Tooltip("UI缩放")]
        public float uiScale = 1.0f;

        [Tooltip("启用字幕")]
        public bool enableSubtitles = false;

        public bool Validate()
        {
            return uiScale >= 0.5f && uiScale <= 2.0f;
        }
    }

    /// <summary>
    /// 用户资料
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        [Header("基本信息")]
        [Tooltip("玩家姓名")]
        public string playerName = "Player";

        [Range(120f, 220f)]
        [Tooltip("玩家身高（VR校准用）")]
        public float heightCm = 170f;

        [Tooltip("主手偏好")]
        public HandPreference handPreference = HandPreference.Right;

        [Tooltip("经验水平")]
        public ExperienceLevel experience = ExperienceLevel.Intermediate;

        [Header("统计数据")]
        [Tooltip("上次游戏时间")]
        public DateTime lastPlayed = DateTime.Now;

        [Tooltip("总游戏时间（分钟）")]
        public int totalPlayTime = 0;

        [Tooltip("总比赛场数")]
        public int totalMatches = 0;

        [Tooltip("获胜场数")]
        public int wins = 0;

        [Header("成就和偏好")]
        [Tooltip("已解锁成就")]
        public List<string> achievements = new List<string>();

        [Tooltip("偏好的球拍颜色")]
        public string preferredPaddleColor = "Blue";

        public bool Validate()
        {
            return !string.IsNullOrEmpty(playerName) &&
                   heightCm >= 120f && heightCm <= 220f &&
                   totalPlayTime >= 0 &&
                   totalMatches >= 0 &&
                   wins >= 0 && wins <= totalMatches;
        }

        /// <summary>
        /// 获取胜率
        /// </summary>
        public float GetWinRate()
        {
            return totalMatches > 0 ? (float)wins / totalMatches : 0f;
        }
    }

    #region 枚举定义

    /// <summary>
    /// 音频质量枚举
    /// </summary>
    public enum AudioQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// 渲染质量枚举
    /// </summary>
    public enum RenderQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// 抗锯齿枚举
    /// </summary>
    public enum AntiAliasing
    {
        None = 0,
        MSAA_2x = 1,
        MSAA_4x = 2,
        MSAA_8x = 3
    }

    /// <summary>
    /// 控制方案枚举
    /// </summary>
    public enum ControlScheme
    {
        Default = 0,
        LeftHanded = 1,
        Custom = 2,
        Accessibility = 3
    }

    /// <summary>
    /// 手偏好枚举
    /// </summary>
    public enum HandPreference
    {
        Left = 0,
        Right = 1,
        Ambidextrous = 2
    }

    /// <summary>
    /// 难度级别枚举
    /// </summary>
    public enum DifficultyLevel
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    /// <summary>
    /// 经验水平枚举
    /// </summary>
    public enum ExperienceLevel
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2,
        Expert = 3
    }

    /// <summary>
    /// 语言代码枚举
    /// </summary>
    public enum LanguageCode
    {
        English = 0,
        Chinese = 1,
        Japanese = 2,
        Korean = 3,
        French = 4,
        German = 5,
        Spanish = 6
    }

    /// <summary>
    /// 分辨率设置枚举
    /// </summary>
    public enum ResolutionSetting
    {
        Auto = 0,
        Low_1280x720 = 1,
        Medium_1920x1080 = 2,
        High_2560x1440 = 3,
        Ultra_3840x2160 = 4
    }

    /// <summary>
    /// 帧率限制枚举
    /// </summary>
    public enum FrameRateLimit
    {
        FPS_60 = 60,
        FPS_72 = 72,
        FPS_90 = 90,
        FPS_120 = 120,
        FPS_144 = 144,
        Unlimited = 0
    }

    /// <summary>
    /// 抗锯齿级别枚举
    /// </summary>
    public enum AntiAliasingLevel
    {
        None = 0,
        MSAA_2x = 1,
        MSAA_4x = 2,
        MSAA_8x = 3
    }

    /// <summary>
    /// 阴影质量级别枚举
    /// </summary>
    public enum ShadowQualityLevel
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    /// <summary>
    /// VR舒适度级别枚举
    /// </summary>
    public enum VRComfortLevel
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    /// <summary>
    /// 主手偏好枚举
    /// </summary>
    public enum DominantHand
    {
        Left = 0,
        Right = 1
    }

    /// <summary>
    /// 移动类型枚举
    /// </summary>
    public enum MovementType
    {
        Teleport = 0,
        SmoothLocomotion = 1,
        Hybrid = 2
    }

    /// <summary>
    /// 交互模式枚举
    /// </summary>
    public enum InteractionMode
    {
        RayInteraction = 0,
        DirectTouch = 1,
        GazeAndSelect = 2
    }

    /// <summary>
    /// 游戏难度枚举
    /// </summary>
    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    /// <summary>
    /// 比赛时长枚举
    /// </summary>
    public enum MatchDuration
    {
        Quick_5Min = 0,
        Standard_10Min = 1,
        Extended_15Min = 2,
        Marathon_30Min = 3
    }

    /// <summary>
    /// 分数限制枚举
    /// </summary>
    public enum ScoreLimit
    {
        FirstTo5 = 5,
        FirstTo11 = 11,
        FirstTo21 = 21,
        NoLimit = 0
    }

    /// <summary>
    /// 语言枚举
    /// </summary>
    public enum Language
    {
        English = 0,
        Chinese = 1,
        Japanese = 2,
        Korean = 3,
        French = 4,
        German = 5,
        Spanish = 6
    }

    /// <summary>
    /// 比赛类型枚举
    /// </summary>
    public enum MatchType
    {
        Practice = 0,
        Quick = 1,
        Ranked = 2,
        Tournament = 3
    }

    /// <summary>
    /// 扳机映射枚举
    /// </summary>
    public enum TriggerMapping
    {
        None = 0,
        Grab = 1,
        Shoot = 2,
        Menu = 3
    }

    /// <summary>
    /// 用户主题枚举
    /// </summary>
    public enum UserTheme
    {
        Default = 0,
        Dark = 1,
        Light = 2,
        HighContrast = 3
    }

    /// <summary>
    /// 隐私级别枚举
    /// </summary>
    public enum PrivacyLevel
    {
        Public = 0,
        Friends = 1,
        Private = 2
    }

    #endregion
}