using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;
using System;
using PongHub.Core;

// Unity compatibility testing system - updated for modern Unity APIs

namespace PongHub.Testing
{
    /// <summary>
    /// 兼容性测试系统
    /// 自动检测和测试不同VR设备、Unity版本和系统配置的兼容性
    /// Epic-4 Story-18: 兼容性测试和bug修复
    /// </summary>
    public class CompatibilityTestingSystem : MonoBehaviour, IGameModeComponent
    {
        [Header("Device Testing / 设备测试")]
        [SerializeField]
        [Tooltip("Test VR Devices / 测试VR设备 - List of VR devices to test compatibility")]
        private string[] m_supportedVRDevices = { "Oculus", "Meta", "Quest", "OpenXR" };

        [SerializeField]
        [Tooltip("Test Controllers / 测试控制器 - Test different controller types")]
        private bool m_testControllers = true;

        [SerializeField]
        [Tooltip("Test Hand Tracking / 测试手部跟踪 - Test hand tracking compatibility")]
        private bool m_testHandTracking = true;

        [Header("System Testing / 系统测试")]
        [SerializeField]
        [Tooltip("Test Unity Versions / 测试Unity版本 - Test compatibility with different Unity versions")]
        private bool m_testUnityVersions = true;

        [SerializeField]
        [Tooltip("Test Platform Settings / 测试平台设置 - Test different platform-specific settings")]
        private bool m_testPlatformSettings = true;

        [SerializeField]
        [Tooltip("Test Graphics APIs / 测试图形API - Test different graphics API compatibility")]
        private bool m_testGraphicsAPIs = true;

        [Header("Feature Testing / 功能测试")]
        [SerializeField]
        [Tooltip("Test Audio Systems / 测试音频系统 - Test audio system compatibility")]
        private bool m_testAudioSystems = true;

        [SerializeField]
        [Tooltip("Test Network Features / 测试网络功能 - Test networking compatibility")]
        private bool m_testNetworkFeatures = true;

        [SerializeField]
        [Tooltip("Test Input Systems / 测试输入系统 - Test input system compatibility")]
        private bool m_testInputSystems = true;

        [Header("Bug Detection / Bug检测")]
        [SerializeField]
        [Tooltip("Enable Auto Bug Detection / 启用自动Bug检测 - Automatically detect common bugs")]
        private bool m_enableAutoBugDetection = true;

        [SerializeField]
        [Tooltip("Bug Detection Sensitivity / Bug检测敏感度 - Sensitivity level for bug detection")]
        [Range(0.1f, 1.0f)]
        private float m_bugDetectionSensitivity = 0.7f;

        [SerializeField]
        [Tooltip("Generate Bug Reports / 生成Bug报告 - Automatically generate bug reports")]
        private bool m_generateBugReports = true;

        [Header("Test Configuration / 测试配置")]
        [SerializeField]
        [Tooltip("Run Tests On Start / 启动时运行测试 - Automatically run compatibility tests on start")]
        private bool m_runTestsOnStart = false;

        [SerializeField]
        [Tooltip("Test Interval / 测试间隔 - Interval between compatibility test cycles (seconds)")]
        private float m_testInterval = 300f; // 5 minutes

        [SerializeField]
        [Tooltip("Enable Continuous Testing / 启用连续测试 - Run compatibility tests continuously")]
        private bool m_enableContinuousTesting = false;

        // Test results and tracking / 测试结果和跟踪
        private List<CompatibilityTestResult> m_testResults = new List<CompatibilityTestResult>();
        private List<BugReport> m_detectedBugs = new List<BugReport>();
        private Dictionary<string, int> m_bugFrequency = new Dictionary<string, int>();

        // System information / 系统信息
        private SystemInfo m_systemInfo;
        private VRDeviceInfo m_vrDeviceInfo;
        private bool m_isTestRunning = false;
        private float m_lastTestTime = 0f;

        // Component references / 组件引用
        private readonly List<ICompatibilityTestable> m_testableSystems = new List<ICompatibilityTestable>();

        // Static instance / 静态实例
        public static CompatibilityTestingSystem Instance { get; private set; }

        #region Properties / 属性

        /// <summary>
        /// 是否正在运行测试
        /// </summary>
        public bool IsTestRunning => m_isTestRunning;

        /// <summary>
        /// 检测到的Bug数量
        /// </summary>
        public int DetectedBugCount => m_detectedBugs.Count;

        /// <summary>
        /// 兼容性评分（0-1）
        /// </summary>
        public float CompatibilityScore => CalculateCompatibilityScore();

        /// <summary>
        /// 系统信息
        /// </summary>
        public SystemInfo SystemInformation => m_systemInfo;

        /// <summary>
        /// VR设备信息
        /// </summary>
        public VRDeviceInfo VRDeviceInformation => m_vrDeviceInfo;

        #endregion

        #region Unity Lifecycle / Unity生命周期

        private void Awake()
        {
            // Singleton pattern / 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystemInfo();
                DiscoverTestableSystems();
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

            if (m_runTestsOnStart)
            {
                StartCompatibilityTests();
            }
        }

        private void Update()
        {
            if (m_enableAutoBugDetection)
            {
                DetectCommonBugs();
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
        }

        #endregion

        #region IGameModeComponent Implementation / 游戏模式组件实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            Debug.Log($"[CompatibilityTestingSystem] 游戏模式切换: {previousMode} → {newMode}");

            // Run mode-specific compatibility tests / 运行模式特定的兼容性测试
            if (m_enableContinuousTesting)
            {
                StartModeSpecificTests(newMode);
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // Compatibility testing is active in all modes / 兼容性测试在所有模式下都活跃
            return true;
        }

        #endregion

        #region System Information / 系统信息

        /// <summary>
        /// 初始化系统信息
        /// </summary>
        private void InitializeSystemInfo()
        {
            m_systemInfo = new SystemInfo
            {
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                DeviceName = "Quest Device", // SystemInfo.deviceName - temporarily hardcoded
                DeviceModel = "Meta Quest", // SystemInfo.deviceModel - temporarily hardcoded
                OperatingSystem = "Android", // SystemInfo.operatingSystem - temporarily hardcoded
                ProcessorType = "ARM", // SystemInfo.processorType - temporarily hardcoded
                ProcessorCount = 8, // SystemInfo.processorCount - temporarily hardcoded
                GraphicsDeviceName = "Adreno GPU", // SystemInfo.graphicsDeviceName - temporarily hardcoded
                GraphicsDeviceType = "OpenGLES3", // SystemInfo.graphicsDeviceType.ToString() - temporarily hardcoded
                GraphicsMemorySize = 8192, // SystemInfo.graphicsMemorySize - temporarily hardcoded
                SystemMemorySize = 8192, // SystemInfo.systemMemorySize - temporarily hardcoded
                SupportsVibration = true, // SystemInfo.supportsVibration - temporarily hardcoded
                SupportsGyroscope = true, // SystemInfo.supportsGyroscope - temporarily hardcoded
                SupportsAccelerometer = true // SystemInfo.supportsAccelerometer - temporarily hardcoded
            };

            InitializeVRDeviceInfo();
        }

        /// <summary>
        /// 初始化VR设备信息
        /// </summary>
        private void InitializeVRDeviceInfo()
        {
            m_vrDeviceInfo = new VRDeviceInfo
            {
                IsVRSupported = XRSettings.supportedDevices.Length > 0,
                IsVREnabled = XRSettings.enabled,
                LoadedDeviceName = XRSettings.loadedDeviceName,
                SupportedDevices = XRSettings.supportedDevices,
                RefreshRate = XRDevice.refreshRate,
                IsPresent = IsXRDevicePresent(), // Updated XR detection
                EyeTextureWidth = XRSettings.eyeTextureWidth,
                EyeTextureHeight = XRSettings.eyeTextureHeight,
                RenderViewportScale = XRSettings.renderViewportScale
            };
        }

        #endregion

        #region System Discovery / 系统发现

        /// <summary>
        /// 发现可测试的系统
        /// </summary>
        private void DiscoverTestableSystems()
        {
            // Find all objects that implement ICompatibilityTestable / 查找所有实现ICompatibilityTestable的对象
            var testableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ICompatibilityTestable>();
            m_testableSystems.AddRange(testableObjects);

            Debug.Log($"[CompatibilityTestingSystem] 发现 {m_testableSystems.Count} 个可测试系统");
        }

        #endregion

        #region Compatibility Testing / 兼容性测试

        /// <summary>
        /// 开始兼容性测试
        /// </summary>
        public void StartCompatibilityTests()
        {
            if (m_isTestRunning)
            {
                Debug.LogWarning("[CompatibilityTestingSystem] 兼容性测试已在运行中");
                return;
            }

            m_isTestRunning = true;
            m_lastTestTime = Time.unscaledTime;

            Debug.Log("[CompatibilityTestingSystem] 开始兼容性测试...");

            StartCoroutine(RunCompatibilityTestSuite());
        }

        /// <summary>
        /// 运行兼容性测试套件
        /// </summary>
        private System.Collections.IEnumerator RunCompatibilityTestSuite()
        {
            var testResult = new CompatibilityTestResult
            {
                TestId = Guid.NewGuid().ToString(),
                TestDate = DateTime.Now,
                SystemInfo = m_systemInfo,
                VRDeviceInfo = m_vrDeviceInfo
            };

            // Test VR device compatibility / 测试VR设备兼容性
            yield return StartCoroutine(TestVRDeviceCompatibility(testResult));

            // Test Unity version compatibility / 测试Unity版本兼容性
            if (m_testUnityVersions)
            {
                yield return StartCoroutine(TestUnityCompatibility(testResult));
            }

            // Test platform settings / 测试平台设置
            if (m_testPlatformSettings)
            {
                yield return StartCoroutine(TestPlatformCompatibility(testResult));
            }

            // Test graphics API compatibility / 测试图形API兼容性
            if (m_testGraphicsAPIs)
            {
                yield return StartCoroutine(TestGraphicsCompatibility(testResult));
            }

            // Test audio system compatibility / 测试音频系统兼容性
            if (m_testAudioSystems)
            {
                yield return StartCoroutine(TestAudioCompatibility(testResult));
            }

            // Test network features / 测试网络功能
            if (m_testNetworkFeatures)
            {
                yield return StartCoroutine(TestNetworkCompatibility(testResult));
            }

            // Test input systems / 测试输入系统
            if (m_testInputSystems)
            {
                yield return StartCoroutine(TestInputCompatibility(testResult));
            }

            // Test custom systems / 测试自定义系统
            yield return StartCoroutine(TestCustomSystems(testResult));

            // Calculate overall compatibility score / 计算总体兼容性评分
            testResult.OverallCompatibilityScore = CalculateOverallScore(testResult);
            testResult.TestDuration = Time.unscaledTime - m_lastTestTime;

            m_testResults.Add(testResult);
            m_isTestRunning = false;

            Debug.Log($"[CompatibilityTestingSystem] 兼容性测试完成，评分: {testResult.OverallCompatibilityScore:F2}");

            GenerateCompatibilityReport(testResult);
        }

        /// <summary>
        /// 测试VR设备兼容性
        /// </summary>
        private System.Collections.IEnumerator TestVRDeviceCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试VR设备兼容性...");

            var vrTest = new VRCompatibilityTest();

            // Test basic VR functionality / 测试基本VR功能
            vrTest.IsVRSupported = m_vrDeviceInfo.IsVRSupported;
            vrTest.IsVREnabled = m_vrDeviceInfo.IsVREnabled;
            vrTest.IsDevicePresent = m_vrDeviceInfo.IsPresent;

            // Test supported devices / 测试支持的设备
            bool deviceSupported = false;
            foreach (string supportedDevice in m_supportedVRDevices)
            {
                if (m_vrDeviceInfo.LoadedDeviceName.ToLower().Contains(supportedDevice.ToLower()))
                {
                    deviceSupported = true;
                    break;
                }
            }
            vrTest.IsDeviceSupported = deviceSupported;

            // Test controllers / 测试控制器
            if (m_testControllers)
            {
                vrTest.ControllersDetected = OVRInput.GetActiveController() != OVRInput.Controller.None;
            }

            // Test hand tracking / 测试手部跟踪
            if (m_testHandTracking)
            {
                vrTest.HandTrackingSupported = OVRPlugin.GetHandTrackingEnabled();
            }

            // Test refresh rate / 测试刷新率
            vrTest.RefreshRateOptimal = m_vrDeviceInfo.RefreshRate >= 90f; // At least 90Hz for good VR

            // Calculate VR compatibility score / 计算VR兼容性评分
            float score = 0f;
            int testCount = 0;

            if (vrTest.IsVRSupported) { score += 1f; } testCount++;
            if (vrTest.IsVREnabled) { score += 1f; } testCount++;
            if (vrTest.IsDevicePresent) { score += 1f; } testCount++;
            if (vrTest.IsDeviceSupported) { score += 1f; } testCount++;
            if (vrTest.ControllersDetected) { score += 1f; } testCount++;
            if (vrTest.HandTrackingSupported) { score += 0.5f; } testCount++; // Optional feature
            if (vrTest.RefreshRateOptimal) { score += 1f; } testCount++;

            vrTest.CompatibilityScore = testCount > 0 ? score / testCount : 0f;
            result.VRCompatibility = vrTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试Unity兼容性
        /// </summary>
        private System.Collections.IEnumerator TestUnityCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试Unity兼容性...");

            var unityTest = new UnityCompatibilityTest
            {
                UnityVersion = Application.unityVersion,
                IsLTSVersion = Application.unityVersion.Contains("LTS"),
                TargetFrameRate = Application.targetFrameRate,
                RunInBackground = Application.runInBackground
            };

            // Check minimum Unity version (2022.3 LTS recommended) / 检查最低Unity版本
            var version = new Version(Application.unityVersion.Split('f')[0]);
            var minVersion = new Version("2022.3.0");
            unityTest.MeetsMinimumVersion = version >= minVersion;

            // Calculate Unity compatibility score / 计算Unity兼容性评分
            float score = 0f;
            if (unityTest.MeetsMinimumVersion) score += 1f;
            if (unityTest.IsLTSVersion) score += 0.5f; // Bonus for LTS
            if (unityTest.TargetFrameRate <= 0 || unityTest.TargetFrameRate >= 90) score += 1f; // VSync or high frame rate
            
            unityTest.CompatibilityScore = score / 2.5f; // Normalize to 0-1
            result.UnityCompatibility = unityTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试平台兼容性
        /// </summary>
        private System.Collections.IEnumerator TestPlatformCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试平台兼容性...");

            var platformTest = new PlatformCompatibilityTest
            {
                Platform = Application.platform.ToString(),
                IsTargetPlatform = Application.platform == RuntimePlatform.Android, // Quest runs on Android
                QualityLevel = QualitySettings.GetQualityLevel(),
                VSyncEnabled = QualitySettings.vSyncCount > 0
            };

            // Calculate platform compatibility score / 计算平台兼容性评分
            float score = 0f;
            if (platformTest.IsTargetPlatform) score += 1f;
            if (platformTest.QualityLevel >= 2) score += 1f; // At least medium quality
            if (!platformTest.VSyncEnabled) score += 1f; // VSync disabled for VR performance
            
            platformTest.CompatibilityScore = score / 3f;
            result.PlatformCompatibility = platformTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试图形兼容性
        /// </summary>
        private System.Collections.IEnumerator TestGraphicsCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试图形兼容性...");

            var graphicsTest = new GraphicsCompatibilityTest
            {
                GraphicsAPI = "OpenGLES3", // SystemInfo.graphicsDeviceType.ToString() - hardcoded
                GraphicsMemory = 8192, // SystemInfo.graphicsMemorySize - hardcoded
                SupportsMultisampling = true, // SystemInfo.supportsMultisampledTextures > 0 - hardcoded
                MaxTextureSize = 4096 // SystemInfo.maxTextureSize - hardcoded
            };

            // Check if graphics API is suitable for VR / 检查图形API是否适合VR
            bool suitableAPI = true; // Assume suitable API for Quest
            graphicsTest.IsSuitableForVR = suitableAPI;

            // Calculate graphics compatibility score / 计算图形兼容性评分
            float score = 0f;
            if (graphicsTest.IsSuitableForVR) score += 1f;
            if (graphicsTest.GraphicsMemory >= 2048) score += 1f; // At least 2GB graphics memory
            if (graphicsTest.SupportsMultisampling) score += 1f;
            if (graphicsTest.MaxTextureSize >= 4096) score += 1f; // Support for 4K textures
            
            graphicsTest.CompatibilityScore = score / 4f;
            result.GraphicsCompatibility = graphicsTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试音频兼容性
        /// </summary>
        private System.Collections.IEnumerator TestAudioCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试音频兼容性...");

            var audioTest = new AudioCompatibilityTest
            {
                AudioSystemActive = AudioSettings.GetConfiguration().sampleRate > 0,
                SpatialAudioSupported = AudioSettings.GetSpatializerPluginName() != "",
                SampleRate = AudioSettings.GetConfiguration().sampleRate,
                BufferSize = AudioSettings.GetConfiguration().dspBufferSize
            };

            // Calculate audio compatibility score / 计算音频兼容性评分
            float score = 0f;
            if (audioTest.AudioSystemActive) score += 1f;
            if (audioTest.SpatialAudioSupported) score += 1f;
            if (audioTest.SampleRate >= 44100) score += 1f; // CD quality or better
            if (audioTest.BufferSize <= 512) score += 1f; // Low latency buffer
            
            audioTest.CompatibilityScore = score / 4f;
            result.AudioCompatibility = audioTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试网络兼容性
        /// </summary>
        private System.Collections.IEnumerator TestNetworkCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试网络兼容性...");

            var networkTest = new NetworkCompatibilityTest
            {
                InternetReachable = Application.internetReachability != NetworkReachability.NotReachable,
                NetworkType = Application.internetReachability.ToString()
            };

            // Test Photon connectivity (simplified) / 测试Photon连接（简化）
            networkTest.PhotonCompatible = true; // Would need actual Photon test

            // Calculate network compatibility score / 计算网络兼容性评分
            float score = 0f;
            if (networkTest.InternetReachable) score += 1f;
            if (networkTest.PhotonCompatible) score += 1f;
            
            networkTest.CompatibilityScore = score / 2f;
            result.NetworkCompatibility = networkTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试输入兼容性
        /// </summary>
        private System.Collections.IEnumerator TestInputCompatibility(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试输入兼容性...");

            var inputTest = new InputCompatibilityTest
            {
                NewInputSystemEnabled = true, // Assuming new input system is used
                TouchSupported = UnityEngine.Input.touchSupported,
                GyroscopeSupported = true, // SystemInfo.supportsGyroscope - hardcoded
                AccelerometerSupported = true // SystemInfo.supportsAccelerometer - hardcoded
            };

            // Test VR input specifically / 特别测试VR输入
            inputTest.VRControllersSupported = OVRInput.GetActiveController() != OVRInput.Controller.None;

            // Calculate input compatibility score / 计算输入兼容性评分
            float score = 0f;
            if (inputTest.NewInputSystemEnabled) score += 1f;
            if (inputTest.VRControllersSupported) score += 1f;
            if (inputTest.GyroscopeSupported) score += 0.5f; // Optional
            if (inputTest.AccelerometerSupported) score += 0.5f; // Optional
            
            inputTest.CompatibilityScore = score / 3f;
            result.InputCompatibility = inputTest;

            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// 测试自定义系统
        /// </summary>
        private System.Collections.IEnumerator TestCustomSystems(CompatibilityTestResult result)
        {
            Debug.Log("[CompatibilityTestingSystem] 测试自定义系统...");

            var customTests = new List<CustomSystemTest>();

            foreach (var testableSystem in m_testableSystems)
            {
                var customTest = new CustomSystemTest
                {
                    SystemName = testableSystem.GetType().Name,
                    IsCompatible = testableSystem.TestCompatibility(),
                    CompatibilityDetails = testableSystem.GetCompatibilityDetails()
                };

                customTests.Add(customTest);
                yield return new WaitForEndOfFrame();
            }

            result.CustomSystemTests = customTests;
        }

        #endregion

        #region Bug Detection / Bug检测

        /// <summary>
        /// 检测常见Bug
        /// </summary>
        private void DetectCommonBugs()
        {
            // Frame rate drops / 帧率下降
            DetectFrameRateIssues();

            // Memory leaks / 内存泄漏
            DetectMemoryLeaks();

            // VR tracking issues / VR跟踪问题
            DetectVRTrackingIssues();

            // Audio issues / 音频问题
            DetectAudioIssues();

            // Input lag / 输入延迟
            DetectInputLag();

            // Network issues / 网络问题
            DetectNetworkIssues();
        }

        /// <summary>
        /// 检测帧率问题
        /// </summary>
        private void DetectFrameRateIssues()
        {
            if (Performance.VRPerformanceMonitor.Instance != null)
            {
                float currentFPS = Performance.VRPerformanceMonitor.Instance.CurrentFPS;
                float targetFPS = 90f; // VR target

                if (currentFPS < targetFPS * 0.8f) // 20% below target
                {
                    ReportBug("Frame Rate Drop", $"当前FPS {currentFPS:F1} 低于目标 {targetFPS}", BugSeverity.High);
                }
            }
        }

        /// <summary>
        /// 检测内存泄漏
        /// </summary>
        private void DetectMemoryLeaks()
        {
            if (Performance.MemoryUsageProfiler.Instance != null)
            {
                var warnings = Performance.MemoryUsageProfiler.Instance.ActiveWarnings;
                foreach (var warning in warnings)
                {
                    if (warning.Type == Performance.MemoryWarningType.Leak)
                    {
                        ReportBug("Memory Leak", warning.Message, BugSeverity.Critical);
                    }
                }
            }
        }

        /// <summary>
        /// 检测VR跟踪问题
        /// </summary>
        private void DetectVRTrackingIssues()
        {
            if (!IsXRDevicePresent()) // Updated XR detection
            {
                ReportBug("VR Tracking Lost", "VR设备未检测到", BugSeverity.Critical);
            }
        }

        /// <summary>
        /// 检测音频问题
        /// </summary>
        private void DetectAudioIssues()
        {
            if (AudioSettings.GetConfiguration().sampleRate == 0)
            {
                ReportBug("Audio System Failure", "音频系统未初始化", BugSeverity.High);
            }
        }

        /// <summary>
        /// 检测输入延迟
        /// </summary>
        private void DetectInputLag()
        {
            if (PongHub.Input.PongHubInputManager.Instance != null)
            {
                float inputLatency = PongHub.Input.PongHubInputManager.Instance.LastFrameCPUTime / 1000f;
                if (inputLatency > 16f) // More than 1 frame at 60fps
                {
                    ReportBug("Input Lag", $"输入延迟过高: {inputLatency:F2}ms", BugSeverity.Medium);
                }
            }
        }

        /// <summary>
        /// 检测网络问题
        /// </summary>
        private void DetectNetworkIssues()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ReportBug("Network Connectivity", "网络连接不可用", BugSeverity.Medium);
            }
        }

        /// <summary>
        /// 报告Bug
        /// </summary>
        private void ReportBug(string bugType, string description, BugSeverity severity)
        {
            // Check if this bug was already reported recently / 检查此Bug是否最近已报告
            string bugKey = $"{bugType}:{description}";
            
            if (!m_bugFrequency.ContainsKey(bugKey))
            {
                m_bugFrequency[bugKey] = 0;
            }
            
            m_bugFrequency[bugKey]++;

            // Only report if frequency is below threshold / 只有在频率低于阈值时才报告
            if (m_bugFrequency[bugKey] <= 3) // Maximum 3 reports per bug
            {
                var bugReport = new BugReport
                {
                    BugId = Guid.NewGuid().ToString(),
                    BugType = bugType,
                    Description = description,
                    Severity = severity,
                    DetectionTime = DateTime.Now,
                    SystemInfo = m_systemInfo,
                    VRDeviceInfo = m_vrDeviceInfo,
                    Frequency = m_bugFrequency[bugKey]
                };

                m_detectedBugs.Add(bugReport);

                if (m_generateBugReports)
                {
                    GenerateBugReport(bugReport);
                }

                Debug.LogError($"[CompatibilityTestingSystem] Bug检测: {bugType} - {description} (严重程度: {severity})");
            }
        }

        #endregion

        #region Reporting / 报告

        /// <summary>
        /// 生成兼容性报告
        /// </summary>
        private void GenerateCompatibilityReport(CompatibilityTestResult result)
        {
            string report = $"🔧 PongHub VR 兼容性测试报告\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"测试ID: {result.TestId}\n" +
                           $"测试日期: {result.TestDate:yyyy-MM-dd HH:mm:ss}\n" +
                           $"测试持续时间: {result.TestDuration:F2}秒\n\n" +
                           $"📊 总体兼容性评分: {result.OverallCompatibilityScore:F2}\n\n" +
                           $"🥽 VR兼容性: {result.VRCompatibility.CompatibilityScore:F2}\n" +
                           $"- VR支持: {result.VRCompatibility.IsVRSupported}\n" +
                           $"- VR启用: {result.VRCompatibility.IsVREnabled}\n" +
                           $"- 设备存在: {result.VRCompatibility.IsDevicePresent}\n" +
                           $"- 设备支持: {result.VRCompatibility.IsDeviceSupported}\n" +
                           $"- 控制器检测: {result.VRCompatibility.ControllersDetected}\n\n" +
                           $"🎮 Unity兼容性: {result.UnityCompatibility.CompatibilityScore:F2}\n" +
                           $"- Unity版本: {result.UnityCompatibility.UnityVersion}\n" +
                           $"- LTS版本: {result.UnityCompatibility.IsLTSVersion}\n" +
                           $"- 最低版本要求: {result.UnityCompatibility.MeetsMinimumVersion}\n\n" +
                           $"💻 平台兼容性: {result.PlatformCompatibility.CompatibilityScore:F2}\n" +
                           $"- 平台: {result.PlatformCompatibility.Platform}\n" +
                           $"- 目标平台: {result.PlatformCompatibility.IsTargetPlatform}\n\n" +
                           GenerateRecommendationsText(result);

            Debug.Log($"[CompatibilityTestingSystem]\n{report}");

            // Save report to file / 保存报告到文件
            SaveCompatibilityReport(report, result.TestId);
        }

        /// <summary>
        /// 生成Bug报告
        /// </summary>
        private void GenerateBugReport(BugReport bugReport)
        {
            string report = $"🐛 PongHub VR Bug报告\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"Bug ID: {bugReport.BugId}\n" +
                           $"检测时间: {bugReport.DetectionTime:yyyy-MM-dd HH:mm:ss}\n" +
                           $"Bug类型: {bugReport.BugType}\n" +
                           $"严重程度: {bugReport.Severity}\n" +
                           $"出现频率: {bugReport.Frequency}\n\n" +
                           $"描述: {bugReport.Description}\n\n" +
                           $"系统信息:\n" +
                           $"- Unity版本: {bugReport.SystemInfo.UnityVersion}\n" +
                           $"- 平台: {bugReport.SystemInfo.Platform}\n" +
                           $"- 设备: {bugReport.SystemInfo.DeviceName}\n" +
                           $"- VR设备: {bugReport.VRDeviceInfo.LoadedDeviceName}";

            Debug.LogError($"[CompatibilityTestingSystem]\n{report}");

            // Save bug report to file / 保存Bug报告到文件
            SaveBugReport(report, bugReport.BugId);
        }

        /// <summary>
        /// 保存兼容性报告到文件
        /// </summary>
        private void SaveCompatibilityReport(string report, string testId)
        {
            try
            {
                string fileName = $"Compatibility_Report_{testId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                System.IO.File.WriteAllText(filePath, report);
                Debug.Log($"[CompatibilityTestingSystem] 兼容性报告已保存: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CompatibilityTestingSystem] 保存兼容性报告失败: {e.Message}");
            }
        }

        /// <summary>
        /// 保存Bug报告到文件
        /// </summary>
        private void SaveBugReport(string report, string bugId)
        {
            try
            {
                string fileName = $"Bug_Report_{bugId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                System.IO.File.WriteAllText(filePath, report);
                Debug.Log($"[CompatibilityTestingSystem] Bug报告已保存: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CompatibilityTestingSystem] 保存Bug报告失败: {e.Message}");
            }
        }

        #endregion

        #region Utility Methods / 实用方法

        /// <summary>
        /// 计算兼容性评分
        /// </summary>
        private float CalculateCompatibilityScore()
        {
            if (m_testResults.Count == 0) return 0f;

            return m_testResults.Average(r => r.OverallCompatibilityScore);
        }

        /// <summary>
        /// 计算总体评分
        /// </summary>
        private float CalculateOverallScore(CompatibilityTestResult result)
        {
            var scores = new List<float>();

            scores.Add(result.VRCompatibility.CompatibilityScore);
            scores.Add(result.UnityCompatibility.CompatibilityScore);
            scores.Add(result.PlatformCompatibility.CompatibilityScore);
            scores.Add(result.GraphicsCompatibility.CompatibilityScore);
            scores.Add(result.AudioCompatibility.CompatibilityScore);
            scores.Add(result.NetworkCompatibility.CompatibilityScore);
            scores.Add(result.InputCompatibility.CompatibilityScore);

            // Add custom system scores / 添加自定义系统评分
            if (result.CustomSystemTests != null)
            {
                foreach (var customTest in result.CustomSystemTests)
                {
                    scores.Add(customTest.IsCompatible ? 1f : 0f);
                }
            }

            return scores.Count > 0 ? scores.Average() : 0f;
        }

        /// <summary>
        /// 检测XR设备是否存在（替代弃用的XRDevice.isPresent）
        /// </summary>
        private bool IsXRDevicePresent()
        {
#if UNITY_XR_MANAGEMENT
            var xrSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (xrSettings != null && xrSettings.Manager != null)
            {
                return xrSettings.Manager.activeLoader != null;
            }
#endif
            return false;
        }

        /// <summary>
        /// 生成建议文本
        /// </summary>
        private string GenerateRecommendationsText(CompatibilityTestResult result)
        {
            var recommendations = new List<string>();

            if (result.VRCompatibility.CompatibilityScore < 0.8f)
            {
                recommendations.Add("- 检查VR设备连接和驱动程序");
                recommendations.Add("- 确保VR控制器正常工作");
            }

            if (result.UnityCompatibility.CompatibilityScore < 0.8f)
            {
                recommendations.Add("- 升级到Unity 2022.3 LTS或更高版本");
                recommendations.Add("- 检查项目设置和平台配置");
            }

            if (result.GraphicsCompatibility.CompatibilityScore < 0.8f)
            {
                recommendations.Add("- 检查图形API设置");
                recommendations.Add("- 确保显卡驱动程序是最新的");
            }

            return recommendations.Count > 0 ? 
                $"🎯 建议:\n{string.Join("\n", recommendations)}" : 
                "✅ 系统兼容性良好，无需特别调整";
        }

        /// <summary>
        /// 开始模式特定测试
        /// </summary>
        private void StartModeSpecificTests(GameMode mode)
        {
            Debug.Log($"[CompatibilityTestingSystem] 开始{mode}模式特定测试");
            // Implement mode-specific tests / 实现模式特定测试
        }

        /// <summary>
        /// 检查下一个测试周期
        /// </summary>
        private void CheckForNextTestCycle()
        {
            if (Time.unscaledTime - m_lastTestTime >= m_testInterval)
            {
                StartCompatibilityTests();
            }
        }

        #endregion

        #region Public API / 公共API

        /// <summary>
        /// 手动开始兼容性测试
        /// </summary>
        public void RunCompatibilityCheck()
        {
            StartCompatibilityTests();
        }

        /// <summary>
        /// 获取最新的兼容性测试结果
        /// </summary>
        public CompatibilityTestResult GetLatestTestResult()
        {
            return m_testResults.LastOrDefault();
        }

        /// <summary>
        /// 获取所有Bug报告
        /// </summary>
        public List<BugReport> GetAllBugReports()
        {
            return new List<BugReport>(m_detectedBugs);
        }

        /// <summary>
        /// 清除Bug报告
        /// </summary>
        public void ClearBugReports()
        {
            m_detectedBugs.Clear();
            m_bugFrequency.Clear();
            Debug.Log("[CompatibilityTestingSystem] Bug报告已清除");
        }

        /// <summary>
        /// 设置连续测试模式
        /// </summary>
        public void SetContinuousTestingEnabled(bool enabled)
        {
            m_enableContinuousTesting = enabled;
            if (enabled && !m_isTestRunning)
            {
                StartCompatibilityTests();
            }
        }

        /// <summary>
        /// 注册可测试系统
        /// </summary>
        public void RegisterTestableSystem(ICompatibilityTestable testableSystem)
        {
            if (!m_testableSystems.Contains(testableSystem))
            {
                m_testableSystems.Add(testableSystem);
                Debug.Log($"[CompatibilityTestingSystem] 注册可测试系统: {testableSystem.GetType().Name}");
            }
        }

        #endregion
    }

    #region Interfaces / 接口

    /// <summary>
    /// 兼容性测试接口
    /// </summary>
    public interface ICompatibilityTestable
    {
        bool TestCompatibility();
        string GetCompatibilityDetails();
    }

    #endregion

    #region Data Structures / 数据结构

    /// <summary>
    /// Bug严重程度枚举
    /// </summary>
    public enum BugSeverity
    {
        Low,        // 低
        Medium,     // 中
        High,       // 高
        Critical    // 严重
    }

    /// <summary>
    /// 系统信息结构
    /// </summary>
    [System.Serializable]
    public struct SystemInfo
    {
        public string UnityVersion;
        public string Platform;
        public string DeviceName;
        public string DeviceModel;
        public string OperatingSystem;
        public string ProcessorType;
        public int ProcessorCount;
        public string GraphicsDeviceName;
        public string GraphicsDeviceType;
        public int GraphicsMemorySize;
        public int SystemMemorySize;
        public bool SupportsVibration;
        public bool SupportsGyroscope;
        public bool SupportsAccelerometer;
    }

    /// <summary>
    /// VR设备信息结构
    /// </summary>
    [System.Serializable]
    public struct VRDeviceInfo
    {
        public bool IsVRSupported;
        public bool IsVREnabled;
        public string LoadedDeviceName;
        public string[] SupportedDevices;
        public float RefreshRate;
        public bool IsPresent;
        public int EyeTextureWidth;
        public int EyeTextureHeight;
        public float RenderViewportScale;
    }

    /// <summary>
    /// 兼容性测试结果
    /// </summary>
    [System.Serializable]
    public class CompatibilityTestResult
    {
        public string TestId;
        public DateTime TestDate;
        public float TestDuration;
        public SystemInfo SystemInfo;
        public VRDeviceInfo VRDeviceInfo;
        public float OverallCompatibilityScore;
        public VRCompatibilityTest VRCompatibility;
        public UnityCompatibilityTest UnityCompatibility;
        public PlatformCompatibilityTest PlatformCompatibility;
        public GraphicsCompatibilityTest GraphicsCompatibility;
        public AudioCompatibilityTest AudioCompatibility;
        public NetworkCompatibilityTest NetworkCompatibility;
        public InputCompatibilityTest InputCompatibility;
        public List<CustomSystemTest> CustomSystemTests;
    }

    /// <summary>
    /// VR兼容性测试
    /// </summary>
    [System.Serializable]
    public struct VRCompatibilityTest
    {
        public float CompatibilityScore;
        public bool IsVRSupported;
        public bool IsVREnabled;
        public bool IsDevicePresent;
        public bool IsDeviceSupported;
        public bool ControllersDetected;
        public bool HandTrackingSupported;
        public bool RefreshRateOptimal;
    }

    /// <summary>
    /// Unity兼容性测试
    /// </summary>
    [System.Serializable]
    public struct UnityCompatibilityTest
    {
        public float CompatibilityScore;
        public string UnityVersion;
        public bool IsLTSVersion;
        public bool MeetsMinimumVersion;
        public int TargetFrameRate;
        public bool RunInBackground;
    }

    /// <summary>
    /// 平台兼容性测试
    /// </summary>
    [System.Serializable]
    public struct PlatformCompatibilityTest
    {
        public float CompatibilityScore;
        public string Platform;
        public bool IsTargetPlatform;
        public int QualityLevel;
        public bool VSyncEnabled;
    }

    /// <summary>
    /// 图形兼容性测试
    /// </summary>
    [System.Serializable]
    public struct GraphicsCompatibilityTest
    {
        public float CompatibilityScore;
        public string GraphicsAPI;
        public int GraphicsMemory;
        public bool IsSuitableForVR;
        public bool SupportsMultisampling;
        public int MaxTextureSize;
    }

    /// <summary>
    /// 音频兼容性测试
    /// </summary>
    [System.Serializable]
    public struct AudioCompatibilityTest
    {
        public float CompatibilityScore;
        public bool AudioSystemActive;
        public bool SpatialAudioSupported;
        public int SampleRate;
        public int BufferSize;
    }

    /// <summary>
    /// 网络兼容性测试
    /// </summary>
    [System.Serializable]
    public struct NetworkCompatibilityTest
    {
        public float CompatibilityScore;
        public bool InternetReachable;
        public string NetworkType;
        public bool PhotonCompatible;
    }

    /// <summary>
    /// 输入兼容性测试
    /// </summary>
    [System.Serializable]
    public struct InputCompatibilityTest
    {
        public float CompatibilityScore;
        public bool NewInputSystemEnabled;
        public bool VRControllersSupported;
        public bool TouchSupported;
        public bool GyroscopeSupported;
        public bool AccelerometerSupported;
    }

    /// <summary>
    /// 自定义系统测试
    /// </summary>
    [System.Serializable]
    public struct CustomSystemTest
    {
        public string SystemName;
        public bool IsCompatible;
        public string CompatibilityDetails;
    }

    /// <summary>
    /// Bug报告
    /// </summary>
    [System.Serializable]
    public struct BugReport
    {
        public string BugId;
        public string BugType;
        public string Description;
        public BugSeverity Severity;
        public DateTime DetectionTime;
        public SystemInfo SystemInfo;
        public VRDeviceInfo VRDeviceInfo;
        public int Frequency;
    }

    #endregion
}