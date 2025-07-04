using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// AudioMixer控制器
    /// 专门负责AudioMixer参数的设置和管理
    /// </summary>
    public class AudioMixerController : MonoBehaviour
    {
        [Header("混音器引用")]
        [SerializeField]
        [Tooltip("Main Mixer / 主混音器 - Main audio mixer for controlling audio output")]
        private AudioMixer m_mainMixer;

        // 配置引用
        private AudioConfiguration m_configuration;

        // 缓存的混音器参数
        private Dictionary<AudioCategory, string> m_mixerParams = new();
        private Dictionary<AudioCategory, float> m_currentVolumes = new();

        /// <summary>
        /// 主混音器引用
        /// </summary>
        public AudioMixer MainMixer => m_mainMixer;

        /// <summary>
        /// 初始化混音器控制器
        /// </summary>
        public void Initialize(AudioConfiguration configuration)
        {
            m_configuration = configuration;

            if (m_configuration != null)
            {
                m_mainMixer = m_configuration.MainMixer;
                CacheMixerParameters();
                InitializeDefaultVolumes();
            }
            else
            {
                Debug.LogError("AudioMixerController: No AudioConfiguration provided!");
            }
        }

        /// <summary>
        /// 缓存混音器参数名称
        /// </summary>
        private void CacheMixerParameters()
        {
            m_mixerParams.Clear();

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                string paramName = m_configuration.GetMixerParam(category);
                if (!string.IsNullOrEmpty(paramName))
                {
                    m_mixerParams[category] = paramName;
                }
            }
        }

        /// <summary>
        /// 初始化默认音量
        /// </summary>
        private void InitializeDefaultVolumes()
        {
            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                float defaultVolume = m_configuration.GetDefaultVolume(category);
                SetMixerVolume(category, defaultVolume);
            }
        }

        /// <summary>
        /// 设置混音器音量
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="linearVolume">线性音量值 (0-1)</param>
        public void SetMixerVolume(AudioCategory category, float linearVolume)
        {
            if (m_mainMixer == null)
            {
                Debug.LogWarning("AudioMixerController: No main mixer assigned!");
                return;
            }

            if (!m_mixerParams.TryGetValue(category, out string paramName))
            {
                Debug.LogWarning($"AudioMixerController: No mixer parameter found for category {category}");
                return;
            }

            // 限制音量范围
            if (m_configuration != null)
            {
                linearVolume = m_configuration.GetVolumeRange(category).Clamp(linearVolume);
            }
            else
            {
                linearVolume = Mathf.Clamp01(linearVolume);
            }

            // 转换为分贝值
            float dbValue = LinearToDecibel(linearVolume);

            // 应用到混音器
            bool success = m_mainMixer.SetFloat(paramName, dbValue);

            if (success)
            {
                m_currentVolumes[category] = linearVolume;
            }
            else
            {
                Debug.LogError($"AudioMixerController: Failed to set mixer parameter {paramName} to {dbValue}dB");
            }
        }

        /// <summary>
        /// 获取混音器音量
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <returns>线性音量值 (0-1)</returns>
        public float GetMixerVolume(AudioCategory category)
        {
            if (m_currentVolumes.TryGetValue(category, out float volume))
            {
                return volume;
            }

            // 尝试从混音器读取
            if (m_mainMixer != null && m_mixerParams.TryGetValue(category, out string paramName))
            {
                if (m_mainMixer.GetFloat(paramName, out float dbValue))
                {
                    volume = DecibelToLinear(dbValue);
                    m_currentVolumes[category] = volume;
                    return volume;
                }
            }

            // 返回默认值
            return m_configuration?.GetDefaultVolume(category) ?? 1f;
        }

        /// <summary>
        /// 设置混音器音调
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="pitch">音调值 (0.1-3.0)</param>
        public void SetMixerPitch(AudioCategory category, float pitch)
        {
            if (m_mainMixer == null) return;

            pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            string pitchParam = GetPitchParamName(category);

            if (!string.IsNullOrEmpty(pitchParam))
            {
                m_mainMixer.SetFloat(pitchParam, pitch);
            }
        }

        /// <summary>
        /// 设置低通滤波器
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="frequency">截止频率 (Hz)</param>
        public void SetMixerLowpass(AudioCategory category, float frequency)
        {
            if (m_mainMixer == null) return;

            frequency = Mathf.Clamp(frequency, 20f, 22000f);
            string lowpassParam = GetLowpassParamName(category);

            if (!string.IsNullOrEmpty(lowpassParam))
            {
                m_mainMixer.SetFloat(lowpassParam, frequency);
            }
        }

        /// <summary>
        /// 应用混响预设
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="preset">混响预设</param>
        public void ApplyReverbZone(AudioCategory category, AudioReverbPreset preset)
        {
            if (!m_configuration.EnableReverb) return;

            // 这里可以根据预设设置混响参数
            // 需要在AudioMixer中预先设置好混响效果器
            var reverbParams = GetReverbParameters(preset);
            ApplyReverbParameters(category, reverbParams);
        }

        /// <summary>
        /// 设置回声效果
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="delay">延迟时间 (ms)</param>
        /// <param name="decay">衰减率 (0-1)</param>
        public void SetEchoEffect(AudioCategory category, float delay, float decay)
        {
            if (m_mainMixer == null) return;

            delay = Mathf.Clamp(delay, 10f, 5000f);
            decay = Mathf.Clamp01(decay);

            string echoDelayParam = GetEchoDelayParamName(category);
            string echoDecayParam = GetEchoDecayParamName(category);

            if (!string.IsNullOrEmpty(echoDelayParam))
            {
                m_mainMixer.SetFloat(echoDelayParam, delay);
            }

            if (!string.IsNullOrEmpty(echoDecayParam))
            {
                m_mainMixer.SetFloat(echoDecayParam, decay);
            }
        }

        /// <summary>
        /// 静音/取消静音分类
        /// </summary>
        /// <param name="category">音频分类</param>
        /// <param name="mute">是否静音</param>
        public void SetCategoryMute(AudioCategory category, bool mute)
        {
            if (mute)
            {
                SetMixerVolume(category, 0f);
            }
            else
            {
                // 恢复到默认音量或上次设置的音量
                float volume = m_configuration?.GetDefaultVolume(category) ?? 1f;
                SetMixerVolume(category, volume);
            }
        }

        /// <summary>
        /// 重置所有混音器参数
        /// </summary>
        public void ResetAllParameters()
        {
            if (m_configuration == null) return;

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                float defaultVolume = m_configuration.GetDefaultVolume(category);
                SetMixerVolume(category, defaultVolume);
            }
        }

        /// <summary>
        /// 创建音频快照
        /// </summary>
        /// <param name="snapshotName">快照名称</param>
        /// <param name="transitionTime">过渡时间</param>
        public void TransitionToSnapshot(string snapshotName, float transitionTime = 1f)
        {
            if (m_mainMixer == null) return;

            var snapshot = m_mainMixer.FindSnapshot(snapshotName);
            if (snapshot != null)
            {
                snapshot.TransitionTo(transitionTime);
            }
            else
            {
                Debug.LogWarning($"AudioMixerController: Snapshot '{snapshotName}' not found!");
            }
        }

        #region 工具方法

        /// <summary>
        /// 线性音量转分贝
        /// </summary>
        private float LinearToDecibel(float linearVolume)
        {
            // 避免log(0)错误，设置最小值
            if (linearVolume <= 0.0001f)
                return -80f; // Unity AudioMixer的最小值通常是-80dB

            return Mathf.Log10(linearVolume) * 20f;
        }

        /// <summary>
        /// 分贝转线性音量
        /// </summary>
        private float DecibelToLinear(float decibelVolume)
        {
            if (decibelVolume <= -80f)
                return 0f;

            return Mathf.Pow(10f, decibelVolume / 20f);
        }

        /// <summary>
        /// 获取音调参数名称
        /// </summary>
        private string GetPitchParamName(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => "MusicPitch",
                AudioCategory.SFX => "SfxPitch",
                AudioCategory.Voice => "VoicePitch",
                AudioCategory.Ambient => "AmbientPitch",
                AudioCategory.Crowd => "CrowdPitch",
                AudioCategory.UI => "UIPitch",
                _ => null
            };
        }

        /// <summary>
        /// 获取低通滤波器参数名称
        /// </summary>
        private string GetLowpassParamName(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => "MusicLowpass",
                AudioCategory.SFX => "SfxLowpass",
                AudioCategory.Voice => "VoiceLowpass",
                AudioCategory.Ambient => "AmbientLowpass",
                AudioCategory.Crowd => "CrowdLowpass",
                AudioCategory.UI => "UILowpass",
                _ => null
            };
        }

        /// <summary>
        /// 获取回声延迟参数名称
        /// </summary>
        private string GetEchoDelayParamName(AudioCategory category)
        {
            return $"{category}EchoDelay";
        }

        /// <summary>
        /// 获取回声衰减参数名称
        /// </summary>
        private string GetEchoDecayParamName(AudioCategory category)
        {
            return $"{category}EchoDecay";
        }

        /// <summary>
        /// 获取混响参数
        /// </summary>
        private ReverbParameters GetReverbParameters(AudioReverbPreset preset)
        {
            return preset switch
            {
                AudioReverbPreset.Cave => new ReverbParameters { Room = -1000, RoomHF = -200, DecayTime = 2.9f },
                AudioReverbPreset.Arena => new ReverbParameters { Room = -1000, RoomHF = -698, DecayTime = 7.2f },
                AudioReverbPreset.Hangar => new ReverbParameters { Room = -1000, RoomHF = -300, DecayTime = 10.0f },
                AudioReverbPreset.Room => new ReverbParameters { Room = -1000, RoomHF = -454, DecayTime = 0.4f },
                _ => new ReverbParameters()
            };
        }

        /// <summary>
        /// 应用混响参数
        /// </summary>
        private void ApplyReverbParameters(AudioCategory category, ReverbParameters parameters)
        {
            // 这里需要根据实际的AudioMixer设置来实现
            // 示例代码，需要根据实际混音器参数调整
            if (m_mainMixer != null)
            {
                string reverbRoomParam = $"{category}ReverbRoom";
                string reverbDecayParam = $"{category}ReverbDecay";

                m_mainMixer.SetFloat(reverbRoomParam, parameters.Room);
                m_mainMixer.SetFloat(reverbDecayParam, parameters.DecayTime);
            }
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 混响参数结构
        /// </summary>
        private struct ReverbParameters
        {
            public float Room;
            public float RoomHF;
            public float DecayTime;
        }

        #endregion

        #region 调试

        /// <summary>
        /// 获取所有音量状态
        /// </summary>
        public Dictionary<AudioCategory, float> GetAllVolumes()
        {
            return new Dictionary<AudioCategory, float>(m_currentVolumes);
        }

        /// <summary>
        /// 验证混音器参数
        /// </summary>
        public bool ValidateMixerParameters()
        {
            if (m_mainMixer == null)
            {
                Debug.LogError("AudioMixerController: No main mixer assigned!");
                return false;
            }

            bool allValid = true;

            foreach (var kvp in m_mixerParams)
            {
                if (!m_mainMixer.GetFloat(kvp.Value, out _))
                {
                    Debug.LogWarning($"AudioMixerController: Parameter '{kvp.Value}' not found in mixer for category {kvp.Key}");
                    allValid = false;
                }
            }

            return allValid;
        }

        private void OnValidate()
        {
            if (m_mainMixer != null && m_configuration != null && Application.isPlaying)
            {
                ValidateMixerParameters();
            }
        }

        #endregion
    }
}