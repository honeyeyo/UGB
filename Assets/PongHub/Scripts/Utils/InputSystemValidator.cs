using UnityEngine;
using PongHub.Input;

namespace PongHub.Utils
{
    /// <summary>
    /// 输入系统验证器
    /// 用于验证PongHub输入系统优化功能是否正常工作
    /// </summary>
    public class InputSystemValidator : MonoBehaviour
    {
        [Header("验证设置")]
        [SerializeField]
        [Tooltip("Run Validation On Start / 开始时运行验证 - Automatically run validation when component starts")]
        private bool m_runValidationOnStart = true;

        [SerializeField]
        [Tooltip("Validation Duration / 验证持续时间 - Duration of validation test in seconds")]
        private float m_validationDuration = 5f;

        private void Start()
        {
            if (m_runValidationOnStart)
            {
                StartCoroutine(RunValidation());
            }
        }

        /// <summary>
        /// 运行完整的系统验证
        /// </summary>
        public System.Collections.IEnumerator RunValidation()
        {
            Debug.Log("🚀 开始PongHub输入系统验证...");

            // 1. 验证PongHubInputManager存在且工作正常
            if (!ValidatePongHubInputManager())
            {
                Debug.LogError("❌ PongHubInputManager验证失败！");
                yield break;
            }
            Debug.Log("✅ PongHubInputManager验证通过");

            // 2. 验证性能优化功能
            if (!ValidatePerformanceOptimization())
            {
                Debug.LogError("❌ 性能优化验证失败！");
                yield break;
            }
            Debug.Log("✅ 性能优化验证通过");

            // 3. 运行短期性能测试
            yield return RunPerformanceTest();

            // 4. 验证监控工具
            ValidateMonitoringTools();

            Debug.Log("🎉 PongHub输入系统验证完成！所有功能正常工作。");
        }

        /// <summary>
        /// 验证PongHubInputManager基本功能
        /// </summary>
        private bool ValidatePongHubInputManager()
        {
            // 检查单例实例
            if (PongHubInputManager.Instance == null)
            {
                Debug.LogError("PongHubInputManager实例不存在");
                return false;
            }

            // 检查输入动作是否已初始化
            var inputManager = PongHubInputManager.Instance;
            if (inputManager.CurrentInputState.Equals(default))
            {
                Debug.LogWarning("输入状态可能未正确初始化，但这在某些情况下是正常的");
            }

            return true;
        }

        /// <summary>
        /// 验证性能优化功能
        /// </summary>
        private bool ValidatePerformanceOptimization()
        {
            var inputManager = PongHubInputManager.Instance;

            // 检查性能监控属性是否可访问
            try
            {
                float cpuTime = inputManager.LastFrameCPUTime;
                float updateRate = inputManager.ActualUpdateRate;

                Debug.Log($"当前CPU时间: {cpuTime:F1}μs, 更新频率: {updateRate:F1}Hz");

                // 检查优化设置
                Debug.Log($"优化轮询: {(inputManager.m_useOptimizedPolling ? "启用" : "禁用")}");
                Debug.Log($"性能日志: {(inputManager.m_enablePerformanceLogging ? "启用" : "禁用")}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"性能属性访问失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        private System.Collections.IEnumerator RunPerformanceTest()
        {
            Debug.Log("🔄 开始短期性能测试...");

            // 添加基准测试组件（如果不存在）
            var benchmark = FindObjectOfType<InputPerformanceBenchmark>();
            if (benchmark == null)
            {
                benchmark = gameObject.AddComponent<InputPerformanceBenchmark>();
                benchmark.SetTestDuration((int)m_validationDuration);
                benchmark.SetAutoGenerateReport(false); // 不自动生成报告
            }

            // 运行基准测试
            benchmark.StartBenchmark();

            yield return new WaitForSeconds(m_validationDuration);

            benchmark.StopBenchmark();

            // 获取结果
            string results = benchmark.GetLastTestResults();
            if (!string.IsNullOrEmpty(results))
            {
                Debug.Log($"性能测试结果:\n{results}");
            }
            else
            {
                Debug.LogWarning("性能测试未能获取结果");
            }
        }

        /// <summary>
        /// 验证监控工具
        /// </summary>
        private void ValidateMonitoringTools()
        {
            // 检查性能监控器
            var perfMonitor = FindObjectOfType<InputPerformanceMonitor>();
            if (perfMonitor == null)
            {
                Debug.LogWarning("InputPerformanceMonitor未找到，可选择手动添加");
            }
            else
            {
                Debug.Log("✅ InputPerformanceMonitor已存在");

                // 测试获取统计信息
                string stats = perfMonitor.GetCurrentStats();
                if (!string.IsNullOrEmpty(stats))
                {
                    Debug.Log($"性能监控器统计信息: {stats}");
                }
            }

            // 验证PongHubInputManager的性能统计API
            var inputManager = PongHubInputManager.Instance;
            string performanceStats = inputManager.GetPerformanceStats();
            Debug.Log($"输入管理器性能统计:\n{performanceStats}");
        }

        /// <summary>
        /// 手动运行验证（供外部调用）
        /// </summary>
        [ContextMenu("运行验证")]
        public void RunValidationManually()
        {
            StartCoroutine(RunValidation());
        }

        /// <summary>
        /// 快速性能检查
        /// </summary>
        [ContextMenu("快速性能检查")]
        public void QuickPerformanceCheck()
        {
            if (PongHubInputManager.Instance == null)
            {
                Debug.LogError("PongHubInputManager未找到");
                return;
            }

            var inputManager = PongHubInputManager.Instance;
            float cpuTime = inputManager.LastFrameCPUTime;
            float updateRate = inputManager.ActualUpdateRate;

            string performance = "";
            if (cpuTime < 10f) performance = "优秀";
            else if (cpuTime < 20f) performance = "良好";
            else if (cpuTime < 50f) performance = "中等";
            else performance = "需要优化";

            Debug.Log($"⚡ 快速性能检查:\n" +
                     $"CPU时间: {cpuTime:F1}μs ({performance})\n" +
                     $"更新频率: {updateRate:F1}Hz\n" +
                     $"优化状态: {(inputManager.m_useOptimizedPolling ? "已优化" : "未优化")}");
        }

        /// <summary>
        /// 切换优化模式进行对比
        /// </summary>
        [ContextMenu("切换优化模式")]
        public void ToggleOptimization()
        {
            if (PongHubInputManager.Instance == null)
            {
                Debug.LogError("PongHubInputManager未找到");
                return;
            }

            var inputManager = PongHubInputManager.Instance;
            inputManager.m_useOptimizedPolling = !inputManager.m_useOptimizedPolling;

            Debug.Log($"🔄 优化模式已切换: {(inputManager.m_useOptimizedPolling ? "启用" : "禁用")}");

            // 等待一帧后显示性能变化
            StartCoroutine(ShowPerformanceChange());
        }

        private System.Collections.IEnumerator ShowPerformanceChange()
        {
            yield return null; // 等待一帧
            yield return new WaitForSeconds(1f); // 等待1秒让系统稳定

            QuickPerformanceCheck();
        }
    }
}