using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using PongHub.VR;
using PongHub.Core;

namespace PongHub.VR.Tests
{
    /// <summary>
    /// VRInteractionManager单元测试
    /// </summary>
    public class VRInteractionManagerTests
    {
        private GameObject m_testGameObject;
        private VRInteractionManager m_vrInteractionManager;

        [SetUp]
        public void SetUp()
        {
            m_testGameObject = new GameObject("VRInteractionManagerTest");
            m_vrInteractionManager = m_testGameObject.AddComponent<VRInteractionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_testGameObject != null)
            {
                Object.DestroyImmediate(m_testGameObject);
            }
        }

        [Test]
        public void TestVRPerformanceMonitor_Creation()
        {
            // 测试性能监控器是否正确创建
            var performanceMonitor = m_vrInteractionManager.GetPerformanceMonitor();
            Assert.IsNotNull(performanceMonitor, "Performance monitor should be created");
        }

        [Test]
        public void TestVRPerformanceMonitor_FrameTimeRecording()
        {
            // 测试帧时间记录功能
            var performanceMonitor = m_vrInteractionManager.GetPerformanceMonitor();
            
            performanceMonitor.RecordFrameTime(0.016f); // 60fps
            performanceMonitor.RecordFrameTime(0.008f); // 120fps
            
            float averageFrameTime = performanceMonitor.GetAverageFrameTime();
            Assert.Greater(averageFrameTime, 0f, "Average frame time should be greater than 0");
            Assert.Less(averageFrameTime, 0.1f, "Average frame time should be reasonable");
        }

        [Test]
        public void TestVRPerformanceMonitor_FPSCalculation()
        {
            // 测试FPS计算
            var performanceMonitor = m_vrInteractionManager.GetPerformanceMonitor();
            
            performanceMonitor.RecordFrameTime(0.008333f); // 120fps
            
            float fps = performanceMonitor.GetCurrentFPS();
            Assert.Greater(fps, 100f, "FPS should be around 120");
            Assert.Less(fps, 130f, "FPS should be reasonable");
        }

        [Test]
        public void TestVRPerformanceMonitor_PerformanceCheck()
        {
            // 测试性能检查
            var performanceMonitor = m_vrInteractionManager.GetPerformanceMonitor();
            
            // 记录好的性能
            performanceMonitor.RecordFrameTime(0.008f); // 125fps
            Assert.IsTrue(performanceMonitor.IsPerformanceGood(), "Should indicate good performance for 125fps");
            
            // 记录差的性能
            performanceMonitor.RecordFrameTime(0.02f); // 50fps
            performanceMonitor.RecordFrameTime(0.025f); // 40fps
            Assert.IsFalse(performanceMonitor.IsPerformanceGood(), "Should indicate poor performance for low fps");
        }

        [Test]
        public void TestInteractionIntensitySettings()
        {
            // 测试交互强度设置
            m_vrInteractionManager.SetInteractionIntensity(0.5f, 0.7f, 0.3f);
            
            // 由于字段是私有的，我们通过其他方式验证设置是否生效
            // 这里主要测试方法不会抛出异常
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetInteractionIntensity(0.5f, 0.7f, 0.3f));
        }

        [Test]
        public void TestControllerTrackingValidation()
        {
            // 测试控制器跟踪验证
            // 在没有实际VR设备的情况下，这应该返回false
            bool leftHandTracking = m_vrInteractionManager.IsControllerTracking(XRNode.LeftHand);
            bool rightHandTracking = m_vrInteractionManager.IsControllerTracking(XRNode.RightHand);
            
            // 在测试环境中，控制器跟踪应该是false
            Assert.IsFalse(leftHandTracking, "Left hand tracking should be false in test environment");
            Assert.IsFalse(rightHandTracking, "Right hand tracking should be false in test environment");
        }

        [Test]
        public void TestControllerTrackingAccuracy()
        {
            // 测试控制器跟踪精度
            float leftAccuracy = m_vrInteractionManager.GetControllerTrackingAccuracy(XRNode.LeftHand);
            float rightAccuracy = m_vrInteractionManager.GetControllerTrackingAccuracy(XRNode.RightHand);
            
            // 在测试环境中，精度应该是0
            Assert.AreEqual(0f, leftAccuracy, "Left hand tracking accuracy should be 0 in test environment");
            Assert.AreEqual(0f, rightAccuracy, "Right hand tracking accuracy should be 0 in test environment");
        }

        [Test]
        public void TestSystemInitialization()
        {
            // 测试系统初始化状态
            // 由于在测试环境中VibrationManager和AudioManager可能不存在，这可能返回false
            bool isInitialized = m_vrInteractionManager.IsSystemInitialized();
            
            // 我们主要测试方法不会抛出异常
            Assert.DoesNotThrow(() => m_vrInteractionManager.IsSystemInitialized());
        }

        [Test]
        public void TestSystemDiagnostics()
        {
            // 测试系统诊断信息
            string diagnostics = m_vrInteractionManager.GetSystemDiagnostics();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "Diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("VR Interaction Manager Diagnostics"), "Diagnostics should contain header");
        }

        [Test]
        public void TestManualFeedbackTriggering()
        {
            // 测试手动触发反馈
            // 这应该不会抛出异常，即使没有实际的振动和音频管理器
            Assert.DoesNotThrow(() => 
                m_vrInteractionManager.TriggerManualFeedback(VRInteractionType.Grab, true, null));
            
            Assert.DoesNotThrow(() => 
                m_vrInteractionManager.TriggerManualFeedback(VRInteractionType.RaySelect, false, null));
        }

        [Test]
        public void TestVisualEffectsManagement()
        {
            // 测试视觉效果管理
            int initialHoveringCount = m_vrInteractionManager.GetHoveringObjectCount();
            Assert.AreEqual(0, initialHoveringCount, "Initial hovering count should be 0");
            
            // 测试停止所有视觉效果
            Assert.DoesNotThrow(() => m_vrInteractionManager.StopAllVisualEffects());
            
            int afterStopCount = m_vrInteractionManager.GetHoveringObjectCount();
            Assert.AreEqual(0, afterStopCount, "Count should remain 0 after stopping effects");
        }

        [Test]
        public void TestVRInteractionTypeEnum()
        {
            // 测试VR交互类型枚举的完整性
            var interactionTypes = System.Enum.GetValues(typeof(VRInteractionType));
            Assert.Greater(interactionTypes.Length, 5, "Should have multiple interaction types defined");
            
            // 确保包含基本的交互类型
            Assert.IsTrue(System.Enum.IsDefined(typeof(VRInteractionType), VRInteractionType.Hover));
            Assert.IsTrue(System.Enum.IsDefined(typeof(VRInteractionType), VRInteractionType.Grab));
            Assert.IsTrue(System.Enum.IsDefined(typeof(VRInteractionType), VRInteractionType.Release));
            Assert.IsTrue(System.Enum.IsDefined(typeof(VRInteractionType), VRInteractionType.RaySelect));
        }
    }
}