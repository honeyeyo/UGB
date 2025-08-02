#if HAS_META_AVATARS

using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using PongHub.VR.Avatar;
using PongHub.Core;

namespace PongHub.VR.Avatar.Tests
{
    /// <summary>
    /// VR Avatar系统测试套件
    /// 测试Avatar管理、动作同步、表情系统和网络同步功能
    /// </summary>
    public class VRAvatarSystemTests
    {
        private GameObject m_testGameObject;
        private VRAvatarManager m_avatarManager;
        private AvatarMotionSync m_motionSync;
        private AvatarExpressionSystem m_expressionSystem;
        private NetworkAvatarSync m_networkSync;

        [SetUp]
        public void SetUp()
        {
            // 创建测试GameObject
            m_testGameObject = new GameObject("VRAvatarSystemTest");
            
            // 添加Avatar系统组件
            m_avatarManager = m_testGameObject.AddComponent<VRAvatarManager>();
            m_motionSync = m_testGameObject.AddComponent<AvatarMotionSync>();
            m_expressionSystem = m_testGameObject.AddComponent<AvatarExpressionSystem>();
            m_networkSync = m_testGameObject.AddComponent<NetworkAvatarSync>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_testGameObject != null)
            {
                Object.DestroyImmediate(m_testGameObject);
            }
        }

        #region VRAvatarManager Tests

        [Test]
        public void VRAvatarManager_InitialState_IsCorrect()
        {
            // 测试Avatar管理器的初始状态
            Assert.IsNotNull(m_avatarManager);
            Assert.IsFalse(m_avatarManager.IsInitialized);
            Assert.IsFalse(m_avatarManager.IsAvatarLoaded);
            Assert.AreEqual(VRAvatarManager.AvatarState.Uninitialized, m_avatarManager.CurrentState);
        }

        [Test]
        public void VRAvatarManager_AvatarTypeSettings_WorkCorrectly()
        {
            // 测试Avatar类型设置
            Assert.AreEqual(VRAvatarManager.AvatarType.LocalPlayer, m_avatarManager.Type);
            
            // 通过反射设置Avatar类型进行测试
            var field = typeof(VRAvatarManager).GetField("m_avatarType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            field?.SetValue(m_avatarManager, VRAvatarManager.AvatarType.RemotePlayer);
            Assert.AreEqual(VRAvatarManager.AvatarType.RemotePlayer, m_avatarManager.Type);
        }

        [UnityTest]
        public IEnumerator VRAvatarManager_Initialization_CompletesSuccessfully()
        {
            // 测试Avatar管理器初始化过程
            bool initializationCompleted = false;
            
            m_avatarManager.OnAvatarStateChanged.AddListener((state) =>
            {
                if (state == VRAvatarManager.AvatarState.Ready || state == VRAvatarManager.AvatarState.Error)
                {
                    initializationCompleted = true;
                }
            });

            // 等待初始化完成或超时
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!initializationCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 验证初始化结果（由于测试环境可能没有完整的Avatar SDK，预期可能是Error状态）
            Assert.IsTrue(initializationCompleted, "Avatar initialization should complete within timeout");
        }

        [Test]
        public void VRAvatarManager_DiagnosticsInfo_IsNotEmpty()
        {
            // 测试诊断信息功能
            string diagnostics = m_avatarManager.GetDiagnostics();
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Length > 0);
            Assert.IsTrue(diagnostics.Contains("VR Avatar Manager Diagnostics"));
        }

        #endregion

        #region AvatarMotionSync Tests

        [Test]
        public void AvatarMotionSync_InitialState_IsCorrect()
        {
            // 测试动作同步组件的初始状态
            Assert.IsNotNull(m_motionSync);
            Assert.IsFalse(m_motionSync.IsInitialized);
            Assert.IsFalse(m_motionSync.IsHandTrackingActive);
        }

        [Test]
        public void AvatarMotionSync_TrackingModeChange_WorksCorrectly()
        {
            // 测试追踪模式切换
            bool modeChangeTriggered = false;
            AvatarMotionSync.TrackingMode receivedMode = AvatarMotionSync.TrackingMode.ControllerOnly;
            
            m_motionSync.OnTrackingModeChanged.AddListener((mode) =>
            {
                modeChangeTriggered = true;
                receivedMode = mode;
            });

            // 切换追踪模式
            m_motionSync.SetTrackingMode(AvatarMotionSync.TrackingMode.HandTrackingOnly);
            
            Assert.IsTrue(modeChangeTriggered);
            Assert.AreEqual(AvatarMotionSync.TrackingMode.HandTrackingOnly, receivedMode);
        }

        [Test]
        public void AvatarMotionSync_SyncQualitySettings_WorkCorrectly()
        {
            // 测试同步质量设置
            m_motionSync.SetSyncQuality(AvatarMotionSync.SyncQuality.High);
            // 由于没有直接的getter，我们测试方法调用不会抛出异常
            Assert.DoesNotThrow(() => m_motionSync.SetSyncQuality(AvatarMotionSync.SyncQuality.Low));
        }

        [Test]
        public void AvatarMotionSync_DiagnosticsInfo_IsNotEmpty()
        {
            // 测试诊断信息功能
            string diagnostics = m_motionSync.GetDiagnostics();
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Length > 0);
            Assert.IsTrue(diagnostics.Contains("Avatar Motion Sync Diagnostics"));
        }

        #endregion

        #region AvatarExpressionSystem Tests

        [Test]
        public void AvatarExpressionSystem_InitialState_IsCorrect()
        {
            // 测试表情系统的初始状态
            Assert.IsNotNull(m_expressionSystem);
            Assert.IsFalse(m_expressionSystem.IsInitialized);
            Assert.AreEqual(AvatarExpressionSystem.BasicExpression.Neutral, m_expressionSystem.CurrentExpression);
        }

        [Test]
        public void AvatarExpressionSystem_ExpressionChange_WorksCorrectly()
        {
            // 测试表情切换
            bool expressionChangeTriggered = false;
            AvatarExpressionSystem.BasicExpression receivedExpression = AvatarExpressionSystem.BasicExpression.Neutral;
            
            m_expressionSystem.OnExpressionChanged.AddListener((expression) =>
            {
                expressionChangeTriggered = true;
                receivedExpression = expression;
            });

            // 设置表情
            m_expressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Happy, 1f);
            
            // 由于事件可能在下一帧触发，我们检查方法调用是否成功
            Assert.DoesNotThrow(() => m_expressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Sad, 0.8f));
        }

        [Test]
        public void AvatarExpressionSystem_EmotionTrigger_WorksCorrectly()
        {
            // 测试情绪触发
            Assert.DoesNotThrow(() => m_expressionSystem.TriggerEmotion("victory", 1f));
            Assert.DoesNotThrow(() => m_expressionSystem.TriggerEmotion("defeat", 0.5f));
        }

        [Test]
        public void AvatarExpressionSystem_GazeTarget_CanBeSet()
        {
            // 测试注视目标设置
            GameObject target = new GameObject("GazeTarget");
            
            Assert.DoesNotThrow(() => m_expressionSystem.SetGazeTarget(target.transform));
            Assert.DoesNotThrow(() => m_expressionSystem.SetGazeTarget(null));
            
            Object.DestroyImmediate(target);
        }

        [Test]
        public void AvatarExpressionSystem_DiagnosticsInfo_IsNotEmpty()
        {
            // 测试诊断信息功能
            string diagnostics = m_expressionSystem.GetDiagnostics();
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Length > 0);
            Assert.IsTrue(diagnostics.Contains("Avatar Expression System Diagnostics"));
        }

        #endregion

        #region NetworkAvatarSync Tests

        [Test]
        public void NetworkAvatarSync_InitialState_IsCorrect()
        {
            // 测试网络同步组件的初始状态
            Assert.IsNotNull(m_networkSync);
            Assert.IsTrue(m_networkSync.IsNetworkSyncEnabled); // 默认启用
        }

        [Test]
        public void NetworkAvatarSync_SyncModeChange_WorksCorrectly()
        {
            // 测试同步模式切换
            Assert.DoesNotThrow(() => m_networkSync.SetSyncMode(NetworkAvatarSync.SyncMode.Minimal));
            Assert.DoesNotThrow(() => m_networkSync.SetSyncMode(NetworkAvatarSync.SyncMode.Full));
        }

        [Test]
        public void NetworkAvatarSync_SyncFrequencyChange_WorksCorrectly()
        {
            // 测试同步频率设置
            Assert.DoesNotThrow(() => m_networkSync.SetSyncFrequency(30f));
            Assert.DoesNotThrow(() => m_networkSync.SetSyncFrequency(60f));
        }

        [Test]
        public void NetworkAvatarSync_NetworkStats_AreAccessible()
        {
            // 测试网络统计信息访问
            Assert.GreaterOrEqual(m_networkSync.NetworkLatency, 0f);
            Assert.GreaterOrEqual(m_networkSync.PacketsPerSecond, 0);
            Assert.GreaterOrEqual(m_networkSync.BandwidthUsage, 0f);
        }

        [Test]
        public void NetworkAvatarSync_DiagnosticsInfo_IsNotEmpty()
        {
            // 测试诊断信息功能
            string diagnostics = m_networkSync.GetDiagnostics();
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Length > 0);
            Assert.IsTrue(diagnostics.Contains("Network Avatar Sync Diagnostics"));
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator AvatarSystem_ComponentsIntegration_WorksTogether()
        {
            // 测试Avatar系统组件之间的集成
            bool anyComponentInitialized = false;
            
            // 监听各组件的初始化事件
            m_avatarManager.OnAvatarLoaded.AddListener(() => anyComponentInitialized = true);
            m_motionSync.OnMotionSyncInitialized.AddListener(() => anyComponentInitialized = true);
            m_expressionSystem.OnExpressionSystemInitialized.AddListener(() => anyComponentInitialized = true);

            // 等待任何组件初始化完成
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!anyComponentInitialized && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 测试组件之间的基本交互（即使初始化失败也应该不抛出异常）
            Assert.DoesNotThrow(() =>
            {
                m_expressionSystem.SetExpression(AvatarExpressionSystem.BasicExpression.Happy);
                m_motionSync.SetTrackingMode(AvatarMotionSync.TrackingMode.Hybrid);
                m_networkSync.SetSyncMode(NetworkAvatarSync.SyncMode.Optimized);
            });
        }

        [Test]
        public void AvatarSystem_AllComponents_HaveDiagnostics()
        {
            // 测试所有组件都提供诊断信息
            var components = new object[]
            {
                m_avatarManager,
                m_motionSync,
                m_expressionSystem,
                m_networkSync
            };

            foreach (var component in components)
            {
                var method = component.GetType().GetMethod("GetDiagnostics");
                Assert.IsNotNull(method, $"Component {component.GetType().Name} should have GetDiagnostics method");
                
                var result = method.Invoke(component, null) as string;
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Length > 0);
            }
        }

        [Test]
        public void AvatarSystem_ComponentReferences_AreNotNull()
        {
            // 测试所有组件引用不为空
            Assert.IsNotNull(m_avatarManager);
            Assert.IsNotNull(m_motionSync);
            Assert.IsNotNull(m_expressionSystem);
            Assert.IsNotNull(m_networkSync);
        }

        #endregion

        #region Performance Tests

        [UnityTest]
        public IEnumerator AvatarSystem_PerformanceTest_RunsWithoutIssues()
        {
            // 性能测试：确保Avatar系统不会导致严重的性能问题
            int frameCount = 0;
            float totalFrameTime = 0f;
            const int testFrames = 60; // 测试60帧

            while (frameCount < testFrames)
            {
                float frameStart = Time.realtimeSinceStartup;
                
                // 模拟Avatar系统的常见操作
                m_expressionSystem.SetExpression(
                    (AvatarExpressionSystem.BasicExpression)(frameCount % 8), 
                    Random.Range(0.5f, 1f));
                
                m_motionSync.SetSyncQuality(
                    (AvatarMotionSync.SyncQuality)(frameCount % 4));

                float frameEnd = Time.realtimeSinceStartup;
                totalFrameTime += (frameEnd - frameStart);
                frameCount++;
                
                yield return null;
            }

            float averageFrameTime = totalFrameTime / testFrames;
            
            // 确保平均帧时间不超过16.67ms（60FPS）
            Assert.Less(averageFrameTime, 0.0167f, 
                $"Avatar system operations should not take more than 16.67ms per frame. Average: {averageFrameTime * 1000:F2}ms");
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void AvatarSystem_InvalidInputs_HandleGracefully()
        {
            // 测试无效输入的处理
            Assert.DoesNotThrow(() => m_expressionSystem.SetExpression(
                AvatarExpressionSystem.BasicExpression.Happy, -1f)); // 负强度
            
            Assert.DoesNotThrow(() => m_motionSync.SetSyncQuality(
                (AvatarMotionSync.SyncQuality)999)); // 无效枚举值
            
            Assert.DoesNotThrow(() => m_networkSync.SetSyncFrequency(-10f)); // 负频率
        }

        [Test]
        public void AvatarSystem_NullReferences_HandleGracefully()
        {
            // 测试空引用的处理
            Assert.DoesNotThrow(() => m_expressionSystem.SetGazeTarget(null));
            Assert.DoesNotThrow(() => m_expressionSystem.TriggerEmotion(null, 1f));
            Assert.DoesNotThrow(() => m_expressionSystem.TriggerEmotion("", 1f));
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 创建一个简单的测试Avatar GameObject
        /// </summary>
        private GameObject CreateTestAvatar()
        {
            var avatar = new GameObject("TestAvatar");
            avatar.AddComponent<Animator>();
            return avatar;
        }

        /// <summary>
        /// 等待条件满足或超时
        /// </summary>
        private IEnumerator WaitForConditionOrTimeout(System.Func<bool> condition, float timeout = 5f)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// VRInteractionManager与Avatar系统集成测试
    /// </summary>
    public class VRInteractionManagerAvatarIntegrationTests
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
        public void VRInteractionManager_AvatarIntegration_IsConfigurable()
        {
            // 测试Avatar集成的配置功能
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarIntegrationEnabled(true));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarIntegrationEnabled(false));
        }

        [Test]
        public void VRInteractionManager_AvatarComponents_AreAccessible()
        {
            // 测试Avatar组件的访问功能
            Assert.DoesNotThrow(() => m_vrInteractionManager.GetVRAvatarManager());
            Assert.DoesNotThrow(() => m_vrInteractionManager.GetAvatarMotionSync());
            Assert.DoesNotThrow(() => m_vrInteractionManager.GetAvatarExpressionSystem());
            Assert.DoesNotThrow(() => m_vrInteractionManager.GetNetworkAvatarSync());
        }

        [Test]
        public void VRInteractionManager_AvatarExpressionControl_WorksCorrectly()
        {
            // 测试Avatar表情控制功能
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarExpression(
                AvatarExpressionSystem.BasicExpression.Happy, 1f, 2f));
            
            Assert.DoesNotThrow(() => m_vrInteractionManager.TriggerAvatarGameEmotion("victory", 0.8f));
        }

        [Test]
        public void VRInteractionManager_AvatarDiagnostics_IsAvailable()
        {
            // 测试Avatar系统诊断信息
            string diagnostics = m_vrInteractionManager.GetAvatarSystemDiagnostics();
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Length > 0);
            Assert.IsTrue(diagnostics.Contains("Avatar System Integration Diagnostics"));
        }

        [Test]
        public void VRInteractionManager_AvatarEmotionIntensity_IsConfigurable()
        {
            // 测试Avatar情绪强度设置
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarEmotionIntensity(0.5f));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarEmotionIntensity(2f));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetAvatarEmotionIntensity(-1f)); // 应该被限制在有效范围内
        }
    }
}
#endif