using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using PongHub.Core;

namespace PongHub.MR
{
    /// <summary>
    /// 混合现实安全边界系统
    /// 负责监控用户与物理边界的距离，提供安全警告和自动保护机制
    /// </summary>
    public class MRSafetyBoundary : MonoBehaviour
    {
        [Header("Safety Settings")]
        [SerializeField]
        [Tooltip("边界警告距离 (米)")]
        [Range(0.1f, 2f)]
        private float m_warningDistance = 0.5f;

        [SerializeField]
        [Tooltip("边界临界距离 (米) - 自动禁用透视")]
        [Range(0.1f, 1f)]
        private float m_criticalDistance = 0.3f;

        [SerializeField]
        [Tooltip("边界超临界距离 (米) - 强制停止")]
        [Range(0.05f, 0.5f)]
        private float m_emergencyDistance = 0.15f;

        [SerializeField]
        [Tooltip("边界可视化预制件")]
        private GameObject m_boundaryVisualPrefab;

        [SerializeField]
        [Tooltip("警告提示预制件")]
        private GameObject m_warningIndicatorPrefab;

        [SerializeField]
        [Tooltip("启用音频警告")]
        private bool m_enableAudioWarning = true;

        [SerializeField]
        [Tooltip("启用触觉反馈")]
        private bool m_enableHapticFeedback = true;

        [Header("Visualization Settings")]
        [SerializeField]
        [Tooltip("边界可视化颜色 - 安全")]
        private Color m_safeColor = Color.green;

        [SerializeField]
        [Tooltip("边界可视化颜色 - 警告")]
        private Color m_warningColor = Color.yellow;

        [SerializeField]
        [Tooltip("边界可视化颜色 - 危险")]
        private Color m_dangerColor = Color.red;

        [SerializeField]
        [Tooltip("边界线宽度")]
        [Range(0.01f, 0.1f)]
        private float m_boundaryLineWidth = 0.02f;

        [SerializeField]
        [Tooltip("边界高度")]
        [Range(1f, 3f)]
        private float m_boundaryHeight = 2.5f;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("边界检测更新频率 (Hz)")]
        [Range(10f, 60f)]
        private float m_updateRate = 30f;

        [SerializeField]
        [Tooltip("平滑移动的衰减因子")]
        [Range(0.1f, 1f)]
        private float m_smoothingFactor = 0.8f;

        // 组件引用
        private MRPassthroughManager m_passthroughManager;
        private Camera m_mainCamera;
        private Transform m_headTransform;

        // 边界数据
        private List<Vector3> m_boundaryPoints = new List<Vector3>();
        private List<Vector3> m_smoothedBoundaryPoints = new List<Vector3>();
        private Vector3 m_playAreaCenter = Vector3.zero;
        private Vector2 m_playAreaSize = Vector2.zero;

        // 状态管理
        private bool m_isInitialized = false;
        private bool m_isNearBoundary = false;
        private bool m_isInCriticalZone = false;
        private bool m_isInEmergencyZone = false;
        private float m_closestBoundaryDistance = float.MaxValue;
        private Vector3 m_closestBoundaryPoint = Vector3.zero;
        private Vector3 m_lastHeadPosition = Vector3.zero;

        // 可视化对象
        private List<GameObject> m_boundaryVisuals = new List<GameObject>();
        private GameObject m_warningIndicator;
        private LineRenderer m_boundaryLineRenderer;

        // 性能优化
        private float m_lastUpdateTime = 0f;
        private float m_updateInterval = 0f;
        private Coroutine m_boundaryCheckCoroutine;

        // 事件
        public System.Action<bool> OnBoundaryWarningChanged;
        public System.Action<float> OnBoundaryDistanceChanged;
        public System.Action OnEmergencyStop;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 是否接近边界
        /// </summary>
        public bool IsNearBoundary => m_isNearBoundary;

        /// <summary>
        /// 是否在临界区域
        /// </summary>
        public bool IsInCriticalZone => m_isInCriticalZone;

        /// <summary>
        /// 是否在紧急区域
        /// </summary>
        public bool IsInEmergencyZone => m_isInEmergencyZone;

        /// <summary>
        /// 最近边界距离
        /// </summary>
        public float ClosestBoundaryDistance => m_closestBoundaryDistance;

        /// <summary>
        /// 游戏区域中心
        /// </summary>
        public Vector3 PlayAreaCenter => m_playAreaCenter;

        /// <summary>
        /// 游戏区域大小
        /// </summary>
        public Vector2 PlayAreaSize => m_playAreaSize;

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
            if (m_isInitialized)
            {
                UpdateVisualization();
            }
        }

        private void OnDestroy()
        {
            CleanupSystem();
        }

        private void InitializeComponents()
        {
            // 获取必需组件
            m_passthroughManager = FindObjectOfType<MRPassthroughManager>();
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
                m_mainCamera = FindObjectOfType<Camera>();

            // 获取头部变换（通常是中心眼锚点）
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
            m_updateInterval = 1f / m_updateRate;

            Debug.Log("[MRSafetyBoundary] Components initialized");
        }

        private IEnumerator InitializeAsync()
        {
            // 等待OVR系统初始化
            while (!OVRManager.isHMDPresent)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 等待边界系统初始化
            yield return new WaitForSeconds(1f);

            // 获取边界数据
            if (LoadBoundaryData())
            {
                SetupVisualization();
                StartBoundaryMonitoring();
                m_isInitialized = true;
                Debug.Log("[MRSafetyBoundary] Initialization complete");
            }
            else
            {
                Debug.LogWarning("[MRSafetyBoundary] Failed to load boundary data - using fallback settings");
                SetupFallbackBoundary();
                m_isInitialized = true;
            }
        }

        private bool LoadBoundaryData()
        {
            try
            {
                // 获取游戏区域边界点
                var boundaryPoints = OVRBoundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                if (boundaryPoints != null && boundaryPoints.Length > 0)
                {
                    m_boundaryPoints.Clear();
                    foreach (var point in boundaryPoints)
                    {
                        m_boundaryPoints.Add(point);
                    }

                    // 计算游戏区域中心和大小
                    CalculatePlayAreaBounds();
                    
                    Debug.Log($"[MRSafetyBoundary] Loaded {m_boundaryPoints.Count} boundary points");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MRSafetyBoundary] Error loading boundary data: {e.Message}");
            }

            return false;
        }

        private void CalculatePlayAreaBounds()
        {
            if (m_boundaryPoints.Count == 0) return;

            Vector3 min = m_boundaryPoints[0];
            Vector3 max = m_boundaryPoints[0];

            foreach (var point in m_boundaryPoints)
            {
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            m_playAreaCenter = (min + max) * 0.5f;
            m_playAreaSize = new Vector2(max.x - min.x, max.z - min.z);

            Debug.Log($"[MRSafetyBoundary] Play area center: {m_playAreaCenter}, size: {m_playAreaSize}");
        }

        private void SetupFallbackBoundary()
        {
            // 创建默认的2x2米游戏区域
            m_boundaryPoints.Clear();
            float halfSize = 1f;
            
            m_boundaryPoints.Add(new Vector3(-halfSize, 0, -halfSize));
            m_boundaryPoints.Add(new Vector3(halfSize, 0, -halfSize));
            m_boundaryPoints.Add(new Vector3(halfSize, 0, halfSize));
            m_boundaryPoints.Add(new Vector3(-halfSize, 0, halfSize));

            m_playAreaCenter = Vector3.zero;
            m_playAreaSize = new Vector2(2f, 2f);

            Debug.Log("[MRSafetyBoundary] Using fallback 2x2m boundary");
        }

        private void SetupVisualization()
        {
            // 创建边界线渲染器
            var boundaryLineObject = new GameObject("BoundaryLine");
            boundaryLineObject.transform.SetParent(transform);
            
            m_boundaryLineRenderer = boundaryLineObject.AddComponent<LineRenderer>();
            m_boundaryLineRenderer.material = CreateBoundaryMaterial();
            m_boundaryLineRenderer.startWidth = m_boundaryLineWidth;
            m_boundaryLineRenderer.endWidth = m_boundaryLineWidth;
            m_boundaryLineRenderer.useWorldSpace = true;
            m_boundaryLineRenderer.loop = true;

            // 设置边界点
            UpdateBoundaryVisualization();

            // 创建警告指示器
            if (m_warningIndicatorPrefab != null)
            {
                m_warningIndicator = Instantiate(m_warningIndicatorPrefab, transform);
                m_warningIndicator.SetActive(false);
            }

            Debug.Log("[MRSafetyBoundary] Visualization setup complete");
        }

        private Material CreateBoundaryMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = m_safeColor;
            return material;
        }

        private void UpdateBoundaryVisualization()
        {
            if (m_boundaryLineRenderer == null || m_boundaryPoints.Count == 0) return;

            // 创建3D边界点（包含高度）
            var visualPoints = new List<Vector3>();
            
            foreach (var point in m_boundaryPoints)
            {
                // 底部点
                visualPoints.Add(point);
                // 顶部点
                visualPoints.Add(point + Vector3.up * m_boundaryHeight);
            }

            // 闭合边界
            if (visualPoints.Count > 0)
            {
                visualPoints.Add(visualPoints[0]);
            }

            m_boundaryLineRenderer.positionCount = visualPoints.Count;
            m_boundaryLineRenderer.SetPositions(visualPoints.ToArray());
        }

        private void StartBoundaryMonitoring()
        {
            if (m_boundaryCheckCoroutine != null)
            {
                StopCoroutine(m_boundaryCheckCoroutine);
            }
            
            m_boundaryCheckCoroutine = StartCoroutine(BoundaryCheckLoop());
            Debug.Log("[MRSafetyBoundary] Boundary monitoring started");
        }

        private IEnumerator BoundaryCheckLoop()
        {
            while (m_isInitialized)
            {
                if (m_headTransform != null)
                {
                    UpdateBoundaryStatus();
                }
                
                yield return new WaitForSeconds(m_updateInterval);
            }
        }

        private void UpdateBoundaryStatus()
        {
            if (m_headTransform == null) return;

            Vector3 headPosition = m_headTransform.position;
            float closestDistance = GetDistanceToBoundary(headPosition);
            
            // 平滑距离变化
            m_closestBoundaryDistance = Mathf.Lerp(m_closestBoundaryDistance, closestDistance, 1f - m_smoothingFactor);

            // 检查各个安全区域
            bool wasNearBoundary = m_isNearBoundary;
            bool wasInCritical = m_isInCriticalZone;
            bool wasInEmergency = m_isInEmergencyZone;

            m_isNearBoundary = m_closestBoundaryDistance < m_warningDistance;
            m_isInCriticalZone = m_closestBoundaryDistance < m_criticalDistance;
            m_isInEmergencyZone = m_closestBoundaryDistance < m_emergencyDistance;

            // 处理状态变化
            if (wasNearBoundary != m_isNearBoundary)
            {
                OnBoundaryWarningChanged?.Invoke(m_isNearBoundary);
                HandleBoundaryWarning(m_isNearBoundary);
            }

            if (wasInCritical != m_isInCriticalZone)
            {
                HandleCriticalZone(m_isInCriticalZone);
            }

            if (wasInEmergency != m_isInEmergencyZone)
            {
                HandleEmergencyZone(m_isInEmergencyZone);
            }

            // 触发距离变化事件
            OnBoundaryDistanceChanged?.Invoke(m_closestBoundaryDistance);

            m_lastHeadPosition = headPosition;
        }

        private float GetDistanceToBoundary(Vector3 position)
        {
            if (m_boundaryPoints.Count == 0) return float.MaxValue;

            float minDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;

            // 计算到每个边界边的最短距离
            for (int i = 0; i < m_boundaryPoints.Count; i++)
            {
                Vector3 p1 = m_boundaryPoints[i];
                Vector3 p2 = m_boundaryPoints[(i + 1) % m_boundaryPoints.Count];
                
                Vector3 closestPointOnLine = GetClosestPointOnLine(position, p1, p2);
                float distance = Vector3.Distance(position, closestPointOnLine);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = closestPointOnLine;
                }
            }

            m_closestBoundaryPoint = closestPoint;
            return minDistance;
        }

        private Vector3 GetClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();

            Vector3 pointDirection = point - lineStart;
            float dot = Vector3.Dot(pointDirection, lineDirection);
            
            dot = Mathf.Clamp(dot, 0f, lineLength);
            return lineStart + lineDirection * dot;
        }

        private void HandleBoundaryWarning(bool isNear)
        {
            if (isNear)
            {
                ShowBoundaryVisualization(true);
                
                if (m_enableAudioWarning)
                {
                    PlayWarningSound();
                }
                
                if (m_enableHapticFeedback)
                {
                    TriggerHapticFeedback(0.3f);
                }
                
                Debug.LogWarning("[MRSafetyBoundary] User approaching boundary");
            }
            else
            {
                ShowBoundaryVisualization(false);
                Debug.Log("[MRSafetyBoundary] User moved away from boundary");
            }
        }

        private void HandleCriticalZone(bool inCritical)
        {
            if (inCritical)
            {
                // 自动禁用透视以确保安全
                if (m_passthroughManager != null)
                {
                    m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.Disabled);
                }
                
                if (m_enableHapticFeedback)
                {
                    TriggerHapticFeedback(0.7f);
                }
                
                Debug.LogWarning("[MRSafetyBoundary] Critical zone entered - Passthrough disabled for safety");
            }
        }

        private void HandleEmergencyZone(bool inEmergency)
        {
            if (inEmergency)
            {
                // 触发紧急停止
                OnEmergencyStop?.Invoke();
                
                // 强制禁用所有MR功能
                if (m_passthroughManager != null)
                {
                    m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.Disabled);
                }
                
                if (m_enableHapticFeedback)
                {
                    TriggerHapticFeedback(1.0f);
                }
                
                Debug.LogError("[MRSafetyBoundary] EMERGENCY: User too close to boundary - All MR functions disabled");
            }
        }

        /// <summary>
        /// 显示/隐藏边界可视化
        /// </summary>
        public void ShowBoundaryVisualization(bool show)
        {
            if (m_boundaryLineRenderer != null)
            {
                m_boundaryLineRenderer.enabled = show;
                
                // 根据安全状态更新颜色
                Color targetColor = m_safeColor;
                if (m_isInEmergencyZone)
                    targetColor = m_dangerColor;
                else if (m_isInCriticalZone)
                    targetColor = m_dangerColor;
                else if (m_isNearBoundary)
                    targetColor = m_warningColor;
                
                m_boundaryLineRenderer.material.color = targetColor;
            }

            if (m_warningIndicator != null)
            {
                m_warningIndicator.SetActive(show && m_isNearBoundary);
                
                if (show && m_isNearBoundary)
                {
                    // 将警告指示器放置在最近的边界点
                    m_warningIndicator.transform.position = m_closestBoundaryPoint + Vector3.up * 1.5f;
                    m_warningIndicator.transform.LookAt(m_headTransform);
                }
            }
        }

        private void PlayWarningSound()
        {
            // 播放警告音效（需要AudioSource组件）
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        private void TriggerHapticFeedback(float intensity)
        {
            // 触发控制器震动反馈
            OVRInput.SetControllerVibration(intensity, intensity, OVRInput.Controller.Touch);
            
            // 延迟停止震动
            StartCoroutine(StopHapticFeedback(0.2f));
        }

        private IEnumerator StopHapticFeedback(float delay)
        {
            yield return new WaitForSeconds(delay);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.Touch);
        }

        private void UpdateVisualization()
        {
            // 更新边界可视化效果（如脉动、颜色变化等）
            if (m_boundaryLineRenderer != null && m_boundaryLineRenderer.enabled)
            {
                // 创建脉动效果
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                float alpha = m_isNearBoundary ? 0.5f + pulse * 0.5f : 0.3f;
                
                var color = m_boundaryLineRenderer.material.color;
                color.a = alpha;
                m_boundaryLineRenderer.material.color = color;
            }
        }

        /// <summary>
        /// 设置安全距离参数
        /// </summary>
        public void SetSafetyDistances(float warning, float critical, float emergency)
        {
            m_warningDistance = Mathf.Clamp(warning, 0.1f, 2f);
            m_criticalDistance = Mathf.Clamp(critical, 0.1f, 1f);
            m_emergencyDistance = Mathf.Clamp(emergency, 0.05f, 0.5f);
            
            Debug.Log($"[MRSafetyBoundary] Safety distances updated: warning={warning:F2}m, critical={critical:F2}m, emergency={emergency:F2}m");
        }

        /// <summary>
        /// 重新加载边界数据
        /// </summary>
        public void RefreshBoundaryData()
        {
            if (LoadBoundaryData())
            {
                UpdateBoundaryVisualization();
                Debug.Log("[MRSafetyBoundary] Boundary data refreshed");
            }
        }

        /// <summary>
        /// 获取系统诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== MR Safety Boundary Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"Boundary Points: {m_boundaryPoints.Count}");
            diagnostics.AppendLine($"Play Area Center: {m_playAreaCenter}");
            diagnostics.AppendLine($"Play Area Size: {m_playAreaSize}");
            diagnostics.AppendLine($"Head Position: {(m_headTransform != null ? m_headTransform.position.ToString("F2") : "N/A")}");
            diagnostics.AppendLine($"Closest Distance: {m_closestBoundaryDistance:F2}m");
            diagnostics.AppendLine($"Near Boundary: {m_isNearBoundary}");
            diagnostics.AppendLine($"Critical Zone: {m_isInCriticalZone}");
            diagnostics.AppendLine($"Emergency Zone: {m_isInEmergencyZone}");
            diagnostics.AppendLine($"Warning Distance: {m_warningDistance:F2}m");
            diagnostics.AppendLine($"Critical Distance: {m_criticalDistance:F2}m");
            diagnostics.AppendLine($"Emergency Distance: {m_emergencyDistance:F2}m");
            diagnostics.AppendLine($"Update Rate: {m_updateRate:F1}Hz");
            diagnostics.AppendLine($"Audio Warning: {m_enableAudioWarning}");
            diagnostics.AppendLine($"Haptic Feedback: {m_enableHapticFeedback}");
            
            return diagnostics.ToString();
        }

        private void CleanupSystem()
        {
            // 停止边界监控
            if (m_boundaryCheckCoroutine != null)
            {
                StopCoroutine(m_boundaryCheckCoroutine);
                m_boundaryCheckCoroutine = null;
            }

            // 清理可视化对象
            foreach (var visual in m_boundaryVisuals)
            {
                if (visual != null)
                {
                    DestroyImmediate(visual);
                }
            }
            m_boundaryVisuals.Clear();

            if (m_warningIndicator != null)
            {
                DestroyImmediate(m_warningIndicator);
                m_warningIndicator = null;
            }

            if (m_boundaryLineRenderer != null)
            {
                DestroyImmediate(m_boundaryLineRenderer.gameObject);
                m_boundaryLineRenderer = null;
            }

            Debug.Log("[MRSafetyBoundary] System cleanup completed");
        }
    }
}