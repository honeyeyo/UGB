using UnityEngine;
using UnityEngine.Audio;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 音频系统配置数据
    /// 使用ScriptableObject进行配置管理，支持编辑器预设和运行时动态配置
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfiguration", menuName = "PongHub/Audio/Audio Configuration")]
    public class AudioConfiguration : ScriptableObject
    {
        [Header("音量设置")]
        [Tooltip("主音量范围设置")]
        public FloatRange MasterVolumeRange = new(0f, 1f);

        [Tooltip("背景音乐音量范围")]
        public FloatRange MusicVolumeRange = new(0f, 1f);

        [Tooltip("音效音量范围")]
        public FloatRange SfxVolumeRange = new(0f, 1f);

        [Tooltip("语音音量范围")]
        public FloatRange VoiceVolumeRange = new(0f, 1f);

        [Tooltip("环境音音量范围")]
        public FloatRange AmbientVolumeRange = new(0f, 1f);

        [Tooltip("人群音效音量范围")]
        public FloatRange CrowdVolumeRange = new(0f, 1f);

        [Tooltip("UI音效音量范围")]
        public FloatRange UIVolumeRange = new(0f, 1f);

        [Header("默认音量值")]
        [Range(0f, 1f)] public float DefaultMasterVolume = 1.0f;
        [Range(0f, 1f)] public float DefaultMusicVolume = 0.8f;
        [Range(0f, 1f)] public float DefaultSfxVolume = 1.0f;
        [Range(0f, 1f)] public float DefaultVoiceVolume = 1.0f;
        [Range(0f, 1f)] public float DefaultAmbientVolume = 0.6f;
        [Range(0f, 1f)] public float DefaultCrowdVolume = 0.8f;
        [Range(0f, 1f)] public float DefaultUIVolume = 0.9f;

        [Header("混音器配置")]
        [Tooltip("主音频混音器")]
        public AudioMixer MainMixer;

        [Tooltip("背景音乐混音器组")]
        public AudioMixerGroup MusicGroup;

        [Tooltip("音效混音器组")]
        public AudioMixerGroup SfxGroup;

        [Tooltip("语音混音器组")]
        public AudioMixerGroup VoiceGroup;

        [Tooltip("环境音混音器组")]
        public AudioMixerGroup AmbientGroup;

        [Tooltip("人群音效混音器组")]
        public AudioMixerGroup CrowdGroup;

        [Tooltip("UI音效混音器组")]
        public AudioMixerGroup UIGroup;

        [Header("混音器参数名称")]
        [Tooltip("主音量参数名")]
        public string MasterVolumeParam = "MasterVolume";

        [Tooltip("音乐音量参数名")]
        public string MusicVolumeParam = "MusicVolume";

        [Tooltip("音效音量参数名")]
        public string SfxVolumeParam = "SfxVolume";

        [Tooltip("语音音量参数名")]
        public string VoiceVolumeParam = "VoiceVolume";

        [Tooltip("环境音音量参数名")]
        public string AmbientVolumeParam = "AmbientVolume";

        [Tooltip("人群音效音量参数名")]
        public string CrowdVolumeParam = "CrowdVolume";

        [Tooltip("UI音效音量参数名")]
        public string UIVolumeParam = "UIVolume";

        [Header("3D音效设置")]
        [Tooltip("默认的音效衰减模式")]
        public AudioRolloffMode DefaultRolloffMode = AudioRolloffMode.Logarithmic;

        [Tooltip("默认最小距离")]
        public float DefaultMinDistance = 1f;

        [Tooltip("默认最大距离")]
        public float DefaultMaxDistance = 50f;

        [Tooltip("默认多普勒级别")]
        [Range(0f, 5f)]
        public float DefaultDopplerLevel = 1f;

        [Tooltip("默认传播速度")]
        public float DefaultSpread = 0f;

        [Header("性能设置")]
        [Tooltip("音源对象池初始大小")]
        [Range(5, 50)]
        public int AudioSourcePoolSize = 20;

        [Tooltip("最大同时播放的音效数量")]
        [Range(10, 100)]
        public int MaxConcurrentAudio = 32;

        [Tooltip("音效播放距离裁剪")]
        public float AudioCullingDistance = 100f;

        [Tooltip("是否启用音效LOD系统")]
        public bool EnableAudioLOD = true;

        [Header("VR特定设置")]
        [Tooltip("是否启用空间音频")]
        public bool EnableSpatialAudio = true;

        [Tooltip("空间音频精度级别")]
        [Range(1, 5)]
        public int SpatialAudioPrecision = 3;

        [Tooltip("头部跟踪强度")]
        [Range(0f, 1f)]
        public float HeadTrackingStrength = 1f;

        [Header("音频质量设置")]
        [Tooltip("音频质量级别")]
        public AudioQualityLevel AudioQuality = AudioQualityLevel.High;

        [Tooltip("是否启用回声效果")]
        public bool EnableReverb = true;

        [Tooltip("是否启用音频压缩")]
        public bool EnableAudioCompression = false;

        /// <summary>
        /// 根据音频分类获取对应的混音器组
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <returns>对应的混音器组</returns>
        public AudioMixerGroup GetMixerGroup(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => MusicGroup,
                AudioCategory.SFX => SfxGroup,
                AudioCategory.Voice => VoiceGroup,
                AudioCategory.Ambient => AmbientGroup,
                AudioCategory.Crowd => CrowdGroup,
                AudioCategory.UI => UIGroup,
                _ => SfxGroup
            };
        }

        /// <summary>
        /// 根据音频分类获取对应的混音器参数名
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <returns>混音器参数名</returns>
        public string GetMixerParam(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Master => MasterVolumeParam,
                AudioCategory.Music => MusicVolumeParam,
                AudioCategory.SFX => SfxVolumeParam,
                AudioCategory.Voice => VoiceVolumeParam,
                AudioCategory.Ambient => AmbientVolumeParam,
                AudioCategory.Crowd => CrowdVolumeParam,
                AudioCategory.UI => UIVolumeParam,
                _ => SfxVolumeParam
            };
        }

        /// <summary>
        /// 根据音频分类获取音量范围
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <returns>音量范围</returns>
        public FloatRange GetVolumeRange(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Master => MasterVolumeRange,
                AudioCategory.Music => MusicVolumeRange,
                AudioCategory.SFX => SfxVolumeRange,
                AudioCategory.Voice => VoiceVolumeRange,
                AudioCategory.Ambient => AmbientVolumeRange,
                AudioCategory.Crowd => CrowdVolumeRange,
                AudioCategory.UI => UIVolumeRange,
                _ => SfxVolumeRange
            };
        }

        /// <summary>
        /// 根据音频分类获取默认音量
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <returns>默认音量值</returns>
        public float GetDefaultVolume(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Master => DefaultMasterVolume,
                AudioCategory.Music => DefaultMusicVolume,
                AudioCategory.SFX => DefaultSfxVolume,
                AudioCategory.Voice => DefaultVoiceVolume,
                AudioCategory.Ambient => DefaultAmbientVolume,
                AudioCategory.Crowd => DefaultCrowdVolume,
                AudioCategory.UI => DefaultUIVolume,
                _ => DefaultSfxVolume
            };
        }

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool ValidateConfiguration()
        {
            if (MainMixer == null)
            {
                Debug.LogError("AudioConfiguration: MainMixer is not assigned!");
                return false;
            }

            // 验证关键混音器组
            if (MusicGroup == null || SfxGroup == null)
            {
                Debug.LogWarning("AudioConfiguration: Essential mixer groups are not assigned!");
            }

            // 验证参数名称
            if (string.IsNullOrEmpty(MasterVolumeParam) || string.IsNullOrEmpty(MusicVolumeParam))
            {
                Debug.LogError("AudioConfiguration: Essential mixer parameter names are not set!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在编辑器中验证配置
        /// </summary>
        private void OnValidate()
        {
            // 确保音量范围合理
            MasterVolumeRange = new FloatRange(
                Mathf.Clamp(MasterVolumeRange.Min, 0f, 1f),
                Mathf.Clamp(MasterVolumeRange.Max, 0f, 1f)
            );

            // 确保对象池大小合理
            AudioSourcePoolSize = Mathf.Max(5, AudioSourcePoolSize);
            MaxConcurrentAudio = Mathf.Max(10, MaxConcurrentAudio);

            // 确保距离设置合理
            DefaultMinDistance = Mathf.Max(0.1f, DefaultMinDistance);
            DefaultMaxDistance = Mathf.Max(DefaultMinDistance + 1f, DefaultMaxDistance);
        }
    }
}