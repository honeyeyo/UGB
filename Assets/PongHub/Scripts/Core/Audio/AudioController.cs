using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 现代化音频控制器
    /// 提供高级音频控制和游戏特定的音频功能
    /// </summary>
    public class AudioController : MonoBehaviour
    {
        [Header("音频设置")]
        [SerializeField] private bool m_enableDebugLogging = false;
        [SerializeField] private bool m_pauseAudioOnFocusLoss = true;
        [SerializeField] private bool m_lowerVolumeOnPause = true;
        [SerializeField] private float m_pausedVolumeFactor = 0.3f;

        [Header("快捷音效配置")]
        [SerializeField] private AudioClip m_buttonClickSound;
        [SerializeField] private AudioClip m_buttonHoverSound;
        [SerializeField] private AudioClip m_errorSound;
        [SerializeField] private AudioClip m_successSound;
        [SerializeField] private AudioClip m_notificationSound;

        // 应用状态管理
        private bool m_isApplicationPaused = false;
        private bool m_applicationHasFocus = true;
        private Dictionary<AudioCategory, float> m_volumesBeforePause = new();

        // 快捷音效缓存
        private Dictionary<string, AudioClip> m_quickSounds = new();

        // 序列播放队列
        private Queue<AudioSequenceItem> m_audioSequence = new();
        private bool m_isPlayingSequence = false;

        /// <summary>
        /// 音频服务引用
        /// </summary>
        public AudioService AudioService => AudioService.Instance;

        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public bool EnableDebugLogging
        {
            get => m_enableDebugLogging;
            set => m_enableDebugLogging = value;
        }

        private void Awake()
        {
            InitializeQuickSounds();
        }

        private void Start()
        {
            // 等待AudioService初始化完成
            StartCoroutine(WaitForAudioServiceAndInitialize());
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            m_isApplicationPaused = pauseStatus;

            if (m_pauseAudioOnFocusLoss)
            {
                HandleApplicationStateChange();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            m_applicationHasFocus = hasFocus;

            if (m_pauseAudioOnFocusLoss)
            {
                HandleApplicationStateChange();
            }
        }

        #region 初始化

        /// <summary>
        /// 等待AudioService初始化并进行配置
        /// </summary>
        private IEnumerator WaitForAudioServiceAndInitialize()
        {
            // 等待AudioService初始化
            while (AudioService == null || !AudioService.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            DebugLog("AudioController: AudioService ready, initializing...");

            // 订阅AudioService事件
            AudioEventBus.Subscribe(OnAudioEvent);

            DebugLog("AudioController: Initialization complete");
        }

        /// <summary>
        /// 初始化快捷音效
        /// </summary>
        private void InitializeQuickSounds()
        {
            m_quickSounds.Clear();

            if (m_buttonClickSound != null) m_quickSounds["click"] = m_buttonClickSound;
            if (m_buttonHoverSound != null) m_quickSounds["hover"] = m_buttonHoverSound;
            if (m_errorSound != null) m_quickSounds["error"] = m_errorSound;
            if (m_successSound != null) m_quickSounds["success"] = m_successSound;
            if (m_notificationSound != null) m_quickSounds["notification"] = m_notificationSound;
        }

        #endregion

        #region 应用状态管理

        /// <summary>
        /// 处理应用状态变化
        /// </summary>
        private void HandleApplicationStateChange()
        {
            bool shouldPause = m_isApplicationPaused || !m_applicationHasFocus;

            if (shouldPause)
            {
                OnApplicationLostFocus();
            }
            else
            {
                OnApplicationGainedFocus();
            }
        }

        /// <summary>
        /// 应用失去焦点时的处理
        /// </summary>
        private void OnApplicationLostFocus()
        {
            DebugLog("AudioController: Application lost focus, adjusting audio...");

            if (m_lowerVolumeOnPause)
            {
                // 保存当前音量并降低
                SaveCurrentVolumes();
                LowerAllVolumes();
            }
            else
            {
                // 暂停所有音频
                PauseAllAudio();
            }
        }

        /// <summary>
        /// 应用获得焦点时的处理
        /// </summary>
        private void OnApplicationGainedFocus()
        {
            DebugLog("AudioController: Application gained focus, restoring audio...");

            if (m_lowerVolumeOnPause)
            {
                RestoreAllVolumes();
            }
            else
            {
                ResumeAllAudio();
            }
        }

        /// <summary>
        /// 保存当前音量
        /// </summary>
        private void SaveCurrentVolumes()
        {
            m_volumesBeforePause.Clear();

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    m_volumesBeforePause[category] = AudioService.GetCategoryVolume(category);
                }
            }
        }

        /// <summary>
        /// 降低所有音量
        /// </summary>
        private void LowerAllVolumes()
        {
            foreach (var kvp in m_volumesBeforePause)
            {
                float newVolume = kvp.Value * m_pausedVolumeFactor;
                AudioService.SetCategoryVolume(kvp.Key, newVolume);
            }
        }

        /// <summary>
        /// 恢复所有音量
        /// </summary>
        private void RestoreAllVolumes()
        {
            foreach (var kvp in m_volumesBeforePause)
            {
                AudioService.SetCategoryVolume(kvp.Key, kvp.Value);
            }

            m_volumesBeforePause.Clear();
        }

        /// <summary>
        /// 暂停所有音频
        /// </summary>
        private void PauseAllAudio()
        {
            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    AudioService.PauseAllInCategory(category);
                }
            }
        }

        /// <summary>
        /// 恢复所有音频
        /// </summary>
        private void ResumeAllAudio()
        {
            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    AudioService.ResumeAllInCategory(category);
                }
            }
        }

        #endregion

        #region 快捷音效API

        /// <summary>
        /// 播放预设的快捷音效
        /// </summary>
        public AudioHandle PlayQuickSound(string soundName, float volume = 1f)
        {
            if (m_quickSounds.TryGetValue(soundName.ToLower(), out AudioClip clip))
            {
                return AudioService.PlayOneShot(clip, AudioCategory.UI, volume);
            }

            DebugLog($"AudioController: Quick sound '{soundName}' not found!");
            return null;
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        public AudioHandle PlayButtonClick(float volume = 1f)
        {
            return PlayQuickSound("click", volume);
        }

        /// <summary>
        /// 播放按钮悬停音效
        /// </summary>
        public AudioHandle PlayButtonHover(float volume = 0.7f)
        {
            return PlayQuickSound("hover", volume);
        }

        /// <summary>
        /// 播放错误音效
        /// </summary>
        public AudioHandle PlayError(float volume = 1f)
        {
            return PlayQuickSound("error", volume);
        }

        /// <summary>
        /// 播放成功音效
        /// </summary>
        public AudioHandle PlaySuccess(float volume = 1f)
        {
            return PlayQuickSound("success", volume);
        }

        /// <summary>
        /// 播放通知音效
        /// </summary>
        public AudioHandle PlayNotification(float volume = 0.8f)
        {
            return PlayQuickSound("notification", volume);
        }

        #endregion

        #region 高级音频控制

        /// <summary>
        /// 播放音频序列
        /// </summary>
        public void PlayAudioSequence(params AudioSequenceItem[] items)
        {
            if (m_isPlayingSequence)
            {
                DebugLog("AudioController: Audio sequence already playing, queuing new sequence...");
            }

            // 添加到队列
            foreach (var item in items)
            {
                m_audioSequence.Enqueue(item);
            }

            // 如果没有正在播放序列，开始播放
            if (!m_isPlayingSequence)
            {
                StartCoroutine(PlaySequenceCoroutine());
            }
        }

        /// <summary>
        /// 播放音频序列的协程
        /// </summary>
        private IEnumerator PlaySequenceCoroutine()
        {
            m_isPlayingSequence = true;

            while (m_audioSequence.Count > 0)
            {
                var item = m_audioSequence.Dequeue();

                if (item.Clip != null)
                {
                    var handle = AudioService.PlayOneShot(item.Clip, item.Category, item.Volume);

                    if (handle != null)
                    {
                        // 等待音频播放完成或指定的延迟时间
                        float waitTime = item.WaitTime > 0 ? item.WaitTime : item.Clip.length;
                        yield return new WaitForSeconds(waitTime);
                    }
                }
                else if (item.WaitTime > 0)
                {
                    // 纯延迟项
                    yield return new WaitForSeconds(item.WaitTime);
                }
            }

            m_isPlayingSequence = false;
            DebugLog("AudioController: Audio sequence completed");
        }

        /// <summary>
        /// 停止当前音频序列
        /// </summary>
        public void StopAudioSequence()
        {
            m_audioSequence.Clear();
            StopCoroutine(PlaySequenceCoroutine());
            m_isPlayingSequence = false;

            DebugLog("AudioController: Audio sequence stopped");
        }

        /// <summary>
        /// 创建音频淡入淡出效果
        /// </summary>
        public AudioHandle PlayWithCustomFade(AudioClip clip, AnimationCurve fadeCurve, float duration, AudioCategory category = AudioCategory.SFX)
        {
            var handle = AudioService.PlayOneShot(clip, category, 0f);
            if (handle != null)
            {
                StartCoroutine(CustomFadeCoroutine(handle, fadeCurve, duration));
            }
            return handle;
        }

        /// <summary>
        /// 自定义淡入淡出协程
        /// </summary>
        private IEnumerator CustomFadeCoroutine(AudioHandle handle, AnimationCurve curve, float duration)
        {
            if (!handle.IsValid) yield break;

            float elapsed = 0f;
            float originalVolume = handle.Volume;

            while (elapsed < duration && handle.IsValid)
            {
                float t = elapsed / duration;
                float volumeMultiplier = curve.Evaluate(t);
                handle.SetVolume(originalVolume * volumeMultiplier);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 播放随机音效（从数组中随机选择）
        /// </summary>
        public AudioHandle PlayRandomSound(AudioClip[] clips, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            if (clips == null || clips.Length == 0) return null;

            var randomClip = clips[Random.Range(0, clips.Length)];
            return AudioService.PlayOneShot(randomClip, category, volume);
        }

        /// <summary>
        /// 播放音效并在指定时间后停止
        /// </summary>
        public AudioHandle PlayTimedSound(AudioClip clip, float duration, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var handle = AudioService.PlayLooped(clip, category, volume);
            if (handle != null)
            {
                StartCoroutine(StopAfterDelay(handle, duration));
            }
            return handle;
        }

        /// <summary>
        /// 延迟停止音频协程
        /// </summary>
        private IEnumerator StopAfterDelay(AudioHandle handle, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (handle.IsValid)
            {
                handle.Stop();
            }
        }

        #endregion

        #region 音量和混音控制

        /// <summary>
        /// 平滑改变分类音量
        /// </summary>
        public void SmoothSetCategoryVolume(AudioCategory category, float targetVolume, float duration = 1f)
        {
            StartCoroutine(SmoothVolumeCoroutine(category, targetVolume, duration));
        }

        /// <summary>
        /// 平滑音量变化协程
        /// </summary>
        private IEnumerator SmoothVolumeCoroutine(AudioCategory category, float targetVolume, float duration)
        {
            float startVolume = AudioService.GetCategoryVolume(category);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float currentVolume = Mathf.Lerp(startVolume, targetVolume, t);
                AudioService.SetCategoryVolume(category, currentVolume);

                elapsed += Time.deltaTime;
                yield return null;
            }

            AudioService.SetCategoryVolume(category, targetVolume);
        }

        /// <summary>
        /// 临时降低指定分类音量
        /// </summary>
        public void DuckCategory(AudioCategory category, float duckVolume, float duration)
        {
            StartCoroutine(DuckCategoryCoroutine(category, duckVolume, duration));
        }

        /// <summary>
        /// 音频闪避协程
        /// </summary>
        private IEnumerator DuckCategoryCoroutine(AudioCategory category, float duckVolume, float duration)
        {
            float originalVolume = AudioService.GetCategoryVolume(category);

            // 快速降低音量
            AudioService.SetCategoryVolume(category, duckVolume);

            // 等待指定时间
            yield return new WaitForSeconds(duration);

            // 恢复原始音量
            SmoothSetCategoryVolume(category, originalVolume, 0.5f);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理音频事件
        /// </summary>
        private void OnAudioEvent(AudioEvent audioEvent)
        {
            if (audioEvent is AudioSystemStateEvent stateEvent)
            {
                HandleSystemStateEvent(stateEvent);
            }
        }

        /// <summary>
        /// 处理系统状态事件
        /// </summary>
        private void HandleSystemStateEvent(AudioSystemStateEvent stateEvent)
        {
            switch (stateEvent.State)
            {
                case AudioSystemStateEvent.SystemState.Ready:
                    DebugLog("AudioController: Audio system ready");
                    break;

                case AudioSystemStateEvent.SystemState.Error:
                    DebugLog("AudioController: Audio system error!");
                    break;

                case AudioSystemStateEvent.SystemState.Cleanup:
                    DebugLog("AudioController: Audio system cleanup");
                    break;
            }
        }

        #endregion

        #region 调试和工具

        /// <summary>
        /// 调试日志
        /// </summary>
        private void DebugLog(string message)
        {
            if (m_enableDebugLogging)
            {
                Debug.Log($"[AudioController] {message}");
            }
        }

        /// <summary>
        /// 获取音频控制器状态信息
        /// </summary>
        public string GetStatusInfo()
        {
            var info = "AudioController Status:\n";
            info += $"- Application Paused: {m_isApplicationPaused}\n";
            info += $"- Application Has Focus: {m_applicationHasFocus}\n";
            info += $"- Playing Sequence: {m_isPlayingSequence}\n";
            info += $"- Quick Sounds Loaded: {m_quickSounds.Count}\n";
            info += $"- Sequence Queue: {m_audioSequence.Count}\n";

            if (AudioService != null)
            {
                info += "\n" + AudioService.GetDetailedDebugInfo();
            }

            return info;
        }

        /// <summary>
        /// 在Inspector中显示设置
        /// </summary>
        private void OnValidate()
        {
            // 限制音量因子范围
            m_pausedVolumeFactor = Mathf.Clamp01(m_pausedVolumeFactor);
        }

        #endregion

        private void OnDestroy()
        {
            // 取消事件订阅
            AudioEventBus.Unsubscribe(OnAudioEvent);

            // 停止所有协程
            StopAllCoroutines();
        }
    }

    /// <summary>
    /// 音频序列项
    /// 用于构建音频播放序列
    /// </summary>
    [System.Serializable]
    public class AudioSequenceItem
    {
        [Tooltip("要播放的音频剪辑")]
        public AudioClip Clip;

        [Tooltip("音频分类")]
        public AudioCategory Category = AudioCategory.SFX;

        [Tooltip("播放音量")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("等待时间（秒）。如果为0，将等待音频播放完成")]
        public float WaitTime = 0f;

        public AudioSequenceItem() { }

        public AudioSequenceItem(AudioClip clip, AudioCategory category = AudioCategory.SFX, float volume = 1f, float waitTime = 0f)
        {
            Clip = clip;
            Category = category;
            Volume = volume;
            WaitTime = waitTime;
        }

        /// <summary>
        /// 创建延迟项（不播放音频，只等待）
        /// </summary>
        public static AudioSequenceItem Delay(float seconds)
        {
            return new AudioSequenceItem { WaitTime = seconds };
        }
    }
}