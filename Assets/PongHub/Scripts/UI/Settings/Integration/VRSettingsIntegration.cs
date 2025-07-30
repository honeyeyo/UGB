using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using PongHub.UI.Settings.Core;

namespace PongHub.UI.Settings.Integration
{
    /// <summary>
    /// VR设置集成
    /// VR Settings Integration - Connects settings to VR SDK and hardware
    /// </summary>
    public class VRSettingsIntegration : MonoBehaviour
    {
        [Header("VR配置")]
        [SerializeField]
        [Tooltip("支持的VR设备列表")]
        private VRDevice[] supportedDevices = new VRDevice[0];

        [SerializeField]
        [Tooltip("手部跟踪精度设置")]
        private HandTrackingConfig handTrackingConfig;

        [Header("校准设置")]
        [SerializeField]
        [Tooltip("自动校准间隔（秒）")]
        private float autoCalibrationInterval = 300f;

        [SerializeField]
        [Tooltip("启用自动校准")]
        private bool enableAutoCalibration = true;

        // 内部状态
        private ControlSettings currentControlSettings;
        private UserProfile currentUserProfile;
        private bool isInitialized = false;
        private bool isVRActive = false;

        // VR组件引用
        private Transform vrCamera;
        private Transform leftController;
        private Transform rightController;

        /// <summary>
        /// VR设备配置
        /// </summary>
        [Serializable]
        public class VRDevice
        {
            [Header("设备信息")]
            public string deviceName;
            public string displayName;
            public bool isSupported;

            [Header("性能配置")]
            public int recommendedRefreshRate;
            public float defaultRenderScale;
            public bool supportsFoveatedRendering;
            public bool supportsHandTracking;

            [Header("舒适度配置")]
            public ComfortSettings defaultComfortSettings;
        }

        /// <summary>
        /// 手部跟踪配置
        /// </summary>
        [Serializable]
        public class HandTrackingConfig
        {
            [Header("跟踪精度")]
            public float positionAccuracy = 1.0f;
            public float rotationAccuracy = 1.0f;
            public float gestureAccuracy = 1.0f;

            [Header("过滤设置")]
            public bool enableSmoothing = true;
            public float smoothingFactor = 0.8f;
            public bool enablePrediction = true;
            public float predictionTime = 0.02f;
        }

        #region Unity 生命周期

        private void Awake()
        {
            InitializeDefaultDevices();
            FindVRComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void Update()
        {
            if (isVRActive && isInitialized)
            {
                UpdateVRTracking();
                CheckDeviceStatus();
            }
        }

        #endregion

        #region 初始化

        private void InitializeDefaultDevices()
        {
            if (supportedDevices.Length == 0)
            {
                supportedDevices = new VRDevice[]
                {
                    new VRDevice
                    {
                        deviceName = "Meta Quest 2",
                        displayName = "Meta Quest 2",
                        isSupported = true,
                        recommendedRefreshRate = 120,
                        defaultRenderScale = 1.0f,
                        supportsFoveatedRendering = true,
                        supportsHandTracking = true,
                        defaultComfortSettings = new ComfortSettings
                        {
                            motionSicknessReduction = true,
                            vignette = false,
                            snapTurn = true,
                            teleportMovement = true,
                            comfortLevel = 1.0f
                        }
                    },
                    new VRDevice
                    {
                        deviceName = "Meta Quest 3",
                        displayName = "Meta Quest 3",
                        isSupported = true,
                        recommendedRefreshRate = 120,
                        defaultRenderScale = 1.2f,
                        supportsFoveatedRendering = true,
                        supportsHandTracking = true,
                        defaultComfortSettings = new ComfortSettings
                        {
                            motionSicknessReduction = false,
                            vignette = false,
                            snapTurn = false,
                            teleportMovement = false,
                            comfortLevel = 0.5f
                        }
                    }
                };
            }
        }

        private void FindVRComponents()
        {
            // 查找VR摄像机
            if (vrCamera == null)
            {
                var cameras = FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam.name.Contains("Center") || cam.name.Contains("Main") || cam.name.Contains("VR"))
                    {
                        vrCamera = cam.transform;
                        break;
                    }
                }
            }

            // 查找控制器
            FindControllers();
        }

        private void FindControllers()
        {
            // 通过XR系统查找控制器
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, inputDevices);

            foreach (var device in inputDevices)
            {
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                {
                    // 左控制器逻辑
                    Debug.Log($"Found left controller: {device.name}");
                }
                else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                {
                    // 右控制器逻辑
                    Debug.Log($"Found right controller: {device.name}");
                }
            }
        }

        private void Initialize()
        {
            // 检测VR状态
            isVRActive = XRSettings.enabled && XRSettings.loadedDeviceName != "";

            if (!isVRActive)
            {
                Debug.LogWarning("VRSettingsIntegration: VR not active");
                return;
            }

            // 获取当前设置
            if (SettingsManager.Instance != null)
            {
                currentControlSettings = SettingsManager.Instance.GetControlSettings();
                currentUserProfile = SettingsManager.Instance.GetUserProfile();

                ApplyVRSettings();
            }

            // 启动自动校准
            if (enableAutoCalibration)
            {
                StartCoroutine(AutoCalibrationRoutine());
            }

            isInitialized = true;
            Debug.Log("VRSettingsIntegration initialized successfully");
        }

        private void RegisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnControlSettingsChanged += OnControlSettingsChanged;
                SettingsManager.OnUserProfileChanged += OnUserProfileChanged;
            }

            // XR事件
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        private void UnregisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnControlSettingsChanged -= OnControlSettingsChanged;
                SettingsManager.OnUserProfileChanged -= OnUserProfileChanged;
            }

            // XR事件
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        #endregion

        #region VR设置应用

        /// <summary>
        /// 应用VR设置
        /// </summary>
        public void ApplyVRSettings()
        {
            if (!isInitialized || !isVRActive)
                return;

            // 应用控制设置
            ApplyControllerSettings();

            // 应用用户配置
            ApplyUserProfile();

            // 应用手部跟踪设置
            ApplyHandTrackingSettings();

            // 应用舒适度设置
            ApplyComfortSettings();

            Debug.Log("VR settings applied successfully");
        }

        private void ApplyControllerSettings()
        {
            if (currentControlSettings == null) return;

            // 设置控制器灵敏度
            SetControllerSensitivity(currentControlSettings.vrControllerSensitivity);

            // 设置死区
            SetControllerDeadZone(currentControlSettings.deadZone);

            // 设置触觉反馈
            SetHapticSettings(currentControlSettings.hapticFeedback, currentControlSettings.hapticIntensity);

            // 设置主手偏好
            SetDominantHand(currentControlSettings.dominantHand);
        }

        private void ApplyUserProfile()
        {
            if (currentUserProfile == null) return;

            // 设置用户身高（影响VR空间校准）
            SetUserHeight(currentUserProfile.heightCm);

            // 设置手部跟踪精度
            SetHandTrackingAccuracy(currentControlSettings.handTrackingAccuracy);

            // 根据经验水平调整设置
            AdjustForExperienceLevel(currentUserProfile.experience);
        }

        private void ApplyHandTrackingSettings()
        {
            if (handTrackingConfig == null) return;

            // 配置手部跟踪精度
            var config = handTrackingConfig;
            config.positionAccuracy = currentControlSettings.handTrackingAccuracy;
            config.rotationAccuracy = currentControlSettings.handTrackingAccuracy;

            // 应用到VR系统
            ApplyHandTrackingConfig(config);
        }

        private void ApplyComfortSettings()
        {
            var deviceConfig = GetCurrentDeviceConfig();
            if (deviceConfig?.defaultComfortSettings == null) return;

            var comfortSettings = deviceConfig.defaultComfortSettings;

            // 根据用户经验调整舒适度设置
            if (currentUserProfile.experience == ExperienceLevel.Beginner)
            {
                comfortSettings.motionSicknessReduction = true;
                comfortSettings.vignette = true;
                comfortSettings.snapTurn = true;
            }
        }

        #endregion

        #region 控制器管理

        private void SetControllerSensitivity(float sensitivity)
        {
            // 应用控制器灵敏度设置
            // 这里可以与OVR SDK或其他VR SDK集成
            Debug.Log($"Controller sensitivity set to: {sensitivity}");
        }

        private void SetControllerDeadZone(float deadZone)
        {
            // 设置控制器摇杆死区
            Debug.Log($"Controller dead zone set to: {deadZone}");
        }

        private void SetHapticSettings(bool enabled, float intensity)
        {
            // 配置触觉反馈
            if (enabled)
            {
                // 可以通过OVR SDK设置触觉强度
                Debug.Log($"Haptic feedback enabled with intensity: {intensity}");
            }
            else
            {
                Debug.Log("Haptic feedback disabled");
            }
        }

        private void SetDominantHand(HandPreference handPreference)
        {
            // 设置主手偏好
            Debug.Log($"Dominant hand set to: {handPreference}");
        }

        #endregion

        #region 用户校准

        private void SetUserHeight(float heightCm)
        {
            // 根据用户身高调整VR空间
            float heightM = heightCm / 100f;

            if (vrCamera != null)
            {
                // 调整VR摄像机高度偏移
                var position = vrCamera.localPosition;
                position.y = heightM - 1.7f; // 假设默认高度为1.7m
                vrCamera.localPosition = position;
            }

            Debug.Log($"User height set to: {heightCm}cm");
        }

        private void SetHandTrackingAccuracy(float accuracy)
        {
            if (handTrackingConfig != null)
            {
                handTrackingConfig.positionAccuracy = accuracy;
                handTrackingConfig.rotationAccuracy = accuracy;
                handTrackingConfig.gestureAccuracy = accuracy;

                ApplyHandTrackingConfig(handTrackingConfig);
            }
        }

        private void ApplyHandTrackingConfig(HandTrackingConfig config)
        {
            // 应用手部跟踪配置到VR系统
            // 这里可以与Meta SDK或其他手部跟踪系统集成
            Debug.Log("Hand tracking configuration applied");
        }

        private void AdjustForExperienceLevel(ExperienceLevel experience)
        {
            switch (experience)
            {
                case ExperienceLevel.Beginner:
                    // 为新手用户启用更多辅助功能
                    EnableBeginnerAssists();
                    break;
                case ExperienceLevel.Intermediate:
                    // 平衡设置
                    EnableIntermediateSettings();
                    break;
                case ExperienceLevel.Expert:
                    // 为专家用户禁用辅助功能
                    EnableExpertSettings();
                    break;
            }
        }

        private void EnableBeginnerAssists()
        {
            // 新手辅助设置
            Debug.Log("Beginner VR assists enabled");
        }

        private void EnableIntermediateSettings()
        {
            // 中级用户设置
            Debug.Log("Intermediate VR settings applied");
        }

        private void EnableExpertSettings()
        {
            // 专家用户设置
            Debug.Log("Expert VR settings applied");
        }

        #endregion

        #region 实时跟踪和监控

        private void UpdateVRTracking()
        {
            // 更新VR跟踪数据
            UpdateControllerPositions();
            UpdateHandTracking();
        }

        private void UpdateControllerPositions()
        {
            // 获取控制器位置和旋转
            var leftDevices = new System.Collections.Generic.List<InputDevice>();
            var rightDevices = new System.Collections.Generic.List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

            // 处理左控制器
            foreach (var device in leftDevices)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPosition) &&
                    device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRotation))
                {
                    // 更新左控制器数据
                    OnLeftControllerUpdate(leftPosition, leftRotation);
                }
            }

            // 处理右控制器
            foreach (var device in rightDevices)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPosition) &&
                    device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRotation))
                {
                    // 更新右控制器数据
                    OnRightControllerUpdate(rightPosition, rightRotation);
                }
            }
        }

        private void UpdateHandTracking()
        {
            // 更新手部跟踪数据（如果支持）
            var currentDevice = GetCurrentDeviceConfig();
            if (currentDevice?.supportsHandTracking == true)
            {
                // 集成手部跟踪SDK
                UpdateHandTrackingData();
            }
        }

        private void UpdateHandTrackingData()
        {
            // 实际的手部跟踪数据更新
            // 这里可以集成Meta Hand Tracking SDK
            Debug.Log("Hand tracking data updated");
        }

        private void CheckDeviceStatus()
        {
            // 检查VR设备状态
            CheckBatteryLevel();
            CheckTrackingQuality();
            CheckTemperature();
        }

        private void CheckBatteryLevel()
        {
            // 检查控制器电量
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);

            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.batteryLevel, out float battery))
                {
                    if (battery < 0.2f) // 电量低于20%
                    {
                        OnLowBattery(device.name, battery);
                    }
                }
            }
        }

        private void CheckTrackingQuality()
        {
            // 检查跟踪质量
            if (XRSettings.enabled)
            {
                // 监控跟踪丢失事件 - 使用新的API
                var headDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);
                Vector3 trackingState = Vector3.zero;
                if (headDevice.isValid)
                {
                    headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out trackingState);
                }
                
                if (trackingState == Vector3.zero)
                {
                    OnTrackingLost();
                }
            }
        }

        private void CheckTemperature()
        {
            // 检查设备温度（如果支持）
            // 在移动VR设备上特别重要
        }

        #endregion

        #region 自动校准

        private IEnumerator AutoCalibrationRoutine()
        {
            while (enabled && isVRActive)
            {
                yield return new WaitForSeconds(autoCalibrationInterval);

                if (ShouldPerformAutoCalibration())
                {
                    PerformAutoCalibration();
                }
            }
        }

        private bool ShouldPerformAutoCalibration()
        {
            // 检查是否需要自动校准
            // 可以基于跟踪质量、用户移动等因素判断
            return true;
        }

        private void PerformAutoCalibration()
        {
            Debug.Log("Performing auto-calibration...");

            // 校准头部位置
            CalibrateHeadPosition();

            // 校准控制器
            CalibrateControllers();

            // 校准游戏空间
            CalibratePlayArea();
        }

        private void CalibrateHeadPosition()
        {
            // 头部位置校准逻辑
        }

        private void CalibrateControllers()
        {
            // 控制器校准逻辑
        }

        private void CalibratePlayArea()
        {
            // 游戏空间校准逻辑
        }

        #endregion

        #region 事件处理

        private void OnControlSettingsChanged(ControlSettings newSettings)
        {
            currentControlSettings = newSettings;
            ApplyControllerSettings();
        }

        private void OnUserProfileChanged(UserProfile newProfile)
        {
            currentUserProfile = newProfile;
            ApplyUserProfile();
        }

        private void OnDeviceConnected(InputDevice device)
        {
            Debug.Log($"VR Device connected: {device.name}");
            FindControllers();
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            Debug.Log($"VR Device disconnected: {device.name}");
        }

        private void OnLeftControllerUpdate(Vector3 position, Quaternion rotation)
        {
            // 左控制器数据更新处理
        }

        private void OnRightControllerUpdate(Vector3 position, Quaternion rotation)
        {
            // 右控制器数据更新处理
        }

        private void OnLowBattery(string deviceName, float batteryLevel)
        {
            Debug.LogWarning($"Low battery warning: {deviceName} - {batteryLevel:P0}");
            // 可以显示UI警告或发送事件
        }

        private void OnTrackingLost()
        {
            Debug.LogWarning("VR tracking lost");
            // 处理跟踪丢失情况
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取当前VR设备配置
        /// </summary>
        /// <returns>设备配置</returns>
        public VRDevice GetCurrentDeviceConfig()
        {
            string deviceName = XRSettings.loadedDeviceName;
            return Array.Find(supportedDevices, d => d.deviceName.Contains(deviceName));
        }

        /// <summary>
        /// 获取VR系统信息
        /// </summary>
        /// <returns>系统信息</returns>
        public VRSystemInfo GetVRSystemInfo()
        {
            return new VRSystemInfo
            {
                deviceName = XRSettings.loadedDeviceName,
                isActive = XRSettings.enabled,
                refreshRate = XRDevice.refreshRate,
                eyeTextureWidth = XRSettings.eyeTextureWidth,
                eyeTextureHeight = XRSettings.eyeTextureHeight,
                renderScale = XRSettings.eyeTextureResolutionScale
            };
        }

        /// <summary>
        /// 手动触发设备校准
        /// </summary>
        public void TriggerCalibration()
        {
            PerformAutoCalibration();
        }

        /// <summary>
        /// 重置VR设置
        /// </summary>
        public void ResetVRSettings()
        {
            var defaultControl = new ControlSettings();
            var defaultProfile = new UserProfile();

            currentControlSettings = defaultControl;
            currentUserProfile = defaultProfile;

            ApplyVRSettings();
        }

        /// <summary>
        /// 播放触觉反馈
        /// </summary>
        /// <param name="hand">手部</param>
        /// <param name="intensity">强度</param>
        /// <param name="duration">持续时间</param>
        public void PlayHaptic(HandPreference hand, float intensity, float duration)
        {
            if (!currentControlSettings.hapticFeedback) return;

            float finalIntensity = intensity * currentControlSettings.hapticIntensity;

            // 播放触觉反馈
            // 这里可以集成OVR SDK的触觉反馈API
            Debug.Log($"Playing haptic feedback - Hand: {hand}, Intensity: {finalIntensity}, Duration: {duration}");
        }

        #endregion

        /// <summary>
        /// VR系统信息
        /// </summary>
        public struct VRSystemInfo
        {
            public string deviceName;
            public bool isActive;
            public float refreshRate;
            public int eyeTextureWidth;
            public int eyeTextureHeight;
            public float renderScale;
        }
    }
}