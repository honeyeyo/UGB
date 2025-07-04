using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 空间音频管理器
    /// 专门管理3D空间音效和位置相关的音频处理
    /// </summary>
    public class SpatialAudioManager : MonoBehaviour
    {
        [Header("空间音频设置")]
        [SerializeField]
        [Tooltip("Audio Listener / 音频监听器 - Transform of the audio listener for spatial calculations")]
        private Transform m_audioListener;

        [SerializeField]
        [Tooltip("Max Audio Distance / 最大音频距离 - Maximum distance for audio to be heard")]
        private float m_maxAudioDistance = 100f;

        [SerializeField]
        [Tooltip("Occlusion Layers / 遮挡层 - Layer mask for audio occlusion detection")]
        private LayerMask m_occlusionLayers = -1;

        [Header("性能优化")]
        [SerializeField]
        [Tooltip("Update Interval / 更新间隔 - Time interval between spatial audio updates")]
        private float m_updateInterval = 0.1f;

        [SerializeField]
        [Tooltip("Max Spatial Audio Sources / 最大空间音频源 - Maximum number of concurrent spatial audio sources")]
        private int m_maxSpatialAudioSources = 32;

        [SerializeField]
        [Tooltip("Enable Occlusion / 启用遮挡 - Whether to enable audio occlusion effects")]
        private bool m_enableOcclusion = true;

        [SerializeField]
        [Tooltip("Enable Doppler / 启用多普勒 - Whether to enable doppler effect for moving audio sources")]
        private bool m_enableDoppler = true;

        // 配置引用
        private AudioConfiguration m_configuration;

        // 空间音频跟踪
        private Dictionary<string, SpatialAudioData> m_spatialAudioSources = new();
        private List<SpatialAudioData> m_activeSpatialSources = new();

        // 距离和重要性计算
        private Dictionary<AudioCategory, float> m_categoryPriorities = new();

        // 协程引用
        private Coroutine m_updateCoroutine;

        /// <summary>
        /// 音频监听器位置
        /// </summary>
        public Vector3 ListenerPosition => m_audioListener != null ? m_audioListener.position : Camera.main?.transform.position ?? Vector3.zero;

        /// <summary>
        /// 当前活跃的空间音频数量
        /// </summary>
        public int ActiveSpatialAudioCount => m_activeSpatialSources.Count;

        /// <summary>
        /// 初始化空间音频管理器
        /// </summary>
        public void Initialize(AudioConfiguration configuration)
        {
            m_configuration = configuration;

            if (m_configuration != null)
            {
                m_maxAudioDistance = m_configuration.AudioCullingDistance;
                m_maxSpatialAudioSources = m_configuration.MaxConcurrentAudio;
                m_enableOcclusion = m_configuration.EnableSpatialAudio;
            }

            // 查找或创建音频监听器
            if (m_audioListener == null)
            {
                var audioListener = FindObjectOfType<AudioListener>();
                m_audioListener = audioListener?.transform;
            }

            // 初始化分类优先级
            InitializeCategoryPriorities();

            // 启动更新协程
            if (m_updateCoroutine == null)
            {
                m_updateCoroutine = StartCoroutine(SpatialAudioUpdateCoroutine());
            }
        }

        /// <summary>
        /// 在指定位置播放音频
        /// </summary>
        public AudioHandle PlayAt(AudioClip clip, Vector3 worldPosition, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            if (clip == null) return null;

            // 检查距离裁剪
            float distance = Vector3.Distance(worldPosition, ListenerPosition);
            if (distance > m_maxAudioDistance)
            {
                return null; // 超出听力范围
            }

            // 创建空间音频数据
            var spatialData = new SpatialAudioData
            {
                Id = System.Guid.NewGuid().ToString(),
                Category = category,
                Position = worldPosition,
                Volume = volume,
                Distance = distance,
                Priority = CalculatePriority(category, distance, volume),
                LastUpdateTime = Time.time
            };

            // 检查是否需要限制数量
            if (m_activeSpatialSources.Count >= m_maxSpatialAudioSources)
            {
                if (!TryReplaceLowerPriorityAudio(spatialData))
                {
                    return null; // 无法播放
                }
            }

            // 播放音频
            var audioHandle = AudioService.Instance?.PlayOneShot(clip, worldPosition, category, volume);
            if (audioHandle != null)
            {
                spatialData.Handle = audioHandle;

                // 应用空间音频设置
                ApplySpatialSettings(audioHandle, spatialData);

                // 注册到管理列表
                m_spatialAudioSources[spatialData.Id] = spatialData;
                m_activeSpatialSources.Add(spatialData);
            }

            return audioHandle;
        }

        /// <summary>
        /// 播放跟随对象的音频
        /// </summary>
        public AudioHandle PlayAttached(AudioClip clip, Transform target, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            if (clip == null || target == null) return null;

            var spatialData = new SpatialAudioData
            {
                Id = System.Guid.NewGuid().ToString(),
                Category = category,
                Target = target,
                Position = target.position,
                Volume = volume,
                Distance = Vector3.Distance(target.position, ListenerPosition),
                Priority = CalculatePriority(category, Vector3.Distance(target.position, ListenerPosition), volume),
                LastUpdateTime = Time.time,
                IsAttached = true
            };

            var audioHandle = AudioService.Instance?.PlayOneShot(clip, target.position, category, volume);
            if (audioHandle != null)
            {
                spatialData.Handle = audioHandle;

                // 附加到目标对象
                if (audioHandle.Source != null)
                {
                    audioHandle.Source.transform.SetParent(target);
                    audioHandle.Source.transform.localPosition = Vector3.zero;
                }

                ApplySpatialSettings(audioHandle, spatialData);

                m_spatialAudioSources[spatialData.Id] = spatialData;
                m_activeSpatialSources.Add(spatialData);
            }

            return audioHandle;
        }

        /// <summary>
        /// 设置监听器环境
        /// </summary>
        public void SetListenerEnvironment(AudioReverbPreset environment)
        {
            // 通过AudioMixerController应用环境效果
            if (AudioService.Instance?.Configuration != null)
            {
                var mixerController = FindObjectOfType<AudioMixerController>();
                if (mixerController != null)
                {
                    // 对所有分类应用混响
                    foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
                    {
                        mixerController.ApplyReverbZone(category, environment);
                    }
                }
            }
        }

        /// <summary>
        /// 启用/禁用多普勒效果
        /// </summary>
        public void SetDopplerEffect(bool enabled)
        {
            m_enableDoppler = enabled;

            // 更新所有活跃的空间音频源
            foreach (var spatialData in m_activeSpatialSources)
            {
                if (spatialData.Handle?.Source != null)
                {
                    spatialData.Handle.Source.dopplerLevel = enabled ?
                        (m_configuration?.DefaultDopplerLevel ?? 1f) : 0f;
                }
            }
        }

        /// <summary>
        /// 更新空间音频位置
        /// </summary>
        public void UpdateAudioPosition(string audioId, Vector3 newPosition, Vector3? velocity = null)
        {
            if (m_spatialAudioSources.TryGetValue(audioId, out SpatialAudioData spatialData))
            {
                spatialData.Position = newPosition;
                spatialData.Distance = Vector3.Distance(newPosition, ListenerPosition);
                spatialData.LastUpdateTime = Time.time;

                if (spatialData.Handle?.Source != null)
                {
                    spatialData.Handle.Source.transform.position = newPosition;

                    // 应用多普勒效果
                    if (m_enableDoppler && velocity.HasValue)
                    {
                        var rigidbody = spatialData.Handle.Source.GetComponent<Rigidbody>();
                        if (rigidbody == null)
                        {
                            rigidbody = spatialData.Handle.Source.gameObject.AddComponent<Rigidbody>();
                            rigidbody.isKinematic = true;
                        }
                        rigidbody.velocity = velocity.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 移除空间音频
        /// </summary>
        public void RemoveSpatialAudio(string audioId)
        {
            if (m_spatialAudioSources.TryGetValue(audioId, out SpatialAudioData spatialData))
            {
                m_spatialAudioSources.Remove(audioId);
                m_activeSpatialSources.Remove(spatialData);
            }
        }

        #region 私有方法

        /// <summary>
        /// 初始化分类优先级
        /// </summary>
        private void InitializeCategoryPriorities()
        {
            m_categoryPriorities[AudioCategory.Voice] = 1.0f;      // 最高优先级
            m_categoryPriorities[AudioCategory.SFX] = 0.8f;       // 高优先级
            m_categoryPriorities[AudioCategory.UI] = 0.7f;        // 中高优先级
            m_categoryPriorities[AudioCategory.Music] = 0.6f;     // 中等优先级
            m_categoryPriorities[AudioCategory.Ambient] = 0.4f;   // 中低优先级
            m_categoryPriorities[AudioCategory.Crowd] = 0.3f;     // 低优先级
        }

        /// <summary>
        /// 计算音频优先级
        /// </summary>
        private float CalculatePriority(AudioCategory category, float distance, float volume)
        {
            float categoryPriority = m_categoryPriorities.GetValueOrDefault(category, 0.5f);
            float distanceFactor = 1f - Mathf.Clamp01(distance / m_maxAudioDistance);
            float volumeFactor = volume;

            return categoryPriority * distanceFactor * volumeFactor;
        }

        /// <summary>
        /// 尝试替换低优先级音频
        /// </summary>
        private bool TryReplaceLowerPriorityAudio(SpatialAudioData newAudio)
        {
            // 找到优先级最低的音频
            SpatialAudioData lowestPriority = null;
            float minPriority = newAudio.Priority;

            foreach (var spatialData in m_activeSpatialSources)
            {
                if (spatialData.Priority < minPriority)
                {
                    minPriority = spatialData.Priority;
                    lowestPriority = spatialData;
                }
            }

            if (lowestPriority != null)
            {
                // 停止低优先级音频
                lowestPriority.Handle?.Stop();
                RemoveSpatialAudio(lowestPriority.Id);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 应用空间音频设置
        /// </summary>
        private void ApplySpatialSettings(AudioHandle audioHandle, SpatialAudioData spatialData)
        {
            if (audioHandle?.Source == null) return;

            var audioSource = audioHandle.Source;

            // 基础3D设置
            audioSource.spatialBlend = 1f; // 完全3D

            if (m_configuration != null)
            {
                audioSource.rolloffMode = m_configuration.DefaultRolloffMode;
                audioSource.minDistance = m_configuration.DefaultMinDistance;
                audioSource.maxDistance = m_configuration.DefaultMaxDistance;
                audioSource.dopplerLevel = m_enableDoppler ? m_configuration.DefaultDopplerLevel : 0f;
                audioSource.spread = m_configuration.DefaultSpread;
            }

            // 设置位置
            audioSource.transform.position = spatialData.Position;

            // 应用遮挡效果
            if (m_enableOcclusion)
            {
                ApplyOcclusion(audioSource, spatialData);
            }
        }

        /// <summary>
        /// 应用遮挡效果
        /// </summary>
        private void ApplyOcclusion(AudioSource audioSource, SpatialAudioData spatialData)
        {
            Vector3 listenerPos = ListenerPosition;
            Vector3 audioPos = spatialData.Position;

            // 射线检测遮挡
            if (Physics.Raycast(listenerPos, (audioPos - listenerPos).normalized,
                out RaycastHit hit, spatialData.Distance, m_occlusionLayers))
            {
                // 有遮挡，降低音量和应用低通滤波
                float occlusionFactor = Mathf.Clamp01(hit.distance / spatialData.Distance);

                // 降低音量
                float occludedVolume = spatialData.Volume * (0.3f + 0.7f * occlusionFactor);
                audioSource.volume = occludedVolume;

                // 应用低通滤波器（模拟声音穿过障碍物的效果）
                var lowPass = audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass == null)
                {
                    lowPass = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                }
                lowPass.cutoffFrequency = Mathf.Lerp(800f, 5000f, occlusionFactor);
            }
            else
            {
                // 无遮挡，恢复原始设置
                audioSource.volume = spatialData.Volume;

                var lowPass = audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    lowPass.cutoffFrequency = 5000f; // 默认值
                }
            }
        }

        /// <summary>
        /// 空间音频更新协程
        /// </summary>
        private IEnumerator SpatialAudioUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_updateInterval);
                UpdateSpatialAudio();
            }
        }

        /// <summary>
        /// 更新空间音频
        /// </summary>
        private void UpdateSpatialAudio()
        {
            Vector3 listenerPos = ListenerPosition;
            var audioToRemove = new List<SpatialAudioData>();

            foreach (var spatialData in m_activeSpatialSources)
            {
                // 检查音频是否仍然有效
                if (spatialData.Handle == null || !spatialData.Handle.IsValid || !spatialData.Handle.IsPlaying)
                {
                    audioToRemove.Add(spatialData);
                    continue;
                }

                // 更新跟随对象的位置
                if (spatialData.IsAttached && spatialData.Target != null)
                {
                    spatialData.Position = spatialData.Target.position;
                }

                // 更新距离
                float newDistance = Vector3.Distance(spatialData.Position, listenerPos);
                spatialData.Distance = newDistance;

                // 距离裁剪
                if (newDistance > m_maxAudioDistance)
                {
                    spatialData.Handle.Stop();
                    audioToRemove.Add(spatialData);
                    continue;
                }

                // 更新优先级
                spatialData.Priority = CalculatePriority(spatialData.Category, newDistance, spatialData.Volume);

                // 更新遮挡效果
                if (m_enableOcclusion && spatialData.Handle.Source != null)
                {
                    ApplyOcclusion(spatialData.Handle.Source, spatialData);
                }

                spatialData.LastUpdateTime = Time.time;
            }

            // 移除无效的音频
            foreach (var audioData in audioToRemove)
            {
                RemoveSpatialAudio(audioData.Id);
            }
        }

        #endregion

        #region 生命周期

        private void OnDestroy()
        {
            if (m_updateCoroutine != null)
            {
                StopCoroutine(m_updateCoroutine);
            }

            // 清理所有空间音频
            foreach (var spatialData in m_activeSpatialSources)
            {
                spatialData.Handle?.Stop();
            }

            m_spatialAudioSources.Clear();
            m_activeSpatialSources.Clear();
        }

        #endregion

        #region 调试

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"SpatialAudio - Active: {ActiveSpatialAudioCount}/{m_maxSpatialAudioSources}, Listener: {ListenerPosition}";
        }

        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // 绘制监听器位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ListenerPosition, 2f);

            // 绘制音频裁剪范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ListenerPosition, m_maxAudioDistance);

            // 绘制活跃的空间音频源
            foreach (var spatialData in m_activeSpatialSources)
            {
                if (spatialData.Handle != null && spatialData.Handle.IsValid)
                {
                    Gizmos.color = GetCategoryColor(spatialData.Category);
                    Gizmos.DrawWireSphere(spatialData.Position, 1f);

                    // 绘制到监听器的连线
                    Gizmos.DrawLine(spatialData.Position, ListenerPosition);
                }
            }
        }

        /// <summary>
        /// 获取分类颜色（调试用）
        /// </summary>
        private Color GetCategoryColor(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => Color.blue,
                AudioCategory.SFX => Color.red,
                AudioCategory.Voice => Color.cyan,
                AudioCategory.Ambient => Color.green,
                AudioCategory.Crowd => Color.magenta,
                AudioCategory.UI => Color.white,
                _ => Color.gray
            };
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 空间音频数据
        /// </summary>
        private class SpatialAudioData
        {
            public string Id;
            public AudioHandle Handle;
            public AudioCategory Category;
            public Vector3 Position;
            public Transform Target; // 跟随的目标对象
            public float Volume;
            public float Distance;
            public float Priority;
            public float LastUpdateTime;
            public bool IsAttached;
        }

        #endregion
    }
}