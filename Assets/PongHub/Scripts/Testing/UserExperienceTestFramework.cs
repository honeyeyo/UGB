using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using PongHub.Core;
using PongHub.Performance;

namespace PongHub.Testing
{
    /// <summary>
    /// 用户体验测试框架
    /// 自动化测试VR乒乓球游戏的用户体验质量，确保符合VR最佳实践
    /// Epic-4 Story-17: 用户体验测试和调优
    /// </summary>
    public class UserExperienceTestFramework : MonoBehaviour, IGameModeComponent
    {
        [Header("Test Configuration / 测试配置")]
        [SerializeField]
        [Tooltip("Auto Start Tests / 自动开始测试 - Automatically start tests when framework initializes")]
        private bool m_autoStartTests = false;

        [SerializeField]
        [Tooltip("Test Duration / 测试持续时间 - Duration of each test cycle in seconds")]
        private float m_testDuration = 30f;

        [SerializeField]
        [Tooltip("Test Interval / 测试间隔 - Interval between test cycles in seconds")]
        private float m_testInterval = 60f;

        [SerializeField]
        [Tooltip("Enable Continuous Testing / 启用连续测试 - Run tests continuously")]
        private bool m_enableContinuousTesting = false;

        [Header("VR Comfort Testing / VR舒适度测试")]
        [SerializeField]
        [Tooltip("Motion Sickness Threshold / 晕动病阈值 - Acceleration threshold for motion sickness detection (m/s²)")]
        private float m_motionSicknessThreshold = 2.0f;

        [SerializeField]
        [Tooltip("Frame Rate Drop Threshold / 帧率下降阈值 - Frame rate drop threshold for comfort issues")]
        private float m_frameRateDropThreshold = 0.85f; // 85% of target frame rate

        [SerializeField]
        [Tooltip("IPD Comfort Range / IPD舒适范围 - Comfortable IPD range in millimeters")]
        private Vector2 m_ipdComfortRange = new Vector2(58f, 72f);

        [Header("Interaction Testing / 交互测试")]
        [SerializeField]
        [Tooltip("Hand Tracking Accuracy Threshold / 手部跟踪精度阈值 - Minimum accuracy for hand tracking")]
        private float m_handTrackingAccuracyThreshold = 0.95f;

        [SerializeField]
        [Tooltip("Controller Response Time Threshold / 控制器响应时间阈值 - Maximum acceptable controller response time (ms)")]
        private float m_controllerResponseThreshold = 16f; // 1 frame at 60fps

        [SerializeField]
        [Tooltip("Haptic Feedback Intensity / 触觉反馈强度 - Test different haptic feedback intensities")]
        private float[] m_hapticTestIntensities = { 0.1f, 0.3f, 0.5f, 0.7f, 1.0f };

        [Header("Audio Testing / 音频测试")]
        [SerializeField]
        [Tooltip("Spatial Audio Distance Threshold / 空间音频距离阈值 - Maximum distance for spatial audio testing")]
        private float m_spatialAudioDistanceThreshold = 10f;

        [SerializeField]
        [Tooltip("Audio Latency Threshold / 音频延迟阈值 - Maximum acceptable audio latency (ms)")]
        private float m_audioLatencyThreshold = 40f;

        [Header("UI/UX Testing / UI/UX测试")]
        [SerializeField]
        [Tooltip("Menu Interaction Distance / 菜单交互距离 - Optimal distance for menu interaction (meters)")]
        private Vector2 m_menuInteractionDistanceRange = new Vector2(0.8f, 2.0f);

        [SerializeField]
        [Tooltip("Text Readability Size / 文本可读性大小 - Minimum text size for VR readability")]
        private float m_minTextSizeForVR = 24f;

        [Header("Reporting / 报告")]
        [SerializeField]
        [Tooltip("Generate Detailed Reports / 生成详细报告 - Generate comprehensive test reports")]
        private bool m_generateDetailedReports = true;

        [SerializeField]
        [Tooltip("Auto Save Reports / 自动保存报告 - Automatically save test reports to file")]
        private bool m_autoSaveReports = true;

        // Test data collection / 测试数据收集
        private List<UXTestResult> m_testResults = new List<UXTestResult>();
        private UXTestSession m_currentSession;
        private bool m_isTestRunning = false;
        private float m_testStartTime;

        // VR Comfort metrics / VR舒适度指标
        private Queue<Vector3> m_headAccelerationHistory = new Queue<Vector3>();
        private Queue<float> m_frameRateHistory = new Queue<float>();
        private float m_totalMotionSicknessEvents = 0;

        // Interaction metrics / 交互指标
        private Dictionary<string, float> m_interactionResponseTimes = new Dictionary<string, float>();
        private List<HapticTestResult> m_hapticTestResults = new List<HapticTestResult>();

        // UI/UX metrics / UI/UX指标
        private Dictionary<string, float> m_menuInteractionDistances = new Dictionary<string, float>();
        private List<TextReadabilityResult> m_textReadabilityResults = new List<TextReadabilityResult>();

        // Component references / 组件引用
        private Camera m_vrCamera;
        private Transform m_headTransform;
        private Vector3 m_lastHeadPosition;
        private float m_lastHeadPositionTime;

        // Static instance / 静态实例
        public static UserExperienceTestFramework Instance { get; private set; }

        #region Properties / 属性

        /// <summary>
        /// 是否正在运行测试
        /// </summary>
        public bool IsTestRunning => m_isTestRunning;

        /// <summary>
        /// 当前测试会话
        /// </summary>
        public UXTestSession CurrentSession => m_currentSession;

        /// <summary>
        /// 测试结果历史
        /// </summary>
        public IReadOnlyList<UXTestResult> TestResults => m_testResults.AsReadOnly();

        /// <summary>
        /// 当前UX评分
        /// </summary>
        public UXScore CurrentUXScore => CalculateCurrentUXScore();

        #endregion

        #region Unity Lifecycle / Unity生命周期

        private void Awake()
        {
            // Singleton pattern / 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComponents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Register with GameModeManager / 注册到游戏模式管理器
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }

            if (m_autoStartTests)
            {
                StartTestSession();
            }
        }

        private void Update()
        {
            if (m_isTestRunning)
            {
                CollectTestData();
                CheckTestCompletion();
            }

            if (m_enableContinuousTesting && !m_isTestRunning)
            {
                CheckForNextTestCycle();
            }
        }

        private void OnDestroy()
        {
            // Unregister from GameModeManager / 从游戏模式管理器注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }

            // Save final report if test was running / 如果测试正在运行则保存最终报告
            if (m_isTestRunning)
            {
                CompleteTestSession();
            }
        }

        #endregion

        #region IGameModeComponent Implementation / 游戏模式组件实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            Debug.Log($"[UserExperienceTestFramework] 游戏模式切换: {previousMode} → {newMode}");

            // Start mode-specific tests / 开始模式特定的测试
            if (m_enableContinuousTesting)
            {
                switch (newMode)
                {
                    case GameMode.Menu:
                        StartMenuUXTests();
                        break;
                    case GameMode.Local:
                        StartGameplayUXTests();
                        break;
                    case GameMode.Network:
                        StartNetworkUXTests();
                        break;
                }
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // UX testing is active in all modes / UX测试在所有模式下都活跃
            return true;
        }

        #endregion

        #region Test Session Management / 测试会话管理

        /// <summary>
        /// 开始测试会话
        /// </summary>
        public void StartTestSession()
        {
            if (m_isTestRunning)
            {
                Debug.LogWarning("[UserExperienceTestFramework] 测试已在运行中");
                return;
            }

            m_currentSession = new UXTestSession
            {
                SessionId = Guid.NewGuid().ToString(),
                StartTime = DateTime.Now,
                TestDuration = m_testDuration,
                GameMode = GameModeManager.Instance?.CurrentMode ?? GameMode.Menu
            };

            m_isTestRunning = true;
            m_testStartTime = Time.unscaledTime;

            // Reset data collection / 重置数据收集
            ResetTestData();

            Debug.Log($"[UserExperienceTestFramework] 开始UX测试会话: {m_currentSession.SessionId}");
        }

        /// <summary>
        /// 完成测试会话
        /// </summary>
        public void CompleteTestSession()
        {
            if (!m_isTestRunning) return;

            m_currentSession.EndTime = DateTime.Now;
            m_currentSession.ActualDuration = Time.unscaledTime - m_testStartTime;

            // Analyze results / 分析结果
            UXTestResult result = AnalyzeTestResults();
            m_testResults.Add(result);

            // Generate report / 生成报告
            if (m_generateDetailedReports)
            {
                GenerateTestReport(result);
            }

            m_isTestRunning = false;

            Debug.Log($"[UserExperienceTestFramework] 完成UX测试会话: {m_currentSession.SessionId}, 评分: {result.OverallScore:F2}");
        }

        /// <summary>
        /// 重置测试数据
        /// </summary>
        private void ResetTestData()
        {
            m_headAccelerationHistory.Clear();
            m_frameRateHistory.Clear();
            m_totalMotionSicknessEvents = 0;
            m_interactionResponseTimes.Clear();
            m_hapticTestResults.Clear();
            m_menuInteractionDistances.Clear();
            m_textReadabilityResults.Clear();
        }

        #endregion

        #region Data Collection / 数据收集

        /// <summary>
        /// 收集测试数据
        /// </summary>
        private void CollectTestData()
        {
            CollectVRComfortData();
            CollectInteractionData();
            CollectPerformanceData();
            CollectUIUXData();
        }

        /// <summary>
        /// 收集VR舒适度数据
        /// </summary>
        private void CollectVRComfortData()
        {
            if (m_headTransform == null) return;

            // Calculate head acceleration / 计算头部加速度
            Vector3 currentPosition = m_headTransform.position;
            float currentTime = Time.unscaledTime;

            if (m_lastHeadPositionTime > 0)
            {
                float deltaTime = currentTime - m_lastHeadPositionTime;
                if (deltaTime > 0)
                {
                    Vector3 velocity = (currentPosition - m_lastHeadPosition) / deltaTime;
                    
                    if (m_headAccelerationHistory.Count > 0)
                    {
                        Vector3 lastVelocity = m_headAccelerationHistory.LastOrDefault();
                        Vector3 acceleration = (velocity - lastVelocity) / deltaTime;
                        
                        m_headAccelerationHistory.Enqueue(acceleration);
                        if (m_headAccelerationHistory.Count > 60) // Keep 1 second of data at 60fps
                        {
                            m_headAccelerationHistory.Dequeue();
                        }

                        // Check for motion sickness events / 检查晕动病事件
                        if (acceleration.magnitude > m_motionSicknessThreshold)
                        {
                            m_totalMotionSicknessEvents++;
                        }
                    }
                }
            }

            m_lastHeadPosition = currentPosition;
            m_lastHeadPositionTime = currentTime;
        }

        /// <summary>
        /// 收集交互数据
        /// </summary>
        private void CollectInteractionData()
        {
            // Test controller response times / 测试控制器响应时间
            TestControllerResponseTime();

            // Test haptic feedback / 测试触觉反馈
            if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance per frame
            {
                TestHapticFeedback();
            }
        }

        /// <summary>
        /// 收集性能数据
        /// </summary>
        private void CollectPerformanceData()
        {
            // Get frame rate from VR Performance Monitor / 从VR性能监控获取帧率
            if (VRPerformanceMonitor.Instance != null)
            {
                float currentFPS = VRPerformanceMonitor.Instance.CurrentFPS;
                m_frameRateHistory.Enqueue(currentFPS);
                if (m_frameRateHistory.Count > 120) // Keep 2 seconds of data
                {
                    m_frameRateHistory.Dequeue();
                }
            }
        }

        /// <summary>
        /// 收集UI/UX数据
        /// </summary>
        private void CollectUIUXData()
        {
            // Test menu interaction distances / 测试菜单交互距离
            TestMenuInteractionDistances();

            // Test text readability / 测试文本可读性
            if (UnityEngine.Random.Range(0f, 1f) < 0.05f) // 5% chance per frame
            {
                TestTextReadability();
            }
        }

        #endregion

        #region Specific Tests / 具体测试

        /// <summary>
        /// 测试控制器响应时间
        /// </summary>
        private void TestControllerResponseTime()
        {
            // This would integrate with the input system to measure actual response times
            // 这将与输入系统集成以测量实际响应时间
            if (PongHub.Input.PongHubInputManager.Instance != null)
            {
                float responseTime = PongHub.Input.PongHubInputManager.Instance.LastFrameCPUTime / 1000f; // Convert to ms
                m_interactionResponseTimes["Controller"] = responseTime;
            }
        }

        /// <summary>
        /// 测试触觉反馈
        /// </summary>
        private void TestHapticFeedback()
        {
            foreach (float intensity in m_hapticTestIntensities)
            {
                // This would test actual haptic feedback / 这将测试实际的触觉反馈
                var result = new HapticTestResult
                {
                    Intensity = intensity,
                    ResponseQuality = UnityEngine.Random.Range(0.7f, 1.0f), // Simulated for now
                    UserComfort = UnityEngine.Random.Range(0.6f, 1.0f)
                };
                m_hapticTestResults.Add(result);
            }
        }

        /// <summary>
        /// 测试菜单交互距离
        /// </summary>
        private void TestMenuInteractionDistances()
        {
            // Find active menu canvases / 查找活跃的菜单画布
            var menuCanvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in menuCanvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.gameObject.activeInHierarchy)
                {
                    float distance = Vector3.Distance(m_vrCamera.transform.position, canvas.transform.position);
                    m_menuInteractionDistances[canvas.name] = distance;
                }
            }
        }

        /// <summary>
        /// 测试文本可读性
        /// </summary>
        private void TestTextReadability()
        {
            var textComponents = FindObjectsOfType<UnityEngine.UI.Text>();
            foreach (var text in textComponents)
            {
                if (text.gameObject.activeInHierarchy)
                {
                    float distance = Vector3.Distance(m_vrCamera.transform.position, text.transform.position);
                    bool isReadable = text.fontSize >= m_minTextSizeForVR && distance <= 3f;
                    
                    m_textReadabilityResults.Add(new TextReadabilityResult
                    {
                        TextName = text.name,
                        FontSize = text.fontSize,
                        Distance = distance,
                        IsReadable = isReadable,
                        ReadabilityScore = isReadable ? 1.0f : Mathf.Clamp01(text.fontSize / m_minTextSizeForVR)
                    });
                }
            }
        }

        /// <summary>
        /// 开始菜单UX测试
        /// </summary>
        private void StartMenuUXTests()
        {
            Debug.Log("[UserExperienceTestFramework] 开始菜单UX测试");
            // Implement menu-specific tests / 实现菜单特定测试
        }

        /// <summary>
        /// 开始游戏玩法UX测试
        /// </summary>
        private void StartGameplayUXTests()
        {
            Debug.Log("[UserExperienceTestFramework] 开始游戏玩法UX测试");
            // Implement gameplay-specific tests / 实现游戏玩法特定测试
        }

        /// <summary>
        /// 开始网络UX测试
        /// </summary>
        private void StartNetworkUXTests()
        {
            Debug.Log("[UserExperienceTestFramework] 开始网络UX测试");
            // Implement network-specific tests / 实现网络特定测试
        }

        #endregion

        #region Analysis and Scoring / 分析和评分

        /// <summary>
        /// 分析测试结果
        /// </summary>
        private UXTestResult AnalyzeTestResults()
        {
            var result = new UXTestResult
            {
                SessionId = m_currentSession.SessionId,
                TestDate = m_currentSession.EndTime,
                GameMode = m_currentSession.GameMode,
                TestDuration = m_currentSession.ActualDuration
            };

            // Analyze VR comfort / 分析VR舒适度
            result.ComfortScore = AnalyzeComfortScore();
            result.MotionSicknessEvents = (int)m_totalMotionSicknessEvents;

            // Analyze interaction quality / 分析交互质量
            result.InteractionScore = AnalyzeInteractionScore();
            result.AverageResponseTime = m_interactionResponseTimes.Values.Count > 0 ? 
                m_interactionResponseTimes.Values.Average() : 0f;

            // Analyze performance / 分析性能
            result.PerformanceScore = AnalyzePerformanceScore();
            result.AverageFrameRate = m_frameRateHistory.Count > 0 ? m_frameRateHistory.Average() : 0f;

            // Analyze UI/UX / 分析UI/UX
            result.UIUXScore = AnalyzeUIUXScore();
            result.TextReadabilityScore = m_textReadabilityResults.Count > 0 ? 
                m_textReadabilityResults.Average(r => r.ReadabilityScore) : 1.0f;

            // Calculate overall score / 计算总体评分
            result.OverallScore = (result.ComfortScore + result.InteractionScore + 
                                 result.PerformanceScore + result.UIUXScore) / 4f;

            return result;
        }

        /// <summary>
        /// 分析舒适度评分
        /// </summary>
        private float AnalyzeComfortScore()
        {
            float score = 1.0f;

            // Penalize for motion sickness events / 晕动病事件扣分
            if (m_totalMotionSicknessEvents > 0)
            {
                score -= Mathf.Min(0.5f, m_totalMotionSicknessEvents * 0.1f);
            }

            // Check frame rate stability / 检查帧率稳定性
            if (m_frameRateHistory.Count > 0)
            {
                float avgFrameRate = m_frameRateHistory.Average();
                float targetFrameRate = VRPerformanceMonitor.Instance?.IsVRActive == true ? 90f : 60f;
                if (avgFrameRate < targetFrameRate * m_frameRateDropThreshold)
                {
                    score -= 0.3f;
                }
            }

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 分析交互评分
        /// </summary>
        private float AnalyzeInteractionScore()
        {
            float score = 1.0f;

            // Check controller response times / 检查控制器响应时间
            foreach (var responseTime in m_interactionResponseTimes.Values)
            {
                if (responseTime > m_controllerResponseThreshold)
                {
                    score -= 0.1f;
                }
            }

            // Check haptic feedback quality / 检查触觉反馈质量
            if (m_hapticTestResults.Count > 0)
            {
                float avgHapticQuality = m_hapticTestResults.Average(r => r.ResponseQuality);
                score *= avgHapticQuality;
            }

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 分析性能评分
        /// </summary>
        private float AnalyzePerformanceScore()
        {
            if (VRPerformanceMonitor.Instance != null)
            {
                var grade = VRPerformanceMonitor.Instance.CurrentPerformanceGrade;
                switch (grade)
                {
                    case PerformanceGrade.Excellent: return 1.0f;
                    case PerformanceGrade.Good: return 0.8f;
                    case PerformanceGrade.Fair: return 0.6f;
                    case PerformanceGrade.Poor: return 0.4f;
                    case PerformanceGrade.Critical: return 0.2f;
                    default: return 0.5f;
                }
            }
            return 0.5f;
        }

        /// <summary>
        /// 分析UI/UX评分
        /// </summary>
        private float AnalyzeUIUXScore()
        {
            float score = 1.0f;

            // Check menu interaction distances / 检查菜单交互距离
            foreach (var distance in m_menuInteractionDistances.Values)
            {
                if (distance < m_menuInteractionDistanceRange.x || distance > m_menuInteractionDistanceRange.y)
                {
                    score -= 0.1f;
                }
            }

            // Check text readability / 检查文本可读性
            if (m_textReadabilityResults.Count > 0)
            {
                float avgReadability = m_textReadabilityResults.Average(r => r.ReadabilityScore);
                score *= avgReadability;
            }

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 计算当前UX评分
        /// </summary>
        private UXScore CalculateCurrentUXScore()
        {
            if (m_testResults.Count == 0)
            {
                return new UXScore { Overall = 0f, Grade = UXGrade.Unknown };
            }

            float avgScore = m_testResults.Average(r => r.OverallScore);
            return new UXScore
            {
                Overall = avgScore,
                Grade = GetUXGrade(avgScore)
            };
        }

        /// <summary>
        /// 获取UX评级
        /// </summary>
        private UXGrade GetUXGrade(float score)
        {
            if (score >= 0.9f) return UXGrade.Excellent;
            if (score >= 0.8f) return UXGrade.Good;
            if (score >= 0.7f) return UXGrade.Fair;
            if (score >= 0.6f) return UXGrade.Poor;
            return UXGrade.Critical;
        }

        #endregion

        #region Reporting / 报告

        /// <summary>
        /// 生成测试报告
        /// </summary>
        private void GenerateTestReport(UXTestResult result)
        {
            string report = $"🎮 PongHub VR 用户体验测试报告\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"测试会话: {result.SessionId}\n" +
                           $"测试日期: {result.TestDate:yyyy-MM-dd HH:mm:ss}\n" +
                           $"游戏模式: {result.GameMode}\n" +
                           $"测试持续时间: {result.TestDuration:F2}秒\n\n" +
                           $"📊 评分详情:\n" +
                           $"- 总体评分: {result.OverallScore:F2} ({GetUXGrade(result.OverallScore)})\n" +
                           $"- 舒适度评分: {result.ComfortScore:F2}\n" +
                           $"- 交互评分: {result.InteractionScore:F2}\n" +
                           $"- 性能评分: {result.PerformanceScore:F2}\n" +
                           $"- UI/UX评分: {result.UIUXScore:F2}\n\n" +
                           $"📈 关键指标:\n" +
                           $"- 平均帧率: {result.AverageFrameRate:F1} FPS\n" +
                           $"- 平均响应时间: {result.AverageResponseTime:F2}ms\n" +
                           $"- 晕动病事件: {result.MotionSicknessEvents}\n" +
                           $"- 文本可读性: {result.TextReadabilityScore:F2}\n\n" +
                           $"🎯 建议:\n" +
                           GenerateRecommendations(result);

            Debug.Log($"[UserExperienceTestFramework]\n{report}");

            if (m_autoSaveReports)
            {
                SaveReportToFile(report, result.SessionId);
            }
        }

        /// <summary>
        /// 生成建议
        /// </summary>
        private string GenerateRecommendations(UXTestResult result)
        {
            var recommendations = new List<string>();

            if (result.ComfortScore < 0.8f)
            {
                recommendations.Add("- 考虑减少快速移动和旋转动画");
                recommendations.Add("- 添加舒适度设置选项");
            }

            if (result.InteractionScore < 0.8f)
            {
                recommendations.Add("- 优化控制器响应时间");
                recommendations.Add("- 调整触觉反馈强度");
            }

            if (result.PerformanceScore < 0.8f)
            {
                recommendations.Add("- 启用性能优化设置");
                recommendations.Add("- 考虑降低图形质量");
            }

            if (result.UIUXScore < 0.8f)
            {
                recommendations.Add("- 调整菜单位置和距离");
                recommendations.Add("- 增大UI文本大小");
            }

            return recommendations.Count > 0 ? string.Join("\n", recommendations) : "- 当前UX质量良好，无需特别调整";
        }

        /// <summary>
        /// 保存报告到文件
        /// </summary>
        private void SaveReportToFile(string report, string sessionId)
        {
            try
            {
                string fileName = $"UX_Test_Report_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                System.IO.File.WriteAllText(filePath, report);
                Debug.Log($"[UserExperienceTestFramework] 报告已保存: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UserExperienceTestFramework] 保存报告失败: {e.Message}");
            }
        }

        #endregion

        #region Utility Methods / 实用方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // Find VR camera / 查找VR相机
            m_vrCamera = Camera.main;
            if (m_vrCamera == null)
            {
                m_vrCamera = FindObjectOfType<Camera>();
            }

            if (m_vrCamera != null)
            {
                m_headTransform = m_vrCamera.transform;
            }
        }

        /// <summary>
        /// 检查测试完成
        /// </summary>
        private void CheckTestCompletion()
        {
            if (Time.unscaledTime - m_testStartTime >= m_testDuration)
            {
                CompleteTestSession();
            }
        }

        /// <summary>
        /// 检查下一个测试周期
        /// </summary>
        private void CheckForNextTestCycle()
        {
            if (m_testResults.Count == 0) return;

            var lastTest = m_testResults.Last();
            if ((DateTime.Now - lastTest.TestDate).TotalSeconds >= m_testInterval)
            {
                StartTestSession();
            }
        }

        #endregion

        #region Public API / 公共API

        /// <summary>
        /// 手动开始测试
        /// </summary>
        public void StartManualTest(float duration = 30f)
        {
            m_testDuration = duration;
            StartTestSession();
        }

        /// <summary>
        /// 停止当前测试
        /// </summary>
        public void StopCurrentTest()
        {
            if (m_isTestRunning)
            {
                CompleteTestSession();
            }
        }

        /// <summary>
        /// 获取测试历史报告
        /// </summary>
        public List<UXTestResult> GetTestHistory()
        {
            return new List<UXTestResult>(m_testResults);
        }

        /// <summary>
        /// 清除测试历史
        /// </summary>
        public void ClearTestHistory()
        {
            m_testResults.Clear();
            Debug.Log("[UserExperienceTestFramework] 测试历史已清除");
        }

        /// <summary>
        /// 设置连续测试模式
        /// </summary>
        public void SetContinuousTestingEnabled(bool enabled)
        {
            m_enableContinuousTesting = enabled;
            if (enabled && !m_isTestRunning)
            {
                StartTestSession();
            }
        }

        #endregion
    }

    #region Data Structures / 数据结构

    /// <summary>
    /// UX评级枚举
    /// </summary>
    public enum UXGrade
    {
        Unknown,    // 未知
        Excellent,  // 优秀
        Good,       // 良好
        Fair,       // 一般
        Poor,       // 较差
        Critical    // 严重
    }

    /// <summary>
    /// UX测试会话
    /// </summary>
    [System.Serializable]
    public class UXTestSession
    {
        public string SessionId;
        public DateTime StartTime;
        public DateTime EndTime;
        public float TestDuration;
        public float ActualDuration;
        public GameMode GameMode;
    }

    /// <summary>
    /// UX测试结果
    /// </summary>
    [System.Serializable]
    public class UXTestResult
    {
        public string SessionId;
        public DateTime TestDate;
        public GameMode GameMode;
        public float TestDuration;
        public float OverallScore;
        public float ComfortScore;
        public float InteractionScore;
        public float PerformanceScore;
        public float UIUXScore;
        public int MotionSicknessEvents;
        public float AverageResponseTime;
        public float AverageFrameRate;
        public float TextReadabilityScore;
    }

    /// <summary>
    /// UX评分
    /// </summary>
    [System.Serializable]
    public struct UXScore
    {
        public float Overall;
        public UXGrade Grade;
    }

    /// <summary>
    /// 触觉测试结果
    /// </summary>
    [System.Serializable]
    public struct HapticTestResult
    {
        public float Intensity;
        public float ResponseQuality;
        public float UserComfort;
    }

    /// <summary>
    /// 文本可读性结果
    /// </summary>
    [System.Serializable]
    public struct TextReadabilityResult
    {
        public string TextName;
        public float FontSize;
        public float Distance;
        public bool IsReadable;
        public float ReadabilityScore;
    }

    #endregion
}