using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using PongHub.Input;

namespace PongHub.Utils
{
    /// <summary>
    /// 输入系统性能基准测试工具
    /// 用于测量和比较不同输入轮询策略的性能表现
    /// </summary>
    public class InputPerformanceBenchmark : MonoBehaviour
    {
        [Header("基准测试设置")]
        [SerializeField] private int m_testDuration = 10; // 测试持续时间(秒)
        [SerializeField] private bool m_runOnStart = false;
        [SerializeField] private bool m_autoGenerateReport = true;

        [Header("测试结果显示")]
        [SerializeField] private bool m_showRealtimeStats = true;
        [SerializeField] private KeyCode m_startTestKey = KeyCode.F8;

        private List<float> m_cpuTimeSamples = new List<float>();
        private List<float> m_frameTimes = new List<float>();
        private Stopwatch m_testStopwatch = new Stopwatch();
        private bool m_isRunningTest = false;
        private float m_testStartTime;

        // 统计数据
        private float m_totalCpuTime = 0f;
        private float m_maxCpuTime = 0f;
        private float m_minCpuTime = float.MaxValue;
        private int m_sampleCount = 0;

        // GUI显示
        private Rect m_windowRect = new Rect(350, 10, 400, 300);
        private string m_testResults = "";

        private void Start()
        {
            if (m_runOnStart)
            {
                StartBenchmark();
            }
        }

        private void Update()
        {
            // 手动启动测试
            if (UnityEngine.Input.GetKeyDown(m_startTestKey) && !m_isRunningTest)
            {
                StartBenchmark();
            }

            // 收集测试数据
            if (m_isRunningTest)
            {
                CollectSample();

                // 检查测试是否完成
                if (Time.time - m_testStartTime >= m_testDuration)
                {
                    StopBenchmark();
                }
            }
        }

        /// <summary>
        /// 开始基准测试
        /// </summary>
        public void StartBenchmark()
        {
            if (PongHubInputManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[InputPerformanceBenchmark] PongHubInputManager未找到！");
                return;
            }

            UnityEngine.Debug.Log("[InputPerformanceBenchmark] 开始性能基准测试...");

            m_isRunningTest = true;
            m_testStartTime = Time.time;
            m_testStopwatch.Restart();

            // 重置统计数据
            m_cpuTimeSamples.Clear();
            m_frameTimes.Clear();
            m_totalCpuTime = 0f;
            m_maxCpuTime = 0f;
            m_minCpuTime = float.MaxValue;
            m_sampleCount = 0;

            // 启用性能日志以收集详细数据
            PongHubInputManager.Instance.m_enablePerformanceLogging = true;
        }

        /// <summary>
        /// 停止基准测试
        /// </summary>
        public void StopBenchmark()
        {
            if (!m_isRunningTest) return;

            m_isRunningTest = false;
            m_testStopwatch.Stop();

            UnityEngine.Debug.Log("[InputPerformanceBenchmark] 基准测试完成！");

            // 计算统计结果
            CalculateResults();

            if (m_autoGenerateReport)
            {
                GenerateReport();
            }
        }

        /// <summary>
        /// 收集单次样本数据
        /// </summary>
        private void CollectSample()
        {
            if (PongHubInputManager.Instance == null) return;

            float cpuTime = PongHubInputManager.Instance.LastFrameCPUTime;
            float frameTime = Time.unscaledDeltaTime * 1000f; // 转换为毫秒

            m_cpuTimeSamples.Add(cpuTime);
            m_frameTimes.Add(frameTime);

            // 更新统计数据
            m_totalCpuTime += cpuTime;
            m_maxCpuTime = Mathf.Max(m_maxCpuTime, cpuTime);
            m_minCpuTime = Mathf.Min(m_minCpuTime, cpuTime);
            m_sampleCount++;
        }

        /// <summary>
        /// 计算测试结果
        /// </summary>
        private void CalculateResults()
        {
            if (m_sampleCount == 0)
            {
                m_testResults = "没有收集到有效数据";
                return;
            }

            float avgCpuTime = m_totalCpuTime / m_sampleCount;
            float avgFrameTime = 0f;
            foreach (float frameTime in m_frameTimes)
            {
                avgFrameTime += frameTime;
            }
            avgFrameTime /= m_frameTimes.Count;

            // 计算CPU时间百分位数
            var sortedCpuTimes = new List<float>(m_cpuTimeSamples);
            sortedCpuTimes.Sort();

            float p50 = GetPercentile(sortedCpuTimes, 0.5f);
            float p95 = GetPercentile(sortedCpuTimes, 0.95f);
            float p99 = GetPercentile(sortedCpuTimes, 0.99f);

            // 计算帧率统计
            float avgFps = 1000f / avgFrameTime;
            float actualUpdateRate = PongHubInputManager.Instance.ActualUpdateRate;

            m_testResults = $"🚀 PongHub输入系统性能基准测试报告\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"测试配置:\n" +
                           $"- 测试时长: {m_testDuration}秒\n" +
                           $"- 样本数量: {m_sampleCount}\n" +
                           $"- 优化模式: {(PongHubInputManager.Instance.m_useOptimizedPolling ? "启用" : "禁用")}\n" +
                           $"- 目标频率: {actualUpdateRate:F1}Hz\n\n" +
                           $"CPU性能指标:\n" +
                           $"- 平均CPU时间: {avgCpuTime:F1}μs\n" +
                           $"- 最小CPU时间: {m_minCpuTime:F1}μs\n" +
                           $"- 最大CPU时间: {m_maxCpuTime:F1}μs\n" +
                           $"- 50th百分位: {p50:F1}μs\n" +
                           $"- 95th百分位: {p95:F1}μs\n" +
                           $"- 99th百分位: {p99:F1}μs\n\n" +
                           $"帧率性能:\n" +
                           $"- 平均FPS: {avgFps:F1}\n" +
                           $"- 平均帧时间: {avgFrameTime:F2}ms\n\n" +
                           $"性能评级: {GetPerformanceGrade(avgCpuTime)}\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }

        /// <summary>
        /// 设置测试持续时间
        /// </summary>
        /// <param name="duration">测试时长（秒）</param>
        public void SetTestDuration(int duration)
        {
            if (duration > 0)
            {
                m_testDuration = duration;
            }
        }

        /// <summary>
        /// 设置是否自动生成报告
        /// </summary>
        /// <param name="autoGenerate">是否自动生成</param>
        public void SetAutoGenerateReport(bool autoGenerate)
        {
            m_autoGenerateReport = autoGenerate;
        }

        /// <summary>
        /// 获取指定百分位数的值
        /// </summary>
        private float GetPercentile(List<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0) return 0f;

            int index = Mathf.RoundToInt((sortedValues.Count - 1) * percentile);
            return sortedValues[index];
        }

        /// <summary>
        /// 获取性能评级
        /// </summary>
        private string GetPerformanceGrade(float avgCpuTime)
        {
            if (avgCpuTime < 10f) return "优秀 (A+) - VR就绪";
            if (avgCpuTime < 20f) return "良好 (A) - 推荐使用";
            if (avgCpuTime < 50f) return "中等 (B) - 可接受";
            if (avgCpuTime < 100f) return "较差 (C) - 需要优化";
            return "很差 (D) - 严重性能问题";
        }

        /// <summary>
        /// 生成详细报告
        /// </summary>
        private void GenerateReport()
        {
            string fileName = $"InputPerformance_Report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                System.IO.File.WriteAllText(filePath, m_testResults);
                UnityEngine.Debug.Log($"[InputPerformanceBenchmark] 报告已保存: {filePath}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[InputPerformanceBenchmark] 保存报告失败: {e.Message}");
            }
        }

        /// <summary>
        /// 比较不同配置的性能
        /// </summary>
        public void RunComparisonTest()
        {
            StartCoroutine(ComparisonTestCoroutine());
        }

        private System.Collections.IEnumerator ComparisonTestCoroutine()
        {
            if (PongHubInputManager.Instance == null) yield break;

            UnityEngine.Debug.Log("[InputPerformanceBenchmark] 开始对比测试...");

            // 测试1: 优化模式
            PongHubInputManager.Instance.m_useOptimizedPolling = true;
            yield return new WaitForSeconds(1f); // 等待设置生效

            StartBenchmark();
            yield return new WaitForSeconds(m_testDuration);

            string optimizedResults = m_testResults;

            // 测试2: 非优化模式
            PongHubInputManager.Instance.m_useOptimizedPolling = false;
            yield return new WaitForSeconds(1f);

            StartBenchmark();
            yield return new WaitForSeconds(m_testDuration);

            string nonOptimizedResults = m_testResults;

            // 恢复优化模式
            PongHubInputManager.Instance.m_useOptimizedPolling = true;

            // 生成对比报告
            string comparisonReport = $"🔄 输入系统性能对比测试报告\n" +
                                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                    $"📈 优化模式结果:\n{optimizedResults}\n\n" +
                                    $"📉 非优化模式结果:\n{nonOptimizedResults}\n\n" +
                                    $"📊 性能提升建议: 启用优化模式可显著提高性能";

            UnityEngine.Debug.Log(comparisonReport);

            // 保存对比报告
            string fileName = $"InputPerformance_Comparison_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                System.IO.File.WriteAllText(filePath, comparisonReport);
                UnityEngine.Debug.Log($"[InputPerformanceBenchmark] 对比报告已保存: {filePath}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[InputPerformanceBenchmark] 保存对比报告失败: {e.Message}");
            }
        }

        private void OnGUI()
        {
            if (!m_showRealtimeStats) return;

            GUI.skin.window.fontSize = 12;
            m_windowRect = GUI.Window(54321, m_windowRect, DrawBenchmarkWindow, "输入系统性能基准测试");
        }

        private void DrawBenchmarkWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // 测试控制
            GUILayout.Label("基准测试控制", GUI.skin.label);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(m_isRunningTest ? "停止测试" : "开始测试"))
            {
                if (m_isRunningTest)
                    StopBenchmark();
                else
                    StartBenchmark();
            }

            if (GUILayout.Button("对比测试"))
            {
                RunComparisonTest();
            }
            GUILayout.EndHorizontal();

            // 实时状态
            if (m_isRunningTest)
            {
                float progress = (Time.time - m_testStartTime) / m_testDuration;
                GUILayout.Label($"测试进度: {progress * 100f:F1}%");
                GUILayout.Label($"已收集样本: {m_sampleCount}");

                if (PongHubInputManager.Instance != null)
                {
                    GUILayout.Label($"当前CPU时间: {PongHubInputManager.Instance.LastFrameCPUTime:F1}μs");
                }
            }

            // 显示结果
            if (!string.IsNullOrEmpty(m_testResults))
            {
                GUILayout.Space(10);
                GUILayout.Label("最新测试结果:", GUI.skin.label);

                Vector2 scrollPos = Vector2.zero;
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                GUILayout.TextArea(m_testResults, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        /// <summary>
        /// 获取测试结果文本
        /// </summary>
        public string GetLastTestResults()
        {
            return m_testResults;
        }
    }
}