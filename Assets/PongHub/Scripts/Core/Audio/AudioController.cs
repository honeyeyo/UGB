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
            // 检查AudioService是否已初始化
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: AudioService not ready, skipping state change handling");
                return;
            }

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
            // 再次检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: AudioService not available for focus lost handling");
                return;
            }

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
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: AudioService not available for focus gained handling");
                return;
            }

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
            // 确保AudioService可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot save volumes - AudioService not available");
                return;
            }

            m_volumesBeforePause.Clear();

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    try
                    {
                        m_volumesBeforePause[category] = AudioService.GetCategoryVolume(category);
                    }
                    catch (System.Exception ex)
                    {
                        DebugLog($"AudioController: Error getting volume for category {category}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 降低所有音量
        /// </summary>
        private void LowerAllVolumes()
        {
            // 确保AudioService可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot lower volumes - AudioService not available");
                return;
            }

            foreach (var kvp in m_volumesBeforePause)
            {
                try
                {
                    float newVolume = kvp.Value * m_pausedVolumeFactor;
                    AudioService.SetCategoryVolume(kvp.Key, newVolume);
                }
                catch (System.Exception ex)
                {
                    DebugLog($"AudioController: Error lowering volume for category {kvp.Key}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 恢复所有音量
        /// </summary>
        private void RestoreAllVolumes()
        {
            // 确保AudioService可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot restore volumes - AudioService not available");
                return;
            }

            foreach (var kvp in m_volumesBeforePause)
            {
                try
                {
                    AudioService.SetCategoryVolume(kvp.Key, kvp.Value);
                }
                catch (System.Exception ex)
                {
                    DebugLog($"AudioController: Error restoring volume for category {kvp.Key}: {ex.Message}");
                }
            }

            m_volumesBeforePause.Clear();
        }

        /// <summary>
        /// 暂停所有音频
        /// </summary>
        private void PauseAllAudio()
        {
            // 确保AudioService可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot pause audio - AudioService not available");
                return;
            }

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    try
                    {
                        AudioService.PauseAllInCategory(category);
                    }
                    catch (System.Exception ex)
                    {
                        DebugLog($"AudioController: Error pausing category {category}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 恢复所有音频
        /// </summary>
        private void ResumeAllAudio()
        {
            // 确保AudioService可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot resume audio - AudioService not available");
                return;
            }

            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    try
                    {
                        AudioService.ResumeAllInCategory(category);
                    }
                    catch (System.Exception ex)
                    {
                        DebugLog($"AudioController: Error resuming category {category}: {ex.Message}");
                    }
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
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot play quick sound - AudioService not available");
                return null;
            }

            if (m_quickSounds.TryGetValue(soundName.ToLower(), out AudioClip clip))
            {
                try
                {
                    return AudioService.PlayOneShot(clip, AudioCategory.UI, volume);
                }
                catch (System.Exception ex)
                {
                    DebugLog($"AudioController: Error playing quick sound '{soundName}': {ex.Message}");
                    return null;
                }
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
                // 检查AudioService是否可用
                if (AudioService == null || !AudioService.IsInitialized)
                {
                    DebugLog("AudioController: AudioService not available, stopping audio sequence");
                    break;
                }

                var item = m_audioSequence.Dequeue();

                if (item.Clip != null)
                {
                    // 先尝试播放音频，然后在try-catch外部处理等待
                    AudioHandle handle = null;
                    bool playSuccess = false;

                    try
                    {
                        handle = AudioService.PlayOneShot(item.Clip, item.Category, item.Volume);
                        playSuccess = handle != null;
                    }
                    catch (System.Exception ex)
                    {
                        DebugLog($"AudioController: Error playing sequence item: {ex.Message}");
                        playSuccess = false;
                    }

                    // 在try-catch外部处理等待
                    if (playSuccess)
                    {
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
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot play with custom fade - AudioService not available");
                return null;
            }

            try
            {
                var handle = AudioService.PlayOneShot(clip, category, 0f);
                if (handle != null)
                {
                    StartCoroutine(CustomFadeCoroutine(handle, fadeCurve, duration));
                }
                return handle;
            }
            catch (System.Exception ex)
            {
                DebugLog($"AudioController: Error playing with custom fade: {ex.Message}");
                return null;
            }
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
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot play random sound - AudioService not available");
                return null;
            }

            if (clips == null || clips.Length == 0) return null;

            var randomClip = clips[Random.Range(0, clips.Length)];

            try
            {
                return AudioService.PlayOneShot(randomClip, category, volume);
            }
            catch (System.Exception ex)
            {
                DebugLog($"AudioController: Error playing random sound: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 播放音效并在指定时间后停止
        /// </summary>
        public AudioHandle PlayTimedSound(AudioClip clip, float duration, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot play timed sound - AudioService not available");
                return null;
            }

            try
            {
                var handle = AudioService.PlayLooped(clip, category, volume);
                if (handle != null)
                {
                    StartCoroutine(StopAfterDelay(handle, duration));
                }
                return handle;
            }
            catch (System.Exception ex)
            {
                DebugLog($"AudioController: Error playing timed sound: {ex.Message}");
                return null;
            }
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
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot smooth set category volume - AudioService not available");
                return;
            }

            StartCoroutine(SmoothVolumeCoroutine(category, targetVolume, duration));
        }

        /// <summary>
        /// 平滑音量变化协程
        /// </summary>
        private IEnumerator SmoothVolumeCoroutine(AudioCategory category, float targetVolume, float duration)
        {
            // 再次检查AudioService（协程可能在延迟后执行）
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: AudioService not available during smooth volume change");
                yield break;
            }

            float startVolume = AudioService.GetCategoryVolume(category);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 在循环中检查AudioService状态
                if (AudioService == null || !AudioService.IsInitialized)
                {
                    DebugLog("AudioController: AudioService became unavailable during smooth volume change");
                    yield break;
                }

                float t = elapsed / duration;
                float currentVolume = Mathf.Lerp(startVolume, targetVolume, t);

                // 尝试设置音量，如果失败则退出
                bool setSuccess = false;
                try
                {
                    AudioService.SetCategoryVolume(category, currentVolume);
                    setSuccess = true;
                }
                catch (System.Exception ex)
                {
                    DebugLog($"AudioController: Error during smooth volume change: {ex.Message}");
                    setSuccess = false;
                }

                // 如果设置失败，在try-catch外部退出
                if (!setSuccess)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 最终设置目标音量
            try
            {
                AudioService.SetCategoryVolume(category, targetVolume);
            }
            catch (System.Exception ex)
            {
                DebugLog($"AudioController: Error setting final volume: {ex.Message}");
            }
        }

        /// <summary>
        /// 临时降低指定分类音量
        /// </summary>
        public void DuckCategory(AudioCategory category, float duckVolume, float duration)
        {
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: Cannot duck category - AudioService not available");
                return;
            }

            StartCoroutine(DuckCategoryCoroutine(category, duckVolume, duration));
        }

        /// <summary>
        /// 音频闪避协程
        /// </summary>
        private IEnumerator DuckCategoryCoroutine(AudioCategory category, float duckVolume, float duration)
        {
            // 检查AudioService是否可用
            if (AudioService == null || !AudioService.IsInitialized)
            {
                DebugLog("AudioController: AudioService not available for ducking");
                yield break;
            }

            float originalVolume;
            try
            {
                originalVolume = AudioService.GetCategoryVolume(category);
                // 快速降低音量
                AudioService.SetCategoryVolume(category, duckVolume);
            }
            catch (System.Exception ex)
            {
                DebugLog($"AudioController: Error starting duck: {ex.Message}");
                yield break;
            }

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