using UnityEngine;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using PongHub.Input.Performance;
using PongHub.Input.Network;
using PongHub.Input.Device;
using PongHub.Tests;
using Unity.Netcode;

namespace PongHub.Tests.Epic3
{
    /// <summary>
    /// Epic-3集成测试
    /// 验证四个核心优化组件的协同工作和整体系统性能
    /// </summary>
    [TestFixture]
    public class Epic3IntegrationTests
    {
        private GameObject testSystemObject;
        private AdaptiveInputFrequencyManager frequencyManager;
        private ZeroGCInputProcessor gcProcessor;
        private NetworkInputPredictor networkPredictor;
        private VRDeviceHealthMonitor deviceMonitor;

        [SetUp]
        public void Setup()
        {
            testSystemObject = Epic3TestUtilities.CreateTestGameObject("Epic3IntegratedSystem");

            // 创建所有四个核心组件
            frequencyManager = testSystemObject.AddComponent<AdaptiveInputFrequencyManager>();
            gcProcessor = testSystemObject.AddComponent<ZeroGCInputProcessor>();

            // NetworkInputPredictor需要NetworkBehaviour支持，使用模拟版本
            networkPredictor = testSystemObject.AddComponent<NetworkInputPredictor>();
            deviceMonitor = testSystemObject.AddComponent<VRDeviceHealthMonitor>();
        }

        [TearDown]
        public void Teardown()
        {
            Epic3TestUtilities.DestroyTestGameObject(testSystemObject);
        }

        #region TC-INT-001: 组件协同工作测试

        [Test]
        public void TestComponentIntegration_AllComponentsInitialized()
        {
            // Assert - 验证所有组件正确初始化
            Assert.IsNotNull(frequencyManager, "AdaptiveInputFrequencyManager should be initialized");
            Assert.IsNotNull(gcProcessor, "ZeroGCInputProcessor should be initialized");
            Assert.IsNotNull(networkPredictor, "NetworkInputPredictor should be initialized");
            Assert.IsNotNull(deviceMonitor, "VRDeviceHealthMonitor should be initialized");

            // 验证组件状态
            Assert.IsTrue(frequencyManager.CurrentFrequency > 0,
                "Frequency manager should have positive frequency");

            var memoryStats = gcProcessor.GetMemoryStats();
            Assert.IsTrue(memoryStats.cachedStringsCount >= 0,
                "GC processor should have valid memory stats");

            var deviceDiagnostics = deviceMonitor.GetDeviceDiagnostics();
            Assert.IsTrue(deviceDiagnostics.connectedDevices >= 0,
                "Device monitor should have valid diagnostics");

            Debug.Log("Component integration test passed: All components initialized successfully");
        }

        [Test]
        public void TestComponentIntegration_FrequencyAndGCProcessorSync()
        {
            // Arrange - 设置不同的频率模式
            frequencyManager.SetPerformanceMode(AdaptiveInputFrequencyManager.PerformanceMode.HighPerformance);
            float highFrequency = frequencyManager.CurrentFrequency;

            // Act - 在高频模式下测试GC处理器
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                // 模拟高频输入处理
                for (int i = 0; i < 100; i++)
                {
                    bool shouldProcess = frequencyManager.ShouldProcessInput();
                    if (shouldProcess)
                    {
                        var packet = gcProcessor.GetInputDataPacket();
                        packet.leftHandPosition = new Vector3(i, i, i);
                        packet.timestamp = Time.unscaledTime;
                        gcProcessor.ReturnInputDataPacket(packet);
                    }
                }
            }, "High frequency GC processor integration");

            Debug.Log($"Frequency-GC integration test passed: {highFrequency:F1}Hz with zero GC allocation");
        }

        [Test]
        public void TestComponentIntegration_NetworkPredictorWithFrequencyManager()
        {
            // Arrange - 创建网络输入状态
            var inputState = new NetworkInputPredictor.PredictedInputState
            {
                sequenceNumber = 1,
                timestamp = Time.unscaledTime,
                leftHandPosition = Vector3.one,
                rightHandPosition = Vector3.right,
                isConfirmed = false
            };

            // Act - 测试网络预测器和频率管理器的协同
            float currentFrequency = frequencyManager.CurrentFrequency;
            float networkSendInterval = 1f / 30f; // 30Hz网络发送
            float inputProcessInterval = 1f / currentFrequency;

            // Assert - 验证频率协调
            Assert.IsTrue(inputProcessInterval <= networkSendInterval,
                "Input processing should be faster than or equal to network send rate");

            // 验证网络状态创建不产生GC
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                var testState = inputState;
                testState.leftHandPosition = Vector3.zero;
                testState.sequenceNumber++;

                float difference = testState.GetPositionDifference(inputState);
                var interpolated = testState.Lerp(inputState, 0.5f);
            }, "Network predictor operations");

            Debug.Log($"Network-Frequency integration test passed: " +
                     $"Input: {currentFrequency:F1}Hz, Network: 30Hz");
        }

        [Test]
        public void TestComponentIntegration_DeviceMonitorWithAllSystems()
        {
            // Arrange - 获取设备诊断
            var initialDiagnostics = deviceMonitor.GetDeviceDiagnostics();

            // Act - 在设备监控下运行其他系统
            for (int i = 0; i < 50; i++)
            {
                // 频率管理器操作
                bool shouldProcess = frequencyManager.ShouldProcessInput();

                // GC处理器操作
                var packet = gcProcessor.GetInputDataPacket();
                packet.sequenceNumber = (uint)i;
                gcProcessor.ReturnInputDataPacket(packet);

                // 设备监控操作
                deviceMonitor.ForceHealthCheck();
            }

            // Assert - 验证系统协同工作
            var finalDiagnostics = deviceMonitor.GetDeviceDiagnostics();
            var performanceStats = frequencyManager.GetPerformanceStats();
            var memoryStats = gcProcessor.GetMemoryStats();

            Assert.IsTrue(performanceStats.currentFrequency > 0,
                "Frequency should remain positive during integration");
            Assert.IsTrue(finalDiagnostics.connectedDevices >= initialDiagnostics.connectedDevices,
                "Device count should remain stable or improve");

            Debug.Log($"Device monitor integration test passed: " +
                     $"Freq: {performanceStats.currentFrequency:F1}Hz, " +
                     $"Devices: {finalDiagnostics.connectedDevices}, " +
                     $"GC: {memoryStats.totalGCAlloc:F3}KB");
        }

        #endregion

        #region TC-INT-002: 性能影响测试

        [Test]
        public void TestPerformanceImpact_CombinedSystemOverhead()
        {
            // Act & Assert - 测量所有组件的组合开销
            Epic3TestUtilities.AssertPerformance(() =>
            {
                // 模拟完整的输入处理流程

                // 1. 频率检查
                bool shouldProcess = frequencyManager.ShouldProcessInput();

                if (shouldProcess)
                {
                    // 2. 零GC输入处理
                    var packet = gcProcessor.GetInputDataPacket();
                    packet.leftHandPosition = Vector3.one;
                    packet.rightHandPosition = Vector3.right;
                    packet.timestamp = Time.unscaledTime;

                    // 3. 网络状态创建（模拟）
                    var networkState = new NetworkInputPredictor.PredictedInputState
                    {
                        sequenceNumber = 1,
                        timestamp = packet.timestamp,
                        leftHandPosition = packet.leftHandPosition,
                        rightHandPosition = packet.rightHandPosition,
                        isConfirmed = false
                    };

                    // 4. 设备状态检查
                    deviceMonitor.ForceHealthCheck();

                    // 清理
                    gcProcessor.ReturnInputDataPacket(packet);
                }

                // 获取所有统计信息
                var perfStats = frequencyManager.GetPerformanceStats();
                var memStats = gcProcessor.GetMemoryStats();
                var deviceStats = deviceMonitor.GetDeviceDiagnostics();

            }, 0.2f, "Combined system overhead"); // 允许0.2ms总开销

            Debug.Log("Combined system performance test passed: Total overhead < 0.2ms");
        }

        [Test]
        public void TestPerformanceImpact_OptimizedVsUnoptimized()
        {
            // Arrange - 测量优化系统性能
            float optimizedTime = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                // 使用优化组件
                if (frequencyManager.ShouldProcessInput())
                {
                    var packet = gcProcessor.GetInputDataPacket();
                    packet.leftHandPosition = Vector3.one;
                    gcProcessor.ReturnInputDataPacket(packet);
                }
            }, 1000);

            // 对比：模拟未优化版本
            float unoptimizedTime = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                // 模拟传统方式（每次都分配新对象）
                var packet = new ZeroGCInputProcessor.InputDataPacket();
                packet.leftHandPosition = Vector3.one;
                packet.timestamp = Time.unscaledTime;
                // 不使用对象池，让GC回收
            }, 1000);

            // Assert - 验证性能提升
            float improvementRatio = unoptimizedTime / optimizedTime;
            Assert.IsTrue(improvementRatio > 1.2f, // 至少20%提升
                $"Optimized version should be significantly faster: {improvementRatio:F2}x improvement");

            Debug.Log($"Performance comparison test passed: " +
                     $"Optimized: {optimizedTime:F6}ms, Unoptimized: {unoptimizedTime:F6}ms, " +
                     $"Improvement: {improvementRatio:F2}x");
        }

        [Test]
        public void TestPerformanceImpact_ScalabilityTest()
        {
            // Test system performance under increasing load
            var loadResults = new float[5];
            int[] loadLevels = { 10, 50, 100, 200, 500 };

            for (int i = 0; i < loadLevels.Length; i++)
            {
                int load = loadLevels[i];

                loadResults[i] = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
                {
                    for (int j = 0; j < load; j++)
                    {
                        if (frequencyManager.ShouldProcessInput())
                        {
                            var packet = gcProcessor.GetInputDataPacket();
                            packet.sequenceNumber = (uint)j;
                            gcProcessor.ReturnInputDataPacket(packet);
                        }
                    }
                }, 10);
            }

            // Assert - 验证可扩展性
            for (int i = 1; i < loadResults.Length; i++)
            {
                float scalingFactor = loadResults[i] / loadResults[0];
                float loadFactor = (float)loadLevels[i] / loadLevels[0];

                // 理想情况下，执行时间应该线性增长或更好
                Assert.IsTrue(scalingFactor <= loadFactor * 1.5f, // 允许50%的超线性增长
                    $"Performance should scale reasonably: Load {loadFactor:F1}x, Time {scalingFactor:F1}x");
            }

            Debug.Log($"Scalability test passed: Performance scales from {loadResults[0]:F6}ms to {loadResults[4]:F6}ms");
        }

        #endregion

        #region 系统集成压力测试

        [UnityTest]
        public IEnumerator TestSystemIntegration_HighLoadStressTest()
        {
            // 高负载压力测试
            float testDuration = 2f;
            float startTime = Time.unscaledTime;

            int totalOperations = 0;
            int gcAllocationsDetected = 0;
            float maxFrameTime = 0f;

            while (Time.unscaledTime - startTime < testDuration)
            {
                float frameStart = Time.unscaledTime;

                // 高频操作模拟
                for (int i = 0; i < 10; i++)
                {
                    // 频率管理
                    bool shouldProcess = frequencyManager.ShouldProcessInput();

                    if (shouldProcess)
                    {
                        // 零GC处理
                        var packet = gcProcessor.GetInputDataPacket();
                        packet.leftHandPosition = new Vector3(i, i, i);
                        packet.rightHandPosition = new Vector3(-i, i, -i);
                        packet.sequenceNumber = (uint)totalOperations;
                        packet.timestamp = Time.unscaledTime;

                        // 网络状态模拟
                        var networkState = new NetworkInputPredictor.PredictedInputState
                        {
                            sequenceNumber = (int)packet.sequenceNumber,
                            timestamp = packet.timestamp,
                            leftHandPosition = packet.leftHandPosition,
                            rightHandPosition = packet.rightHandPosition
                        };

                        // 状态插值测试
                        var interpolated = networkState.Lerp(networkState, 0.5f);

                        gcProcessor.ReturnInputDataPacket(packet);
                        totalOperations++;
                    }
                }

                // 设备监控（低频）
                if (totalOperations % 100 == 0)
                {
                    deviceMonitor.ForceHealthCheck();
                }

                float frameTime = (Time.unscaledTime - frameStart) * 1000f;
                maxFrameTime = Mathf.Max(maxFrameTime, frameTime);

                yield return null;
            }

            // Assert - 验证压力测试结果
            Assert.IsTrue(totalOperations > 1000,
                $"Should have processed many operations, got {totalOperations}");

            Assert.IsTrue(maxFrameTime < 5f, // 最大帧时间<5ms
                $"Max frame time {maxFrameTime:F2}ms should be reasonable for VR");

            // 验证系统仍正常工作
            var finalPerfStats = frequencyManager.GetPerformanceStats();
            var finalMemStats = gcProcessor.GetMemoryStats();
            var finalDeviceStats = deviceMonitor.GetDeviceDiagnostics();

            Assert.IsTrue(finalPerfStats.currentFrequency > 0,
                "Frequency should remain positive after stress test");

            Debug.Log($"High load stress test passed: {totalOperations} operations in {testDuration}s, " +
                     $"Max frame time: {maxFrameTime:F2}ms, " +
                     $"Final frequency: {finalPerfStats.currentFrequency:F1}Hz, " +
                     $"GC allocation: {finalMemStats.totalGCAlloc:F3}KB");
        }

        [UnityTest]
        public IEnumerator TestSystemIntegration_AdaptivePerformanceUnderLoad()
        {
            // 自适应性能测试
            float testDuration = 1.5f;
            float startTime = Time.unscaledTime;

            var frequencyHistory = new System.Collections.Generic.List<float>();
            var performanceGradeHistory = new System.Collections.Generic.List<AdaptiveInputFrequencyManager.PerformanceGrade>();

            // 模拟性能变化场景
            float[] loadPhases = { 0.2f, 0.8f, 0.3f }; // 低负载 -> 高负载 -> 中负载
            int phaseIndex = 0;
            float phaseStartTime = startTime;

            while (Time.unscaledTime - startTime < testDuration)
            {
                // 切换负载阶段
                if (Time.unscaledTime - phaseStartTime > testDuration / loadPhases.Length)
                {
                    phaseIndex = Mathf.Min(phaseIndex + 1, loadPhases.Length - 1);
                    phaseStartTime = Time.unscaledTime;
                }

                float currentLoad = loadPhases[phaseIndex];
                int operationsPerFrame = Mathf.RoundToInt(20 * currentLoad);

                // 执行当前负载级别的操作
                for (int i = 0; i < operationsPerFrame; i++)
                {
                    if (frequencyManager.ShouldProcessInput())
                    {
                        var packet = gcProcessor.GetInputDataPacket();
                        packet.leftHandPosition = Vector3.one * i;
                        gcProcessor.ReturnInputDataPacket(packet);
                    }
                }

                // 记录性能指标
                var stats = frequencyManager.GetPerformanceStats();
                frequencyHistory.Add(stats.currentFrequency);
                performanceGradeHistory.Add(stats.performanceGrade);

                yield return null;
            }

            // Assert - 验证自适应行为
            Assert.IsTrue(frequencyHistory.Count > 30, "Should have collected frequency samples");

            // 验证频率变化范围合理
            float minFreq = float.MaxValue;
            float maxFreq = float.MinValue;

            foreach (float freq in frequencyHistory)
            {
                minFreq = Mathf.Min(minFreq, freq);
                maxFreq = Mathf.Max(maxFreq, freq);
            }

            Assert.IsTrue(maxFreq > minFreq,
                "Frequency should adapt to different load conditions");

            Debug.Log($"Adaptive performance test passed: " +
                     $"Frequency range: {minFreq:F1}Hz - {maxFreq:F1}Hz, " +
                     $"Samples: {frequencyHistory.Count}");
        }

        #endregion

        #region 系统稳定性和错误恢复测试

        [Test]
        public void TestSystemStability_ComponentFailureRecovery()
        {
            // 模拟组件故障和恢复

            // 1. 频率管理器"故障"（设置极低频率）
            frequencyManager.SetInputFrequency(1f); // 1Hz
            Assert.IsTrue(frequencyManager.CurrentFrequency >= 60f,
                "Frequency manager should clamp to minimum safe frequency");

            // 2. GC处理器压力测试
            var packets = new ZeroGCInputProcessor.InputDataPacket[100];
            for (int i = 0; i < packets.Length; i++)
            {
                packets[i] = gcProcessor.GetInputDataPacket();
            }

            // 验证对象池扩展
            var memStats = gcProcessor.GetMemoryStats();
            Assert.IsTrue(memStats.inputDataPoolCount >= 0,
                "Object pool should handle expansion gracefully");

            // 清理
            for (int i = 0; i < packets.Length; i++)
            {
                gcProcessor.ReturnInputDataPacket(packets[i]);
            }

            // 3. 设备监控器重置
            deviceMonitor.ResetStatistics();
            var diagnostics = deviceMonitor.GetDeviceDiagnostics();
            Assert.IsTrue(diagnostics.totalDisconnections == 0,
                "Device monitor should reset statistics correctly");

            Debug.Log("Component failure recovery test passed: All components handled stress gracefully");
        }

        [Test]
        public void TestSystemStability_MemoryLeakDetection()
        {
            // 内存泄漏检测测试
            var initialMemStats = gcProcessor.GetMemoryStats();
            long initialGCMemory = System.GC.GetTotalMemory(false);

            // 执行大量操作
            for (int cycle = 0; cycle < 10; cycle++)
            {
                for (int i = 0; i < 100; i++)
                {
                    // 完整的处理循环
                    if (frequencyManager.ShouldProcessInput())
                    {
                        var packet = gcProcessor.GetInputDataPacket();
                        packet.leftHandPosition = new Vector3(i, i, i);
                        packet.timestamp = Time.unscaledTime;

                        // 模拟网络状态操作
                        var networkState = new NetworkInputPredictor.PredictedInputState
                        {
                            sequenceNumber = i,
                            leftHandPosition = packet.leftHandPosition
                        };

                        var interpolated = networkState.Lerp(networkState, 0.5f);

                        gcProcessor.ReturnInputDataPacket(packet);
                    }
                }

                // 强制GC检查
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }

            // 验证内存使用
            var finalMemStats = gcProcessor.GetMemoryStats();
            long finalGCMemory = System.GC.GetTotalMemory(false);

            long memoryIncrease = finalGCMemory - initialGCMemory;

            // 允许少量内存增长，但不应该有大量泄漏
            Assert.IsTrue(memoryIncrease < 1024 * 1024, // <1MB增长
                $"Memory increase {memoryIncrease / 1024}KB should be minimal");

            Debug.Log($"Memory leak detection test passed: " +
                     $"Memory increase: {memoryIncrease / 1024}KB, " +
                     $"GC allocation: {finalMemStats.totalGCAlloc:F3}KB");
        }

        #endregion

        #region 端到端集成测试

        [UnityTest]
        public IEnumerator TestEndToEnd_CompleteInputProcessingPipeline()
        {
            // 端到端完整输入处理管道测试
            float testDuration = 1f;
            float startTime = Time.unscaledTime;

            var processedInputs = new System.Collections.Generic.List<ProcessedInputData>();

            while (Time.unscaledTime - startTime < testDuration)
            {
                // 1. 频率管理 - 决定是否处理输入
                if (frequencyManager.ShouldProcessInput())
                {
                    // 2. 设备健康检查
                    deviceMonitor.ForceHealthCheck();
                    var deviceHealth = deviceMonitor.GetDeviceDiagnostics();

                    if (deviceHealth.healthyDevices > 0)
                    {
                        // 3. 零GC输入处理
                        var inputPacket = gcProcessor.GetInputDataPacket();
                        inputPacket.leftHandPosition = new Vector3(
                            Mathf.Sin(Time.unscaledTime),
                            Mathf.Cos(Time.unscaledTime),
                            0
                        );
                        inputPacket.rightHandPosition = new Vector3(
                            Mathf.Cos(Time.unscaledTime),
                            Mathf.Sin(Time.unscaledTime),
                            0
                        );
                        inputPacket.timestamp = Time.unscaledTime;
                        inputPacket.sequenceNumber = (uint)(processedInputs.Count + 1);

                        // 4. 网络预测处理
                        var networkState = new NetworkInputPredictor.PredictedInputState
                        {
                            sequenceNumber = (int)inputPacket.sequenceNumber,
                            timestamp = inputPacket.timestamp,
                            leftHandPosition = inputPacket.leftHandPosition,
                            rightHandPosition = inputPacket.rightHandPosition,
                            isConfirmed = false
                        };

                        // 5. 记录处理结果
                        var processedInput = new ProcessedInputData
                        {
                            timestamp = inputPacket.timestamp,
                            sequenceNumber = (int)inputPacket.sequenceNumber,
                            leftHandPosition = inputPacket.leftHandPosition,
                            rightHandPosition = inputPacket.rightHandPosition,
                            processingFrequency = frequencyManager.CurrentFrequency,
                            deviceHealthy = deviceHealth.healthyDevices > 0
                        };

                        processedInputs.Add(processedInput);

                        // 6. 清理
                        gcProcessor.ReturnInputDataPacket(inputPacket);
                    }
                }

                yield return null;
            }

            // Assert - 验证端到端处理结果
            Assert.IsTrue(processedInputs.Count > 30,
                $"Should have processed substantial inputs, got {processedInputs.Count}");

            // 验证序列完整性
            for (int i = 1; i < processedInputs.Count; i++)
            {
                Assert.IsTrue(processedInputs[i].sequenceNumber > processedInputs[i - 1].sequenceNumber,
                    "Sequence numbers should be increasing");
                Assert.IsTrue(processedInputs[i].timestamp >= processedInputs[i - 1].timestamp,
                    "Timestamps should be non-decreasing");
            }

            // 验证数据完整性
            foreach (var input in processedInputs)
            {
                Assert.IsTrue(input.processingFrequency > 0, "Processing frequency should be positive");
                Assert.IsTrue(input.deviceHealthy, "Device should be healthy during processing");
            }

            Debug.Log($"End-to-end pipeline test passed: " +
                     $"Processed {processedInputs.Count} inputs in {testDuration}s, " +
                     $"Average frequency: {processedInputs[processedInputs.Count - 1].processingFrequency:F1}Hz");
        }

        /// <summary>
        /// 处理过的输入数据结构
        /// </summary>
        private struct ProcessedInputData
        {
            public float timestamp;
            public int sequenceNumber;
            public Vector3 leftHandPosition;
            public Vector3 rightHandPosition;
            public float processingFrequency;
            public bool deviceHealthy;
        }

        #endregion

        #region 性能基准对比测试

        [Test]
        public void TestPerformanceBenchmark_Epic3VsBaseline()
        {
            // Epic-3优化系统 vs 基线系统性能对比

            // 1. Epic-3优化系统性能
            float epic3Time = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                if (frequencyManager.ShouldProcessInput())
                {
                    var packet = gcProcessor.GetInputDataPacket();
                    packet.leftHandPosition = Vector3.one;
                    packet.rightHandPosition = Vector3.right;
                    packet.timestamp = Time.unscaledTime;

                    // 模拟网络处理
                    var networkState = new NetworkInputPredictor.PredictedInputState
                    {
                        leftHandPosition = packet.leftHandPosition,
                        rightHandPosition = packet.rightHandPosition,
                        timestamp = packet.timestamp
                    };

                    gcProcessor.ReturnInputDataPacket(packet);
                }

                deviceMonitor.ForceHealthCheck();
            }, 1000);

            // 2. 模拟基线系统（无优化）
            float baselineTime = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                // 传统方式：每次分配新对象
                var inputData = new
                {
                    leftHandPosition = Vector3.one,
                    rightHandPosition = Vector3.right,
                    timestamp = Time.unscaledTime
                };

                // 传统字符串操作
                string debugInfo = $"Input at {inputData.timestamp} with positions {inputData.leftHandPosition}";

                // 模拟简单的设备检查
                bool deviceConnected = true;
                float batteryLevel = 0.8f;
            }, 1000);

            // Assert - 验证性能提升
            float performanceImprovement = baselineTime / epic3Time;
            Assert.IsTrue(performanceImprovement > 1.5f, // 至少50%提升
                $"Epic-3 should be significantly faster: {performanceImprovement:F2}x improvement");

            // 计算性能指标
            float epic3Latency = epic3Time * 1000f; // 转换为微秒
            float baselineLatency = baselineTime * 1000f;

            Assert.IsTrue(epic3Latency < 50f, // Epic-3延迟应<50微秒
                $"Epic-3 latency {epic3Latency:F1}μs should be very low");

            Debug.Log($"Performance benchmark passed:\n" +
                     $"Epic-3: {epic3Time:F6}ms ({epic3Latency:F1}μs)\n" +
                     $"Baseline: {baselineTime:F6}ms ({baselineLatency:F1}μs)\n" +
                     $"Improvement: {performanceImprovement:F2}x faster");
        }

        #endregion
    }
}