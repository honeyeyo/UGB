using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;
using System.Collections.Generic; // Added for List

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 控制设置面板
    /// Control settings panel for VR input and interaction options
    /// </summary>
    public class ControlSettingsPanel : MonoBehaviour
    {
        [Header("手柄设置")]
        [SerializeField]
        [Tooltip("左手灵敏度滑块")]
        private SettingSlider leftHandSensitivitySlider;

        [SerializeField]
        [Tooltip("右手灵敏度滑块")]
        private SettingSlider rightHandSensitivitySlider;

        [SerializeField]
        [Tooltip("触觉反馈开关")]
        private SettingToggle hapticFeedbackToggle;

        [SerializeField]
        [Tooltip("触觉反馈强度滑块")]
        private SettingSlider hapticIntensitySlider;

        [SerializeField]
        [Tooltip("主手偏好下拉框")]
        private SettingDropdown dominantHandDropdown;

        [Header("移动设置")]
        [SerializeField]
        [Tooltip("移动方式下拉框")]
        private SettingDropdown movementTypeDropdown;

        [SerializeField]
        [Tooltip("移动速度滑块")]
        private SettingSlider movementSpeedSlider;

        [SerializeField]
        [Tooltip("转向速度滑块")]
        private SettingSlider turnSpeedSlider;

        [SerializeField]
        [Tooltip("传送移动开关")]
        private SettingToggle teleportMovementToggle;

        [SerializeField]
        [Tooltip("边界显示开关")]
        private SettingToggle boundaryDisplayToggle;

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("交互模式下拉框")]
        private SettingDropdown interactionModeDropdown;

        [SerializeField]
        [Tooltip("抓取距离滑块")]
        private SettingSlider grabDistanceSlider;

        [SerializeField]
        [Tooltip("指向距离滑块")]
        private SettingSlider pointingDistanceSlider;

        [SerializeField]
        [Tooltip("自动抓取开关")]
        private SettingToggle autoGrabToggle;

        [Header("手部追踪设置")]
        [SerializeField]
        [Tooltip("手部追踪开关")]
        private SettingToggle handTrackingToggle;

        [SerializeField]
        [Tooltip("手势识别开关")]
        private SettingToggle gestureRecognitionToggle;

        [SerializeField]
        [Tooltip("手部追踪精度滑块")]
        private SettingSlider handTrackingAccuracySlider;

        [Header("按键映射")]
        [SerializeField]
        [Tooltip("左手触发器映射下拉框")]
        private SettingDropdown leftTriggerMappingDropdown;

        [SerializeField]
        [Tooltip("右手触发器映射下拉框")]
        private SettingDropdown rightTriggerMappingDropdown;

        [SerializeField]
        [Tooltip("左手握把映射下拉框")]
        private SettingDropdown leftGripMappingDropdown;

        [SerializeField]
        [Tooltip("右手握把映射下拉框")]
        private SettingDropdown rightGripMappingDropdown;

        [Header("高级设置")]
        [SerializeField]
        [Tooltip("死区大小滑块")]
        private SettingSlider deadZoneSizeSlider;

        [SerializeField]
        [Tooltip("输入平滑滑块")]
        private SettingSlider inputSmoothingSlider;

        [SerializeField]
        [Tooltip("手柄振动开关")]
        private SettingToggle controllerVibrationToggle;

        [Header("校准和测试")]
        [SerializeField]
        [Tooltip("重新校准按钮")]
        private Button recalibrateButton;

        [SerializeField]
        [Tooltip("测试左手按钮")]
        private Button testLeftHandButton;

        [SerializeField]
        [Tooltip("测试右手按钮")]
        private Button testRightHandButton;

        [SerializeField]
        [Tooltip("输入状态显示")]
        private Transform inputStatusDisplay;

        [SerializeField]
        [Tooltip("左手状态文本")]
        private TextMeshProUGUI leftHandStatusText;

        [SerializeField]
        [Tooltip("右手状态文本")]
        private TextMeshProUGUI rightHandStatusText;

        // 组件引用
        private SettingsManager settingsManager;
        private VRHapticFeedback hapticFeedback;

        // VR输入系统
        private InputDevice leftHandDevice;
        private InputDevice rightHandDevice;
        private bool isMonitoringInput = false;

        // 输入测试状态
        private bool isTestingLeftHand = false;
        private bool isTestingRightHand = false;

        #region Unity 生命周期

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupControlComponents();
            InitializeVRDevices();
            RefreshPanel();
        }

        private void Update()
        {
            if (isMonitoringInput)
            {
                UpdateInputMonitoring();
            }

            UpdateHandTesting();
        }

        private void OnEnable()
        {
            isMonitoringInput = true;
            RegisterInputEvents();
        }

        private void OnDisable()
        {
            isMonitoringInput = false;
            UnregisterInputEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                settingsManager = FindObjectOfType<SettingsManager>();
            }

            hapticFeedback = FindObjectOfType<VRHapticFeedback>();
        }

        /// <summary>
        /// 设置控制组件
        /// </summary>
        private void SetupControlComponents()
        {
            // 手柄设置事件
            if (leftHandSensitivitySlider != null)
            {
                leftHandSensitivitySlider.OnValueChanged += OnLeftHandSensitivityChanged;
            }

            if (rightHandSensitivitySlider != null)
            {
                rightHandSensitivitySlider.OnValueChanged += OnRightHandSensitivityChanged;
            }

            if (hapticFeedbackToggle != null)
            {
                hapticFeedbackToggle.OnValueChanged += OnHapticFeedbackChanged;
            }

            if (hapticIntensitySlider != null)
            {
                hapticIntensitySlider.OnValueChanged += OnHapticIntensityChanged;
            }

            if (dominantHandDropdown != null)
            {
                dominantHandDropdown.OnValueChanged += OnDominantHandChanged;
            }

            // 移动设置事件
            if (movementTypeDropdown != null)
            {
                movementTypeDropdown.OnValueChanged += OnMovementTypeChanged;
            }

            if (movementSpeedSlider != null)
            {
                movementSpeedSlider.OnValueChanged += OnMovementSpeedChanged;
            }

            if (turnSpeedSlider != null)
            {
                turnSpeedSlider.OnValueChanged += OnTurnSpeedChanged;
            }

            if (teleportMovementToggle != null)
            {
                teleportMovementToggle.OnValueChanged += OnTeleportMovementChanged;
            }

            if (boundaryDisplayToggle != null)
            {
                boundaryDisplayToggle.OnValueChanged += OnBoundaryDisplayChanged;
            }

            // 交互设置事件
            if (interactionModeDropdown != null)
            {
                interactionModeDropdown.OnValueChanged += OnInteractionModeChanged;
            }

            if (grabDistanceSlider != null)
            {
                grabDistanceSlider.OnValueChanged += OnGrabDistanceChanged;
            }

            if (pointingDistanceSlider != null)
            {
                pointingDistanceSlider.OnValueChanged += OnPointingDistanceChanged;
            }

            if (autoGrabToggle != null)
            {
                autoGrabToggle.OnValueChanged += OnAutoGrabChanged;
            }

            // 手部追踪设置事件
            if (handTrackingToggle != null)
            {
                handTrackingToggle.OnValueChanged += OnHandTrackingChanged;
            }

            if (gestureRecognitionToggle != null)
            {
                gestureRecognitionToggle.OnValueChanged += OnGestureRecognitionChanged;
            }

            if (handTrackingAccuracySlider != null)
            {
                handTrackingAccuracySlider.OnValueChanged += OnHandTrackingAccuracyChanged;
            }

            // 按键映射事件
            if (leftTriggerMappingDropdown != null)
            {
                leftTriggerMappingDropdown.OnValueChanged += OnLeftTriggerMappingChanged;
            }

            if (rightTriggerMappingDropdown != null)
            {
                rightTriggerMappingDropdown.OnValueChanged += OnRightTriggerMappingChanged;
            }

            // 高级设置事件
            if (deadZoneSizeSlider != null)
            {
                deadZoneSizeSlider.OnValueChanged += OnDeadZoneSizeChanged;
            }

            if (inputSmoothingSlider != null)
            {
                inputSmoothingSlider.OnValueChanged += OnInputSmoothingChanged;
            }

            if (controllerVibrationToggle != null)
            {
                controllerVibrationToggle.OnValueChanged += OnControllerVibrationChanged;
            }

            // 按钮事件
            if (recalibrateButton != null)
            {
                recalibrateButton.onClick.AddListener(OnRecalibrate);
            }

            if (testLeftHandButton != null)
            {
                testLeftHandButton.onClick.AddListener(() => StartHandTest(true));
            }

            if (testRightHandButton != null)
            {
                testRightHandButton.onClick.AddListener(() => StartHandTest(false));
            }
        }

        /// <summary>
        /// 初始化VR设备
        /// </summary>
        private void InitializeVRDevices()
        {
            // 获取VR手柄设备
            var leftHandDevices = new List<InputDevice>();
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

            if (leftHandDevices.Count > 0)
            {
                leftHandDevice = leftHandDevices[0];
            }

            if (rightHandDevices.Count > 0)
            {
                rightHandDevice = rightHandDevices[0];
            }

            Debug.Log($"VR Devices - Left: {leftHandDevice.name}, Right: {rightHandDevice.name}");
        }

        #endregion

        #region 手柄设置事件

        /// <summary>
        /// 左手灵敏度变更事件
        /// </summary>
        private void OnLeftHandSensitivityChanged(object value)
        {
            if (value is float sensitivity && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.leftHandSensitivity = sensitivity;
                settingsManager.UpdateControlSettings(controlSettings);

                // 应用灵敏度设置
                ApplyHandSensitivity(true, sensitivity);
            }
        }

        /// <summary>
        /// 右手灵敏度变更事件
        /// </summary>
        private void OnRightHandSensitivityChanged(object value)
        {
            if (value is float sensitivity && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.rightHandSensitivity = sensitivity;
                settingsManager.UpdateControlSettings(controlSettings);

                // 应用灵敏度设置
                ApplyHandSensitivity(false, sensitivity);
            }
        }

        /// <summary>
        /// 触觉反馈变更事件
        /// </summary>
        private void OnHapticFeedbackChanged(object value)
        {
            if (value is bool enabled && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.hapticFeedback = enabled;
                settingsManager.UpdateControlSettings(controlSettings);

                // 启用/禁用触觉反馈系统
                if (hapticFeedback != null)
                {
                    hapticFeedback.enabled = enabled;
                }
            }
        }

        /// <summary>
        /// 触觉反馈强度变更事件
        /// </summary>
        private void OnHapticIntensityChanged(object value)
        {
            if (value is float intensity && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.hapticIntensity = intensity;
                settingsManager.UpdateControlSettings(controlSettings);

                // 设置触觉反馈强度
                if (hapticFeedback != null)
                {
                    hapticFeedback.SetGlobalIntensity(intensity);
                }
            }
        }

        /// <summary>
        /// 主手偏好变更事件
        /// </summary>
        private void OnDominantHandChanged(object value)
        {
            if (value is int handIndex && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.dominantHand = (DominantHand)handIndex;
                settingsManager.UpdateControlSettings(controlSettings);

                // 应用主手设置
                ApplyDominantHandSettings((DominantHand)handIndex);
            }
        }

        #endregion

        #region 移动设置事件

        /// <summary>
        /// 移动方式变更事件
        /// </summary>
        private void OnMovementTypeChanged(object value)
        {
            if (value is int movementType && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.movementType = (MovementType)movementType;
                settingsManager.UpdateControlSettings(controlSettings);

                // 应用移动方式设置
                ApplyMovementType((MovementType)movementType);
            }
        }

        /// <summary>
        /// 移动速度变更事件
        /// </summary>
        private void OnMovementSpeedChanged(object value)
        {
            if (value is float speed && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.movementSpeed = speed;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 转向速度变更事件
        /// </summary>
        private void OnTurnSpeedChanged(object value)
        {
            if (value is float speed && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.turnSpeed = speed;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 传送移动变更事件
        /// </summary>
        private void OnTeleportMovementChanged(object value)
        {
            if (value is bool teleport && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.teleportMovement = teleport;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 边界显示变更事件
        /// </summary>
        private void OnBoundaryDisplayChanged(object value)
        {
            if (value is bool display && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.boundaryDisplay = display;
                settingsManager.UpdateControlSettings(controlSettings);

                // 控制VR边界显示
                ApplyBoundaryDisplay(display);
            }
        }

        #endregion

        #region 交互设置事件

        /// <summary>
        /// 交互模式变更事件
        /// </summary>
        private void OnInteractionModeChanged(object value)
        {
            if (value is int mode && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.interactionMode = (InteractionMode)mode;
                settingsManager.UpdateControlSettings(controlSettings);

                // 应用交互模式设置
                ApplyInteractionMode((InteractionMode)mode);
            }
        }

        /// <summary>
        /// 抓取距离变更事件
        /// </summary>
        private void OnGrabDistanceChanged(object value)
        {
            if (value is float distance && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.grabDistance = distance;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 指向距离变更事件
        /// </summary>
        private void OnPointingDistanceChanged(object value)
        {
            if (value is float distance && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.pointingDistance = distance;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 自动抓取变更事件
        /// </summary>
        private void OnAutoGrabChanged(object value)
        {
            if (value is bool autoGrab && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.autoGrab = autoGrab;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        #endregion

        #region 手部追踪设置事件

        /// <summary>
        /// 手部追踪变更事件
        /// </summary>
        private void OnHandTrackingChanged(object value)
        {
            if (value is bool enabled && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.handTracking = enabled;
                settingsManager.UpdateControlSettings(controlSettings);

                // 启用/禁用手部追踪
                ApplyHandTracking(enabled);
            }
        }

        /// <summary>
        /// 手势识别变更事件
        /// </summary>
        private void OnGestureRecognitionChanged(object value)
        {
            if (value is bool enabled && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.gestureRecognition = enabled;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 手部追踪精度变更事件
        /// </summary>
        private void OnHandTrackingAccuracyChanged(object value)
        {
            if (value is float accuracy && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.handTrackingAccuracy = accuracy;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        #endregion

        #region 按键映射事件

        /// <summary>
        /// 左手触发器映射变更事件
        /// </summary>
        private void OnLeftTriggerMappingChanged(object value)
        {
            if (value is int mapping && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.leftTriggerMapping = (TriggerMapping)mapping;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 右手触发器映射变更事件
        /// </summary>
        private void OnRightTriggerMappingChanged(object value)
        {
            if (value is int mapping && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.rightTriggerMapping = (TriggerMapping)mapping;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        #endregion

        #region 高级设置事件

        /// <summary>
        /// 死区大小变更事件
        /// </summary>
        private void OnDeadZoneSizeChanged(object value)
        {
            if (value is float deadZone && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.deadZoneSize = deadZone;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 输入平滑变更事件
        /// </summary>
        private void OnInputSmoothingChanged(object value)
        {
            if (value is float smoothing && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.inputSmoothing = smoothing;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        /// <summary>
        /// 手柄振动变更事件
        /// </summary>
        private void OnControllerVibrationChanged(object value)
        {
            if (value is bool vibration && settingsManager != null)
            {
                var controlSettings = settingsManager.GetControlSettings();
                controlSettings.controllerVibration = vibration;
                settingsManager.UpdateControlSettings(controlSettings);
            }
        }

        #endregion

        #region 设置应用方法

        /// <summary>
        /// 应用手部灵敏度设置
        /// </summary>
        /// <param name="isLeftHand">是否为左手</param>
        /// <param name="sensitivity">灵敏度值</param>
        private void ApplyHandSensitivity(bool isLeftHand, float sensitivity)
        {
            // 这里可以调整VR手柄的输入灵敏度
            Debug.Log($"{(isLeftHand ? "Left" : "Right")} hand sensitivity set to {sensitivity}");
        }

        /// <summary>
        /// 应用主手设置
        /// </summary>
        /// <param name="dominantHand">主手偏好</param>
        private void ApplyDominantHandSettings(DominantHand dominantHand)
        {
            // 根据主手偏好调整UI布局和交互逻辑
            Debug.Log($"Dominant hand set to {dominantHand}");
        }

        /// <summary>
        /// 应用移动方式设置
        /// </summary>
        /// <param name="movementType">移动方式</param>
        private void ApplyMovementType(MovementType movementType)
        {
            // 切换移动模式（传送、平滑移动等）
            Debug.Log($"Movement type set to {movementType}");
        }

        /// <summary>
        /// 应用边界显示设置
        /// </summary>
        /// <param name="display">是否显示边界</param>
        private void ApplyBoundaryDisplay(bool display)
        {
            // 控制VR边界的显示
            if (XRSettings.enabled)
            {
                // 具体的VR SDK边界控制代码
                Debug.Log($"Boundary display: {(display ? "Enabled" : "Disabled")}");
            }
        }

        /// <summary>
        /// 应用交互模式设置
        /// </summary>
        /// <param name="mode">交互模式</param>
        private void ApplyInteractionMode(InteractionMode mode)
        {
            // 切换交互模式（射线、直接接触等）
            Debug.Log($"Interaction mode set to {mode}");
        }

        /// <summary>
        /// 应用手部追踪设置
        /// </summary>
        /// <param name="enabled">是否启用手部追踪</param>
        private void ApplyHandTracking(bool enabled)
        {
            // 启用/禁用手部追踪功能
            Debug.Log($"Hand tracking: {(enabled ? "Enabled" : "Disabled")}");
        }

        #endregion

        #region 输入监控和测试

        /// <summary>
        /// 更新输入监控
        /// </summary>
        private void UpdateInputMonitoring()
        {
            // 监控左手输入状态
            if (leftHandDevice.isValid && leftHandStatusText != null)
            {
                bool triggerPressed = false;
                bool gripPressed = false;

                leftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
                leftHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed);

                leftHandStatusText.text = $"左手: 触发器{(triggerPressed ? "按下" : "松开")} 握把{(gripPressed ? "按下" : "松开")}";
            }

            // 监控右手输入状态
            if (rightHandDevice.isValid && rightHandStatusText != null)
            {
                bool triggerPressed = false;
                bool gripPressed = false;

                rightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
                rightHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed);

                rightHandStatusText.text = $"右手: 触发器{(triggerPressed ? "按下" : "松开")} 握把{(gripPressed ? "按下" : "松开")}";
            }
        }

        /// <summary>
        /// 开始手部测试
        /// </summary>
        /// <param name="leftHand">是否测试左手</param>
        private void StartHandTest(bool leftHand)
        {
            if (leftHand)
            {
                isTestingLeftHand = true;
                if (testLeftHandButton != null)
                {
                    var colors = testLeftHandButton.colors;
                    colors.normalColor = Color.yellow;
                    testLeftHandButton.colors = colors;
                }
            }
            else
            {
                isTestingRightHand = true;
                if (testRightHandButton != null)
                {
                    var colors = testRightHandButton.colors;
                    colors.normalColor = Color.yellow;
                    testRightHandButton.colors = colors;
                }
            }

            // 停止测试的延迟调用
            Invoke(nameof(StopHandTests), 3f);

            // 触觉反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
            }
        }

        /// <summary>
        /// 更新手部测试状态
        /// </summary>
        private void UpdateHandTesting()
        {
            if (isTestingLeftHand && leftHandDevice.isValid)
            {
                // 监测左手输入并提供反馈
                bool triggerPressed = false;
                leftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

                if (triggerPressed && hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ButtonPress);
                }
            }

            if (isTestingRightHand && rightHandDevice.isValid)
            {
                // 监测右手输入并提供反馈
                bool triggerPressed = false;
                rightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

                if (triggerPressed && hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ButtonPress);
                }
            }
        }

        /// <summary>
        /// 停止手部测试
        /// </summary>
        private void StopHandTests()
        {
            isTestingLeftHand = false;
            isTestingRightHand = false;

            // 恢复按钮颜色
            if (testLeftHandButton != null)
            {
                var colors = testLeftHandButton.colors;
                colors.normalColor = Color.white;
                testLeftHandButton.colors = colors;
            }

            if (testRightHandButton != null)
            {
                var colors = testRightHandButton.colors;
                colors.normalColor = Color.white;
                testRightHandButton.colors = colors;
            }
        }

        /// <summary>
        /// 重新校准
        /// </summary>
        private void OnRecalibrate()
        {
            // 重新校准VR设备
            Debug.Log("VR device recalibration started");

            // 触觉反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
            }
        }

        #endregion

        #region 事件注册

        /// <summary>
        /// 注册输入事件
        /// </summary>
        private void RegisterInputEvents()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        /// <summary>
        /// 取消注册输入事件
        /// </summary>
        private void UnregisterInputEvents()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        /// <summary>
        /// 设备连接事件
        /// </summary>
        private void OnDeviceConnected(InputDevice device)
        {
            Debug.Log($"Device connected: {device.name}");
            InitializeVRDevices(); // 重新初始化设备
        }

        /// <summary>
        /// 设备断开事件
        /// </summary>
        private void OnDeviceDisconnected(InputDevice device)
        {
            Debug.Log($"Device disconnected: {device.name}");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 刷新面板
        /// </summary>
        public void RefreshPanel()
        {
            if (settingsManager == null) return;

            var controlSettings = settingsManager.GetControlSettings();

            // 更新手柄设置
            leftHandSensitivitySlider?.SetValue(controlSettings.leftHandSensitivity);
            rightHandSensitivitySlider?.SetValue(controlSettings.rightHandSensitivity);
            hapticFeedbackToggle?.SetValue(controlSettings.hapticFeedback);
            hapticIntensitySlider?.SetValue(controlSettings.hapticIntensity);
            dominantHandDropdown?.SetValue((int)controlSettings.dominantHand);

            // 更新移动设置
            movementTypeDropdown?.SetValue((int)controlSettings.movementType);
            movementSpeedSlider?.SetValue(controlSettings.movementSpeed);
            turnSpeedSlider?.SetValue(controlSettings.turnSpeed);
            teleportMovementToggle?.SetValue(controlSettings.teleportMovement);
            boundaryDisplayToggle?.SetValue(controlSettings.boundaryDisplay);

            // 更新交互设置
            interactionModeDropdown?.SetValue((int)controlSettings.interactionMode);
            grabDistanceSlider?.SetValue(controlSettings.grabDistance);
            pointingDistanceSlider?.SetValue(controlSettings.pointingDistance);
            autoGrabToggle?.SetValue(controlSettings.autoGrab);

            // 更新手部追踪设置
            handTrackingToggle?.SetValue(controlSettings.handTracking);
            gestureRecognitionToggle?.SetValue(controlSettings.gestureRecognition);
            handTrackingAccuracySlider?.SetValue(controlSettings.handTrackingAccuracy);

            // 更新按键映射
            leftTriggerMappingDropdown?.SetValue((int)controlSettings.leftTriggerMapping);
            rightTriggerMappingDropdown?.SetValue((int)controlSettings.rightTriggerMapping);

            // 更新高级设置
            deadZoneSizeSlider?.SetValue(controlSettings.deadZoneSize);
            inputSmoothingSlider?.SetValue(controlSettings.inputSmoothing);
            controllerVibrationToggle?.SetValue(controlSettings.controllerVibration);
        }

        #endregion

        #region 清理

        private void OnDestroy()
        {
            UnregisterInputEvents();

            // 清理所有事件监听
            if (leftHandSensitivitySlider != null)
                leftHandSensitivitySlider.OnValueChanged -= OnLeftHandSensitivityChanged;
            if (rightHandSensitivitySlider != null)
                rightHandSensitivitySlider.OnValueChanged -= OnRightHandSensitivityChanged;
            if (hapticFeedbackToggle != null)
                hapticFeedbackToggle.OnValueChanged -= OnHapticFeedbackChanged;
            if (hapticIntensitySlider != null)
                hapticIntensitySlider.OnValueChanged -= OnHapticIntensityChanged;

            // ... 清理其他事件监听器

            // 清理按钮事件
            if (recalibrateButton != null)
                recalibrateButton.onClick.RemoveAllListeners();
            if (testLeftHandButton != null)
                testLeftHandButton.onClick.RemoveAllListeners();
            if (testRightHandButton != null)
                testRightHandButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}