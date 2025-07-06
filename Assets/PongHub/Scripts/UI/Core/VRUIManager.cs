using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;

namespace PongHub.UI.Core
{
    /// <summary>
    /// VR UI管理器
    /// 负责管理UI组件、主题和交互
    /// </summary>
    public class VRUIManager : MonoBehaviour
    {
        [Header("主题设置")]
        [SerializeField]
        [Tooltip("Default Theme / 默认主题 - Default theme for UI components")]
        private VRUITheme m_defaultTheme;

        [SerializeField]
        [Tooltip("Auto Apply Theme / 自动应用主题 - Automatically apply theme to new components")]
        private bool m_autoApplyTheme = true;

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("Haptic Feedback Enabled / 启用触觉反馈 - Enable haptic feedback for interactions")]
        private bool m_hapticFeedbackEnabled = true;

        // 移除未使用的字段或添加注释说明保留这些字段用于将来功能
        // 保留这些字段用于将来实现音频和视觉反馈功能
#pragma warning disable 0414
        [SerializeField]
        [Tooltip("Audio Feedback Enabled / 启用音频反馈 - Enable audio feedback for interactions")]
        private bool m_audioFeedbackEnabled = true;

        [SerializeField]
        [Tooltip("Visual Feedback Enabled / 启用视觉反馈 - Enable visual feedback for interactions")]
        private bool m_visualFeedbackEnabled = true;
#pragma warning restore 0414

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging and visualization")]
        private bool m_debugMode = false;

        // 单例实例
        private static VRUIManager s_instance;
        public static VRUIManager Instance => s_instance;

        // 当前主题
        private VRUITheme m_currentTheme;

        // 注册的组件列表
        private readonly List<VRUIComponent> m_registeredComponents = new List<VRUIComponent>();

        // XR控制器设备
        private InputDevice m_leftController;
        private InputDevice m_rightController;
        private bool m_controllersInitialized = false;

        #region Unity生命周期

        private void Awake()
        {
            // 单例模式实现
            if (s_instance != null && s_instance != this)
            {
                Debug.LogWarning("[VRUIManager] 检测到重复实例，销毁 " + gameObject.name);
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化主题
            InitializeTheme();

            if (m_debugMode)
            {
                Debug.Log("[VRUIManager] 初始化完成");
            }
        }

        private void Start()
        {
            // 初始化XR控制器
            InitializeControllers();
        }

        private void Update()
        {
            // 如果控制器未初始化，尝试初始化
            if (!m_controllersInitialized)
            {
                InitializeControllers();
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 注册UI组件
        /// </summary>
        public void RegisterComponent(VRUIComponent component)
        {
            if (component == null)
            {
                Debug.LogError("[VRUIManager] 尝试注册空组件");
                return;
            }

            if (!m_registeredComponents.Contains(component))
            {
                m_registeredComponents.Add(component);

                // 自动应用主题
                if (m_autoApplyTheme && m_currentTheme != null)
                {
                    component.SetTheme(m_currentTheme);
                }

                if (m_debugMode)
                {
                    Debug.Log($"[VRUIManager] 注册组件: {component.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// 注销UI组件
        /// </summary>
        public void UnregisterComponent(VRUIComponent component)
        {
            if (component == null)
            {
                return;
            }

            if (m_registeredComponents.Remove(component) && m_debugMode)
            {
                Debug.Log($"[VRUIManager] 注销组件: {component.GetType().Name}");
            }
        }

        /// <summary>
        /// 获取当前主题
        /// </summary>
        public VRUITheme GetCurrentTheme()
        {
            return m_currentTheme;
        }

        /// <summary>
        /// 设置当前主题
        /// </summary>
        public void SetTheme(VRUITheme theme)
        {
            if (theme == null)
            {
                Debug.LogError("[VRUIManager] 尝试设置空主题");
                return;
            }

            m_currentTheme = theme;

            // 应用到所有注册的组件
            foreach (var component in m_registeredComponents)
            {
                if (component != null)
                {
                    component.SetTheme(m_currentTheme);
                }
            }

            if (m_debugMode)
            {
                Debug.Log($"[VRUIManager] 应用主题: {theme.name}");
            }
        }

        /// <summary>
        /// 触发触觉反馈
        /// </summary>
        public void TriggerHapticFeedback(float intensity = 0.2f, float duration = 0.05f)
        {
            if (!m_hapticFeedbackEnabled)
                return;

            // 在两个控制器上触发触觉反馈
            SendHapticImpulse(m_leftController, intensity, duration);
            SendHapticImpulse(m_rightController, intensity, duration);
        }

        /// <summary>
        /// 获取注册的组件数量
        /// </summary>
        public int GetRegisteredComponentCount()
        {
            return m_registeredComponents.Count;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化主题
        /// </summary>
        private void InitializeTheme()
        {
            // 如果没有指定默认主题，创建一个
            if (m_defaultTheme == null)
            {
                m_defaultTheme = VRUITheme.CreateDefaultTheme();

                if (m_debugMode)
                {
                    Debug.Log("[VRUIManager] 创建默认主题");
                }
            }

            // 设置当前主题
            m_currentTheme = m_defaultTheme;
        }

        /// <summary>
        /// 初始化XR控制器
        /// </summary>
        private void InitializeControllers()
        {
            // 查找左右控制器
            var characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;
            var leftHandedControllers = new List<InputDevice>();
            var rightHandedControllers = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(characteristics | InputDeviceCharacteristics.Left, leftHandedControllers);
            InputDevices.GetDevicesWithCharacteristics(characteristics | InputDeviceCharacteristics.Right, rightHandedControllers);

            if (leftHandedControllers.Count > 0)
            {
                m_leftController = leftHandedControllers[0];
            }

            if (rightHandedControllers.Count > 0)
            {
                m_rightController = rightHandedControllers[0];
            }

            m_controllersInitialized = m_leftController.isValid || m_rightController.isValid;

            if (m_controllersInitialized && m_debugMode)
            {
                Debug.Log($"[VRUIManager] 控制器初始化完成 - 左: {m_leftController.isValid}, 右: {m_rightController.isValid}");
            }
        }

        /// <summary>
        /// 发送触觉脉冲
        /// </summary>
        private void SendHapticImpulse(InputDevice device, float amplitude, float duration)
        {
            if (!device.isValid)
                return;

            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        #endregion
    }
}