using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using PongHub.Core.Audio;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式选择音效和触觉反馈管理器
    /// 为模式选择界面提供丰富的音效和触觉反馈体验
    /// </summary>
    public class ModeSelectionAudioHaptics : MonoBehaviour
    {
        [Header("音效配置")]
        [SerializeField] private AudioMixerGroup m_uiAudioMixerGroup;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private float m_masterVolume = 1.0f;
        [SerializeField] private float m_uiVolume = 0.8f;
        [SerializeField] private float m_feedbackVolume = 0.6f;

        [Header("界面音效")]
        [SerializeField] private AudioClip m_panelOpenSound;
        [SerializeField] private AudioClip m_panelCloseSound;
        [SerializeField] private AudioClip m_buttonClickSound;
        [SerializeField] private AudioClip m_buttonHoverSound;
        [SerializeField] private AudioClip m_cardSelectSound;
        [SerializeField] private AudioClip m_cardHoverSound;
        [SerializeField] private AudioClip m_backButtonSound;

        [Header("模式切换音效")]
        [SerializeField] private AudioClip m_modeTransitionSound;
        [SerializeField] private AudioClip m_modeConfirmSound;
        [SerializeField] private AudioClip m_modeCancelSound;
        [SerializeField] private AudioClip m_quickStartSound;
        [SerializeField] private AudioClip m_difficultyChangeSound;

        [Header("反馈音效")]
        [SerializeField] private AudioClip m_successSound;
        [SerializeField] private AudioClip m_errorSound;
        [SerializeField] private AudioClip m_warningSound;
        [SerializeField] private AudioClip m_connectionSound;
        [SerializeField] private AudioClip m_disconnectionSound;

        [Header("触觉反馈配置")]
        [SerializeField] private bool m_enableHapticFeedback = true;
        [SerializeField] private float m_lightHapticIntensity = 0.3f;
        [SerializeField] private float m_mediumHapticIntensity = 0.6f;
        [SerializeField] private float m_strongHapticIntensity = 1.0f;
        [SerializeField] private float m_hapticDuration = 0.1f;

        [Header("音效组")]
        [SerializeField] private AudioClip[] m_positiveAudioClips;
        [SerializeField] private AudioClip[] m_negativeAudioClips;
        [SerializeField] private AudioClip[] m_neutralAudioClips;

        // 音效类型
        public enum AudioType
        {
            // 界面音效
            PanelOpen,
            PanelClose,
            ButtonClick,
            ButtonHover,
            CardSelect,
            CardHover,
            BackButton,

            // 模式切换
            ModeTransition,
            ModeConfirm,
            ModeCancel,
            QuickStart,
            DifficultyChange,

            // 反馈音效
            Success,
            Error,
            Warning,
            Connection,
            Disconnection,

            // 音效组
            Positive,
            Negative,
            Neutral
        }

        // 触觉反馈类型
        public enum HapticType
        {
            Light,      // 轻微触觉
            Medium,     // 中等触觉
            Strong,     // 强烈触觉
            Pulse,      // 脉冲触觉
            Continuous  // 连续触觉
        }

        // 触觉反馈模式
        public enum HapticPattern
        {
            Single,     // 单次触觉
            Double,     // 双次触觉
            Triple,     // 三次触觉
            Rhythm,     // 节奏触觉
            Fade        // 渐变触觉
        }

        private Dictionary<AudioType, AudioClip> m_audioClips = new Dictionary<AudioType, AudioClip>();
        private Coroutine m_currentHapticCoroutine;

        private bool m_isInitialized = false;

        // VR 触觉反馈控制器
        private OVRInput.Controller m_leftController = OVRInput.Controller.LTouch;
        private OVRInput.Controller m_rightController = OVRInput.Controller.RTouch;

        /// <summary>
        /// 初始化音效和触觉反馈系统
        /// </summary>
        private void Start()
        {
            InitializeComponents();
            SetupAudioClips();
            ConfigureAudioSource();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            if (m_isInitialized) return;

            // 创建音频源（如果不存在）
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            m_isInitialized = true;
        }

        /// <summary>
        /// 设置音效剪辑映射
        /// </summary>
        private void SetupAudioClips()
        {
            m_audioClips.Clear();

            // 界面音效
            m_audioClips[AudioType.PanelOpen] = m_panelOpenSound;
            m_audioClips[AudioType.PanelClose] = m_panelCloseSound;
            m_audioClips[AudioType.ButtonClick] = m_buttonClickSound;
            m_audioClips[AudioType.ButtonHover] = m_buttonHoverSound;
            m_audioClips[AudioType.CardSelect] = m_cardSelectSound;
            m_audioClips[AudioType.CardHover] = m_cardHoverSound;
            m_audioClips[AudioType.BackButton] = m_backButtonSound;

            // 模式切换音效
            m_audioClips[AudioType.ModeTransition] = m_modeTransitionSound;
            m_audioClips[AudioType.ModeConfirm] = m_modeConfirmSound;
            m_audioClips[AudioType.ModeCancel] = m_modeCancelSound;
            m_audioClips[AudioType.QuickStart] = m_quickStartSound;
            m_audioClips[AudioType.DifficultyChange] = m_difficultyChangeSound;

            // 反馈音效
            m_audioClips[AudioType.Success] = m_successSound;
            m_audioClips[AudioType.Error] = m_errorSound;
            m_audioClips[AudioType.Warning] = m_warningSound;
            m_audioClips[AudioType.Connection] = m_connectionSound;
            m_audioClips[AudioType.Disconnection] = m_disconnectionSound;
        }

        /// <summary>
        /// 配置音频源
        /// </summary>
        private void ConfigureAudioSource()
        {
            if (m_audioSource == null) return;

            m_audioSource.outputAudioMixerGroup = m_uiAudioMixerGroup;
            m_audioSource.playOnAwake = false;
            m_audioSource.loop = false;
            m_audioSource.volume = m_uiVolume;
            m_audioSource.pitch = 1.0f;
            m_audioSource.spatialBlend = 0.0f; // 2D 音效
            m_audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        public void PlayAudio(AudioType audioType, float volumeMultiplier = 1.0f)
        {
            if (!m_audioClips.ContainsKey(audioType))
            {
                Debug.LogWarning($"Audio clip not found for type: {audioType}");
                return;
            }

            AudioClip clip = m_audioClips[audioType];
            if (clip == null) return;

            // 根据音效类型调整音量
            float volume = GetVolumeForAudioType(audioType) * volumeMultiplier * m_masterVolume;

            if (m_audioSource != null)
            {
                m_audioSource.volume = volume;
                m_audioSource.clip = clip;
                m_audioSource.Play();
            }
        }

        /// <summary>
        /// 播放随机音效组
        /// </summary>
        public void PlayRandomAudio(AudioType audioType, float volumeMultiplier = 1.0f)
        {
            AudioClip[] clips = GetAudioClipsForType(audioType);
            if (clips == null || clips.Length == 0) return;

            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            if (randomClip == null) return;

            float volume = GetVolumeForAudioType(audioType) * volumeMultiplier * m_masterVolume;

            if (m_audioSource != null)
            {
                m_audioSource.volume = volume;
                m_audioSource.clip = randomClip;
                m_audioSource.Play();
            }
        }

        /// <summary>
        /// 播放音效（一次性）
        /// </summary>
        public void PlayAudioOneShot(AudioType audioType, float volumeMultiplier = 1.0f)
        {
            if (!m_audioClips.ContainsKey(audioType))
            {
                Debug.LogWarning($"Audio clip not found for type: {audioType}");
                return;
            }

            AudioClip clip = m_audioClips[audioType];
            if (clip == null) return;

            float volume = GetVolumeForAudioType(audioType) * volumeMultiplier * m_masterVolume;

            if (m_audioSource != null)
            {
                m_audioSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// 提供触觉反馈
        /// </summary>
        public void ProvideFeedback(bool isLeftHand, HapticType hapticType, HapticPattern pattern = HapticPattern.Single)
        {
            if (!m_enableHapticFeedback) return;

            if (m_currentHapticCoroutine != null)
            {
                StopCoroutine(m_currentHapticCoroutine);
            }

            m_currentHapticCoroutine = StartCoroutine(HapticFeedbackCoroutine(isLeftHand, hapticType, pattern));
        }

        /// <summary>
        /// 触觉反馈协程
        /// </summary>
        private IEnumerator HapticFeedbackCoroutine(bool isLeftHand, HapticType hapticType, HapticPattern pattern)
        {
            OVRInput.Controller controller = isLeftHand ? m_leftController : m_rightController;
            float intensity = GetHapticIntensity(hapticType);

            switch (pattern)
            {
                case HapticPattern.Single:
                    yield return StartCoroutine(SingleHaptic(controller, intensity));
                    break;
                case HapticPattern.Double:
                    yield return StartCoroutine(DoubleHaptic(controller, intensity));
                    break;
                case HapticPattern.Triple:
                    yield return StartCoroutine(TripleHaptic(controller, intensity));
                    break;
                case HapticPattern.Rhythm:
                    yield return StartCoroutine(RhythmHaptic(controller, intensity));
                    break;
                case HapticPattern.Fade:
                    yield return StartCoroutine(FadeHaptic(controller, intensity));
                    break;
            }
        }

        /// <summary>
        /// 单次触觉反馈
        /// </summary>
        private IEnumerator SingleHaptic(OVRInput.Controller controller, float intensity)
        {
            OVRInput.SetControllerVibration(0, intensity, controller);
            yield return new WaitForSeconds(m_hapticDuration);
            OVRInput.SetControllerVibration(0, 0, controller);
        }

        /// <summary>
        /// 双次触觉反馈
        /// </summary>
        private IEnumerator DoubleHaptic(OVRInput.Controller controller, float intensity)
        {
            // 第一次
            OVRInput.SetControllerVibration(0, intensity, controller);
            yield return new WaitForSeconds(m_hapticDuration);
            OVRInput.SetControllerVibration(0, 0, controller);

            // 间隔
            yield return new WaitForSeconds(0.1f);

            // 第二次
            OVRInput.SetControllerVibration(0, intensity, controller);
            yield return new WaitForSeconds(m_hapticDuration);
            OVRInput.SetControllerVibration(0, 0, controller);
        }

        /// <summary>
        /// 三次触觉反馈
        /// </summary>
        private IEnumerator TripleHaptic(OVRInput.Controller controller, float intensity)
        {
            for (int i = 0; i < 3; i++)
            {
                OVRInput.SetControllerVibration(0, intensity, controller);
                yield return new WaitForSeconds(m_hapticDuration);
                OVRInput.SetControllerVibration(0, 0, controller);

                if (i < 2)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        /// <summary>
        /// 节奏触觉反馈
        /// </summary>
        private IEnumerator RhythmHaptic(OVRInput.Controller controller, float intensity)
        {
            // 节奏模式：长-短-短-长
            float[] durations = { 0.2f, 0.1f, 0.1f, 0.2f };

            for (int i = 0; i < durations.Length; i++)
            {
                OVRInput.SetControllerVibration(0, intensity, controller);
                yield return new WaitForSeconds(durations[i]);
                OVRInput.SetControllerVibration(0, 0, controller);

                if (i < durations.Length - 1)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        /// <summary>
        /// 渐变触觉反馈
        /// </summary>
        private IEnumerator FadeHaptic(OVRInput.Controller controller, float intensity)
        {
            float fadeDuration = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentIntensity = intensity * (1f - elapsedTime / fadeDuration);
                OVRInput.SetControllerVibration(0, currentIntensity, controller);
                yield return null;
            }

            OVRInput.SetControllerVibration(0, 0, controller);
        }

        /// <summary>
        /// 组合音效和触觉反馈
        /// </summary>
        public void PlayAudioWithHaptics(AudioType audioType, bool isLeftHand, HapticType hapticType,
            HapticPattern pattern = HapticPattern.Single, float volumeMultiplier = 1.0f)
        {
            // 播放音效
            PlayAudioOneShot(audioType, volumeMultiplier);

            // 提供触觉反馈
            ProvideFeedback(isLeftHand, hapticType, pattern);
        }

        /// <summary>
        /// 按钮点击反馈
        /// </summary>
        public void OnButtonClick(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.ButtonClick, isLeftHand, HapticType.Light, HapticPattern.Single);
        }

        /// <summary>
        /// 按钮悬停反馈
        /// </summary>
        public void OnButtonHover(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.ButtonHover, isLeftHand, HapticType.Light, HapticPattern.Single, 0.5f);
        }

        /// <summary>
        /// 卡片选择反馈
        /// </summary>
        public void OnCardSelect(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.CardSelect, isLeftHand, HapticType.Medium, HapticPattern.Double);
        }

        /// <summary>
        /// 卡片悬停反馈
        /// </summary>
        public void OnCardHover(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.CardHover, isLeftHand, HapticType.Light, HapticPattern.Single, 0.7f);
        }

        /// <summary>
        /// 模式切换反馈
        /// </summary>
        public void OnModeTransition(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.ModeTransition, isLeftHand, HapticType.Strong, HapticPattern.Rhythm);
        }

        /// <summary>
        /// 错误反馈
        /// </summary>
        public void OnError(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.Error, isLeftHand, HapticType.Strong, HapticPattern.Triple);
        }

        /// <summary>
        /// 成功反馈
        /// </summary>
        public void OnSuccess(bool isLeftHand)
        {
            PlayAudioWithHaptics(AudioType.Success, isLeftHand, HapticType.Medium, HapticPattern.Fade);
        }

        /// <summary>
        /// 获取音效类型对应的音量
        /// </summary>
        private float GetVolumeForAudioType(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.ButtonHover:
                case AudioType.CardHover:
                    return m_feedbackVolume * 0.7f;

                case AudioType.ButtonClick:
                case AudioType.CardSelect:
                    return m_feedbackVolume;

                case AudioType.ModeTransition:
                case AudioType.ModeConfirm:
                    return m_uiVolume;

                case AudioType.Error:
                case AudioType.Warning:
                    return m_uiVolume * 1.2f;

                default:
                    return m_uiVolume;
            }
        }

        /// <summary>
        /// 获取触觉反馈强度
        /// </summary>
        private float GetHapticIntensity(HapticType hapticType)
        {
            switch (hapticType)
            {
                case HapticType.Light:
                    return m_lightHapticIntensity;
                case HapticType.Medium:
                    return m_mediumHapticIntensity;
                case HapticType.Strong:
                    return m_strongHapticIntensity;
                default:
                    return m_mediumHapticIntensity;
            }
        }

        /// <summary>
        /// 获取音效组剪辑
        /// </summary>
        private AudioClip[] GetAudioClipsForType(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.Positive:
                    return m_positiveAudioClips;
                case AudioType.Negative:
                    return m_negativeAudioClips;
                case AudioType.Neutral:
                    return m_neutralAudioClips;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置UI音量
        /// </summary>
        public void SetUIVolume(float volume)
        {
            m_uiVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置反馈音量
        /// </summary>
        public void SetFeedbackVolume(float volume)
        {
            m_feedbackVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 启用/禁用触觉反馈
        /// </summary>
        public void SetHapticFeedbackEnabled(bool enabled)
        {
            m_enableHapticFeedback = enabled;
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllAudio()
        {
            if (m_audioSource != null)
            {
                m_audioSource.Stop();
            }
        }

        /// <summary>
        /// 停止所有触觉反馈
        /// </summary>
        public void StopAllHaptics()
        {
            if (m_currentHapticCoroutine != null)
            {
                StopCoroutine(m_currentHapticCoroutine);
                m_currentHapticCoroutine = null;
            }

            // 停止所有控制器震动
            OVRInput.SetControllerVibration(0, 0, m_leftController);
            OVRInput.SetControllerVibration(0, 0, m_rightController);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            StopAllAudio();
            StopAllHaptics();
        }

        /// <summary>
        /// 应用失焦时停止所有反馈
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                StopAllHaptics();
            }
        }

        /// <summary>
        /// 应用暂停时停止所有反馈
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopAllHaptics();
            }
        }
    }
}