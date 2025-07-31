using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;
using PongHub.Core;

namespace PongHub.Performance
{
    /// <summary>
    /// VR性能监控系统
    /// 专为Meta Quest设备优化，监控关键VR性能指标，确保120fps稳定运行
    /// Epic-4 Story-15: VR性能优化和帧率稳定
    /// </summary>
    public class VRPerformanceMonitor : MonoBehaviour, IGameModeComponent
    {
        [Header("Performance Targets / 性能目标")]
        [SerializeField]
        [Tooltip("Target FPS / 目标帧率 - Target framerate for VR (typically 72, 90, or 120)")]
        private int m_targetFPS = 120;

        [SerializeField]
        [Tooltip("Frame Time Warning Threshold / 帧时间警告阈值 - Frame time threshold for warnings (ms)")]
        private float m_frameTimeWarningThreshold = 8.33f; // 120fps = 8.33ms per frame

        [SerializeField]
        [Tooltip("CPU Time Warning Threshold / CPU时间警告阈值 - CPU time threshold for warnings (ms)")]
        private float m_cpuTimeWarningThreshold = 5.0f;

        [SerializeField]
        [Tooltip("GPU Time Warning Threshold / GPU时间警告阈值 - GPU time threshold for warnings (ms)")]
        private float m_gpuTimeWarningThreshold = 6.0f;

        [Header("Monitoring Settings / 监控设置")]
        [SerializeField]
        [Tooltip("Update Interval / 更新间隔 - How often to update performance metrics (seconds)")]
        private float m_updateInterval = 0.5f;

        [SerializeField]
        [Tooltip("Sample Count / 采样数量 - Number of samples to keep for averaging")]
        private int m_sampleCount = 60;

        [SerializeField]
        [Tooltip("Enable Performance Warnings / 启用性能警告 - Show warnings when performance drops")]
        private bool m_enablePerformanceWarnings = true;

        [SerializeField]
        [Tooltip("Enable Auto Optimization / 启用自动优化 - Automatically adjust settings when performance drops")]
        private bool m_enableAutoOptimization = false;

        [Header("Display Settings / 显示设置")]
        [SerializeField]
        [Tooltip("Show Performance HUD / 显示性能HUD - Show performance overlay in VR")]
        private bool m_showPerformanceHUD = false;

        [SerializeField]
        [Tooltip("HUD Position / HUD位置 - Position of the performance HUD in world space")]
        private Vector3 m_hudPosition = new Vector3(0, 2.5f, 2);

        [SerializeField]
        [Tooltip("HUD Canvas / HUD画布 - Canvas for displaying performance information")]
        private Canvas m_hudCanvas;

        [SerializeField]
        [Tooltip("Performance Text / 性能文本 - UI Text component for displaying performance stats")]
        private UnityEngine.UI.Text m_performanceText;

        // Performance metrics / 性能指标
        private Queue<float> m_frameTimeHistory = new Queue<float>();
        private Queue<float> m_cpuTimeHistory = new Queue<float>();
        private Queue<float> m_gpuTimeHistory = new Queue<float>();
        private Queue<int> m_droppedFramesHistory = new Queue<int>();

        // Current performance state / 当前性能状态
        private float m_currentFPS;
        private float m_averageFrameTime;
        private float m_averageCPUTime;
        private float m_averageGPUTime;
        private int m_droppedFramesCount;
        private int m_totalFramesCount;

        // VR specific metrics / VR特定指标
        private float m_vrDisplayRefreshRate;
        private bool m_isVRActive;
        private string m_vrDeviceName;

        // Timing / 计时
        private float m_lastUpdateTime;
        private float m_lastFrameTime;

        // Performance warnings / 性能警告
        private List<PerformanceWarning> m_activeWarnings = new List<PerformanceWarning>();

        // Static instance for global access / 全局访问的静态实例
        public static VRPerformanceMonitor Instance { get; private set; }

        #region Properties / 属性

        /// <summary>
        /// 当前FPS
        /// </summary>
        public float CurrentFPS => m_currentFPS;

        /// <summary>
        /// 平均帧时间（毫秒）
        /// </summary>
        public float AverageFrameTime => m_averageFrameTime;

        /// <summary>
        /// 平均CPU时间（毫秒）
        /// </summary>
        public float AverageCPUTime => m_averageCPUTime;

        /// <summary>
        /// 平均GPU时间（毫秒）
        /// </summary>
        public float AverageGPUTime => m_averageGPUTime;

        /// <summary>
        /// 丢帧数量
        /// </summary>
        public int DroppedFramesCount => m_droppedFramesCount;

        /// <summary>
        /// 性能评级
        /// </summary>
        public PerformanceGrade CurrentPerformanceGrade => GetPerformanceGrade();

        /// <summary>
        /// 是否正在运行VR
        /// </summary>
        public bool IsVRActive => m_isVRActive;

        #endregion

        #region Unity Lifecycle / Unity生命周期

        private void Awake()
        {
            // Singleton pattern / 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeVRInfo();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Register with GameModeManager / 注册到游戏模式管理器
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }

            InitializeHUD();
            m_lastUpdateTime = Time.unscaledTime;
            m_lastFrameTime = Time.unscaledTime;
        }

        private void Update()
        {
            UpdatePerformanceMetrics();

            if (Time.unscaledTime - m_lastUpdateTime >= m_updateInterval)
            {
                ProcessPerformanceData();
                UpdateHUD();
                CheckPerformanceWarnings();
                m_lastUpdateTime = Time.unscaledTime;
            }
        }

        private void OnDestroy()
        {
            // Unregister from GameModeManager / 从游戏模式管理器注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region IGameModeComponent Implementation / 游戏模式组件实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            // Adjust monitoring based on game mode / 根据游戏模式调整监控
            switch (newMode)
            {
                case GameMode.Menu:
                    m_enableAutoOptimization = false; // 菜单模式下不自动优化
                    break;
                case GameMode.Local:
                case GameMode.Network:
                    m_enableAutoOptimization = true; // 游戏模式下启用自动优化
                    break;
            }

            Debug.Log($"[VRPerformanceMonitor] 游戏模式切换: {previousMode} → {newMode}");
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // Performance monitoring is active in all modes / 性能监控在所有模式下都活跃
            return true;
        }

        #endregion

        #region Performance Monitoring / 性能监控

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            float currentTime = Time.unscaledTime;
            float frameTime = (currentTime - m_lastFrameTime) * 1000f; // Convert to milliseconds / 转换为毫秒
            m_lastFrameTime = currentTime;

            // Add frame time to history / 添加帧时间到历史记录
            m_frameTimeHistory.Enqueue(frameTime);
            if (m_frameTimeHistory.Count > m_sampleCount)
            {
                m_frameTimeHistory.Dequeue();
            }

            // Update FPS / 更新FPS
            m_currentFPS = 1000f / frameTime;
            m_totalFramesCount++;

            // Check for dropped frames / 检查丢帧
            if (frameTime > m_frameTimeWarningThreshold)
            {
                m_droppedFramesCount++;
            }

            // Update CPU and GPU times (using Unity's profiler data) / 更新CPU和GPU时间
            UpdateCPUGPUTimes();
        }

        /// <summary>
        /// 更新CPU和GPU时间
        /// </summary>
        private void UpdateCPUGPUTimes()
        {
            // Get CPU time from Unity profiler / 从Unity分析器获取CPU时间
            float cpuTime = Time.unscaledDeltaTime * 1000f; // Approximation / 近似值
            
            m_cpuTimeHistory.Enqueue(cpuTime);
            if (m_cpuTimeHistory.Count > m_sampleCount)
            {
                m_cpuTimeHistory.Dequeue();
            }

            // GPU time would require more sophisticated profiling / GPU时间需要更复杂的分析
            // For now, we'll use a simplified approach / 目前使用简化方法
            float gpuTime = cpuTime * 0.8f; // Approximation / 近似值
            
            m_gpuTimeHistory.Enqueue(gpuTime);
            if (m_gpuTimeHistory.Count > m_sampleCount)
            {
                m_gpuTimeHistory.Dequeue();
            }
        }

        /// <summary>
        /// 处理性能数据
        /// </summary>
        private void ProcessPerformanceData()
        {
            // Calculate averages / 计算平均值
            m_averageFrameTime = m_frameTimeHistory.Count > 0 ? m_frameTimeHistory.Average() : 0f;
            m_averageCPUTime = m_cpuTimeHistory.Count > 0 ? m_cpuTimeHistory.Average() : 0f;
            m_averageGPUTime = m_gpuTimeHistory.Count > 0 ? m_gpuTimeHistory.Average() : 0f;

            // Update dropped frames history / 更新丢帧历史
            m_droppedFramesHistory.Enqueue(m_droppedFramesCount);
            if (m_droppedFramesHistory.Count > m_sampleCount / 10) // Keep shorter history for dropped frames / 为丢帧保持更短的历史
            {
                m_droppedFramesHistory.Dequeue();
            }
        }

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private void CheckPerformanceWarnings()
        {
            if (!m_enablePerformanceWarnings) return;

            m_activeWarnings.Clear();

            // Check frame time warning / 检查帧时间警告
            if (m_averageFrameTime > m_frameTimeWarningThreshold)
            {
                m_activeWarnings.Add(new PerformanceWarning
                {
                    Type = WarningType.FrameTime,
                    Message = $"帧时间过高: {m_averageFrameTime:F2}ms (目标: {m_frameTimeWarningThreshold:F2}ms)",
                    Severity = GetWarningSeverity(m_averageFrameTime, m_frameTimeWarningThreshold)
                });
            }

            // Check CPU time warning / 检查CPU时间警告
            if (m_averageCPUTime > m_cpuTimeWarningThreshold)
            {
                m_activeWarnings.Add(new PerformanceWarning
                {
                    Type = WarningType.CPUTime,
                    Message = $"CPU时间过高: {m_averageCPUTime:F2}ms (目标: {m_cpuTimeWarningThreshold:F2}ms)",
                    Severity = GetWarningSeverity(m_averageCPUTime, m_cpuTimeWarningThreshold)
                });
            }

            // Check GPU time warning / 检查GPU时间警告
            if (m_averageGPUTime > m_gpuTimeWarningThreshold)
            {
                m_activeWarnings.Add(new PerformanceWarning
                {
                    Type = WarningType.GPUTime,
                    Message = $"GPU时间过高: {m_averageGPUTime:F2}ms (目标: {m_gpuTimeWarningThreshold:F2}ms)",
                    Severity = GetWarningSeverity(m_averageGPUTime, m_gpuTimeWarningThreshold)
                });
            }

            // Auto optimization if enabled / 如果启用自动优化
            if (m_enableAutoOptimization && m_activeWarnings.Count > 0)
            {
                ApplyAutoOptimizations();
            }

            // Log warnings / 记录警告
            foreach (var warning in m_activeWarnings)
            {
                Debug.LogWarning($"[VRPerformanceMonitor] {warning.Message}");
            }
        }

        #endregion

        #region VR Information / VR信息

        /// <summary>
        /// 初始化VR信息
        /// </summary>
        private void InitializeVRInfo()
        {
            m_isVRActive = XRSettings.enabled && XRSettings.isDeviceActive;
            
            if (m_isVRActive)
            {
                m_vrDeviceName = XRSettings.loadedDeviceName;
                m_vrDisplayRefreshRate = XRDevice.refreshRate;
                
                // Adjust target FPS based on VR display refresh rate / 根据VR显示刷新率调整目标FPS
                if (m_vrDisplayRefreshRate > 0)
                {
                    m_targetFPS = Mathf.RoundToInt(m_vrDisplayRefreshRate);
                    m_frameTimeWarningThreshold = 1000f / m_targetFPS;
                }
                
                Debug.Log($"[VRPerformanceMonitor] VR设备检测: {m_vrDeviceName}, 刷新率: {m_vrDisplayRefreshRate}Hz, 目标FPS: {m_targetFPS}");
            }
            else
            {
                Debug.Log("[VRPerformanceMonitor] 非VR模式运行");
            }
        }

        #endregion

        #region HUD Display / HUD显示

        /// <summary>
        /// 初始化HUD
        /// </summary>
        private void InitializeHUD()
        {
            if (m_hudCanvas == null)
            {
                // Create HUD canvas if not assigned / 如果未分配则创建HUD画布
                GameObject hudObject = new GameObject("VR Performance HUD");
                hudObject.transform.SetParent(transform);
                hudObject.transform.localPosition = m_hudPosition;

                m_hudCanvas = hudObject.AddComponent<Canvas>();
                m_hudCanvas.renderMode = RenderMode.WorldSpace;
                m_hudCanvas.worldCamera = Camera.main;

                // Create text component / 创建文本组件
                GameObject textObject = new GameObject("Performance Text");
                textObject.transform.SetParent(m_hudCanvas.transform);
                
                m_performanceText = textObject.AddComponent<UnityEngine.UI.Text>();
                m_performanceText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                m_performanceText.fontSize = 12;
                m_performanceText.color = Color.green;
                m_performanceText.alignment = TextAnchor.UpperLeft;

                var rectTransform = textObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(300, 200);
                rectTransform.localPosition = Vector3.zero;
            }

            UpdateHUDVisibility();
        }

        /// <summary>
        /// 更新HUD显示
        /// </summary>
        private void UpdateHUD()
        {
            if (!m_showPerformanceHUD || m_performanceText == null) return;

            string performanceInfo = GetPerformanceInfoString();
            m_performanceText.text = performanceInfo;

            // Update text color based on performance / 根据性能更新文本颜色
            PerformanceGrade grade = GetPerformanceGrade();
            m_performanceText.color = GetGradeColor(grade);
        }

        /// <summary>
        /// 更新HUD可见性
        /// </summary>
        private void UpdateHUDVisibility()
        {
            if (m_hudCanvas != null)
            {
                m_hudCanvas.gameObject.SetActive(m_showPerformanceHUD);
            }
        }

        #endregion

        #region Auto Optimization / 自动优化

        /// <summary>
        /// 应用自动优化
        /// </summary>
        private void ApplyAutoOptimizations()
        {
            Debug.Log("[VRPerformanceMonitor] 应用自动性能优化...");

            // Example optimizations - these would be more sophisticated in practice
            // 示例优化 - 实际使用中会更复杂

            // Reduce render scale if GPU bound / 如果GPU受限则降低渲染比例
            if (m_activeWarnings.Any(w => w.Type == WarningType.GPUTime))
            {
                float currentRenderScale = XRSettings.renderViewportScale;
                if (currentRenderScale > 0.7f)
                {
                    XRSettings.renderViewportScale = Mathf.Max(0.7f, currentRenderScale - 0.1f);
                    Debug.Log($"[VRPerformanceMonitor] 降低渲染比例到 {XRSettings.renderViewportScale:F2}");
                }
            }

            // Additional optimizations could be added here / 这里可以添加额外的优化
            // - Reduce shadow quality / 降低阴影质量
            // - Disable post-processing effects / 禁用后处理效果
            // - Reduce particle counts / 减少粒子数量
            // - Lower texture quality / 降低纹理质量
        }

        #endregion

        #region Utility Methods / 实用方法

        /// <summary>
        /// 获取性能评级
        /// </summary>
        private PerformanceGrade GetPerformanceGrade()
        {
            float fps = m_currentFPS;
            float targetFps = m_targetFPS;

            if (fps >= targetFps * 0.95f) return PerformanceGrade.Excellent;
            if (fps >= targetFps * 0.85f) return PerformanceGrade.Good;
            if (fps >= targetFps * 0.70f) return PerformanceGrade.Fair;
            if (fps >= targetFps * 0.50f) return PerformanceGrade.Poor;
            return PerformanceGrade.Critical;
        }

        /// <summary>
        /// 获取警告严重程度
        /// </summary>
        private WarningSeverity GetWarningSeverity(float current, float threshold)
        {
            float ratio = current / threshold;
            if (ratio >= 2.0f) return WarningSeverity.Critical;
            if (ratio >= 1.5f) return WarningSeverity.High;
            if (ratio >= 1.2f) return WarningSeverity.Medium;
            return WarningSeverity.Low;
        }

        /// <summary>
        /// 获取评级颜色
        /// </summary>
        private Color GetGradeColor(PerformanceGrade grade)
        {
            switch (grade)
            {
                case PerformanceGrade.Excellent: return Color.green;
                case PerformanceGrade.Good: return Color.yellow;
                case PerformanceGrade.Fair: return new Color(1f, 0.5f, 0f); // Orange
                case PerformanceGrade.Poor: return Color.red;
                case PerformanceGrade.Critical: return Color.magenta;
                default: return Color.white;
            }
        }

        /// <summary>
        /// 获取性能信息字符串
        /// </summary>
        private string GetPerformanceInfoString()
        {
            return $"VR Performance Monitor\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"FPS: {m_currentFPS:F1} / {m_targetFPS}\n" +
                   $"Frame Time: {m_averageFrameTime:F2}ms\n" +
                   $"CPU Time: {m_averageCPUTime:F2}ms\n" +
                   $"GPU Time: {m_averageGPUTime:F2}ms\n" +
                   $"Dropped Frames: {m_droppedFramesCount}\n" +
                   $"Grade: {GetPerformanceGrade()}\n" +
                   $"Device: {m_vrDeviceName}\n" +
                   $"Refresh Rate: {m_vrDisplayRefreshRate}Hz";
        }

        #endregion

        #region Public API / 公共API

        /// <summary>
        /// 切换性能HUD显示
        /// </summary>
        public void TogglePerformanceHUD()
        {
            m_showPerformanceHUD = !m_showPerformanceHUD;
            UpdateHUDVisibility();
        }

        /// <summary>
        /// 设置性能HUD显示状态
        /// </summary>
        public void SetPerformanceHUDVisible(bool visible)
        {
            m_showPerformanceHUD = visible;
            UpdateHUDVisibility();
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return new PerformanceReport
            {
                CurrentFPS = m_currentFPS,
                AverageFrameTime = m_averageFrameTime,
                AverageCPUTime = m_averageCPUTime,
                AverageGPUTime = m_averageGPUTime,
                DroppedFramesCount = m_droppedFramesCount,
                TotalFramesCount = m_totalFramesCount,
                PerformanceGrade = GetPerformanceGrade(),
                ActiveWarnings = new List<PerformanceWarning>(m_activeWarnings),
                IsVRActive = m_isVRActive,
                VRDeviceName = m_vrDeviceName,
                VRRefreshRate = m_vrDisplayRefreshRate
            };
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetStatistics()
        {
            m_frameTimeHistory.Clear();
            m_cpuTimeHistory.Clear();
            m_gpuTimeHistory.Clear();
            m_droppedFramesHistory.Clear();
            m_droppedFramesCount = 0;
            m_totalFramesCount = 0;
            m_activeWarnings.Clear();
            
            Debug.Log("[VRPerformanceMonitor] 性能统计已重置");
        }

        #endregion
    }

    #region Data Structures / 数据结构

    /// <summary>
    /// 性能评级枚举
    /// </summary>
    public enum PerformanceGrade
    {
        Excellent,  // 优秀
        Good,       // 良好
        Fair,       // 一般
        Poor,       // 较差
        Critical    // 严重
    }

    /// <summary>
    /// 警告类型枚举
    /// </summary>
    public enum WarningType
    {
        FrameTime,  // 帧时间
        CPUTime,    // CPU时间
        GPUTime,    // GPU时间
        Memory      // 内存
    }

    /// <summary>
    /// 警告严重程度枚举
    /// </summary>
    public enum WarningSeverity
    {
        Low,        // 低
        Medium,     // 中
        High,       // 高
        Critical    // 严重
    }

    /// <summary>
    /// 性能警告结构
    /// </summary>
    [System.Serializable]
    public struct PerformanceWarning
    {
        public WarningType Type;
        public string Message;
        public WarningSeverity Severity;
    }

    /// <summary>
    /// 性能报告结构
    /// </summary>
    [System.Serializable]
    public struct PerformanceReport
    {
        public float CurrentFPS;
        public float AverageFrameTime;
        public float AverageCPUTime;
        public float AverageGPUTime;
        public int DroppedFramesCount;
        public int TotalFramesCount;
        public PerformanceGrade PerformanceGrade;
        public List<PerformanceWarning> ActiveWarnings;
        public bool IsVRActive;
        public string VRDeviceName;
        public float VRRefreshRate;
    }

    #endregion
}