using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using PongHub.UI.Settings.Core;
using PongHub.Core.Audio;

namespace PongHub.UI.Settings.Integration
{
    /// <summary>
    /// 音频系统集成
    /// Audio System Integration - Connects settings to Unity's audio system
    /// </summary>
    public class AudioSystemIntegration : MonoBehaviour
    {
        [Header("音频配置")]
        [SerializeField]
        [Tooltip("主音频混合器")]
        private AudioMixer audioMixer;

        [SerializeField]
        [Tooltip("音频管理器")]
        private AudioManager audioManager;

        [Header("音频组配置")]
        [SerializeField]
        [Tooltip("主音量组名")]
        private string masterVolumeGroup = "MasterVolume";

        [SerializeField]
        [Tooltip("音乐音量组名")]
        private string musicVolumeGroup = "MusicVolume";

        [SerializeField]
        [Tooltip("音效音量组名")]
        private string sfxVolumeGroup = "SfxVolume";

        [SerializeField]
        [Tooltip("语音音量组名")]
        private string voiceVolumeGroup = "VoiceVolume";

        [Header("音频设备配置")]
        [SerializeField]
        [Tooltip("支持的音频设备列表")]
        private AudioDevice[] supportedDevices = new AudioDevice[0];

        // 内部状态
        private PongHub.UI.Settings.Core.AudioSettings currentSettings;
        private bool isInitialized = false;
        private Coroutine fadeCoroutine;

        /// <summary>
        /// 音频设备信息
        /// </summary>
        [Serializable]
        public class AudioDevice
        {
            public string deviceName;
            public string displayName;
            public bool isVROptimized;
            public UnityEngine.AudioConfiguration configuration;
        }

        #region Unity 生命周期

        private void Awake()
        {
            FindComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        #endregion

        #region 初始化

        private void FindComponents()
        {
            // 自动查找音频混合器（如果未指定）
            if (audioMixer == null)
            {
                var audioMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
                foreach (var mixer in audioMixers)
                {
                    if (mixer.name.Contains("PHAudioMixer") || mixer.name.Contains("Main"))
                    {
                        audioMixer = mixer;
                        break;
                    }
                }
            }

            // 查找音频管理器
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<AudioManager>();
            }
        }

        private void Initialize()
        {
            if (audioMixer == null)
            {
                Debug.LogError("AudioSystemIntegration: AudioMixer not found!");
                return;
            }

            // 获取当前音频设置
            if (SettingsManager.Instance != null)
            {
                currentSettings = SettingsManager.Instance.GetAudioSettings();
                ApplyAudioSettings(currentSettings);
            }

            // 检测可用音频设备
            DetectAudioDevices();

            isInitialized = true;
            Debug.Log("AudioSystemIntegration initialized successfully");
        }

        private void RegisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnAudioSettingsChanged += OnAudioSettingsChanged;
            }
        }

        private void UnregisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnAudioSettingsChanged -= OnAudioSettingsChanged;
            }
        }

        #endregion

        #region 音频设置应用

        /// <summary>
        /// 应用音频设置
        /// </summary>
        /// <param name="settings">音频设置</param>
        public void ApplyAudioSettings(PongHub.UI.Settings.Core.AudioSettings settings)
        {
            if (!isInitialized || settings == null)
                return;

            currentSettings = settings;

            // 应用音量设置
            ApplyVolumeSettings();

            // 应用音频质量设置
            ApplyAudioQuality();

            // 应用空间音频设置
            ApplySpatialAudio();

            // 应用其他音频选项
            ApplyAudioOptions();

            Debug.Log("Audio settings applied successfully");
        }

        private void ApplyVolumeSettings()
        {
            if (audioMixer == null) return;

            // 转换线性音量到分贝
            float masterVolumeDb = VolumeToDecibel(currentSettings.masterVolume);
            float musicVolumeDb = VolumeToDecibel(currentSettings.musicVolume);
            float sfxVolumeDb = VolumeToDecibel(currentSettings.sfxVolume);
            float voiceVolumeDb = VolumeToDecibel(currentSettings.voiceVolume);

            // 设置音频混合器参数
            audioMixer.SetFloat(masterVolumeGroup, masterVolumeDb);
            audioMixer.SetFloat(musicVolumeGroup, musicVolumeDb);
            audioMixer.SetFloat(sfxVolumeGroup, sfxVolumeDb);
            audioMixer.SetFloat(voiceVolumeGroup, voiceVolumeDb);

            // 通知音频管理器
            if (audioManager != null)
            {
                audioManager.UpdateVolumeSettings(currentSettings);
            }
        }

        private void ApplyAudioQuality()
        {
            // 根据音频质量设置配置参数
            switch (currentSettings.audioQuality)
            {
                case AudioQuality.Low:
                    UnityEngine.AudioSettings.outputSampleRate = 22050;
                    break;
                case AudioQuality.Medium:
                    UnityEngine.AudioSettings.outputSampleRate = 44100;
                    break;
                case AudioQuality.High:
                    UnityEngine.AudioSettings.outputSampleRate = 48000;
                    break;
            }

            // 设置音频缓冲区大小（VR优化）
            if (Application.platform == RuntimePlatform.Android)
            {
                // Quest设备优化 - SetConfiguration方法在新版Unity中已废除
                // UnityEngine.AudioSettings.SetConfiguration(new UnityEngine.AudioConfiguration
                // {
                //     sampleRate = UnityEngine.AudioSettings.outputSampleRate,
                //     dspBufferSize = currentSettings.audioQuality == AudioQuality.High ? 256 : 512, 
                //     numRealVoices = 32,
                //     numVirtualVoices = 512
                // });
                Debug.Log("Audio configuration skipped for new Unity version compatibility");
            }
        }

        private void ApplySpatialAudio()
        {
            // 查找AudioListener实例
            var audioListener = FindObjectOfType<AudioListener>();
            
            if (!currentSettings.spatialAudio)
            {
                if (audioListener != null)
                {
                    audioListener.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
                }
                return;
            }

            // 启用空间音频
            if (audioListener != null)
            {
                audioListener.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
            }

            // 设置音频传播范围
            var audioSources = FindObjectsOfType<AudioSource>();
            foreach (var source in audioSources)
            {
                if (source.spatialBlend > 0.5f) // 只影响3D音频源
                {
                    source.maxDistance = currentSettings.audioRange;
                    source.rolloffMode = AudioRolloffMode.Logarithmic;
                }
            }
        }

        private void ApplyAudioOptions()
        {
            // 失去焦点时静音
            if (currentSettings.muteOnFocusLoss)
            {
                Application.focusChanged += OnApplicationFocusChanged;
            }
            else
            {
                Application.focusChanged -= OnApplicationFocusChanged;
            }

            // 设置音频设备
            if (!string.IsNullOrEmpty(currentSettings.audioDevice) &&
                currentSettings.audioDevice != "Default")
            {
                SetAudioDevice(currentSettings.audioDevice);
            }
        }

        #endregion

        #region 音频设备管理

        /// <summary>
        /// 检测可用音频设备
        /// </summary>
        private void DetectAudioDevices()
        {
            var devices = new System.Collections.Generic.List<AudioDevice>();

            // 添加默认设备
            devices.Add(new AudioDevice
            {
                deviceName = "Default",
                displayName = "默认设备",
                isVROptimized = false,
                configuration = UnityEngine.AudioSettings.GetConfiguration()
            });

            // 检测VR优化设备
            if (UnityEngine.XR.XRSettings.enabled)
            {
                devices.Add(new AudioDevice
                {
                    deviceName = "VR_Optimized",
                    displayName = "VR优化音频",
                    isVROptimized = true,
                    configuration = new UnityEngine.AudioConfiguration
                    {
                        sampleRate = 48000,
                        dspBufferSize = 256,
                        numRealVoices = 24,
                        numVirtualVoices = 256
                    }
                });
            }

            supportedDevices = devices.ToArray();
        }

        /// <summary>
        /// 设置音频设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        private void SetAudioDevice(string deviceName)
        {
            var device = Array.Find(supportedDevices, d => d.deviceName == deviceName);
            if (device != null)
            {
                // UnityEngine.AudioSettings.SetConfiguration(device.configuration);
                // SetConfiguration method is deprecated in newer Unity versions
                Debug.Log($"Audio device set to: {device.displayName} (SetConfiguration skipped for compatibility)");}
        }

        /// <summary>
        /// 获取可用音频设备列表
        /// </summary>
        /// <returns>设备名称数组</returns>
        public string[] GetAvailableDevices()
        {
            var deviceNames = new string[supportedDevices.Length];
            for (int i = 0; i < supportedDevices.Length; i++)
            {
                deviceNames[i] = supportedDevices[i].displayName;
            }
            return deviceNames;
        }

        #endregion

        #region 音频效果和渐变

        /// <summary>
        /// 淡入音量
        /// </summary>
        /// <param name="duration">渐变时长</param>
        public void FadeIn(float duration = 1f)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeVolume(0f, currentSettings.masterVolume, duration));
        }

        /// <summary>
        /// 淡出音量
        /// </summary>
        /// <param name="duration">渐变时长</param>
        public void FadeOut(float duration = 1f)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeVolume(currentSettings.masterVolume, 0f, duration));
        }

        private IEnumerator FadeVolume(float fromVolume, float toVolume, float duration)
        {
            float elapsed = 0f;
            float fromDb = VolumeToDecibel(fromVolume);
            float toDb = VolumeToDecibel(toVolume);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentDb = Mathf.Lerp(fromDb, toDb, progress);

                audioMixer.SetFloat(masterVolumeGroup, currentDb);
                yield return null;
            }

            audioMixer.SetFloat(masterVolumeGroup, toDb);
            fadeCoroutine = null;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 音频设置变更事件处理
        /// </summary>
        private void OnAudioSettingsChanged(PongHub.UI.Settings.Core.AudioSettings newSettings)
        {
            ApplyAudioSettings(newSettings);
        }

        /// <summary>
        /// 应用程序焦点变更事件
        /// </summary>
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!currentSettings.muteOnFocusLoss) return;

            if (hasFocus)
            {
                FadeIn(0.5f);
            }
            else
            {
                FadeOut(0.5f);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 线性音量转换为分贝
        /// </summary>
        /// <param name="volume">线性音量 (0-1)</param>
        /// <returns>分贝值</returns>
        private float VolumeToDecibel(float volume)
        {
            if (volume <= 0f)
                return -80f; // 最小音量

            return Mathf.Log10(volume) * 20f;
        }

        /// <summary>
        /// 分贝转换为线性音量
        /// </summary>
        /// <param name="decibel">分贝值</param>
        /// <returns>线性音量 (0-1)</returns>
        private float DecibelToVolume(float decibel)
        {
            if (decibel <= -80f)
                return 0f;

            return Mathf.Pow(10f, decibel / 20f);
        }

        /// <summary>
        /// 播放音效预览
        /// </summary>
        /// <param name="clip">音效片段</param>
        public void PlayPreviewSound(AudioClip clip)
        {
            if (audioManager != null && clip != null)
            {
                audioManager.PlaySound(clip, currentSettings.sfxVolume);
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 测试音频设置
        /// </summary>
        public void TestAudioSettings()
        {
            if (audioManager != null)
            {
                // 播放测试音效
                audioManager.PlayTestSound();
            }
        }

        /// <summary>
        /// 重置音频设置
        /// </summary>
        public void ResetAudioSettings()
        {
            var defaultSettings = new PongHub.UI.Settings.Core.AudioSettings();
            ApplyAudioSettings(defaultSettings);
        }

        /// <summary>
        /// 获取当前音频延迟
        /// </summary>
        /// <returns>音频延迟（毫秒）</returns>
        public float GetAudioLatency()
        {
            var config = UnityEngine.AudioSettings.GetConfiguration();
            return (float)config.dspBufferSize / config.sampleRate * 1000f;
        }

        #endregion
    }
}