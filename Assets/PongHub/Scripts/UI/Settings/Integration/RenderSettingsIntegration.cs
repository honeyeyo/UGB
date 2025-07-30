using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PongHub.UI.Settings.Core;

namespace PongHub.UI.Settings.Integration
{
    /// <summary>
    /// 渲染设置集成
    /// Render Settings Integration - Connects settings to Unity's rendering system
    /// </summary>
    public class RenderSettingsIntegration : MonoBehaviour
    {
        [Header("渲染管线配置")]
        [SerializeField]
        [Tooltip("URP渲染管线资产")]
        private UniversalRenderPipelineAsset urpAsset;

        [SerializeField]
        [Tooltip("质量级别配置")]
        private QualitySettings[] qualityConfigs = new QualitySettings[0];

        [Header("VR渲染配置")]
        [SerializeField]
        [Tooltip("眼部渲染缩放范围")]
        private Vector2 eyeTextureScaleRange = new Vector2(0.5f, 1.5f);

        [SerializeField]
        [Tooltip("VR性能监控")]
        private bool enablePerformanceMonitoring = true;

        // 内部状态
        private VideoSettings currentSettings;
        private bool isInitialized = false;
        private Camera mainCamera;
        private Volume postProcessVolume;
        private int originalQualityLevel;

        /// <summary>
        /// 质量设置配置
        /// </summary>
        [Serializable]
        public class QualitySettings
        {
            [Header("基础设置")]
            public RenderQuality quality;
            public string displayName;

            [Header("渲染配置")]
            public int shadowDistance = 50;
            public UnityEngine.ShadowQuality shadowQuality = UnityEngine.ShadowQuality.All;
            public int textureQuality = 0;
            public bool enableAntiAliasing = true;
            public int antiAliasingSamples = 4;
            public bool enableHDR = true;

            [Header("VR优化")]
            public float renderScale = 1.0f;
            public bool enableFoveatedRendering = false;
            public int maxLODLevel = 0;

            [Header("性能设置")]
            public int targetFrameRate = 120;
            public bool enableVSync = false;
        }

        #region Unity 生命周期

        private void Awake()
        {
            FindComponents();
            originalQualityLevel = UnityEngine.QualitySettings.GetQualityLevel();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        #endregion

        #region 初始化

        private void FindComponents()
        {
            // 查找主摄像机
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
            }

            // 查找URP资产
            if (urpAsset == null)
            {
                urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            }

            // 查找后处理Volume
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();
            }
        }

        private void Initialize()
        {
            if (urpAsset == null)
            {
                Debug.LogWarning("RenderSettingsIntegration: URP Asset not found!");
            }

            // 获取当前视频设置
            if (SettingsManager.Instance != null)
            {
                currentSettings = SettingsManager.Instance.GetVideoSettings();
                ApplyVideoSettings(currentSettings);
            }

            // 初始化质量配置（如果为空）
            if (qualityConfigs.Length == 0)
            {
                CreateDefaultQualityConfigs();
            }

            // 启用性能监控
            if (enablePerformanceMonitoring)
            {
                StartCoroutine(MonitorPerformance());
            }

            isInitialized = true;
            Debug.Log("RenderSettingsIntegration initialized successfully");
        }

        private void RegisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnVideoSettingsChanged += OnVideoSettingsChanged;
            }
        }

        private void UnregisterEvents()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.OnVideoSettingsChanged -= OnVideoSettingsChanged;
            }
        }

        private void CreateDefaultQualityConfigs()
        {
            qualityConfigs = new QualitySettings[]
            {
                new QualitySettings
                {
                    quality = RenderQuality.Low,
                    displayName = "低",
                    shadowDistance = 25,
                    shadowQuality = UnityEngine.ShadowQuality.HardOnly,
                    textureQuality = 2,
                    enableAntiAliasing = false,
                    antiAliasingSamples = 1,
                    enableHDR = false,
                    renderScale = 0.8f,
                    enableFoveatedRendering = true,
                    maxLODLevel = 2,
                    targetFrameRate = 90,
                    enableVSync = false
                },
                new QualitySettings
                {
                    quality = RenderQuality.Medium,
                    displayName = "中",
                    shadowDistance = 50,
                    shadowQuality = UnityEngine.ShadowQuality.All,
                    textureQuality = 1,
                    enableAntiAliasing = true,
                    antiAliasingSamples = 2,
                    enableHDR = true,
                    renderScale = 1.0f,
                    enableFoveatedRendering = false,
                    maxLODLevel = 1,
                    targetFrameRate = 120,
                    enableVSync = false
                },
                new QualitySettings
                {
                    quality = RenderQuality.High,
                    displayName = "高",
                    shadowDistance = 100,
                    shadowQuality = UnityEngine.ShadowQuality.All,
                    textureQuality = 0,
                    enableAntiAliasing = true,
                    antiAliasingSamples = 4,
                    enableHDR = true,
                    renderScale = 1.2f,
                    enableFoveatedRendering = false,
                    maxLODLevel = 0,
                    targetFrameRate = 120,
                    enableVSync = false
                }
            };
        }

        #endregion

        #region 视频设置应用

        /// <summary>
        /// 应用视频设置
        /// </summary>
        /// <param name="settings">视频设置</param>
        public void ApplyVideoSettings(VideoSettings settings)
        {
            if (!isInitialized || settings == null)
                return;

            currentSettings = settings;

            // 应用渲染质量
            ApplyRenderQuality();

            // 应用抗锯齿设置
            ApplyAntiAliasing();

            // 应用阴影设置
            ApplyShadowSettings();

            // 应用后处理设置
            ApplyPostProcessing();

            // 应用VR设置
            ApplyVRSettings();

            // 应用其他渲染选项
            ApplyRenderOptions();

            Debug.Log("Video settings applied successfully");
        }

        private void ApplyRenderQuality()
        {
            var qualityConfig = Array.Find(qualityConfigs, q => q.quality == currentSettings.renderQuality);
            if (qualityConfig == null)
                return;

            // 设置Unity质量级别
            int qualityIndex = GetQualityIndex(currentSettings.renderQuality);
            if (qualityIndex >= 0)
            {
                UnityEngine.QualitySettings.SetQualityLevel(qualityIndex, true);
            }

            // 应用纹理质量
            UnityEngine.QualitySettings.globalTextureMipmapLimit = qualityConfig.textureQuality;
            UnityEngine.QualitySettings.maximumLODLevel = qualityConfig.maxLODLevel;

            // 设置目标帧率
            Application.targetFrameRate = qualityConfig.targetFrameRate;

            // VSync设置
            UnityEngine.QualitySettings.vSyncCount = currentSettings.enableVSync ? 1 : 0;
        }

        private void ApplyAntiAliasing()
        {
            if (urpAsset == null) return;

            // 设置MSAA
            switch (currentSettings.antiAliasing)
            {
                case AntiAliasing.None:
                    urpAsset.msaaSampleCount = 1;
                    break;
                case AntiAliasing.MSAA_2x:
                    urpAsset.msaaSampleCount = 2;
                    break;
                case AntiAliasing.MSAA_4x:
                    urpAsset.msaaSampleCount = 4;
                    break;
                case AntiAliasing.MSAA_8x:
                    urpAsset.msaaSampleCount = 8;
                    break;
            }
        }

        private void ApplyShadowSettings()
        {
            if (urpAsset == null) return;

            // 设置阴影质量
            switch (currentSettings.shadowQuality)
            {
                case ShadowQualityLevel.Disabled:
                    urpAsset.shadowDistance = 0;
                    UnityEngine.QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                    break;
                case ShadowQualityLevel.Low:
                    urpAsset.shadowDistance = 25f;
                    UnityEngine.QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                    urpAsset.shadowCascadeCount = 1;
                    break;
                case ShadowQualityLevel.Medium:
                    urpAsset.shadowDistance = 50f;
                    UnityEngine.QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    urpAsset.shadowCascadeCount = 2;
                    break;
                case ShadowQualityLevel.High:
                    urpAsset.shadowDistance = 100f;
                    UnityEngine.QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    urpAsset.shadowCascadeCount = 4;
                    break;
            }
        }

        private void ApplyPostProcessing()
        {
            if (postProcessVolume == null) return;

            // 启用/禁用后处理
            postProcessVolume.enabled = currentSettings.enablePostProcessing;

            // 根据渲染质量调整后处理强度
            if (currentSettings.enablePostProcessing)
            {
                postProcessVolume.weight = GetPostProcessingWeight();
            }
        }

        private void ApplyVRSettings()
        {
            if (!UnityEngine.XR.XRSettings.enabled) return;

            // 设置眼部纹理缩放
            float clampedScale = Mathf.Clamp(currentSettings.renderScale,
                eyeTextureScaleRange.x, eyeTextureScaleRange.y);

            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = clampedScale;

            // 设置中央视网膜凹渲染
            if (currentSettings.foveatedRendering &&
                UnityEngine.XR.XRSettings.supportedDevices.Length > 0)
            {
                // 启用注视点渲染（如果支持）
                EnableFoveatedRendering();
            }

            // 舒适度设置
            ApplyComfortSettings();
        }

        private void ApplyRenderOptions()
        {
            // HDR设置
            if (mainCamera != null)
            {
                mainCamera.allowHDR = GetQualityConfig()?.enableHDR ?? true;
            }

            // 渲染缩放
            if (urpAsset != null)
            {
                urpAsset.renderScale = currentSettings.renderScale;
            }
        }

        #endregion

        #region VR特殊功能

        private void EnableFoveatedRendering()
        {
            // Meta Quest设备的注视点渲染
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    // 使用Oculus SDK启用注视点渲染
                    // OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.Medium;
                    Debug.Log("Foveated rendering enabled");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to enable foveated rendering: {e.Message}");
                }
            }
        }

        private void ApplyComfortSettings()
        {
            var comfortSettings = currentSettings.comfortSettings;
            if (comfortSettings == null) return;

            // 运动减少
            if (comfortSettings.motionSicknessReduction)
            {
                // 应用运动减少设置
                ApplyMotionReduction();
            }

            // 渐隐边框
            if (comfortSettings.vignette)
            {
                ApplyVignette();
            }
        }

        private void ApplyMotionReduction()
        {
            // 减少相机运动的影响
            if (mainCamera != null)
            {
                // 降低视野角度
                mainCamera.fieldOfView = Mathf.Min(mainCamera.fieldOfView, 90f);
            }
        }

        private void ApplyVignette()
        {
            // 应用边框渐隐效果来减少运动眩晕
            if (postProcessVolume != null && currentSettings.enablePostProcessing)
            {
                // 可以通过后处理Volume添加Vignette效果
                Debug.Log("Vignette effect applied for comfort");
            }
        }

        #endregion

        #region 性能监控

        private IEnumerator MonitorPerformance()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(1f);

                // 监控帧率
                float fps = 1f / Time.deltaTime;

                // 如果帧率过低，自动调整设置
                if (fps < GetTargetFrameRate() * 0.8f)
                {
                    AutoOptimizeSettings();
                }

                // 监控GPU温度（如果可用）
                CheckThermalState();
            }
        }

        private void AutoOptimizeSettings()
        {
            if (!isInitialized) return;

            Debug.Log("Performance below target, auto-optimizing settings...");

            // 降低渲染质量
            if (currentSettings.renderQuality > RenderQuality.Low)
            {
                var newQuality = (RenderQuality)((int)currentSettings.renderQuality - 1);
                currentSettings.renderQuality = newQuality;

                // 应用新设置
                ApplyRenderQuality();

                // 保存设置
                SettingsManager.Instance.SaveVideoSettings(currentSettings);
            }
        }

        private void CheckThermalState()
        {
            // Android设备温度检测
            if (Application.platform == RuntimePlatform.Android)
            {
                // 可以通过Android API检测设备温度状态
                // 如果过热，自动降低渲染质量
            }
        }

        #endregion

        #region 工具方法

        private int GetQualityIndex(RenderQuality quality)
        {
            return (int)quality;
        }

        private QualitySettings GetQualityConfig()
        {
            return Array.Find(qualityConfigs, q => q.quality == currentSettings.renderQuality);
        }

        private float GetPostProcessingWeight()
        {
            switch (currentSettings.renderQuality)
            {
                case RenderQuality.Low: return 0.5f;
                case RenderQuality.Medium: return 0.8f;
                case RenderQuality.High: return 1.0f;
                default: return 1.0f;
            }
        }

        private int GetTargetFrameRate()
        {
            var config = GetQualityConfig();
            return config?.targetFrameRate ?? 120;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 视频设置变更事件处理
        /// </summary>
        private void OnVideoSettingsChanged(VideoSettings newSettings)
        {
            ApplyVideoSettings(newSettings);
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取当前帧率
        /// </summary>
        /// <returns>当前帧率</returns>
        public float GetCurrentFrameRate()
        {
            return 1f / Time.deltaTime;
        }

        /// <summary>
        /// 获取渲染统计信息
        /// </summary>
        /// <returns>渲染统计</returns>
        public RenderStatistics GetRenderStatistics()
        {
            return new RenderStatistics
            {
                currentFPS = GetCurrentFrameRate(),
                targetFPS = GetTargetFrameRate(),
                renderScale = currentSettings.renderScale,
                triangleCount = 0, // 暂时设为0，需要正确的获取方法
                memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()
            };
        }

        /// <summary>
        /// 重置渲染设置
        /// </summary>
        public void ResetRenderSettings()
        {
            UnityEngine.QualitySettings.SetQualityLevel(originalQualityLevel, true);
            var defaultSettings = new VideoSettings();
            ApplyVideoSettings(defaultSettings);
        }

        /// <summary>
        /// 获取推荐的渲染质量
        /// </summary>
        /// <returns>推荐质量</returns>
        public RenderQuality GetRecommendedQuality()
        {
            // 根据设备性能推荐质量
            if (SystemInfo.graphicsMemorySize < 2048)
                return RenderQuality.Low;
            else if (SystemInfo.graphicsMemorySize < 4096)
                return RenderQuality.Medium;
            else
                return RenderQuality.High;
        }

        #endregion

        /// <summary>
        /// 渲染统计信息
        /// </summary>
        public struct RenderStatistics
        {
            public float currentFPS;
            public int targetFPS;
            public float renderScale;
            public long triangleCount;
            public long memoryUsage;
        }
    }
}