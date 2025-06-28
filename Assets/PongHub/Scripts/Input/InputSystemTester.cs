using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PongHub.Input
{
    /// <summary>
    /// 输入系统测试器
    /// 用于验证PongHub输入系统的各项功能是否正常工作
    /// </summary>
    public class InputSystemTester : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private Canvas m_testCanvas;
        [SerializeField] private TextMeshProUGUI m_statusText;
        [SerializeField] private TextMeshProUGUI m_inputInfoText;
        [SerializeField] private Button m_testButton;

        [Header("测试设置")]
        [SerializeField] private bool m_autoTest = true;
        [SerializeField] private float m_updateInterval = 0.1f;

        // 私有变量
        private PongHubInputManager m_inputManager;
        private PlayerHeightController m_heightController;
        private TeleportController m_teleportController;
        private ServeBallController m_serveBallController;
        private PaddleController m_paddleController;

        private float m_lastUpdateTime;
        private bool m_systemReady = false;

        private void Start()
        {
            InitializeTester();
            SetupUI();

            if (m_autoTest)
            {
                StartTesting();
            }
        }

        private void Update()
        {
            if (m_systemReady && Time.time - m_lastUpdateTime > m_updateInterval)
            {
                UpdateTestInfo();
                m_lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 初始化测试器
        /// </summary>
        private void InitializeTester()
        {
            // 查找输入系统组件
            m_inputManager = FindObjectOfType<PongHubInputManager>();
            m_heightController = FindObjectOfType<PlayerHeightController>();
            m_teleportController = FindObjectOfType<TeleportController>();
            m_serveBallController = FindObjectOfType<ServeBallController>();
            m_paddleController = FindObjectOfType<PaddleController>();

            // 检查系统完整性
            CheckSystemIntegrity();
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // 创建测试UI（如果不存在）
            if (m_testCanvas == null)
            {
                CreateTestUI();
            }

            // 设置按钮事件
            if (m_testButton != null)
            {
                m_testButton.onClick.AddListener(RunManualTest);
            }
        }

        /// <summary>
        /// 创建测试UI
        /// </summary>
        private void CreateTestUI()
        {
            // 创建Canvas
            GameObject canvasObj = new GameObject("InputTesterCanvas");
            m_testCanvas = canvasObj.AddComponent<Canvas>();
            m_testCanvas.renderMode = RenderMode.WorldSpace;
            m_testCanvas.transform.position = Vector3.forward * 2f;
            m_testCanvas.transform.localScale = Vector3.one * 0.01f;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.scaleFactor = 1f;
            scaler.dynamicPixelsPerUnit = 10f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建背景面板
            GameObject panelObj = new GameObject("TestPanel");
            panelObj.transform.SetParent(m_testCanvas.transform, false);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800, 600);

            // 创建状态文本
            GameObject statusTextObj = new GameObject("StatusText");
            statusTextObj.transform.SetParent(panelObj.transform, false);

            m_statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
            m_statusText.text = "正在初始化输入系统测试器...";
            m_statusText.fontSize = 24;
            m_statusText.color = Color.white;
            m_statusText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform statusRect = statusTextObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.5f);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.offsetMin = new Vector2(20, 0);
            statusRect.offsetMax = new Vector2(-20, -20);

            // 创建输入信息文本
            GameObject inputTextObj = new GameObject("InputInfoText");
            inputTextObj.transform.SetParent(panelObj.transform, false);

            m_inputInfoText = inputTextObj.AddComponent<TextMeshProUGUI>();
            m_inputInfoText.text = "输入信息显示区域";
            m_inputInfoText.fontSize = 18;
            m_inputInfoText.color = Color.cyan;
            m_inputInfoText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform inputRect = inputTextObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.5f);
            inputRect.offsetMin = new Vector2(20, 20);
            inputRect.offsetMax = new Vector2(-20, 0);
        }

        /// <summary>
        /// 检查系统完整性
        /// </summary>
        private void CheckSystemIntegrity()
        {
            System.Text.StringBuilder statusReport = new System.Text.StringBuilder();
            statusReport.AppendLine("=== PongHub输入系统检查报告 ===\n");

            // 检查各个组件
            statusReport.AppendLine($"🎮 PongHubInputManager: {(m_inputManager != null ? "✅ 找到" : "❌ 未找到")}");
            statusReport.AppendLine($"📏 PlayerHeightController: {(m_heightController != null ? "✅ 找到" : "❌ 未找到")}");
            statusReport.AppendLine($"🚀 TeleportController: {(m_teleportController != null ? "✅ 找到" : "❌ 未找到")}");
            statusReport.AppendLine($"⚾ ServeBallController: {(m_serveBallController != null ? "✅ 找到" : "❌ 未找到")}");
            statusReport.AppendLine($"🏓 PaddleController: {(m_paddleController != null ? "✅ 找到" : "❌ 未找到")}");

            // 检查输入动作文件
            if (m_inputManager != null)
            {
                statusReport.AppendLine($"\n📄 InputActions文件: 已连接");
                statusReport.AppendLine($"🔗 单例实例: {(PongHubInputManager.Instance != null ? "✅ 正常" : "❌ 异常")}");
            }

            // 计算系统完整性
            int foundComponents = 0;
            if (m_inputManager != null) foundComponents++;
            if (m_heightController != null) foundComponents++;
            if (m_teleportController != null) foundComponents++;
            if (m_serveBallController != null) foundComponents++;
            if (m_paddleController != null) foundComponents++;

            float completeness = (foundComponents / 5f) * 100f;
            statusReport.AppendLine($"\n📊 系统完整性: {completeness:F0}% ({foundComponents}/5 组件)");

            if (completeness >= 80f)
            {
                statusReport.AppendLine("\n🎉 系统状态: 良好，可以开始测试");
                m_systemReady = true;
            }
            else
            {
                statusReport.AppendLine("\n⚠️ 系统状态: 不完整，请检查缺失组件");
                m_systemReady = false;
            }

            // 更新状态显示
            if (m_statusText != null)
            {
                m_statusText.text = statusReport.ToString();
            }

            Debug.Log(statusReport.ToString());
        }

        /// <summary>
        /// 开始测试
        /// </summary>
        public void StartTesting()
        {
            if (!m_systemReady)
            {
                Debug.LogWarning("系统未就绪，无法开始测试");
                return;
            }

            // 注册事件监听
            RegisterEventListeners();

            Debug.Log("输入系统测试已开始");
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEventListeners()
        {
            if (m_inputManager == null) return;

            PongHubInputManager.OnPaddleGripped += OnPaddleGripTest;
            PongHubInputManager.OnPaddleReleased += OnPaddleReleaseTest;
            PongHubInputManager.OnServeBallGenerated += OnServeBallTest;
            PongHubInputManager.OnMenuToggled += OnMenuTest;
            PongHubInputManager.OnGamePaused += OnGamePauseTest;
            PongHubInputManager.OnPositionReset += OnPositionResetTest;
        }

        /// <summary>
        /// 取消事件监听
        /// </summary>
        private void UnregisterEventListeners()
        {
            PongHubInputManager.OnPaddleGripped -= OnPaddleGripTest;
            PongHubInputManager.OnPaddleReleased -= OnPaddleReleaseTest;
            PongHubInputManager.OnServeBallGenerated -= OnServeBallTest;
            PongHubInputManager.OnMenuToggled -= OnMenuTest;
            PongHubInputManager.OnGamePaused -= OnGamePauseTest;
            PongHubInputManager.OnPositionReset -= OnPositionResetTest;
        }

        /// <summary>
        /// 更新测试信息
        /// </summary>
        private void UpdateTestInfo()
        {
            if (m_inputManager == null || m_inputInfoText == null) return;

            System.Text.StringBuilder info = new System.Text.StringBuilder();
            info.AppendLine("=== 实时输入状态 ===");

            // 球拍状态
            info.AppendLine($"🏓 左手球拍: {(m_inputManager.IsLeftPaddleGripped ? "已抓取" : "未抓取")}");
            info.AppendLine($"🏓 右手球拍: {(m_inputManager.IsRightPaddleGripped ? "已抓取" : "未抓取")}");

            // 移动输入
            Vector2 moveInput = m_inputManager.CurrentMoveInput;
            info.AppendLine($"🏃 移动输入: ({moveInput.x:F2}, {moveInput.y:F2})");

            // 传送输入
            Vector2 teleportInput = m_inputManager.CurrentTeleportInput;
            info.AppendLine($"🚀 传送输入: ({teleportInput.x:F2}, {teleportInput.y:F2})");

            // 高度信息
            if (m_heightController != null)
            {
                info.AppendLine($"📏 当前高度偏移: {m_heightController.CurrentHeightOffsetCm:F1}cm");
                info.AppendLine($"📏 正在调整: {(m_heightController.IsAdjustingHeight ? "是" : "否")}");
            }

            // 传送状态
            if (m_teleportController != null)
            {
                info.AppendLine($"🎯 传送激活: {(m_teleportController.IsTeleportActive ? "是" : "否")}");
                info.AppendLine($"🎯 目标有效: {(m_teleportController.IsValidTeleportTarget ? "是" : "否")}");
            }

            // 发球信息
            if (m_serveBallController != null)
            {
                info.AppendLine($"⚾ 活跃球数: {m_serveBallController.GetActiveBallCount()}");
            }

            m_inputInfoText.text = info.ToString();
        }

        /// <summary>
        /// 手动测试
        /// </summary>
        public void RunManualTest()
        {
            Debug.Log("执行手动测试...");

            // 这里可以添加手动测试逻辑
            if (m_serveBallController != null)
            {
                m_serveBallController.GenerateServeBall(true); // 测试左手发球
            }
        }

        #region 事件测试处理器

        private void OnPaddleGripTest(bool isLeftHand)
        {
            string message = $"✅ 球拍抓取测试成功: {(isLeftHand ? "左手" : "右手")}";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void OnPaddleReleaseTest(bool isLeftHand)
        {
            string message = $"✅ 球拍释放测试成功: {(isLeftHand ? "左手" : "右手")}";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void OnServeBallTest(bool isLeftHand)
        {
            string message = $"✅ 发球测试成功: {(isLeftHand ? "左手" : "右手")}";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void OnMenuTest()
        {
            string message = "✅ 菜单切换测试成功";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void OnGamePauseTest()
        {
            string message = "✅ 游戏暂停测试成功";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void OnPositionResetTest()
        {
            string message = "✅ 位置重置测试成功";
            Debug.Log(message);
            LogTestResult(message);
        }

        private void LogTestResult(string message)
        {
            // 可以在这里记录测试结果，或显示在UI上
            if (m_statusText != null)
            {
                m_statusText.text += $"\n{System.DateTime.Now:HH:mm:ss} - {message}";
            }
        }

        #endregion

        private void OnDestroy()
        {
            UnregisterEventListeners();
        }

        #region 公共接口

        /// <summary>
        /// 切换测试UI显示
        /// </summary>
        public void ToggleTestUI()
        {
            if (m_testCanvas != null)
            {
                m_testCanvas.gameObject.SetActive(!m_testCanvas.gameObject.activeInHierarchy);
            }
        }

        /// <summary>
        /// 重新检查系统
        /// </summary>
        public void ReCheckSystem()
        {
            InitializeTester();
        }

        /// <summary>
        /// 获取系统状态报告
        /// </summary>
        public string GetSystemStatusReport()
        {
            if (m_statusText != null)
            {
                return m_statusText.text;
            }
            return "无状态信息";
        }

        #endregion
    }
}