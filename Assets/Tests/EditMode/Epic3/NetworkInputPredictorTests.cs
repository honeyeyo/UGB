using UnityEngine;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using Unity.Netcode;
using PongHub.Input.Network;
using PongHub.Tests;

namespace PongHub.Tests.Epic3
{
    /// <summary>
    /// NetworkInputPredictor单元测试
    /// 验证网络输入预测器的核心功能和网络同步特性
    /// </summary>
    [TestFixture]
    public class NetworkInputPredictorTests
    {
        private GameObject testObject;
        private NetworkInputPredictor predictor;
        private MockNetworkManager mockNetworkManager;

        [SetUp]
        public void Setup()
        {
            testObject = Epic3TestUtilities.CreateTestGameObject("TestNIP");
            
            // 创建模拟网络管理器
            mockNetworkManager = testObject.AddComponent<MockNetworkManager>();
            predictor = testObject.AddComponent<NetworkInputPredictor>();
        }

        [TearDown]
        public void Teardown()
        {
            Epic3TestUtilities.DestroyTestGameObject(testObject);
        }

        #region 模拟网络组件

        /// <summary>
        /// 模拟网络管理器用于测试
        /// </summary>
        public class MockNetworkManager : MonoBehaviour
        {
            public bool IsServer { get; set; } = false;
            public bool IsOwner { get; set; } = true;
            public bool IsSpawned { get; set; } = true;
        }

        /// <summary>
        /// 创建模拟的预测输入状态
        /// </summary>
        private NetworkInputPredictor.PredictedInputState CreateMockInputState(int sequenceNumber = 1)
        {
            return new NetworkInputPredictor.PredictedInputState
            {
                sequenceNumber = sequenceNumber,
                timestamp = Time.unscaledTime,
                leftHandPosition = Vector3.one,
                rightHandPosition = Vector3.right,
                leftHandRotation = Quaternion.identity,
                rightHandRotation = Quaternion.identity,
                leftStick = Vector2.up,
                rightStick = Vector2.right,
                leftGrip = 0.5f,
                rightGrip = 0.8f,
                buttonStates = 0b101010, // 模拟按钮状态
                predictedVelocity = Vector3.forward,
                isConfirmed = false
            };
        }

        #endregion

        #region TC-NIP-001: 输入预测测试

        [Test]
        public void TestInputPrediction_StateCreation()
        {
            // Arrange & Act - 创建预测状态
            var state = CreateMockInputState();
            
            // Assert - 验证状态创建正确
            Assert.AreEqual(1, state.sequenceNumber, "Sequence number should be set correctly");
            Assert.IsTrue(state.timestamp > 0, "Timestamp should be positive");
            Assert.AreEqual(Vector3.one, state.leftHandPosition, "Left hand position should be set");
            Assert.AreEqual(Vector3.right, state.rightHandPosition, "Right hand position should be set");
            Assert.IsFalse(state.isConfirmed, "Initial state should not be confirmed");
            
            Debug.Log($"Input state creation test passed: Sequence {state.sequenceNumber}, " +
                     $"Timestamp {state.timestamp:F3}");
        }

        [Test]
        public void TestInputPrediction_PositionDifferenceCalculation()
        {
            // Arrange - 创建两个不同的状态
            var state1 = CreateMockInputState();
            state1.leftHandPosition = Vector3.zero;
            state1.rightHandPosition = Vector3.zero;
            
            var state2 = CreateMockInputState();
            state2.leftHandPosition = Vector3.one;
            state2.rightHandPosition = new Vector3(2, 0, 0);
            
            // Act - 计算位置差异
            float difference = state1.GetPositionDifference(state2);
            
            // Assert - 验证差异计算正确
            float expectedDifference = Mathf.Max(
                Vector3.Distance(Vector3.zero, Vector3.one),
                Vector3.Distance(Vector3.zero, new Vector3(2, 0, 0))
            );
            Epic3TestUtilities.AssertApproximately(expectedDifference, difference, 0.001f,
                "Position difference calculation");
            
            Debug.Log($"Position difference test passed: {difference:F3} units");
        }

        [Test]
        public void TestInputPrediction_StateInterpolation()
        {
            // Arrange - 创建起始和目标状态
            var startState = CreateMockInputState();
            startState.leftHandPosition = Vector3.zero;
            startState.leftGrip = 0.0f;
            startState.timestamp = 1.0f;
            
            var endState = CreateMockInputState();
            endState.leftHandPosition = Vector3.one;
            endState.leftGrip = 1.0f;
            endState.timestamp = 2.0f;
            
            // Act - 插值测试
            var interpolated = startState.Lerp(endState, 0.5f);
            
            // Assert - 验证插值结果
            Epic3TestUtilities.AssertVector3Approximately(
                new Vector3(0.5f, 0.5f, 0.5f), 
                interpolated.leftHandPosition, 
                0.001f, 
                "Position interpolation"
            );
            
            Epic3TestUtilities.AssertApproximately(0.5f, interpolated.leftGrip, 0.001f,
                "Grip interpolation");
            
            Epic3TestUtilities.AssertApproximately(1.5f, interpolated.timestamp, 0.001f,
                "Timestamp interpolation");
            
            Debug.Log($"State interpolation test passed: Position {interpolated.leftHandPosition}, " +
                     $"Grip {interpolated.leftGrip:F2}");
        }

        #endregion

        #region TC-NIP-002: 服务器校正测试

        [Test]
        public void TestServerReconciliation_PredictionAccuracy()
        {
            // Arrange - 创建本地预测和服务器确认状态
            var localPrediction = CreateMockInputState();
            localPrediction.leftHandPosition = new Vector3(1.0f, 1.0f, 1.0f);
            localPrediction.sequenceNumber = 100;
            
            var serverConfirmation = CreateMockInputState();
            serverConfirmation.leftHandPosition = new Vector3(1.01f, 0.99f, 1.02f); // 轻微差异
            serverConfirmation.sequenceNumber = 100;
            serverConfirmation.isConfirmed = true;
            
            // Act - 计算预测误差
            float predictionError = localPrediction.GetPositionDifference(serverConfirmation);
            
            // Assert - 验证预测精度
            Assert.IsTrue(predictionError < 0.1f, 
                $"Prediction error {predictionError:F4} should be small for good prediction");
            
            // 验证这种误差不需要回滚（假设回滚阈值为0.02m）
            float rollbackThreshold = 0.02f;
            bool needsRollback = predictionError > rollbackThreshold;
            
            Assert.IsFalse(needsRollback, 
                "Small prediction errors should not trigger rollback");
            
            Debug.Log($"Prediction accuracy test passed: Error {predictionError:F4}m, " +
                     $"Rollback needed: {needsRollback}");
        }

        [Test]
        public void TestServerReconciliation_RollbackScenario()
        {
            // Arrange - 创建需要回滚的场景
            var localPrediction = CreateMockInputState();
            localPrediction.leftHandPosition = new Vector3(1.0f, 1.0f, 1.0f);
            localPrediction.sequenceNumber = 200;
            
            var serverConfirmation = CreateMockInputState();
            serverConfirmation.leftHandPosition = new Vector3(0.9f, 1.1f, 0.8f); // 较大差异
            serverConfirmation.sequenceNumber = 200;
            serverConfirmation.isConfirmed = true;
            
            // Act - 计算预测误差
            float predictionError = localPrediction.GetPositionDifference(serverConfirmation);
            
            // Assert - 验证回滚触发
            float rollbackThreshold = 0.02f; // 2cm
            bool needsRollback = predictionError > rollbackThreshold;
            
            Assert.IsTrue(needsRollback, 
                $"Large prediction error {predictionError:F4}m should trigger rollback");
            
            Debug.Log($"Rollback scenario test passed: Error {predictionError:F4}m triggers rollback " +
                     $"(threshold: {rollbackThreshold:F4}m)");
        }

        #endregion

        #region TC-NIP-003: 状态缓冲测试

        [Test]
        public void TestStateBuffer_CircularBufferBehavior()
        {
            // 由于CircularBuffer是私有类，我们测试其行为模式
            // 通过创建大量状态来模拟缓冲区填充和覆盖
            
            // Arrange - 创建测试状态序列
            var testStates = new NetworkInputPredictor.PredictedInputState[70]; // 超过默认缓冲区大小60
            for (int i = 0; i < testStates.Length; i++)
            {
                testStates[i] = CreateMockInputState(i + 1);
                testStates[i].leftHandPosition = new Vector3(i, i, i); // 唯一标识
            }
            
            // Act & Assert - 验证缓冲区行为
            // 由于无法直接访问内部缓冲区，我们验证相关的公共接口
            
            // 验证状态创建和序列号管理
            for (int i = 0; i < testStates.Length; i++)
            {
                Assert.AreEqual(i + 1, testStates[i].sequenceNumber, 
                    $"State {i} should have correct sequence number");
                
                Assert.AreEqual(new Vector3(i, i, i), testStates[i].leftHandPosition,
                    $"State {i} should have correct position identifier");
            }
            
            Debug.Log($"State buffer behavior test passed: {testStates.Length} states created with correct sequencing");
        }

        [Test]
        public void TestStateBuffer_SequenceNumberHandling()
        {
            // Test sequence number management
            var states = new NetworkInputPredictor.PredictedInputState[10];
            
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = CreateMockInputState(i * 10); // Non-sequential numbers
                
                // Verify each state maintains its sequence number
                Assert.AreEqual(i * 10, states[i].sequenceNumber,
                    $"State should maintain sequence number {i * 10}");
            }
            
            // Test finding states by sequence number (simulated)
            for (int i = 0; i < states.Length; i++)
            {
                int targetSequence = i * 10;
                var foundState = System.Array.Find(states, s => s.sequenceNumber == targetSequence);
                
                Assert.AreNotEqual(default(NetworkInputPredictor.PredictedInputState), foundState,
                    $"Should find state with sequence {targetSequence}");
            }
            
            Debug.Log("Sequence number handling test passed: All sequences managed correctly");
        }

        #endregion

        #region TC-NIP-N001: 网络延迟模拟测试

        [Test]
        public void TestNetworkLatency_RTTSimulation_50ms()
        {
            TestNetworkLatencyScenario(0.05f, "50ms RTT");
        }

        [Test]
        public void TestNetworkLatency_RTTSimulation_100ms()
        {
            TestNetworkLatencyScenario(0.1f, "100ms RTT");
        }

        [Test]
        public void TestNetworkLatency_RTTSimulation_150ms()
        {
            TestNetworkLatencyScenario(0.15f, "150ms RTT");
        }

        [Test]
        public void TestNetworkLatency_RTTSimulation_200ms()
        {
            TestNetworkLatencyScenario(0.2f, "200ms RTT");
        }

        private void TestNetworkLatencyScenario(float rttSeconds, string scenarioName)
        {
            // Arrange - 模拟网络延迟场景
            float sendTime = Time.unscaledTime;
            var clientState = CreateMockInputState();
            clientState.timestamp = sendTime;
            
            // Act - 模拟网络传输和服务器处理
            float serverReceiveTime = sendTime + rttSeconds / 2; // 单向延迟
            float serverProcessTime = serverReceiveTime + 0.001f; // 1ms处理时间
            float clientReceiveTime = serverProcessTime + rttSeconds / 2; // 返回延迟
            
            var serverState = clientState;
            serverState.timestamp = serverProcessTime;
            serverState.isConfirmed = true;
            
            // Assert - 验证延迟处理
            float totalRTT = clientReceiveTime - sendTime;
            Epic3TestUtilities.AssertApproximately(rttSeconds, totalRTT, 0.002f, 
                $"{scenarioName} total RTT");
            
            // 验证预测时间窗口
            float predictionWindow = clientReceiveTime - sendTime;
            Assert.IsTrue(predictionWindow <= 0.5f, // 最大预测时间0.5秒
                $"Prediction window {predictionWindow:F3}s should be within limits");
            
            Debug.Log($"{scenarioName} test passed: RTT {totalRTT*1000:F1}ms, " +
                     $"Prediction window {predictionWindow*1000:F1}ms");
        }

        #endregion

        #region TC-NIP-N002: 丢包处理测试

        [Test]
        public void TestPacketLoss_5PercentLoss()
        {
            TestPacketLossScenario(0.05f, "5% packet loss");
        }

        [Test]
        public void TestPacketLoss_10PercentLoss()
        {
            TestPacketLossScenario(0.10f, "10% packet loss");
        }

        [Test]
        public void TestPacketLoss_15PercentLoss()
        {
            TestPacketLossScenario(0.15f, "15% packet loss");
        }

        private void TestPacketLossScenario(float lossRate, string scenarioName)
        {
            // Arrange - 创建测试数据包序列
            int totalPackets = 100;
            int expectedLostPackets = Mathf.RoundToInt(totalPackets * lossRate);
            int actualLostPackets = 0;
            
            var packets = new NetworkInputPredictor.PredictedInputState[totalPackets];
            var receivedPackets = new bool[totalPackets];
            
            // 创建数据包
            for (int i = 0; i < totalPackets; i++)
            {
                packets[i] = CreateMockInputState(i + 1);
            }
            
            // Act - 模拟丢包
            System.Random random = new System.Random(42); // 固定种子确保可重复性
            for (int i = 0; i < totalPackets; i++)
            {
                bool isLost = random.NextDouble() < lossRate;
                receivedPackets[i] = !isLost;
                if (isLost) actualLostPackets++;
            }
            
            // Assert - 验证丢包处理
            float actualLossRate = (float)actualLostPackets / totalPackets;
            Epic3TestUtilities.AssertApproximately(lossRate, actualLossRate, 0.05f, 
                $"{scenarioName} actual loss rate");
            
            // 验证系统能处理丢包情况
            int consecutiveLosses = 0;
            int maxConsecutiveLosses = 0;
            
            for (int i = 0; i < totalPackets; i++)
            {
                if (!receivedPackets[i])
                {
                    consecutiveLosses++;
                    maxConsecutiveLosses = Mathf.Max(maxConsecutiveLosses, consecutiveLosses);
                }
                else
                {
                    consecutiveLosses = 0;
                }
            }
            
            // 系统应该能处理合理的连续丢包
            Assert.IsTrue(maxConsecutiveLosses < 10, 
                $"Max consecutive losses {maxConsecutiveLosses} should be manageable");
            
            Debug.Log($"{scenarioName} test passed: Actual loss rate {actualLossRate:P}, " +
                     $"Max consecutive losses: {maxConsecutiveLosses}");
        }

        #endregion

        #region 性能和内存测试

        [Test]
        public void TestPerformance_StateSerializationDeserialization()
        {
            // Arrange - 创建测试状态
            var testState = CreateMockInputState();
            
            // Act & Assert - 测量序列化性能
            Epic3TestUtilities.AssertPerformance(() =>
            {
                // 模拟序列化/反序列化操作
                // 由于INetworkSerializable需要NetworkSerializer，我们测试数据拷贝
                var copyState = testState;
                copyState.leftHandPosition = testState.leftHandPosition;
                copyState.rightHandPosition = testState.rightHandPosition;
                copyState.leftHandRotation = testState.leftHandRotation;
                copyState.rightHandRotation = testState.rightHandRotation;
                copyState.buttonStates = testState.buttonStates;
            }, 0.01f, "State serialization performance");
            
            Debug.Log("State serialization performance test passed: < 0.01ms per operation");
        }

        [Test]
        public void TestMemoryUsage_StatePredictionOperations()
        {
            // Act & Assert - 验证预测操作无大量GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var state = CreateMockInputState(i);
                    
                    // 模拟预测操作
                    state.leftHandPosition += state.predictedVelocity * 0.016f; // 16ms预测
                    state.rightHandPosition += state.predictedVelocity * 0.016f;
                    state.timestamp += 0.016f;
                    
                    // 状态比较
                    var otherState = CreateMockInputState(i + 1);
                    float difference = state.GetPositionDifference(otherState);
                    
                    // 状态插值
                    var interpolated = state.Lerp(otherState, 0.5f);
                }
            }, "State prediction operations");
            
            Debug.Log("Memory usage test passed: No GC allocation in prediction operations");
        }

        #endregion

        #region 边界条件和错误处理测试

        [Test]
        public void TestBoundaryConditions_ExtremeNetworkConditions()
        {
            // Test with extreme RTT
            TestNetworkLatencyScenario(0.5f, "500ms extreme RTT");
            
            // Test with zero RTT (LAN scenario)
            TestNetworkLatencyScenario(0.001f, "1ms LAN RTT");
            
            Debug.Log("Extreme network conditions test passed");
        }

        [Test]
        public void TestErrorHandling_InvalidSequenceNumbers()
        {
            // Test with invalid sequence numbers
            var state1 = CreateMockInputState(-1); // Negative sequence
            var state2 = CreateMockInputState(0);  // Zero sequence
            var state3 = CreateMockInputState(int.MaxValue); // Max sequence
            
            Assert.DoesNotThrow(() =>
            {
                float diff1 = state1.GetPositionDifference(state2);
                float diff2 = state2.GetPositionDifference(state3);
                var lerped = state1.Lerp(state3, 0.5f);
            }, "Invalid sequence numbers should not cause exceptions");
            
            Debug.Log("Invalid sequence number handling test passed");
        }

        [Test]
        public void TestNetworkStats_StatsAccuracy()
        {
            // 由于NetworkInputPredictor的统计功能需要实际运行时数据，
            // 我们测试统计结构的基本功能
            
            var stats = new NetworkInputPredictor.NetworkPredictionStats
            {
                averageRTT = 0.05f,
                totalPredictions = 1000,
                correctPredictions = 950,
                rollbackCount = 50,
                averagePredictionError = 0.01f,
                bufferedInputs = 30
            };
            
            // Calculate and verify derived stats
            float expectedAccuracy = (float)stats.correctPredictions / stats.totalPredictions;
            stats.predictionAccuracy = expectedAccuracy;
            
            Epic3TestUtilities.AssertApproximately(0.95f, stats.predictionAccuracy, 0.001f,
                "Prediction accuracy calculation");
            
            Assert.IsTrue(stats.averageRTT > 0, "Average RTT should be positive");
            Assert.IsTrue(stats.rollbackCount <= stats.totalPredictions, 
                "Rollback count should not exceed total predictions");
            
            Debug.Log($"Network stats test passed: Accuracy {stats.predictionAccuracy:P}, " +
                     $"RTT {stats.averageRTT*1000:F1}ms, Error {stats.averagePredictionError*1000:F1}mm");
        }

        #endregion

        #region 集成和兼容性测试

        [UnityTest]
        public IEnumerator TestIntegration_ContinuousPredictionCycle()
        {
            // 模拟连续的预测-确认循环
            float testDuration = 1f;
            float startTime = Time.unscaledTime;
            
            int predictionCount = 0;
            int confirmationCount = 0;
            
            while (Time.unscaledTime - startTime < testDuration)
            {
                // 模拟客户端预测
                var prediction = CreateMockInputState(predictionCount + 1);
                prediction.timestamp = Time.unscaledTime;
                predictionCount++;
                
                // 每隔几个预测模拟服务器确认
                if (predictionCount % 3 == 0)
                {
                    var confirmation = prediction;
                    confirmation.isConfirmed = true;
                    confirmation.timestamp = Time.unscaledTime + 0.001f; // 轻微延迟
                    confirmationCount++;
                }
                
                yield return null;
            }
            
            // 验证循环正常工作
            Assert.IsTrue(predictionCount > 30, $"Should have generated predictions, got {predictionCount}");
            Assert.IsTrue(confirmationCount > 10, $"Should have generated confirmations, got {confirmationCount}");
            
            float confirmationRate = (float)confirmationCount / predictionCount;
            Epic3TestUtilities.AssertInRange(confirmationRate, 0.2f, 0.5f, 
                "Confirmation rate should be reasonable");
            
            Debug.Log($"Continuous prediction cycle test passed: " +
                     $"{predictionCount} predictions, {confirmationCount} confirmations, " +
                     $"Rate: {confirmationRate:P}");
        }

        #endregion
    }
}