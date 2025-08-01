using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;
using PongHub.Core;

namespace PongHub.MR
{
    /// <summary>
    /// 环境融合系统
    /// 负责虚拟对象与真实环境的自然融合，包括材质转换、光照匹配、遮挡处理
    /// </summary>
    public class EnvironmentBlendingSystem : MonoBehaviour
    {
        [Header("Blending Settings")]
        [SerializeField]
        [Tooltip("需要融合的虚拟对象图层")]
        private LayerMask m_virtualObjectLayers = -1;

        [SerializeField]
        [Tooltip("MR兼容着色器")]
        private Shader m_mrCompatibleShader;

        [SerializeField]
        [Tooltip("是否启用遮挡处理")]
        private bool m_enableOcclusion = true;

        [SerializeField]
        [Tooltip("环境光强度调整")]
        [Range(0f, 2f)]
        private float m_environmentLightIntensity = 1.0f;

        [SerializeField]
        [Tooltip("环境光颜色调整")]
        private Color m_environmentLightColor = Color.white;

        [Header("Material Conversion")]
        [SerializeField]
        [Tooltip("自动转换材质为MR兼容")]
        private bool m_autoConvertMaterials = true;

        [SerializeField]
        [Tooltip("保持原始材质备份")]
        private bool m_keepOriginalMaterials = true;

        [SerializeField]
        [Tooltip("透明度调整因子")]
        [Range(0f, 1f)]
        private float m_opacityFactor = 0.9f;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("启用LOD系统")]
        private bool m_enableLOD = true;

        [SerializeField]
        [Tooltip("遮挡剔除启用")]
        private bool m_enableOcclusionCulling = true;

        [SerializeField]
        [Tooltip("最大同时处理的对象数量")]
        [Range(10, 100)]
        private int m_maxConcurrentObjects = 50;

        // 组件引用
        private MRPassthroughManager m_passthroughManager;
        private Camera m_mainCamera;
        private Light m_mainLight;

        // 材质管理
        private MaterialPropertyBlock m_propertyBlock;
        private Dictionary<Renderer, Material[]> m_originalMaterials = new Dictionary<Renderer, Material[]>();
        private Dictionary<Renderer, Material[]> m_mrMaterials = new Dictionary<Renderer, Material[]>();

        // 状态管理
        private bool m_isInitialized = false;
        private bool m_isMRMode = false;
        private List<GameObject> m_virtualObjects = new List<GameObject>();
        private Dictionary<GameObject, LODGroup> m_lodGroups = new Dictionary<GameObject, LODGroup>();

        // 性能监控
        private int m_processedObjectCount = 0;
        private float m_lastProcessTime = 0f;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 是否处于MR模式
        /// </summary>
        public bool IsMRMode => m_isMRMode;

        /// <summary>
        /// 当前处理的对象数量
        /// </summary>
        public int ProcessedObjectCount => m_processedObjectCount;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void Update()
        {
            if (m_isInitialized)
            {
                UpdateEnvironmentLighting();
                MonitorPerformance();
            }
        }

        private void OnDestroy()
        {
            CleanupSystem();
        }

        private void InitializeComponents()
        {
            // 获取必需组件
            m_passthroughManager = FindObjectOfType<MRPassthroughManager>();
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
                m_mainCamera = FindObjectOfType<Camera>();

            m_mainLight = FindObjectOfType<Light>();

            // 创建材质属性块
            m_propertyBlock = new MaterialPropertyBlock();

            Debug.Log("[EnvironmentBlendingSystem] Components initialized");
        }

        private IEnumerator InitializeAsync()
        {
            // 等待其他系统初始化
            yield return new WaitForSeconds(0.5f);

            // 如果没有指定MR着色器，尝试找到默认的
            if (m_mrCompatibleShader == null)
            {
                m_mrCompatibleShader = Shader.Find("Universal Render Pipeline/Lit");
                if (m_mrCompatibleShader == null)
                    m_mrCompatibleShader = Shader.Find("Standard");
            }

            // 查找并注册虚拟对象
            RegisterVirtualObjects();

            // 监听Passthrough管理器事件
            if (m_passthroughManager != null)
            {
                m_passthroughManager.OnPassthroughModeChanged.AddListener(OnPassthroughModeChanged);
            }

            m_isInitialized = true;
            Debug.Log("[EnvironmentBlendingSystem] Initialization complete");
        }

        private void RegisterVirtualObjects()
        {
            // 查找所有指定图层的对象
            var allRenderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (IsInTargetLayer(renderer.gameObject.layer))
                {
                    if (!m_virtualObjects.Contains(renderer.gameObject))
                    {
                        m_virtualObjects.Add(renderer.gameObject);
                        
                        // 备份原始材质
                        if (m_keepOriginalMaterials)
                        {
                            BackupOriginalMaterials(renderer);
                        }
                    }
                }
            }

            Debug.Log($"[EnvironmentBlendingSystem] Registered {m_virtualObjects.Count} virtual objects");
        }

        private bool IsInTargetLayer(int layer)
        {
            return (m_virtualObjectLayers.value & (1 << layer)) != 0;
        }

        private void BackupOriginalMaterials(Renderer renderer)
        {
            if (renderer == null) return;

            if (!m_originalMaterials.ContainsKey(renderer))
            {
                m_originalMaterials[renderer] = renderer.materials;
            }
        }

        private void OnPassthroughModeChanged(MRPassthroughManager.PassthroughMode mode)
        {
            bool enteringMRMode = mode != MRPassthroughManager.PassthroughMode.Disabled;
            
            if (enteringMRMode != m_isMRMode)
            {
                m_isMRMode = enteringMRMode;
                
                if (m_isMRMode)
                {
                    SetupMRMaterials();
                }
                else
                {
                    RestoreOriginalMaterials();
                }
                
                Debug.Log($"[EnvironmentBlendingSystem] MR mode: {m_isMRMode}");
            }
        }

        /// <summary>
        /// 设置MR兼容材质
        /// </summary>
        public void SetupMRMaterials()
        {
            if (!m_isInitialized || !m_autoConvertMaterials) return;

            StartCoroutine(SetupMRMaterialsAsync());
        }

        private IEnumerator SetupMRMaterialsAsync()
        {
            int processedCount = 0;
            
            foreach (var virtualObject in m_virtualObjects)
            {
                if (virtualObject == null) continue;

                var renderer = virtualObject.GetComponent<Renderer>();
                if (renderer == null) continue;

                // 创建MR兼容材质
                CreateMRMaterials(renderer);
                
                processedCount++;
                
                // 性能控制：每处理几个对象暂停一帧
                if (processedCount % 5 == 0)
                {
                    yield return null;
                }
                
                // 限制同时处理的对象数量
                if (processedCount >= m_maxConcurrentObjects)
                {
                    yield return new WaitForSeconds(0.1f);
                    processedCount = 0;
                }
            }

            m_processedObjectCount = processedCount;
            m_lastProcessTime = Time.time;
            
            Debug.Log($"[EnvironmentBlendingSystem] Setup MR materials for {processedCount} objects");
        }

        private void CreateMRMaterials(Renderer renderer)
        {
            if (renderer == null || m_mrCompatibleShader == null) return;

            var originalMaterials = renderer.materials;
            var mrMaterials = new Material[originalMaterials.Length];

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                var originalMat = originalMaterials[i];
                if (originalMat == null) continue;

                // 创建MR兼容材质
                var mrMat = new Material(m_mrCompatibleShader);
                mrMat.name = originalMat.name + "_MR";

                // 复制基础属性
                CopyMaterialProperties(originalMat, mrMat);
                
                // 应用MR特定设置
                ApplyMRSettings(mrMat);
                
                mrMaterials[i] = mrMat;
            }

            // 缓存MR材质
            m_mrMaterials[renderer] = mrMaterials;
            
            // 应用MR材质
            renderer.materials = mrMaterials;
        }

        private void CopyMaterialProperties(Material source, Material target)
        {
            // 复制基础纹理
            if (source.HasProperty("_MainTex") && target.HasProperty("_BaseMap"))
            {
                target.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
            }
            
            if (source.HasProperty("_BaseMap") && target.HasProperty("_BaseMap"))
            {
                target.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            }

            // 复制颜色
            if (source.HasProperty("_Color") && target.HasProperty("_BaseColor"))
            {
                target.SetColor("_BaseColor", source.GetColor("_Color"));
            }
            
            if (source.HasProperty("_BaseColor") && target.HasProperty("_BaseColor"))
            {
                target.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            }

            // 复制金属度、光滑度等PBR属性
            if (source.HasProperty("_Metallic") && target.HasProperty("_Metallic"))
            {
                target.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            }
            
            if (source.HasProperty("_Smoothness") && target.HasProperty("_Smoothness"))
            {
                target.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
            }
        }

        private void ApplyMRSettings(Material material)
        {
            if (material == null) return;

            // 设置渲染模式为透明或半透明
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1); // Transparent
            }
            
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0); // Alpha
            }

            // 调整透明度
            if (material.HasProperty("_BaseColor"))
            {
                var color = material.GetColor("_BaseColor");
                color.a *= m_opacityFactor;
                material.SetColor("_BaseColor", color);
            }

            // 启用深度测试但禁用深度写入
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.LessEqual);

            // 设置渲染队列
            material.renderQueue = (int)RenderQueue.Transparent;

            // 启用关键字
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        /// <summary>
        /// 恢复原始材质
        /// </summary>
        public void RestoreOriginalMaterials()
        {
            if (!m_keepOriginalMaterials) return;

            foreach (var kvp in m_originalMaterials)
            {
                var renderer = kvp.Key;
                var originalMaterials = kvp.Value;
                
                if (renderer != null && originalMaterials != null)
                {
                    renderer.materials = originalMaterials;
                }
            }

            // 清理MR材质
            foreach (var kvp in m_mrMaterials)
            {
                var mrMaterials = kvp.Value;
                if (mrMaterials != null)
                {
                    foreach (var mat in mrMaterials)
                    {
                        if (mat != null)
                        {
                            DestroyImmediate(mat);
                        }
                    }
                }
            }
            
            m_mrMaterials.Clear();
            
            Debug.Log("[EnvironmentBlendingSystem] Restored original materials");
        }

        /// <summary>
        /// 更新环境光照
        /// </summary>
        public void UpdateEnvironmentLighting()
        {
            if (!m_isMRMode || m_mainLight == null) return;

            // 动态调整环境光照以匹配真实环境
            var currentIntensity = m_mainLight.intensity;
            var targetIntensity = m_environmentLightIntensity;
            
            if (Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
            {
                m_mainLight.intensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 2f);
            }

            // 调整环境光颜色
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, m_environmentLightColor, Time.deltaTime);
        }

        /// <summary>
        /// 设置环境光照参数
        /// </summary>
        public void SetEnvironmentLighting(float intensity, Color color)
        {
            m_environmentLightIntensity = Mathf.Clamp(intensity, 0f, 2f);
            m_environmentLightColor = color;
            
            Debug.Log($"[EnvironmentBlendingSystem] Environment lighting set: intensity={intensity:F2}, color={color}");
        }

        /// <summary>
        /// 添加虚拟对象
        /// </summary>
        public void AddVirtualObject(GameObject obj)
        {
            if (obj == null || m_virtualObjects.Contains(obj)) return;

            m_virtualObjects.Add(obj);
            
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (m_keepOriginalMaterials)
                {
                    BackupOriginalMaterials(renderer);
                }
                
                if (m_isMRMode)
                {
                    CreateMRMaterials(renderer);
                }
            }

            // 如果启用LOD，添加LOD组件
            if (m_enableLOD && obj.GetComponent<LODGroup>() == null)
            {
                SetupLODForObject(obj);
            }

            Debug.Log($"[EnvironmentBlendingSystem] Added virtual object: {obj.name}");
        }

        /// <summary>
        /// 移除虚拟对象
        /// </summary>
        public void RemoveVirtualObject(GameObject obj)
        {
            if (obj == null || !m_virtualObjects.Contains(obj)) return;

            m_virtualObjects.Remove(obj);
            
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 恢复原始材质
                if (m_originalMaterials.ContainsKey(renderer))
                {
                    renderer.materials = m_originalMaterials[renderer];
                    m_originalMaterials.Remove(renderer);
                }
                
                // 清理MR材质
                if (m_mrMaterials.ContainsKey(renderer))
                {
                    var mrMaterials = m_mrMaterials[renderer];
                    foreach (var mat in mrMaterials)
                    {
                        if (mat != null)
                        {
                            DestroyImmediate(mat);
                        }
                    }
                    m_mrMaterials.Remove(renderer);
                }
            }

            // 清理LOD
            if (m_lodGroups.ContainsKey(obj))
            {
                m_lodGroups.Remove(obj);
            }

            Debug.Log($"[EnvironmentBlendingSystem] Removed virtual object: {obj.name}");
        }

        private void SetupLODForObject(GameObject obj)
        {
            var lodGroup = obj.AddComponent<LODGroup>();
            var renderers = obj.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                var lods = new LOD[3];
                
                // LOD 0 - 高质量 (0-50%)
                lods[0] = new LOD(0.5f, renderers);
                
                // LOD 1 - 中等质量 (50-80%)
                lods[1] = new LOD(0.2f, renderers);
                
                // LOD 2 - 低质量 (80-100%)
                lods[2] = new LOD(0.01f, renderers);
                
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
                
                m_lodGroups[obj] = lodGroup;
            }
        }

        private void MonitorPerformance()
        {
            // 简单的性能监控，如果处理时间过长则调整设置
            if (Time.time - m_lastProcessTime > 5f && m_processedObjectCount > m_maxConcurrentObjects)
            {
                // 自动调整最大并发处理数量
                m_maxConcurrentObjects = Mathf.Max(10, m_maxConcurrentObjects - 10);
                Debug.LogWarning($"[EnvironmentBlendingSystem] Performance optimization: reduced max concurrent objects to {m_maxConcurrentObjects}");
            }
        }

        /// <summary>
        /// 获取系统诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Environment Blending System Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"MR Mode: {m_isMRMode}");
            diagnostics.AppendLine($"Virtual Objects: {m_virtualObjects.Count}");
            diagnostics.AppendLine($"Processed Objects: {m_processedObjectCount}");
            diagnostics.AppendLine($"Original Materials Cached: {m_originalMaterials.Count}");
            diagnostics.AppendLine($"MR Materials Created: {m_mrMaterials.Count}");
            diagnostics.AppendLine($"Auto Convert Materials: {m_autoConvertMaterials}");
            diagnostics.AppendLine($"Enable Occlusion: {m_enableOcclusion}");
            diagnostics.AppendLine($"Environment Light Intensity: {m_environmentLightIntensity:F2}");
            diagnostics.AppendLine($"Opacity Factor: {m_opacityFactor:F2}");
            diagnostics.AppendLine($"Max Concurrent Objects: {m_maxConcurrentObjects}");
            diagnostics.AppendLine($"LOD Enabled: {m_enableLOD}");
            diagnostics.AppendLine($"LOD Groups: {m_lodGroups.Count}");
            
            return diagnostics.ToString();
        }

        private void CleanupSystem()
        {
            // 恢复所有原始材质
            RestoreOriginalMaterials();
            
            // 清理LOD组件
            foreach (var kvp in m_lodGroups)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
            
            m_lodGroups.Clear();
            m_virtualObjects.Clear();
            m_originalMaterials.Clear();
            
            Debug.Log("[EnvironmentBlendingSystem] System cleanup completed");
        }
    }
}