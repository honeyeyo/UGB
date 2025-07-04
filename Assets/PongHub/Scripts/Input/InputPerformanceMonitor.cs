using UnityEngine;
using PongHub.Input;

namespace PongHub.Utils
{
    /// <summary>
    /// 输入系统性能监控器
    /// 用于实时监控PongHubInputManager的性能表现
    /// </summary>
    public class InputPerformanceMonitor : MonoBehaviour
    {
        [Header("监控设置")]
        [SerializeField]
        [Tooltip("Show In Game UI / 显示游戏内UI - Whether to show performance UI in game")]
        private bool m_showInGameUI = false;

        [SerializeField]
        [Tooltip("Log To Console / 记录到控制台 - Whether to log performance data to console")]
        private bool m_logToConsole = false;

        [SerializeField]
        [Tooltip("Update Interval / 更新间隔 - Interval between performance updates")]
        private float m_updateInterval = 1f;

        [Header("UI设置")]
        [SerializeField]
        [Tooltip("Toggle Key / 切换键 - Key for toggling performance display")]
        private KeyCode m_toggleKey = KeyCode.F9;

        [SerializeField]
        [Tooltip("Font Size / 字体大小 - Font size for performance display")]
        private int m_fontSize = 14;

        private float m_lastUpdateTime;
        private string m_cachedStats = "";
        private bool m_isUIVisible = false;
        private Rect m_windowRect = new Rect(10, 10, 300, 150);

        private void Start()
        {
            m_lastUpdateTime = Time.time;
        }

        private void Update()
        {
            // 检查切换键
            if (UnityEngine.Input.GetKeyDown(m_toggleKey))
            {
                m_isUIVisible = !m_isUIVisible;
            }

            // 定期更新统计信息
            if (Time.time - m_lastUpdateTime >= m_updateInterval)
            {
                UpdateStats();
                m_lastUpdateTime = Time.time;
            }
        }

        private void UpdateStats()
        {
            if (PongHubInputManager.Instance != null)
            {
                m_cachedStats = PongHubInputManager.Instance.GetPerformanceStats();

                if (m_logToConsole)
                {
                    Debug.Log($"[InputPerformanceMonitor]\n{m_cachedStats}");
                }
            }
            else
            {
                m_cachedStats = "PongHubInputManager未找到";
            }
        }

        private void OnGUI()
        {
            if (!m_showInGameUI && !m_isUIVisible) return;

            // 设置字体大小
            GUI.skin.label.fontSize = m_fontSize;
            GUI.skin.window.fontSize = m_fontSize;

            // 绘制性能窗口
            m_windowRect = GUI.Window(12345, m_windowRect, DrawPerformanceWindow, "输入系统性能监控");
        }

        private void DrawPerformanceWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // 显示统计信息
            GUILayout.Label(m_cachedStats);

            GUILayout.Space(10);

            // 控制按钮
            GUILayout.BeginHorizontal();

            if (PongHubInputManager.Instance != null)
            {
                // 性能日志开关
                bool newLogging = GUILayout.Toggle(PongHubInputManager.Instance.m_enablePerformanceLogging, "详细日志");
                if (newLogging != PongHubInputManager.Instance.m_enablePerformanceLogging)
                {
                    PongHubInputManager.Instance.m_enablePerformanceLogging = newLogging;
                }

                // 优化模式开关
                bool newOptimized = GUILayout.Toggle(PongHubInputManager.Instance.m_useOptimizedPolling, "优化模式");
                if (newOptimized != PongHubInputManager.Instance.m_useOptimizedPolling)
                {
                    PongHubInputManager.Instance.m_useOptimizedPolling = newOptimized;
                }
            }

            GUILayout.EndHorizontal();

            // 使用说明
            GUILayout.Space(5);
            GUILayout.Label($"按 {m_toggleKey} 切换显示", GUI.skin.box);

            GUILayout.EndVertical();

            // 使窗口可拖拽
            GUI.DragWindow();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 在Scene视图中显示性能信息
            if (PongHubInputManager.Instance != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"输入系统: {PongHubInputManager.Instance.LastFrameCPUTime:F1}μs",
                    new GUIStyle() {
                        normal = { textColor = Color.yellow },
                        fontSize = 12
                    }
                );
            }
        }
#endif

        /// <summary>
        /// 外部调用：显示/隐藏性能UI
        /// </summary>
        public void ToggleUI()
        {
            m_isUIVisible = !m_isUIVisible;
        }

        /// <summary>
        /// 外部调用：获取当前性能统计
        /// </summary>
        public string GetCurrentStats()
        {
            return m_cachedStats;
        }
    }
}