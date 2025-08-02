using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using PongHub.Core;
using PongHub.VR;

namespace PongHub.MR
{
    /// <summary>
    /// 混合现实透视管理器
    /// 基于Meta XR SDK的OVRPassthroughLayer实现MR功能
    /// 支持透视模式切换、环境融合、安全边界管理
    /// </summary>
    public class MRPassthroughManager : MonoBehaviour
    {
        /// <summary>
        /// 透视模式枚举
        /// </summary>
        public enum PassthroughMode
        {
            Disabled,           // 关闭透视，纯VR模式
            FullPassthrough,    // 全透视，完全MR模式
            SelectivePassthrough // 选择性透视，混合模式
        }

        [Header("Passthrough Settings")]
        [SerializeField]
        [Tooltip("是否启用透视功能")]
        private bool m_enablePassthrough = false;

        [SerializeField]
        [Tooltip("透视模式")]
        private PassthroughMode m_passthroughMode = PassthroughMode.Disabled;

        [SerializeField]
        [Tooltip("透视不透明度 (0-1)")]
        [Range(0f, 1f)]
        private float m_passthroughOpacity = 1.0f;

        [SerializeField]
        [Tooltip("是否启用颜色映射")]
        private bool m_enableColorMapping = true;

        [SerializeField]
        [Tooltip("是否启用边缘渲染")]
        private bool m_enableEdgeRendering = false;

        [SerializeField]
        [Tooltip("边缘渲染颜色")]
        private Color m_edgeRenderingColor = Color.white;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("透视更新频率 (FPS)")]
        [Range(30f, 90f)]
        private float m_passthroughUpdateRate = 60f;

        [SerializeField]
        [Tooltip("是否启用性能优化")]
        private bool m_enablePerformanceOptimization = true;

        [Header("Safety Settings")]
        [SerializeField]
        [Tooltip("边界警告距离 (米)")]
        [Range(0.1f, 2f)]
        private float m_boundaryWarningDistance = 0.5f;

        [SerializeField]
        [Tooltip("边界临界距离 (米)")]
        [Range(0.1f, 1f)]
        private float m_boundaryDisableDistance = 0.3f;

        [SerializeField]
        [Tooltip("启用安全边界检测")]
        private bool m_enableSafetyBoundary = true;

        // Meta SDK组件引用
        private OVRPassthroughLayer m_passthroughLayer;
        private OVRManager m_ovrManager;
        private OVRCameraRig m_cameraRig;

        // 状态管理
        private PassthroughMode m_currentMode = PassthroughMode.Disabled;
        private bool m_isPassthroughAvailable = false;
        private bool m_isInitialized = false;

        // 性能监控
        private float m_lastUpdateTime = 0f;
        private int m_framesSinceLastUpdate = 0;
        private float m_averageFPS = 90f;

        // 边界管理
        private bool m_isNearBoundary = false;
        private Vector3 m_lastHeadPosition = Vector3.zero;

        // 事件
        public UnityEvent<PassthroughMode> OnPassthroughModeChanged = new UnityEvent<PassthroughMode>();
        public UnityEvent<bool> OnPassthroughAvailabilityChanged = new UnityEvent<bool>();
        public UnityEvent<bool> OnBoundaryWarningChanged = new UnityEvent<bool>();

        /// <summary>
        /// 当前透视模式
        /// </summary>
        public PassthroughMode CurrentMode => m_currentMode;

        /// <summary>
        /// 透视功能是否可用
        /// </summary>
        public bool IsPassthroughAvailable => m_isPassthroughAvailable;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 当前透视不透明度
        /// </summary>
        public float CurrentOpacity => m_passthroughOpacity;

        /// <summary>
        /// 是否接近边界
        /// </summary>
        public bool IsNearBoundary => m_isNearBoundary;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartCoroutine(InitializePassthroughAsync());
        }

        private void Update()
        {
            if (m_isInitialized)
            {
                UpdatePerformanceMonitoring();
                if (m_enableSafetyBoundary)
                {
                    UpdateSafetyBoundary();
                }
            }
        }

        private void OnDestroy()
        {
            CleanupPassthrough();
        }

        private void InitializeComponents()
        {
            // 查找必需的组件
            m_ovrManager = FindObjectOfType<OVRManager>();
            m_cameraRig = FindObjectOfType<OVRCameraRig>();

            if (m_ovrManager == null)
            {
                Debug.LogError("[MRPassthroughManager] OVRManager not found in scene");
                return;
            }

            if (m_cameraRig == null)
            {
                Debug.LogError("[MRPassthroughManager] OVRCameraRig not found in scene");
                return;
            }

            Debug.Log("[MRPassthroughManager] Components initialized successfully");
        }

        private IEnumerator InitializePassthroughAsync()
        {
            // 等待OVR系统初始化
            while (!OVRManager.isHmdPresent)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 检查设备是否支持Passthrough
            yield return StartCoroutine(CheckPassthroughSupport());

            if (m_isPassthroughAvailable)
            {
                SetupPassthroughLayer();
                m_isInitialized = true;

                if (m_enablePassthrough)
                {
                    SetPassthroughMode(m_passthroughMode);
                }

                Debug.Log("[MRPassthroughManager] Passthrough initialized successfully");
            }
            else
            {
                Debug.LogWarning("[MRPassthroughManager] Passthrough not supported on this device");
            }
        }

        private IEnumerator CheckPassthroughSupport()
        {
            // 检查设备能力
            yield return new WaitForEndOfFrame();

            try
            {
                // 尝试查询Passthrough支持
                var headsetType = OVRPlugin.GetSystemHeadsetType();
                m_isPassthroughAvailable = headsetType == OVRPlugin.SystemHeadset.Oculus_Quest_2 ||
                                          headsetType == OVRPlugin.SystemHeadset.Meta_Quest_3 ||
                                          headsetType == OVRPlugin.SystemHeadset.Meta_Quest_Pro;

                Debug.Log($"[MRPassthroughManager] Device: {OVRPlugin.GetSystemHeadsetType()}, Passthrough Available: {m_isPassthroughAvailable}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MRPassthroughManager] Error checking passthrough support: {e.Message}");
                m_isPassthroughAvailable = false;
            }

            OnPassthroughAvailabilityChanged?.Invoke(m_isPassthroughAvailable);
        }

        private void SetupPassthroughLayer()
        {
            // 创建或获取PassthroughLayer组件
            m_passthroughLayer = GetComponent<OVRPassthroughLayer>();
            if (m_passthroughLayer == null)
            {
                m_passthroughLayer = gameObject.AddComponent<OVRPassthroughLayer>();
            }

            // 配置PassthroughLayer
            ConfigurePassthroughLayer();

            Debug.Log("[MRPassthroughManager] Passthrough layer setup complete");
        }

        private void ConfigurePassthroughLayer()
        {
            if (m_passthroughLayer == null) return;

            // 基础设置
            m_passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
            m_passthroughLayer.compositionDepth = 0;
            m_passthroughLayer.hidden = true; // 默认隐藏

            // 颜色和透明度设置
            m_passthroughLayer.overridePerLayerColorScaleAndOffset = true;
            m_passthroughLayer.colorScale = new Vector4(1f, 1f, 1f, m_passthroughOpacity);
            m_passthroughLayer.colorOffset = Vector4.zero;

            // 边缘渲染设置
            m_passthroughLayer.edgeRenderingEnabled = m_enableEdgeRendering;
            m_passthroughLayer.edgeColor = m_edgeRenderingColor;

            // 投射表面类型
            m_passthroughLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;
        }

        /// <summary>
        /// 设置透视模式
        /// </summary>
        public void SetPassthroughMode(PassthroughMode mode)
        {
            if (!m_isInitialized)
            {
                Debug.LogWarning("[MRPassthroughManager] Cannot set mode - not initialized");
                return;
            }

            if (mode != PassthroughMode.Disabled && !m_isPassthroughAvailable)
            {
                Debug.LogWarning("[MRPassthroughManager] Cannot enable passthrough - not supported on device");
                return;
            }

            var previousMode = m_currentMode;
            m_currentMode = mode;

            ApplyPassthroughMode();

            OnPassthroughModeChanged?.Invoke(mode);
            Debug.Log($"[MRPassthroughManager] Mode changed: {previousMode} -> {mode}");
        }

        private void ApplyPassthroughMode()
        {
            if (m_passthroughLayer == null) return;

            switch (m_currentMode)
            {
                case PassthroughMode.Disabled:
                    DisablePassthrough();
                    break;

                case PassthroughMode.FullPassthrough:
                    EnableFullPassthrough();
                    break;

                case PassthroughMode.SelectivePassthrough:
                    EnableSelectivePassthrough();
                    break;
            }
        }

        private void DisablePassthrough()
        {
            if (m_ovrManager != null)
            {
                m_ovrManager.isInsightPassthroughEnabled = false;
            }

            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.hidden = true;
            }

            Debug.Log("[MRPassthroughManager] Passthrough disabled");
        }

        private void EnableFullPassthrough()
        {
            if (m_ovrManager != null)
            {
                m_ovrManager.isInsightPassthroughEnabled = true;
            }

            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.hidden = false;
                m_passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
                m_passthroughLayer.colorScale = new Vector4(1f, 1f, 1f, m_passthroughOpacity);
            }

            Debug.Log("[MRPassthroughManager] Full passthrough enabled");
        }

        private void EnableSelectivePassthrough()
        {
            if (m_ovrManager != null)
            {
                m_ovrManager.isInsightPassthroughEnabled = true;
            }

            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.hidden = false;
                m_passthroughLayer.overlayType = OVROverlay.OverlayType.Overlay;
                m_passthroughLayer.colorScale = new Vector4(1f, 1f, 1f, m_passthroughOpacity * 0.7f); // 降低不透明度
            }

            Debug.Log("[MRPassthroughManager] Selective passthrough enabled");
        }

        /// <summary>
        /// 设置透视不透明度
        /// </summary>
        public void SetPassthroughOpacity(float opacity)
        {
            m_passthroughOpacity = Mathf.Clamp01(opacity);

            if (m_passthroughLayer != null && m_currentMode != PassthroughMode.Disabled)
            {
                var scale = m_passthroughLayer.colorScale;
                scale.w = m_passthroughOpacity;
                m_passthroughLayer.colorScale = scale;
            }

            Debug.Log($"[MRPassthroughManager] Opacity set to {m_passthroughOpacity:F2}");
        }

        /// <summary>
        /// 启用/禁用边缘渲染
        /// </summary>
        public void SetEdgeRenderingEnabled(bool enabled)
        {
            m_enableEdgeRendering = enabled;

            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.edgeRenderingEnabled = enabled;
            }

            Debug.Log($"[MRPassthroughManager] Edge rendering {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// 设置边缘渲染颜色
        /// </summary>
        public void SetEdgeRenderingColor(Color color)
        {
            m_edgeRenderingColor = color;

            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.edgeColor = color;
            }
        }

        private void UpdatePerformanceMonitoring()
        {
            m_framesSinceLastUpdate++;
            float deltaTime = Time.time - m_lastUpdateTime;

            if (deltaTime >= 1f / m_passthroughUpdateRate)
            {
                m_averageFPS = m_framesSinceLastUpdate / deltaTime;
                m_framesSinceLastUpdate = 0;
                m_lastUpdateTime = Time.time;

                // 性能优化：如果帧率过低，降低透视质量
                if (m_enablePerformanceOptimization && m_averageFPS < 60f && m_currentMode != PassthroughMode.Disabled)
                {
                    OptimizePerformance();
                }
            }
        }

        private void OptimizePerformance()
        {
            // 动态调整透视设置以提高性能
            if (m_passthroughOpacity > 0.7f)
            {
                SetPassthroughOpacity(0.7f);
                Debug.Log("[MRPassthroughManager] Performance optimization: Reduced opacity");
            }

            if (m_enableEdgeRendering)
            {
                SetEdgeRenderingEnabled(false);
                Debug.Log("[MRPassthroughManager] Performance optimization: Disabled edge rendering");
            }
        }

        private void UpdateSafetyBoundary()
        {
            if (m_cameraRig?.centerEyeAnchor == null) return;

            Vector3 headPosition = m_cameraRig.centerEyeAnchor.position;

            // 检查是否接近边界
            bool wasNearBoundary = m_isNearBoundary;
            m_isNearBoundary = CheckBoundaryProximity(headPosition);

            // 边界状态变化时触发事件
            if (wasNearBoundary != m_isNearBoundary)
            {
                OnBoundaryWarningChanged?.Invoke(m_isNearBoundary);

                if (m_isNearBoundary)
                {
                    Debug.LogWarning("[MRPassthroughManager] User approaching boundary");

                    // 如果非常接近边界，自动禁用透视以确保安全
                    float distanceToBoundary = GetDistanceToBoundary(headPosition);
                    if (distanceToBoundary < m_boundaryDisableDistance)
                    {
                        SetPassthroughMode(PassthroughMode.Disabled);
                        Debug.LogWarning("[MRPassthroughManager] Passthrough disabled for safety - too close to boundary");
                    }
                }
            }

            m_lastHeadPosition = headPosition;
        }

        private bool CheckBoundaryProximity(Vector3 position)
        {
            float distanceToBoundary = GetDistanceToBoundary(position);
            return distanceToBoundary < m_boundaryWarningDistance;
        }

        private float GetDistanceToBoundary(Vector3 position)
        {
            // 使用OVRBoundary API获取边界距离
            try
            {
                var ovrBoundary = new OVRBoundary();
                var boundaryPoints = ovrBoundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                if (boundaryPoints.Length == 0)
                    return float.MaxValue;

                float minDistance = float.MaxValue;
                for (int i = 0; i < boundaryPoints.Length; i++)
                {
                    Vector3 boundaryPoint = boundaryPoints[i];
                    float distance = Vector3.Distance(position, boundaryPoint);
                    minDistance = Mathf.Min(minDistance, distance);
                }

                return minDistance;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MRPassthroughManager] Error getting boundary distance: {e.Message}");
                return float.MaxValue;
            }
        }

        /// <summary>
        /// 强制刷新透视设置
        /// </summary>
        public void RefreshPassthrough()
        {
            if (m_isInitialized && m_currentMode != PassthroughMode.Disabled)
            {
                ApplyPassthroughMode();
                Debug.Log("[MRPassthroughManager] Passthrough refreshed");
            }
        }

        /// <summary>
        /// 获取系统诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== MR Passthrough Manager Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"Passthrough Available: {m_isPassthroughAvailable}");
            diagnostics.AppendLine($"Current Mode: {m_currentMode}");
            diagnostics.AppendLine($"Opacity: {m_passthroughOpacity:F2}");
            diagnostics.AppendLine($"Edge Rendering: {m_enableEdgeRendering}");
            diagnostics.AppendLine($"Average FPS: {m_averageFPS:F1}");
            diagnostics.AppendLine($"Near Boundary: {m_isNearBoundary}");
            diagnostics.AppendLine($"OVR Manager Present: {m_ovrManager != null}");
            diagnostics.AppendLine($"Passthrough Layer Present: {m_passthroughLayer != null}");

            if (m_ovrManager != null)
            {
                diagnostics.AppendLine($"Insight Passthrough Enabled: {m_ovrManager.isInsightPassthroughEnabled}");
            }

            return diagnostics.ToString();
        }

        private void CleanupPassthrough()
        {
            if (m_passthroughLayer != null)
            {
                m_passthroughLayer.hidden = true;
            }

            if (m_ovrManager != null)
            {
                m_ovrManager.isInsightPassthroughEnabled = false;
            }

            Debug.Log("[MRPassthroughManager] Cleanup completed");
        }

        /// <summary>
        /// 检查设备是否支持彩色透视
        /// </summary>
        public bool SupportsColorPassthrough()
        {
            var headsetType = OVRPlugin.GetSystemHeadsetType();
            return headsetType == OVRPlugin.SystemHeadset.Meta_Quest_3 ||
                   headsetType == OVRPlugin.SystemHeadset.Meta_Quest_Pro;
        }

        /// <summary>
        /// 获取推荐的透视设置
        /// </summary>
        public void ApplyRecommendedSettings()
        {
            var headsetType = OVRPlugin.GetSystemHeadsetType();

            switch (headsetType)
            {
                case OVRPlugin.SystemHeadset.Oculus_Quest_2:
                    // Quest 2: 黑白透视，较低分辨率
                    m_passthroughOpacity = 0.8f;
                    m_enableEdgeRendering = true;
                    m_passthroughUpdateRate = 60f;
                    break;

                case OVRPlugin.SystemHeadset.Meta_Quest_3:
                    // Quest 3: 彩色透视，高分辨率
                    m_passthroughOpacity = 0.9f;
                    m_enableEdgeRendering = false;
                    m_passthroughUpdateRate = 90f;
                    break;

                case OVRPlugin.SystemHeadset.Meta_Quest_Pro:
                    // Quest Pro: 彩色透视，高分辨率
                    m_passthroughOpacity = 0.9f;
                    m_enableEdgeRendering = false;
                    m_passthroughUpdateRate = 90f;
                    break;

                default:
                    // 默认保守设置
                    m_passthroughOpacity = 0.7f;
                    m_enableEdgeRendering = true;
                    m_passthroughUpdateRate = 60f;
                    break;
            }

            if (m_isInitialized)
            {
                ConfigurePassthroughLayer();
                ApplyPassthroughMode();
            }

            Debug.Log($"[MRPassthroughManager] Applied recommended settings for {headsetType}");
        }
    }
}