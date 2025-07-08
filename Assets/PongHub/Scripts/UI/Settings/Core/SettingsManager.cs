using System;
using System.Threading.Tasks;
using UnityEngine;
using PongHub.Core;
using PongHub.Core.Audio;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 设置管理器 - 管理游戏设置的核心类
    /// Settings Manager - Core class for managing game settings
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField]
        [Tooltip("设置文件名")]
        private string settingsFileName = "GameSettings.json";

        [SerializeField]
        [Tooltip("是否启用自动保存")]
        private bool enableAutoSave = true;

        [SerializeField]
        [Tooltip("自动保存间隔（秒）")]
        private float autoSaveInterval = 30f;

        // 单例实例
        public static SettingsManager Instance { get; private set; }

        // 当前设置
        private GameSettings currentSettings;

        // 组件引用
        private SettingsValidator validator;
        private SettingsPersistence persistence;

        // 事件
        public static event Action<GameSettings> OnSettingsLoaded;
        public static event Action<GameSettings> OnSettingsChanged;
        public static event Action<AudioSettings> OnAudioSettingsChanged;
        public static event Action<VideoSettings> OnVideoSettingsChanged;
        public static event Action<ControlSettings> OnControlSettingsChanged;
        public static event Action<GameplaySettings> OnGameplaySettingsChanged;
        public static event Action<UserProfile> OnUserProfileChanged;

        // 自动保存状态
        private float autoSaveTimer;
        private bool hasUnsavedChanges;

        #region Unity 生命周期

        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComponents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            await LoadSettingsAsync();
            ApplyAllSettings();
        }

        private void Update()
        {
            // 自动保存逻辑
            if (enableAutoSave && hasUnsavedChanges)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    _ = SaveSettingsAsync();
                    autoSaveTimer = 0f;
                }
            }
        }

        #endregion

        #region 初始化

        private void InitializeComponents()
        {
            // 初始化验证器
            validator = gameObject.AddComponent<SettingsValidator>();

            // 初始化持久化管理器
            persistence = gameObject.AddComponent<SettingsPersistence>();
            persistence.Initialize(settingsFileName);

            // 创建默认设置
            currentSettings = new GameSettings();
        }

        #endregion

        #region 设置访问

        /// <summary>
        /// 获取当前设置
        /// </summary>
        public GameSettings GetCurrentSettings()
        {
            return currentSettings;
        }

        /// <summary>
        /// 获取音频设置
        /// </summary>
        public AudioSettings GetAudioSettings()
        {
            return currentSettings.audioSettings;
        }

        /// <summary>
        /// 获取视频设置
        /// </summary>
        public VideoSettings GetVideoSettings()
        {
            return currentSettings.videoSettings;
        }

        /// <summary>
        /// 获取控制设置
        /// </summary>
        public ControlSettings GetControlSettings()
        {
            return currentSettings.controlSettings;
        }

        /// <summary>
        /// 获取游戏设置
        /// </summary>
        public GameplaySettings GetGameplaySettings()
        {
            return currentSettings.gameplaySettings;
        }

        /// <summary>
        /// 获取用户资料
        /// </summary>
        public UserProfile GetUserProfile()
        {
            return currentSettings.userProfile;
        }

        #endregion

        #region 设置修改

        /// <summary>
        /// 更新音频设置
        /// </summary>
        public void UpdateAudioSettings(AudioSettings newSettings)
        {
            if (validator.ValidateAudioSettings(newSettings))
            {
                currentSettings.audioSettings = newSettings;
                ApplyAudioSettings();
                OnAudioSettingsChanged?.Invoke(newSettings);
                OnSettingsChanged?.Invoke(currentSettings);
                MarkAsChanged();
            }
            else
            {
                Debug.LogError("Invalid audio settings provided");
            }
        }

        /// <summary>
        /// 更新视频设置
        /// </summary>
        public void UpdateVideoSettings(VideoSettings newSettings)
        {
            if (validator.ValidateVideoSettings(newSettings))
            {
                currentSettings.videoSettings = newSettings;
                ApplyVideoSettings();
                OnVideoSettingsChanged?.Invoke(newSettings);
                OnSettingsChanged?.Invoke(currentSettings);
                MarkAsChanged();
            }
            else
            {
                Debug.LogError("Invalid video settings provided");
            }
        }

        /// <summary>
        /// 更新控制设置
        /// </summary>
        public void UpdateControlSettings(ControlSettings newSettings)
        {
            if (validator.ValidateControlSettings(newSettings))
            {
                currentSettings.controlSettings = newSettings;
                ApplyControlSettings();
                OnControlSettingsChanged?.Invoke(newSettings);
                OnSettingsChanged?.Invoke(currentSettings);
                MarkAsChanged();
            }
            else
            {
                Debug.LogError("Invalid control settings provided");
            }
        }

        /// <summary>
        /// 更新游戏设置
        /// </summary>
        public void UpdateGameplaySettings(GameplaySettings newSettings)
        {
            if (validator.ValidateGameplaySettings(newSettings))
            {
                currentSettings.gameplaySettings = newSettings;
                ApplyGameplaySettings();
                OnGameplaySettingsChanged?.Invoke(newSettings);
                OnSettingsChanged?.Invoke(currentSettings);
                MarkAsChanged();
            }
            else
            {
                Debug.LogError("Invalid gameplay settings provided");
            }
        }

        /// <summary>
        /// 更新用户资料
        /// </summary>
        public void UpdateUserProfile(UserProfile newProfile)
        {
            if (validator.ValidateUserProfile(newProfile))
            {
                currentSettings.userProfile = newProfile;
                OnUserProfileChanged?.Invoke(newProfile);
                OnSettingsChanged?.Invoke(currentSettings);
                MarkAsChanged();
            }
            else
            {
                Debug.LogError("Invalid user profile provided");
            }
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            currentSettings.ResetToDefaults();
            ApplyAllSettings();
            OnSettingsChanged?.Invoke(currentSettings);
            MarkAsChanged();
        }

        #endregion

        #region 设置应用

        /// <summary>
        /// 应用所有设置
        /// </summary>
        public void ApplyAllSettings()
        {
            ApplyAudioSettings();
            ApplyVideoSettings();
            ApplyControlSettings();
            ApplyGameplaySettings();
        }

        /// <summary>
        /// 应用音频设置
        /// </summary>
        private void ApplyAudioSettings()
        {
            var audioSettings = currentSettings.audioSettings;

            // 设置Unity音频
            AudioListener.volume = audioSettings.masterVolume;

            // 设置AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(audioSettings.masterVolume);
                AudioManager.Instance.SetMusicVolume(audioSettings.musicVolume);
                AudioManager.Instance.SetSFXVolume(audioSettings.sfxVolume);
            }

            // 设置空间音频
            if (audioSettings.spatialAudio)
            {
                AudioSettings.speakerMode = AudioSpeakerMode.Mode7point1;
            }
            else
            {
                AudioSettings.speakerMode = AudioSpeakerMode.Stereo;
            }
        }

        /// <summary>
        /// 应用视频设置
        /// </summary>
        private void ApplyVideoSettings()
        {
            var videoSettings = currentSettings.videoSettings;

            // 设置渲染质量
            QualitySettings.SetQualityLevel((int)videoSettings.renderQuality);

            // 设置抗锯齿
            QualitySettings.antiAliasing = (int)Math.Pow(2, (int)videoSettings.antiAliasing);

            // 设置阴影质量
            QualitySettings.shadows = (ShadowQuality)videoSettings.shadowQuality;

            // 设置垂直同步
            QualitySettings.vSyncCount = videoSettings.enableVSync ? 1 : 0;

            // 设置目标帧率
            Application.targetFrameRate = videoSettings.targetFrameRate;

            // VR特定设置
            if (UnityEngine.XR.XRSettings.enabled)
            {
                UnityEngine.XR.XRSettings.renderViewportScale = videoSettings.renderScale;
            }
        }

        /// <summary>
        /// 应用控制设置
        /// </summary>
        private void ApplyControlSettings()
        {
            var controlSettings = currentSettings.controlSettings;

            // 设置输入管理器
            var inputManager = FindObjectOfType<PongHub.Core.Input.InputManager>();
            if (inputManager != null)
            {
                // 应用灵敏度设置
                // inputManager.SetSensitivity(controlSettings.vrControllerSensitivity);
                // inputManager.SetDeadZone(controlSettings.deadZone);
            }

            // 设置触觉反馈
            var hapticFeedback = FindObjectOfType<SettingsHapticFeedback>();
            if (hapticFeedback != null)
            {
                // hapticFeedback.SetEnabled(controlSettings.hapticFeedback);
                // hapticFeedback.SetIntensity(controlSettings.hapticIntensity);
            }
        }

        /// <summary>
        /// 应用游戏设置
        /// </summary>
        private void ApplyGameplaySettings()
        {
            var gameplaySettings = currentSettings.gameplaySettings;

            // 设置本地化
            var localizationManager = FindObjectOfType<PongHub.Core.Localization.LocalizationManager>();
            if (localizationManager != null)
            {
                // localizationManager.SetLanguage(gameplaySettings.language.ToString());
            }

            // 设置UI缩放
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    canvas.scaleFactor = gameplaySettings.uiScale;
                }
            }
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 异步加载设置
        /// </summary>
        public async Task LoadSettingsAsync()
        {
            try
            {
                var loadedSettings = await persistence.LoadAsync<GameSettings>();
                if (loadedSettings != null && validator.ValidateGameSettings(loadedSettings))
                {
                    currentSettings = loadedSettings;
                    Debug.Log("Settings loaded successfully");
                }
                else
                {
                    Debug.LogWarning("Invalid settings loaded, using defaults");
                    currentSettings = new GameSettings();
                }

                OnSettingsLoaded?.Invoke(currentSettings);
                hasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                currentSettings = new GameSettings();
            }
        }

        /// <summary>
        /// 异步保存设置
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            try
            {
                if (validator.ValidateGameSettings(currentSettings))
                {
                    await persistence.SaveAsync(currentSettings);
                    hasUnsavedChanges = false;
                    autoSaveTimer = 0f;
                    Debug.Log("Settings saved successfully");
                }
                else
                {
                    Debug.LogError("Cannot save invalid settings");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        /// <summary>
        /// 同步保存设置
        /// </summary>
        public void SaveSettings()
        {
            _ = SaveSettingsAsync();
        }

        /// <summary>
        /// 标记设置已更改
        /// </summary>
        private void MarkAsChanged()
        {
            hasUnsavedChanges = true;
            autoSaveTimer = 0f;
        }

        #endregion

        #region 设置导入导出

        /// <summary>
        /// 导出设置到指定路径
        /// </summary>
        public async Task ExportSettingsAsync(string filePath)
        {
            try
            {
                await persistence.ExportToFileAsync(currentSettings, filePath);
                Debug.Log($"Settings exported to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export settings: {e.Message}");
            }
        }

        /// <summary>
        /// 从指定路径导入设置
        /// </summary>
        public async Task ImportSettingsAsync(string filePath)
        {
            try
            {
                var importedSettings = await persistence.ImportFromFileAsync<GameSettings>(filePath);
                if (importedSettings != null && validator.ValidateGameSettings(importedSettings))
                {
                    currentSettings = importedSettings;
                    ApplyAllSettings();
                    OnSettingsChanged?.Invoke(currentSettings);
                    MarkAsChanged();
                    Debug.Log($"Settings imported from: {filePath}");
                }
                else
                {
                    Debug.LogError("Invalid settings file");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import settings: {e.Message}");
            }
        }

        #endregion

        #region 清理

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // 保存设置在销毁前
                if (hasUnsavedChanges)
                {
                    SaveSettings();
                }

                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && hasUnsavedChanges)
            {
                SaveSettings();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && hasUnsavedChanges)
            {
                SaveSettings();
            }
        }

        #endregion
    }
}