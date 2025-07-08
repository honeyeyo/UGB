using UnityEngine;
using System.Collections;
using PongHub.Core.Audio;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// VR触觉反馈控制器
    /// 为模式选择界面提供丰富的触觉反馈体验
    /// </summary>
    public class VRHapticFeedback : MonoBehaviour
    {
        [Header("基础触觉配置")]
        [SerializeField] private float m_lightIntensity = 0.3f;
        [SerializeField] private float m_mediumIntensity = 0.6f;
        [SerializeField] private float m_strongIntensity = 1.0f;
        [SerializeField] private float m_shortDuration = 0.1f;
        [SerializeField] private float m_mediumDuration = 0.2f;
        [SerializeField] private float m_longDuration = 0.4f;

        [Header("模式选择触觉")]
        [SerializeField] private float m_modeHoverIntensity = 0.4f;
        [SerializeField] private float m_modeHoverDuration = 0.15f;
        [SerializeField] private float m_modeSelectIntensity = 0.8f;
        [SerializeField] private float m_modeSelectDuration = 0.25f;
        [SerializeField] private float m_modeConfirmIntensity = 1.0f;
        [SerializeField] private float m_modeConfirmDuration = 0.3f;

        [Header("错误和警告触觉")]
        [SerializeField] private float m_errorIntensity = 0.9f;
        [SerializeField] private float m_errorDuration = 0.5f;
        [SerializeField] private int m_errorPulseCount = 3;
        [SerializeField] private float m_errorPulseInterval = 0.1f;
        [SerializeField] private float m_warningIntensity = 0.6f;
        [SerializeField] private float m_warningDuration = 0.2f;

        [Header("过渡和动画触觉")]
        [SerializeField] private float m_transitionIntensity = 0.5f;
        [SerializeField] private float m_transitionDuration = 0.3f;
        [SerializeField] private float m_pageChangeIntensity = 0.7f;
        [SerializeField] private float m_pageChangeDuration = 0.25f;

        [Header("快速操作触觉")]
        [SerializeField] private float m_quickStartIntensity = 0.6f;
        [SerializeField] private float m_quickStartDuration = 0.2f;
        [SerializeField] private float m_backButtonIntensity = 0.4f;
        [SerializeField] private float m_backButtonDuration = 0.15f;

        // 触觉反馈类型
        public enum HapticType
        {
            Light,          // 轻微反馈
            Medium,         // 中等反馈
            Strong,         // 强烈反馈
            ModeHover,      // 模式悬停
            ModeSelect,     // 模式选择
            ModeConfirm,    // 模式确认
            Error,          // 错误反馈
            Warning,        // 警告反馈
            Transition,     // 过渡效果
            PageChange,     // 页面切换
            QuickStart,     // 快速开始
            Back,           // 返回操作
            Custom          // 自定义反馈
        }

        // OVR控制器引用
        private OVRInput.Controller m_leftController = OVRInput.Controller.LTouch;
        private OVRInput.Controller m_rightController = OVRInput.Controller.RTouch;

        // 当前触觉协程
        private Coroutine m_currentLeftHaptic;
        private Coroutine m_currentRightHaptic;

        private bool m_isInitialized = false;

        #region Unity生命周期

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            StopAllHaptics();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化触觉反馈系统
        /// </summary>
        private void Initialize()
        {
            if (m_isInitialized) return;

            // 检查OVR插件是否可用
            if (!OVRPlugin.initialized)
            {
                Debug.LogWarning("OVR Plugin not initialized. Haptic feedback will be disabled.");
                return;
            }

            m_isInitialized = true;
            Debug.Log("VRHapticFeedback initialized successfully");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 播放指定类型的触觉反馈
        /// </summary>
        /// <param name="hapticType">触觉类型</param>
        /// <param name="isLeftHand">是否为左手</param>
        public void PlayHaptic(HapticType hapticType, bool isLeftHand = false)
        {
            if (!m_isInitialized) return;

            switch (hapticType)
            {
                case HapticType.Light:
                    TriggerHaptic(isLeftHand, m_lightIntensity, m_shortDuration);
                    break;
                case HapticType.Medium:
                    TriggerHaptic(isLeftHand, m_mediumIntensity, m_mediumDuration);
                    break;
                case HapticType.Strong:
                    TriggerHaptic(isLeftHand, m_strongIntensity, m_longDuration);
                    break;
                case HapticType.ModeHover:
                    TriggerHaptic(isLeftHand, m_modeHoverIntensity, m_modeHoverDuration);
                    break;
                case HapticType.ModeSelect:
                    TriggerHaptic(isLeftHand, m_modeSelectIntensity, m_modeSelectDuration);
                    break;
                case HapticType.ModeConfirm:
                    TriggerHaptic(isLeftHand, m_modeConfirmIntensity, m_modeConfirmDuration);
                    break;
                case HapticType.Error:
                    StartErrorHaptic(isLeftHand);
                    break;
                case HapticType.Warning:
                    TriggerHaptic(isLeftHand, m_warningIntensity, m_warningDuration);
                    break;
                case HapticType.Transition:
                    TriggerHaptic(isLeftHand, m_transitionIntensity, m_transitionDuration);
                    break;
                case HapticType.PageChange:
                    TriggerHaptic(isLeftHand, m_pageChangeIntensity, m_pageChangeDuration);
                    break;
                case HapticType.QuickStart:
                    TriggerHaptic(isLeftHand, m_quickStartIntensity, m_quickStartDuration);
                    break;
                case HapticType.Back:
                    TriggerHaptic(isLeftHand, m_backButtonIntensity, m_backButtonDuration);
                    break;
            }
        }

        /// <summary>
        /// 播放自定义触觉反馈
        /// </summary>
        /// <param name="intensity">强度 (0-1)</param>
        /// <param name="duration">持续时间 (秒)</param>
        /// <param name="isLeftHand">是否为左手</param>
        public void PlayCustomHaptic(float intensity, float duration, bool isLeftHand = false)
        {
            TriggerHaptic(isLeftHand, intensity, duration);
        }

        /// <summary>
        /// 播放脉冲触觉反馈
        /// </summary>
        /// <param name="intensity">强度 (0-1)</param>
        /// <param name="pulseCount">脉冲次数</param>
        /// <param name="pulseInterval">脉冲间隔 (秒)</param>
        /// <param name="isLeftHand">是否为左手</param>
        public void PlayPulseHaptic(float intensity, int pulseCount, float pulseInterval, bool isLeftHand = false)
        {
            if (!m_isInitialized) return;

            if (isLeftHand)
            {
                if (m_currentLeftHaptic != null)
                    StopCoroutine(m_currentLeftHaptic);
                m_currentLeftHaptic = StartCoroutine(PulseHapticCoroutine(true, intensity, pulseCount, pulseInterval));
            }
            else
            {
                if (m_currentRightHaptic != null)
                    StopCoroutine(m_currentRightHaptic);
                m_currentRightHaptic = StartCoroutine(PulseHapticCoroutine(false, intensity, pulseCount, pulseInterval));
            }
        }

        /// <summary>
        /// 同时触发双手触觉反馈
        /// </summary>
        /// <param name="hapticType">触觉类型</param>
        public void PlayBilateralHaptic(HapticType hapticType)
        {
            PlayHaptic(hapticType, true);   // 左手
            PlayHaptic(hapticType, false);  // 右手
        }

        /// <summary>
        /// 停止所有触觉反馈
        /// </summary>
        public void StopAllHaptics()
        {
            if (!m_isInitialized) return;

            // 停止协程
            if (m_currentLeftHaptic != null)
            {
                StopCoroutine(m_currentLeftHaptic);
                m_currentLeftHaptic = null;
            }

            if (m_currentRightHaptic != null)
            {
                StopCoroutine(m_currentRightHaptic);
                m_currentRightHaptic = null;
            }

            // 停止OVR触觉
            OVRInput.SetControllerVibration(0, 0, m_leftController);
            OVRInput.SetControllerVibration(0, 0, m_rightController);
        }

        /// <summary>
        /// 停止指定手的触觉反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void StopHaptic(bool isLeftHand)
        {
            if (!m_isInitialized) return;

            if (isLeftHand)
            {
                if (m_currentLeftHaptic != null)
                {
                    StopCoroutine(m_currentLeftHaptic);
                    m_currentLeftHaptic = null;
                }
                OVRInput.SetControllerVibration(0, 0, m_leftController);
            }
            else
            {
                if (m_currentRightHaptic != null)
                {
                    StopCoroutine(m_currentRightHaptic);
                    m_currentRightHaptic = null;
                }
                OVRInput.SetControllerVibration(0, 0, m_rightController);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 触发基础触觉反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        /// <param name="intensity">强度 (0-1)</param>
        /// <param name="duration">持续时间 (秒)</param>
        private void TriggerHaptic(bool isLeftHand, float intensity, float duration)
        {
            if (!m_isInitialized) return;

            // 限制参数范围
            intensity = Mathf.Clamp01(intensity);
            duration = Mathf.Max(0, duration);

            if (isLeftHand)
            {
                if (m_currentLeftHaptic != null)
                    StopCoroutine(m_currentLeftHaptic);
                m_currentLeftHaptic = StartCoroutine(HapticCoroutine(true, intensity, duration));
            }
            else
            {
                if (m_currentRightHaptic != null)
                    StopCoroutine(m_currentRightHaptic);
                m_currentRightHaptic = StartCoroutine(HapticCoroutine(false, intensity, duration));
            }
        }

        /// <summary>
        /// 触觉反馈协程
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        /// <param name="intensity">强度</param>
        /// <param name="duration">持续时间</param>
        private IEnumerator HapticCoroutine(bool isLeftHand, float intensity, float duration)
        {
            OVRInput.Controller controller = isLeftHand ? m_leftController : m_rightController;

            // 开始触觉反馈
            OVRInput.SetControllerVibration(intensity, intensity, controller);

            // 等待指定时间
            yield return new WaitForSeconds(duration);

            // 停止触觉反馈
            OVRInput.SetControllerVibration(0, 0, controller);

            // 清理协程引用
            if (isLeftHand)
                m_currentLeftHaptic = null;
            else
                m_currentRightHaptic = null;
        }

        /// <summary>
        /// 脉冲触觉反馈协程
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        /// <param name="intensity">强度</param>
        /// <param name="pulseCount">脉冲次数</param>
        /// <param name="pulseInterval">脉冲间隔</param>
        private IEnumerator PulseHapticCoroutine(bool isLeftHand, float intensity, int pulseCount, float pulseInterval)
        {
            OVRInput.Controller controller = isLeftHand ? m_leftController : m_rightController;

            for (int i = 0; i < pulseCount; i++)
            {
                // 触觉脉冲
                OVRInput.SetControllerVibration(intensity, intensity, controller);
                yield return new WaitForSeconds(0.05f); // 短暂的脉冲

                OVRInput.SetControllerVibration(0, 0, controller);

                // 脉冲间隔
                if (i < pulseCount - 1) // 最后一个脉冲后不需要等待
                    yield return new WaitForSeconds(pulseInterval);
            }

            // 清理协程引用
            if (isLeftHand)
                m_currentLeftHaptic = null;
            else
                m_currentRightHaptic = null;
        }

        /// <summary>
        /// 启动错误触觉反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        private void StartErrorHaptic(bool isLeftHand)
        {
            PlayPulseHaptic(m_errorIntensity, m_errorPulseCount, m_errorPulseInterval, isLeftHand);
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 模式卡片悬停反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnModeHover(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.ModeHover, isLeftHand);
        }

        /// <summary>
        /// 模式选择反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnModeSelect(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.ModeSelect, isLeftHand);
        }

        /// <summary>
        /// 模式确认反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnModeConfirm(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.ModeConfirm, isLeftHand);
        }

        /// <summary>
        /// 页面过渡反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnPageTransition(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.Transition, isLeftHand);
        }

        /// <summary>
        /// 错误操作反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnError(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.Error, isLeftHand);
        }

        /// <summary>
        /// 返回按钮反馈
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        public void OnBack(bool isLeftHand = false)
        {
            PlayHaptic(HapticType.Back, isLeftHand);
        }

        #endregion
    }
}