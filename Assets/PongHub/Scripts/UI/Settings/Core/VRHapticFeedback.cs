using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System.Collections.Generic;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 设置系统VR触觉反馈
    /// Settings VR Haptic Feedback system for enhanced user interaction
    /// </summary>
    public class SettingsHapticFeedback : MonoBehaviour
    {
        [Header("触觉反馈设置")]
        [SerializeField]
        [Tooltip("全局触觉反馈强度")]
        [Range(0f, 1f)]
        private float globalIntensity = 1.0f;

        [SerializeField]
        [Tooltip("是否启用触觉反馈")]
        private bool hapticEnabled = true;

        [Header("反馈类型设置")]
        [SerializeField]
        [Tooltip("按钮点击反馈")]
        private HapticSettings buttonPress = new HapticSettings(0.3f, 0.1f);

        [SerializeField]
        [Tooltip("选择反馈")]
        private HapticSettings selection = new HapticSettings(0.2f, 0.05f);

        [SerializeField]
        [Tooltip("页面切换反馈")]
        private HapticSettings pageChange = new HapticSettings(0.5f, 0.15f);

        [SerializeField]
        [Tooltip("模式确认反馈")]
        private HapticSettings modeConfirm = new HapticSettings(0.7f, 0.2f);

        [SerializeField]
        [Tooltip("警告反馈")]
        private HapticSettings warning = new HapticSettings(0.8f, 0.3f);

        [SerializeField]
        [Tooltip("返回反馈")]
        private HapticSettings back = new HapticSettings(0.2f, 0.08f);

        // VR设备引用
        private UnityEngine.XR.InputDevice leftHandDevice;
        private UnityEngine.XR.InputDevice rightHandDevice;

        // 触觉反馈类型枚举
        public enum HapticType
        {
            ButtonPress,
            Selection,
            PageChange,
            ModeConfirm,
            Warning,
            Back
        }

        // 触觉反馈设置结构
        [System.Serializable]
        public class HapticSettings
        {
            [Range(0f, 1f)]
            public float amplitude;

            [Range(0f, 1f)]
            public float duration;

            public HapticSettings(float amplitude, float duration)
            {
                this.amplitude = amplitude;
                this.duration = duration;
            }
        }

        #region Unity 生命周期

        private void Start()
        {
            InitializeVRDevices();
            LoadHapticSettings();
        }

        private void OnEnable()
        {
            RegisterDeviceEvents();
        }

        private void OnDisable()
        {
            UnregisterDeviceEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化VR设备
        /// </summary>
        private void InitializeVRDevices()
        {
            var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();

            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

            if (leftHandDevices.Count > 0)
            {
                leftHandDevice = leftHandDevices[0];
                Debug.Log($"Left hand device initialized: {leftHandDevice.name}");
            }

            if (rightHandDevices.Count > 0)
            {
                rightHandDevice = rightHandDevices[0];
                Debug.Log($"Right hand device initialized: {rightHandDevice.name}");
            }
        }

        /// <summary>
        /// 加载触觉反馈设置
        /// </summary>
        private void LoadHapticSettings()
        {
            // 从SettingsManager加载设置
            var settingsManager = SettingsManager.Instance;
            if (settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                hapticEnabled = controlSettings.hapticFeedback;
                globalIntensity = controlSettings.hapticIntensity;
            }
        }

        /// <summary>
        /// 注册设备事件
        /// </summary>
        private void RegisterDeviceEvents()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        /// <summary>
        /// 取消注册设备事件
        /// </summary>
        private void UnregisterDeviceEvents()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        #endregion

        #region 设备事件

        /// <summary>
        /// 设备连接事件
        /// </summary>
        private void OnDeviceConnected(UnityEngine.XR.InputDevice device)
        {
            Debug.Log($"VR Device connected: {device.name}");
            InitializeVRDevices(); // 重新初始化设备
        }

        /// <summary>
        /// 设备断开事件
        /// </summary>
        private void OnDeviceDisconnected(UnityEngine.XR.InputDevice device)
        {
            Debug.Log($"VR Device disconnected: {device.name}");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 播放触觉反馈
        /// </summary>
        /// <param name="hapticType">触觉反馈类型</param>
        /// <param name="hand">手部（0=左手，1=右手，-1=双手）</param>
        public void PlayHaptic(HapticType hapticType, int hand = -1)
        {
            if (!hapticEnabled || globalIntensity <= 0f)
                return;

            var settings = GetHapticSettings(hapticType);
            if (settings == null)
                return;

            float finalAmplitude = settings.amplitude * globalIntensity;
            float finalDuration = settings.duration;

            // 播放到指定手部或双手
            if (hand == 0 || hand == -1) // 左手或双手
            {
                PlayHapticOnDevice(leftHandDevice, finalAmplitude, finalDuration);
            }

            if (hand == 1 || hand == -1) // 右手或双手
            {
                PlayHapticOnDevice(rightHandDevice, finalAmplitude, finalDuration);
            }

            Debug.Log($"Haptic feedback played: {hapticType} (Amplitude: {finalAmplitude:F2}, Duration: {finalDuration:F2})");
        }

        /// <summary>
        /// 播放自定义触觉反馈
        /// </summary>
        /// <param name="amplitude">振幅 (0-1)</param>
        /// <param name="duration">持续时间</param>
        /// <param name="hand">手部（0=左手，1=右手，-1=双手）</param>
        public void PlayCustomHaptic(float amplitude, float duration, int hand = -1)
        {
            if (!hapticEnabled || globalIntensity <= 0f)
                return;

            float finalAmplitude = Mathf.Clamp01(amplitude * globalIntensity);

            if (hand == 0 || hand == -1) // 左手或双手
            {
                PlayHapticOnDevice(leftHandDevice, finalAmplitude, duration);
            }

            if (hand == 1 || hand == -1) // 右手或双手
            {
                PlayHapticOnDevice(rightHandDevice, finalAmplitude, duration);
            }
        }

        /// <summary>
        /// 设置全局触觉反馈强度
        /// </summary>
        /// <param name="intensity">强度值 (0-1)</param>
        public void SetGlobalIntensity(float intensity)
        {
            globalIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 启用/禁用触觉反馈
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetHapticEnabled(bool enabled)
        {
            hapticEnabled = enabled;
        }

        /// <summary>
        /// 获取当前触觉反馈状态
        /// </summary>
        /// <returns>是否启用触觉反馈</returns>
        public bool IsHapticEnabled()
        {
            return hapticEnabled;
        }

        /// <summary>
        /// 获取全局强度
        /// </summary>
        /// <returns>全局强度值</returns>
        public float GetGlobalIntensity()
        {
            return globalIntensity;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取触觉反馈设置
        /// </summary>
        /// <param name="hapticType">触觉反馈类型</param>
        /// <returns>触觉反馈设置</returns>
        private HapticSettings GetHapticSettings(HapticType hapticType)
        {
            switch (hapticType)
            {
                case HapticType.ButtonPress:
                    return buttonPress;
                case HapticType.Selection:
                    return selection;
                case HapticType.PageChange:
                    return pageChange;
                case HapticType.ModeConfirm:
                    return modeConfirm;
                case HapticType.Warning:
                    return warning;
                case HapticType.Back:
                    return back;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 在指定设备上播放触觉反馈
        /// </summary>
        /// <param name="device">VR设备</param>
        /// <param name="amplitude">振幅</param>
        /// <param name="duration">持续时间</param>
        private void PlayHapticOnDevice(UnityEngine.XR.InputDevice device, float amplitude, float duration)
        {
            if (!device.isValid)
                return;

            // 尝试使用XR方式发送触觉反馈
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, amplitude, duration);
                }
            }
        }

        /// <summary>
        /// 更新触觉反馈设置
        /// </summary>
        /// <param name="hapticType">触觉反馈类型</param>
        /// <param name="settings">新的设置</param>
        public void UpdateHapticSettings(HapticType hapticType, HapticSettings settings)
        {
            switch (hapticType)
            {
                case HapticType.ButtonPress:
                    buttonPress = settings;
                    break;
                case HapticType.Selection:
                    selection = settings;
                    break;
                case HapticType.PageChange:
                    pageChange = settings;
                    break;
                case HapticType.ModeConfirm:
                    modeConfirm = settings;
                    break;
                case HapticType.Warning:
                    warning = settings;
                    break;
                case HapticType.Back:
                    back = settings;
                    break;
            }
        }

        #endregion

        #region 调试功能

        /// <summary>
        /// 测试所有触觉反馈类型
        /// </summary>
        [ContextMenu("Test All Haptic Types")]
        public void TestAllHapticTypes()
        {
            if (!Application.isPlaying)
                return;

            StartCoroutine(TestHapticSequence());
        }

        /// <summary>
        /// 测试触觉反馈序列
        /// </summary>
        private System.Collections.IEnumerator TestHapticSequence()
        {
            var hapticTypes = System.Enum.GetValues(typeof(HapticType));

            foreach (HapticType type in hapticTypes)
            {
                Debug.Log($"Testing haptic type: {type}");
                PlayHaptic(type);
                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion

        #region Singleton支持

        private static SettingsHapticFeedback instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static SettingsHapticFeedback Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SettingsHapticFeedback>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SettingsHapticFeedback");
                        instance = go.AddComponent<SettingsHapticFeedback>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        #endregion
    }
}