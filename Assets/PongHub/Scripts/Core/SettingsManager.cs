using UnityEngine;
using System;

using PongHub.Core.Audio;

namespace PongHub.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // 设置键名
        private const string KEY_MUSIC_VOLUME = "MusicVolume";
        private const string KEY_SOUND_VOLUME = "SoundVolume";
        private const string KEY_VR_CONTROLLER_TYPE = "VRControllerType";
        private const string KEY_DIFFICULTY = "Difficulty";
        private const string KEY_LANGUAGE = "Language";

        // 默认值
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SOUND_VOLUME = 1.0f;
        private const int DEFAULT_VR_CONTROLLER_TYPE = 0;
        private const int DEFAULT_DIFFICULTY = 1;
        private const string DEFAULT_LANGUAGE = "zh_CN";

        // 设置变更事件
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSoundVolumeChanged;
        public event Action<int> OnVRControllerTypeChanged;
        public event Action<int> OnDifficultyChanged;
        public event Action<string> OnLanguageChanged;

        // 当前设置值
        public float MusicVolume { get; private set; }
        public float SoundVolume { get; private set; }
        public int VRControllerType { get; private set; }
        public int Difficulty { get; private set; }
        public string Language { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadSettings()
        {
            // 从PlayerPrefs加载设置
            MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
            SoundVolume = PlayerPrefs.GetFloat(KEY_SOUND_VOLUME, DEFAULT_SOUND_VOLUME);
            VRControllerType = PlayerPrefs.GetInt(KEY_VR_CONTROLLER_TYPE, DEFAULT_VR_CONTROLLER_TYPE);
            Difficulty = PlayerPrefs.GetInt(KEY_DIFFICULTY, DEFAULT_DIFFICULTY);
            Language = PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);

            // 应用设置
            ApplySettings();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, MusicVolume);
            PlayerPrefs.SetFloat(KEY_SOUND_VOLUME, SoundVolume);
            PlayerPrefs.SetInt(KEY_VR_CONTROLLER_TYPE, VRControllerType);
            PlayerPrefs.SetInt(KEY_DIFFICULTY, Difficulty);
            PlayerPrefs.SetString(KEY_LANGUAGE, Language);
            PlayerPrefs.Save();
        }

        private void ApplySettings()
        {
            // 应用音频设置
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(MusicVolume);
                AudioManager.Instance.SetSoundVolume(SoundVolume);
            }

            // 应用VR控制器设置
            ApplyVRControllerSettings();

            // 应用语言设置
            ApplyLanguageSettings();

            // 应用振动设置
            ApplyVibrationSettings();
        }

        /// <summary>
        /// 应用VR控制器设置
        /// </summary>
        private void ApplyVRControllerSettings()
        {
            // VR控制器类型设置可以影响输入映射或控制器行为
            // 这里可以根据需要添加具体的控制器配置逻辑
            Debug.Log($"Applied VR Controller Type: {VRControllerType}");
        }

        /// <summary>
        /// 应用语言设置
        /// </summary>
        private void ApplyLanguageSettings()
        {
            // 应用语言设置到本地化系统
            var localizationManager = FindObjectOfType<PongHub.UI.Localization.LocalizationManager>();
            if (localizationManager != null)
            {
                localizationManager.SwitchLanguage(Language);
            }
        }

        /// <summary>
        /// 应用振动设置
        /// </summary>
        private void ApplyVibrationSettings()
        {
            // 应用振动设置到VibrationManager
            if (VibrationManager.Instance != null)
            {
                // 可以添加振动强度等设置的应用
                VibrationManager.Instance.SetVibrationEnabled(true); // 默认启用
            }
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            OnMusicVolumeChanged?.Invoke(MusicVolume);
            SaveSettings();
            ApplySettings();
        }

        public void SetSoundVolume(float volume)
        {
            SoundVolume = Mathf.Clamp01(volume);
            OnSoundVolumeChanged?.Invoke(SoundVolume);
            SaveSettings();
            ApplySettings();
        }

        public void SetVRControllerType(int type)
        {
            VRControllerType = type;
            OnVRControllerTypeChanged?.Invoke(VRControllerType);
            SaveSettings();
            ApplySettings();
        }

        public void SetDifficulty(int difficulty)
        {
            Difficulty = difficulty;
            OnDifficultyChanged?.Invoke(Difficulty);
            SaveSettings();
            ApplySettings();
        }

        public void SetLanguage(string language)
        {
            Language = language;
            OnLanguageChanged?.Invoke(Language);
            SaveSettings();
            ApplySettings();
        }

        public void ResetToDefaults()
        {
            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SoundVolume = DEFAULT_SOUND_VOLUME;
            VRControllerType = DEFAULT_VR_CONTROLLER_TYPE;
            Difficulty = DEFAULT_DIFFICULTY;
            Language = DEFAULT_LANGUAGE;

            SaveSettings();
            ApplySettings();

            // 触发所有设置变更事件
            OnMusicVolumeChanged?.Invoke(MusicVolume);
            OnSoundVolumeChanged?.Invoke(SoundVolume);
            OnVRControllerTypeChanged?.Invoke(VRControllerType);
            OnDifficultyChanged?.Invoke(Difficulty);
            OnLanguageChanged?.Invoke(Language);
        }
    }
}