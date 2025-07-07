using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单输入处理器
    /// 处理VR控制器输入并提供触觉、音频反馈
    /// </summary>
    public class MenuInputHandler : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private MainMenuController m_menuController;
        [SerializeField] private VRMenuInteraction m_menuInteraction;

        [Header("输入设置")]
        [SerializeField] private InputActionProperty m_menuAction;
        [SerializeField] private InputActionProperty m_backAction;
        [SerializeField] private InputActionProperty m_selectAction;

        [Header("触觉反馈")]
        [SerializeField] private bool m_enableHapticFeedback = true;
        [SerializeField] private float m_hapticAmplitude = 0.3f;
        [SerializeField] private float m_hapticDuration = 0.05f;
        [SerializeField] private float m_strongHapticAmplitude = 0.7f;
        [SerializeField] private float m_strongHapticDuration = 0.1f;
        [SerializeField] private float m_lightHapticAmplitude = 0.1f;
        [SerializeField] private float m_lightHapticDuration = 0.03f;

        [Header("音频反馈")]
        [SerializeField] private bool m_enableAudioFeedback = true;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_buttonClickSound;
        [SerializeField] private AudioClip m_menuOpenSound;
        [SerializeField] private AudioClip m_menuCloseSound;
        [SerializeField] private AudioClip m_hoverSound;
        [SerializeField] private AudioClip m_errorSound;
        [SerializeField] private float m_audioVolume = 0.5f;

        [Header("高级触觉设置")]
        [SerializeField] private bool m_useAdvancedHaptics = true;
        [SerializeField] [Range(0, 1)] private AnimationCurve m_buttonClickPattern;
        [SerializeField] [Range(0, 1)] private AnimationCurve m_menuNavigationPattern;
        [SerializeField] private float m_patternDuration = 0.2f;
        [SerializeField] private int m_patternSteps = 10;

        // 私有变量
        private bool m_isMenuPressed = false;
        private bool m_isBackPressed = false;

        // 协程引用
        private Coroutine m_leftHapticCoroutine;
        private Coroutine m_rightHapticCoroutine;

        #region Unity生命周期

        private void Awake()
        {
            // 初始化音频源
            InitializeAudioSource();
        }

        private void OnEnable()
        {
            // 启用输入动作
            EnableInputActions();
        }

        private void OnDisable()
        {
            // 禁用输入动作
            DisableInputActions();
        }

        private void Update()
        {
            // 处理输入状态
            ProcessInputState();
        }

        #endregion

        #region 公共方法 - 触觉反馈

        /// <summary>
        /// 提供触觉反馈
        /// </summary>
        public void ProvideFeedback(bool isLeftController, float amplitude, float duration)
        {
            if (!m_enableHapticFeedback) return;

            OVRInput.Controller controller = isLeftController ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

            // 使用OVR的振动API
            OVRInput.SetControllerVibration(amplitude, amplitude, controller);

            // 延迟停止振动
            StartCoroutine(StopVibrationAfterDelay(controller, duration));
        }

        /// <summary>
        /// 提供标准触觉反馈
        /// </summary>
        public void ProvideFeedback(bool isLeftController)
        {
            ProvideFeedback(isLeftController, m_hapticAmplitude, m_hapticDuration);
        }

        /// <summary>
        /// 为两个控制器提供反馈
        /// </summary>
        public void ProvideFeedbackBoth()
        {
            ProvideFeedback(true);
            ProvideFeedback(false);
        }

        /// <summary>
        /// 提供强烈触觉反馈
        /// </summary>
        public void ProvideStrongFeedback(bool isLeftController)
        {
            ProvideFeedback(isLeftController, m_strongHapticAmplitude, m_strongHapticDuration);
        }

        /// <summary>
        /// 为两个控制器提供强烈反馈
        /// </summary>
        public void ProvideStrongFeedbackBoth()
        {
            ProvideStrongFeedback(true);
            ProvideStrongFeedback(false);
        }

        /// <summary>
        /// 提供轻微触觉反馈
        /// </summary>
        public void ProvideLightFeedback(bool isLeftController)
        {
            ProvideFeedback(isLeftController, m_lightHapticAmplitude, m_lightHapticDuration);
        }

        /// <summary>
        /// 为两个控制器提供轻微反馈
        /// </summary>
        public void ProvideLightFeedbackBoth()
        {
            ProvideLightFeedback(true);
            ProvideLightFeedback(false);
        }

        /// <summary>
        /// 提供模式触觉反馈
        /// </summary>
        /// <param name="isLeftController">是否为左控制器</param>
        /// <param name="pattern">振动模式曲线</param>
        public void ProvidePatternFeedback(bool isLeftController, AnimationCurve pattern)
        {
            if (!m_enableHapticFeedback || !m_useAdvancedHaptics) return;

            // 停止当前协程
            if (isLeftController && m_leftHapticCoroutine != null)
            {
                StopCoroutine(m_leftHapticCoroutine);
            }
            else if (!isLeftController && m_rightHapticCoroutine != null)
            {
                StopCoroutine(m_rightHapticCoroutine);
            }

            // 启动新协程
            Coroutine hapticCoroutine = StartCoroutine(PlayHapticPattern(isLeftController, pattern));

            // 保存协程引用
            if (isLeftController)
            {
                m_leftHapticCoroutine = hapticCoroutine;
            }
            else
            {
                m_rightHapticCoroutine = hapticCoroutine;
            }
        }

        /// <summary>
        /// 提供按钮点击模式触觉反馈
        /// </summary>
        /// <param name="isLeftController">是否为左控制器</param>
        public void ProvideButtonClickFeedback(bool isLeftController)
        {
            if (m_useAdvancedHaptics && m_buttonClickPattern != null)
            {
                ProvidePatternFeedback(isLeftController, m_buttonClickPattern);
            }
            else
            {
                ProvideFeedback(isLeftController);
            }

            // 播放按钮点击音效
            PlaySound(m_buttonClickSound);
        }

        /// <summary>
        /// 提供菜单导航模式触觉反馈
        /// </summary>
        /// <param name="isLeftController">是否为左控制器</param>
        public void ProvideMenuNavigationFeedback(bool isLeftController)
        {
            if (m_useAdvancedHaptics && m_menuNavigationPattern != null)
            {
                ProvidePatternFeedback(isLeftController, m_menuNavigationPattern);
            }
            else
            {
                ProvideStrongFeedback(isLeftController);
            }

            // 播放菜单导航音效
            PlaySound(m_menuOpenSound);
        }

        /// <summary>
        /// 提供悬停触觉反馈
        /// </summary>
        /// <param name="isLeftController">是否为左控制器</param>
        public void ProvideHoverFeedback(bool isLeftController)
        {
            ProvideLightFeedback(isLeftController);

            // 播放悬停音效
            PlaySound(m_hoverSound, 0.2f);
        }

        /// <summary>
        /// 提供错误触觉反馈
        /// </summary>
        /// <param name="isLeftController">是否为左控制器</param>
        public void ProvideErrorFeedback(bool isLeftController)
        {
            // 错误反馈使用两次短促强烈振动
            StartCoroutine(PlayErrorHaptics(isLeftController));

            // 播放错误音效
            PlaySound(m_errorSound);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="clip">音效剪辑</param>
        /// <param name="volumeScale">音量缩放</param>
        public void PlaySound(AudioClip clip, float volumeScale = 1.0f)
        {
            if (!m_enableAudioFeedback || clip == null || m_audioSource == null) return;

            m_audioSource.PlayOneShot(clip, m_audioVolume * volumeScale);
        }

        /// <summary>
        /// 停止所有触觉反馈
        /// </summary>
        public void StopAllHaptics()
        {
            if (m_leftHapticCoroutine != null)
            {
                StopCoroutine(m_leftHapticCoroutine);
                m_leftHapticCoroutine = null;
            }

            if (m_rightHapticCoroutine != null)
            {
                StopCoroutine(m_rightHapticCoroutine);
                m_rightHapticCoroutine = null;
            }

            // 停止OVR输入的振动
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化音频源
        /// </summary>
        private void InitializeAudioSource()
        {
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
                if (m_audioSource == null)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                    m_audioSource.playOnAwake = false;
                    m_audioSource.spatialBlend = 0.0f; // 2D音效
                    m_audioSource.volume = m_audioVolume;
                }
            }
        }

        /// <summary>
        /// 启用输入动作
        /// </summary>
        private void EnableInputActions()
        {
            // 启用菜单按钮动作
            if (m_menuAction.action != null)
            {
                m_menuAction.action.performed += OnMenuActionPerformed;
                m_menuAction.action.canceled += OnMenuActionCanceled;
                m_menuAction.action.Enable();
            }

            // 启用返回按钮动作
            if (m_backAction.action != null)
            {
                m_backAction.action.performed += OnBackActionPerformed;
                m_backAction.action.canceled += OnBackActionCanceled;
                m_backAction.action.Enable();
            }

            // 启用选择按钮动作
            if (m_selectAction.action != null)
            {
                m_selectAction.action.performed += OnSelectActionPerformed;
                m_selectAction.action.Enable();
            }
        }

        /// <summary>
        /// 禁用输入动作
        /// </summary>
        private void DisableInputActions()
        {
            // 禁用菜单按钮动作
            if (m_menuAction.action != null)
            {
                m_menuAction.action.performed -= OnMenuActionPerformed;
                m_menuAction.action.canceled -= OnMenuActionCanceled;
                m_menuAction.action.Disable();
            }

            // 禁用返回按钮动作
            if (m_backAction.action != null)
            {
                m_backAction.action.performed -= OnBackActionPerformed;
                m_backAction.action.canceled -= OnBackActionCanceled;
                m_backAction.action.Disable();
            }

            // 禁用选择按钮动作
            if (m_selectAction.action != null)
            {
                m_selectAction.action.performed -= OnSelectActionPerformed;
                m_selectAction.action.Disable();
            }
        }

        /// <summary>
        /// 处理输入状态
        /// </summary>
        private void ProcessInputState()
        {
            // 处理菜单按钮状态
            if (m_isMenuPressed)
            {
                // 菜单按钮被按下的持续处理
            }

            // 处理返回按钮状态
            if (m_isBackPressed)
            {
                // 返回按钮被按下的持续处理
            }

            // 处理其他输入状态...
        }

        /// <summary>
        /// 播放触觉模式
        /// </summary>
        private IEnumerator PlayHapticPattern(bool isLeftController, AnimationCurve pattern)
        {
            OVRInput.Controller controller = isLeftController ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            if (controller == OVRInput.Controller.None) yield break;

            float timeStep = m_patternDuration / m_patternSteps;

            for (int i = 0; i < m_patternSteps; i++)
            {
                float t = (float)i / (m_patternSteps - 1);
                float amplitude = pattern.Evaluate(t);

                OVRInput.SetControllerVibration(amplitude, amplitude, controller);
                yield return new WaitForSeconds(timeStep);
            }

            // 重置协程引用
            if (isLeftController)
            {
                m_leftHapticCoroutine = null;
            }
            else
            {
                m_rightHapticCoroutine = null;
            }
        }

        /// <summary>
        /// 播放错误触觉反馈
        /// </summary>
        private IEnumerator PlayErrorHaptics(bool isLeftController)
        {
            // 第一次振动
            ProvideFeedback(isLeftController, m_strongHapticAmplitude, m_strongHapticDuration / 2);
            yield return new WaitForSeconds(0.1f);

            // 第二次振动
            ProvideFeedback(isLeftController, m_strongHapticAmplitude, m_strongHapticDuration / 2);
        }

        /// <summary>
        /// 延迟停止振动
        /// </summary>
        private IEnumerator StopVibrationAfterDelay(OVRInput.Controller controller, float delay)
        {
            yield return new WaitForSeconds(delay);
            OVRInput.SetControllerVibration(0, 0, controller);
        }

        #endregion

        #region 输入事件处理

        /// <summary>
        /// 菜单按钮按下事件
        /// </summary>
        private void OnMenuActionPerformed(InputAction.CallbackContext context)
        {
            m_isMenuPressed = true;

            // 切换菜单显示状态
            if (m_menuController != null)
            {
                bool wasMenuVisible = m_menuController.IsMenuVisible;
                m_menuController.ToggleMenu();

                // 根据菜单状态播放不同音效
                if (wasMenuVisible)
                {
                    PlaySound(m_menuCloseSound);
                }
                else
                {
                    PlaySound(m_menuOpenSound);
                }

                // 提供触觉反馈
                ProvideMenuNavigationFeedback(context.control.device.name.ToLower().Contains("left"));
            }
        }

        /// <summary>
        /// 菜单按钮释放事件
        /// </summary>
        private void OnMenuActionCanceled(InputAction.CallbackContext context)
        {
            m_isMenuPressed = false;
        }

        /// <summary>
        /// 返回按钮按下事件
        /// </summary>
        private void OnBackActionPerformed(InputAction.CallbackContext context)
        {
            m_isBackPressed = true;

            // 返回上一级菜单
            if (m_menuController != null)
            {
                m_menuController.GoBack();

                // 播放菜单关闭音效
                PlaySound(m_menuCloseSound);

                // 提供触觉反馈
                bool isLeftHand = context.control.device.name.ToLower().Contains("left");
                ProvideButtonClickFeedback(isLeftHand);
            }
        }

        /// <summary>
        /// 返回按钮释放事件
        /// </summary>
        private void OnBackActionCanceled(InputAction.CallbackContext context)
        {
            m_isBackPressed = false;
        }

        /// <summary>
        /// 选择按钮按下事件
        /// </summary>
        private void OnSelectActionPerformed(InputAction.CallbackContext context)
        {
            // 处理选择事件
            if (m_menuInteraction != null)
            {
                m_menuInteraction.TriggerSelection();

                // 确定控制器类型并提供反馈
                bool isLeftHand = context.control.device.name.ToLower().Contains("left");
                ProvideButtonClickFeedback(isLeftHand);
            }
        }

        #endregion
    }
}