using UnityEngine;
using PongHub.App;
using System.Collections;

namespace PongHub.Core
{
    /// <summary>
    /// 启动控制器
    /// 负责游戏的启动流程优化，直接进入Local模式而不是MainMenu
    /// 提供向后兼容性支持
    /// </summary>
    public class StartupController : MonoBehaviour
    {
        [Header("Startup Configuration / 启动配置")]
        [SerializeField]
        [Tooltip("Direct Local Start / 直接本地启动 - Enable automatic local mode startup without main menu")]
        private bool m_enableDirectLocalStart = true;

        [SerializeField]
        [Tooltip("Skip Main Menu / 跳过主菜单 - Skip main menu and go directly to game mode")]
        private bool m_skipMainMenu = true;

        [SerializeField]
        [Tooltip("Startup Delay / 启动延迟 - Delay in seconds before starting the game")]
        private float m_startupDelay = 1.0f;

        [Header("Debug Settings / 调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for startup operations")]
        private bool m_debugMode = false;

        [Header("Backward Compatibility / 向后兼容性")]
        [SerializeField]
        [Tooltip("Fallback to Main Menu / 回退到主菜单 - Fallback to main menu if direct startup fails")]
        private bool m_fallbackToMainMenu = true;

        private bool m_hasStarted = false;

        #region Unity 生命周期

        private void Start()
        {
            if (m_debugMode)
            {
                Debug.Log("[StartupController] 启动控制器初始化");
            }

            // 延迟启动以确保所有系统初始化完成
            StartCoroutine(DelayedStartup());
        }

        #endregion

        #region 启动流程

        /// <summary>
        /// 延迟启动协程
        /// </summary>
        private IEnumerator DelayedStartup()
        {
            // 等待启动延迟
            yield return new WaitForSeconds(m_startupDelay);

            // 检查是否已经启动过
            if (m_hasStarted)
            {
                yield break;
            }

            m_hasStarted = true;

            if (m_debugMode)
            {
                Debug.Log("[StartupController] 开始启动流程");
            }

            // 执行启动流程
            if (m_enableDirectLocalStart && m_skipMainMenu)
            {
                yield return StartCoroutine(StartDirectLocalMode());
            }
            else
            {
                yield return StartCoroutine(StartTraditionalFlow());
            }
        }

        /// <summary>
        /// 直接启动本地模式
        /// </summary>
        private IEnumerator StartDirectLocalMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[StartupController] 启动直接本地模式");
            }

            // 执行实际的启动逻辑
            yield return StartCoroutine(DoStartDirectLocalMode());
        }

        /// <summary>
        /// 执行直接启动到Local模式的实际逻辑
        /// </summary>
        private IEnumerator DoStartDirectLocalMode()
        {
            bool success = false;
            System.Exception caughtException = null;

            // 先执行可能失败的异步操作
            yield return StartCoroutine(TryExecuteDirectLocalModeSteps(result => success = result.success, ex => caughtException = ex));

            // 处理异常情况
            if (!success && caughtException != null)
            {
                Debug.LogError($"[StartupController] 直接启动Local模式失败: {caughtException.Message}");

                if (m_fallbackToMainMenu)
                {
                    Debug.Log("[StartupController] 回退到传统启动流程");
                    yield return StartCoroutine(StartTraditionalFlow());
                }
            }
        }

        /// <summary>
        /// 尝试执行直接本地模式启动步骤
        /// </summary>
        private IEnumerator TryExecuteDirectLocalModeSteps(System.Action<(bool success, System.Exception exception)> onComplete, System.Action<System.Exception> onError)
        {
            // 等待GameModeManager初始化和所有组件准备就绪
            yield return StartCoroutine(WaitForGameModeManagerAndInitialize());

            // 执行同步操作，如果出错通过回调返回
            try
            {
                // 直接切换到Local模式
                GameModeManager.Instance.SwitchToMode(GameMode.Local);

                if (m_debugMode)
                {
                    Debug.Log("[StartupController] 成功启动到Local模式");
                }

                // 隐藏加载界面（如果存在）
                HideLoadingUI();

                // 通知其他系统启动完成
                NotifyStartupComplete();

                onComplete?.Invoke((true, null));
            }
            catch (System.Exception e)
            {
                onError?.Invoke(e);
                onComplete?.Invoke((false, e));
            }
        }

        /// <summary>
        /// 等待GameModeManager初始化并准备组件
        /// </summary>
        private IEnumerator WaitForGameModeManagerAndInitialize()
        {
            // 确保GameModeManager已初始化
            yield return new WaitUntil(() => GameModeManager.Instance != null);

            // 等待一帧确保所有组件都已准备就绪
            yield return null;
        }

        /// <summary>
        /// 传统启动流程（回退方案）
        /// </summary>
        private IEnumerator StartTraditionalFlow()
        {
            if (m_debugMode)
            {
                Debug.Log("[StartupController] 启动传统流程");
            }

            // 执行实际的传统启动逻辑
            yield return StartCoroutine(DoStartTraditionalFlow());
        }

        /// <summary>
        /// 执行传统启动流程的实际逻辑
        /// </summary>
        private IEnumerator DoStartTraditionalFlow()
        {
            // 执行传统启动流程的步骤
            yield return StartCoroutine(TryExecuteTraditionalFlowSteps());
        }

        /// <summary>
        /// 尝试执行传统启动流程的步骤
        /// </summary>
        private IEnumerator TryExecuteTraditionalFlowSteps()
        {
            // 等待GameModeManager初始化
            yield return new WaitUntil(() => GameModeManager.Instance != null);

            // 执行同步操作
            try
            {
                // 检查是否需要加载MainMenu场景
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (currentScene.name != "MainMenu")
                {
                    if (m_debugMode)
                    {
                        Debug.Log("[StartupController] 当前不在MainMenu场景，切换到Menu模式");
                    }

                    // 切换到Menu模式
                    GameModeManager.Instance.SwitchToMode(GameMode.Menu);
                }

                // 隐藏加载界面
                HideLoadingUI();

                // 通知启动完成
                NotifyStartupComplete();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StartupController] 传统启动流程失败: {e.Message}");
            }
        }

        /// <summary>
        /// 仅等待GameModeManager初始化
        /// </summary>
        private IEnumerator WaitForGameModeManagerOnly()
        {
            // 确保GameModeManager已初始化
            yield return new WaitUntil(() => GameModeManager.Instance != null);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 隐藏加载界面
        /// </summary>
        private void HideLoadingUI()
        {
            try
            {
                // 查找并隐藏LoadingCanvas
                var loadingCanvas = GameObject.FindWithTag("LoadingUI");
                if (loadingCanvas != null)
                {
                    loadingCanvas.SetActive(false);
                    if (m_debugMode)
                    {
                        Debug.Log("[StartupController] 已隐藏加载界面");
                    }
                }

                // 查找LoadingCanvas组件并隐藏
                var loadingCanvasComponent = FindObjectOfType<Canvas>();
                if (loadingCanvasComponent != null && loadingCanvasComponent.name.Contains("Loading"))
                {
                    loadingCanvasComponent.gameObject.SetActive(false);
                }

                // 如果使用OVR屏幕淡入效果
                if (OVRScreenFade.instance != null)
                {
                    OVRScreenFade.instance.FadeIn();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StartupController] 隐藏加载界面时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 通知启动完成
        /// </summary>
        private void NotifyStartupComplete()
        {
            // 发送启动完成事件
            var phApplication = PHApplication.Instance;
            if (phApplication != null)
            {
                // 可以在这里通知PHApplication启动完成
                if (m_debugMode)
                {
                    Debug.Log("[StartupController] 已通知PHApplication启动完成");
                }
            }

            if (m_debugMode)
            {
                Debug.Log("[StartupController] 启动流程完成");
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 强制重新启动到Local模式
        /// </summary>
        public void ForceRestartToLocalMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[StartupController] 强制重新启动到Local模式");
            }

            m_hasStarted = false;
            StartCoroutine(StartDirectLocalMode());
        }

        /// <summary>
        /// 切换到菜单模式
        /// </summary>
        public void SwitchToMenuMode()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SwitchToMode(GameMode.Menu);
            }
        }

        /// <summary>
        /// 检查是否已完成启动
        /// </summary>
        public bool HasStarted => m_hasStarted;

        #endregion

        #region 调试方法

        [ContextMenu("重新启动到Local模式")]
        private void DebugRestartToLocal()
        {
            ForceRestartToLocalMode();
        }

        [ContextMenu("切换到菜单模式")]
        private void DebugSwitchToMenu()
        {
            SwitchToMenuMode();
        }

        [ContextMenu("输出启动状态")]
        private void DebugPrintStatus()
        {
            Debug.Log($"[StartupController] 启动状态: HasStarted={m_hasStarted}, " +
                     $"DirectLocalStart={m_enableDirectLocalStart}, " +
                     $"SkipMainMenu={m_skipMainMenu}");
        }

        #endregion
    }
}