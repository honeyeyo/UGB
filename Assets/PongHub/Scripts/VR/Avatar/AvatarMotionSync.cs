using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using Oculus.Avatar2;
using PongHub.Core;
using PongHub.VR;

namespace PongHub.VR.Avatar
{
    /// <summary>
    /// Avatar动作同步组件
    /// 负责将VR追踪数据精确同步到Avatar骨骼系统，支持多种追踪模式和高级手部动作
    /// </summary>
    public class AvatarMotionSync : MonoBehaviour
    {
        /// <summary>
        /// 追踪模式枚举
        /// </summary>
        public enum TrackingMode
        {
            ControllerOnly,      // 仅控制器追踪
            HandTrackingOnly,    // 仅手部追踪
            Hybrid              // 混合模式（自动切换）
        }

        /// <summary>
        /// 同步质量级别
        /// </summary>
        public enum SyncQuality
        {
            Low,         // 低质量（性能优先）
            Medium,      // 中等质量
            High,        // 高质量（精度优先）
            Ultra        // 极高质量（实验性）
        }

        [Header("Motion Sync Settings")]
        [SerializeField]
        [Tooltip("启用动作同步")]
        private bool m_enableMotionSync = true;

        [SerializeField]
        [Tooltip("追踪模式")]
        private TrackingMode m_trackingMode = TrackingMode.Hybrid;

        [SerializeField]
        [Tooltip("同步质量")]
        private SyncQuality m_syncQuality = SyncQuality.High;

        [SerializeField]
        [Tooltip("更新频率")]
        [Range(30f, 120f)]
        private float m_updateFrequency = 90f;

        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("启用手指精确追踪")]
        private bool m_enableFingerTracking = true;

        [SerializeField]
        [Tooltip("手部置信度阈值")]
        [Range(0.1f, 1f)]
        private float m_handConfidenceThreshold = 0.7f;

        [SerializeField]
        [Tooltip("手指弯曲敏感度")]
        [Range(0.1f, 2f)]
        private float m_fingerFlexSensitivity = 1.2f;

        [SerializeField]
        [Tooltip("手部平滑因子")]
        [Range(0.1f, 1f)]
        private float m_handSmoothingFactor = 0.8f;

        [Header("Body Tracking")]
        [SerializeField]
        [Tooltip("启用身体追踪")]
        private bool m_enableBodyTracking = true;

        [SerializeField]
        [Tooltip("头部追踪权重")]
        [Range(0f, 2f)]
        private float m_headTrackingWeight = 1.2f;

        [SerializeField]
        [Tooltip("肩膀追踪权重")]
        [Range(0f, 2f)]
        private float m_shoulderTrackingWeight = 0.8f;

        [SerializeField]
        [Tooltip("身体平滑因子")]
        [Range(0.1f, 1f)]
        private float m_bodySmoothingFactor = 0.9f;

        [Header("Performance")]
        [SerializeField]
        [Tooltip("启用距离剔除")]
        private bool m_enableDistanceCulling = true;

        [SerializeField]
        [Tooltip("最大同步距离")]
        [Range(5f, 50f)]
        private float m_maxSyncDistance = 25f;

        [SerializeField]
        [Tooltip("启用LOD同步")]
        private bool m_enableLODSync = true;

        // 组件引用
        private VRAvatarManager m_avatarManager;
        private EnhancedXRInputManager m_inputManager;
        private OvrAvatarEntity m_avatarEntity;
        private Camera m_mainCamera;

        // 骨骼映射
        private Dictionary<CAPI.ovrAvatar2JointType, Transform> m_jointTransforms = new Dictionary<CAPI.ovrAvatar2JointType, Transform>();

        // 追踪数据
        private struct TrackingData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public bool isValid;
            public float confidence;
            public float timestamp;
        }

        private TrackingData m_headTracking;
        private TrackingData m_leftHandTracking;
        private TrackingData m_rightHandTracking;
        private TrackingData[] m_leftFingerTracking = new TrackingData[5];  // 5个手指
        private TrackingData[] m_rightFingerTracking = new TrackingData[5]; // 5个手指

        // 状态管理
        private bool m_isInitialized = false;
        private bool m_isHandTrackingActive = false;
        private float m_lastUpdateTime = 0f;
        private float m_updateInterval = 0f;
        private TrackingMode m_currentTrackingMode;

        // 性能监控
        private float m_avgSyncTime = 0f;
        private int m_syncFrameCount = 0;
        private const int PERF_SAMPLE_FRAMES = 60;

        // 事件
        public UnityEvent<TrackingMode> OnTrackingModeChanged = new UnityEvent<TrackingMode>();
        public UnityEvent<bool> OnHandTrackingStateChanged = new UnityEvent<bool>();
        public UnityEvent OnMotionSyncInitialized = new UnityEvent();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 当前追踪模式
        /// </summary>
        public TrackingMode CurrentTrackingMode => m_currentTrackingMode;

        /// <summary>
        /// 手部追踪是否激活
        /// </summary>
        public bool IsHandTrackingActive => m_isHandTrackingActive;

        /// <summary>
        /// 平均同步时间（毫秒）
        /// </summary>
        public float AverageSyncTime => m_avgSyncTime;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void Update()
        {
            if (m_isInitialized && m_enableMotionSync)
            {
                if (Time.time - m_lastUpdateTime >= m_updateInterval)
                {
                    UpdateMotionSync();
                    m_lastUpdateTime = Time.time;
                }
            }
        }

        private void OnDestroy()
        {
            CleanupMotionSync();
        }

        private void InitializeComponents()
        {
            // 获取Avatar管理器
            m_avatarManager = GetComponent<VRAvatarManager>();
            if (m_avatarManager == null)
            {
                m_avatarManager = FindObjectOfType<VRAvatarManager>();
            }

            // 获取输入管理器
            m_inputManager = FindObjectOfType<EnhancedXRInputManager>();

            // 获取相机
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                m_mainCamera = FindObjectOfType<Camera>();
            }

            // 计算更新间隔
            m_updateInterval = 1f / m_updateFrequency;
            m_currentTrackingMode = m_trackingMode;

            Debug.Log("[AvatarMotionSync] Components initialized");
        }

        private IEnumerator InitializeAsync()
        {
            // 等待Avatar管理器初始化
            while (m_avatarManager == null || !m_avatarManager.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 获取Avatar实体
            m_avatarEntity = m_avatarManager.AvatarEntity;
            if (m_avatarEntity == null)
            {
                Debug.LogError("[AvatarMotionSync] Avatar entity not found");
                yield break;
            }

            // 等待Avatar骨骼加载
            yield return StartCoroutine(WaitForAvatarSkeleton());

            // 构建骨骼映射
            BuildJointMapping();

            // 设置输入事件监听
            SetupInputEventListeners();

            m_isInitialized = true;
            OnMotionSyncInitialized?.Invoke();
            
            Debug.Log("[AvatarMotionSync] Motion sync initialized successfully");
        }

        private IEnumerator WaitForAvatarSkeleton()
        {
            float timeout = 30f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (m_avatarEntity != null && m_avatarEntity.SkeletonLoaded)
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning("[AvatarMotionSync] Avatar skeleton load timeout");
        }

        private void BuildJointMapping()
        {
            if (m_avatarEntity == null || !m_avatarEntity.SkeletonLoaded)
            {
                Debug.LogError("[AvatarMotionSync] Cannot build joint mapping - skeleton not loaded");
                return;
            }

            m_jointTransforms.Clear();

            // 添加主要关节映射
            var jointTypes = new CAPI.ovrAvatar2JointType[]
            {
                CAPI.ovrAvatar2JointType.Head,
                CAPI.ovrAvatar2JointType.LeftHandWrist,
                CAPI.ovrAvatar2JointType.RightHandWrist,
                CAPI.ovrAvatar2JointType.LeftShoulder,
                CAPI.ovrAvatar2JointType.RightShoulder,
                CAPI.ovrAvatar2JointType.LeftHandThumbTip,
                CAPI.ovrAvatar2JointType.LeftHandIndexTip,
                CAPI.ovrAvatar2JointType.LeftHandMiddleTip,
                CAPI.ovrAvatar2JointType.LeftHandRingTip,
                CAPI.ovrAvatar2JointType.LeftHandPinkyTip,
                CAPI.ovrAvatar2JointType.RightHandThumbTip,
                CAPI.ovrAvatar2JointType.RightHandIndexTip,
                CAPI.ovrAvatar2JointType.RightHandMiddleTip,
                CAPI.ovrAvatar2JointType.RightHandRingTip,
                CAPI.ovrAvatar2JointType.RightHandPinkyTip
            };

            foreach (var jointType in jointTypes)
            {
                var transform = m_avatarEntity.GetSkeletonTransformByType(jointType);
                if (transform != null)
                {
                    m_jointTransforms[jointType] = transform;
                }
            }

            Debug.Log($"[AvatarMotionSync] Built joint mapping with {m_jointTransforms.Count} joints");
        }

        private void SetupInputEventListeners()
        {
            if (m_inputManager != null)
            {
                m_inputManager.OnInputModeChanged += OnInputModeChanged;
                m_inputManager.OnGestureRecognized += OnGestureRecognized;
            }
        }

        private void UpdateMotionSync()
        {
            var startTime = Time.realtimeSinceStartup;

            // 检查距离剔除
            if (m_enableDistanceCulling && !IsWithinSyncDistance())
            {
                return;
            }

            // 更新追踪数据
            UpdateTrackingData();

            // 根据质量设置执行同步
            switch (m_syncQuality)
            {
                case SyncQuality.Low:
                    SyncBasicMotion();
                    break;
                case SyncQuality.Medium:
                    SyncBasicMotion();
                    SyncHandMotion();
                    break;
                case SyncQuality.High:
                    SyncBasicMotion();
                    SyncHandMotion();
                    SyncFingerMotion();
                    break;
                case SyncQuality.Ultra:
                    SyncBasicMotion();
                    SyncHandMotion();
                    SyncFingerMotion();
                    SyncAdvancedBodyMotion();
                    break;
            }

            // 更新性能统计
            UpdatePerformanceStats(Time.realtimeSinceStartup - startTime);
        }

        private bool IsWithinSyncDistance()
        {
            if (m_mainCamera == null || m_avatarEntity == null)
                return true;

            float distance = Vector3.Distance(m_mainCamera.transform.position, m_avatarEntity.transform.position);
            return distance <= m_maxSyncDistance;
        }

        private void UpdateTrackingData()
        {
            // 更新头部追踪
            if (m_inputManager != null)
            {
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    var headTransform = cameraRig.centerEyeAnchor;
                    m_headTracking.position = headTransform.position;
                    m_headTracking.rotation = headTransform.rotation;
                    m_headTracking.isValid = true;
                    m_headTracking.confidence = 1f;
                    m_headTracking.timestamp = Time.time;
                }
            }

            // 更新手部追踪
            UpdateHandTrackingData();
        }

        private void UpdateHandTrackingData()
        {
            if (m_inputManager == null) return;

            // 左手追踪
            bool leftHandValid = false;
            if (m_isHandTrackingActive)
            {
                float leftConfidence = m_inputManager.GetHandTrackingConfidence(true);
                if (leftConfidence >= m_handConfidenceThreshold)
                {
                    m_leftHandTracking.position = m_inputManager.GetHandPosition(true);
                    m_leftHandTracking.rotation = m_inputManager.GetHandRotation(true);
                    m_leftHandTracking.confidence = leftConfidence;
                    m_leftHandTracking.isValid = true;
                    leftHandValid = true;
                }
            }

            // 如果手部追踪不可用，使用控制器数据
            if (!leftHandValid && m_currentTrackingMode != TrackingMode.HandTrackingOnly)
            {
                m_leftHandTracking.position = m_inputManager.GetLeftControllerPosition();
                m_leftHandTracking.rotation = m_inputManager.GetLeftControllerRotation();
                m_leftHandTracking.confidence = 0.9f;
                m_leftHandTracking.isValid = true;
            }

            // 右手追踪（类似处理）
            bool rightHandValid = false;
            if (m_isHandTrackingActive)
            {
                float rightConfidence = m_inputManager.GetHandTrackingConfidence(false);
                if (rightConfidence >= m_handConfidenceThreshold)
                {
                    m_rightHandTracking.position = m_inputManager.GetHandPosition(false);
                    m_rightHandTracking.rotation = m_inputManager.GetHandRotation(false);
                    m_rightHandTracking.confidence = rightConfidence;
                    m_rightHandTracking.isValid = true;
                    rightHandValid = true;
                }
            }

            if (!rightHandValid && m_currentTrackingMode != TrackingMode.HandTrackingOnly)
            {
                m_rightHandTracking.position = m_inputManager.GetRightControllerPosition();
                m_rightHandTracking.rotation = m_inputManager.GetRightControllerRotation();
                m_rightHandTracking.confidence = 0.9f;
                m_rightHandTracking.isValid = true;
            }

            m_leftHandTracking.timestamp = Time.time;
            m_rightHandTracking.timestamp = Time.time;
        }

        private void SyncBasicMotion()
        {
            // 同步头部
            if (m_headTracking.isValid && m_jointTransforms.ContainsKey(CAPI.ovrAvatar2JointType.Head))
            {
                var headJoint = m_jointTransforms[CAPI.ovrAvatar2JointType.Head];
                var targetPos = m_headTracking.position * m_headTrackingWeight;
                var targetRot = m_headTracking.rotation;

                headJoint.position = Vector3.Lerp(headJoint.position, targetPos, 1f - m_bodySmoothingFactor);
                headJoint.rotation = Quaternion.Lerp(headJoint.rotation, targetRot, 1f - m_bodySmoothingFactor);
            }

            // 同步手部基础位置
            SyncHandPosition(CAPI.ovrAvatar2JointType.LeftHandWrist, m_leftHandTracking);
            SyncHandPosition(CAPI.ovrAvatar2JointType.RightHandWrist, m_rightHandTracking);
        }

        private void SyncHandMotion()
        {
            // 更精确的手部动作同步
            if (m_leftHandTracking.isValid)
            {
                SyncHandWithSmoothing(CAPI.ovrAvatar2JointType.LeftHandWrist, m_leftHandTracking, true);
            }

            if (m_rightHandTracking.isValid)
            {
                SyncHandWithSmoothing(CAPI.ovrAvatar2JointType.RightHandWrist, m_rightHandTracking, false);
            }
        }

        private void SyncFingerMotion()
        {
            if (!m_enableFingerTracking || !m_isHandTrackingActive) return;

            // 同步左手手指
            SyncFingers(true);
            
            // 同步右手手指
            SyncFingers(false);
        }

        private void SyncAdvancedBodyMotion()
        {
            // 高级身体动作同步（肩膀、躯干等）
            if (m_enableBodyTracking)
            {
                SyncShoulders();
            }
        }

        private void SyncHandPosition(CAPI.ovrAvatar2JointType jointType, TrackingData trackingData)
        {
            if (!trackingData.isValid || !m_jointTransforms.ContainsKey(jointType))
                return;

            var joint = m_jointTransforms[jointType];
            joint.position = Vector3.Lerp(joint.position, trackingData.position, 1f - m_handSmoothingFactor);
            joint.rotation = Quaternion.Lerp(joint.rotation, trackingData.rotation, 1f - m_handSmoothingFactor);
        }

        private void SyncHandWithSmoothing(CAPI.ovrAvatar2JointType jointType, TrackingData trackingData, bool isLeftHand)
        {
            if (!trackingData.isValid || !m_jointTransforms.ContainsKey(jointType))
                return;

            var joint = m_jointTransforms[jointType];
            
            // 基于置信度调整平滑因子
            float confidenceBasedSmoothing = Mathf.Lerp(m_handSmoothingFactor * 0.5f, m_handSmoothingFactor, trackingData.confidence);
            
            joint.position = Vector3.Lerp(joint.position, trackingData.position, 1f - confidenceBasedSmoothing);
            joint.rotation = Quaternion.Lerp(joint.rotation, trackingData.rotation, 1f - confidenceBasedSmoothing);
        }

        private void SyncFingers(bool isLeftHand)
        {
            var fingerJointTypes = isLeftHand ? 
                new CAPI.ovrAvatar2JointType[]
                {
                    CAPI.ovrAvatar2JointType.LeftHandThumbTip,
                    CAPI.ovrAvatar2JointType.LeftHandIndexTip,
                    CAPI.ovrAvatar2JointType.LeftHandMiddleTip,
                    CAPI.ovrAvatar2JointType.LeftHandRingTip,
                    CAPI.ovrAvatar2JointType.LeftHandPinkyTip
                } :
                new CAPI.ovrAvatar2JointType[]
                {
                    CAPI.ovrAvatar2JointType.RightHandThumbTip,
                    CAPI.ovrAvatar2JointType.RightHandIndexTip,
                    CAPI.ovrAvatar2JointType.RightHandMiddleTip,
                    CAPI.ovrAvatar2JointType.RightHandRingTip,
                    CAPI.ovrAvatar2JointType.RightHandPinkyTip
                };

            var fingerTrackingData = isLeftHand ? m_leftFingerTracking : m_rightFingerTracking;

            for (int i = 0; i < fingerJointTypes.Length; i++)
            {
                if (i >= fingerTrackingData.Length) continue;

                var jointType = fingerJointTypes[i];
                var trackingData = fingerTrackingData[i];

                if (trackingData.isValid && m_jointTransforms.ContainsKey(jointType))
                {
                    var joint = m_jointTransforms[jointType];
                    float fingerSmoothing = m_handSmoothingFactor * m_fingerFlexSensitivity;
                    
                    joint.position = Vector3.Lerp(joint.position, trackingData.position, 1f - fingerSmoothing);
                    joint.rotation = Quaternion.Lerp(joint.rotation, trackingData.rotation, 1f - fingerSmoothing);
                }
            }
        }

        private void SyncShoulders()
        {
            // 基于手部位置推算肩膀动作
            if (m_leftHandTracking.isValid && m_jointTransforms.ContainsKey(CAPI.ovrAvatar2JointType.LeftShoulder))
            {
                var leftShoulder = m_jointTransforms[CAPI.ovrAvatar2JointType.LeftShoulder];
                var targetRotation = CalculateShoulderRotation(m_leftHandTracking.position, m_headTracking.position, true);
                leftShoulder.rotation = Quaternion.Lerp(leftShoulder.rotation, targetRotation, 1f - m_bodySmoothingFactor * m_shoulderTrackingWeight);
            }

            if (m_rightHandTracking.isValid && m_jointTransforms.ContainsKey(CAPI.ovrAvatar2JointType.RightShoulder))
            {
                var rightShoulder = m_jointTransforms[CAPI.ovrAvatar2JointType.RightShoulder];
                var targetRotation = CalculateShoulderRotation(m_rightHandTracking.position, m_headTracking.position, false);
                rightShoulder.rotation = Quaternion.Lerp(rightShoulder.rotation, targetRotation, 1f - m_bodySmoothingFactor * m_shoulderTrackingWeight);
            }
        }

        private Quaternion CalculateShoulderRotation(Vector3 handPos, Vector3 headPos, bool isLeftShoulder)
        {
            Vector3 shoulderToHand = handPos - headPos;
            Vector3 forward = Vector3.forward;
            Vector3 up = Vector3.up;

            // 计算肩膀到手的方向
            Vector3 direction = shoulderToHand.normalized;
            
            // 基于是否为左肩调整方向
            if (isLeftShoulder)
            {
                direction.x = -Mathf.Abs(direction.x);
            }
            else
            {
                direction.x = Mathf.Abs(direction.x);
            }

            return Quaternion.LookRotation(direction, up);
        }

        private void UpdatePerformanceStats(float syncTime)
        {
            m_syncFrameCount++;
            m_avgSyncTime = (m_avgSyncTime * (m_syncFrameCount - 1) + syncTime * 1000f) / m_syncFrameCount;

            // 每隔一定帧数重置统计
            if (m_syncFrameCount >= PERF_SAMPLE_FRAMES)
            {
                m_syncFrameCount = 0;
            }
        }

        private void OnInputModeChanged(EnhancedXRInputManager.VRInputMode newMode, EnhancedXRInputManager.VRInputMode previousMode)
        {
            bool wasHandTrackingActive = m_isHandTrackingActive;
            m_isHandTrackingActive = newMode == EnhancedXRInputManager.VRInputMode.HandTracking || 
                                   newMode == EnhancedXRInputManager.VRInputMode.Hybrid;

            // 更新追踪模式
            if (m_trackingMode == TrackingMode.Hybrid)
            {
                if (m_isHandTrackingActive)
                {
                    m_currentTrackingMode = TrackingMode.HandTrackingOnly;
                }
                else
                {
                    m_currentTrackingMode = TrackingMode.ControllerOnly;
                }
            }

            if (wasHandTrackingActive != m_isHandTrackingActive)
            {
                OnHandTrackingStateChanged?.Invoke(m_isHandTrackingActive);
                OnTrackingModeChanged?.Invoke(m_currentTrackingMode);
            }

            Debug.Log($"[AvatarMotionSync] Tracking mode changed to: {m_currentTrackingMode}, Hand tracking: {m_isHandTrackingActive}");
        }

        private void OnGestureRecognized(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand, bool started)
        {
            // 根据手势调整Avatar动作
            // 这里可以添加特定手势的Avatar响应
        }

        private void CleanupMotionSync()
        {
            if (m_inputManager != null)
            {
                m_inputManager.OnInputModeChanged -= OnInputModeChanged;
                m_inputManager.OnGestureRecognized -= OnGestureRecognized;
            }

            m_jointTransforms.Clear();
            Debug.Log("[AvatarMotionSync] Motion sync cleanup completed");
        }

        /// <summary>
        /// 设置同步质量
        /// </summary>
        public void SetSyncQuality(SyncQuality quality)
        {
            m_syncQuality = quality;
            Debug.Log($"[AvatarMotionSync] Sync quality set to: {quality}");
        }

        /// <summary>
        /// 设置追踪模式
        /// </summary>
        public void SetTrackingMode(TrackingMode mode)
        {
            m_trackingMode = mode;
            m_currentTrackingMode = mode;
            OnTrackingModeChanged?.Invoke(m_currentTrackingMode);
            Debug.Log($"[AvatarMotionSync] Tracking mode set to: {mode}");
        }

        /// <summary>
        /// 获取动作同步诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Avatar Motion Sync Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"Motion Sync Enabled: {m_enableMotionSync}");
            diagnostics.AppendLine($"Current Tracking Mode: {m_currentTrackingMode}");
            diagnostics.AppendLine($"Hand Tracking Active: {m_isHandTrackingActive}");
            diagnostics.AppendLine($"Sync Quality: {m_syncQuality}");
            diagnostics.AppendLine($"Update Frequency: {m_updateFrequency:F1}Hz");
            diagnostics.AppendLine($"Joint Mapping Count: {m_jointTransforms.Count}");
            diagnostics.AppendLine($"Average Sync Time: {m_avgSyncTime:F2}ms");
            diagnostics.AppendLine($"Head Tracking Valid: {m_headTracking.isValid}");
            diagnostics.AppendLine($"Left Hand Confidence: {m_leftHandTracking.confidence:F2}");
            diagnostics.AppendLine($"Right Hand Confidence: {m_rightHandTracking.confidence:F2}");
            diagnostics.AppendLine($"Finger Tracking Enabled: {m_enableFingerTracking}");
            diagnostics.AppendLine($"Body Tracking Enabled: {m_enableBodyTracking}");
            diagnostics.AppendLine($"Distance Culling: {m_enableDistanceCulling}");
            diagnostics.AppendLine($"Max Sync Distance: {m_maxSyncDistance:F1}m");
            
            return diagnostics.ToString();
        }
    }
}