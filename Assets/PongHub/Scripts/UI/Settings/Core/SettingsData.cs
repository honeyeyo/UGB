using System;
using System.Collections.Generic;
using UnityEngine;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 设置系统数据结构
    /// Settings system data structures for VR table tennis game
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        [Header("设置版本")]
        public string version = "1.0.0";

        [Header("各类设置")]
        public AudioSettings audioSettings = new AudioSettings();
        public VideoSettings videoSettings = new VideoSettings();
        public ControlSettings controlSettings = new ControlSettings();
        public GameplaySettings gameplaySettings = new GameplaySettings();
        public UserProfile userProfile = new UserProfile();

        /// <summary>
        /// 重置为默认设置
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
        /// 验证所有设置数据的有效性
        /// </summary>
        public bool ValidateAll()
        {
            return audioSettings.IsValid() &&
                   videoSettings.IsValid() &&
                   controlSettings.IsValid() &&
                   gameplaySettings.IsValid() &&
                   userProfile.IsValid();
        }
    }

    #region 音频设置数据

    [Serializable]
    public class AudioSettings
    {
        [Header("音量控制")]
        [Range(0f, 1f)]
        public float masterVolume = 1.0f;

        [Range(0f, 1f)]
        public float musicVolume = 0.8f;

        [Range(0f, 1f)]
        public float sfxVolume = 0.9f;

        [Range(0f, 1f)]
        public float voiceVolume = 1.0f;

        [Header("语音聊天")]
        public bool voiceChatEnabled = true;
        public float microphoneVolume = 1.0f;
        public bool microphoneNoiseGate = true;

        [Header("音频设备")]
        public string outputDevice = "Default";
        public string inputDevice = "Default";

        [Header("音频质量")]
        public AudioQuality audioQuality = AudioQuality.High;
        public bool enable3DAudio = true;
        public bool enableAudioOcclusion = true;

        public bool IsValid()
        {
            return masterVolume >= 0f && masterVolume <= 1f &&
                   musicVolume >= 0f && musicVolume <= 1f &&
                   sfxVolume >= 0f && sfxVolume <= 1f &&
                   voiceVolume >= 0f && voiceVolume <= 1f &&
                   microphoneVolume >= 0f && microphoneVolume <= 1f;
        }
    }

    #endregion

    #region 视频设置数据

    [Serializable]
    public class VideoSettings
    {
        [Header("画质设置")]
        public QualityLevel qualityLevel = QualityLevel.High;
        public ResolutionSetting resolution = ResolutionSetting.Auto;
        public FrameRateLimit frameRateLimit = FrameRateLimit.FPS_90;

        [Header("渲染设置")]
        public AntiAliasingLevel antiAliasing = AntiAliasingLevel.MSAA_4x;
        public ShadowQualityLevel shadowQuality = ShadowQualityLevel.High;
        public bool postProcessing = true;
        public bool vsync = false;

        [Header("VR特殊设置")]
        [Range(0.5f, 2.0f)]
        public float renderScale = 1.0f;
        public bool foveatedRendering = true;
        public bool fixedFoveatedRendering = false;

        [Header("舒适度设置")]
        public VRComfortLevel comfortLevel = VRComfortLevel.Medium;
        public bool vignetting = true;
        public bool motionSicknessReduction = true;

        [Header("显示设置")]
        [Range(0.5f, 1.5f)]
        public float brightness = 1.0f;
        [Range(0.5f, 1.5f)]
        public float contrast = 1.0f;

        public bool IsValid()
        {
            return renderScale >= 0.5f && renderScale <= 2.0f &&
                   brightness >= 0.5f && brightness <= 1.5f &&
                   contrast >= 0.5f && contrast <= 1.5f;
        }
    }

    #endregion

    #region 控制设置数据

    [Serializable]
    public class ControlSettings
    {
        [Header("手部设置")]
        public DominantHand dominantHand = DominantHand.Right;

        [Header("移动设置")]
        public MovementType movementType = MovementType.Teleport;
        [Range(0.1f, 3.0f)]
        public float movementSpeed = 1.0f;
        [Range(0.1f, 3.0f)]
        public float turnSpeed = 1.0f;

        [Header("交互设置")]
        public InteractionMode interactionMode = InteractionMode.RayInteraction;
        [Range(0.1f, 5.0f)]
        public float grabDistance = 1.0f;
        [Range(0.1f, 10.0f)]
        public float pointerDistance = 5.0f;
        public bool autoGrab = false;

        [Header("灵敏度设置")]
        [Range(0.1f, 3.0f)]
        public float lookSensitivity = 1.0f;
        [Range(0.1f, 3.0f)]
        public float moveSensitivity = 1.0f;

        [Header("触觉反馈")]
        public bool hapticFeedback = true;
        [Range(0f, 1f)]
        public float hapticIntensity = 1.0f;

        [Header("手势识别")]
        public bool gestureRecognition = true;
        public bool handTracking = true;

        [Header("瞬移转向")]
        public bool snapTurn = true;
        [Range(15f, 90f)]
        public float snapTurnAngle = 30f;

        [Header("边界设置")]
        public bool showBoundaries = true;
        public bool hapticsOnBoundary = true;

        public bool IsValid()
        {
            return movementSpeed >= 0.1f && movementSpeed <= 3.0f &&
                   turnSpeed >= 0.1f && turnSpeed <= 3.0f &&
                   lookSensitivity >= 0.1f && lookSensitivity <= 3.0f &&
                   moveSensitivity >= 0.1f && moveSensitivity <= 3.0f &&
                   hapticIntensity >= 0f && hapticIntensity <= 1f &&
                   grabDistance >= 0.1f && grabDistance <= 5.0f &&
                   pointerDistance >= 0.1f && pointerDistance <= 10.0f &&
                   snapTurnAngle >= 15f && snapTurnAngle <= 90f;
        }
    }

    #endregion

    #region 游戏玩法设置数据

    [Serializable]
    public class GameplaySettings
    {
        [Header("AI难度")]
        public GameDifficulty gameDifficulty = GameDifficulty.Normal;
        [Range(0f, 1f)]
        public float aiDifficulty = 0.5f;

        [Header("比赛设置")]
        public MatchDuration matchDuration = MatchDuration.Standard;
        public ScoreLimit scoreLimit = ScoreLimit.FirstTo11;
        public bool suddenDeath = false;

        [Header("物理设置")]
        [Range(0.5f, 2.0f)]
        public float ballSpeed = 1.0f;
        [Range(0.5f, 1.5f)]
        public float paddleSize = 1.0f;

        [Header("辅助功能")]
        public bool aimAssist = false;
        [Range(0f, 1f)]
        public float assistLevel = 0.3f;
        public bool trajectoryPrediction = false;
        public bool slowMotionMode = false;

        [Header("UI设置")]
        public bool showScore = true;
        public bool showTimer = true;
        public bool showStatistics = true;
        [Range(0.5f, 2.0f)]
        public float uiScale = 1.0f;

        [Header("通知设置")]
        public bool achievementNotifications = true;
        public bool matchInviteNotifications = true;

        public bool IsValid()
        {
            return aiDifficulty >= 0f && aiDifficulty <= 1f &&
                   ballSpeed >= 0.5f && ballSpeed <= 2.0f &&
                   paddleSize >= 0.5f && paddleSize <= 1.5f &&
                   assistLevel >= 0f && assistLevel <= 1f &&
                   uiScale >= 0.5f && uiScale <= 2.0f;
        }
    }

    #endregion

    #region 用户配置文件数据

    [Serializable]
    public class UserProfile
    {
        [Header("基本信息")]
        public string username = "Player";
        public string avatarId = "default";
        public Language language = Language.Chinese;

        [Header("统计数据")]
        public int gamesPlayed = 0;
        public int gamesWon = 0;
        public int totalScore = 0;
        public float totalPlayTime = 0f; // 小时
        public DateTime lastPlayed = DateTime.Now;

        [Header("偏好设置")]
        public UserTheme theme = UserTheme.Default;
        public bool showOnlineStatus = true;
        public bool allowFriendRequests = true;
        public bool shareStatistics = true;

        [Header("隐私设置")]
        public PrivacyLevel privacyLevel = PrivacyLevel.Friends;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(username) &&
                   username.Length >= 3 &&
                   username.Length <= 20 &&
                   gamesPlayed >= 0 &&
                   gamesWon >= 0 &&
                   gamesWon <= gamesPlayed &&
                   totalScore >= 0 &&
                   totalPlayTime >= 0f;
        }

        public float GetWinRate()
        {
            return gamesPlayed > 0 ? (float)gamesWon / gamesPlayed : 0f;
        }
    }

    #endregion

    #region 枚举定义

    // 音频相关枚举
    public enum AudioQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    // 视频相关枚举
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum ResolutionSetting
    {
        Auto = 0,
        Low_1280x720 = 1,
        Medium_1920x1080 = 2,
        High_2560x1440 = 3,
        Ultra_3840x2160 = 4
    }

    public enum FrameRateLimit
    {
        FPS_60 = 60,
        FPS_72 = 72,
        FPS_90 = 90,
        FPS_120 = 120,
        Unlimited = 0
    }

    public enum AntiAliasingLevel
    {
        None = 0,
        MSAA_2x = 1,
        MSAA_4x = 2,
        MSAA_8x = 3
    }

    public enum ShadowQualityLevel
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum VRComfortLevel
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    // 控制相关枚举
    public enum DominantHand
    {
        Left = 0,
        Right = 1
    }

    public enum MovementType
    {
        Teleport = 0,
        SmoothLocomotion = 1,
        Hybrid = 2
    }

    public enum InteractionMode
    {
        RayInteraction = 0,
        DirectTouch = 1,
        GazeAndSelect = 2
    }

    // 游戏玩法相关枚举
    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    public enum MatchDuration
    {
        Quick_5Min = 0,
        Standard_10Min = 1,
        Extended_15Min = 2,
        Marathon_30Min = 3
    }

    public enum ScoreLimit
    {
        FirstTo5 = 5,
        FirstTo11 = 11,
        FirstTo21 = 21,
        NoLimit = 0
    }

    // 用户相关枚举
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

    public enum UserTheme
    {
        Default = 0,
        Dark = 1,
        Light = 2,
        HighContrast = 3
    }

    public enum PrivacyLevel
    {
        Public = 0,
        Friends = 1,
        Private = 2
    }

    #endregion
}