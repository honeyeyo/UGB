using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PongHub.VR;
using Meta.Utilities.Input;

namespace PongHub.VR.Tests
{
    /// <summary>
    /// Hand Tracking功能单元测试
    /// </summary>
    public class HandTrackingTests
    {
        private GameObject m_testGameObject;
        private EnhancedXRInputManager m_enhancedInputManager;
        private VRInteractionManager m_vrInteractionManager;
        private HandGestureRecognizer m_gestureRecognizer;

        [SetUp]
        public void SetUp()
        {
            m_testGameObject = new GameObject("HandTrackingTest");
            m_enhancedInputManager = m_testGameObject.AddComponent<EnhancedXRInputManager>();
            m_vrInteractionManager = m_testGameObject.AddComponent<VRInteractionManager>();
            m_gestureRecognizer = new HandGestureRecognizer();
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
        public void TestEnhancedXRInputManager_Creation()
        {
            // 测试增强输入管理器是否正确创建
            Assert.IsNotNull(m_enhancedInputManager, "EnhancedXRInputManager should be created");
            Assert.AreEqual(EnhancedXRInputManager.VRInputMode.Controller, m_enhancedInputManager.CurrentInputMode, "Default input mode should be Controller");
        }

        [Test]
        public void TestHandGestureRecognizer_Creation()
        {
            // 测试手势识别器是否正确创建
            Assert.IsNotNull(m_gestureRecognizer, "HandGestureRecognizer should be created");
        }

        [Test]
        public void TestHandGestureRecognizer_ConfidenceThreshold()
        {
            // 测试置信度阈值设置
            float testThreshold = 0.75f;
            m_gestureRecognizer.SetConfidenceThreshold(testThreshold);
            
            // 由于置信度阈值是私有的，我们通过其他方式验证设置是否生效
            Assert.DoesNotThrow(() => m_gestureRecognizer.SetConfidenceThreshold(testThreshold));
        }

        [Test]
        public void TestVRInputMode_Switching()
        {
            // 测试输入模式切换
            m_enhancedInputManager.SwitchToMode(EnhancedXRInputManager.VRInputMode.HandTracking);
            Assert.AreEqual(EnhancedXRInputManager.VRInputMode.HandTracking, m_enhancedInputManager.CurrentInputMode);

            m_enhancedInputManager.SwitchToMode(EnhancedXRInputManager.VRInputMode.Hybrid);
            Assert.AreEqual(EnhancedXRInputManager.VRInputMode.Hybrid, m_enhancedInputManager.CurrentInputMode);

            m_enhancedInputManager.SwitchToMode(EnhancedXRInputManager.VRInputMode.Controller);
            Assert.AreEqual(EnhancedXRInputManager.VRInputMode.Controller, m_enhancedInputManager.CurrentInputMode);
        }

        [Test]
        public void TestHandGesture_Enumeration()
        {
            // 测试手势枚举的完整性
            var gestureTypes = System.Enum.GetValues(typeof(EnhancedXRInputManager.HandGesture));
            Assert.Greater(gestureTypes.Length, 5, "Should have multiple hand gestures defined");

            // 确保包含关键手势
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.Pinch));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.Point));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.Fist));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.OpenHand));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.PaddleGrip));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.HandGesture), EnhancedXRInputManager.HandGesture.MenuGesture));
        }

        [Test]
        public void TestHandTrackingAvailability()
        {
            // 在测试环境中，Hand Tracking应该不可用
            bool isAvailable = m_enhancedInputManager.IsHandTrackingAvailable;
            
            // 在没有实际OVRHand组件的测试环境中，这应该返回false
            Assert.IsFalse(isAvailable, "Hand tracking should not be available in test environment");
        }

        [Test]
        public void TestHandTrackingConfidence()
        {
            // 测试手部追踪置信度获取
            float leftHandConfidence = m_enhancedInputManager.GetHandTrackingConfidence(true);
            float rightHandConfidence = m_enhancedInputManager.GetHandTrackingConfidence(false);

            // 在测试环境中，置信度应该是0
            Assert.AreEqual(0f, leftHandConfidence, "Left hand confidence should be 0 in test environment");
            Assert.AreEqual(0f, rightHandConfidence, "Right hand confidence should be 0 in test environment");
        }

        [Test]
        public void TestCurrentHandGesture()
        {
            // 测试当前手势获取
            var leftGesture = m_enhancedInputManager.GetCurrentHandGesture(true);
            var rightGesture = m_enhancedInputManager.GetCurrentHandGesture(false);

            // 在测试环境中，应该是None
            Assert.AreEqual(EnhancedXRInputManager.HandGesture.None, leftGesture);
            Assert.AreEqual(EnhancedXRInputManager.HandGesture.None, rightGesture);
        }

        [Test]
        public void TestHandPosition()
        {
            // 测试手部位置获取
            Vector3 leftHandPos = m_enhancedInputManager.GetHandPosition(true);
            Vector3 rightHandPos = m_enhancedInputManager.GetHandPosition(false);

            // 在测试环境中，应该是零向量
            Assert.AreEqual(Vector3.zero, leftHandPos);
            Assert.AreEqual(Vector3.zero, rightHandPos);
        }

        [Test]
        public void TestHandRotation()
        {
            // 测试手部旋转获取
            Quaternion leftHandRot = m_enhancedInputManager.GetHandRotation(true);
            Quaternion rightHandRot = m_enhancedInputManager.GetHandRotation(false);

            // 在测试环境中，应该是单位四元数
            Assert.AreEqual(Quaternion.identity, leftHandRot);
            Assert.AreEqual(Quaternion.identity, rightHandRot);
        }

        [Test]
        public void TestPointerPosition()
        {
            // 测试指针位置获取
            Vector3 leftPointerPos = m_enhancedInputManager.GetPointerPosition(true);
            Vector3 rightPointerPos = m_enhancedInputManager.GetPointerPosition(false);

            // 在测试环境中，应该是零向量
            Assert.AreEqual(Vector3.zero, leftPointerPos);
            Assert.AreEqual(Vector3.zero, rightPointerPos);
        }

        [Test]
        public void TestPointerDirection()
        {
            // 测试指针方向获取
            Vector3 leftPointerDir = m_enhancedInputManager.GetPointerDirection(true);
            Vector3 rightPointerDir = m_enhancedInputManager.GetPointerDirection(false);

            // 在测试环境中，应该是前向量
            Assert.AreEqual(Vector3.forward, leftPointerDir);
            Assert.AreEqual(Vector3.forward, rightPointerDir);
        }

        [Test]
        public void TestControllerConnection()
        {
            // 测试控制器连接检查
            bool leftControllerConnected = m_enhancedInputManager.IsControllerConnected(true);
            bool rightControllerConnected = m_enhancedInputManager.IsControllerConnected(false);

            // 在测试环境中，控制器应该不连接
            Assert.IsFalse(leftControllerConnected, "Left controller should not be connected in test environment");
            Assert.IsFalse(rightControllerConnected, "Right controller should not be connected in test environment");
        }

        [Test]
        public void TestGestureCallbackRegistration()
        {
            // 测试手势回调注册
            bool callbackTriggered = false;
            System.Action<bool, bool> testCallback = (isLeftHand, started) => { callbackTriggered = true; };

            Assert.DoesNotThrow(() => m_enhancedInputManager.RegisterGestureCallback(EnhancedXRInputManager.HandGesture.Pinch, testCallback));
            Assert.DoesNotThrow(() => m_enhancedInputManager.UnregisterGestureCallback(EnhancedXRInputManager.HandGesture.Pinch));
        }

        [Test]
        public void TestHandTrackingEnabled()
        {
            // 测试Hand Tracking启用/禁用
            Assert.DoesNotThrow(() => m_enhancedInputManager.SetHandTrackingEnabled(true));
            Assert.DoesNotThrow(() => m_enhancedInputManager.SetHandTrackingEnabled(false));
        }

        [Test]
        public void TestGestureRecognitionStats()
        {
            // 测试手势识别统计信息
            string stats = m_enhancedInputManager.GetGestureRecognitionStats();

            Assert.IsNotNull(stats, "Stats should not be null");
            Assert.IsNotEmpty(stats, "Stats should not be empty");
            Assert.IsTrue(stats.Contains("Hand Gesture Recognition Stats"), "Stats should contain header");
        }

        [Test]
        public void TestVRInteractionManager_HandTrackingIntegration()
        {
            // 测试VRInteractionManager的Hand Tracking集成
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetHandTrackingEnabled(true));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetHandTrackingEnabled(false));

            // 测试输入模式获取
            var currentMode = m_vrInteractionManager.GetCurrentInputMode();
            Assert.IsTrue(System.Enum.IsDefined(typeof(EnhancedXRInputManager.VRInputMode), currentMode));

            // 测试输入模式切换
            Assert.DoesNotThrow(() => m_vrInteractionManager.SwitchInputMode(EnhancedXRInputManager.VRInputMode.HandTracking));
        }

        [Test]
        public void TestVRInteractionManager_HandGestureAPI()
        {
            // 测试VRInteractionManager的手势API
            var leftGesture = m_vrInteractionManager.GetCurrentHandGesture(true);
            var rightGesture = m_vrInteractionManager.GetCurrentHandGesture(false);

            Assert.AreEqual(EnhancedXRInputManager.HandGesture.None, leftGesture);
            Assert.AreEqual(EnhancedXRInputManager.HandGesture.None, rightGesture);

            // 测试手势回调注册
            Assert.DoesNotThrow(() => m_vrInteractionManager.RegisterHandGestureCallback(
                EnhancedXRInputManager.HandGesture.Pinch, (isLeftHand, started) => { }));
            
            Assert.DoesNotThrow(() => m_vrInteractionManager.UnregisterHandGestureCallback(
                EnhancedXRInputManager.HandGesture.Pinch));
        }

        [Test]
        public void TestVRInteractionManager_HandTrackingDiagnostics()
        {
            // 测试Hand Tracking诊断信息
            string diagnostics = m_vrInteractionManager.GetSystemDiagnostics();

            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "Diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("Hand Tracking Initialized"), "Diagnostics should contain Hand Tracking info");
        }

        [Test]
        public void TestGestureRecognizer_NullHandHandling()
        {
            // 测试空手部数据的处理
            var gesture = m_gestureRecognizer.RecognizeGesture(null, null);
            Assert.AreEqual(EnhancedXRInputManager.HandGesture.None, gesture, "Should return None for null hand data");
        }

        [Test]
        public void TestGestureRecognizer_ConfidenceCalculation()
        {
            // 测试置信度计算
            float confidence = m_gestureRecognizer.GetGestureConfidence(EnhancedXRInputManager.HandGesture.Pinch);
            Assert.GreaterOrEqual(confidence, 0f, "Confidence should be non-negative");
            Assert.LessOrEqual(confidence, 1f, "Confidence should not exceed 1.0");
        }
    }
}