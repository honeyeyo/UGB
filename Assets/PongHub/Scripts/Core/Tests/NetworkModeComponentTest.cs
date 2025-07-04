using UnityEngine;
using Unity.Netcode;
using PongHub.Core.Components;

namespace PongHub.Core.Tests
{
    /// <summary>
    /// NetworkModeComponent 测试脚本
    /// 提供调试功能和自动化测试支持
    /// </summary>
    public class NetworkModeComponentTest : MonoBehaviour
    {
        [Header("测试目标")]
        [SerializeField]
        [Tooltip("Test Target / 测试目标 - NetworkModeComponent to test")]
        private NetworkModeComponent m_testTarget;

        [Header("测试设置")]
        [SerializeField]
        [Tooltip("Auto Test Mode / 自动测试模式 - Enable automatic testing on start")]
        private bool m_autoTestMode = false;

        [SerializeField]
        [Tooltip("Test Interval / 测试间隔 - Time between automated tests")]
        private float m_testInterval = 5f;

        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging")]
        private bool m_debugMode = true;

        [Header("键盘快捷键测试")]
        [SerializeField]
        [Tooltip("Enable Keyboard Testing / 启用键盘测试 - Allow keyboard shortcuts for testing")]
        private bool m_enableKeyboardTesting = true;

        // 测试状态
        private float m_lastTestTime = 0f;
        private int m_testCount = 0;

        #region Unity 生命周期

        private void Start()
        {
            InitializeTest();
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleAutoTesting();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化测试
        /// </summary>
        private void InitializeTest()
        {
            // 自动查找测试目标
            if (m_testTarget == null)
            {
                m_testTarget = FindObjectOfType<NetworkModeComponent>();
            }

            if (m_testTarget == null)
            {
                if (m_debugMode)
                {
                    Debug.LogWarning("[NetworkModeComponentTest] 未找到NetworkModeComponent测试目标");
                }
                return;
            }

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 测试初始化完成 - 目标: {m_testTarget.name}");
            }

            LogKeyboardShortcuts();
        }

        /// <summary>
        /// 显示键盘快捷键说明
        /// </summary>
        private void LogKeyboardShortcuts()
        {
            if (m_enableKeyboardTesting && m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 键盘快捷键:\n" +
                         "N - 切换网络同步\n" +
                         "C - 测试网络连接\n" +
                         "D - 断开网络连接\n" +
                         "R - 强制重新连接\n" +
                         "S - 显示网络状态\n" +
                         "T - 运行完整测试\n" +
                         "F - 设置同步频率到30Hz\n" +
                         "G - 设置同步频率到60Hz\n" +
                         "H - 设置同步频率到90Hz");
            }
        }

        #endregion

        #region 输入处理

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            if (!m_enableKeyboardTesting || m_testTarget == null) return;

            if (Input.GetKeyDown(KeyCode.N))
            {
                TestToggleNetworkSync();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                TestNetworkConnection();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                TestDisconnectNetwork();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                TestForceReconnect();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                ShowNetworkStatus();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                RunFullTest();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                TestSetSyncRate(30f);
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                TestSetSyncRate(60f);
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                TestSetSyncRate(90f);
            }
        }

        /// <summary>
        /// 处理自动测试
        /// </summary>
        private void HandleAutoTesting()
        {
            if (!m_autoTestMode || m_testTarget == null) return;

            if (Time.time - m_lastTestTime >= m_testInterval)
            {
                RunAutomatedTest();
                m_lastTestTime = Time.time;
            }
        }

        #endregion

        #region 测试方法

        /// <summary>
        /// 测试网络同步切换
        /// </summary>
        [ContextMenu("测试网络同步切换")]
        public void TestToggleNetworkSync()
        {
            if (m_testTarget == null) return;

            // 获取当前状态并切换
            bool currentState = true; // 假设当前启用，实际应该从组件获取
            m_testTarget.SetNetworkSyncEnabled(!currentState);

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 网络同步已切换到: {!currentState}");
            }
        }

        /// <summary>
        /// 测试网络连接
        /// </summary>
        [ContextMenu("测试网络连接")]
        public void TestNetworkConnection()
        {
            if (m_testTarget == null) return;

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 开始测试网络连接");
            }

            // 触发网络模式切换以测试连接
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(GameMode.Network);
            }
        }

        /// <summary>
        /// 测试断开网络连接
        /// </summary>
        [ContextMenu("测试断开网络连接")]
        public void TestDisconnectNetwork()
        {
            if (m_testTarget == null) return;

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 测试断开网络连接");
            }

            // 切换到本地模式以断开网络
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(GameMode.Local);
            }
        }

        /// <summary>
        /// 测试强制重新连接
        /// </summary>
        [ContextMenu("测试强制重新连接")]
        public void TestForceReconnect()
        {
            if (m_testTarget == null) return;

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 测试强制重新连接");
            }

            m_testTarget.ForceReconnect();
        }

        /// <summary>
        /// 显示网络状态
        /// </summary>
        [ContextMenu("显示网络状态")]
        public void ShowNetworkStatus()
        {
            if (m_testTarget == null) return;

            string statusInfo = m_testTarget.GetNetworkStatusInfo();

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 网络状态:\n{statusInfo}");
            }
        }

        /// <summary>
        /// 测试设置同步频率
        /// </summary>
        public void TestSetSyncRate(float rate)
        {
            if (m_testTarget == null) return;

            m_testTarget.SetSyncRate(rate);

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 同步频率已设置为: {rate}Hz");
            }
        }

        /// <summary>
        /// 运行完整测试
        /// </summary>
        [ContextMenu("运行完整测试")]
        public void RunFullTest()
        {
            if (m_testTarget == null)
            {
                if (m_debugMode)
                {
                    Debug.LogError("[NetworkModeComponentTest] 无法运行测试，测试目标为null");
                }
                return;
            }

            StartCoroutine(RunFullTestCoroutine());
        }

        /// <summary>
        /// 运行自动化测试
        /// </summary>
        public void RunAutomatedTest()
        {
            m_testCount++;

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 自动化测试 #{m_testCount}");
            }

            ShowNetworkStatus();

            // 每5次测试切换一次同步频率
            if (m_testCount % 5 == 0)
            {
                float[] testRates = { 30f, 60f, 90f };
                float newRate = testRates[m_testCount / 5 % testRates.Length];
                TestSetSyncRate(newRate);
            }
        }

        #endregion

        #region 协程测试

        /// <summary>
        /// 完整测试协程
        /// </summary>
        private System.Collections.IEnumerator RunFullTestCoroutine()
        {
            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 开始完整测试序列");
            }

            // 1. 显示初始状态
            ShowNetworkStatus();
            yield return new WaitForSeconds(1f);

            // 2. 测试同步频率设置
            TestSetSyncRate(30f);
            yield return new WaitForSeconds(1f);

            TestSetSyncRate(60f);
            yield return new WaitForSeconds(1f);

            TestSetSyncRate(90f);
            yield return new WaitForSeconds(1f);

            // 3. 测试网络同步切换
            TestToggleNetworkSync();
            yield return new WaitForSeconds(1f);

            TestToggleNetworkSync();
            yield return new WaitForSeconds(1f);

            // 4. 显示最终状态
            ShowNetworkStatus();

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 完整测试序列完成");
            }
        }

        #endregion

        #region 验证测试

        /// <summary>
        /// 验证组件状态
        /// </summary>
        public bool ValidateComponentState()
        {
            if (m_testTarget == null)
            {
                if (m_debugMode)
                {
                    Debug.LogError("[NetworkModeComponentTest] 验证失败：测试目标为null");
                }
                return false;
            }

            // 检查组件是否正确实现了接口
            if (!(m_testTarget is IGameModeComponent))
            {
                if (m_debugMode)
                {
                    Debug.LogError("[NetworkModeComponentTest] 验证失败：组件未实现IGameModeComponent接口");
                }
                return false;
            }

            // 检查组件是否正确继承了NetworkBehaviour
            if (!(m_testTarget is NetworkBehaviour))
            {
                if (m_debugMode)
                {
                    Debug.LogError("[NetworkModeComponentTest] 验证失败：组件未继承NetworkBehaviour");
                }
                return false;
            }

            if (m_debugMode)
            {
                Debug.Log("[NetworkModeComponentTest] 组件状态验证通过");
            }

            return true;
        }

        /// <summary>
        /// 验证网络管理器状态
        /// </summary>
        public bool ValidateNetworkManagerState()
        {
            if (NetworkManager.Singleton == null)
            {
                if (m_debugMode)
                {
                    Debug.LogWarning("[NetworkModeComponentTest] NetworkManager.Singleton为null");
                }
                return false;
            }

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] NetworkManager状态: " +
                         $"IsHost={NetworkManager.Singleton.IsHost}, " +
                         $"IsClient={NetworkManager.Singleton.IsConnectedClient}, " +
                         $"IsServer={NetworkManager.Singleton.IsServer}");
            }

            return true;
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置测试目标
        /// </summary>
        public void SetTestTarget(NetworkModeComponent target)
        {
            m_testTarget = target;

            if (m_debugMode)
            {
                Debug.Log($"[NetworkModeComponentTest] 测试目标已设置: {(target != null ? target.name : "null")}");
            }
        }

        /// <summary>
        /// 获取测试结果
        /// </summary>
        public string GetTestResults()
        {
            return $"测试执行次数: {m_testCount}\n" +
                   $"组件状态验证: {(ValidateComponentState() ? "通过" : "失败")}\n" +
                   $"网络管理器验证: {(ValidateNetworkManagerState() ? "通过" : "失败")}\n" +
                   $"测试目标: {(m_testTarget != null ? m_testTarget.name : "无")}";
        }

        #endregion
    }
}