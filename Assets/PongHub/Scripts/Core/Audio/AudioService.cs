using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Meta.Utilities;
using PongHub.App;
using UnityEngine.Audio;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 现代化音频服务
    /// 统一的音频系统入口点，提供完整的音频管理功能
    /// </summary>
    public class AudioService : Singleton<AudioService>
    {
        [Header("配置")]
        [SerializeField] private AudioConfiguration m_configuration;

        [Header("组件引用")]
        [SerializeField] private AudioSourcePool m_audioSourcePool;
        [SerializeField] private AudioMixerController m_mixerController;
        [SerializeField] private SpatialAudioManager m_spatialAudioManager;

        // 当前播放的音频跟踪
        private Dictionary<string, AudioHandle> m_activeAudio = new();
        private Dictionary<AudioCategory, List<AudioHandle>> m_audioByCategory = new();
        private Dictionary<string, Coroutine> m_fadeCoroutines = new();

        // 音量缓存
        private Dictionary<AudioCategory, float> m_currentVolumes = new();

        // 初始化状态
        private bool m_isInitialized = false;

        /// <summary>
        /// 获取当前音频配置
        /// </summary>
        public AudioConfiguration Configuration => m_configuration;

        /// <summary>
        /// 音频系统是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 获取当前播放的音频数量
        /// </summary>
        public int ActiveAudioCount => m_activeAudio.Count;

        /// <summary>
        /// 音源池引用
        /// </summary>
        public AudioSourcePool AudioSourcePool => m_audioSourcePool;

        /// <summary>
        /// 混音器控制器引用
        /// </summary>
        public AudioMixerController MixerController => m_mixerController;

        /// <summary>
        /// 空间音频管理器引用
        /// </summary>
        public SpatialAudioManager SpatialAudioManager => m_spatialAudioManager;

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSystem();
        }

        private void Start()
        {
            // 订阅音频事件
            AudioEventBus.Subscribe(HandleAudioEvent);

            // 从GameSettings加载音量设置
            LoadVolumeSettings();

            // 标记为已初始化
            m_isInitialized = true;
            AudioEventBus.Publish(new AudioSystemStateEvent(AudioSystemStateEvent.SystemState.Ready));

            Debug.Log("AudioService: System initialized successfully");
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            AudioEventBus.Unsubscribe(HandleAudioEvent);

            // 停止所有音频
            StopAllAudio();

            // 清理协程
            StopAllCoroutines();

            AudioEventBus.Publish(new AudioSystemStateEvent(AudioSystemStateEvent.SystemState.Cleanup));
        }

        #region 初始化

        /// <summary>
        /// 初始化音频系统
        /// </summary>
        private void InitializeAudioSystem()
        {
            AudioEventBus.Publish(new AudioSystemStateEvent(AudioSystemStateEvent.SystemState.Initializing));

            // 验证配置
            if (m_configuration == null)
            {
                Debug.LogError("AudioService: No AudioConfiguration assigned!");
                return;
            }

            if (!m_configuration.ValidateConfiguration())
            {
                Debug.LogError("AudioService: Invalid AudioConfiguration!");
                return;
            }

            // 初始化组件
            InitializeComponents();

            // 初始化音频分类字典
            InitializeAudioCategories();

            // 应用音频质量设置
            ApplyAudioQualitySettings();
        }

        /// <summary>
        /// 初始化子组件
        /// </summary>
        private void InitializeComponents()
        {
            // 创建或获取子组件
            if (m_audioSourcePool == null)
            {
                m_audioSourcePool = GetComponentInChildren<AudioSourcePool>();
                if (m_audioSourcePool == null)
                {
                    var poolGO = new GameObject("AudioSourcePool");
                    poolGO.transform.SetParent(transform);
                    m_audioSourcePool = poolGO.AddComponent<AudioSourcePool>();
                }
            }

            if (m_mixerController == null)
            {
                m_mixerController = GetComponentInChildren<AudioMixerController>();
                if (m_mixerController == null)
                {
                    var mixerGO = new GameObject("AudioMixerController");
                    mixerGO.transform.SetParent(transform);
                    m_mixerController = mixerGO.AddComponent<AudioMixerController>();
                }
            }

            if (m_spatialAudioManager == null)
            {
                m_spatialAudioManager = GetComponentInChildren<SpatialAudioManager>();
                if (m_spatialAudioManager == null)
                {
                    var spatialGO = new GameObject("SpatialAudioManager");
                    spatialGO.transform.SetParent(transform);
                    m_spatialAudioManager = spatialGO.AddComponent<SpatialAudioManager>();
                }
            }

            // 初始化组件
            m_audioSourcePool.Initialize(m_configuration);
            m_mixerController.Initialize(m_configuration);
            m_spatialAudioManager.Initialize(m_configuration);
        }

        /// <summary>
        /// 初始化音频分类字典
        /// </summary>
        private void InitializeAudioCategories()
        {
            m_audioByCategory.Clear();
            m_currentVolumes.Clear();

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                m_audioByCategory[category] = new List<AudioHandle>();
                m_currentVolumes[category] = m_configuration.GetDefaultVolume(category);
            }
        }

        /// <summary>
        /// 应用音频质量设置
        /// </summary>
        private void ApplyAudioQualitySettings()
        {
            // 设置Unity音频质量
            AudioSettings.outputSampleRate = m_configuration.AudioQuality switch
            {
                AudioQualityLevel.Low => 22050,
                AudioQualityLevel.Medium => 44100,
                AudioQualityLevel.High => 48000,
                _ => 44100
            };

            // 设置空间音频
            AudioSettings.speakerMode = m_configuration.EnableSpatialAudio ?
                AudioSpeakerMode.Stereo : AudioSpeakerMode.Mono;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理音频事件
        /// </summary>
        private void HandleAudioEvent(AudioEvent audioEvent)
        {
            switch (audioEvent)
            {
                case OneShotAudioEvent oneShotEvent:
                    HandleOneShotEvent(oneShotEvent);
                    break;

                case LoopAudioEvent loopEvent:
                    HandleLoopEvent(loopEvent);
                    break;

                case VolumeChangeEvent volumeEvent:
                    HandleVolumeChangeEvent(volumeEvent);
                    break;

                case StopAudioEvent stopEvent:
                    HandleStopEvent(stopEvent);
                    break;

                case StopCategoryEvent stopCategoryEvent:
                    HandleStopCategoryEvent(stopCategoryEvent);
                    break;

                case PauseResumeEvent pauseResumeEvent:
                    HandlePauseResumeEvent(pauseResumeEvent);
                    break;

                case SpatialAudioUpdateEvent spatialEvent:
                    HandleSpatialUpdateEvent(spatialEvent);
                    break;
            }
        }

        private void HandleOneShotEvent(OneShotAudioEvent audioEvent)
        {
            if (audioEvent.Clip == null) return;

            var handle = PlayAudioInternal(audioEvent.Clip, audioEvent.Category, audioEvent.Parameters);
            if (handle != null && audioEvent.Parameters.FadeInTime > 0)
            {
                StartFadeIn(handle, audioEvent.Parameters.FadeInTime, audioEvent.Parameters.FadeType);
            }
        }

        private void HandleLoopEvent(LoopAudioEvent audioEvent)
        {
            if (audioEvent.Clip == null) return;

            var handle = PlayAudioInternal(audioEvent.Clip, audioEvent.Category, audioEvent.Parameters);
            if (handle != null)
            {
                // 注册循环音频ID以便后续控制
                m_activeAudio[audioEvent.LoopId] = handle;

                if (audioEvent.Parameters.FadeInTime > 0)
                {
                    StartFadeIn(handle, audioEvent.Parameters.FadeInTime, audioEvent.Parameters.FadeType);
                }
            }
        }

        private void HandleVolumeChangeEvent(VolumeChangeEvent volumeEvent)
        {
            SetCategoryVolumeInternal(volumeEvent.TargetCategory, volumeEvent.NewVolume);
        }

        private void HandleStopEvent(StopAudioEvent stopEvent)
        {
            if (m_activeAudio.TryGetValue(stopEvent.AudioId, out AudioHandle handle))
            {
                if (stopEvent.FadeOut)
                {
                    StartFadeOut(handle, stopEvent.FadeTime, AudioFadeType.Linear, () =>
                    {
                        handle.Stop();
                        RemoveAudioHandle(handle);
                    });
                }
                else
                {
                    handle.Stop();
                    RemoveAudioHandle(handle);
                }
            }
        }

        private void HandleStopCategoryEvent(StopCategoryEvent stopCategoryEvent)
        {
            StopCategoryInternal(stopCategoryEvent.TargetCategory, stopCategoryEvent.FadeOut, stopCategoryEvent.FadeTime);
        }

        private void HandlePauseResumeEvent(PauseResumeEvent pauseResumeEvent)
        {
            if (!string.IsNullOrEmpty(pauseResumeEvent.AudioId))
            {
                // 暂停/恢复特定音频
                if (m_activeAudio.TryGetValue(pauseResumeEvent.AudioId, out AudioHandle handle))
                {
                    if (pauseResumeEvent.IsPause)
                        handle.Pause();
                    else
                        handle.Resume();
                }
            }
            else if (pauseResumeEvent.TargetCategory.HasValue)
            {
                // 暂停/恢复整个分类
                var categoryHandles = m_audioByCategory[pauseResumeEvent.TargetCategory.Value];
                foreach (var handle in categoryHandles)
                {
                    if (pauseResumeEvent.IsPause)
                        handle.Pause();
                    else
                        handle.Resume();
                }
            }
        }

        private void HandleSpatialUpdateEvent(SpatialAudioUpdateEvent spatialEvent)
        {
            if (m_activeAudio.TryGetValue(spatialEvent.AudioId, out AudioHandle handle))
            {
                if (handle.IsValid)
                {
                    handle.Source.transform.position = spatialEvent.NewPosition;

                    if (spatialEvent.NewVelocity.HasValue)
                    {
                        // 通过SpatialAudioManager处理多普勒效果
                        m_spatialAudioManager.UpdateAudioPosition(spatialEvent.AudioId, spatialEvent.NewPosition, spatialEvent.NewVelocity);
                    }
                }
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 播放单次音效
        /// </summary>
        public AudioHandle PlayOneShot(AudioClip clip, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var parameters = AudioPlayParams.Simple(volume);
            return PlayAudioInternal(clip, category, parameters);
        }

        /// <summary>
        /// 在指定位置播放音效
        /// </summary>
        public AudioHandle PlayOneShot(AudioClip clip, Vector3 position, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var parameters = AudioPlayParams.At(position, volume);
            return PlayAudioInternal(clip, category, parameters);
        }

        /// <summary>
        /// 播放循环音效
        /// </summary>
        public AudioHandle PlayLooped(AudioClip clip, AudioCategory category = AudioCategory.Music, float volume = 1f)
        {
            var parameters = AudioPlayParams.Looped(volume);
            return PlayAudioInternal(clip, category, parameters);
        }

        /// <summary>
        /// 播放带淡入淡出的音效
        /// </summary>
        public AudioHandle PlayWithFade(AudioClip clip, float fadeInTime, float fadeOutTime = 0f, AudioCategory category = AudioCategory.Music)
        {
            var parameters = new AudioPlayParams
            {
                FadeInTime = fadeInTime,
                FadeOutTime = fadeOutTime
            };

            var handle = PlayAudioInternal(clip, category, parameters);
            if (handle != null && fadeInTime > 0)
            {
                StartFadeIn(handle, fadeInTime, AudioFadeType.Linear);
            }

            return handle;
        }

        /// <summary>
        /// 设置分类音量
        /// </summary>
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            // 发布事件，让系统处理
            AudioEventBus.Publish(new VolumeChangeEvent(category, volume, GetCategoryVolume(category)));
        }

        /// <summary>
        /// 内部设置分类音量
        /// </summary>
        private void SetCategoryVolumeInternal(AudioCategory category, float volume)
        {
            volume = m_configuration.GetVolumeRange(category).Clamp(volume);

            float oldVolume = m_currentVolumes[category];
            m_currentVolumes[category] = volume;

            // 应用到混音器
            m_mixerController.SetMixerVolume(category, volume);

            // 保存到GameSettings
            SaveVolumeToSettings(category, volume);

            Debug.Log($"AudioService: Set {category} volume to {volume:F2}");
        }

        /// <summary>
        /// 获取分类音量
        /// </summary>
        public float GetCategoryVolume(AudioCategory category)
        {
            return m_currentVolumes.TryGetValue(category, out float volume) ? volume : 1f;
        }

        /// <summary>
        /// 停止分类中的所有音效
        /// </summary>
        public void StopAllInCategory(AudioCategory category)
        {
            AudioEventBus.Publish(new StopCategoryEvent(category));
        }

        /// <summary>
        /// 暂停分类中的所有音效
        /// </summary>
        public void PauseAllInCategory(AudioCategory category)
        {
            AudioEventBus.Publish(new PauseResumeEvent(category, true));
        }

        /// <summary>
        /// 恢复分类中的所有音效
        /// </summary>
        public void ResumeAllInCategory(AudioCategory category)
        {
            AudioEventBus.Publish(new PauseResumeEvent(category, false));
        }

        /// <summary>
        /// 停止所有音频
        /// </summary>
        public void StopAllAudio()
        {
            foreach (var handle in m_activeAudio.Values)
            {
                handle.Stop();
            }

            m_activeAudio.Clear();

            foreach (var categoryList in m_audioByCategory.Values)
            {
                categoryList.Clear();
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 播放音频的内部实现
        /// </summary>
        private AudioHandle PlayAudioInternal(AudioClip clip, AudioCategory category, AudioPlayParams parameters)
        {
            if (clip == null || !m_isInitialized) return null;

            // 检查并发限制
            if (m_activeAudio.Count >= m_configuration.MaxConcurrentAudio)
            {
                Debug.LogWarning("AudioService: Maximum concurrent audio limit reached!");
                return null;
            }

            // 从对象池获取AudioSource
            var audioSource = m_audioSourcePool.GetPooledSource(category);
            if (audioSource == null)
            {
                Debug.LogError("AudioService: Failed to get AudioSource from pool!");
                return null;
            }

            // 配置AudioSource
            ConfigureAudioSource(audioSource, clip, category, parameters);

            // 创建音频句柄
            var handle = new AudioHandle(audioSource, category);
            var handleId = System.Guid.NewGuid().ToString();

            // 注册音频句柄
            m_activeAudio[handleId] = handle;
            m_audioByCategory[category].Add(handle);

            // 播放音频
            audioSource.Play();

            // 如果不是循环播放，设置自动清理
            if (!parameters.Loop)
            {
                StartCoroutine(AutoCleanupAudio(handle, clip.length));
            }

            return handle;
        }

        /// <summary>
        /// 配置AudioSource
        /// </summary>
        private void ConfigureAudioSource(AudioSource audioSource, AudioClip clip, AudioCategory category, AudioPlayParams parameters)
        {
            audioSource.clip = clip;
            audioSource.volume = parameters.Volume * GetCategoryVolume(category);
            audioSource.pitch = parameters.Pitch;
            audioSource.loop = parameters.Loop;
            audioSource.priority = (int)parameters.Priority;

            // 3D音效设置
            audioSource.spatialBlend = parameters.SpatialBlend;
            audioSource.minDistance = parameters.MinDistance;
            audioSource.maxDistance = parameters.MaxDistance;
            audioSource.rolloffMode = parameters.RolloffMode;
            audioSource.dopplerLevel = m_configuration.DefaultDopplerLevel;

            // 位置设置
            if (parameters.Position.HasValue)
            {
                audioSource.transform.position = parameters.Position.Value;
            }

            if (parameters.AttachTo != null)
            {
                audioSource.transform.SetParent(parameters.AttachTo);
                audioSource.transform.localPosition = Vector3.zero;
            }

            // 混音器组设置
            audioSource.outputAudioMixerGroup = m_configuration.GetMixerGroup(category);
        }

        /// <summary>
        /// 移除音频句柄
        /// </summary>
        private void RemoveAudioHandle(AudioHandle handle)
        {
            // 从分类列表中移除
            if (m_audioByCategory.TryGetValue(handle.Category, out List<AudioHandle> categoryList))
            {
                categoryList.Remove(handle);
            }

            // 从活跃音频字典中移除
            foreach (var kvp in m_activeAudio)
            {
                if (kvp.Value == handle)
                {
                    m_activeAudio.Remove(kvp.Key);
                    break;
                }
            }

            // 归还AudioSource到对象池
            if (handle.IsValid)
            {
                m_audioSourcePool.ReturnToPool(handle.Source);
            }
        }

        /// <summary>
        /// 停止分类音频的内部实现
        /// </summary>
        private void StopCategoryInternal(AudioCategory category, bool fadeOut, float fadeTime)
        {
            if (!m_audioByCategory.TryGetValue(category, out List<AudioHandle> handles)) return;

            // 创建副本以避免迭代时修改集合
            var handlesCopy = new List<AudioHandle>(handles);

            foreach (var handle in handlesCopy)
            {
                if (fadeOut)
                {
                    StartFadeOut(handle, fadeTime, AudioFadeType.Linear, () =>
                    {
                        handle.Stop();
                        RemoveAudioHandle(handle);
                    });
                }
                else
                {
                    handle.Stop();
                    RemoveAudioHandle(handle);
                }
            }
        }

        /// <summary>
        /// 从GameSettings加载音量设置
        /// </summary>
        private void LoadVolumeSettings()
        {
            var settings = GameSettings.Instance;

            SetCategoryVolumeInternal(AudioCategory.Master, 1.0f); // Master总是1.0，通过mixer控制
            SetCategoryVolumeInternal(AudioCategory.Music, settings.MusicVolume);
            SetCategoryVolumeInternal(AudioCategory.SFX, settings.SfxVolume);
            SetCategoryVolumeInternal(AudioCategory.Crowd, settings.CrowdVolume);

            // 其他分类使用默认值
            SetCategoryVolumeInternal(AudioCategory.Voice, m_configuration.DefaultVoiceVolume);
            SetCategoryVolumeInternal(AudioCategory.Ambient, m_configuration.DefaultAmbientVolume);
            SetCategoryVolumeInternal(AudioCategory.UI, m_configuration.DefaultUIVolume);
        }

        /// <summary>
        /// 保存音量到GameSettings
        /// </summary>
        private void SaveVolumeToSettings(AudioCategory category, float volume)
        {
            var settings = GameSettings.Instance;

            switch (category)
            {
                case AudioCategory.Music:
                    settings.MusicVolume = volume;
                    break;
                case AudioCategory.SFX:
                    settings.SfxVolume = volume;
                    break;
                case AudioCategory.Crowd:
                    settings.CrowdVolume = volume;
                    break;
                // 其他分类不保存到GameSettings，使用配置默认值
            }
        }

        #endregion

        #region 淡入淡出

        /// <summary>
        /// 开始淡入
        /// </summary>
        private void StartFadeIn(AudioHandle handle, float fadeTime, AudioFadeType fadeType)
        {
            if (!handle.IsValid) return;

            var handleId = GetHandleId(handle);
            if (handleId != null)
            {
                StopFadeCoroutine(handleId);
                m_fadeCoroutines[handleId] = StartCoroutine(FadeVolume(handle, 0f, handle.Source.volume, fadeTime, fadeType));
            }
        }

        /// <summary>
        /// 开始淡出
        /// </summary>
        private void StartFadeOut(AudioHandle handle, float fadeTime, AudioFadeType fadeType, System.Action onComplete = null)
        {
            if (!handle.IsValid) return;

            var handleId = GetHandleId(handle);
            if (handleId != null)
            {
                StopFadeCoroutine(handleId);
                m_fadeCoroutines[handleId] = StartCoroutine(FadeVolume(handle, handle.Source.volume, 0f, fadeTime, fadeType, onComplete));
            }
        }

        /// <summary>
        /// 停止淡入淡出协程
        /// </summary>
        private void StopFadeCoroutine(string handleId)
        {
            if (m_fadeCoroutines.TryGetValue(handleId, out Coroutine coroutine))
            {
                if (coroutine != null) StopCoroutine(coroutine);
                m_fadeCoroutines.Remove(handleId);
            }
        }

        /// <summary>
        /// 音量淡入淡出协程
        /// </summary>
        private IEnumerator FadeVolume(AudioHandle handle, float fromVolume, float toVolume, float duration, AudioFadeType fadeType, System.Action onComplete = null)
        {
            if (!handle.IsValid) yield break;

            float elapsed = 0f;

            while (elapsed < duration && handle.IsValid)
            {
                float t = elapsed / duration;

                // 应用淡入淡出曲线
                float curveValue = fadeType switch
                {
                    AudioFadeType.EaseIn => t * t,
                    AudioFadeType.EaseOut => t * (2f - t),
                    AudioFadeType.EaseInOut => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                    _ => t // Linear
                };

                float currentVolume = Mathf.Lerp(fromVolume, toVolume, curveValue);
                handle.SetVolume(currentVolume);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (handle.IsValid)
            {
                handle.SetVolume(toVolume);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 获取音频句柄的ID
        /// </summary>
        private string GetHandleId(AudioHandle handle)
        {
            foreach (var kvp in m_activeAudio)
            {
                if (kvp.Value == handle)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// 自动清理音频协程
        /// </summary>
        private IEnumerator AutoCleanupAudio(AudioHandle handle, float duration)
        {
            yield return new WaitForSeconds(duration + 0.1f); // 稍微延长以确保播放完成

            if (handle.IsValid && !handle.IsPlaying)
            {
                RemoveAudioHandle(handle);
            }
        }

        #endregion

        #region 调试和统计

        /// <summary>
        /// 获取音频统计信息
        /// </summary>
        public void GetAudioStats(out int totalActive, out Dictionary<AudioCategory, int> byCategory)
        {
            totalActive = m_activeAudio.Count;
            byCategory = new Dictionary<AudioCategory, int>();

            foreach (var kvp in m_audioByCategory)
            {
                byCategory[kvp.Key] = kvp.Value.Count;
            }
        }

        /// <summary>
        /// 获取完整的调试信息
        /// </summary>
        public string GetDetailedDebugInfo()
        {
            GetAudioStats(out int totalActive, out var byCategory);

            var info = $"AudioService Debug Info:\n";
            info += $"- Total Active: {totalActive}/{m_configuration.MaxConcurrentAudio}\n";
            info += $"- Pool: {m_audioSourcePool?.GetDebugInfo()}\n";
            info += $"- Spatial: {m_spatialAudioManager?.GetDebugInfo()}\n";
            info += $"- By Category:\n";

            foreach (var kvp in byCategory)
            {
                float volume = GetCategoryVolume(kvp.Key);
                info += $"  {kvp.Key}: {kvp.Value} playing, Volume: {volume:F2}\n";
            }

            return info;
        }

        /// <summary>
        /// 在Inspector中显示调试信息
        /// </summary>
        private void OnValidate()
        {
            if (m_configuration != null)
            {
                m_configuration.ValidateConfiguration();
            }
        }

        #endregion
    }
}