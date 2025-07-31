using UnityEngine;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using PongHub.Input.Performance;
using PongHub.Tests;

namespace PongHub.Tests.Epic3
{
    /// <summary>
    /// AdaptiveInputFrequencyManager单元测试
    /// 验证自适应频率管理器的核心功能和性能特性
    /// </summary>
    [TestFixture]
    public class AdaptiveInputFrequencyManagerTests
    {
        private GameObject testObject;
        private AdaptiveInputFrequencyManager manager;

        [SetUp]
        public void Setup()
        {
            testObject = Epic3TestUtilities.CreateTestGameObject("TestAIFM");
            manager = testObject.AddComponent<AdaptiveInputFrequencyManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Epic3TestUtilities.DestroyTestGameObject(testObject);
        }

        #region TC-AIFM-001: 初始化测试
        
        [Test]
        public void TestInitialization_ValidatesDefaultValues()
        {
            // Arrange & Act - 组件在Setup中已初始化
            
            // Assert - 验证初始状态
            Epic3TestUtilities.AssertInRange(manager.CurrentFrequency, 60f, 360f, 
                "Initial frequency should be within valid range");
            
            Assert.IsTrue(manager.CurrentFrequency > 0, 
                "Current frequency should be positive");
            
            // 验证性能等级初始化
            var stats = manager.GetPerformanceStats();
            Assert.IsNotNull(stats, "Performance stats should be initialized");
            
            Debug.Log($"Initial frequency: {manager.CurrentFrequency:F1}Hz, Grade: {stats.performanceGrade}");
        }

        [Test]
        public void TestInitialization_ValidatesFrequencyInterval()
        {
            // Arrange & Act
            float frequency = manager.CurrentFrequency;
            
            // Assert - 验证频率间隔计算正确
            float expectedInterval = 1f / frequency;
            
            // 通过调用ShouldProcessInput间接验证间隔计算
            bool shouldProcess1 = manager.ShouldProcessInput();
            bool shouldProcess2 = manager.ShouldProcessInput(); // 立即再次调用
            
            // 第一次调用应该返回true（初始状态），第二次应该返回false（间隔未到）
            Assert.IsTrue(shouldProcess1, "First ShouldProcessInput call should return true");
            Assert.IsFalse(shouldProcess2, "Immediate second call should return false due to interval");
        }

        #endregion

        #region TC-AIFM-002: 频率调整测试

        [Test]
        public void TestFrequencyAdjustment_HighPerformance()
        {
            // Arrange - 记录初始频率
            float initialFrequency = manager.CurrentFrequency;
            
            // Act - 模拟高性能环境（低CPU/GPU使用率）
            // 通过私有反射或公共接口模拟性能数据
            var highPerfMode = AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance;
            manager.SetPerformanceMode(highPerfMode);
            
            // Assert - 验证频率向最大值调整
            float adjustedFrequency = manager.CurrentFrequency;
            Assert.IsTrue(adjustedFrequency >= initialFrequency, 
                $"High performance should increase frequency: {initialFrequency:F1}Hz -> {adjustedFrequency:F1}Hz");
            
            Debug.Log($"High performance mode: {initialFrequency:F1}Hz -> {adjustedFrequency:F1}Hz");
        }

        [Test]
        public void TestFrequencyAdjustment_LowPerformance()
        {
            // Arrange - 先设置高频率
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            float highFrequency = manager.CurrentFrequency;
            
            // Act - 切换到省电模式
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.PowerSaving);
            
            // Assert - 验证频率向最小值调整
            float lowFrequency = manager.CurrentFrequency;
            Assert.IsTrue(lowFrequency < highFrequency,
                $"Power saving should decrease frequency: {highFrequency:F1}Hz -> {lowFrequency:F1}Hz");
            
            Debug.Log($"Power saving mode: {highFrequency:F1}Hz -> {lowFrequency:F1}Hz");
        }

        [Test]
        public void TestFrequencyAdjustment_BalancedMode()
        {
            // Arrange - 获取最小和最大频率
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.PowerSaving);
            float minFreq = manager.CurrentFrequency;
            
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            float maxFreq = manager.CurrentFrequency;
            
            // Act - 设置平衡模式
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.Balanced);
            float balancedFreq = manager.CurrentFrequency;
            
            // Assert - 验证平衡频率在中间范围
            Epic3TestUtilities.AssertInRange(balancedFreq, minFreq, maxFreq,
                "Balanced frequency should be between min and max");
            
            // 验证接近中点
            float expectedMidpoint = (minFreq + maxFreq) * 0.5f;
            Epic3TestUtilities.AssertApproximately(expectedMidpoint, balancedFreq, 20f,
                "Balanced frequency should be near midpoint");
            
            Debug.Log($"Balanced mode: {balancedFreq:F1}Hz (range: {minFreq:F1}-{maxFreq:F1}Hz)");
        }

        #endregion

        #region TC-AIFM-003: 性能等级评估测试

        [Test]
        public void TestPerformanceGradeEvaluation_ExcellentGrade()
        {
            // Arrange - 设置高性能模式
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            
            // Act - 等待性能评估更新
            // 在实际测试中，可能需要等待几帧让性能监控生效
            
            // Assert - 验证性能等级
            var stats = manager.GetPerformanceStats();
            
            // 高性能模式下应该获得较好的性能等级
            Assert.IsTrue(stats.performanceGrade <= AdaptiveInputFrequencyManager.PerformanceGrade.Good,
                $"High performance mode should achieve Good or better grade, got: {stats.performanceGrade}");
            
            Debug.Log($"Performance grade in high-perf mode: {stats.performanceGrade}");
        }

        [Test]
        public void TestPerformanceGradeEvaluation_TotalLatencyCalculation()
        {
            // Arrange & Act
            var stats = manager.GetPerformanceStats();
            
            // Assert - 验证总延迟计算逻辑
            Assert.IsTrue(stats.totalLatency > 0, "Total latency should be positive");
            
            // 验证总延迟包含CPU、GPU和输入间隔时间
            float expectedMinLatency = 1000f / 360f; // 最高频率下的最小间隔
            Assert.IsTrue(stats.totalLatency >= expectedMinLatency,
                $"Total latency {stats.totalLatency:F2}ms should be at least {expectedMinLatency:F2}ms");
            
            Debug.Log($"Total latency: {stats.totalLatency:F2}ms " +
                     $"(CPU: {stats.avgCpuTime:F2}ms, GPU: {stats.avgGpuTime:F2}ms, Freq: {stats.currentFrequency:F1}Hz)");
        }

        [Test]
        public void TestPerformanceGradeMapping()
        {
            // 测试性能等级映射逻辑
            var stats = manager.GetPerformanceStats();
            
            // 根据总延迟验证等级映射
            if (stats.totalLatency < 3f)
                Assert.AreEqual(AdaptiveInputFrequencyManager.PerformanceGrade.Excellent, stats.performanceGrade);
            else if (stats.totalLatency < 5f)
                Assert.AreEqual(AdaptiveInputFrequencyManager.PerformanceGrade.Good, stats.performanceGrade);
            else if (stats.totalLatency < 8f)
                Assert.AreEqual(AdaptiveInputFrequencyManager.PerformanceGrade.Average, stats.performanceGrade);
            else if (stats.totalLatency < 12f)
                Assert.AreEqual(AdaptiveInputFrequencyManager.PerformanceGrade.Poor, stats.performanceGrade);
            else
                Assert.AreEqual(AdaptiveInputFrequencyManager.PerformanceGrade.Critical, stats.performanceGrade);
            
            Debug.Log($"Performance grade mapping verified: {stats.totalLatency:F2}ms -> {stats.performanceGrade}");
        }

        #endregion

        #region TC-AIFM-P001: CPU开销测试

        [Test]
        public void TestCPUOverhead_UpdateMethod()
        {
            // Arrange - 让组件运行一段时间稳定化
            for (int i = 0; i < 10; i++)
            {
                manager.GetPerformanceStats(); // 触发内部更新
            }
            
            // Act & Assert - 测量Update方法的CPU开销
            Epic3TestUtilities.AssertPerformance(() =>
            {
                // 模拟Update调用
                manager.GetPerformanceStats();
            }, 0.1f, "AdaptiveInputFrequencyManager Update overhead");
            
            Debug.Log("CPU overhead test passed: Update method < 0.1ms");
        }

        [Test]
        public void TestCPUOverhead_FrequencyAdjustment()
        {
            // Act & Assert - 测量频率调整的CPU开销
            Epic3TestUtilities.AssertPerformance(() =>
            {
                manager.SetInputFrequency(240f);
                manager.SetInputFrequency(120f);
                manager.SetInputFrequency(180f);
            }, 0.05f, "Frequency adjustment overhead");
            
            Debug.Log("CPU overhead test passed: Frequency adjustment < 0.05ms");
        }

        [Test]
        public void TestCPUOverhead_BatchOperations()
        {
            // 测试批量操作的平均开销
            float avgTime = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                manager.ShouldProcessInput();
                var stats = manager.GetPerformanceStats();
            }, 1000);
            
            Assert.IsTrue(avgTime < 0.01f, 
                $"Average operation time {avgTime:F4}ms should be < 0.01ms");
            
            Debug.Log($"Batch operations average time: {avgTime:F4}ms");
        }

        #endregion

        #region TC-AIFM-P002: 内存使用测试

        [Test]
        public void TestMemoryUsage_NoGCInUpdate()
        {
            // Arrange - 稳定化系统
            for (int i = 0; i < 10; i++)
            {
                manager.GetPerformanceStats();
            }
            
            // Act & Assert - 验证Update操作无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    manager.ShouldProcessInput();
                    var stats = manager.GetPerformanceStats();
                }
            }, "AdaptiveInputFrequencyManager Update operations");
            
            Debug.Log("Memory test passed: No GC allocation in Update operations");
        }

        [Test]
        public void TestMemoryUsage_FrequencyAdjustmentNoGC()
        {
            // Act & Assert - 验证频率调整无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                manager.SetInputFrequency(120f);
                manager.SetInputFrequency(240f);
                manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.Balanced);
            }, "Frequency adjustment operations");
            
            Debug.Log("Memory test passed: No GC allocation in frequency adjustments");
        }

        #endregion

        #region 边界条件和异常处理测试

        [Test]
        public void TestBoundaryConditions_FrequencyLimits()
        {
            // Test minimum frequency limit
            manager.SetInputFrequency(0f);
            Assert.IsTrue(manager.CurrentFrequency >= 60f, 
                "Frequency should not go below minimum (60Hz)");
            
            // Test maximum frequency limit
            manager.SetInputFrequency(1000f);
            Assert.IsTrue(manager.CurrentFrequency <= 360f, 
                "Frequency should not exceed maximum (360Hz)");
            
            // Test negative frequency
            manager.SetInputFrequency(-100f);
            Assert.IsTrue(manager.CurrentFrequency >= 60f, 
                "Negative frequency should be clamped to minimum");
            
            Debug.Log($"Boundary test passed: Frequency properly clamped to [{60f}, {360f}]Hz");
        }

        [Test]
        public void TestErrorHandling_InvalidPerformanceMode()
        {
            // Test with all enum values
            foreach (AdaptiveInputFrequencyManager.PerformanceMode mode in 
                     System.Enum.GetValues(typeof(AdaptiveInputFrequencyManager.PerformanceMode)))
            {
                Assert.DoesNotThrow(() => manager.SetPerformanceMode(mode),
                    $"Setting performance mode {mode} should not throw exception");
            }
            
            Debug.Log("Error handling test passed: All performance modes handled correctly");
        }

        [Test]
        public void TestTargetPerformanceMet()
        {
            // Test IsTargetPerformanceMet property
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            
            bool targetMet = manager.IsTargetPerformanceMet;
            var stats = manager.GetPerformanceStats();
            
            bool expectedTargetMet = stats.performanceGrade <= AdaptiveInputFrequencyManager.PerformanceGrade.Good;
            Assert.AreEqual(expectedTargetMet, targetMet,
                "IsTargetPerformanceMet should reflect performance grade");
            
            Debug.Log($"Target performance met: {targetMet} (Grade: {stats.performanceGrade})");
        }

        #endregion

        #region 事件系统测试

        [Test]
        public void TestEventSystem_FrequencyChanged()
        {
            // Arrange
            bool eventFired = false;
            float receivedFrequency = 0f;
            
            manager.OnFrequencyChanged += (frequency) =>
            {
                eventFired = true;
                receivedFrequency = frequency;
            };
            
            // Act
            float testFrequency = 180f;
            manager.SetInputFrequency(testFrequency);
            
            // Assert
            Assert.IsTrue(eventFired, "OnFrequencyChanged event should fire");
            Epic3TestUtilities.AssertApproximately(testFrequency, receivedFrequency, 0.1f,
                "Event should pass correct frequency value");
            
            Debug.Log($"Event test passed: Frequency changed event fired with {receivedFrequency:F1}Hz");
        }

        [Test]
        public void TestEventSystem_PerformanceGradeChanged()
        {
            // Arrange
            bool eventFired = false;
            AdaptiveInputFrequencyManager.PerformanceGrade receivedGrade = 
                AdaptiveInputFrequencyManager.PerformanceGrade.Unknown;
            
            manager.OnPerformanceGradeChanged += (grade) =>
            {
                eventFired = true;
                receivedGrade = grade;
            };
            
            // Act - 触发性能等级变化（通过切换性能模式）
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.PowerSaving);
            manager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            
            // 注意：实际项目中可能需要等待几帧让性能监控检测到变化
            
            Debug.Log($"Performance grade change event test setup completed");
        }

        #endregion

        #region 长时间运行测试

        [UnityTest]
        public IEnumerator TestLongRunning_StabilityTest()
        {
            // 长时间运行稳定性测试
            float startTime = Time.unscaledTime;
            float testDuration = 1f; // 1秒测试（实际项目中可能需要更长）
            
            int frameCount = 0;
            
            while (Time.unscaledTime - startTime < testDuration)
            {
                // 模拟正常使用
                bool shouldProcess = manager.ShouldProcessInput();
                var stats = manager.GetPerformanceStats();
                
                frameCount++;
                yield return null;
            }
            
            // 验证系统仍正常工作
            Assert.IsTrue(manager.CurrentFrequency > 0, "Frequency should remain positive after long run");
            Assert.IsTrue(frameCount > 30, $"Should have processed at least 30 frames, got {frameCount}");
            
            Debug.Log($"Long running test passed: {frameCount} frames processed in {testDuration}s");
        }

        #endregion
    }
}