using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace PongHub.Input.Performance
{
    /// <summary>
    /// 自适应输入频率管理器 - Epic-3核心优化组件
    /// 功能：动态调整输入更新频率，实现超低延迟VR输入体验
    /// 目标：延迟<5ms，240Hz+自适应频率
    /// </summary>
    public class AdaptiveInputFrequencyManager : MonoBehaviour
    {
        [Header("频率控制设置")]
        [SerializeField]
        [Tooltip("Minimum Update Frequency / 最小更新频率 - Minimum input update frequency (Hz)")]
        private float m_minFrequency = 60f;

        [SerializeField]
        [Tooltip("Maximum Update Frequency / 最大更新频率 - Maximum input update frequency (Hz)")]
        private float m_maxFrequency = 360f;

        [SerializeField]
        [Tooltip("Target Frame Rate / 目标帧率 - Target application frame rate")]
        private int m_targetFrameRate = 120;

        [Header("性能监控设置")]
        [SerializeField]
        [Tooltip("CPU Budget Threshold / CPU预算阈值 - CPU time budget threshold (ms)")]
        private float m_cpuBudgetThreshold = 4.0f;

        [SerializeField]
        [Tooltip("GPU Budget Threshold / GPU预算阈值 - GPU time budget threshold (ms)")]
        private float m_gpuBudgetThreshold = 6.0f;

        [SerializeField]
        [Tooltip("Performance Sample Size / 性能采样大小 - Number of frames to average for performance")]
        private int m_performanceSampleSize = 60;

        [Header("自适应算法设置")]
        [SerializeField]
        [Tooltip("Frequency Adjustment Speed / 频率调整速度 - Speed of frequency adjustment")]
        private float m_adjustmentSpeed = 0.1f;

        [SerializeField]
        [Tooltip("Stability Threshold / 稳定性阈值 - Performance stability threshold")]
        private float m_stabilityThreshold = 0.05f;

        [Header("调试信息")]
        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        [SerializeField]
        [Tooltip("Debug GUI Position / 调试GUI位置 - Position for debug GUI")]
        private Vector2 m_debugGuiPosition = new Vector2(10, 10);

        // 性能监控
        private ProfilerRecorder m_cpuTimeRecorder;
        private ProfilerRecorder m_gpuTimeRecorder;
        private ProfilerRecorder m_frameTimeRecorder;
        
        // 性能数据队列
        private Queue<float> m_cpuTimes = new Queue<float>();
        private Queue<float> m_gpuTimes = new Queue<float>();
        private Queue<float> m_frameTimes = new Queue<float>();
        
        // 当前状态
        private float m_currentFrequency;
        private float m_currentInterval;
        private float m_lastUpdateTime;
        
        // 性能统计
        private float m_avgCpuTime;
        private float m_avgGpuTime;
        private float m_avgFrameTime;
        private PerformanceGrade m_currentGrade;
        
        // 事件系统
        public System.Action<float> OnFrequencyChanged;
        public System.Action<PerformanceGrade> OnPerformanceGradeChanged;

        /// <summary>
        /// 性能等级枚举
        /// </summary>
        public enum PerformanceGrade
        {
            Unknown,    // 未知状态
            Excellent,  // A+ - 优秀: <3ms 总延迟
            Good,       // A  - 良好: <5ms 总延迟
            Average,    // B  - 中等: <8ms 总延迟
            Poor,       // C  - 较差: <12ms 总延迟
            Critical    // D  - 严重: >12ms 总延迟
        }

        /// <summary>
        /// 当前输入频率 (Hz)
        /// </summary>
        public float CurrentFrequency => m_currentFrequency;

        /// <summary>
        /// 当前性能等级
        /// </summary>
        public PerformanceGrade CurrentGrade => m_currentGrade;

        /// <summary>
        /// 是否达到目标性能
        /// </summary>
        public bool IsTargetPerformanceMet => m_currentGrade <= PerformanceGrade.Good;

        private void Awake()
        {
            // 初始化频率设置
            m_currentFrequency = Mathf.Clamp(m_targetFrameRate * 2f, m_minFrequency, m_maxFrequency);
            m_currentInterval = 1f / m_currentFrequency;
            
            // 设置应用程序目标帧率
            Application.targetFrameRate = m_targetFrameRate;
            
            // 启用性能分析器
            EnableProfilerRecorders();
        }

        private void Start()
        {
            // 预热性能采样
            for (int i = 0; i < m_performanceSampleSize; i++)
            {
                m_cpuTimes.Enqueue(0f);
                m_gpuTimes.Enqueue(0f);
                m_frameTimes.Enqueue(0f);
            }

            if (m_showDebugInfo)
            {
                Debug.Log($"[AdaptiveInputFrequencyManager] 初始化完成 - 起始频率: {m_currentFrequency:F1}Hz");
            }
        }

        private void Update()
        {
            // 收集性能数据
            CollectPerformanceData();
            
            // 计算平均性能
            CalculateAveragePerformance();
            
            // 评估性能等级
            EvaluatePerformanceGrade();
            
            // 自适应调整频率
            AdaptiveFrequencyAdjustment();
        }

        private void OnDestroy()
        {
            DisableProfilerRecorders();
        }

        /// <summary>
        /// 启用性能分析器记录器
        /// </summary>
        private void EnableProfilerRecorders()
        {
            m_cpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            m_gpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 15);
            m_frameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 15);
        }

        /// <summary>
        /// 禁用性能分析器记录器
        /// </summary>
        private void DisableProfilerRecorders()
        {
            m_cpuTimeRecorder.Dispose();
            m_gpuTimeRecorder.Dispose();
            m_frameTimeRecorder.Dispose();
        }

        /// <summary>
        /// 收集当前帧性能数据
        /// </summary>
        private void CollectPerformanceData()
        {
            // 获取CPU时间（纳秒转毫秒）
            float cpuTime = 0f;
            if (m_cpuTimeRecorder.Valid && m_cpuTimeRecorder.Count > 0)
            {
                cpuTime = (float)(m_cpuTimeRecorder.LastValue / 1e6);
            }

            // 获取GPU时间（纳秒转毫秒）
            float gpuTime = 0f;
            if (m_gpuTimeRecorder.Valid && m_gpuTimeRecorder.Count > 0)
            {
                gpuTime = (float)(m_gpuTimeRecorder.LastValue / 1e6);
            }

            // 获取总帧时间（纳秒转毫秒）
            float frameTime = Time.unscaledDeltaTime * 1000f;

            // 更新性能数据队列
            if (m_cpuTimes.Count >= m_performanceSampleSize)
            {
                m_cpuTimes.Dequeue();
                m_gpuTimes.Dequeue();
                m_frameTimes.Dequeue();
            }

            m_cpuTimes.Enqueue(cpuTime);
            m_gpuTimes.Enqueue(gpuTime);
            m_frameTimes.Enqueue(frameTime);
        }

        /// <summary>
        /// 计算平均性能指标
        /// </summary>
        private void CalculateAveragePerformance()
        {
            if (m_cpuTimes.Count == 0) return;

            m_avgCpuTime = m_cpuTimes.Average();
            m_avgGpuTime = m_gpuTimes.Average();
            m_avgFrameTime = m_frameTimes.Average();
        }

        /// <summary>
        /// 评估当前性能等级
        /// </summary>
        private void EvaluatePerformanceGrade()
        {
            float totalLatency = m_avgCpuTime + m_avgGpuTime + (1000f / m_currentFrequency);
            
            PerformanceGrade newGrade;
            
            if (totalLatency < 3f)
                newGrade = PerformanceGrade.Excellent;
            else if (totalLatency < 5f)
                newGrade = PerformanceGrade.Good;
            else if (totalLatency < 8f)
                newGrade = PerformanceGrade.Average;
            else if (totalLatency < 12f)
                newGrade = PerformanceGrade.Poor;
            else
                newGrade = PerformanceGrade.Critical;

            if (newGrade != m_currentGrade)
            {
                m_currentGrade = newGrade;
                OnPerformanceGradeChanged?.Invoke(m_currentGrade);
                
                if (m_showDebugInfo)
                {
                    Debug.Log($"[AdaptiveInputFrequencyManager] 性能等级变更: {m_currentGrade} (总延迟: {totalLatency:F2}ms)");
                }
            }
        }

        /// <summary>
        /// 自适应频率调整算法
        /// </summary>
        private void AdaptiveFrequencyAdjustment()
        {
            float targetFrequency = CalculateOptimalFrequency();
            
            // 平滑调整频率，避免频繁跳跃
            float frequencyDelta = (targetFrequency - m_currentFrequency) * m_adjustmentSpeed * Time.unscaledDeltaTime;
            
            // 只有变化足够大时才调整
            if (Mathf.Abs(frequencyDelta) > m_stabilityThreshold)
            {
                float newFrequency = Mathf.Clamp(m_currentFrequency + frequencyDelta, m_minFrequency, m_maxFrequency);
                
                if (Mathf.Abs(newFrequency - m_currentFrequency) > 0.1f)
                {
                    SetInputFrequency(newFrequency);
                }
            }
        }

        /// <summary>
        /// 计算最优输入频率
        /// </summary>
        private float CalculateOptimalFrequency()
        {
            // 基础频率计算：基于当前帧率
            float baseFrequency = 1000f / Mathf.Max(m_avgFrameTime, 1f);
            
            // 性能因子：CPU和GPU使用率
            float cpuFactor = Mathf.Clamp01(1f - (m_avgCpuTime / m_cpuBudgetThreshold));
            float gpuFactor = Mathf.Clamp01(1f - (m_avgGpuTime / m_gpuBudgetThreshold));
            float performanceFactor = Mathf.Min(cpuFactor, gpuFactor);
            
            // 动态频率计算
            float targetFrequency = baseFrequency * 2f * performanceFactor;
            
            // VR特殊优化：保证最小90Hz输入频率
            if (Application.targetFrameRate >= 90)
            {
                targetFrequency = Mathf.Max(targetFrequency, 90f);
            }
            
            return Mathf.Clamp(targetFrequency, m_minFrequency, m_maxFrequency);
        }

        /// <summary>
        /// 设置新的输入频率
        /// </summary>
        public void SetInputFrequency(float newFrequency)
        {
            newFrequency = Mathf.Clamp(newFrequency, m_minFrequency, m_maxFrequency);
            
            if (Mathf.Abs(newFrequency - m_currentFrequency) > 0.1f)
            {
                m_currentFrequency = newFrequency;
                m_currentInterval = 1f / m_currentFrequency;
                
                OnFrequencyChanged?.Invoke(m_currentFrequency);
                
                if (m_showDebugInfo)
                {
                    Debug.Log($"[AdaptiveInputFrequencyManager] 频率调整: {m_currentFrequency:F1}Hz (间隔: {m_currentInterval*1000:F2}ms)");
                }
            }
        }

        /// <summary>
        /// 检查是否应该处理输入更新
        /// </summary>
        public bool ShouldProcessInput()
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - m_lastUpdateTime >= m_currentInterval)
            {
                m_lastUpdateTime = currentTime;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 强制设置性能模式
        /// </summary>
        public void SetPerformanceMode(PerformanceMode mode)
        {
            switch (mode)
            {
                case PerformanceMode.HighPerformance:
                    SetInputFrequency(m_maxFrequency);
                    break;
                case PerformanceMode.Balanced:
                    SetInputFrequency((m_minFrequency + m_maxFrequency) * 0.5f);
                    break;
                case PerformanceMode.PowerSaving:
                    SetInputFrequency(m_minFrequency);
                    break;
            }
        }

        /// <summary>
        /// 性能模式枚举
        /// </summary>
        public enum PerformanceMode
        {
            HighPerformance,
            Balanced,
            PowerSaving
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                currentFrequency = m_currentFrequency,
                avgCpuTime = m_avgCpuTime,
                avgGpuTime = m_avgGpuTime,
                avgFrameTime = m_avgFrameTime,
                totalLatency = m_avgCpuTime + m_avgGpuTime + (1000f / m_currentFrequency),
                performanceGrade = m_currentGrade
            };
        }

        /// <summary>
        /// 性能统计结构
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public float currentFrequency;
            public float avgCpuTime;
            public float avgGpuTime;
            public float avgFrameTime;
            public float totalLatency;
            public PerformanceGrade performanceGrade;
        }

        private void OnGUI()
        {
            if (!m_showDebugInfo) return;

            var stats = GetPerformanceStats();
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };

            string debugText = $"=== 自适应输入频率管理器 ===\n" +
                             $"当前频率: {stats.currentFrequency:F1} Hz\n" +
                             $"输入间隔: {m_currentInterval*1000:F2} ms\n" +
                             $"性能等级: {stats.performanceGrade}\n" +
                             $"总延迟: {stats.totalLatency:F2} ms\n" +
                             $"CPU时间: {stats.avgCpuTime:F2} ms\n" +
                             $"GPU时间: {stats.avgGpuTime:F2} ms\n" +
                             $"帧时间: {stats.avgFrameTime:F2} ms\n" +
                             $"目标达成: {(IsTargetPerformanceMet ? "是" : "否")}";

            GUI.Box(new Rect(m_debugGuiPosition.x, m_debugGuiPosition.y, 250, 200), debugText, style);
        }
    }
}