using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;
using PongHub.Core.Audio;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 音频设置面板
    /// Audio settings panel for managing sound and music options
    /// </summary>
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("AudioMixer集成")]
        [SerializeField]
        [Tooltip("主音频混合器")]
        private AudioMixerGroup masterMixerGroup;

        [SerializeField]
        [Tooltip("音乐混合器组")]
        private AudioMixerGroup musicMixerGroup;

        [SerializeField]
        [Tooltip("音效混合器组")]
        private AudioMixerGroup sfxMixerGroup;

        [SerializeField]
        [Tooltip("语音混合器组")]
        private AudioMixerGroup voiceMixerGroup;

        [Header("音量设置组件")]
        [SerializeField]
        [Tooltip("主音量滑块")]
        private SettingSlider masterVolumeSlider;

        [SerializeField]
        [Tooltip("音乐音量滑块")]
        private SettingSlider musicVolumeSlider;

        [SerializeField]
        [Tooltip("音效音量滑块")]
        private SettingSlider sfxVolumeSlider;

        [SerializeField]
        [Tooltip("语音音量滑块")]
        private SettingSlider voiceVolumeSlider;

        [Header("音频选项组件")]
        [SerializeField]
        [Tooltip("失去焦点时静音开关")]
        private SettingToggle muteOnFocusLossToggle;

        [SerializeField]
        [Tooltip("空间音频开关")]
        private SettingToggle spatialAudioToggle;

        [SerializeField]
        [Tooltip("音频质量下拉框")]
        private SettingDropdown audioQualityDropdown;

        [SerializeField]
        [Tooltip("音频设备下拉框")]
        private SettingDropdown audioDeviceDropdown;

        [Header("VR音频设置")]
        [SerializeField]
        [Tooltip("音频传播距离滑块")]
        private SettingSlider audioRangeSlider;

        [Header("音频预览")]
        [SerializeField]
        [Tooltip("测试音频按钮")]
        private Button testAudioButton;

        [SerializeField]
        [Tooltip("测试音乐按钮")]
        private Button testMusicButton;

        [SerializeField]
        [Tooltip("测试音效按钮")]
        private Button testSfxButton;

        [SerializeField]
        [Tooltip("测试音频源")]
        private AudioSource testAudioSource;

        [SerializeField]
        [Tooltip("测试音频片段")]
        private AudioClip[] testAudioClips;

        [Header("音频可视化")]
        [SerializeField]
        [Tooltip("音量指示器")]
        private Slider volumeIndicator;

        [SerializeField]
        [Tooltip("音频频谱显示")]
        private Image[] spectrumBars;

        // 组件引用
        private SettingsManager settingsManager;
        private AudioManager audioManager;

        // 音频监控
        private bool isMonitoringAudio = false;
        private float[] spectrumData = new float[64];

        #region Unity 生命周期

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupAudioComponents();
            RefreshPanel();
        }

        private void Update()
        {
            if (isMonitoringAudio)
            {
                UpdateAudioVisualization();
            }
        }

        private void OnEnable()
        {
            isMonitoringAudio = true;
        }

        private void OnDisable()
        {
            isMonitoringAudio = false;
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                settingsManager = FindObjectOfType<SettingsManager>();
            }

            audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<AudioManager>();
            }

            // 确保测试音频源存在
            if (testAudioSource == null)
            {
                testAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// 设置音频组件
        /// </summary>
        private void SetupAudioComponents()
        {
            // 设置音量滑块
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.OnValueChanged += OnMasterVolumeChanged;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.OnValueChanged += OnMusicVolumeChanged;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.OnValueChanged += OnSfxVolumeChanged;
            }

            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.OnValueChanged += OnVoiceVolumeChanged;
            }

            if (audioRangeSlider != null)
            {
                audioRangeSlider.OnValueChanged += OnAudioRangeChanged;
            }

            // 设置开关
            if (muteOnFocusLossToggle != null)
            {
                muteOnFocusLossToggle.OnValueChanged += OnMuteOnFocusLossChanged;
            }

            if (spatialAudioToggle != null)
            {
                spatialAudioToggle.OnValueChanged += OnSpatialAudioChanged;
            }

            // 设置下拉框
            if (audioQualityDropdown != null)
            {
                audioQualityDropdown.OnValueChanged += OnAudioQualityChanged;
            }

            if (audioDeviceDropdown != null)
            {
                UpdateAudioDeviceList();
                audioDeviceDropdown.OnValueChanged += OnAudioDeviceChanged;
            }

            // 设置测试按钮
            if (testAudioButton != null)
            {
                testAudioButton.onClick.AddListener(() => PlayTestAudio(0));
            }

            if (testMusicButton != null)
            {
                testMusicButton.onClick.AddListener(() => PlayTestAudio(1));
            }

            if (testSfxButton != null)
            {
                testSfxButton.onClick.AddListener(() => PlayTestAudio(2));
            }
        }

        #endregion

        #region 音量设置事件

        /// <summary>
        /// 主音量变更事件
        /// </summary>
        private void OnMasterVolumeChanged(object value)
        {
            if (value is float volume && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.masterVolume = volume;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 实时应用到AudioMixer
                if (masterMixerGroup != null)
                {
                    float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                    masterMixerGroup.audioMixer.SetFloat("MasterVolume", dbValue);
                }
            }
        }

        /// <summary>
        /// 音乐音量变更事件
        /// </summary>
        private void OnMusicVolumeChanged(object value)
        {
            if (value is float volume && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.musicVolume = volume;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 实时应用到AudioMixer
                if (musicMixerGroup != null)
                {
                    float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                    musicMixerGroup.audioMixer.SetFloat("MusicVolume", dbValue);
                }
            }
        }

        /// <summary>
        /// 音效音量变更事件
        /// </summary>
        private void OnSfxVolumeChanged(object value)
        {
            if (value is float volume && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.sfxVolume = volume;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 实时应用到AudioMixer
                if (sfxMixerGroup != null)
                {
                    float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                    sfxMixerGroup.audioMixer.SetFloat("SFXVolume", dbValue);
                }
            }
        }

        /// <summary>
        /// 语音音量变更事件
        /// </summary>
        private void OnVoiceVolumeChanged(object value)
        {
            if (value is float volume && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.voiceVolume = volume;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 实时应用到AudioMixer
                if (voiceMixerGroup != null)
                {
                    float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                    voiceMixerGroup.audioMixer.SetFloat("VoiceVolume", dbValue);
                }
            }
        }

        /// <summary>
        /// 音频传播距离变更事件
        /// </summary>
        private void OnAudioRangeChanged(object value)
        {
            if (value is float range && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.audioRange = range;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 更新所有3D音频源的最大距离
                UpdateAudioSourceRanges(range);
            }
        }

        #endregion

        #region 音频选项事件

        /// <summary>
        /// 失去焦点时静音变更事件
        /// </summary>
        private void OnMuteOnFocusLossChanged(object value)
        {
            if (value is bool mute && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.muteOnFocusLoss = mute;
                settingsManager.UpdateAudioSettings(audioSettings);
            }
        }

        /// <summary>
        /// 空间音频变更事件
        /// </summary>
        private void OnSpatialAudioChanged(object value)
        {
            if (value is bool spatial && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.spatialAudio = spatial;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 更新音频设置
                ApplySpatialAudioSettings(spatial);
            }
        }

        /// <summary>
        /// 音频质量变更事件
        /// </summary>
        private void OnAudioQualityChanged(object value)
        {
            if (value is int quality && settingsManager != null)
            {
                var audioSettings = settingsManager.GetAudioSettings();
                audioSettings.audioQuality = (AudioQuality)quality;
                settingsManager.UpdateAudioSettings(audioSettings);

                // 应用音频质量设置
                ApplyAudioQualitySettings((AudioQuality)quality);
            }
        }

        /// <summary>
        /// 音频设备变更事件
        /// </summary>
        private void OnAudioDeviceChanged(object value)
        {
            if (value is int deviceIndex && settingsManager != null)
            {
                // 获取可用音频设备列表
                string[] devices = Microphone.devices;
                if (deviceIndex >= 0 && deviceIndex < devices.Length)
                {
                    var audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.audioDevice = devices[deviceIndex];
                    settingsManager.UpdateAudioSettings(audioSettings);
                }
            }
        }

        #endregion

        #region 音频测试

        /// <summary>
        /// 播放测试音频
        /// </summary>
        /// <param name="audioType">音频类型 (0=通用, 1=音乐, 2=音效)</param>
        private void PlayTestAudio(int audioType)
        {
            if (testAudioSource == null || testAudioClips == null || testAudioClips.Length == 0)
                return;

            // 选择测试音频片段
            AudioClip clipToPlay = null;
            if (audioType < testAudioClips.Length)
            {
                clipToPlay = testAudioClips[audioType];
            }
            else if (testAudioClips.Length > 0)
            {
                clipToPlay = testAudioClips[0];
            }

            if (clipToPlay != null)
            {
                // 设置音频源配置
                testAudioSource.clip = clipToPlay;
                testAudioSource.volume = GetTestVolume(audioType);

                // 设置AudioMixerGroup
                switch (audioType)
                {
                    case 1: // 音乐
                        testAudioSource.outputAudioMixerGroup = musicMixerGroup;
                        break;
                    case 2: // 音效
                        testAudioSource.outputAudioMixerGroup = sfxMixerGroup;
                        break;
                    default: // 通用
                        testAudioSource.outputAudioMixerGroup = masterMixerGroup;
                        break;
                }

                // 播放音频
                testAudioSource.Play();
            }
        }

        /// <summary>
        /// 获取测试音量
        /// </summary>
        /// <param name="audioType">音频类型</param>
        /// <returns>音量值</returns>
        private float GetTestVolume(int audioType)
        {
            if (settingsManager == null)
                return 1.0f;

            var audioSettings = settingsManager.GetAudioSettings();
            switch (audioType)
            {
                case 1: // 音乐
                    return audioSettings.masterVolume * audioSettings.musicVolume;
                case 2: // 音效
                    return audioSettings.masterVolume * audioSettings.sfxVolume;
                default: // 通用
                    return audioSettings.masterVolume;
            }
        }

        #endregion

        #region 音频可视化

        /// <summary>
        /// 更新音频可视化
        /// </summary>
        private void UpdateAudioVisualization()
        {
            // 更新音量指示器
            if (volumeIndicator != null && testAudioSource != null)
            {
                volumeIndicator.value = testAudioSource.isPlaying ? testAudioSource.volume : 0f;
            }

            // 更新频谱显示
            if (spectrumBars != null && spectrumBars.Length > 0 && testAudioSource != null && testAudioSource.isPlaying)
            {
                AudioListener.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);

                for (int i = 0; i < spectrumBars.Length && i < spectrumData.Length; i++)
                {
                    if (spectrumBars[i] != null)
                    {
                        float barHeight = Mathf.Clamp01(spectrumData[i] * 50f);
                        spectrumBars[i].fillAmount = barHeight;
                    }
                }
            }
        }

        #endregion

        #region 音频系统集成

        /// <summary>
        /// 应用空间音频设置
        /// </summary>
        /// <param name="enabled">是否启用</param>
        private void ApplySpatialAudioSettings(bool enabled)
        {
            if (enabled)
            {
                UnityEngine.AudioSettings.speakerMode = AudioSpeakerMode.Mode7point1;
            }
            else
            {
                UnityEngine.AudioSettings.speakerMode = AudioSpeakerMode.Stereo;
            }

            // 更新所有音频源的空间化设置
            AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in audioSources)
            {
                source.spatialBlend = enabled ? 1.0f : 0.0f;
            }
        }

        /// <summary>
        /// 应用音频质量设置
        /// </summary>
        /// <param name="quality">音频质量</param>
        private void ApplyAudioQualitySettings(AudioQuality quality)
        {
            UnityEngine.AudioConfiguration config = UnityEngine.AudioSettings.GetConfiguration();

            switch (quality)
            {
                case AudioQuality.Low:
                    config.sampleRate = 22050;
                    config.dspBufferSize = 1024;
                    break;
                case AudioQuality.Medium:
                    config.sampleRate = 44100;
                    config.dspBufferSize = 512;
                    break;
                case AudioQuality.High:
                    config.sampleRate = 48000;
                    config.dspBufferSize = 256;
                    break;
                case AudioQuality.Ultra:
                    config.sampleRate = 96000;
                    config.dspBufferSize = 128;
                    break;
            }

            UnityEngine.AudioSettings.Reset(config);
        }

        /// <summary>
        /// 更新音频源传播距离
        /// </summary>
        /// <param name="range">传播距离</param>
        private void UpdateAudioSourceRanges(float range)
        {
            AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in audioSources)
            {
                if (source.spatialBlend > 0.5f) // 3D音频源
                {
                    source.maxDistance = range;
                }
            }
        }

        /// <summary>
        /// 更新音频设备列表
        /// </summary>
        private void UpdateAudioDeviceList()
        {
            if (audioDeviceDropdown != null)
            {
                var devices = new System.Collections.Generic.List<string>();
                devices.Add("默认设备");
                devices.AddRange(Microphone.devices);
                audioDeviceDropdown.SetCustomOptions(devices);
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 刷新面板
        /// </summary>
        public void RefreshPanel()
        {
            if (settingsManager == null) return;

            var audioSettings = settingsManager.GetAudioSettings();

            // 更新音量滑块
            masterVolumeSlider?.SetValue(audioSettings.masterVolume);
            musicVolumeSlider?.SetValue(audioSettings.musicVolume);
            sfxVolumeSlider?.SetValue(audioSettings.sfxVolume);
            voiceVolumeSlider?.SetValue(audioSettings.voiceVolume);
            audioRangeSlider?.SetValue(audioSettings.audioRange);

            // 更新开关
            muteOnFocusLossToggle?.SetValue(audioSettings.muteOnFocusLoss);
            spatialAudioToggle?.SetValue(audioSettings.spatialAudio);

            // 更新下拉框
            audioQualityDropdown?.SetValue((int)audioSettings.audioQuality);

            // 更新音频设备选择
            UpdateAudioDeviceSelection(audioSettings.audioDevice);
        }

        /// <summary>
        /// 更新音频设备选择
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        private void UpdateAudioDeviceSelection(string deviceName)
        {
            if (audioDeviceDropdown == null) return;

            // 查找设备索引
            string[] devices = Microphone.devices;
            int deviceIndex = 0; // 默认设备

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == deviceName)
                {
                    deviceIndex = i + 1; // +1 因为第一个是"默认设备"
                    break;
                }
            }

            audioDeviceDropdown.SetValue(deviceIndex);
        }

        #endregion

        #region 清理

        private void OnDestroy()
        {
            // 清理事件
            if (masterVolumeSlider != null)
                masterVolumeSlider.OnValueChanged -= OnMasterVolumeChanged;
            if (musicVolumeSlider != null)
                musicVolumeSlider.OnValueChanged -= OnMusicVolumeChanged;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.OnValueChanged -= OnSfxVolumeChanged;
            if (voiceVolumeSlider != null)
                voiceVolumeSlider.OnValueChanged -= OnVoiceVolumeChanged;
            if (audioRangeSlider != null)
                audioRangeSlider.OnValueChanged -= OnAudioRangeChanged;

            if (muteOnFocusLossToggle != null)
                muteOnFocusLossToggle.OnValueChanged -= OnMuteOnFocusLossChanged;
            if (spatialAudioToggle != null)
                spatialAudioToggle.OnValueChanged -= OnSpatialAudioChanged;

            if (audioQualityDropdown != null)
                audioQualityDropdown.OnValueChanged -= OnAudioQualityChanged;
            if (audioDeviceDropdown != null)
                audioDeviceDropdown.OnValueChanged -= OnAudioDeviceChanged;

            // 清理按钮事件
            if (testAudioButton != null)
                testAudioButton.onClick.RemoveAllListeners();
            if (testMusicButton != null)
                testMusicButton.onClick.RemoveAllListeners();
            if (testSfxButton != null)
                testSfxButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}