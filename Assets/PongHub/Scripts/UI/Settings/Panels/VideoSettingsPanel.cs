using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;
using System.Collections.Generic;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 视频设置面板
    /// Video settings panel for graphics and VR display configuration
    /// </summary>
    public class VideoSettingsPanel : MonoBehaviour
    {
        [Header("渲染设置组件")]
        [SerializeField]
        private TMP_Dropdown qualityDropdown;

        [SerializeField]
        private TMP_Dropdown resolutionDropdown;

        [SerializeField]
        private TMP_Dropdown frameRateDropdown;

        [SerializeField]
        private TMP_Dropdown antiAliasingDropdown;

        [SerializeField]
        private TMP_Dropdown shadowQualityDropdown;

        [SerializeField]
        private Toggle postProcessingToggle;

        [SerializeField]
        private Toggle vsyncToggle;

        [Header("VR设置组件")]
        [SerializeField]
        private Slider renderScaleSlider;

        [SerializeField]
        private Toggle foveatedRenderingToggle;

        [SerializeField]
        private Toggle fixedFoveatedRenderingToggle;

        [SerializeField]
        private Slider targetFrameRateSlider;

        [Header("舒适度设置")]
        [SerializeField]
        private Slider comfortLevelSlider;

        [SerializeField]
        private Toggle snapTurnToggle;

        [SerializeField]
        private Toggle motionSicknessToggle;

        [SerializeField]
        private Toggle vignetteToggle;

        [Header("高级设置")]
        [SerializeField]
        private TMP_Dropdown textureQualityDropdown;

        [SerializeField]
        private TMP_Dropdown anisotropicFilteringDropdown;

        [SerializeField]
        private Toggle softParticlesToggle;

        [SerializeField]
        private Toggle realtimeReflectionsToggle;

        [Header("性能监控")]
        [SerializeField]
        private Toggle showFPSToggle;

        [SerializeField]
        private Toggle performanceStatsToggle;

        [SerializeField]
        private TextMeshProUGUI currentFPSText;

        [SerializeField]
        private TextMeshProUGUI gpuMemoryText;

        [Header("渲染管线引用")]
        [SerializeField]
        private UniversalRenderPipelineAsset[] qualityPresets;

        [SerializeField]
        private UniversalRenderPipelineAsset urpAsset;

        // 组件引用
        private SettingsManager settingsManager;
        private VRHapticFeedback hapticFeedback;

        // 性能监控变量
        private float fpsUpdateTimer = 0f;
        private const float FPS_UPDATE_INTERVAL = 0.5f;
        private int frameCount = 0;
        private float lastFPSUpdate = 0f;

        #region Unity 生命周期

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupVideoComponents();
            RefreshPanel();
        }

        private void Update()
        {
            UpdatePerformanceMonitoring();
        }

        private void OnEnable()
        {
            RegisterSettingsEvents();
        }

        private void OnDisable()
        {
            UnregisterSettingsEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取设置管理器
            settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                Debug.LogError("SettingsManager not found! VideoSettingsPanel requires SettingsManager.");
                return;
            }

            // 获取触觉反馈组件
            hapticFeedback = FindObjectOfType<VRHapticFeedback>();
            if (hapticFeedback == null)
            {
                Debug.LogWarning("VRHapticFeedback not found. Haptic feedback will be disabled.");
            }

            // 获取URP资产
            if (urpAsset == null)
            {
                urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            }
        }

        /// <summary>
        /// 设置视频组件
        /// </summary>
        private void SetupVideoComponents()
        {
            // 设置画质下拉菜单
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qualityOptions = new List<string> { "低", "中", "高", "超高" };
                qualityDropdown.AddOptions(qualityOptions);
                qualityDropdown.onValueChanged.AddListener(OnRenderQualityChanged);
            }

            // 设置分辨率下拉菜单
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                var resolutionOptions = new List<string>
                {
                    "自动", "1280x720", "1920x1080", "2560x1440", "3840x2160"
                };
                resolutionDropdown.AddOptions(resolutionOptions);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // 设置帧率下拉菜单
            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                var frameRateOptions = new List<string>
                {
                    "60 FPS", "72 FPS", "90 FPS", "120 FPS", "无限制"
                };
                frameRateDropdown.AddOptions(frameRateOptions);
                frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
            }

            // 设置抗锯齿下拉菜单
            if (antiAliasingDropdown != null)
            {
                antiAliasingDropdown.ClearOptions();
                var aaOptions = new List<string> { "关闭", "MSAA 2x", "MSAA 4x", "MSAA 8x" };
                antiAliasingDropdown.AddOptions(aaOptions);
                antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
            }

            // 设置阴影质量下拉菜单
            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.ClearOptions();
                var shadowOptions = new List<string> { "关闭", "低", "中", "高" };
                shadowQualityDropdown.AddOptions(shadowOptions);
                shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
            }

            // 设置纹理质量下拉菜单
            if (textureQualityDropdown != null)
            {
                textureQualityDropdown.ClearOptions();
                var textureOptions = new List<string> { "低", "中", "高", "超高" };
                textureQualityDropdown.AddOptions(textureOptions);
                textureQualityDropdown.onValueChanged.AddListener(OnTextureQualityChanged);
            }

            // 设置各向异性过滤下拉菜单
            if (anisotropicFilteringDropdown != null)
            {
                anisotropicFilteringDropdown.ClearOptions();
                var anisoOptions = new List<string> { "关闭", "2x", "4x", "8x", "16x" };
                anisotropicFilteringDropdown.AddOptions(anisoOptions);
                anisotropicFilteringDropdown.onValueChanged.AddListener(OnAnisotropicFilteringChanged);
            }

            // 设置开关组件
            if (postProcessingToggle != null)
                postProcessingToggle.onValueChanged.AddListener(OnPostProcessingChanged);

            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            if (foveatedRenderingToggle != null)
                foveatedRenderingToggle.onValueChanged.AddListener(OnFoveatedRenderingChanged);

            if (fixedFoveatedRenderingToggle != null)
                fixedFoveatedRenderingToggle.onValueChanged.AddListener(OnFixedFoveatedRenderingChanged);

            if (snapTurnToggle != null)
                snapTurnToggle.onValueChanged.AddListener(OnSnapTurnChanged);

            if (motionSicknessToggle != null)
                motionSicknessToggle.onValueChanged.AddListener(OnMotionSicknessChanged);

            if (vignetteToggle != null)
                vignetteToggle.onValueChanged.AddListener(OnVignetteChanged);

            if (softParticlesToggle != null)
                softParticlesToggle.onValueChanged.AddListener(OnSoftParticlesChanged);

            if (realtimeReflectionsToggle != null)
                realtimeReflectionsToggle.onValueChanged.AddListener(OnRealtimeReflectionsChanged);

            if (showFPSToggle != null)
                showFPSToggle.onValueChanged.AddListener(OnShowFPSChanged);

            if (performanceStatsToggle != null)
                performanceStatsToggle.onValueChanged.AddListener(OnPerformanceStatsChanged);

            // 设置滑块组件
            if (renderScaleSlider != null)
            {
                renderScaleSlider.minValue = 0.5f;
                renderScaleSlider.maxValue = 2.0f;
                renderScaleSlider.onValueChanged.AddListener(OnRenderScaleChanged);
            }

            if (targetFrameRateSlider != null)
            {
                targetFrameRateSlider.minValue = 60f;
                targetFrameRateSlider.maxValue = 120f;
                targetFrameRateSlider.onValueChanged.AddListener(OnTargetFrameRateChanged);
            }

            if (comfortLevelSlider != null)
            {
                comfortLevelSlider.minValue = 0f;
                comfortLevelSlider.maxValue = 1f;
                comfortLevelSlider.onValueChanged.AddListener(OnComfortLevelChanged);
            }
        }

        #endregion

        #region 设置事件处理

        /// <summary>
        /// 渲染质量改变事件
        /// </summary>
        private void OnRenderQualityChanged(int value)
        {
            var quality = (RenderQuality)value;

            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.renderQuality = quality;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyRenderQuality(quality);

            // 播放触觉反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeSelect);
            }

            Debug.Log($"Render quality changed to: {quality}");
        }

        /// <summary>
        /// 分辨率改变事件
        /// </summary>
        private void OnResolutionChanged(int value)
        {
            var resolution = (ResolutionSetting)value;

            // 直接应用分辨率设置，不保存到VideoSettings结构中
            ApplyResolution(resolution);
            Debug.Log($"Resolution changed to: {resolution}");
        }

        /// <summary>
        /// 帧率改变事件
        /// </summary>
        private void OnFrameRateChanged(int value)
        {
            var frameRateLimit = GetFrameRateLimitFromIndex(value);

            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.targetFrameRate = (int)frameRateLimit;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyFrameRateLimit(frameRateLimit);
            Debug.Log($"Frame rate changed to: {frameRateLimit}");
        }

        /// <summary>
        /// 抗锯齿改变事件
        /// </summary>
        private void OnAntiAliasingChanged(int value)
        {
            var antiAliasing = (AntiAliasingLevel)value;

            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.antiAliasing = (AntiAliasing)value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyAntiAliasing(antiAliasing);
            Debug.Log($"Anti-aliasing changed to: {antiAliasing}");
        }

        /// <summary>
        /// 阴影质量改变事件
        /// </summary>
        private void OnShadowQualityChanged(int value)
        {
            var shadowQuality = (ShadowQualityLevel)value;

            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.shadowQuality = shadowQuality;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyShadowQuality(shadowQuality);
            Debug.Log($"Shadow quality changed to: {shadowQuality}");
        }

        /// <summary>
        /// 后处理改变事件
        /// </summary>
        private void OnPostProcessingChanged(bool value)
        {
            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.enablePostProcessing = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyPostProcessing(value);
            Debug.Log($"Post-processing: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 垂直同步改变事件
        /// </summary>
        private void OnVSyncChanged(bool value)
        {
            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.enableVSync = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            QualitySettings.vSyncCount = value ? 1 : 0;
            Debug.Log($"VSync: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 渲染缩放改变事件
        /// </summary>
        private void OnRenderScaleChanged(float value)
        {
            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.renderScale = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyRenderScale(value);
            Debug.Log($"Render scale changed to: {value:F2}");
        }

        /// <summary>
        /// 目标帧率改变事件
        /// </summary>
        private void OnTargetFrameRateChanged(float value)
        {
            int targetFrameRate = Mathf.RoundToInt(value);
            Application.targetFrameRate = targetFrameRate;
            Debug.Log($"Target frame rate changed to: {targetFrameRate}");
        }

        /// <summary>
        /// 中心凹渲染改变事件
        /// </summary>
        private void OnFoveatedRenderingChanged(bool value)
        {
            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.foveatedRendering = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyFoveatedRendering(value);
            Debug.Log($"Foveated rendering: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 固定中心凹渲染改变事件
        /// </summary>
        private void OnFixedFoveatedRenderingChanged(bool value)
        {
            // 固定中心凹渲染设置不保存到VideoSettings结构中，直接应用
            ApplyFixedFoveatedRendering(value);
            Debug.Log($"Fixed foveated rendering: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 舒适度等级改变事件
        /// </summary>
        private void OnComfortLevelChanged(float value)
        {
            var comfortLevel = (VRComfortLevel)Mathf.RoundToInt(value * 3); // 0-3

            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.comfortSettings.comfortLevel = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyComfortSettings(value);
            Debug.Log($"Comfort level changed to: {comfortLevel}");
        }

        /// <summary>
        /// 瞬移转向改变事件
        /// </summary>
        private void OnSnapTurnChanged(bool value)
        {
            // 这个设置应该在控制设置中，但可能影响视觉体验
            Debug.Log($"Snap turn: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 晕动症减缓改变事件
        /// </summary>
        private void OnMotionSicknessChanged(bool value)
        {
            var videoSettings = settingsManager.GetVideoSettings();
            videoSettings.comfortSettings.motionSicknessReduction = value;
            settingsManager.UpdateVideoSettings(videoSettings);

            ApplyMotionSicknessReduction(value);
            Debug.Log($"Motion sickness reduction: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 晕影效果改变事件
        /// </summary>
        private void OnVignetteChanged(bool value)
        {
            // 晕影效果设置不保存到VideoSettings结构中，直接应用
            ApplyVignetteEffect(value);
            Debug.Log($"Vignette effect: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 纹理质量改变事件
        /// </summary>
        private void OnTextureQualityChanged(int value)
        {
            QualitySettings.globalTextureMipmapLimit = 3 - value; // 反向映射：0=Ultra, 3=Low
            Debug.Log($"Texture quality changed to: {value}");
        }

        /// <summary>
        /// 各向异性过滤改变事件
        /// </summary>
        private void OnAnisotropicFilteringChanged(int value)
        {
            switch (value)
            {
                case 0: QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable; break;
                case 1: QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable; break;
                case 2: QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; break;
                default: QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable; break;
            }
            Debug.Log($"Anisotropic filtering changed to: {value}");
        }

        /// <summary>
        /// 软粒子改变事件
        /// </summary>
        private void OnSoftParticlesChanged(bool value)
        {
            // 需要在URP资产中设置软粒子
            if (urpAsset != null)
            {
                // urpAsset.supportsSoftParticles = value; // URP中可能需要其他方式
            }
            Debug.Log($"Soft particles: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 实时反射改变事件
        /// </summary>
        private void OnRealtimeReflectionsChanged(bool value)
        {
            // 需要控制反射探针的实时更新
            var reflectionProbes = FindObjectsOfType<ReflectionProbe>();
            foreach (var probe in reflectionProbes)
            {
                probe.mode = value ? UnityEngine.Rendering.ReflectionProbeMode.Realtime :
                                   UnityEngine.Rendering.ReflectionProbeMode.Baked;
            }
            Debug.Log($"Realtime reflections: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 显示FPS改变事件
        /// </summary>
        private void OnShowFPSChanged(bool value)
        {
            if (currentFPSText != null)
            {
                currentFPSText.gameObject.SetActive(value);
            }
            Debug.Log($"Show FPS: {(value ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 性能统计改变事件
        /// </summary>
        private void OnPerformanceStatsChanged(bool value)
        {
            if (gpuMemoryText != null)
            {
                gpuMemoryText.gameObject.SetActive(value);
            }

            // 启用/禁用Unity性能分析器
            if (value)
            {
                UnityEngine.Profiling.Profiler.enabled = true;
            }
            else
            {
                UnityEngine.Profiling.Profiler.enabled = false;
            }

            Debug.Log($"Performance stats: {(value ? "Enabled" : "Disabled")}");
        }

        #endregion

        #region 设置应用

        /// <summary>
        /// 应用渲染质量设置
        /// </summary>
        /// <param name="quality">渲染质量</param>
        private void ApplyRenderQuality(RenderQuality quality)
        {
            QualitySettings.SetQualityLevel((int)quality);

            if (qualityPresets != null && qualityPresets.Length > (int)quality)
            {
                var preset = qualityPresets[(int)quality];
                // 注意：currentRenderPipeline是只读属性，不能在运行时修改
                // 渲染管线需要在项目设置中配置
                Debug.Log($"Quality preset: {preset.name}");
            }
        }

        /// <summary>
        /// 应用抗锯齿设置
        /// </summary>
        /// <param name="antiAliasing">抗锯齿级别</param>
        private void ApplyAntiAliasing(AntiAliasingLevel antiAliasing)
        {
            if (urpAsset != null)
            {
                switch (antiAliasing)
                {
                    case AntiAliasingLevel.None:
                        urpAsset.msaaSampleCount = 1;
                        break;
                    case AntiAliasingLevel.MSAA_2x:
                        urpAsset.msaaSampleCount = 2;
                        break;
                    case AntiAliasingLevel.MSAA_4x:
                        urpAsset.msaaSampleCount = 4;
                        break;
                    case AntiAliasingLevel.MSAA_8x:
                        urpAsset.msaaSampleCount = 8;
                        break;
                }
            }
        }

        /// <summary>
        /// 应用阴影质量设置
        /// </summary>
        /// <param name="shadowQuality">阴影质量</param>
        private void ApplyShadowQuality(ShadowQualityLevel shadowQuality)
        {
            switch (shadowQuality)
            {
                case ShadowQualityLevel.Disabled:
                    QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                    break;
                case ShadowQualityLevel.Low:
                    QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Low;
                    break;
                case ShadowQualityLevel.Medium:
                    QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
                    break;
                case ShadowQualityLevel.High:
                    QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                    QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
                    break;
            }
        }

        /// <summary>
        /// 应用分辨率设置
        /// </summary>
        /// <param name="resolution">分辨率设置</param>
        private void ApplyResolution(ResolutionSetting resolution)
        {
            switch (resolution)
            {
                case ResolutionSetting.Auto:
                    // 使用当前屏幕分辨率
                    break;
                case ResolutionSetting.Low_1280x720:
                    Screen.SetResolution(1280, 720, false);
                    break;
                case ResolutionSetting.Medium_1920x1080:
                    Screen.SetResolution(1920, 1080, false);
                    break;
                case ResolutionSetting.High_2560x1440:
                    Screen.SetResolution(2560, 1440, false);
                    break;
                case ResolutionSetting.Ultra_3840x2160:
                    Screen.SetResolution(3840, 2160, false);
                    break;
            }
        }

        /// <summary>
        /// 应用帧率限制设置
        /// </summary>
        /// <param name="frameRateLimit">帧率限制</param>
        private void ApplyFrameRateLimit(FrameRateLimit frameRateLimit)
        {
            Application.targetFrameRate = (int)frameRateLimit;
        }

        /// <summary>
        /// 从下拉菜单索引获取帧率限制
        /// </summary>
        private FrameRateLimit GetFrameRateLimitFromIndex(int index)
        {
            switch (index)
            {
                case 0: return FrameRateLimit.FPS_60;
                case 1: return FrameRateLimit.FPS_72;
                case 2: return FrameRateLimit.FPS_90;
                case 3: return FrameRateLimit.FPS_120;
                case 4: return FrameRateLimit.Unlimited;
                default: return FrameRateLimit.FPS_90;
            }
        }

        /// <summary>
        /// 应用后处理设置
        /// </summary>
        /// <param name="enabled">是否启用后处理</param>
        private void ApplyPostProcessing(bool enabled)
        {
            // 查找并控制后处理体积
            var postProcessVolumes = FindObjectsOfType<UnityEngine.Rendering.Volume>();
            foreach (var volume in postProcessVolumes)
            {
                volume.enabled = enabled;
            }
        }

        /// <summary>
        /// 应用渲染缩放
        /// </summary>
        /// <param name="scale">渲染缩放值</param>
        private void ApplyRenderScale(float scale)
        {
            if (urpAsset != null)
            {
                urpAsset.renderScale = scale;
            }

            // VR特定的渲染缩放设置
            if (UnityEngine.XR.XRSettings.enabled)
            {
                UnityEngine.XR.XRSettings.renderViewportScale = scale;
            }
        }

        /// <summary>
        /// 应用中心凹渲染设置
        /// </summary>
        /// <param name="enabled">是否启用中心凹渲染</param>
        private void ApplyFoveatedRendering(bool enabled)
        {
            // VR SDK特定的中心凹渲染设置
            // 这里需要根据使用的VR SDK（如Oculus SDK）进行具体实现
            Debug.Log($"Foveated rendering: {(enabled ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// 应用固定中心凹渲染设置
        /// </summary>
        /// <param name="enabled">是否启用固定中心凹渲染</param>
        private void ApplyFixedFoveatedRendering(bool enabled)
        {
            // Oculus特定的固定中心凹渲染设置
            Debug.Log($"Fixed foveated rendering: {(enabled ? "Enabled" : "Disabled")}");
        }

        #endregion

        #region 舒适度设置应用

        /// <summary>
        /// 应用舒适度设置
        /// </summary>
        /// <param name="comfortLevel">舒适度等级 (0-1)</param>
        private void ApplyComfortSettings(float comfortLevel)
        {
            // 根据舒适度等级调整各种设置
            if (comfortLevel < 0.3f) // 高舒适度
            {
                // 降低帧率要求，增加稳定性
                Application.targetFrameRate = 60;

                // 启用更多舒适度功能
                ApplyMotionSicknessReduction(true);
                ApplyVignetteEffect(true);
            }
            else if (comfortLevel > 0.7f) // 低舒适度（高性能）
            {
                // 提高帧率，减少舒适度限制
                Application.targetFrameRate = 90;

                ApplyMotionSicknessReduction(false);
                ApplyVignetteEffect(false);
            }
        }

        /// <summary>
        /// 应用晕动症减缓设置
        /// </summary>
        /// <param name="enabled">是否启用晕动症减缓</param>
        private void ApplyMotionSicknessReduction(bool enabled)
        {
            if (enabled)
            {
                // 降低移动速度
                // 启用隧道视觉效果
                // 减少快速移动
                Debug.Log("Motion sickness reduction enabled");
            }
            else
            {
                Debug.Log("Motion sickness reduction disabled");
            }
        }

        /// <summary>
        /// 应用晕影效果
        /// </summary>
        /// <param name="enabled">是否启用晕影效果</param>
        private void ApplyVignetteEffect(bool enabled)
        {
            // 查找并控制晕影后处理效果
            var postProcessVolumes = FindObjectsOfType<UnityEngine.Rendering.Volume>();
            foreach (var volume in postProcessVolumes)
            {
                // 这里需要具体的后处理配置文件支持
                Debug.Log($"Vignette effect: {(enabled ? "Enabled" : "Disabled")}");
            }
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 更新性能监控
        /// </summary>
        private void UpdatePerformanceMonitoring()
        {
            frameCount++;
            fpsUpdateTimer += Time.unscaledDeltaTime;

            if (fpsUpdateTimer >= FPS_UPDATE_INTERVAL)
            {
                float fps = frameCount / fpsUpdateTimer;

                // 更新FPS显示
                if (currentFPSText != null && currentFPSText.gameObject.activeInHierarchy)
                {
                    currentFPSText.text = $"FPS: {fps:F1}";
                }

                // 更新GPU内存显示
                if (gpuMemoryText != null && gpuMemoryText.gameObject.activeInHierarchy)
                {
                    long gpuMemory = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
                    gpuMemoryText.text = $"GPU内存: {gpuMemory / (1024 * 1024):F1} MB";
                }

                // 重置计数器
                frameCount = 0;
                fpsUpdateTimer = 0f;
            }
        }

        #endregion

        #region 面板刷新和预设

        /// <summary>
        /// 刷新面板显示
        /// </summary>
        public void RefreshPanel()
        {
            if (settingsManager == null) return;

            var videoSettings = settingsManager.GetVideoSettings();

            // 更新渲染设置UI
            if (qualityDropdown != null)
                qualityDropdown.value = (int)videoSettings.renderQuality;

            // 分辨率下拉菜单设置为默认值，因为VideoSettings中不存储分辨率
            if (resolutionDropdown != null)
                resolutionDropdown.value = 0; // 默认为Auto

            if (frameRateDropdown != null)
                frameRateDropdown.value = GetFrameRateDropdownIndex((FrameRateLimit)videoSettings.targetFrameRate);

            if (antiAliasingDropdown != null)
                antiAliasingDropdown.value = (int)videoSettings.antiAliasing;

            if (shadowQualityDropdown != null)
                shadowQualityDropdown.value = (int)videoSettings.shadowQuality;

            // 更新开关状态
            if (postProcessingToggle != null)
                postProcessingToggle.isOn = videoSettings.enablePostProcessing;

            if (vsyncToggle != null)
                vsyncToggle.isOn = videoSettings.enableVSync;

            if (foveatedRenderingToggle != null)
                foveatedRenderingToggle.isOn = videoSettings.foveatedRendering;

            // 固定中心凹渲染设置为默认值，因为VideoSettings中不存储此设置
            if (fixedFoveatedRenderingToggle != null)
                fixedFoveatedRenderingToggle.isOn = false;

            if (motionSicknessToggle != null)
                motionSicknessToggle.isOn = videoSettings.comfortSettings.motionSicknessReduction;

            // 晕影效果设置为默认值，因为VideoSettings中不存储此设置
            if (vignetteToggle != null)
                vignetteToggle.isOn = false;

            // 更新滑块数值
            if (renderScaleSlider != null)
                renderScaleSlider.value = videoSettings.renderScale;

            if (comfortLevelSlider != null)
                comfortLevelSlider.value = videoSettings.comfortSettings.comfortLevel;
        }

        /// <summary>
        /// 获取帧率下拉菜单索引
        /// </summary>
        private int GetFrameRateDropdownIndex(FrameRateLimit frameRateLimit)
        {
            switch (frameRateLimit)
            {
                case FrameRateLimit.FPS_60: return 0;
                case FrameRateLimit.FPS_72: return 1;
                case FrameRateLimit.FPS_90: return 2;
                case FrameRateLimit.FPS_120: return 3;
                case FrameRateLimit.Unlimited: return 4;
                default: return 2; // 默认90FPS
            }
        }

        /// <summary>
        /// 应用性能预设
        /// </summary>
        /// <param name="preset">预设名称 (Performance/Balanced/Quality)</param>
        public void ApplyPerformancePreset(string preset)
        {
            var videoSettings = settingsManager.GetVideoSettings();

            switch (preset.ToLower())
            {
                case "performance":
                    videoSettings.renderQuality = RenderQuality.Low;
                    videoSettings.antiAliasing = (AntiAliasing)((int)AntiAliasingLevel.None);
                    videoSettings.shadowQuality = ShadowQualityLevel.Low;
                    videoSettings.enablePostProcessing = false;
                    videoSettings.renderScale = 0.8f;
                    videoSettings.targetFrameRate = (int)FrameRateLimit.FPS_90;
                    break;

                case "balanced":
                    videoSettings.renderQuality = RenderQuality.Medium;
                    videoSettings.antiAliasing = (AntiAliasing)((int)AntiAliasingLevel.MSAA_2x);
                    videoSettings.shadowQuality = ShadowQualityLevel.Medium;
                    videoSettings.enablePostProcessing = true;
                    videoSettings.renderScale = 1.0f;
                    videoSettings.targetFrameRate = (int)FrameRateLimit.FPS_72;
                    break;

                case "quality":
                    videoSettings.renderQuality = RenderQuality.High;
                    videoSettings.antiAliasing = (AntiAliasing)((int)AntiAliasingLevel.MSAA_4x);
                    videoSettings.shadowQuality = ShadowQualityLevel.High;
                    videoSettings.enablePostProcessing = true;
                    videoSettings.renderScale = 1.2f;
                    videoSettings.targetFrameRate = (int)FrameRateLimit.FPS_60;
                    break;
            }

            settingsManager.UpdateVideoSettings(videoSettings);
            RefreshPanel();

            // 播放确认反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
            }

            Debug.Log($"Applied {preset} preset");
        }

        #endregion

        #region 事件注册

        /// <summary>
        /// 注册设置事件
        /// </summary>
        private void RegisterSettingsEvents()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnVideoSettingsChanged += OnVideoSettingsUpdated;
            }
        }

        /// <summary>
        /// 取消注册设置事件
        /// </summary>
        private void UnregisterSettingsEvents()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnVideoSettingsChanged -= OnVideoSettingsUpdated;
            }
        }

        /// <summary>
        /// 视频设置更新事件处理
        /// </summary>
        private void OnVideoSettingsUpdated(VideoSettings videoSettings)
        {
            RefreshPanel();
        }

        #endregion

        #region 清理

        private void OnDestroy()
        {
            UnregisterSettingsEvents();
        }

        #endregion
    }
}