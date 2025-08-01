using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using Oculus.Avatar2;
using PongHub.Core;
using PongHub.VR;
using Unity.Netcode;
using Meta.Multiplayer.Avatar;

namespace PongHub.VR.Avatar
{
    /// <summary>
    /// VR Avatar管理器
    /// 基于Meta Avatar SDK增强Avatar系统，集成Hand Tracking、动作同步、表情控制等功能
    /// </summary>
    public class VRAvatarManager : MonoBehaviour
    {
        /// <summary>
        /// Avatar类型枚举
        /// </summary>
        public enum AvatarType
        {
            LocalPlayer,        // 本地玩家Avatar
            RemotePlayer,       // 远程玩家Avatar
            Spectator          // 观众Avatar
        }

        /// <summary>
        /// Avatar状态枚举
        /// </summary>
        public enum AvatarState
        {
            Uninitialized,      // 未初始化
            Loading,            // 加载中
            Ready,              // 就绪
            Error               // 错误
        }

        [Header("Avatar Settings")]
        [SerializeField]
        [Tooltip("是否启用Avatar功能")]
        private bool m_enableAvatar = true;

        [SerializeField]
        [Tooltip("Avatar类型")]
        private AvatarType m_avatarType = AvatarType.LocalPlayer;

        [SerializeField]
        [Tooltip("Avatar ID (用户ID或预设ID)")]
        private string m_avatarId = "";

        [SerializeField]
        [Tooltip("是否在镜子中显示")]
        private bool m_showInMirror = false;

        [SerializeField]
        [Tooltip("Avatar预制件")]
        private GameObject m_avatarPrefab;

        [Header("Hand Tracking Integration")]
        [SerializeField]
        [Tooltip("启用Hand Tracking集成")]
        private bool m_enableHandTracking = true;

        [SerializeField]
        [Tooltip("手部追踪精度")]
        [Range(0.1f, 1f)]
        private float m_handTrackingAccuracy = 0.8f;

        [SerializeField]
        [Tooltip("手部动作平滑度")]
        [Range(0.1f, 1f)]
        private float m_handSmoothingFactor = 0.7f;

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("启用身体动画")]
        private bool m_enableBodyAnimation = true;

        [SerializeField]
        [Tooltip("启用面部表情")]
        private bool m_enableFacialExpression = true;

        [SerializeField]
        [Tooltip("启用口型同步")]
        private bool m_enableLipSync = true;

        [SerializeField]
        [Tooltip("动画更新频率")]
        [Range(30f, 90f)]
        private float m_animationUpdateRate = 60f;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("启用LOD优化")]
        private bool m_enableLOD = true;

        [SerializeField]
        [Tooltip("最大渲染距离")]
        [Range(5f, 50f)]
        private float m_maxRenderDistance = 20f;

        [SerializeField]
        [Tooltip("启用遮挡剔除")]
        private bool m_enableOcclusionCulling = true;

        // 组件引用
        private VRInteractionManager m_vrInteractionManager;
        private EnhancedXRInputManager m_inputManager;
        private Camera m_mainCamera;
        private Transform m_headTransform;

        // Avatar组件
        private OvrAvatarEntity m_avatarEntity;
        private PlayerAvatarEntity m_playerAvatarEntity;
        private AvatarEntity m_multiplayerAvatarEntity;
        private OvrAvatarLipSyncContext m_lipSyncContext;
        private Transform m_avatarRoot;

        // 状态管理
        private AvatarState m_currentState = AvatarState.Uninitialized;
        private bool m_isInitialized = false;
        private bool m_isAvatarLoaded = false;
        private float m_lastUpdateTime = 0f;
        private float m_updateInterval = 0f;

        // 动作数据
        private Dictionary<CAPI.ovrAvatar2JointType, Transform> m_jointMap = new Dictionary<CAPI.ovrAvatar2JointType, Transform>();
        private Vector3 m_lastHeadPosition = Vector3.zero;
        private Quaternion m_lastHeadRotation = Quaternion.identity;
        private Vector3[] m_lastHandPositions = new Vector3[2];
        private Quaternion[] m_lastHandRotations = new Quaternion[2];

        // Hand Tracking数据
        private float[] m_fingerFlexions = new float[10]; // 每只手5个手指
        private bool m_isHandTrackingActive = false;

        // 事件
        public UnityEvent<AvatarState> OnAvatarStateChanged = new UnityEvent<AvatarState>();
        public UnityEvent OnAvatarLoaded = new UnityEvent();
        public UnityEvent OnAvatarError = new UnityEvent();

        /// <summary>
        /// 当前Avatar状态
        /// </summary>
        public AvatarState CurrentState => m_currentState;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// Avatar是否已加载
        /// </summary>
        public bool IsAvatarLoaded => m_isAvatarLoaded;

        /// <summary>
        /// Avatar类型
        /// </summary>
        public AvatarType Type => m_avatarType;

        /// <summary>
        /// Avatar实体引用
        /// </summary>
        public OvrAvatarEntity AvatarEntity => m_avatarEntity;

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
            if (m_isInitialized && m_isAvatarLoaded)
            {
                UpdateAvatarMotion();
                UpdatePerformanceOptimization();
            }
        }

        private void OnDestroy()
        {
            CleanupAvatar();
        }

        private void InitializeComponents()
        {
            // 获取必需组件
            m_vrInteractionManager = FindObjectOfType<VRInteractionManager>();
            m_inputManager = FindObjectOfType<EnhancedXRInputManager>();
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
                m_mainCamera = FindObjectOfType<Camera>();

            // 获取头部变换
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                m_headTransform = cameraRig.centerEyeAnchor;
            }
            else if (m_mainCamera != null)
            {
                m_headTransform = m_mainCamera.transform;
            }

            // 计算更新间隔
            m_updateInterval = 1f / m_animationUpdateRate;

            Debug.Log("[VRAvatarManager] Components initialized");
        }

        private IEnumerator InitializeAsync()
        {
            if (!m_enableAvatar)
            {
                Debug.Log("[VRAvatarManager] Avatar disabled");
                return null;
            }

            SetAvatarState(AvatarState.Loading);

            // 等待Avatar系统初始化
            yield return new WaitForSeconds(0.5f);

            // 检查Avatar Manager是否可用
            while (OvrAvatarManager.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 根据Avatar类型加载不同的Avatar
            yield return StartCoroutine(LoadAvatarBasedOnType());

            if (m_avatarEntity != null)
            {
                SetupAvatarComponents();
                SetupHandTrackingIntegration();
                SetupPerformanceOptimization();
                
                m_isInitialized = true;
                SetAvatarState(AvatarState.Ready);
                
                Debug.Log("[VRAvatarManager] Avatar initialization complete");
            }
            else
            {
                SetAvatarState(AvatarState.Error);
                Debug.LogError("[VRAvatarManager] Failed to load avatar");
            }
        }

        private IEnumerator LoadAvatarBasedOnType()
        {
            switch (m_avatarType)
            {
                case AvatarType.LocalPlayer:
                    yield return StartCoroutine(LoadLocalPlayerAvatar());
                    break;
                    
                case AvatarType.RemotePlayer:
                    yield return StartCoroutine(LoadRemotePlayerAvatar());
                    break;
                    
                case AvatarType.Spectator:
                    yield return StartCoroutine(LoadSpectatorAvatar());
                    break;
            }
        }

        private IEnumerator LoadLocalPlayerAvatar()
        {
            // 检查是否已经有现有的PlayerAvatarEntity
            m_playerAvatarEntity = FindObjectOfType<PlayerAvatarEntity>();
            
            if (m_playerAvatarEntity != null)
            {
                // 使用现有的PlayerAvatarEntity
                m_avatarEntity = m_playerAvatarEntity;
                Debug.Log("[VRAvatarManager] Using existing PlayerAvatarEntity");
            }
            else
            {
                // 创建新的Avatar
                if (m_avatarPrefab != null)
                {
                    var avatarObject = Instantiate(m_avatarPrefab, transform);
                    m_avatarEntity = avatarObject.GetComponent<OvrAvatarEntity>();
                    m_playerAvatarEntity = avatarObject.GetComponent<PlayerAvatarEntity>();
                }
                else
                {
                    // 创建基础Avatar
                    var avatarObject = new GameObject("LocalPlayerAvatar");
                    avatarObject.transform.SetParent(transform);
                    m_avatarEntity = avatarObject.AddComponent<OvrAvatarEntity>();
                }
                
                Debug.Log("[VRAvatarManager] Created new local player avatar");
            }

            // 等待Avatar加载完成
            if (m_avatarEntity != null)
            {
                yield return StartCoroutine(WaitForAvatarLoad());
            }
        }

        private IEnumerator LoadRemotePlayerAvatar()
        {
            // 检查是否有多人Avatar组件
            m_multiplayerAvatarEntity = GetComponent<AvatarEntity>();
            
            if (m_multiplayerAvatarEntity != null)
            {
                m_avatarEntity = m_multiplayerAvatarEntity;
                Debug.Log("[VRAvatarManager] Using existing AvatarEntity for remote player");
            }
            else
            {
                // 创建远程玩家Avatar
                if (m_avatarPrefab != null)
                {
                    var avatarObject = Instantiate(m_avatarPrefab, transform);
                    m_avatarEntity = avatarObject.GetComponent<OvrAvatarEntity>();
                }
                else
                {
                    var avatarObject = new GameObject("RemotePlayerAvatar");
                    avatarObject.transform.SetParent(transform);
                    m_avatarEntity = avatarObject.AddComponent<OvrAvatarEntity>();
                }
                
                Debug.Log("[VRAvatarManager] Created new remote player avatar");
            }

            yield return StartCoroutine(WaitForAvatarLoad());
        }

        private IEnumerator LoadSpectatorAvatar()
        {
            // 创建观众Avatar（简化版本）
            var avatarObject = new GameObject("SpectatorAvatar");
            avatarObject.transform.SetParent(transform);
            m_avatarEntity = avatarObject.AddComponent<OvrAvatarEntity>();
            
            Debug.Log("[VRAvatarManager] Created spectator avatar");
            
            yield return StartCoroutine(WaitForAvatarLoad());
        }

        private IEnumerator WaitForAvatarLoad()
        {
            float timeout = 30f; // 30秒超时
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                if (m_avatarEntity != null && m_avatarEntity.IsCreated)
                {
                    // 等待骨骼加载
                    if (m_playerAvatarEntity != null && m_playerAvatarEntity.IsSkeletonReady)
                    {
                        break;
                    }
                    else if (m_avatarEntity.SkeletonLoaded)
                    {
                        break;
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("[VRAvatarManager] Avatar load timeout");
            }
            else
            {
                m_isAvatarLoaded = true;
                OnAvatarLoaded?.Invoke();
                Debug.Log("[VRAvatarManager] Avatar loaded successfully");
            }
        }

        private void SetupAvatarComponents()
        {
            if (m_avatarEntity == null) return;

            // 设置Avatar根节点
            m_avatarRoot = m_avatarEntity.transform;

            // 设置口型同步
            if (m_enableLipSync)
            {
                m_lipSyncContext = m_avatarEntity.GetComponent<OvrAvatarLipSyncContext>();
                if (m_lipSyncContext == null)
                {
                    m_lipSyncContext = m_avatarEntity.gameObject.AddComponent<OvrAvatarLipSyncContext>();
                }
            }

            // 构建关节映射
            BuildJointMap();

            Debug.Log("[VRAvatarManager] Avatar components setup complete");
        }

        private void BuildJointMap()
        {
            if (m_avatarEntity == null || !m_avatarEntity.SkeletonLoaded) return;

            m_jointMap.Clear();

            // 获取主要关节
            var skeleton = m_avatarEntity.GetComponent<OvrAvatarTrackingSkeleton>();
            if (skeleton != null)
            {
                // 添加头部关节
                var headJoint = skeleton.GetJoint(CAPI.ovrAvatar2JointType.Head);
                if (headJoint != null)
                {
                    m_jointMap[CAPI.ovrAvatar2JointType.Head] = headJoint;
                }

                // 添加手部关节
                var leftHandJoint = skeleton.GetJoint(CAPI.ovrAvatar2JointType.LeftHandWrist);
                if (leftHandJoint != null)
                {
                    m_jointMap[CAPI.ovrAvatar2JointType.LeftHandWrist] = leftHandJoint;
                }

                var rightHandJoint = skeleton.GetJoint(CAPI.ovrAvatar2JointType.RightHandWrist);
                if (rightHandJoint != null)
                {
                    m_jointMap[CAPI.ovrAvatar2JointType.RightHandWrist] = rightHandJoint;
                }
            }

            Debug.Log($"[VRAvatarManager] Built joint map with {m_jointMap.Count} joints");
        }

        private void SetupHandTrackingIntegration()
        {
            if (!m_enableHandTracking || m_inputManager == null) return;

            // 注册Hand Tracking事件
            m_inputManager.OnGestureRecognized += OnHandGestureRecognized;
            m_inputManager.OnInputModeChanged += OnInputModeChanged;

            Debug.Log("[VRAvatarManager] Hand tracking integration setup complete");
        }

        private void SetupPerformanceOptimization()
        {
            if (m_avatarEntity == null) return;

            // 设置LOD
            if (m_enableLOD)
            {
                var lodGroup = m_avatarEntity.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = m_avatarEntity.gameObject.AddComponent<LODGroup>();
                    SetupAvatarLOD(lodGroup);
                }
            }

            // 设置遮挡剔除
            if (m_enableOcclusionCulling)
            {
                var renderers = m_avatarEntity.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.allowOcclusionWhenDynamic = true;
                }
            }

            Debug.Log("[VRAvatarManager] Performance optimization setup complete");
        }

        private void SetupAvatarLOD(LODGroup lodGroup)
        {
            var renderers = m_avatarEntity.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var lods = new LOD[3];
            
            // LOD 0 - 高质量 (0-30%)
            lods[0] = new LOD(0.3f, renderers);
            
            // LOD 1 - 中等质量 (30-60%)
            lods[1] = new LOD(0.15f, renderers);
            
            // LOD 2 - 低质量 (60-100%)
            lods[2] = new LOD(0.01f, renderers);
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        private void UpdateAvatarMotion()
        {
            if (Time.time - m_lastUpdateTime < m_updateInterval) return;

            // 仅为本地Avatar更新动作
            if (m_avatarType == AvatarType.LocalPlayer)
            {
                UpdateLocalAvatarMotion();
            }

            m_lastUpdateTime = Time.time;
        }

        private void UpdateLocalAvatarMotion()
        {
            if (m_headTransform == null || m_avatarEntity == null) return;

            // 更新头部位置和旋转
            UpdateHeadPose();

            // 更新手部位置和旋转
            if (m_vrInteractionManager != null)
            {
                UpdateHandPoses();
            }

            // 更新Hand Tracking数据
            if (m_isHandTrackingActive && m_inputManager != null)
            {
                UpdateHandTrackingData();
            }
        }

        private void UpdateHeadPose()
        {
            Vector3 currentHeadPos = m_headTransform.position;
            Quaternion currentHeadRot = m_headTransform.rotation;

            // 平滑处理
            if (m_lastHeadPosition != Vector3.zero)
            {
                currentHeadPos = Vector3.Lerp(m_lastHeadPosition, currentHeadPos, 1f - m_handSmoothingFactor);
                currentHeadRot = Quaternion.Lerp(m_lastHeadRotation, currentHeadRot, 1f - m_handSmoothingFactor);
            }

            // 更新Avatar头部
            if (m_jointMap.ContainsKey(CAPI.ovrAvatar2JointType.Head))
            {
                var headJoint = m_jointMap[CAPI.ovrAvatar2JointType.Head];
                headJoint.position = currentHeadPos;
                headJoint.rotation = currentHeadRot;
            }

            m_lastHeadPosition = currentHeadPos;
            m_lastHeadRotation = currentHeadRot;
        }

        private void UpdateHandPoses()
        {
            // 左手
            Vector3 leftHandPos = m_vrInteractionManager.GetLeftControllerPosition();
            Quaternion leftHandRot = m_vrInteractionManager.GetLeftControllerRotation();
            
            // 右手
            Vector3 rightHandPos = m_vrInteractionManager.GetRightControllerPosition();
            Quaternion rightHandRot = m_vrInteractionManager.GetRightControllerRotation();

            // 平滑处理
            if (m_lastHandPositions[0] != Vector3.zero)
            {
                leftHandPos = Vector3.Lerp(m_lastHandPositions[0], leftHandPos, 1f - m_handSmoothingFactor);
                leftHandRot = Quaternion.Lerp(m_lastHandRotations[0], leftHandRot, 1f - m_handSmoothingFactor);
            }

            if (m_lastHandPositions[1] != Vector3.zero)
            {
                rightHandPos = Vector3.Lerp(m_lastHandPositions[1], rightHandPos, 1f - m_handSmoothingFactor);
                rightHandRot = Quaternion.Lerp(m_lastHandRotations[1], rightHandRot, 1f - m_handSmoothingFactor);
            }

            // 更新Avatar手部
            if (m_jointMap.ContainsKey(CAPI.ovrAvatar2JointType.LeftHandWrist))
            {
                var leftHandJoint = m_jointMap[CAPI.ovrAvatar2JointType.LeftHandWrist];
                leftHandJoint.position = leftHandPos;
                leftHandJoint.rotation = leftHandRot;
            }

            if (m_jointMap.ContainsKey(CAPI.ovrAvatar2JointType.RightHandWrist))
            {
                var rightHandJoint = m_jointMap[CAPI.ovrAvatar2JointType.RightHandWrist];
                rightHandJoint.position = rightHandPos;
                rightHandJoint.rotation = rightHandRot;
            }

            m_lastHandPositions[0] = leftHandPos;
            m_lastHandPositions[1] = rightHandPos;
            m_lastHandRotations[0] = leftHandRot;
            m_lastHandRotations[1] = rightHandRot;
        }

        private void UpdateHandTrackingData()
        {
            if (m_inputManager == null) return;

            // 获取Hand Tracking数据并更新Avatar手部精确动作
            // 这里可以添加更精确的手指动作控制
            for (int hand = 0; hand < 2; hand++)
            {
                bool isLeftHand = hand == 0;
                if (m_inputManager.GetHandTrackingConfidence(isLeftHand) > m_handTrackingAccuracy)
                {
                    var handPos = m_inputManager.GetHandPosition(isLeftHand);
                    var handRot = m_inputManager.GetHandRotation(isLeftHand);
                    
                    // 更新对应的Avatar手部
                    var jointType = isLeftHand ? CAPI.ovrAvatar2JointType.LeftHandWrist : CAPI.ovrAvatar2JointType.RightHandWrist;
                    if (m_jointMap.ContainsKey(jointType))
                    {
                        var handJoint = m_jointMap[jointType];
                        handJoint.position = handPos;
                        handJoint.rotation = handRot;
                    }
                }
            }
        }

        private void UpdatePerformanceOptimization()
        {
            if (m_mainCamera == null || m_avatarEntity == null) return;

            // 距离剔除
            float distance = Vector3.Distance(m_mainCamera.transform.position, m_avatarRoot.position);
            bool shouldRender = distance <= m_maxRenderDistance;

            var renderers = m_avatarEntity.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.enabled != shouldRender)
                {
                    renderer.enabled = shouldRender;
                }
            }
        }

        private void OnHandGestureRecognized(EnhancedXRInputManager.HandGesture gesture, bool isLeftHand, bool started)
        {
            // 根据手势更新Avatar手部姿态
            // 这里可以添加特定手势的Avatar表现
        }

        private void OnInputModeChanged(EnhancedXRInputManager.VRInputMode newMode, EnhancedXRInputManager.VRInputMode previousMode)
        {
            m_isHandTrackingActive = newMode == EnhancedXRInputManager.VRInputMode.HandTracking || 
                                   newMode == EnhancedXRInputManager.VRInputMode.Hybrid;
            
            Debug.Log($"[VRAvatarManager] Hand tracking active: {m_isHandTrackingActive}");
        }

        private void SetAvatarState(AvatarState newState)
        {
            if (m_currentState != newState)
            {
                var previousState = m_currentState;
                m_currentState = newState;
                OnAvatarStateChanged?.Invoke(newState);
                
                if (newState == AvatarState.Error)
                {
                    OnAvatarError?.Invoke();
                }
                
                Debug.Log($"[VRAvatarManager] Avatar state changed: {previousState} -> {newState}");
            }
        }

        /// <summary>
        /// 加载指定ID的Avatar
        /// </summary>
        public void LoadAvatar(string avatarId)
        {
            if (!m_isInitialized)
            {
                Debug.LogWarning("[VRAvatarManager] Cannot load avatar - not initialized");
                return;
            }

            m_avatarId = avatarId;
            
            if (m_avatarEntity != null)
            {
                // 重新加载Avatar
                StartCoroutine(ReloadAvatar());
            }
        }

        private IEnumerator ReloadAvatar()
        {
            SetAvatarState(AvatarState.Loading);
            m_isAvatarLoaded = false;
            
            // 清理现有Avatar
            if (m_avatarEntity != null && m_avatarEntity.IsCreated)
            {
                m_avatarEntity.TeardownEntity();
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 重新加载
            yield return StartCoroutine(LoadAvatarBasedOnType());
            
            if (m_isAvatarLoaded)
            {
                SetAvatarState(AvatarState.Ready);
            }
            else
            {
                SetAvatarState(AvatarState.Error);
            }
        }

        /// <summary>
        /// 设置Avatar可见性
        /// </summary>
        public void SetAvatarVisible(bool visible)
        {
            if (m_avatarEntity == null) return;

            var renderers = m_avatarEntity.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        /// <summary>
        /// 获取Avatar诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== VR Avatar Manager Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"Avatar Loaded: {m_isAvatarLoaded}");
            diagnostics.AppendLine($"Current State: {m_currentState}");
            diagnostics.AppendLine($"Avatar Type: {m_avatarType}");
            diagnostics.AppendLine($"Avatar ID: {m_avatarId}");
            diagnostics.AppendLine($"Enable Avatar: {m_enableAvatar}");
            diagnostics.AppendLine($"Hand Tracking Active: {m_isHandTrackingActive}");
            diagnostics.AppendLine($"Hand Tracking Accuracy: {m_handTrackingAccuracy:F2}");
            diagnostics.AppendLine($"Animation Update Rate: {m_animationUpdateRate:F1}Hz");
            diagnostics.AppendLine($"Joint Map Count: {m_jointMap.Count}");
            diagnostics.AppendLine($"Avatar Entity Present: {m_avatarEntity != null}");
            diagnostics.AppendLine($"Player Avatar Entity Present: {m_playerAvatarEntity != null}");
            diagnostics.AppendLine($"Lip Sync Context Present: {m_lipSyncContext != null}");
            diagnostics.AppendLine($"Enable LOD: {m_enableLOD}");
            diagnostics.AppendLine($"Max Render Distance: {m_maxRenderDistance:F1}m");
            
            return diagnostics.ToString();
        }

        private void CleanupAvatar()
        {
            // 取消事件监听
            if (m_inputManager != null)
            {
                m_inputManager.OnGestureRecognized -= OnHandGestureRecognized;
                m_inputManager.OnInputModeChanged -= OnInputModeChanged;
            }

            // 清理Avatar
            if (m_avatarEntity != null && m_avatarEntity.IsCreated)
            {
                m_avatarEntity.TeardownEntity();
            }

            m_jointMap.Clear();
            
            Debug.Log("[VRAvatarManager] Avatar cleanup completed");
        }
    }
}