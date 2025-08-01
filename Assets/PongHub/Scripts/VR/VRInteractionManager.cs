using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using XRController = UnityEngine.XR.Interaction.Toolkit.XRController;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.VR.Avatar;
using Meta.Utilities.Input;
using System.Collections.Generic;
using System.Collections;

namespace PongHub.VR
{
    /// <summary>
    /// VR交互类型枚举
    /// </summary>
    public enum VRInteractionType
    {
        Hover,          // 悬停
        HoverExit,      // 悬停退出
        Grab,           // 抓取
        Release,        // 释放
        RayHover,       // 射线悬停
        RayHoverExit,   // 射线悬停退出
        RaySelect,      // 射线选择
        RayDeselect,    // 射线取消选择
        ButtonPress,    // 按钮按下
        MenuOpen,       // 菜单打开
        MenuClose       // 菜单关闭
    }

    /// <summary>
    /// VR性能监控器
    /// </summary>
    public class VRPerformanceMonitor
    {
        private Queue<float> m_frameTimeHistory = new Queue<float>();
        private int m_interactionCount = 0;
        private float m_averageFrameTime = 0f;
        private const int MAX_HISTORY_SIZE = 120; // 1秒历史记录

        public void RecordFrameTime(float deltaTime)
        {
            m_frameTimeHistory.Enqueue(deltaTime);
            
            if (m_frameTimeHistory.Count > MAX_HISTORY_SIZE)
                m_frameTimeHistory.Dequeue();
                
            // 计算平均帧时间
            float total = 0f;
            foreach (float time in m_frameTimeHistory)
                total += time;
            m_averageFrameTime = total / m_frameTimeHistory.Count;
        }

        public void RecordInteraction()
        {
            m_interactionCount++;
        }

        public float GetAverageFrameTime() => m_averageFrameTime;
        public float GetCurrentFPS() => m_averageFrameTime > 0 ? 1f / m_averageFrameTime : 0f;
        public int GetInteractionCount() => m_interactionCount;
        public bool IsPerformanceGood() => GetCurrentFPS() >= 90f; // VR基准90fps
    }

    public class VRInteractionManager : MonoBehaviour
    {
        [Header("控制器引用")]
        [SerializeField]
        [Tooltip("Left Controller / 左控制器 - Reference to the left XR controller")]
        private XRController m_leftController;

        [SerializeField]
        [Tooltip("Right Controller / 右控制器 - Reference to the right XR controller")]
        private XRController m_rightController;

        [Header("交互器引用")]
        [SerializeField]
        [Tooltip("Left Interactor / 左交互器 - Base interactor for left hand direct interaction")]
        private XRBaseInteractor m_leftInteractor;

        [SerializeField]
        [Tooltip("Right Interactor / 右交互器 - Base interactor for right hand direct interaction")]
        private XRBaseInteractor m_rightInteractor;

        [SerializeField]
        [Tooltip("Left Ray Interactor / 左射线交互器 - Ray interactor for left hand pointer interaction")]
        private XRRayInteractor m_leftRayInteractor;

        [SerializeField]
        [Tooltip("Right Ray Interactor / 右射线交互器 - Ray interactor for right hand pointer interaction")]
        private XRRayInteractor m_rightRayInteractor;

        [Header("输入动作")]
        [SerializeField]
        [Tooltip("Left Grip Action / 左手握持动作 - Input action for left hand grip button")]
        private InputActionReference m_leftGripAction;

        [SerializeField]
        [Tooltip("Right Grip Action / 右手握持动作 - Input action for right hand grip button")]
        private InputActionReference m_rightGripAction;

        [SerializeField]
        [Tooltip("Left Trigger Action / 左手扳机动作 - Input action for left hand trigger button")]
        private InputActionReference m_leftTriggerAction;

        [SerializeField]
        [Tooltip("Right Trigger Action / 右手扳机动作 - Input action for right hand trigger button")]
        private InputActionReference m_rightTriggerAction;

        [SerializeField]
        [Tooltip("Left Activate Action / 左手激活动作 - Input action for left hand activate button")]
        private InputActionReference m_leftActivateAction;

        [SerializeField]
        [Tooltip("Right Activate Action / 右手激活动作 - Input action for right hand activate button")]
        private InputActionReference m_rightActivateAction;

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("Grab Threshold / 抓取阈值 - Minimum input value required to trigger grab")]
        private float m_grabThreshold = 0.1f;

        [SerializeField]
        [Tooltip("Throw Force / 投掷力度 - Force multiplier for throwing objects")]
        private float m_throwForce = 10f;

        [SerializeField]
        [Tooltip("Throw Angle / 投掷角度 - Angle adjustment for throwing trajectory")]
        private float m_throwAngle = 45f;

        private bool m_isLeftGrabbing;
        private bool m_isRightGrabbing;
        private GameObject m_leftGrabbedObject;
        private GameObject m_rightGrabbedObject;

        [Header("交互反馈设置")]
        [SerializeField]
        [Tooltip("悬停效果强度")]
        private float m_hoverEffectIntensity = 0.3f;

        [SerializeField]
        [Tooltip("抓取效果强度")]
        private float m_grabEffectIntensity = 0.6f;

        [SerializeField]
        [Tooltip("射线选择效果强度")]
        private float m_raySelectIntensity = 0.4f;

        // 内部组件引用
        private VibrationManager m_vibrationManager;
        private AudioManager m_audioManager;
        private VRPerformanceMonitor m_performanceMonitor;
        
        // 跟踪状态验证
        private Dictionary<XRNode, bool> m_controllerTrackingState = new Dictionary<XRNode, bool>();
        
        // 悬停对象缓存
        private Dictionary<GameObject, Coroutine> m_hoverEffectCoroutines = new Dictionary<GameObject, Coroutine>();
        
        [Header("Hand Tracking Integration")]
        [SerializeField]
        [Tooltip("增强XR输入管理器引用")]
        private EnhancedXRInputManager m_enhancedInputManager;
        
        [SerializeField]
        [Tooltip("是否启用Hand Tracking交互")]
        private bool m_enableHandTrackingInteraction = true;
        
        // Hand Tracking状态
        private bool m_handTrackingInitialized = false;
        private Dictionary<bool, EnhancedXRInputManager.HandGesture> m_lastHandGestures = new Dictionary<bool, EnhancedXRInputManager.HandGesture>();

        [Header("Mixed Reality Integration")]
        [SerializeField]
        [Tooltip("MR透视管理器引用")]
        private PongHub.MR.MRPassthroughManager m_mrPassthroughManager;
        
        [SerializeField]
        [Tooltip("环境融合系统引用")]
        private PongHub.MR.EnvironmentBlendingSystem m_environmentBlendingSystem;
        
        [SerializeField]
        [Tooltip("MR安全边界系统引用")]
        private PongHub.MR.MRSafetyBoundary m_mrSafetyBoundary;
        
        [SerializeField]
        [Tooltip("是否启用MR功能")]
        private bool m_enableMRFeatures = false;
        
        [SerializeField]
        [Tooltip("自动切换MR模式")]
        private bool m_autoSwitchMRMode = true;
        
        // MR状态管理
        private bool m_mrInitialized = false;
        private PongHub.MR.MRPassthroughManager.PassthroughMode m_currentMRMode = PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled;
        private bool m_isMRSafetyActive = true;

        [Header("Avatar System Integration")]
        [SerializeField]
        [Tooltip("VR Avatar管理器引用")]
        private VRAvatarManager m_vrAvatarManager;
        
        [SerializeField]
        [Tooltip("Avatar动作同步组件引用")]
        private AvatarMotionSync m_avatarMotionSync;
        
        [SerializeField]
        [Tooltip("Avatar表情系统引用")]
        private AvatarExpressionSystem m_avatarExpressionSystem;
        
        [SerializeField]
        [Tooltip("Avatar网络同步组件引用")]
        private NetworkAvatarSync m_networkAvatarSync;
        
        [SerializeField]
        [Tooltip("是否启用Avatar集成")]
        private bool m_enableAvatarIntegration = true;
        
        [SerializeField]
        [Tooltip("Avatar情绪响应强度")]
        [Range(0.1f, 2f)]
        private float m_avatarEmotionIntensity = 1f;
        
        [SerializeField]
        [Tooltip("启用Avatar手势反应")]
        private bool m_enableAvatarGestureReaction = true;
        
        // Avatar状态管理
        private bool m_avatarSystemInitialized = false;
        private Dictionary<string, float> m_avatarEmotionTimers = new Dictionary<string, float>();

        private void Awake()
        {
            SetupControllers();
            SetupInteractors();
            InitializeComponents();
            InitializePerformanceMonitor();
        }

        private void InitializeComponents()
        {
            m_vibrationManager = VibrationManager.Instance;
            m_audioManager = AudioManager.Instance;
            
            if (m_vibrationManager == null)
            {
                Debug.LogWarning("[VRInteractionManager] VibrationManager instance not found");
            }
            
            if (m_audioManager == null)
            {
                Debug.LogWarning("[VRInteractionManager] AudioManager instance not found");
            }
            
            // 初始化Hand Tracking
            InitializeHandTracking();
            
            // 初始化MR功能
            InitializeMRIntegration();
            
            // 初始化Avatar系统
            InitializeAvatarSystem();
        }
        
        private void InitializeHandTracking()
        {
            // 如果没有手动分配，尝试自动查找
            if (m_enhancedInputManager == null)
            {
                m_enhancedInputManager = FindObjectOfType<EnhancedXRInputManager>();
            }
            
            if (m_enhancedInputManager != null && m_enableHandTrackingInteraction)
            {
                // 注册手势事件
                m_enhancedInputManager.OnGestureRecognized += OnHandGestureRecognized;
                m_enhancedInputManager.OnInputModeChanged += OnInputModeChanged;
                
                // 初始化手势状态
                m_lastHandGestures[true] = EnhancedXRInputManager.HandGesture.None;  // 左手
                m_lastHandGestures[false] = EnhancedXRInputManager.HandGesture.None; // 右手
                
                m_handTrackingInitialized = true;
                Debug.Log("[VRInteractionManager] Hand Tracking integration initialized");
            }
            else
            {
                Debug.LogWarning("[VRInteractionManager] EnhancedXRInputManager not found or Hand Tracking disabled");
            }
        }
        
        private void InitializeMRIntegration()
        {
            if (!m_enableMRFeatures)
            {
                Debug.Log("[VRInteractionManager] MR features disabled");
                return;
            }
            
            // 自动查找MR组件（如果没有手动分配）
            if (m_mrPassthroughManager == null)
            {
                m_mrPassthroughManager = FindObjectOfType<PongHub.MR.MRPassthroughManager>();
            }
            
            if (m_environmentBlendingSystem == null)
            {
                m_environmentBlendingSystem = FindObjectOfType<PongHub.MR.EnvironmentBlendingSystem>();
            }
            
            if (m_mrSafetyBoundary == null)
            {
                m_mrSafetyBoundary = FindObjectOfType<PongHub.MR.MRSafetyBoundary>();
            }
            
            // 注册MR事件
            if (m_mrPassthroughManager != null)
            {
                m_mrPassthroughManager.OnPassthroughModeChanged.AddListener(OnMRModeChanged);
                m_mrPassthroughManager.OnPassthroughAvailabilityChanged.AddListener(OnMRAvailabilityChanged);
                Debug.Log("[VRInteractionManager] MR Passthrough Manager connected");
            }
            
            if (m_mrSafetyBoundary != null)
            {
                m_mrSafetyBoundary.OnBoundaryWarningChanged += OnMRBoundaryWarning;
                m_mrSafetyBoundary.OnEmergencyStop += OnMREmergencyStop;
                Debug.Log("[VRInteractionManager] MR Safety Boundary connected");
            }
            
            if (m_environmentBlendingSystem != null)
            {
                Debug.Log("[VRInteractionManager] Environment Blending System connected");
            }
            
            m_mrInitialized = true;
            Debug.Log("[VRInteractionManager] MR integration initialized");
        }
        
        private void InitializeAvatarSystem()
        {
            if (!m_enableAvatarIntegration)
            {
                Debug.Log("[VRInteractionManager] Avatar integration disabled");
                return;
            }
            
            // 自动查找Avatar组件（如果没有手动分配）
            if (m_vrAvatarManager == null)
            {
                m_vrAvatarManager = FindObjectOfType<VRAvatarManager>();
            }
            
            if (m_avatarMotionSync == null)
            {
                m_avatarMotionSync = FindObjectOfType<AvatarMotionSync>();
            }
            
            if (m_avatarExpressionSystem == null)
            {
                m_avatarExpressionSystem = FindObjectOfType<AvatarExpressionSystem>();
            }
            
            if (m_networkAvatarSync == null)
            {
                m_networkAvatarSync = FindObjectOfType<NetworkAvatarSync>();
            }
            
            // 注册Avatar事件
            if (m_vrAvatarManager != null)
            {
                m_vrAvatarManager.OnAvatarStateChanged.AddListener(OnAvatarStateChanged);
                m_vrAvatarManager.OnAvatarLoaded.AddListener(OnAvatarLoaded);
                m_vrAvatarManager.OnAvatarError.AddListener(OnAvatarError);
                Debug.Log("[VRInteractionManager] VR Avatar Manager connected");
            }
            
            if (m_avatarMotionSync != null)
            {
                m_avatarMotionSync.OnTrackingModeChanged.AddListener(OnAvatarTrackingModeChanged);
                m_avatarMotionSync.OnHandTrackingStateChanged.AddListener(OnAvatarHandTrackingChanged);
                m_avatarMotionSync.OnMotionSyncInitialized.AddListener(OnAvatarMotionSyncReady);
                Debug.Log("[VRInteractionManager] Avatar Motion Sync connected");
            }
            
            if (m_avatarExpressionSystem != null)
            {
                m_avatarExpressionSystem.OnExpressionChanged.AddListener(OnAvatarExpressionChanged);
                m_avatarExpressionSystem.OnSpeechDetected.AddListener(OnAvatarSpeechDetected);
                m_avatarExpressionSystem.OnGazeDirectionChanged.AddListener(OnAvatarGazeChanged);
                m_avatarExpressionSystem.OnExpressionSystemInitialized.AddListener(OnAvatarExpressionSystemReady);
                Debug.Log("[VRInteractionManager] Avatar Expression System connected");
            }
            
            if (m_networkAvatarSync != null)
            {
                m_networkAvatarSync.OnAvatarConnected.AddListener(OnNetworkAvatarConnected);
                m_networkAvatarSync.OnAvatarDisconnected.AddListener(OnNetworkAvatarDisconnected);
                m_networkAvatarSync.OnNetworkQualityChanged.AddListener(OnAvatarNetworkQualityChanged);
                Debug.Log("[VRInteractionManager] Network Avatar Sync connected");
            }
            
            // 初始化情绪计时器
            m_avatarEmotionTimers["victory"] = 0f;
            m_avatarEmotionTimers["defeat"] = 0f;
            m_avatarEmotionTimers["surprise"] = 0f;
            m_avatarEmotionTimers["focus"] = 0f;
            
            m_avatarSystemInitialized = true;
            Debug.Log("[VRInteractionManager] Avatar system integration initialized");
        }

        private void InitializePerformanceMonitor()
        {
            m_performanceMonitor = new VRPerformanceMonitor();
            
            // 初始化控制器跟踪状态
            m_controllerTrackingState[XRNode.LeftHand] = false;
            m_controllerTrackingState[XRNode.RightHand] = false;
        }

        private void Update()
        {
            UpdatePerformanceMonitoring();
            ValidateControllerTracking();
            UpdateAvatarSystem();
        }

        private void UpdatePerformanceMonitoring()
        {
            if (m_performanceMonitor != null)
            {
                m_performanceMonitor.RecordFrameTime(Time.deltaTime);
                
                // 如果性能不佳，记录警告
                if (!m_performanceMonitor.IsPerformanceGood())
                {
                    Debug.LogWarning($"[VRInteractionManager] Performance issue detected. FPS: {m_performanceMonitor.GetCurrentFPS():F1}");
                }
            }
        }
        
        private void UpdateAvatarSystem()
        {
            if (!m_avatarSystemInitialized || !m_enableAvatarIntegration)
                return;
                
            // 更新情绪计时器
            UpdateAvatarEmotionTimers();
            
            // 同步Avatar与VR交互状态
            SyncAvatarWithInteractionState();
        }
        
        private void UpdateAvatarEmotionTimers()
        {
            var keysToUpdate = new List<string>(m_avatarEmotionTimers.Keys);
            
            foreach (var key in keysToUpdate)
            {
                if (m_avatarEmotionTimers[key] > 0f)
                {
                    m_avatarEmotionTimers[key] -= Time.deltaTime;
                    if (m_avatarEmotionTimers[key] <= 0f)
                    {
                        m_avatarEmotionTimers[key] = 0f;
                    }
                }
            }
        }
        
        private void SyncAvatarWithInteractionState()
        {
            if (m_avatarExpressionSystem == null) return;
            
            // 根据交互状态调整Avatar表情
            if (m_isLeftGrabbing || m_isRightGrabbing)
            {
                // 抓取时显示专注表情
                TriggerAvatarEmotion("focus", 0.7f);
            }
            
            // 根据Hand Tracking状态调整Avatar
            if (m_handTrackingInitialized && m_avatarMotionSync != null)
            {
                var currentMode = GetCurrentInputMode();
                if (currentMode == EnhancedXRInputManager.VRInputMode.HandTracking ||
                    currentMode == EnhancedXRInputManager.VRInputMode.Hybrid)
                {
                    // Hand Tracking激活时，确保Avatar动作同步质量更高
                    m_avatarMotionSync.SetSyncQuality(AvatarMotionSync.SyncQuality.High);
                }
            }
        }

        private void ValidateControllerTracking()
        {
            ValidateControllerTrackingForNode(XRNode.LeftHand);
            ValidateControllerTrackingForNode(XRNode.RightHand);
        }

        private void ValidateControllerTrackingForNode(XRNode node)
        {
            var device = InputDevices.GetDeviceAtNode(node);
            bool isTracking = false;
            
            if (device.isValid)
            {
                // 检查位置和旋转跟踪
                bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
                bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
                bool hasTrackingState = device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState trackingState);
                
                isTracking = hasPosition && hasRotation && hasTrackingState && 
                           (trackingState & (InputTrackingState.Position | InputTrackingState.Rotation)) != 0;
            }
            
            // 跟踪状态变化时记录
            if (m_controllerTrackingState.TryGetValue(node, out bool previousState))
            {
                if (previousState != isTracking)
                {
                    Debug.Log($"[VRInteractionManager] Controller {node} tracking changed: {isTracking}");
                    m_controllerTrackingState[node] = isTracking;
                }
            }
            else
            {
                m_controllerTrackingState[node] = isTracking;
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void EnableInputActions()
        {
            m_leftGripAction?.action.Enable();
            m_rightGripAction?.action.Enable();
            m_leftTriggerAction?.action.Enable();
            m_rightTriggerAction?.action.Enable();
        }

        private void DisableInputActions()
        {
            m_leftGripAction?.action.Disable();
            m_rightGripAction?.action.Disable();
            m_leftTriggerAction?.action.Disable();
            m_rightTriggerAction?.action.Disable();
        }

        private void SetupControllers()
        {
            if (m_leftController != null)
            {
                m_leftController.enableInputActions = true;
            }

            if (m_rightController != null)
            {
                m_rightController.enableInputActions = true;
            }
        }

        private void SetupInteractors()
        {
            // 设置直接交互器
            if (m_leftInteractor != null)
            {
                m_leftInteractor.enabled = true;
                m_leftInteractor.hoverEntered.AddListener(OnLeftHoverEntered);
                m_leftInteractor.hoverExited.AddListener(OnLeftHoverExited);
                m_leftInteractor.selectEntered.AddListener(OnLeftSelectEntered);
                m_leftInteractor.selectExited.AddListener(OnLeftSelectExited);
            }

            if (m_rightInteractor != null)
            {
                m_rightInteractor.enabled = true;
                m_rightInteractor.hoverEntered.AddListener(OnRightHoverEntered);
                m_rightInteractor.hoverExited.AddListener(OnRightHoverExited);
                m_rightInteractor.selectEntered.AddListener(OnRightSelectEntered);
                m_rightInteractor.selectExited.AddListener(OnRightSelectExited);
            }

            // 设置射线交互器
            if (m_leftRayInteractor != null)
            {
                m_leftRayInteractor.enabled = true;
                m_leftRayInteractor.hoverEntered.AddListener(OnLeftRayHoverEntered);
                m_leftRayInteractor.hoverExited.AddListener(OnLeftRayHoverExited);
                m_leftRayInteractor.selectEntered.AddListener(OnLeftRaySelectEntered);
                m_leftRayInteractor.selectExited.AddListener(OnLeftRaySelectExited);
            }

            if (m_rightRayInteractor != null)
            {
                m_rightRayInteractor.enabled = true;
                m_rightRayInteractor.hoverEntered.AddListener(OnRightRayHoverEntered);
                m_rightRayInteractor.hoverExited.AddListener(OnRightRayHoverExited);
                m_rightRayInteractor.selectEntered.AddListener(OnRightRaySelectEntered);
                m_rightRayInteractor.selectExited.AddListener(OnRightRaySelectExited);
            }
        }

        #region 交互反馈系统
        
        /// <summary>
        /// 触发VR交互反馈（触觉+音频）
        /// </summary>
        private void TriggerInteractionFeedback(VRInteractionType interactionType, bool isLeftHand, GameObject interactable = null)
        {
            // 记录性能监控
            m_performanceMonitor?.RecordInteraction();
            
            // 触觉反馈
            TriggerHapticFeedback(interactionType, isLeftHand);
            
            // 音频反馈
            TriggerAudioFeedback(interactionType, interactable);
            
            // 视觉效果
            TriggerVisualFeedback(interactionType, interactable);
        }
        
        /// <summary>
        /// 触发触觉反馈
        /// </summary>
        private void TriggerHapticFeedback(VRInteractionType interactionType, bool isLeftHand)
        {
            if (m_vibrationManager == null) return;
            
            VibrationManager.VibrationType vibrationType = VibrationManager.VibrationType.ButtonPress;
            
            switch (interactionType)
            {
                case VRInteractionType.Hover:
                case VRInteractionType.RayHover:
                    vibrationType = VibrationManager.VibrationType.UIInteraction;
                    break;
                case VRInteractionType.Grab:
                    vibrationType = VibrationManager.VibrationType.Grab;
                    break;
                case VRInteractionType.Release:
                    vibrationType = VibrationManager.VibrationType.Release;
                    break;
                case VRInteractionType.RaySelect:
                    vibrationType = VibrationManager.VibrationType.ButtonPress;
                    break;
                case VRInteractionType.ButtonPress:
                case VRInteractionType.MenuOpen:
                case VRInteractionType.MenuClose:
                    vibrationType = VibrationManager.VibrationType.ButtonPress;
                    break;
            }
            
            int handIndex = isLeftHand ? 0 : 1;
            m_vibrationManager.PlayVibration(vibrationType, handIndex);
        }
        
        /// <summary>
        /// 触发音频反馈
        /// </summary>
        private void TriggerAudioFeedback(VRInteractionType interactionType, GameObject interactable)
        {
            if (m_audioManager == null) return;
            
            Vector3 audioPosition = interactable != null ? interactable.transform.position : transform.position;
            
            switch (interactionType)
            {
                case VRInteractionType.Hover:
                case VRInteractionType.RayHover:
                    // UI交互音效（如果AudioManager有相关方法）
                    break;
                case VRInteractionType.Grab:
                    // 抓取音效
                    break;
                case VRInteractionType.Release:
                    // 释放音效
                    break;
                case VRInteractionType.RaySelect:
                case VRInteractionType.ButtonPress:
                    // 按钮点击音效
                    break;
                case VRInteractionType.MenuOpen:
                case VRInteractionType.MenuClose:
                    // 菜单音效
                    break;
            }
        }
        
        /// <summary>
        /// 触发视觉效果
        /// </summary>
        private void TriggerVisualFeedback(VRInteractionType interactionType, GameObject interactable)
        {
            if (interactable == null) return;
            
            switch (interactionType)
            {
                case VRInteractionType.Hover:
                case VRInteractionType.RayHover:
                    StartHoverEffect(interactable);
                    break;
                case VRInteractionType.HoverExit:
                case VRInteractionType.RayHoverExit:
                    StopHoverEffect(interactable);
                    break;
                case VRInteractionType.Grab:
                    StartGrabEffect(interactable);
                    break;
                case VRInteractionType.Release:
                    StopGrabEffect(interactable);
                    break;
                case VRInteractionType.RaySelect:
                    StartRaySelectEffect(interactable);
                    break;
                case VRInteractionType.RayDeselect:
                    StopRaySelectEffect(interactable);
                    break;
            }
        }
        
        /// <summary>
        /// 开始悬停效果
        /// </summary>
        private void StartHoverEffect(GameObject target)
        {
            if (target == null) return;
            
            // 停止之前的悬停效果
            StopHoverEffect(target);
            
            // 开始新的悬停效果协程
            var coroutine = StartCoroutine(HoverEffectCoroutine(target));
            m_hoverEffectCoroutines[target] = coroutine;
        }
        
        /// <summary>
        /// 停止悬停效果
        /// </summary>
        private void StopHoverEffect(GameObject target)
        {
            if (target == null) return;
            
            if (m_hoverEffectCoroutines.TryGetValue(target, out Coroutine coroutine))
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
                m_hoverEffectCoroutines.Remove(target);
            }
            
            // 重置对象的视觉状态
            ResetObjectVisuals(target);
        }
        
        /// <summary>
        /// 悬停效果协程
        /// </summary>
        private IEnumerator HoverEffectCoroutine(GameObject target)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) yield break;
            
            var originalColor = GetOriginalColor(renderer);
            var hoverColor = originalColor + Color.white * m_hoverEffectIntensity;
            
            float time = 0f;
            const float pulseSpeed = 2f;
            
            while (true)
            {
                time += Time.deltaTime * pulseSpeed;
                float alpha = (Mathf.Sin(time) + 1f) * 0.5f;
                var currentColor = Color.Lerp(originalColor, hoverColor, alpha * m_hoverEffectIntensity);
                
                SetObjectColor(renderer, currentColor);
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 开始抓取效果
        /// </summary>
        private void StartGrabEffect(GameObject target)
        {
            if (target == null) return;
            
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                var originalColor = GetOriginalColor(renderer);
                var grabColor = originalColor + Color.yellow * m_grabEffectIntensity;
                SetObjectColor(renderer, grabColor);
            }
        }
        
        /// <summary>
        /// 停止抓取效果
        /// </summary>
        private void StopGrabEffect(GameObject target)
        {
            if (target == null) return;
            ResetObjectVisuals(target);
        }
        
        /// <summary>
        /// 开始射线选择效果
        /// </summary>
        private void StartRaySelectEffect(GameObject target)
        {
            if (target == null) return;
            
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                var originalColor = GetOriginalColor(renderer);
                var selectColor = originalColor + Color.cyan * m_raySelectIntensity;
                SetObjectColor(renderer, selectColor);
            }
        }
        
        /// <summary>
        /// 停止射线选择效果
        /// </summary>
        private void StopRaySelectEffect(GameObject target)
        {
            if (target == null) return;
            ResetObjectVisuals(target);
        }
        
        /// <summary>
        /// 重置对象视觉状态
        /// </summary>
        private void ResetObjectVisuals(GameObject target)
        {
            if (target == null) return;
            
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                var originalColor = GetOriginalColor(renderer);
                SetObjectColor(renderer, originalColor);
            }
        }
        
        /// <summary>
        /// 获取对象原始颜色
        /// </summary>
        private Color GetOriginalColor(Renderer renderer)
        {
            if (renderer?.material != null)
            {
                return renderer.material.color;
            }
            return Color.white;
        }
        
        /// <summary>
        /// 设置对象颜色
        /// </summary>
        private void SetObjectColor(Renderer renderer, Color color)
        {
            if (renderer?.material != null)
            {
                renderer.material.color = color;
            }
        }
        
        #endregion
        #region 直接交互事件处理
        
        private void OnLeftHoverEntered(HoverEnterEventArgs args)
        {
            // 处理左手悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.Hover, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left hand hover entered: {interactable.transform.name}");
            }
        }

        private void OnLeftHoverExited(HoverExitEventArgs args)
        {
            // 处理左手悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.HoverExit, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left hand hover exited: {interactable.transform.name}");
            }
        }

        private void OnLeftSelectEntered(SelectEnterEventArgs args)
        {
            // 处理左手抓取
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isLeftGrabbing = true;
                m_leftGrabbedObject = interactable.transform.gameObject;
                TriggerInteractionFeedback(VRInteractionType.Grab, true, m_leftGrabbedObject);
                Debug.Log($"[VRInteractionManager] Left hand grabbed: {interactable.transform.name}");
            }
        }

        private void OnLeftSelectExited(SelectExitEventArgs args)
        {
            // 处理左手释放
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.Release, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left hand released: {interactable.transform.name}");
                
                m_isLeftGrabbing = false;
                m_leftGrabbedObject = null;
            }
        }

        private void OnRightHoverEntered(HoverEnterEventArgs args)
        {
            // 处理右手悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.Hover, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right hand hover entered: {interactable.transform.name}");
            }
        }

        private void OnRightHoverExited(HoverExitEventArgs args)
        {
            // 处理右手悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.HoverExit, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right hand hover exited: {interactable.transform.name}");
            }
        }

        private void OnRightSelectEntered(SelectEnterEventArgs args)
        {
            // 处理右手抓取
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isRightGrabbing = true;
                m_rightGrabbedObject = interactable.transform.gameObject;
                TriggerInteractionFeedback(VRInteractionType.Grab, false, m_rightGrabbedObject);
                Debug.Log($"[VRInteractionManager] Right hand grabbed: {interactable.transform.name}");
            }
        }

        private void OnRightSelectExited(SelectExitEventArgs args)
        {
            // 处理右手释放
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.Release, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right hand released: {interactable.transform.name}");
                
                m_isRightGrabbing = false;
                m_rightGrabbedObject = null;
            }
        }
        #endregion

        #region 射线交互事件处理
        #region 射线交互事件处理
        
        private void OnLeftRayHoverEntered(HoverEnterEventArgs args)
        {
            // 处理左手射线悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayHover, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left ray hover entered: {interactable.transform.name}");
            }
        }

        private void OnLeftRayHoverExited(HoverExitEventArgs args)
        {
            // 处理左手射线悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayHoverExit, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left ray hover exited: {interactable.transform.name}");
            }
        }

        private void OnLeftRaySelectEntered(SelectEnterEventArgs args)
        {
            // 处理左手射线选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RaySelect, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left ray selected: {interactable.transform.name}");
            }
        }

        private void OnLeftRaySelectExited(SelectExitEventArgs args)
        {
            // 处理左手射线取消选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayDeselect, true, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Left ray deselected: {interactable.transform.name}");
            }
        }

        private void OnRightRayHoverEntered(HoverEnterEventArgs args)
        {
            // 处理右手射线悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayHover, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right ray hover entered: {interactable.transform.name}");
            }
        }

        private void OnRightRayHoverExited(HoverExitEventArgs args)
        {
            // 处理右手射线悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayHoverExit, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right ray hover exited: {interactable.transform.name}");
            }
        }

        private void OnRightRaySelectEntered(SelectEnterEventArgs args)
        {
            // 处理右手射线选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RaySelect, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right ray selected: {interactable.transform.name}");
            }
        }

        private void OnRightRaySelectExited(SelectExitEventArgs args)
        {
            // 处理右手射线取消选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                TriggerInteractionFeedback(VRInteractionType.RayDeselect, false, interactable.transform.gameObject);
                Debug.Log($"[VRInteractionManager] Right ray deselected: {interactable.transform.name}");
            }
        }
        #endregion

        #region 公共API和工具方法
        
        /// <summary>
        /// 获取性能监控数据
        /// </summary>
        public VRPerformanceMonitor GetPerformanceMonitor()
        {
            return m_performanceMonitor;
        }
        
        /// <summary>
        /// 检查控制器跟踪状态
        /// </summary>
        public bool IsControllerTracking(XRNode node)
        {
            return m_controllerTrackingState.TryGetValue(node, out bool isTracking) && isTracking;
        }
        
        /// <summary>
        /// 获取控制器跟踪精度
        /// </summary>
        public float GetControllerTrackingAccuracy(XRNode node)
        {
            var device = InputDevices.GetDeviceAtNode(node);
            if (!device.isValid) return 0f;
            
            if (device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState trackingState))
            {
                float accuracy = 0f;
                if ((trackingState & InputTrackingState.Position) != 0) accuracy += 0.5f;
                if ((trackingState & InputTrackingState.Rotation) != 0) accuracy += 0.5f;
                return accuracy;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 手动触发交互反馈（供外部系统调用）
        /// </summary>
        public void TriggerManualFeedback(VRInteractionType interactionType, bool isLeftHand, GameObject target = null)
        {
            TriggerInteractionFeedback(interactionType, isLeftHand, target);
        }
        
        /// <summary>
        /// 强制停止所有视觉效果
        /// </summary>
        public void StopAllVisualEffects()
        {
            foreach (var kvp in m_hoverEffectCoroutines.ToArray())
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
                ResetObjectVisuals(kvp.Key);
            }
            m_hoverEffectCoroutines.Clear();
        }
        
        /// <summary>
        /// 获取当前悬停的对象数量
        /// </summary>
        public int GetHoveringObjectCount()
        {
            return m_hoverEffectCoroutines.Count;
        }
        
        /// <summary>
        /// 设置交互效果强度
        /// </summary>
        public void SetInteractionIntensity(float hoverIntensity, float grabIntensity, float raySelectIntensity)
        {
            m_hoverEffectIntensity = Mathf.Clamp01(hoverIntensity);
            m_grabEffectIntensity = Mathf.Clamp01(grabIntensity);
            m_raySelectIntensity = Mathf.Clamp01(raySelectIntensity);
        }
        
        /// <summary>
        /// 检查系统初始化状态
        /// </summary>
        public bool IsSystemInitialized()
        {
            return m_vibrationManager != null && m_audioManager != null && m_performanceMonitor != null;
        }
        
        /// <summary>
        /// 获取系统诊断信息
        /// </summary>
        public string GetSystemDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== VR Interaction Manager Diagnostics ===");
            diagnostics.AppendLine($"System Initialized: {IsSystemInitialized()}");
            diagnostics.AppendLine($"VibrationManager: {(m_vibrationManager != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"AudioManager: {(m_audioManager != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Left Controller Tracking: {IsControllerTracking(XRNode.LeftHand)}");
            diagnostics.AppendLine($"Right Controller Tracking: {IsControllerTracking(XRNode.RightHand)}");
            diagnostics.AppendLine($"Left Hand Grabbing: {m_isLeftGrabbing}");
            diagnostics.AppendLine($"Right Hand Grabbing: {m_isRightGrabbing}");
            diagnostics.AppendLine($"Hovering Objects: {GetHoveringObjectCount()}");
            
            // Hand Tracking状态
            diagnostics.AppendLine($"Hand Tracking Initialized: {m_handTrackingInitialized}");
            diagnostics.AppendLine($"Enhanced Input Manager: {(m_enhancedInputManager != null ? "OK" : "Missing")}");
            if (m_enhancedInputManager != null)
            {
                diagnostics.AppendLine($"Current Input Mode: {m_enhancedInputManager.CurrentInputMode}");
                diagnostics.AppendLine($"Hand Tracking Available: {m_enhancedInputManager.IsHandTrackingAvailable}");
                diagnostics.AppendLine($"Left Hand Gesture: {m_enhancedInputManager.GetCurrentHandGesture(true)}");
                diagnostics.AppendLine($"Right Hand Gesture: {m_enhancedInputManager.GetCurrentHandGesture(false)}");
                diagnostics.AppendLine($"Left Hand Confidence: {m_enhancedInputManager.GetHandTrackingConfidence(true):F2}");
                diagnostics.AppendLine($"Right Hand Confidence: {m_enhancedInputManager.GetHandTrackingConfidence(false):F2}");
            }
            
            // MR状态
            diagnostics.AppendLine($"MR Initialized: {m_mrInitialized}");
            diagnostics.AppendLine($"MR Features Enabled: {m_enableMRFeatures}");
            diagnostics.AppendLine($"Current MR Mode: {m_currentMRMode}");
            diagnostics.AppendLine($"MR Passthrough Manager: {(m_mrPassthroughManager != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Environment Blending System: {(m_environmentBlendingSystem != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"MR Safety Boundary: {(m_mrSafetyBoundary != null ? "OK" : "Missing")}");
            if (m_mrPassthroughManager != null)
            {
                diagnostics.AppendLine($"MR Available: {m_mrPassthroughManager.IsPassthroughAvailable}");
                diagnostics.AppendLine($"MR Opacity: {m_mrPassthroughManager.CurrentOpacity:F2}");
                diagnostics.AppendLine($"Near Boundary: {m_mrPassthroughManager.IsNearBoundary}");
            }
            
            // Avatar系统状态
            diagnostics.AppendLine($"Avatar System Initialized: {m_avatarSystemInitialized}");
            diagnostics.AppendLine($"Avatar Integration Enabled: {m_enableAvatarIntegration}");
            if (m_avatarSystemInitialized)
            {
                diagnostics.AppendLine(GetAvatarSystemDiagnostics());
            }
            
            if (m_performanceMonitor != null)
            {
                diagnostics.AppendLine($"Current FPS: {m_performanceMonitor.GetCurrentFPS():F1}");
                diagnostics.AppendLine($"Interaction Count: {m_performanceMonitor.GetInteractionCount()}");
                diagnostics.AppendLine($"Performance Good: {m_performanceMonitor.IsPerformanceGood()}");
            }
            
            return diagnostics.ToString();
        }
        
        /// <summary>
        /// 启用/禁用Hand Tracking交互
        /// </summary>
        public void SetHandTrackingEnabled(bool enabled)
        {
            m_enableHandTrackingInteraction = enabled;
            
            if (m_enhancedInputManager != null)
            {
                m_enhancedInputManager.SetHandTrackingEnabled(enabled);
            }
            
            Debug.Log($"[VRInteractionManager] Hand Tracking interaction {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 获取当前输入模式
        /// </summary>
        public EnhancedXRInputManager.VRInputMode GetCurrentInputMode()
        {
            return m_enhancedInputManager != null ? m_enhancedInputManager.CurrentInputMode : EnhancedXRInputManager.VRInputMode.Controller;
        }
        
        /// <summary>
        /// 手动切换输入模式
        /// </summary>
        public void SwitchInputMode(EnhancedXRInputManager.VRInputMode mode)
        {
            if (m_enhancedInputManager != null)
            {
                m_enhancedInputManager.SwitchToMode(mode);
            }
        }
        
        /// <summary>
        /// 获取手部位置（世界坐标）
        /// </summary>
        public Vector3 GetHandPosition(bool isLeftHand)
        {
            if (m_enhancedInputManager != null)
            {
                return m_enhancedInputManager.GetHandPosition(isLeftHand);
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// 获取扌部旋转（世界坐标）
        /// </summary>
        public Quaternion GetHandRotation(bool isLeftHand)
        {
            if (m_enhancedInputManager != null)
            {
                return m_enhancedInputManager.GetHandRotation(isLeftHand);
            }
            return Quaternion.identity;
        }
        
        /// <summary>
        /// 获取当前手势
        /// </summary>
        public EnhancedXRInputManager.HandGesture GetCurrentHandGesture(bool isLeftHand)
        {
            if (m_enhancedInputManager != null)
            {
                return m_enhancedInputManager.GetCurrentHandGesture(isLeftHand);
            }
            return EnhancedXRInputManager.HandGesture.None;
        }
        
        /// <summary>
        /// 获取手部追踪置信度
        /// </summary>
        public float GetHandTrackingConfidence(bool isLeftHand)
        {
            if (m_enhancedInputManager != null)
            {
                return m_enhancedInputManager.GetHandTrackingConfidence(isLeftHand);
            }
            return 0f;
        }
        
        /// <summary>
        /// Hand Tracking是否可用
        /// </summary>
        public bool IsHandTrackingAvailable()
        {
            return m_enhancedInputManager != null && m_enhancedInputManager.IsHandTrackingAvailable;
        }
        
        /// <summary>
        /// 注册手势回调
        /// </summary>
        public void RegisterHandGestureCallback(EnhancedXRInputManager.HandGesture gesture, System.Action<bool, bool> callback)
        {
            if (m_enhancedInputManager != null)
            {
                m_enhancedInputManager.RegisterGestureCallback(gesture, callback);
            }
        }
        
        /// <summary>
        /// 取消注册手势回调
        /// </summary>
        public void UnregisterHandGestureCallback(EnhancedXRInputManager.HandGesture gesture)
        {
            if (m_enhancedInputManager != null)
            {
                m_enhancedInputManager.UnregisterGestureCallback(gesture);
            }
        }
        
        #region Mixed Reality Public API
        
        /// <summary>
        /// 启用/禁用MR功能
        /// </summary>
        public void SetMREnabled(bool enabled)
        {
            m_enableMRFeatures = enabled;
            
            if (!enabled && m_mrInitialized)
            {
                // 禁用MR时切换到VR模式
                if (m_mrPassthroughManager != null)
                {
                    m_mrPassthroughManager.SetPassthroughMode(PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled);
                }
            }
            
            Debug.Log($"[VRInteractionManager] MR features {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 获取当前MR模式
        /// </summary>
        public PongHub.MR.MRPassthroughManager.PassthroughMode GetCurrentMRMode()
        {
            return m_currentMRMode;
        }
        
        /// <summary>
        /// 设置MR模式
        /// </summary>
        public void SetMRMode(PongHub.MR.MRPassthroughManager.PassthroughMode mode)
        {
            if (!m_mrInitialized || !m_enableMRFeatures)
            {
                Debug.LogWarning("[VRInteractionManager] MR not initialized or disabled");
                return;
            }
            
            if (m_mrPassthroughManager != null)
            {
                m_mrPassthroughManager.SetPassthroughMode(mode);
            }
        }
        
        /// <summary>
        /// MR功能是否可用
        /// </summary>
        public bool IsMRAvailable()
        {
            return m_mrInitialized && m_enableMRFeatures && 
                   m_mrPassthroughManager != null && 
                   m_mrPassthroughManager.IsPassthroughAvailable;
        }
        
        /// <summary>
        /// 获取MR透视不透明度
        /// </summary>
        public float GetMROpacity()
        {
            if (m_mrPassthroughManager != null)
            {
                return m_mrPassthroughManager.CurrentOpacity;
            }
            return 0f;
        }
        
        /// <summary>
        /// 设置MR透视不透明度
        /// </summary>
        public void SetMROpacity(float opacity)
        {
            if (m_mrPassthroughManager != null)
            {
                m_mrPassthroughManager.SetPassthroughOpacity(opacity);
            }
        }
        
        /// <summary>
        /// 是否接近MR安全边界
        /// </summary>
        public bool IsNearMRBoundary()
        {
            if (m_mrSafetyBoundary != null)
            {
                return m_mrSafetyBoundary.IsNearBoundary;
            }
            return false;
        }
        
        /// <summary>
        /// 获取到MR边界的距离
        /// </summary>
        public float GetMRBoundaryDistance()
        {
            if (m_mrSafetyBoundary != null)
            {
                return m_mrSafetyBoundary.ClosestBoundaryDistance;
            }
            return float.MaxValue;
        }
        
        /// <summary>
        /// 强制刷新MR边界数据
        /// </summary>
        public void RefreshMRBoundary()
        {
            if (m_mrSafetyBoundary != null)
            {
                m_mrSafetyBoundary.RefreshBoundaryData();
            }
        }
        
        /// <summary>
        /// 启用/禁用MR安全功能
        /// </summary>
        public void SetMRSafetyEnabled(bool enabled)
        {
            m_isMRSafetyActive = enabled;
            
            if (m_mrSafetyBoundary != null)
            {
                m_mrSafetyBoundary.ShowBoundaryVisualization(enabled);
            }
            
            Debug.Log($"[VRInteractionManager] MR safety {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 添加虚拟对象到环境融合系统
        /// </summary>
        public void AddVirtualObjectToMR(GameObject obj)
        {
            if (m_environmentBlendingSystem != null)
            {
                m_environmentBlendingSystem.AddVirtualObject(obj);
            }
        }
        
        /// <summary>
        /// 从环境融合系统移除虚拟对象
        /// </summary>
        public void RemoveVirtualObjectFromMR(GameObject obj)
        {
            if (m_environmentBlendingSystem != null)
            {
                m_environmentBlendingSystem.RemoveVirtualObject(obj);
            }
        }
        
        /// <summary>
        /// 设置MR环境光照
        /// </summary>
        public void SetMREnvironmentLighting(float intensity, Color color)
        {
            if (m_environmentBlendingSystem != null)
            {
                m_environmentBlendingSystem.SetEnvironmentLighting(intensity, color);
            }
        }
        
        /// <summary>
        /// 获取MR诊断信息
        /// </summary>
        public string GetMRDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Mixed Reality Diagnostics ===");
            
            if (m_mrPassthroughManager != null)
            {
                diagnostics.AppendLine(m_mrPassthroughManager.GetDiagnostics());
            }
            
            if (m_environmentBlendingSystem != null)
            {
                diagnostics.AppendLine(m_environmentBlendingSystem.GetDiagnostics());
            }
            
            if (m_mrSafetyBoundary != null)
            {
                diagnostics.AppendLine(m_mrSafetyBoundary.GetDiagnostics());
            }
            
            return diagnostics.ToString();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // 停止所有视觉效果
            StopAllVisualEffects();
            
            // 取消事件监听
            UnsubscribeFromInteractorEvents();
            
            // 取消Hand Tracking事件监听
            UnsubscribeFromHandTrackingEvents();
            
            // 取消MR事件监听
            UnsubscribeFromMREvents();
            
            // 取消Avatar事件监听
            UnsubscribeFromAvatarEvents();
        }
        
        private void UnsubscribeFromHandTrackingEvents()
        {
            if (m_enhancedInputManager != null)
            {
                m_enhancedInputManager.OnGestureRecognized -= OnHandGestureRecognized;
                m_enhancedInputManager.OnInputModeChanged -= OnInputModeChanged;
            }
        }
        
        private void UnsubscribeFromMREvents()
        {
            if (m_mrPassthroughManager != null)
            {
                m_mrPassthroughManager.OnPassthroughModeChanged.RemoveListener(OnMRModeChanged);
                m_mrPassthroughManager.OnPassthroughAvailabilityChanged.RemoveListener(OnMRAvailabilityChanged);
            }
            
            if (m_mrSafetyBoundary != null)
            {
                m_mrSafetyBoundary.OnBoundaryWarningChanged -= OnMRBoundaryWarning;
                m_mrSafetyBoundary.OnEmergencyStop -= OnMREmergencyStop;
            }
        }
        
        private void UnsubscribeFromAvatarEvents()
        {
            if (m_vrAvatarManager != null)
            {
                m_vrAvatarManager.OnAvatarStateChanged.RemoveListener(OnAvatarStateChanged);
                m_vrAvatarManager.OnAvatarLoaded.RemoveListener(OnAvatarLoaded);
                m_vrAvatarManager.OnAvatarError.RemoveListener(OnAvatarError);
            }
            
            if (m_avatarMotionSync != null)
            {
                m_avatarMotionSync.OnTrackingModeChanged.RemoveListener(OnAvatarTrackingModeChanged);
                m_avatarMotionSync.OnHandTrackingStateChanged.RemoveListener(OnAvatarHandTrackingChanged);
                m_avatarMotionSync.OnMotionSyncInitialized.RemoveListener(OnAvatarMotionSyncReady);
            }
            
            if (m_avatarExpressionSystem != null)
            {
                m_avatarExpressionSystem.OnExpressionChanged.RemoveListener(OnAvatarExpressionChanged);
                m_avatarExpressionSystem.OnSpeechDetected.RemoveListener(OnAvatarSpeechDetected);
                m_avatarExpressionSystem.OnGazeDirectionChanged.RemoveListener(OnAvatarGazeChanged);
                m_avatarExpressionSystem.OnExpressionSystemInitialized.RemoveListener(OnAvatarExpressionSystemReady);
            }
            
            if (m_networkAvatarSync != null)
            {
                m_networkAvatarSync.OnAvatarConnected.RemoveListener(OnNetworkAvatarConnected);
                m_networkAvatarSync.OnAvatarDisconnected.RemoveListener(OnNetworkAvatarDisconnected);
                m_networkAvatarSync.OnNetworkQualityChanged.RemoveListener(OnAvatarNetworkQualityChanged);
            }
        }
        
        private void UnsubscribeFromInteractorEvents()
        {
            // 直接交互器事件取消订阅
            if (m_leftInteractor != null)
            {
                m_leftInteractor.hoverEntered.RemoveListener(OnLeftHoverEntered);
                m_leftInteractor.hoverExited.RemoveListener(OnLeftHoverExited);
                m_leftInteractor.selectEntered.RemoveListener(OnLeftSelectEntered);
                m_leftInteractor.selectExited.RemoveListener(OnLeftSelectExited);
            }

            if (m_rightInteractor != null)
            {
                m_rightInteractor.hoverEntered.RemoveListener(OnRightHoverEntered);
                m_rightInteractor.hoverExited.RemoveListener(OnRightHoverExited);
                m_rightInteractor.selectEntered.RemoveListener(OnRightSelectEntered);
                m_rightInteractor.selectExited.RemoveListener(OnRightSelectExited);
            }

            // 射线交互器事件取消订阅
            if (m_leftRayInteractor != null)
            {
                m_leftRayInteractor.hoverEntered.RemoveListener(OnLeftRayHoverEntered);
                m_leftRayInteractor.hoverExited.RemoveListener(OnLeftRayHoverExited);
                m_leftRayInteractor.selectEntered.RemoveListener(OnLeftRaySelectEntered);
                m_leftRayInteractor.selectExited.RemoveListener(OnLeftRaySelectExited);
            }

            if (m_rightRayInteractor != null)
            {
                m_rightRayInteractor.hoverEntered.RemoveListener(OnRightRayHoverEntered);
                m_rightRayInteractor.hoverExited.RemoveListener(OnRightRayHoverExited);
                m_rightRayInteractor.selectEntered.RemoveListener(OnRightRaySelectEntered);
                m_rightRayInteractor.selectExited.RemoveListener(OnRightRaySelectExited);
            }
        }
        
        #endregion
        
        #region Hand Tracking事件处理
        
        /// <summary>
        /// 手势识别事件处理
        /// </summary>
        private void OnHandGestureRecognized(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand, bool started)
        {
            if (!m_handTrackingInitialized || !m_enableHandTrackingInteraction)
                return;
                
            Debug.Log($"[VRInteractionManager] Hand gesture {(started ? "started" : "ended")}: {gesture} ({(isLeftHand ? "Left" : "Right")} hand)");
            
            if (started)
            {
                HandleHandGestureStart(gesture, isLeftHand);
            }
            else
            {
                HandleHandGestureEnd(gesture, isLeftHand);
            }
            
            m_lastHandGestures[isLeftHand] = started ? gesture : EnhancedXRInputManager.HandGesture.None;
        }
        
        /// <summary>
        /// 输入模式变化事件处理
        /// </summary>
        private void OnInputModeChanged(EnhancedXRInputManager.VRInputMode newMode, EnhancedXRInputManager.VRInputMode previousMode)
        {
            Debug.Log($"[VRInteractionManager] Input mode changed: {previousMode} -> {newMode}");
            
            // 根据输入模式调整交互行为
            switch (newMode)
            {
                case EnhancedXRInputManager.VRInputMode.Controller:
                    // 禁用Hand Tracking交互，启用控制器交互
                    SetHandTrackingInteractionEnabled(false);
                    SetControllerInteractionEnabled(true);
                    break;
                    
                case EnhancedXRInputManager.VRInputMode.HandTracking:
                    // 启用Hand Tracking交互，禁用控制器交互
                    SetHandTrackingInteractionEnabled(true);
                    SetControllerInteractionEnabled(false);
                    break;
                    
                case EnhancedXRInputManager.VRInputMode.Hybrid:
                    // 同时启用两种交互方式
                    SetHandTrackingInteractionEnabled(true);
                    SetControllerInteractionEnabled(true);
                    break;
            }
        }
        
        /// <summary>
        /// 处理手势开始
        /// </summary>
        private void HandleHandGestureStart(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand)
        {
            // 触发Avatar手势反应
            if (m_enableAvatarGestureReaction)
            {
                TriggerAvatarGestureReaction(gesture, isLeftHand, true);
            }
            
            switch (gesture)
            {
                case EnhancedXRInputManager.HandGesture.Pinch:
                    HandleHandPinchStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.Point:
                    HandleHandPointStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.Fist:
                    HandleHandFistStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.OpenHand:
                    HandleHandOpenStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.PaddleGrip:
                    HandlePaddleGripStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.MenuGesture:
                    HandleMenuGestureStart(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.ThumbsUp:
                    HandleThumbsUpStart(isLeftHand);
                    break;
            }
        }
        
        /// <summary>
        /// 处理手势结束
        /// </summary>
        private void HandleHandGestureEnd(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand)
        {
            switch (gesture)
            {
                case EnhancedXRInputManager.HandGesture.Pinch:
                    HandleHandPinchEnd(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.Point:
                    HandleHandPointEnd(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.Fist:
                    HandleHandFistEnd(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.PaddleGrip:
                    HandlePaddleGripEnd(isLeftHand);
                    break;
                    
                case EnhancedXRInputManager.HandGesture.MenuGesture:
                    HandleMenuGestureEnd(isLeftHand);
                    break;
            }
        }
        
        /// <summary>
        /// 处理手部捉取开始
        /// </summary>
        private void HandleHandPinchStart(bool isLeftHand)
        {
            // 模拟控制器抓取事件
            if (isLeftHand)
            {
                m_isLeftGrabbing = true;
                // 查找可抓取的对象
                var grabbableObject = FindNearbyGrabbableObject(isLeftHand);
                if (grabbableObject != null)
                {
                    m_leftGrabbedObject = grabbableObject;
                    TriggerInteractionFeedback(VRInteractionType.Grab, isLeftHand, grabbableObject);
                }
            }
            else
            {
                m_isRightGrabbing = true;
                var grabbableObject = FindNearbyGrabbableObject(isLeftHand);
                if (grabbableObject != null)
                {
                    m_rightGrabbedObject = grabbableObject;
                    TriggerInteractionFeedback(VRInteractionType.Grab, isLeftHand, grabbableObject);
                }
            }
        }
        
        /// <summary>
        /// 处理手部捉取结束
        /// </summary>
        private void HandleHandPinchEnd(bool isLeftHand)
        {
            if (isLeftHand && m_isLeftGrabbing)
            {
                m_isLeftGrabbing = false;
                if (m_leftGrabbedObject != null)
                {
                    TriggerInteractionFeedback(VRInteractionType.Release, isLeftHand, m_leftGrabbedObject);
                    m_leftGrabbedObject = null;
                }
            }
            else if (!isLeftHand && m_isRightGrabbing)
            {
                m_isRightGrabbing = false;
                if (m_rightGrabbedObject != null)
                {
                    TriggerInteractionFeedback(VRInteractionType.Release, isLeftHand, m_rightGrabbedObject);
                    m_rightGrabbedObject = null;
                }
            }
        }
        
        /// <summary>
        /// 处理手部指向开始
        /// </summary>
        private void HandleHandPointStart(bool isLeftHand)
        {
            // 启用射线交互
            var rayInteractor = isLeftHand ? m_leftRayInteractor : m_rightRayInteractor;
            if (rayInteractor != null)
            {
                rayInteractor.gameObject.SetActive(true);
                Debug.Log($"[VRInteractionManager] Hand ray activated for {(isLeftHand ? "left" : "right")} hand");
            }
        }
        
        /// <summary>
        /// 处理手部指向结束
        /// </summary>
        private void HandleHandPointEnd(bool isLeftHand)
        {
            // 禁用射线交互
            var rayInteractor = isLeftHand ? m_leftRayInteractor : m_rightRayInteractor;
            if (rayInteractor != null)
            {
                rayInteractor.gameObject.SetActive(false);
                Debug.Log($"[VRInteractionManager] Hand ray deactivated for {(isLeftHand ? "left" : "right")} hand");
            }
        }
        
        /// <summary>
        /// 处理手部握拳开始
        /// </summary>
        private void HandleHandFistStart(bool isLeftHand)
        {
            // 强力抓取模式
            TriggerInteractionFeedback(VRInteractionType.Grab, isLeftHand);
        }
        
        /// <summary>
        /// 处理手部握拳结束
        /// </summary>
        private void HandleHandFistEnd(bool isLeftHand)
        {
            TriggerInteractionFeedback(VRInteractionType.Release, isLeftHand);
        }
        
        /// <summary>
        /// 处理手部张开开始
        /// </summary>
        private void HandleHandOpenStart(bool isLeftHand)
        {
            // 释放所有抓取的对象
            if (isLeftHand && m_isLeftGrabbing)
            {
                HandleHandPinchEnd(isLeftHand);
            }
            else if (!isLeftHand && m_isRightGrabbing)
            {
                HandleHandPinchEnd(isLeftHand);
            }
        }
        
        /// <summary>
        /// 处理球拍握持开始（乒乓球专用）
        /// </summary>
        private void HandlePaddleGripStart(bool isLeftHand)
        {
            // 通知VRPaddle组件手部握持模式
            var paddleControllers = FindObjectsOfType<VRPaddle>();
            foreach (var paddle in paddleControllers)
            {
                if (paddle.IsLeftHand() == isLeftHand)
                {
                    // 这里可以添加特定的球拍握持逻辑
                    TriggerInteractionFeedback(VRInteractionType.Grab, isLeftHand, paddle.gameObject);
                    Debug.Log($"[VRInteractionManager] Paddle grip detected for {(isLeftHand ? "left" : "right")} hand");
                }
            }
        }
        
        /// <summary>
        /// 处理球拍握持结束
        /// </summary>
        private void HandlePaddleGripEnd(bool isLeftHand)
        {
            TriggerInteractionFeedback(VRInteractionType.Release, isLeftHand);
        }
        
        /// <summary>
        /// 处理菜单手势开始
        /// </summary>
        private void HandleMenuGestureStart(bool isLeftHand)
        {
            TriggerInteractionFeedback(VRInteractionType.MenuOpen, isLeftHand);
            Debug.Log($"[VRInteractionManager] Menu gesture detected for {(isLeftHand ? "left" : "right")} hand");
        }
        
        /// <summary>
        /// 处理菜单手势结束
        /// </summary>
        private void HandleMenuGestureEnd(bool isLeftHand)
        {
            TriggerInteractionFeedback(VRInteractionType.MenuClose, isLeftHand);
        }
        
        /// <summary>
        /// 处理点赞手势开始
        /// </summary>
        private void HandleThumbsUpStart(bool isLeftHand)
        {
            // 点赞手势可用于确认操作或显示满意度
            TriggerInteractionFeedback(VRInteractionType.ButtonPress, isLeftHand);
            Debug.Log($"[VRInteractionManager] Thumbs up gesture detected for {(isLeftHand ? "left" : "right")} hand");
        }
        
        /// <summary>
        /// 查找附近可抓取的对象
        /// </summary>
        private GameObject FindNearbyGrabbableObject(bool isLeftHand)
        {
            if (m_enhancedInputManager == null)
                return null;
                
            Vector3 handPosition = m_enhancedInputManager.GetHandPosition(isLeftHand);
            float grabRadius = 0.15f; // 15cm抓取半径
            
            var colliders = Physics.OverlapSphere(handPosition, grabRadius);
            foreach (var collider in colliders)
            {
                // 检查是否是可交互对象
                var interactable = collider.GetComponent<XRGrabInteractable>();
                if (interactable != null)
                {
                    return collider.gameObject;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 设置Hand Tracking交互启用状态
        /// </summary>
        private void SetHandTrackingInteractionEnabled(bool enabled)
        {
            // 这里可以添加更多的Hand Tracking交互控制逻辑
            Debug.Log($"[VRInteractionManager] Hand Tracking interaction {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 设置控制器交互启用状态
        /// </summary>
        private void SetControllerInteractionEnabled(bool enabled)
        {
            // 控制器交互器的启用/禁用
            if (m_leftInteractor != null)
                m_leftInteractor.enabled = enabled;
            if (m_rightInteractor != null)
                m_rightInteractor.enabled = enabled;
                
            Debug.Log($"[VRInteractionManager] Controller interaction {(enabled ? "enabled" : "disabled")}");
        }
        
        #endregion
        
        #region Mixed Reality事件处理
        
        /// <summary>
        /// MR模式变化事件处理
        /// </summary>
        private void OnMRModeChanged(PongHub.MR.MRPassthroughManager.PassthroughMode mode)
        {
            if (!m_mrInitialized)
                return;
                
            var previousMode = m_currentMRMode;
            m_currentMRMode = mode;
            
            Debug.Log($"[VRInteractionManager] MR mode changed: {previousMode} -> {mode}");
            
            // 根据MR模式调整交互行为
            switch (mode)
            {
                case PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled:
                    HandleMRModeDisabled();
                    break;
                    
                case PongHub.MR.MRPassthroughManager.PassthroughMode.FullPassthrough:
                    HandleMRModeFullPassthrough();
                    break;
                    
                case PongHub.MR.MRPassthroughManager.PassthroughMode.SelectivePassthrough:
                    HandleMRModeSelectivePassthrough();
                    break;
            }
        }
        
        /// <summary>
        /// MR可用性变化事件处理
        /// </summary>
        private void OnMRAvailabilityChanged(bool isAvailable)
        {
            Debug.Log($"[VRInteractionManager] MR availability changed: {isAvailable}");
            
            if (!isAvailable && m_currentMRMode != PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled)
            {
                // MR不可用时自动切换到VR模式
                if (m_mrPassthroughManager != null)
                {
                    m_mrPassthroughManager.SetPassthroughMode(PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled);
                }
            }
        }
        
        /// <summary>
        /// MR边界警告事件处理
        /// </summary>
        private void OnMRBoundaryWarning(bool isNearBoundary)
        {
            if (!m_mrInitialized || !m_isMRSafetyActive)
                return;
                
            Debug.Log($"[VRInteractionManager] MR boundary warning: {isNearBoundary}");
            
            if (isNearBoundary)
            {
                // 用户接近边界时的处理
                HandleMRBoundaryWarning();
            }
            else
            {
                // 用户远离边界时的处理
                HandleMRBoundaryCleared();
            }
        }
        
        /// <summary>
        /// MR紧急停止事件处理
        /// </summary>
        private void OnMREmergencyStop()
        {
            Debug.LogError("[VRInteractionManager] MR EMERGENCY STOP triggered!");
            
            // 立即禁用所有MR功能
            if (m_mrPassthroughManager != null)
            {
                m_mrPassthroughManager.SetPassthroughMode(PongHub.MR.MRPassthroughManager.PassthroughMode.Disabled);
            }
            
            // 触发强烈的触觉和音频反馈
            SendHapticImpulse(true, 1.0f, 0.5f);
            SendHapticImpulse(false, 1.0f, 0.5f);
            
            // 播放警告音效
            PlayInteractionAudio(VRInteractionType.ButtonPress, true, 1.5f); // 使用按钮音效作为警告
        }
        
        /// <summary>
        /// 处理MR模式禁用
        /// </summary>
        private void HandleMRModeDisabled()
        {
            // 纯VR模式，恢复标准VR交互
            Debug.Log("[VRInteractionManager] Entering pure VR mode");
        }
        
        /// <summary>
        /// 处理全透视MR模式
        /// </summary>
        private void HandleMRModeFullPassthrough()
        {
            // 全透视模式，调整交互参数以适应MR
            Debug.Log("[VRInteractionManager] Entering full passthrough MR mode");
            
            // 在MR模式下增强Hand Tracking（如果可用）
            if (m_handTrackingInitialized && m_autoSwitchMRMode)
            {
                if (m_enhancedInputManager != null && m_enhancedInputManager.IsHandTrackingAvailable)
                {
                    m_enhancedInputManager.SwitchToMode(EnhancedXRInputManager.VRInputMode.Hybrid);
                }
            }
        }
        
        /// <summary>
        /// 处理选择性透视MR模式
        /// </summary>
        private void HandleMRModeSelectivePassthrough()
        {
            // 选择性透视模式，平衡VR和MR交互
            Debug.Log("[VRInteractionManager] Entering selective passthrough MR mode");
        }
        
        /// <summary>
        /// 处理MR边界警告
        /// </summary>
        private void HandleMRBoundaryWarning()
        {
            // 触觉反馈
            SendHapticImpulse(true, 0.5f, 0.2f);
            SendHapticImpulse(false, 0.5f, 0.2f);
            
            // 音频警告
            PlayInteractionAudio(VRInteractionType.HoverExit, true, 0.8f);
        }
        
        /// <summary>
        /// 处理MR边界警告解除
        /// </summary>
        private void HandleMRBoundaryCleared()
        {
            // 轻微的确认反馈
            SendHapticImpulse(true, 0.2f, 0.1f);
            SendHapticImpulse(false, 0.2f, 0.1f);
        }
        
        #endregion
        
        #region Avatar System事件处理和API
        
        /// <summary>
        /// Avatar状态变化事件处理
        /// </summary>
        private void OnAvatarStateChanged(VRAvatarManager.AvatarState newState)
        {
            Debug.Log($"[VRInteractionManager] Avatar state changed to: {newState}");
            
            switch (newState)
            {
                case VRAvatarManager.AvatarState.Ready:
                    // Avatar准备就绪，可以开始同步
                    if (m_avatarMotionSync != null && m_enhancedInputManager != null)
                    {
                        // 同步当前输入模式到Avatar系统
                        var currentMode = m_enhancedInputManager.CurrentInputMode;
                        OnInputModeChanged(currentMode, currentMode);
                    }
                    break;
                    
                case VRAvatarManager.AvatarState.Error:
                    Debug.LogError("[VRInteractionManager] Avatar system error detected");
                    // 可以在这里添加错误恢复逻辑
                    break;
            }
        }
        
        /// <summary>
        /// Avatar加载完成事件处理
        /// </summary>
        private void OnAvatarLoaded()
        {
            Debug.Log("[VRInteractionManager] Avatar loaded successfully");
            TriggerAvatarEmotion("surprise", 0.5f); // 轻微的惊喜表情
        }
        
        /// <summary>
        /// Avatar错误事件处理
        /// </summary>
        private void OnAvatarError()
        {
            Debug.LogWarning("[VRInteractionManager] Avatar error occurred");
        }
        
        /// <summary>
        /// Avatar追踪模式变化事件处理
        /// </summary>
        private void OnAvatarTrackingModeChanged(AvatarMotionSync.TrackingMode newMode)
        {
            Debug.Log($"[VRInteractionManager] Avatar tracking mode changed to: {newMode}");
        }
        
        /// <summary>
        /// Avatar手部追踪状态变化事件处理
        /// </summary>
        private void OnAvatarHandTrackingChanged(bool isActive)
        {
            Debug.Log($"[VRInteractionManager] Avatar hand tracking: {(isActive ? "active" : "inactive")}");
        }
        
        /// <summary>
        /// Avatar动作同步就绪事件处理
        /// </summary>
        private void OnAvatarMotionSyncReady()
        {
            Debug.Log("[VRInteractionManager] Avatar motion sync ready");
        }
        
        /// <summary>
        /// Avatar表情变化事件处理
        /// </summary>
        private void OnAvatarExpressionChanged(AvatarExpressionSystem.BasicExpression expression)
        {
            Debug.Log($"[VRInteractionManager] Avatar expression changed to: {expression}");
        }
        
        /// <summary>
        /// Avatar语音检测事件处理
        /// </summary>
        private void OnAvatarSpeechDetected(float volume)
        {
            // 可以根据语音音量调整交互反馈
            if (volume > 0.5f)
            {
                // 大声说话时给予更强的触觉反馈
                SendHapticImpulse(true, volume * 0.3f, 0.1f);
                SendHapticImpulse(false, volume * 0.3f, 0.1f);
            }
        }
        
        /// <summary>
        /// Avatar注视方向变化事件处理
        /// </summary>
        private void OnAvatarGazeChanged(Vector3 gazeDirection)
        {
            // 可以根据注视方向调整UI或交互反馈
        }
        
        /// <summary>
        /// Avatar表情系统就绪事件处理
        /// </summary>
        private void OnAvatarExpressionSystemReady()
        {
            Debug.Log("[VRInteractionManager] Avatar expression system ready");
        }
        
        /// <summary>
        /// 网络Avatar连接事件处理
        /// </summary>
        private void OnNetworkAvatarConnected(ulong clientId)
        {
            Debug.Log($"[VRInteractionManager] Network avatar connected: {clientId}");
        }
        
        /// <summary>
        /// 网络Avatar断开事件处理
        /// </summary>
        private void OnNetworkAvatarDisconnected(ulong clientId)
        {
            Debug.Log($"[VRInteractionManager] Network avatar disconnected: {clientId}");
        }
        
        /// <summary>
        /// Avatar网络质量变化事件处理
        /// </summary>
        private void OnAvatarNetworkQualityChanged(float quality)
        {
            if (quality < 0.5f)
            {
                Debug.LogWarning($"[VRInteractionManager] Avatar network quality low: {quality:F2}");
            }
        }
        
        /// <summary>
        /// 触发Avatar手势反应
        /// </summary>
        private void TriggerAvatarGestureReaction(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand, bool started)
        {
            if (m_avatarExpressionSystem == null) return;
            
            if (started)
            {
                switch (gesture)
                {
                    case EnhancedXRInputManager.HandGesture.Pinch:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Focused, 0.8f, 1f);
                        break;
                        
                    case EnhancedXRInputManager.HandGesture.Point:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Focused, 0.6f, 0.5f);
                        break;
                        
                    case EnhancedXRInputManager.HandGesture.Fist:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Focused, 1f, 1.5f);
                        break;
                        
                    case EnhancedXRInputManager.HandGesture.ThumbsUp:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Happy, 1f, 2f);
                        break;
                        
                    case EnhancedXRInputManager.HandGesture.MenuGesture:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Confused, 0.5f, 1f);
                        break;
                        
                    case EnhancedXRInputManager.HandGesture.PaddleGrip:
                        m_avatarExpressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Excited, 0.8f, 2f);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 触发Avatar情绪反应
        /// </summary>
        private void TriggerAvatarEmotion(string emotionType, float intensity)
        {
            if (m_avatarExpressionSystem == null) return;
            
            // 防止频繁触发同一情绪
            if (m_avatarEmotionTimers.ContainsKey(emotionType) && m_avatarEmotionTimers[emotionType] > 0f)
                return;
                
            m_avatarExpressionSystem.TriggerEmotion(emotionType, intensity * m_avatarEmotionIntensity);
            m_avatarEmotionTimers[emotionType] = 2f; // 2秒冷却时间
        }
        
        /// <summary>
        /// 启用/禁用Avatar集成
        /// </summary>
        public void SetAvatarIntegrationEnabled(bool enabled)
        {
            m_enableAvatarIntegration = enabled;
            Debug.Log($"[VRInteractionManager] Avatar integration {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 获取Avatar系统是否已初始化
        /// </summary>
        public bool IsAvatarSystemInitialized()
        {
            return m_avatarSystemInitialized;
        }
        
        /// <summary>
        /// 获取VR Avatar管理器
        /// </summary>
        public VRAvatarManager GetVRAvatarManager()
        {
            return m_vrAvatarManager;
        }
        
        /// <summary>
        /// 获取Avatar动作同步组件
        /// </summary>
        public AvatarMotionSync GetAvatarMotionSync()
        {
            return m_avatarMotionSync;
        }
        
        /// <summary>
        /// 获取Avatar表情系统
        /// </summary>
        public AvatarExpressionSystem GetAvatarExpressionSystem()
        {
            return m_avatarExpressionSystem;
        }
        
        /// <summary>
        /// 获取Avatar网络同步组件
        /// </summary>
        public NetworkAvatarSync GetNetworkAvatarSync()
        {
            return m_networkAvatarSync;
        }
        
        /// <summary>
        /// 设置Avatar表情
        /// </summary>
        public void SetAvatarExpression(AvatarExpressionSystem.BasicExpression expression, float intensity = 1f, float duration = 0f)
        {
            if (m_avatarExpressionSystem != null)
            {
                m_avatarExpressionSystem.SetExpression(expression, intensity * m_avatarEmotionIntensity, duration);
            }
        }
        
        /// <summary>
        /// 设置Avatar注视目标
        /// </summary>
        public void SetAvatarGazeTarget(Transform target)
        {
            if (m_avatarExpressionSystem != null)
            {
                m_avatarExpressionSystem.SetGazeTarget(target);
            }
        }
        
        /// <summary>
        /// 触发Avatar游戏事件情绪
        /// </summary>
        public void TriggerAvatarGameEmotion(string eventType, float intensity = 1f)
        {
            TriggerAvatarEmotion(eventType, intensity);
        }
        
        /// <summary>
        /// 设置Avatar情绪响应强度
        /// </summary>
        public void SetAvatarEmotionIntensity(float intensity)
        {
            m_avatarEmotionIntensity = Mathf.Clamp(intensity, 0.1f, 2f);
        }
        
        /// <summary>
        /// 获取Avatar系统诊断信息
        /// </summary>
        public string GetAvatarSystemDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Avatar System Integration Diagnostics ===");
            diagnostics.AppendLine($"Avatar Integration Enabled: {m_enableAvatarIntegration}");
            diagnostics.AppendLine($"Avatar System Initialized: {m_avatarSystemInitialized}");
            diagnostics.AppendLine($"VR Avatar Manager: {(m_vrAvatarManager != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Avatar Motion Sync: {(m_avatarMotionSync != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Avatar Expression System: {(m_avatarExpressionSystem != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Network Avatar Sync: {(m_networkAvatarSync != null ? "OK" : "Missing")}");
            diagnostics.AppendLine($"Avatar Gesture Reaction: {m_enableAvatarGestureReaction}");
            diagnostics.AppendLine($"Avatar Emotion Intensity: {m_avatarEmotionIntensity:F2}");
            
            if (m_vrAvatarManager != null)
            {
                diagnostics.AppendLine($"Avatar State: {m_vrAvatarManager.CurrentState}");
                diagnostics.AppendLine($"Avatar Type: {m_vrAvatarManager.Type}");
                diagnostics.AppendLine($"Avatar Loaded: {m_vrAvatarManager.IsAvatarLoaded}");
            }
            
            if (m_avatarMotionSync != null)
            {
                diagnostics.AppendLine($"Motion Sync Initialized: {m_avatarMotionSync.IsInitialized}");
                diagnostics.AppendLine($"Motion Tracking Mode: {m_avatarMotionSync.CurrentTrackingMode}");
                diagnostics.AppendLine($"Hand Tracking Active: {m_avatarMotionSync.IsHandTrackingActive}");
            }
            
            if (m_avatarExpressionSystem != null)
            {
                diagnostics.AppendLine($"Expression System Initialized: {m_avatarExpressionSystem.IsInitialized}");
                diagnostics.AppendLine($"Current Expression: {m_avatarExpressionSystem.CurrentExpression}");
                diagnostics.AppendLine($"Speech Volume: {m_avatarExpressionSystem.SpeechVolume:F3}");
            }
            
            if (m_networkAvatarSync != null)
            {
                diagnostics.AppendLine($"Network Sync Enabled: {m_networkAvatarSync.IsNetworkSyncEnabled}");
                diagnostics.AppendLine($"Network Latency: {m_networkAvatarSync.NetworkLatency * 1000f:F1}ms");
                diagnostics.AppendLine($"Bandwidth Usage: {m_networkAvatarSync.BandwidthUsage:F2}KB/s");
                diagnostics.AppendLine($"Packets Per Second: {m_networkAvatarSync.PacketsPerSecond}");
            }
            
            return diagnostics.ToString();
        }
        
        #endregion
        
        #region 原有公共方法保持（向后兼容）
        
        // 获取控制器位置和旋转
        public Vector3 GetLeftControllerPosition()
        {
            return m_leftController != null ? m_leftController.transform.position : Vector3.zero;
        }

        public Quaternion GetLeftControllerRotation()
        {
            return m_leftController != null ? m_leftController.transform.rotation : Quaternion.identity;
        }

        public Vector3 GetRightControllerPosition()
        {
            return m_rightController != null ? m_rightController.transform.position : Vector3.zero;
        }

        public Quaternion GetRightControllerRotation()
        {
            return m_rightController != null ? m_rightController.transform.rotation : Quaternion.identity;
        }

        // 获取抓取状态
        public bool IsLeftGrabbing()
        {
            return m_isLeftGrabbing;
        }

        public bool IsRightGrabbing()
        {
            return m_isRightGrabbing;
        }

        // 获取抓取的对象
        public GameObject GetLeftGrabbedObject()
        {
            return m_leftGrabbedObject;
        }

        public GameObject GetRightGrabbedObject()
        {
            return m_rightGrabbedObject;
        }

        // 发送触觉反馈
        public void SendHapticImpulse(bool isLeft, float amplitude, float duration)
        {
            var controller = isLeft ? m_leftController : m_rightController;
            if (controller != null)
            {
                controller.SendHapticImpulse(amplitude, duration);
            }
        }

        public bool IsControllerGrabbing(bool isLeft)
        {
            var controller = isLeft ? m_leftController : m_rightController;
            var activateAction = isLeft ? m_leftActivateAction : m_rightActivateAction;

            if (controller != null && activateAction != null)
            {
                return activateAction.action.ReadValue<float>() > 0.5f;
            }
            return false;
        }

        public Vector3 CalculateThrowVelocity(Vector3 direction)
        {
            // 根据投掷角度和力度计算投掷速度
            float angleRad = m_throwAngle * Mathf.Deg2Rad;
            Vector3 throwDirection = Quaternion.Euler(-m_throwAngle, 0f, 0f) * direction;
            return throwDirection.normalized * m_throwForce;
        }

        // 属性
        public XRController LeftController => m_leftController;
        public XRController RightController => m_rightController;
        public XRBaseInteractor LeftInteractor => m_leftInteractor;
        public XRBaseInteractor RightInteractor => m_rightInteractor;
        public XRRayInteractor LeftRayInteractor => m_leftRayInteractor;
        public XRRayInteractor RightRayInteractor => m_rightRayInteractor;

        public bool CheckGrabInput(bool isLeft)
        {
            float gripValue = isLeft ?
                m_leftGripAction?.action.ReadValue<float>() ?? 0f :
                m_rightGripAction?.action.ReadValue<float>() ?? 0f;

            return gripValue > m_grabThreshold;
        }
        
        #endregion
    }
}