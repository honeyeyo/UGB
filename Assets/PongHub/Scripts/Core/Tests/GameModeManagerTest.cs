using UnityEngine;
using System.Collections;
using PongHub.Core;

namespace PongHub.Core.Tests
{
    /// <summary>
    /// GameModeManager单元测试
    /// 使用简单的MonoBehaviour测试而不是NUnit测试框架
    /// </summary>
    public class GameModeManagerTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField]
        [Tooltip("Enable Auto Testing / 启用自动测试 - Whether to automatically run tests at intervals")]
        private bool enableAutoTesting = false;
        [SerializeField]
        [Tooltip("Test Interval / 测试间隔 - Time interval between automatic test runs")]
        private float testInterval = 2f;

        private GameObject m_testGameObject;
        private GameModeManager m_gameModeManager;
        private TestGameModeComponent m_testComponent;
        private float m_lastTestTime;

        private void Start()
        {
            SetupTest();
        }

        private void Update()
        {
            if (enableAutoTesting && Time.time - m_lastTestTime >= testInterval)
            {
                m_lastTestTime = Time.time;
                RunTests();
            }
        }

        private void SetupTest()
        {
            // 创建测试GameObject和GameModeManager组件
            m_testGameObject = new GameObject("TestGameModeManager");
            m_gameModeManager = m_testGameObject.AddComponent<GameModeManager>();
            m_testComponent = new TestGameModeComponent();

            Debug.Log("[GameModeManagerTest] 测试环境设置完成");
        }

        private void RunTests()
        {
            Debug.Log("[GameModeManagerTest] 开始运行测试...");

            // 测试1: 单例模式
            TestSingleton();

            // 测试2: 默认模式
            TestDefaultMode();

            // 测试3: 组件注册
            TestComponentRegistration();

            // 测试4: 重复注册
            TestDuplicateRegistration();

            // 测试5: 空组件注册
            TestNullComponentRegistration();

            Debug.Log("[GameModeManagerTest] 所有测试完成");
        }

        private void TestSingleton()
        {
            Debug.Log("[Test] 测试单例模式...");
            if (GameModeManager.Instance != null && GameModeManager.Instance == m_gameModeManager)
            {
                Debug.Log("[Test] ✓ 单例模式测试通过");
            }
            else
            {
                Debug.LogError("[Test] ✗ 单例模式测试失败");
            }
        }

        private void TestDefaultMode()
        {
            Debug.Log("[Test] 测试默认模式...");
            if (m_gameModeManager.CurrentMode == GameMode.Local)
            {
                Debug.Log("[Test] ✓ 默认模式测试通过");
            }
            else
            {
                Debug.LogError($"[Test] ✗ 默认模式测试失败，当前模式: {m_gameModeManager.CurrentMode}");
            }
        }

        private void TestComponentRegistration()
        {
            Debug.Log("[Test] 测试组件注册...");

            // 测试注册
            m_gameModeManager.RegisterComponent(m_testComponent);
            int countAfterRegister = m_gameModeManager.GetRegisteredComponentCount();

            // 测试注销
            m_gameModeManager.UnregisterComponent(m_testComponent);
            int countAfterUnregister = m_gameModeManager.GetRegisteredComponentCount();

            if (countAfterRegister == 1 && countAfterUnregister == 0)
            {
                Debug.Log("[Test] ✓ 组件注册测试通过");
            }
            else
            {
                Debug.LogError($"[Test] ✗ 组件注册测试失败，注册后: {countAfterRegister}，注销后: {countAfterUnregister}");
            }
        }

        private void TestDuplicateRegistration()
        {
            Debug.Log("[Test] 测试重复注册...");

            // 多次注册同一组件
            m_gameModeManager.RegisterComponent(m_testComponent);
            m_gameModeManager.RegisterComponent(m_testComponent);
            int count = m_gameModeManager.GetRegisteredComponentCount();

            if (count == 1)
            {
                Debug.Log("[Test] ✓ 重复注册测试通过");
            }
            else
            {
                Debug.LogError($"[Test] ✗ 重复注册测试失败，组件数量: {count}");
            }

            // 清理
            m_gameModeManager.UnregisterComponent(m_testComponent);
        }

        private void TestNullComponentRegistration()
        {
            Debug.Log("[Test] 测试空组件注册...");

            try
            {
                m_gameModeManager.RegisterComponent(null);
                m_gameModeManager.UnregisterComponent(null);
                int count = m_gameModeManager.GetRegisteredComponentCount();

                if (count == 0)
                {
                    Debug.Log("[Test] ✓ 空组件注册测试通过");
                }
                else
                {
                    Debug.LogError($"[Test] ✗ 空组件注册测试失败，组件数量: {count}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] ✗ 空组件注册测试异常: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            // 清理测试对象
            if (m_testGameObject != null)
            {
                DestroyImmediate(m_testGameObject);
            }
        }

        [ContextMenu("运行所有测试")]
        public void RunAllTests()
        {
            RunTests();
        }

        [ContextMenu("测试模式切换")]
        public void TestModeSwitch()
        {
            Debug.Log("[Test] 测试模式切换...");

            m_gameModeManager.RegisterComponent(m_testComponent);
            m_gameModeManager.SwitchToMode(GameMode.Network);

            StartCoroutine(CheckModeSwitchResult());
        }

        private IEnumerator CheckModeSwitchResult()
        {
            yield return new WaitForSeconds(0.5f);

            if (m_gameModeManager.CurrentMode == GameMode.Network && m_testComponent.WasModeChangeCalled)
            {
                Debug.Log("[Test] ✓ 模式切换测试通过");
            }
            else
            {
                Debug.LogError($"[Test] ✗ 模式切换测试失败，当前模式: {m_gameModeManager.CurrentMode}，组件通知: {m_testComponent.WasModeChangeCalled}");
            }

            m_gameModeManager.UnregisterComponent(m_testComponent);
        }
    }

    /// <summary>
    /// 测试用的GameModeComponent实现
    /// </summary>
    public class TestGameModeComponent : IGameModeComponent
    {
        public bool WasModeChangeCalled { get; private set; }
        public GameMode LastNewMode { get; private set; }
        public GameMode LastPreviousMode { get; private set; }

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            WasModeChangeCalled = true;
            LastNewMode = newMode;
            LastPreviousMode = previousMode;
            Debug.Log($"[TestGameModeComponent] 模式改变: {previousMode} -> {newMode}");
        }

        public bool IsActiveInMode(GameMode mode)
        {
            return mode == GameMode.Local || mode == GameMode.Network;
        }
    }
}