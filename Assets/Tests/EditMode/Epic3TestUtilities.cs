using UnityEngine;
using NUnit.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using Unity.PerformanceTesting;

namespace PongHub.Tests
{
    /// <summary>
    /// Epic-3测试工具类 - 提供通用测试功能和断言方法
    /// </summary>
    public static class Epic3TestUtilities
    {
        /// <summary>
        /// 创建测试GameObject
        /// </summary>
        public static GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            return go;
        }

        /// <summary>
        /// 清理测试GameObject
        /// </summary>
        public static void DestroyTestGameObject(GameObject go)
        {
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 性能测试断言 - 验证执行时间不超过阈值
        /// </summary>
        public static void AssertPerformance(Action action, float maxTimeMs, string testName = "Performance Test")
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
            }
            
            float actualTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            Assert.IsTrue(actualTimeMs <= maxTimeMs, 
                $"{testName} failed: {actualTimeMs:F3}ms > {maxTimeMs:F3}ms");
        }

        /// <summary>
        /// 零GC分配测试断言
        /// </summary>
        public static void AssertNoGCAlloc(Action action, string testName = "Zero GC Test")
        {
            // 强制GC清理，获取基准内存
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long beforeGC = GC.GetTotalMemory(false);
            
            try
            {
                action();
            }
            finally
            {
                long afterGC = GC.GetTotalMemory(false);
                long allocatedBytes = afterGC - beforeGC;
                
                Assert.IsTrue(allocatedBytes <= 0, 
                    $"{testName} failed: {allocatedBytes} bytes allocated");
            }
        }

        /// <summary>
        /// 近似相等断言 - 用于浮点数比较
        /// </summary>
        public static void AssertApproximately(float expected, float actual, float tolerance, string message = "")
        {
            float difference = Mathf.Abs(expected - actual);
            Assert.IsTrue(difference <= tolerance,
                $"{message} Expected: {expected:F6}, Actual: {actual:F6}, Tolerance: {tolerance:F6}");
        }

        /// <summary>
        /// 范围断言 - 验证值在指定范围内
        /// </summary>
        public static void AssertInRange(float value, float min, float max, string message = "")
        {
            Assert.IsTrue(value >= min && value <= max,
                $"{message} Value {value:F3} not in range [{min:F3}, {max:F3}]");
        }

        /// <summary>
        /// 数组近似相等断言
        /// </summary>
        public static void AssertArrayApproximately(float[] expected, float[] actual, float tolerance, string message = "")
        {
            Assert.AreEqual(expected.Length, actual.Length, $"{message} Array lengths differ");
            
            for (int i = 0; i < expected.Length; i++)
            {
                AssertApproximately(expected[i], actual[i], tolerance, 
                    $"{message} Element[{i}]");
            }
        }

        /// <summary>
        /// 向量近似相等断言
        /// </summary>
        public static void AssertVector3Approximately(Vector3 expected, Vector3 actual, float tolerance, string message = "")
        {
            AssertApproximately(expected.x, actual.x, tolerance, $"{message} X component");
            AssertApproximately(expected.y, actual.y, tolerance, $"{message} Y component");
            AssertApproximately(expected.z, actual.z, tolerance, $"{message} Z component");
        }

        /// <summary>
        /// 创建模拟性能数据
        /// </summary>
        public static (float cpu, float gpu, float frame) CreateMockPerformanceData(
            float cpuTimeMs = 2.0f, 
            float gpuTimeMs = 1.0f, 
            float frameTimeMs = 16.67f)
        {
            return (cpuTimeMs, gpuTimeMs, frameTimeMs);
        }

        /// <summary>
        /// 等待帧数
        /// </summary>
        public static IEnumerator WaitForFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 等待时间（秒）
        /// </summary>
        public static IEnumerator WaitForSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 模拟输入动作上下文
        /// </summary>
        public static MockInputActionContext CreateMockInputContext(string actionName, object value)
        {
            return new MockInputActionContext(actionName, value);
        }

        /// <summary>
        /// 模拟输入动作上下文类
        /// </summary>
        public class MockInputActionContext
        {
            public string ActionName { get; }
            public object Value { get; }
            public float StartTime { get; }

            public MockInputActionContext(string actionName, object value)
            {
                ActionName = actionName;
                Value = value;
                StartTime = Time.unscaledTime;
            }

            public T ReadValue<T>()
            {
                if (Value is T typedValue)
                {
                    return typedValue;
                }
                return default(T);
            }
        }

        /// <summary>
        /// 批量执行动作并测量平均性能
        /// </summary>
        public static float MeasureAverageExecutionTime(Action action, int iterations = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            
            stopwatch.Stop();
            return (float)stopwatch.Elapsed.TotalMilliseconds / iterations;
        }

        /// <summary>
        /// 统计性能数据
        /// </summary>
        public static PerformanceStats CalculatePerformanceStats(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return new PerformanceStats();

            Array.Sort(samples);
            
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample;
            }
            
            return new PerformanceStats
            {
                Count = samples.Length,
                Average = sum / samples.Length,
                Min = samples[0],
                Max = samples[samples.Length - 1],
                Percentile50 = samples[samples.Length / 2],
                Percentile95 = samples[(int)(samples.Length * 0.95f)],
                Percentile99 = samples[(int)(samples.Length * 0.99f)]
            };
        }

        /// <summary>
        /// 性能统计结构
        /// </summary>
        public struct PerformanceStats
        {
            public int Count;
            public float Average;
            public float Min;
            public float Max;
            public float Percentile50;
            public float Percentile95;
            public float Percentile99;

            public override string ToString()
            {
                return $"Count: {Count}, Avg: {Average:F3}ms, " +
                       $"Min: {Min:F3}ms, Max: {Max:F3}ms, " +
                       $"P50: {Percentile50:F3}ms, P95: {Percentile95:F3}ms, P99: {Percentile99:F3}ms";
            }
        }
    }
}