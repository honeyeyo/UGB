using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Threading.Tasks;
using PongHub.UI.Settings.Core;

namespace PongHub.Core
{
    /// <summary>
    /// 振动反馈管理器 - 提供统一的VR控制器振动反馈接口
    /// Vibration Manager - Provides unified VR controller haptic feedback interface
    /// </summary>
    public class VibrationManager : MonoBehaviour
    {
        private static VibrationManager s_instance;
        public static VibrationManager Instance => s_instance;

        [Header("振动反馈设置")]
        [SerializeField]
        [Tooltip("全局振动强度倍数 (0-1)")]
        [Range(0f, 1f)]
        private float m_vibrationIntensity = 1f;

        [SerializeField]
        [Tooltip("是否启用振动反馈")]
        private bool m_vibrationEnabled = true;

        [Header("预设振动类型")]
        [SerializeField]
        [Tooltip("球拍击球振动设置")]
        private VibrationSettings m_paddleHitVibration = new VibrationSettings(0.6f, 0.15f);

        [SerializeField]
        [Tooltip("UI交互振动设置")]
        private VibrationSettings m_uiInteractionVibration = new VibrationSettings(0.3f, 0.08f);

        [SerializeField]
        [Tooltip("警告振动设置")]
        private VibrationSettings m_warningVibration = new VibrationSettings(0.8f, 0.25f);
        
        [SerializeField]
        [Tooltip("抓取振动设置")]
        private VibrationSettings m_grabVibration = new VibrationSettings(0.4f, 0.1f);
        
        [SerializeField]
        [Tooltip("释放振动设置")]
        private VibrationSettings m_releaseVibration = new VibrationSettings(0.3f, 0.08f);
        
        [SerializeField]
        [Tooltip("按钮按下振动设置")]
        private VibrationSettings m_buttonPressVibration = new VibrationSettings(0.5f, 0.12f);
        
        [SerializeField]
        [Tooltip("菜单打开振动设置")]
        private VibrationSettings m_menuOpenVibration = new VibrationSettings(0.4f, 0.15f);
        
        [SerializeField]
        [Tooltip("菜单关闭振动设置")]
        private VibrationSettings m_menuCloseVibration = new VibrationSettings(0.3f, 0.1f);

        // VR设备引用
        private InputDevice m_leftHandDevice;
        private InputDevice m_rightHandDevice;
        private bool m_devicesInitialized = false;

        // 触觉反馈系统引用
        private SettingsHapticFeedback m_hapticFeedback;

        /// <summary>
        /// 振动类型枚举
        /// </summary>
        public enum VibrationType
        {
            PaddleHit,
            UIInteraction,
            Warning,
            Custom,
            Grab,
            Release,
            ButtonPress,
            MenuOpen,
            MenuClose
        }

        /// <summary>
        /// 振动设置结构
        /// </summary>
        [System.Serializable]
        public class VibrationSettings
        {
            [Range(0f, 1f)]
            [Tooltip("振动强度")]
            public float intensity;

            [Range(0f, 1f)]
            [Tooltip("振动持续时间")]
            public float duration;

            public VibrationSettings(float intensity, float duration)
            {
                this.intensity = intensity;
                this.duration = duration;
            }
        }

        #region Unity 生命周期

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeVRDevices();
        }

        private void OnEnable()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        #endregion

        #region 初始化

        public async Task InitializeAsync()
        {
            await Task.Yield();
            
            // 尝试获取设置系统的触觉反馈组件
            m_hapticFeedback = SettingsHapticFeedback.Instance;
            if (m_hapticFeedback != null)
            {
                // 同步设置
                m_vibrationEnabled = m_hapticFeedback.IsHapticEnabled();
                m_vibrationIntensity = m_hapticFeedback.GetGlobalIntensity();
            }

            InitializeVRDevices();
        }

        /// <summary>
        /// 初始化VR设备
        /// </summary>
        private void InitializeVRDevices()
        {
            var leftHandDevices = new List<InputDevice>();
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

            if (leftHandDevices.Count > 0)
            {
                m_leftHandDevice = leftHandDevices[0];
                Debug.Log($"VibrationManager: Left hand device initialized: {m_leftHandDevice.name}");
            }

            if (rightHandDevices.Count > 0)
            {
                m_rightHandDevice = rightHandDevices[0];
                Debug.Log($"VibrationManager: Right hand device initialized: {m_rightHandDevice.name}");
            }

            m_devicesInitialized = (leftHandDevices.Count > 0 || rightHandDevices.Count > 0);
        }

        #endregion

        #region 设备事件

        private void OnDeviceConnected(InputDevice device)
        {
            Debug.Log($"VibrationManager: VR Device connected: {device.name}");
            InitializeVRDevices();
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            Debug.Log($"VibrationManager: VR Device disconnected: {device.name}");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 播放振动反馈
        /// </summary>
        /// <param name="intensity">振动强度 (0-1)</param>
        /// <param name="hand">手部 (0=左手, 1=右手, -1=双手)</param>
        public void PlayVibration(float intensity, int hand = -1)
        {
            if (!m_vibrationEnabled || m_vibrationIntensity <= 0f)
                return;

            float finalIntensity = Mathf.Clamp01(intensity * m_vibrationIntensity);
            PlayVibrationOnDevice(finalIntensity, 0.1f, hand);
        }

        /// <summary>
        /// 播放预设振动类型
        /// </summary>
        /// <param name="vibrationType">振动类型</param>
        /// <param name="hand">手部 (0=左手, 1=右手, -1=双手)</param>
        public void PlayVibration(VibrationType vibrationType, int hand = -1)
        {
            if (!m_vibrationEnabled || m_vibrationIntensity <= 0f)
                return;

            var settings = GetVibrationSettings(vibrationType);
            if (settings != null)
            {
                float finalIntensity = Mathf.Clamp01(settings.intensity * m_vibrationIntensity);
                PlayVibrationOnDevice(finalIntensity, settings.duration, hand);
            }
        }

        /// <summary>
        /// 播放自定义振动
        /// </summary>
        /// <param name="intensity">振动强度 (0-1)</param>
        /// <param name="duration">持续时间</param>
        /// <param name="hand">手部 (0=左手, 1=右手, -1=双手)</param>
        public void PlayCustomVibration(float intensity, float duration, int hand = -1)
        {
            if (!m_vibrationEnabled || m_vibrationIntensity <= 0f)
                return;

            float finalIntensity = Mathf.Clamp01(intensity * m_vibrationIntensity);
            PlayVibrationOnDevice(finalIntensity, duration, hand);
        }

        /// <summary>
        /// 设置振动强度
        /// </summary>
        /// <param name="intensity">强度值 (0-1)</param>
        public void SetVibrationIntensity(float intensity)
        {
            m_vibrationIntensity = Mathf.Clamp01(intensity);
            
            // 同步到触觉反馈系统
            if (m_hapticFeedback != null)
            {
                m_hapticFeedback.SetGlobalIntensity(m_vibrationIntensity);
            }
        }

        /// <summary>
        /// 启用/禁用振动反馈
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetVibrationEnabled(bool enabled)
        {
            m_vibrationEnabled = enabled;
            
            // 同步到触觉反馈系统
            if (m_hapticFeedback != null)
            {
                m_hapticFeedback.SetHapticEnabled(enabled);
            }
        }

        /// <summary>
        /// 获取振动是否启用
        /// </summary>
        /// <returns>是否启用振动</returns>
        public bool IsVibrationEnabled()
        {
            return m_vibrationEnabled;
        }

        /// <summary>
        /// 获取振动强度
        /// </summary>
        /// <returns>振动强度值</returns>
        public float GetVibrationIntensity()
        {
            return m_vibrationIntensity;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取振动设置
        /// </summary>
        /// <param name="vibrationType">振动类型</param>
        /// <returns>振动设置</returns>
        private VibrationSettings GetVibrationSettings(VibrationType vibrationType)
        {
            switch (vibrationType)
            {
                case VibrationType.PaddleHit:
                    return m_paddleHitVibration;
                case VibrationType.UIInteraction:
                    return m_uiInteractionVibration;
                case VibrationType.Warning:
                    return m_warningVibration;
                case VibrationType.Grab:
                    return m_grabVibration;
                case VibrationType.Release:
                    return m_releaseVibration;
                case VibrationType.ButtonPress:
                    return m_buttonPressVibration;
                case VibrationType.MenuOpen:
                    return m_menuOpenVibration;
                case VibrationType.MenuClose:
                    return m_menuCloseVibration;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 在指定设备上播放振动
        /// </summary>
        /// <param name="intensity">振动强度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="hand">手部</param>
        private void PlayVibrationOnDevice(float intensity, float duration, int hand)
        {
            if (!m_devicesInitialized)
            {
                Debug.LogWarning("VibrationManager: VR devices not initialized");
                return;
            }

            // 优先使用设置系统的触觉反馈
            if (m_hapticFeedback != null)
            {
                m_hapticFeedback.PlayCustomHaptic(intensity, duration, hand);
                return;
            }

            // 备用方案：直接使用XR设备API
            if (hand == 0 || hand == -1) // 左手或双手
            {
                SendHapticToDevice(m_leftHandDevice, intensity, duration);
            }

            if (hand == 1 || hand == -1) // 右手或双手
            {
                SendHapticToDevice(m_rightHandDevice, intensity, duration);
            }
        }

        /// <summary>
        /// 向指定设备发送触觉反馈
        /// </summary>
        /// <param name="device">VR设备</param>
        /// <param name="intensity">强度</param>
        /// <param name="duration">持续时间</param>
        private void SendHapticToDevice(InputDevice device, float intensity, float duration)
        {
            if (!device.isValid)
                return;

            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, intensity, duration);
                }
            }
        }

        public void Cleanup()
        {
            // 清理资源
            m_devicesInitialized = false;
            m_hapticFeedback = null;
        }

        #endregion

        #region 调试功能

        /// <summary>
        /// 测试所有振动类型
        /// </summary>
        [ContextMenu("Test All Vibration Types")]
        public void TestAllVibrationTypes()
        {
            if (!Application.isPlaying)
                return;

            StartCoroutine(TestVibrationSequence());
        }

        /// <summary>
        /// 测试振动序列
        /// </summary>
        private System.Collections.IEnumerator TestVibrationSequence()
        {
            var vibrationTypes = System.Enum.GetValues(typeof(VibrationType));

            foreach (VibrationType type in vibrationTypes)
            {
                if (type == VibrationType.Custom)
                    continue;

                Debug.Log($"Testing vibration type: {type}");
                PlayVibration(type);
                yield return new WaitForSeconds(0.8f);
            }
        }

        #endregion
    }
}