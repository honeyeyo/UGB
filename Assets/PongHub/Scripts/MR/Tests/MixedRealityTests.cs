#if HAS_META_AVATARS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PongHub.MR;
using PongHub.VR;
using System.Collections;

namespace PongHub.MR.Tests
{
    /// <summary>
    /// Mixed Reality功能单元测试
    /// </summary>
    public class MixedRealityTests
    {
        private GameObject m_testGameObject;
        private MRPassthroughManager m_passthroughManager;
        private EnvironmentBlendingSystem m_blendingSystem;
        private MRSafetyBoundary m_safetyBoundary;
        private VRInteractionManager m_vrInteractionManager;

        [SetUp]
        public void SetUp()
        {
            m_testGameObject = new GameObject("MRTest");
            m_passthroughManager = m_testGameObject.AddComponent<MRPassthroughManager>();
            m_blendingSystem = m_testGameObject.AddComponent<EnvironmentBlendingSystem>();
            m_safetyBoundary = m_testGameObject.AddComponent<MRSafetyBoundary>();
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

        #region MRPassthroughManager Tests

        [Test]
        public void TestMRPassthroughManager_Creation()
        {
            // 测试MR透视管理器是否正确创建
            Assert.IsNotNull(m_passthroughManager, "MRPassthroughManager should be created");
            Assert.AreEqual(MRPassthroughManager.PassthroughMode.Disabled, m_passthroughManager.CurrentMode, "Default mode should be Disabled");
            Assert.IsFalse(m_passthroughManager.IsInitialized, "Should not be initialized immediately");
        }

        [Test]
        public void TestPassthroughMode_Enumeration()
        {
            // 测试透视模式枚举的完整性
            var modeTypes = System.Enum.GetValues(typeof(MRPassthroughManager.PassthroughMode));
            Assert.AreEqual(3, modeTypes.Length, "Should have 3 passthrough modes");

            // 确保包含所有关键模式
            Assert.IsTrue(System.Enum.IsDefined(typeof(MRPassthroughManager.PassthroughMode), MRPassthroughManager.PassthroughMode.Disabled));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MRPassthroughManager.PassthroughMode), MRPassthroughManager.PassthroughMode.FullPassthrough));
            Assert.IsTrue(System.Enum.IsDefined(typeof(MRPassthroughManager.PassthroughMode), MRPassthroughManager.PassthroughMode.SelectivePassthrough));
        }

        [Test]
        public void TestPassthroughMode_Switching()
        {
            // 在测试环境中，模式切换不会实际生效，但应该不会抛出异常
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.FullPassthrough));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.SelectivePassthrough));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.Disabled));
        }

        [Test]
        public void TestPassthroughOpacity_Setting()
        {
            // 测试透视不透明度设置
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(0.5f));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(0.0f));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(1.0f));
            
            // 测试边界值
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(-0.5f)); // 应该被限制到0
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(1.5f));  // 应该被限制到1
        }

        [Test]
        public void TestPassthroughAvailability()
        {
            // 在测试环境中，透视功能应该不可用
            Assert.IsFalse(m_passthroughManager.IsPassthroughAvailable, "Passthrough should not be available in test environment");
        }

        [Test]
        public void TestColorPassthroughSupport()
        {
            // 测试彩色透视支持检查
            Assert.DoesNotThrow(() => m_passthroughManager.SupportsColorPassthrough());
        }

        [Test]
        public void TestRecommendedSettings()
        {
            // 测试推荐设置应用
            Assert.DoesNotThrow(() => m_passthroughManager.ApplyRecommendedSettings());
        }

        [Test]
        public void TestPassthroughDiagnostics()
        {
            // 测试诊断信息获取
            string diagnostics = m_passthroughManager.GetDiagnostics();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "Diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("MR Passthrough Manager Diagnostics"), "Diagnostics should contain header");
            Assert.IsTrue(diagnostics.Contains("Initialized:"), "Diagnostics should contain initialization status");
            Assert.IsTrue(diagnostics.Contains("Current Mode:"), "Diagnostics should contain current mode");
        }

        #endregion

        #region EnvironmentBlendingSystem Tests

        [Test]
        public void TestEnvironmentBlendingSystem_Creation()
        {
            // 测试环境融合系统是否正确创建
            Assert.IsNotNull(m_blendingSystem, "EnvironmentBlendingSystem should be created");
            Assert.IsFalse(m_blendingSystem.IsInitialized, "Should not be initialized immediately");
            Assert.IsFalse(m_blendingSystem.IsMRMode, "Should not be in MR mode initially");
        }

        [Test]
        public void TestVirtualObject_AddRemove()
        {
            // 测试虚拟对象添加和移除
            var testObject = new GameObject("TestVirtualObject");
            testObject.AddComponent<Renderer>();
            
            Assert.DoesNotThrow(() => m_blendingSystem.AddVirtualObject(testObject));
            Assert.DoesNotThrow(() => m_blendingSystem.RemoveVirtualObject(testObject));
            
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void TestEnvironmentLighting_Setting()
        {
            // 测试环境光照设置
            Assert.DoesNotThrow(() => m_blendingSystem.SetEnvironmentLighting(1.0f, Color.white));
            Assert.DoesNotThrow(() => m_blendingSystem.SetEnvironmentLighting(0.5f, Color.blue));
            Assert.DoesNotThrow(() => m_blendingSystem.SetEnvironmentLighting(2.0f, Color.red));
        }

        [Test]
        public void TestMaterialSetup_Methods()
        {
            // 测试MR材质设置和恢复
            Assert.DoesNotThrow(() => m_blendingSystem.SetupMRMaterials());
            Assert.DoesNotThrow(() => m_blendingSystem.RestoreOriginalMaterials());
        }

        [Test]
        public void TestBlendingSystemDiagnostics()
        {
            // 测试环境融合系统诊断信息
            string diagnostics = m_blendingSystem.GetDiagnostics();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "Diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("Environment Blending System Diagnostics"), "Diagnostics should contain header");
            Assert.IsTrue(diagnostics.Contains("Initialized:"), "Diagnostics should contain initialization status");
            Assert.IsTrue(diagnostics.Contains("MR Mode:"), "Diagnostics should contain MR mode status");
        }

        #endregion

        #region MRSafetyBoundary Tests

        [Test]
        public void TestMRSafetyBoundary_Creation()
        {
            // 测试MR安全边界系统是否正确创建
            Assert.IsNotNull(m_safetyBoundary, "MRSafetyBoundary should be created");
            Assert.IsFalse(m_safetyBoundary.IsInitialized, "Should not be initialized immediately");
            Assert.IsFalse(m_safetyBoundary.IsNearBoundary, "Should not be near boundary initially");
            Assert.IsFalse(m_safetyBoundary.IsInCriticalZone, "Should not be in critical zone initially");
            Assert.IsFalse(m_safetyBoundary.IsInEmergencyZone, "Should not be in emergency zone initially");
        }

        [Test]
        public void TestBoundaryDistance_Methods()
        {
            // 测试边界距离相关方法
            float distance = m_safetyBoundary.ClosestBoundaryDistance;
            Assert.IsTrue(distance >= 0f, "Boundary distance should be non-negative");
            
            // 在测试环境中应该返回最大值
            Assert.AreEqual(float.MaxValue, distance, "Should return max value in test environment");
        }

        [Test]
        public void TestPlayAreaInfo()
        {
            // 测试游戏区域信息
            Vector3 center = m_safetyBoundary.PlayAreaCenter;
            Vector2 size = m_safetyBoundary.PlayAreaSize;
            
            Assert.IsNotNull(center, "Play area center should not be null");
            Assert.IsNotNull(size, "Play area size should not be null");
        }

        [Test]
        public void TestSafetyDistances_Setting()
        {
            // 测试安全距离设置
            Assert.DoesNotThrow(() => m_safetyBoundary.SetSafetyDistances(0.5f, 0.3f, 0.1f));
            Assert.DoesNotThrow(() => m_safetyBoundary.SetSafetyDistances(1.0f, 0.5f, 0.2f));
        }

        [Test]
        public void TestBoundaryVisualization()
        {
            // 测试边界可视化
            Assert.DoesNotThrow(() => m_safetyBoundary.ShowBoundaryVisualization(true));
            Assert.DoesNotThrow(() => m_safetyBoundary.ShowBoundaryVisualization(false));
        }

        [Test]
        public void TestBoundaryDataRefresh()
        {
            // 测试边界数据刷新
            Assert.DoesNotThrow(() => m_safetyBoundary.RefreshBoundaryData());
        }

        [Test]
        public void TestSafetyBoundaryDiagnostics()
        {
            // 测试安全边界诊断信息
            string diagnostics = m_safetyBoundary.GetDiagnostics();
            
            Assert.IsNotNull(diagnostics, "Diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "Diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("MR Safety Boundary Diagnostics"), "Diagnostics should contain header");
            Assert.IsTrue(diagnostics.Contains("Initialized:"), "Diagnostics should contain initialization status");
            Assert.IsTrue(diagnostics.Contains("Boundary Points:"), "Diagnostics should contain boundary points info");
        }

        #endregion

        #region VRInteractionManager MR Integration Tests

        [Test]
        public void TestVRInteractionManager_MRIntegration()
        {
            // 测试VRInteractionManager的MR功能集成
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMREnabled(true));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMREnabled(false));
        }

        [Test]
        public void TestMRMode_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager设置MR模式
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMRMode(MRPassthroughManager.PassthroughMode.FullPassthrough));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMRMode(MRPassthroughManager.PassthroughMode.SelectivePassthrough));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMRMode(MRPassthroughManager.PassthroughMode.Disabled));
        }

        [Test]
        public void TestMRAvailability_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager检查MR可用性
            bool isAvailable = m_vrInteractionManager.IsMRAvailable();
            Assert.IsFalse(isAvailable, "MR should not be available in test environment");
        }

        [Test]
        public void TestMROpacity_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager控制MR透视不透明度
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMROpacity(0.5f));
            
            float opacity = m_vrInteractionManager.GetMROpacity();
            Assert.GreaterOrEqual(opacity, 0f, "Opacity should be non-negative");
            Assert.LessOrEqual(opacity, 1f, "Opacity should not exceed 1.0");
        }

        [Test]
        public void TestMRBoundary_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager检查MR边界
            bool nearBoundary = m_vrInteractionManager.IsNearMRBoundary();
            Assert.IsFalse(nearBoundary, "Should not be near boundary in test environment");
            
            float distance = m_vrInteractionManager.GetMRBoundaryDistance();
            Assert.IsTrue(distance >= 0f, "Boundary distance should be non-negative");
        }

        [Test]
        public void TestMRSafety_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager控制MR安全功能
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMRSafetyEnabled(true));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMRSafetyEnabled(false));
            Assert.DoesNotThrow(() => m_vrInteractionManager.RefreshMRBoundary());
        }

        [Test]
        public void TestMRVirtualObjects_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager管理MR虚拟对象
            var testObject = new GameObject("TestMRObject");
            
            Assert.DoesNotThrow(() => m_vrInteractionManager.AddVirtualObjectToMR(testObject));
            Assert.DoesNotThrow(() => m_vrInteractionManager.RemoveVirtualObjectFromMR(testObject));
            
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void TestMREnvironmentLighting_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager设置MR环境光照
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMREnvironmentLighting(1.0f, Color.white));
            Assert.DoesNotThrow(() => m_vrInteractionManager.SetMREnvironmentLighting(0.5f, Color.blue));
        }

        [Test]
        public void TestMRDiagnostics_ViaInteractionManager()
        {
            // 测试通过VRInteractionManager获取MR诊断信息
            string diagnostics = m_vrInteractionManager.GetMRDiagnostics();
            
            Assert.IsNotNull(diagnostics, "MR diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "MR diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("Mixed Reality Diagnostics"), "Diagnostics should contain MR header");
        }

        [Test]
        public void TestSystemDiagnostics_WithMR()
        {
            // 测试VRInteractionManager的系统诊断包含MR信息
            string diagnostics = m_vrInteractionManager.GetSystemDiagnostics();
            
            Assert.IsNotNull(diagnostics, "System diagnostics should not be null");
            Assert.IsNotEmpty(diagnostics, "System diagnostics should not be empty");
            Assert.IsTrue(diagnostics.Contains("MR Initialized:"), "Diagnostics should contain MR initialization status");
            Assert.IsTrue(diagnostics.Contains("MR Features Enabled:"), "Diagnostics should contain MR features status");
            Assert.IsTrue(diagnostics.Contains("Current MR Mode:"), "Diagnostics should contain current MR mode");
        }

        #endregion

        #region Integration and Performance Tests

        [Test]
        public void TestMR_ComponentsIntegration()
        {
            // 测试MR组件间的集成
            Assert.DoesNotThrow(() => {
                // 模拟组件间的交互
                m_passthroughManager.SetPassthroughMode(MRPassthroughManager.PassthroughMode.FullPassthrough);
                m_blendingSystem.SetupMRMaterials();
                m_safetyBoundary.ShowBoundaryVisualization(true);
            });
        }

        [Test]
        public void TestMR_NullSafety()
        {
            // 测试空引用安全性
            var emptyManager = new GameObject("EmptyMRTest").AddComponent<VRInteractionManager>();
            
            // 这些操作在没有MR组件的情况下应该安全执行
            Assert.DoesNotThrow(() => emptyManager.SetMREnabled(true));
            Assert.DoesNotThrow(() => emptyManager.SetMRMode(MRPassthroughManager.PassthroughMode.FullPassthrough));
            Assert.DoesNotThrow(() => emptyManager.SetMROpacity(0.5f));
            Assert.DoesNotThrow(() => emptyManager.SetMRSafetyEnabled(true));
            
            Assert.IsFalse(emptyManager.IsMRAvailable(), "MR should not be available without components");
            
            Object.DestroyImmediate(emptyManager.gameObject);
        }

        [Test]
        public void TestMR_ErrorHandling()
        {
            // 测试错误处理和边界情况
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(float.NaN));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(float.PositiveInfinity));
            Assert.DoesNotThrow(() => m_passthroughManager.SetPassthroughOpacity(float.NegativeInfinity));
            
            Assert.DoesNotThrow(() => m_blendingSystem.AddVirtualObject(null));
            Assert.DoesNotThrow(() => m_blendingSystem.RemoveVirtualObject(null));
            
            Assert.DoesNotThrow(() => m_safetyBoundary.SetSafetyDistances(-1f, -1f, -1f));
        }

        #endregion
    }
}
#endif
