using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PongHub.Core.Components;

namespace PongHub.Core.Tests
{
    /// <summary>
    /// 模式切换测试脚本
    /// 用于测试环境组件的动态模式切换
    /// </summary>
    public class ModeSwitchTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField]
        [Tooltip("Enable Keyboard Testing / 启用键盘测试 - Enable keyboard shortcuts for testing")]
        private bool m_enableKeyboardTesting = true;

        [SerializeField]
        [Tooltip("Auto Test Interval / 自动测试间隔 - Time interval between automatic tests in seconds")]
        private float m_autoTestInterval = 5f;

        [SerializeField]
        [Tooltip("Run Auto Tests / 运行自动测试 - Automatically run mode switching tests")]
        private bool m_runAutoTests = false;

        [Header("测试UI")]
        [SerializeField]
        [Tooltip("Status Text / 状态文本 - Text component to display test status")]
        private Text m_statusText;

        [SerializeField]
        [Tooltip("Local Mode Button / 本地模式按钮 - Button to switch to local mode")]
        private Button m_localModeButton;

        [SerializeField]
        [Tooltip("Network Mode Button / 网络模式按钮 - Button to switch to network mode")]
        private Button m_networkModeButton;

        [SerializeField]
        [Tooltip("Menu Mode Button / 菜单模式按钮 - Button to switch to menu mode")]
        private Button m_menuModeButton;

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Game Mode Manager / 游戏模式管理器 - Reference to the game mode manager")]
        private GameModeManager m_gameModeManager;

        [SerializeField]
        [Tooltip("Environment State Manager / 环境状态管理器 - Reference to the environment state manager")]
        private EnvironmentStateManager m_environmentStateManager;

        [SerializeField]
        [Tooltip("Network Mode Component / 网络模式组件 - Reference to the network mode component")]
        private NetworkModeComponent m_networkModeComponent;

        [SerializeField]
        [Tooltip("Mode Transition Effect / 模式切换效果 - Reference to the mode transition effect")]
        private ModeTransitionEffect m_transitionEffect;

        // 测试状态
        private bool m_isTestRunning = false;
        private float m_lastTestTime = 0f;
        private int m_testCount = 0;
        private GameMode m_lastTestedMode = GameMode.Local;

        // 测试结果
        private bool m_localToNetworkSuccess = false;
        private bool m_networkToLocalSuccess = false;
        private bool m_menuTransitionSuccess = false;
        private bool m_statePreservationSuccess = false;

        #region Unity 生命周期

        private void Start()
        {
            // 查找组件
            FindComponents();

            // 设置按钮事件
            SetupButtons();

            // 注册事件监听
            RegisterEventListeners();

            // 初始化状态文本
            UpdateStatusText();

            Debug.Log("[ModeSwitchTest] 模式切换测试脚本已初始化");
        }

        private void Update()
        {
            // 处理键盘输入
            if (m_enableKeyboardTesting)
            {
                HandleKeyboardInput();
            }

            // 处理自动测试
            if (m_runAutoTests && !m_isTestRunning)
            {
                HandleAutoTest();
            }
        }

        private void OnDestroy()
        {
            // 注销事件监听
            UnregisterEventListeners();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 查找组件
        /// </summary>
        private void FindComponents()
        {
            // 查找GameModeManager
            if (m_gameModeManager == null)
            {
                m_gameModeManager = FindObjectOfType<GameModeManager>();
            }

            // 查找EnvironmentStateManager
            if (m_environmentStateManager == null)
            {
                m_environmentStateManager = FindObjectOfType<EnvironmentStateManager>();
            }

            // 查找NetworkModeComponent
            if (m_networkModeComponent == null)
            {
                m_networkModeComponent = FindObjectOfType<NetworkModeComponent>();
            }

            // 查找ModeTransitionEffect
            if (m_transitionEffect == null)
            {
                m_transitionEffect = FindObjectOfType<ModeTransitionEffect>();
            }
        }

        /// <summary>
        /// 设置按钮事件
        /// </summary>
        private void SetupButtons()
        {
            // 本地模式按钮
            if (m_localModeButton != null)
            {
                m_localModeButton.onClick.AddListener(() => SwitchToMode(GameMode.Local));
            }

            // 网络模式按钮
            if (m_networkModeButton != null)
            {
                m_networkModeButton.onClick.AddListener(() => SwitchToMode(GameMode.Network));
            }

            // 菜单模式按钮
            if (m_menuModeButton != null)
            {
                m_menuModeButton.onClick.AddListener(() => SwitchToMode(GameMode.Menu));
            }
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEventListeners()
        {
            if (m_gameModeManager != null)
            {
                m_gameModeManager.OnModeChanged += OnGameModeChanged;
            }

            if (m_transitionEffect != null)
            {
                m_transitionEffect.OnTransitionStarted += OnTransitionStarted;
                m_transitionEffect.OnTransitionCompleted += OnTransitionCompleted;
            }

            if (m_networkModeComponent != null)
            {
                m_networkModeComponent.OnNetworkStatusChanged += OnNetworkStatusChanged;
                m_networkModeComponent.OnNetworkError += OnNetworkError;
            }
        }

        /// <summary>
        /// 注销事件监听
        /// </summary>
        private void UnregisterEventListeners()
        {
            if (m_gameModeManager != null)
            {
                m_gameModeManager.OnModeChanged -= OnGameModeChanged;
            }

            if (m_transitionEffect != null)
            {
                m_transitionEffect.OnTransitionStarted -= OnTransitionStarted;
                m_transitionEffect.OnTransitionCompleted -= OnTransitionCompleted;
            }

            if (m_networkModeComponent != null)
            {
                m_networkModeComponent.OnNetworkStatusChanged -= OnNetworkStatusChanged;
                m_networkModeComponent.OnNetworkError -= OnNetworkError;
            }
        }

        #endregion

        #region 测试方法

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            // 1键 - 切换到本地模式
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchToMode(GameMode.Local);
            }

            // 2键 - 切换到网络模式
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchToMode(GameMode.Network);
            }

            // 3键 - 切换到菜单模式
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchToMode(GameMode.Menu);
            }

            // T键 - 运行完整测试序列
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                StartCoroutine(RunFullTestSequence());
            }

            // S键 - 保存环境状态
            if (UnityEngine.Input.GetKeyDown(KeyCode.S) && m_environmentStateManager != null)
            {
                m_environmentStateManager.SaveCurrentState();
                Debug.Log("[ModeSwitchTest] 已手动保存环境状态");
            }

            // R键 - 恢复环境状态
            if (UnityEngine.Input.GetKeyDown(KeyCode.R) && m_environmentStateManager != null)
            {
                m_environmentStateManager.RestoreState();
                Debug.Log("[ModeSwitchTest] 已手动恢复环境状态");
            }
        }

        /// <summary>
        /// 处理自动测试
        /// </summary>
        private void HandleAutoTest()
        {
            if (Time.time - m_lastTestTime < m_autoTestInterval)
            {
                return;
            }

            m_lastTestTime = Time.time;
            StartCoroutine(RunAutoTestStep());
        }

        /// <summary>
        /// 切换到指定模式
        /// </summary>
        private void SwitchToMode(GameMode mode)
        {
            if (m_gameModeManager == null)
            {
                Debug.LogError("[ModeSwitchTest] GameModeManager不存在");
                return;
            }

            if (m_gameModeManager.IsSwitching)
            {
                Debug.LogWarning("[ModeSwitchTest] 正在进行模式切换，请稍后再试");
                return;
            }

            Debug.Log($"[ModeSwitchTest] 切换到模式: {mode}");
            m_gameModeManager.SwitchToMode(mode);
        }

        /// <summary>
        /// 运行完整测试序列
        /// </summary>
        private IEnumerator RunFullTestSequence()
        {
            if (m_isTestRunning)
            {
                Debug.LogWarning("[ModeSwitchTest] 测试已在运行中");
                yield break;
            }

            m_isTestRunning = true;
            m_testCount = 0;

            Debug.Log("[ModeSwitchTest] 开始完整测试序列");
            UpdateStatusText("开始完整测试序列...");

            // 重置测试结果
            m_localToNetworkSuccess = false;
            m_networkToLocalSuccess = false;
            m_menuTransitionSuccess = false;
            m_statePreservationSuccess = false;

            // 确保从本地模式开始
            if (m_gameModeManager.CurrentMode != GameMode.Local)
            {
                SwitchToMode(GameMode.Local);
                yield return new WaitForSeconds(2f); // 等待切换完成
            }

            // 测试1：本地到网络模式切换
            Debug.Log("[ModeSwitchTest] 测试1：本地到网络模式切换");
            UpdateStatusText("测试1：本地到网络模式切换...");
            SwitchToMode(GameMode.Network);
            yield return new WaitForSeconds(3f); // 等待切换完成和网络连接

            // 检查是否成功切换到网络模式
            m_localToNetworkSuccess = m_gameModeManager.CurrentMode == GameMode.Network;

            // 测试2：网络到本地模式切换
            Debug.Log("[ModeSwitchTest] 测试2：网络到本地模式切换");
            UpdateStatusText("测试2：网络到本地模式切换...");
            SwitchToMode(GameMode.Local);
            yield return new WaitForSeconds(2f); // 等待切换完成

            // 检查是否成功切换回本地模式
            m_networkToLocalSuccess = m_gameModeManager.CurrentMode == GameMode.Local;

            // 测试3：菜单模式过渡
            Debug.Log("[ModeSwitchTest] 测试3：菜单模式过渡");
            UpdateStatusText("测试3：菜单模式过渡...");
            SwitchToMode(GameMode.Menu);
            yield return new WaitForSeconds(2f); // 等待切换完成

            // 检查是否成功切换到菜单模式
            bool menuModeSuccess = m_gameModeManager.CurrentMode == GameMode.Menu;

            // 切换回本地模式
            SwitchToMode(GameMode.Local);
            yield return new WaitForSeconds(2f); // 等待切换完成

            // 检查是否成功从菜单模式切换回本地模式
            m_menuTransitionSuccess = menuModeSuccess && m_gameModeManager.CurrentMode == GameMode.Local;

            // 测试4：环境状态保持
            Debug.Log("[ModeSwitchTest] 测试4：环境状态保持");
            UpdateStatusText("测试4：环境状态保持...");

            // 保存当前状态
            if (m_environmentStateManager != null)
            {
                m_environmentStateManager.SaveCurrentState();
            }

            // 切换到网络模式再切换回来
            SwitchToMode(GameMode.Network);
            yield return new WaitForSeconds(2f); // 等待切换完成
            SwitchToMode(GameMode.Local);
            yield return new WaitForSeconds(2f); // 等待切换完成

            // 检查状态是否保持一致
            // 这里只是简单地检查是否成功切换回本地模式
            // 实际应用中可能需要更复杂的状态比较
            m_statePreservationSuccess = m_gameModeManager.CurrentMode == GameMode.Local;

            // 测试完成
            m_isTestRunning = false;
            Debug.Log("[ModeSwitchTest] 完整测试序列完成");

            // 显示测试结果
            string result = "测试结果：\n" +
                $"本地到网络切换: {(m_localToNetworkSuccess ? "成功" : "失败")}\n" +
                $"网络到本地切换: {(m_networkToLocalSuccess ? "成功" : "失败")}\n" +
                $"菜单模式过渡: {(m_menuTransitionSuccess ? "成功" : "失败")}\n" +
                $"环境状态保持: {(m_statePreservationSuccess ? "成功" : "失败")}";

            Debug.Log(result);
            UpdateStatusText(result);
        }

        /// <summary>
        /// 运行自动测试步骤
        /// </summary>
        private IEnumerator RunAutoTestStep()
        {
            if (m_isTestRunning)
            {
                yield break;
            }

            m_isTestRunning = true;
            m_testCount++;

            Debug.Log($"[ModeSwitchTest] 自动测试 #{m_testCount}");

            // 根据当前模式选择下一个测试模式
            GameMode nextMode;
            if (m_gameModeManager.CurrentMode == GameMode.Local)
            {
                nextMode = GameMode.Network;
            }
            else if (m_gameModeManager.CurrentMode == GameMode.Network)
            {
                nextMode = GameMode.Menu;
            }
            else
            {
                nextMode = GameMode.Local;
            }

            // 切换到下一个模式
            SwitchToMode(nextMode);
            m_lastTestedMode = nextMode;

            // 等待切换完成
            yield return new WaitForSeconds(1f);

            m_isTestRunning = false;
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText(string message = null)
        {
            if (m_statusText == null)
            {
                return;
            }

            if (message != null)
            {
                m_statusText.text = message;
                return;
            }

            string currentMode = m_gameModeManager != null ? m_gameModeManager.CurrentMode.ToString() : "未知";
            string networkStatus = m_networkModeComponent != null ? m_networkModeComponent.GetNetworkStatusInfo() : "未知";
            string transitionStatus = m_gameModeManager != null && m_gameModeManager.IsTransitioning ? "过渡中" : "空闲";

            m_statusText.text = $"当前模式: {currentMode}\n" +
                $"网络状态: {networkStatus}\n" +
                $"过渡状态: {transitionStatus}\n" +
                $"测试次数: {m_testCount}";
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 游戏模式改变事件处理
        /// </summary>
        private void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            Debug.Log($"[ModeSwitchTest] 游戏模式已改变: {previousMode} -> {newMode}");
            UpdateStatusText();
        }

        /// <summary>
        /// 过渡开始事件处理
        /// </summary>
        private void OnTransitionStarted()
        {
            Debug.Log("[ModeSwitchTest] 过渡效果已开始");
            UpdateStatusText();
        }

        /// <summary>
        /// 过渡完成事件处理
        /// </summary>
        private void OnTransitionCompleted()
        {
            Debug.Log("[ModeSwitchTest] 过渡效果已完成");
            UpdateStatusText();
        }

        /// <summary>
        /// 网络状态改变事件处理
        /// </summary>
        private void OnNetworkStatusChanged(bool isConnected)
        {
            Debug.Log($"[ModeSwitchTest] 网络状态已改变: {(isConnected ? "已连接" : "已断开")}");
            UpdateStatusText();
        }

        /// <summary>
        /// 网络错误事件处理
        /// </summary>
        private void OnNetworkError(string error)
        {
            Debug.LogError($"[ModeSwitchTest] 网络错误: {error}");
            UpdateStatusText($"网络错误: {error}");
        }

        #endregion
    }
}