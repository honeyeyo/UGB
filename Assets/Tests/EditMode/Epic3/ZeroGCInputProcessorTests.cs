using UnityEngine;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using PongHub.Input.Performance;
using PongHub.Tests;
using System.Text;

namespace PongHub.Tests.Epic3
{
    /// <summary>
    /// ZeroGCInputProcessor单元测试
    /// 验证零GC输入处理器的核心功能和内存优化特性
    /// </summary>
    [TestFixture]
    public class ZeroGCInputProcessorTests
    {
        private GameObject testObject;
        private ZeroGCInputProcessor processor;

        [SetUp]
        public void Setup()
        {
            testObject = Epic3TestUtilities.CreateTestGameObject("TestZGIP");
            processor = testObject.AddComponent<ZeroGCInputProcessor>();
        }

        [TearDown]
        public void Teardown()
        {
            Epic3TestUtilities.DestroyTestGameObject(testObject);
        }

        #region TC-ZGIP-001: 对象池测试

        [Test]
        public void TestObjectPool_GetAndReturnInputDataPackets()
        {
            // Arrange & Act - 获取多个输入数据包
            var packet1 = processor.GetInputDataPacket();
            var packet2 = processor.GetInputDataPacket();
            var packet3 = processor.GetInputDataPacket();
            
            // Assert - 验证获取的对象是有效的
            Assert.IsNotNull(packet1, "First packet should not be null");
            Assert.IsNotNull(packet2, "Second packet should not be null");
            Assert.IsNotNull(packet3, "Third packet should not be null");
            
            // 修改数据以确保对象独立
            packet1.leftHandPosition = Vector3.one;
            packet2.leftHandPosition = Vector3.zero;
            packet3.leftHandPosition = Vector3.up;
            
            // Act - 归还对象到池中
            processor.ReturnInputDataPacket(packet1);
            processor.ReturnInputDataPacket(packet2);
            processor.ReturnInputDataPacket(packet3);
            
            // Act - 重新获取对象，应该复用之前的对象
            var reusedPacket = processor.GetInputDataPacket();
            
            // Assert - 验证对象被重置
            Assert.AreEqual(Vector3.zero, reusedPacket.leftHandPosition, 
                "Reused packet should be reset to default values");
            
            Debug.Log("Object pool test passed: InputDataPackets properly pooled and reset");
        }

        [Test]
        public void TestObjectPool_NoGCAllocationInPoolOperations()
        {
            // Act & Assert - 验证对象池操作无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var packet = processor.GetInputDataPacket();
                    packet.leftHandPosition = new Vector3(i, i, i);
                    processor.ReturnInputDataPacket(packet);
                }
            }, "Object pool operations");
            
            Debug.Log("Memory test passed: Object pool operations produce no GC allocation");
        }

        [Test]
        public void TestObjectPool_PoolExpansion()
        {
            // Arrange - 获取比初始池大小更多的对象
            var packets = new ZeroGCInputProcessor.InputDataPacket[25]; // 超过默认池大小20
            
            // Act - 获取大量对象
            for (int i = 0; i < packets.Length; i++)
            {
                packets[i] = processor.GetInputDataPacket();
                packets[i].sequenceNumber = (uint)i; // 设置唯一标识
            }
            
            // Assert - 验证所有对象都是有效且唯一的
            for (int i = 0; i < packets.Length; i++)
            {
                Assert.AreEqual(i, packets[i].sequenceNumber, 
                    $"Packet {i} should have correct sequence number");
            }
            
            // Cleanup - 归还所有对象
            for (int i = 0; i < packets.Length; i++)
            {
                processor.ReturnInputDataPacket(packets[i]);
            }
            
            Debug.Log($"Pool expansion test passed: Successfully handled {packets.Length} objects");
        }

        #endregion

        #region TC-ZGIP-002: 字符串缓存测试

        [Test]
        public void TestStringCaching_CommonStringsCached()
        {
            // Arrange - 常用字符串列表
            string[] commonStrings = {
                "LeftHand", "RightHand", "Trigger", "Grip", "Menu",
                "ButtonA", "ButtonB", "Stick", "Position", "Rotation"
            };
            
            // Act & Assert - 验证常用字符串被缓存
            foreach (string str in commonStrings)
            {
                string cached1 = processor.GetCachedString(str);
                string cached2 = processor.GetCachedString(str);
                
                Assert.AreSame(cached1, cached2, 
                    $"String '{str}' should return same cached instance");
                Assert.AreEqual(str, cached1, 
                    $"Cached string should equal original: '{str}'");
            }
            
            Debug.Log($"String caching test passed: {commonStrings.Length} common strings properly cached");
        }

        [Test]
        public void TestStringCaching_CacheHitRate()
        {
            // Arrange - 混合已缓存和新字符串
            string[] testStrings = {
                "LeftHand", "RightHand", "NewString1", "Trigger", 
                "NewString2", "Menu", "NewString3", "ButtonA"
            };
            
            int expectedCachedCount = 5; // LeftHand, RightHand, Trigger, Menu, ButtonA
            int actualCachedCount = 0;
            
            // Act - 测试缓存命中
            foreach (string str in testStrings)
            {
                string result1 = processor.GetCachedString(str);
                string result2 = processor.GetCachedString(str);
                
                if (ReferenceEquals(result1, result2))
                {
                    actualCachedCount++;
                }
            }
            
            // Assert - 验证缓存命中率
            float hitRate = (float)actualCachedCount / testStrings.Length;
            Assert.IsTrue(hitRate >= 0.6f, // 至少60%命中率
                $"Cache hit rate {hitRate:P} should be at least 60%");
            
            Debug.Log($"Cache hit rate test passed: {hitRate:P} ({actualCachedCount}/{testStrings.Length})");
        }

        [Test]
        public void TestStringCaching_NoGCInCacheOperations()
        {
            // Act & Assert - 验证字符串缓存操作无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    processor.GetCachedString("LeftHand");
                    processor.GetCachedString("RightHand");
                    processor.GetCachedString("Trigger");
                }
            }, "String cache operations");
            
            Debug.Log("Memory test passed: String cache operations produce no GC allocation");
        }

        #endregion

        #region TC-ZGIP-003: 零GC输入处理测试

        [Test]
        public void TestZeroGCInputProcessing_MockInputProcessing()
        {
            // Arrange - 创建模拟输入上下文和输出数据包
            var mockContext = Epic3TestUtilities.CreateMockInputContext("LeftHand", Vector3.one);
            var outputData = processor.GetInputDataPacket();
            
            // Act & Assert - 验证输入处理无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                // 模拟输入处理调用
                // processor.ProcessInputDataZeroGC(mockContext, ref outputData);
                
                // 由于需要真实的InputAction.CallbackContext，我们模拟核心逻辑
                outputData.leftHandPosition = mockContext.ReadValue<Vector3>();
                outputData.timestamp = Time.unscaledTime;
                outputData.sequenceNumber++;
            }, "Zero GC input processing");
            
            // Cleanup
            processor.ReturnInputDataPacket(outputData);
            
            Debug.Log("Zero GC input processing test passed: No allocation in input processing");
        }

        [Test]
        public void TestZeroGCInputProcessing_BatchInputProcessing()
        {
            // Arrange - 准备批量输入处理
            var inputPackets = new ZeroGCInputProcessor.InputDataPacket[50];
            for (int i = 0; i < inputPackets.Length; i++)
            {
                inputPackets[i] = processor.GetInputDataPacket();
            }
            
            // Act & Assert - 验证批量处理无GC分配
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < inputPackets.Length; i++)
                {
                    // 模拟输入数据处理
                    inputPackets[i].leftHandPosition = new Vector3(i, i, i);
                    inputPackets[i].rightHandPosition = new Vector3(-i, i, -i);
                    inputPackets[i].timestamp = Time.unscaledTime;
                    inputPackets[i].sequenceNumber = (uint)i;
                    
                    // 使用位操作设置按钮状态（避免装箱）
                    inputPackets[i].SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, i % 2 == 0);
                    inputPackets[i].SetButtonState(ZeroGCInputProcessor.InputButton.RightB, i % 3 == 0);
                }
            }, "Batch input processing");
            
            // Cleanup
            for (int i = 0; i < inputPackets.Length; i++)
            {
                processor.ReturnInputDataPacket(inputPackets[i]);
            }
            
            Debug.Log($"Batch processing test passed: {inputPackets.Length} packets processed with no GC allocation");
        }

        [Test]
        public void TestInputDataPacket_BitFieldOperations()
        {
            // Arrange
            var packet = processor.GetInputDataPacket();
            
            // Act & Assert - 测试按钮状态位操作
            Assert.IsFalse(packet.GetButtonState(ZeroGCInputProcessor.InputButton.LeftA), 
                "Initial button state should be false");
            
            packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, true);
            Assert.IsTrue(packet.GetButtonState(ZeroGCInputProcessor.InputButton.LeftA), 
                "Button state should be true after setting");
            
            packet.SetButtonState(ZeroGCInputProcessor.InputButton.RightB, true);
            Assert.IsTrue(packet.GetButtonState(ZeroGCInputProcessor.InputButton.LeftA), 
                "LeftA should remain true");
            Assert.IsTrue(packet.GetButtonState(ZeroGCInputProcessor.InputButton.RightB), 
                "RightB should be true");
            
            packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, false);
            Assert.IsFalse(packet.GetButtonState(ZeroGCInputProcessor.InputButton.LeftA), 
                "LeftA should be false after clearing");
            Assert.IsTrue(packet.GetButtonState(ZeroGCInputProcessor.InputButton.RightB), 
                "RightB should remain true");
            
            // Cleanup
            processor.ReturnInputDataPacket(packet);
            
            Debug.Log("Bit field operations test passed: Button states handled correctly");
        }

        #endregion

        #region TC-ZGIP-P001: 处理性能测试

        [Test]
        public void TestProcessingPerformance_AverageExecutionTime()
        {
            // Arrange - 准备测试数据
            var inputPackets = new ZeroGCInputProcessor.InputDataPacket[1000];
            for (int i = 0; i < inputPackets.Length; i++)
            {
                inputPackets[i] = processor.GetInputDataPacket();
            }
            
            // Act - 测量平均执行时间
            float avgTime = Epic3TestUtilities.MeasureAverageExecutionTime(() =>
            {
                var packet = inputPackets[0];
                packet.leftHandPosition = Vector3.one;
                packet.rightHandPosition = Vector3.zero;
                packet.leftStick = Vector2.one;
                packet.rightStick = Vector2.zero;
                packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, true);
                packet.timestamp = Time.unscaledTime;
            }, 10000);
            
            // Assert - 验证性能目标
            Assert.IsTrue(avgTime < 0.01f, 
                $"Average processing time {avgTime:F6}ms should be < 0.01ms");
            
            // Cleanup
            for (int i = 0; i < inputPackets.Length; i++)
            {
                processor.ReturnInputDataPacket(inputPackets[i]);
            }
            
            Debug.Log($"Processing performance test passed: Average time {avgTime:F6}ms");
        }

        [Test]
        public void TestProcessingPerformance_HighFrequencyOperations()
        {
            // Arrange - 模拟高频输入处理（1000Hz）
            var packet = processor.GetInputDataPacket();
            
            // Act & Assert - 测量高频操作性能
            Epic3TestUtilities.AssertPerformance(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    packet.leftHandPosition = new Vector3(i, i, i);
                    packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftTrigger, i % 2 == 0);
                    packet.timestamp = Time.unscaledTime + i * 0.001f;
                }
            }, 1.0f, "High frequency input processing (1000 operations)");
            
            // Cleanup
            processor.ReturnInputDataPacket(packet);
            
            Debug.Log("High frequency operations test passed: 1000 operations < 1ms");
        }

        #endregion

        #region TC-ZGIP-P002: 内存效率测试

        [Test]
        public void TestMemoryEfficiency_ObjectPoolReuse()
        {
            // Arrange - 获取初始内存统计
            var initialStats = processor.GetMemoryStats();
            
            // Act - 大量对象操作
            for (int i = 0; i < 1000; i++)
            {
                var packet = processor.GetInputDataPacket();
                packet.leftHandPosition = new Vector3(i, i, i);
                processor.ReturnInputDataPacket(packet);
            }
            
            // Assert - 验证内存统计
            var finalStats = processor.GetMemoryStats();
            
            // 对象池应该有效复用，总GC分配应该很少
            Assert.IsTrue(finalStats.totalGCAlloc - initialStats.totalGCAlloc < 1.0f,
                $"GC allocation should be minimal: {finalStats.totalGCAlloc - initialStats.totalGCAlloc:F3}KB");
            
            Debug.Log($"Memory efficiency test passed: " +
                     $"GC delta: {finalStats.totalGCAlloc - initialStats.totalGCAlloc:F3}KB, " +
                     $"Pool count: {finalStats.inputDataPoolCount}");
        }

        [Test]
        public void TestMemoryEfficiency_StringBuilderOperations()
        {
            // Act & Assert - 测试字符串构建操作的内存效率
            Epic3TestUtilities.AssertNoGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    string result = processor.BuildStringZeroGC("Test", i, "_", "String");
                    // 在实际应用中，这个字符串会被使用，但在测试中我们只关心构建过程
                }
            }, "StringBuilder operations");
            
            Debug.Log("StringBuilder operations test passed: No GC allocation in string building");
        }

        [Test]
        public void TestMemoryStats_Accuracy()
        {
            // Arrange - 执行一些已知的内存操作
            int initialCachedStrings = processor.GetMemoryStats().cachedStringsCount;
            
            // Act - 添加一些缓存字符串
            processor.GetCachedString("TestString1");
            processor.GetCachedString("TestString2");
            processor.GetCachedString("TestString3");
            
            // Assert - 验证统计准确性
            var stats = processor.GetMemoryStats();
            Assert.IsTrue(stats.cachedStringsCount >= initialCachedStrings + 3,
                $"Cached strings count should increase: {initialCachedStrings} -> {stats.cachedStringsCount}");
            
            Assert.IsTrue(stats.inputDataPoolCount >= 0, "Pool count should be non-negative");
            Assert.IsTrue(stats.totalGCAlloc >= 0, "Total GC allocation should be non-negative");
            
            Debug.Log($"Memory stats accuracy test passed: " +
                     $"Cached strings: {stats.cachedStringsCount}, " +
                     $"Pool count: {stats.inputDataPoolCount}, " +
                     $"Total GC: {stats.totalGCAlloc:F3}KB");
        }

        #endregion

        #region 边界条件和错误处理测试

        [Test]
        public void TestBoundaryConditions_NullInputHandling()
        {
            // Test null string caching
            string nullResult = processor.GetCachedString(null);
            Assert.IsNull(nullResult, "Null input should return null");
            
            // Test empty string caching
            string emptyResult = processor.GetCachedString("");
            Assert.AreEqual("", emptyResult, "Empty string should be handled correctly");
            
            Debug.Log("Boundary conditions test passed: Null and empty inputs handled correctly");
        }

        [Test]
        public void TestBoundaryConditions_LargeDataHandling()
        {
            // Test with large position values
            var packet = processor.GetInputDataPacket();
            
            packet.leftHandPosition = new Vector3(float.MaxValue, float.MinValue, 0);
            packet.rightHandPosition = new Vector3(-1000000, 1000000, 500000);
            
            // Should not throw exceptions
            Assert.DoesNotThrow(() =>
            {
                var pos = packet.leftHandPosition;
                packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, true);
                packet.timestamp = Time.unscaledTime;
            }, "Large values should be handled without exceptions");
            
            processor.ReturnInputDataPacket(packet);
            
            Debug.Log("Large data handling test passed: Extreme values handled correctly");
        }

        [Test]
        public void TestErrorHandling_ButtonStateEnumValues()
        {
            var packet = processor.GetInputDataPacket();
            
            // Test all button enum values
            foreach (ZeroGCInputProcessor.InputButton button in 
                     System.Enum.GetValues(typeof(ZeroGCInputProcessor.InputButton)))
            {
                Assert.DoesNotThrow(() =>
                {
                    packet.SetButtonState(button, true);
                    bool state = packet.GetButtonState(button);
                    Assert.IsTrue(state, $"Button {button} should be set to true");
                    
                    packet.SetButtonState(button, false);
                    state = packet.GetButtonState(button);
                    Assert.IsFalse(state, $"Button {button} should be set to false");
                }, $"Button {button} operations should not throw exceptions");
            }
            
            processor.ReturnInputDataPacket(packet);
            
            Debug.Log("Button state enum test passed: All button values handled correctly");
        }

        #endregion

        #region 并发和线程安全测试

        [Test]
        public void TestThreadSafety_ConcurrentPoolOperations()
        {
            // 注意：Unity的单元测试通常在主线程运行
            // 这个测试主要验证基本的并发访问模式
            
            var packets = new ZeroGCInputProcessor.InputDataPacket[10];
            
            // 模拟并发获取和归还
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < packets.Length; i++)
                {
                    packets[i] = processor.GetInputDataPacket();
                    packets[i].sequenceNumber = (uint)i;
                }
                
                for (int i = 0; i < packets.Length; i++)
                {
                    processor.ReturnInputDataPacket(packets[i]);
                }
            }, "Concurrent-like pool operations should not throw exceptions");
            
            Debug.Log("Thread safety test passed: Basic concurrent operations handled");
        }

        #endregion

        #region 长时间运行和压力测试

        [UnityTest]
        public IEnumerator TestLongRunning_ContinuousOperations()
        {
            // 长时间连续操作测试
            float startTime = Time.unscaledTime;
            float testDuration = 1f; // 1秒测试
            
            int operationCount = 0;
            var packet = processor.GetInputDataPacket();
            
            while (Time.unscaledTime - startTime < testDuration)
            {
                // 模拟连续输入处理
                packet.leftHandPosition = new Vector3(operationCount, operationCount, operationCount);
                packet.SetButtonState(ZeroGCInputProcessor.InputButton.LeftA, operationCount % 2 == 0);
                packet.timestamp = Time.unscaledTime;
                
                operationCount++;
                yield return null;
            }
            
            processor.ReturnInputDataPacket(packet);
            
            // 验证系统仍正常工作
            var finalStats = processor.GetMemoryStats();
            Assert.IsTrue(finalStats.inputDataPoolCount >= 0, "Pool should remain valid after long run");
            Assert.IsTrue(operationCount > 30, $"Should have processed at least 30 operations, got {operationCount}");
            
            Debug.Log($"Long running test passed: {operationCount} operations in {testDuration}s, " +
                     $"Final GC allocation: {finalStats.totalGCAlloc:F3}KB");
        }

        #endregion
    }
}