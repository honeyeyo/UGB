using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;
using PongHub.Core;

namespace PongHub.Performance
{
    /// <summary>
    /// 内存使用分析器
    /// 监控和分析内存使用情况，确保VR游戏在Meta Quest设备上的内存占用<50MB
    /// Epic-4 Story-16: 内存使用优化
    /// </summary>
    public class MemoryUsageProfiler : MonoBehaviour, IGameModeComponent
    {
        [Header("Memory Monitoring Settings / 内存监控设置")]
        [SerializeField]
        [Tooltip("Memory Warning Threshold / 内存警告阈值 - Memory usage threshold for warnings (MB)")]
        private float m_memoryWarningThreshold = 40f; // 40MB warning threshold

        [SerializeField]
        [Tooltip("Memory Critical Threshold / 内存严重阈值 - Memory usage threshold for critical warnings (MB)")]
        private float m_memoryCriticalThreshold = 50f; // 50MB critical threshold

        [SerializeField]
        [Tooltip("Update Interval / 更新间隔 - How often to update memory metrics (seconds)")]
        private float m_updateInterval = 1.0f;

        [SerializeField]
        [Tooltip("Enable Auto Cleanup / 启用自动清理 - Automatically trigger garbage collection when needed")]
        private bool m_enableAutoCleanup = true;

        [SerializeField]
        [Tooltip("Sample Count / 采样数量 - Number of memory samples to keep for analysis")]
        private int m_sampleCount = 60;

        [Header("Memory Categories / 内存分类")]
        [SerializeField]
        [Tooltip("Track Texture Memory / 跟踪纹理内存 - Monitor texture memory usage")]
        private bool m_trackTextureMemory = true;

        [SerializeField]
        [Tooltip("Track Mesh Memory / 跟踪网格内存 - Monitor mesh memory usage")]
        private bool m_trackMeshMemory = true;

        [SerializeField]
        [Tooltip("Track Audio Memory / 跟踪音频内存 - Monitor audio memory usage")]
        private bool m_trackAudioMemory = true;

        [SerializeField]
        [Tooltip("Track Script Memory / 跟踪脚本内存 - Monitor script memory usage")]
        private bool m_trackScriptMemory = true;

        [Header("Display Settings / 显示设置")]
        [SerializeField]
        [Tooltip("Show Memory HUD / 显示内存HUD - Show memory usage overlay")]
        private bool m_showMemoryHUD = false;

        [SerializeField]
        [Tooltip("HUD Position / HUD位置 - Position of memory HUD in world space")]
        private Vector3 m_hudPosition = new Vector3(-1.5f, 2.5f, 2);

        [SerializeField]
        [Tooltip("Memory HUD Canvas / 内存HUD画布 - Canvas for displaying memory information")]
        private Canvas m_memoryHudCanvas;

        [SerializeField]
        [Tooltip("Memory Text / 内存文本 - UI Text component for displaying memory stats")]
        private UnityEngine.UI.Text m_memoryText;

        // Memory tracking data / 内存跟踪数据
        private Queue<MemorySnapshot> m_memoryHistory = new Queue<MemorySnapshot>();
        private MemorySnapshot m_currentSnapshot;
        private List<MemoryWarning> m_activeMemoryWarnings = new List<MemoryWarning>();

        // Memory categories / 内存分类
        private Dictionary<string, Queue<float>> m_categoryHistory = new Dictionary<string, Queue<float>>();

        // Timing / 计时
        private float m_lastUpdateTime;

        // Memory optimization state / 内存优化状态
        private bool m_isOptimizing = false;
        private float m_lastGCTime = 0f;
        private int m_gcCallCount = 0;

        // Static instance / 静态实例
        public static MemoryUsageProfiler Instance { get; private set; }

        #region Properties / 属性

        /// <summary>
        /// 当前内存使用量（MB）
        /// </summary>
        public float CurrentMemoryUsage => m_currentSnapshot.TotalAllocatedMemory;

        /// <summary>
        /// 当前内存快照
        /// </summary>
        public MemorySnapshot CurrentSnapshot => m_currentSnapshot;

        /// <summary>
        /// 内存使用历史记录
        /// </summary>
        public IReadOnlyCollection<MemorySnapshot> MemoryHistory => m_memoryHistory.ToList().AsReadOnly();

        /// <summary>
        /// 活跃的内存警告
        /// </summary>
        public IReadOnlyList<MemoryWarning> ActiveWarnings => m_activeMemoryWarnings.AsReadOnly();

        /// <summary>
        /// 内存使用评级
        /// </summary>
        public MemoryUsageGrade CurrentMemoryGrade => GetMemoryUsageGrade();

        #endregion

        #region Unity Lifecycle / Unity生命周期

        private void Awake()
        {
            // Singleton pattern / 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCategoryTracking();
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

            InitializeMemoryHUD();
            m_lastUpdateTime = Time.unscaledTime;
            
            // Take initial snapshot / 获取初始快照
            TakeMemorySnapshot();
        }

        private void Update()
        {
            if (Time.unscaledTime - m_lastUpdateTime >= m_updateInterval)
            {
                UpdateMemoryMetrics();
                CheckMemoryWarnings();
                UpdateMemoryHUD();
                m_lastUpdateTime = Time.unscaledTime;
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
            // Adjust memory monitoring based on game mode / 根据游戏模式调整内存监控
            switch (newMode)
            {
                case GameMode.Menu:
                    // More aggressive cleanup in menu mode / 菜单模式下更积极的清理
                    m_enableAutoCleanup = true;
                    TriggerMemoryCleanup("Game mode switched to Menu");
                    break;
                case GameMode.Local:
                    // Standard monitoring for local mode / 本地模式标准监控
                    m_enableAutoCleanup = true;
                    break;
                case GameMode.Network:
                    // More conservative cleanup in network mode / 网络模式下更保守的清理
                    m_enableAutoCleanup = false; // 避免网络游戏中的卡顿
                    break;
            }

            Debug.Log($"[MemoryUsageProfiler] 游戏模式切换: {previousMode} → {newMode}, 当前内存使用: {CurrentMemoryUsage:F2}MB");
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // Memory profiling is active in all modes / 内存分析在所有模式下都活跃
            return true;
        }

        #endregion

        #region Memory Monitoring / 内存监控

        /// <summary>
        /// 更新内存指标
        /// </summary>
        private void UpdateMemoryMetrics()
        {
            TakeMemorySnapshot();
            UpdateCategoryTracking();
        }

        /// <summary>
        /// 获取内存快照
        /// </summary>
        private void TakeMemorySnapshot()
        {
            m_currentSnapshot = new MemorySnapshot
            {
                Timestamp = Time.unscaledTime,
                TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f), // Convert to MB
                TotalReservedMemory = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f),
                TotalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f),
                MonoHeapSize = Profiler.GetMonoHeapSizeLong() / (1024f * 1024f),
                MonoUsedSize = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f),
                TempAllocatorSize = Profiler.GetTempAllocatorSize() / (1024f * 1024f),
                TextureMemory = m_trackTextureMemory ? GetTextureMemoryUsage() : 0f,
                MeshMemory = m_trackMeshMemory ? GetMeshMemoryUsage() : 0f,
                AudioMemory = m_trackAudioMemory ? GetAudioMemoryUsage() : 0f,
                ScriptMemory = m_trackScriptMemory ? GetScriptMemoryUsage() : 0f
            };

            // Add to history / 添加到历史记录
            m_memoryHistory.Enqueue(m_currentSnapshot);
            if (m_memoryHistory.Count > m_sampleCount)
            {
                m_memoryHistory.Dequeue();
            }
        }

        /// <summary>
        /// 更新分类跟踪
        /// </summary>
        private void UpdateCategoryTracking()
        {
            var categories = new Dictionary<string, float>
            {
                ["Total"] = m_currentSnapshot.TotalAllocatedMemory,
                ["Mono"] = m_currentSnapshot.MonoUsedSize,
                ["Texture"] = m_currentSnapshot.TextureMemory,
                ["Mesh"] = m_currentSnapshot.MeshMemory,
                ["Audio"] = m_currentSnapshot.AudioMemory,
                ["Script"] = m_currentSnapshot.ScriptMemory
            };

            foreach (var category in categories)
            {
                if (!m_categoryHistory.ContainsKey(category.Key))
                {
                    m_categoryHistory[category.Key] = new Queue<float>();
                }

                m_categoryHistory[category.Key].Enqueue(category.Value);
                if (m_categoryHistory[category.Key].Count > m_sampleCount)
                {
                    m_categoryHistory[category.Key].Dequeue();
                }
            }
        }

        /// <summary>
        /// 检查内存警告
        /// </summary>
        private void CheckMemoryWarnings()
        {
            m_activeMemoryWarnings.Clear();

            float currentMemory = CurrentMemoryUsage;

            // Check critical threshold / 检查严重阈值
            if (currentMemory >= m_memoryCriticalThreshold)
            {
                m_activeMemoryWarnings.Add(new MemoryWarning
                {
                    Type = MemoryWarningType.Critical,
                    Message = $"内存使用严重超标: {currentMemory:F2}MB (上限: {m_memoryCriticalThreshold:F2}MB)",
                    CurrentUsage = currentMemory,
                    Threshold = m_memoryCriticalThreshold
                });

                // Force cleanup on critical memory usage / 严重内存使用时强制清理
                if (m_enableAutoCleanup)
                {
                    TriggerMemoryCleanup("Critical memory usage detected");
                }
            }
            // Check warning threshold / 检查警告阈值
            else if (currentMemory >= m_memoryWarningThreshold)
            {
                m_activeMemoryWarnings.Add(new MemoryWarning
                {
                    Type = MemoryWarningType.Warning,
                    Message = $"内存使用过高: {currentMemory:F2}MB (警告: {m_memoryWarningThreshold:F2}MB)",
                    CurrentUsage = currentMemory,
                    Threshold = m_memoryWarningThreshold
                });

                // Trigger cleanup on warning if enabled / 如果启用，在警告时触发清理
                if (m_enableAutoCleanup && Time.unscaledTime - m_lastGCTime > 5f) // Avoid frequent GC calls
                {
                    TriggerMemoryCleanup("High memory usage detected");
                }
            }

            // Check for memory leaks (rapid growth) / 检查内存泄漏（快速增长）
            CheckForMemoryLeaks();

            // Log warnings / 记录警告
            foreach (var warning in m_activeMemoryWarnings)
            {
                if (warning.Type == MemoryWarningType.Critical)
                {
                    Debug.LogError($"[MemoryUsageProfiler] {warning.Message}");
                }
                else
                {
                    Debug.LogWarning($"[MemoryUsageProfiler] {warning.Message}");
                }
            }
        }

        /// <summary>
        /// 检查内存泄漏
        /// </summary>
        private void CheckForMemoryLeaks()
        {
            if (m_memoryHistory.Count < 10) return; // Need enough samples

            var recentSnapshots = m_memoryHistory.TakeLast(10).ToList();
            float startMemory = recentSnapshots.First().TotalAllocatedMemory;
            float endMemory = recentSnapshots.Last().TotalAllocatedMemory;
            float growth = endMemory - startMemory;

            // If memory grew by more than 5MB in recent samples, warn about potential leak
            // 如果内存在最近的样本中增长超过5MB，警告可能的泄漏
            if (growth > 5f)
            {
                m_activeMemoryWarnings.Add(new MemoryWarning
                {
                    Type = MemoryWarningType.Leak,
                    Message = $"检测到可能的内存泄漏: {growth:F2}MB增长",
                    CurrentUsage = endMemory,
                    Threshold = startMemory
                });
            }
        }

        #endregion

        #region Memory Cleanup / 内存清理

        /// <summary>
        /// 触发内存清理
        /// </summary>
        public void TriggerMemoryCleanup(string reason = "Manual cleanup")
        {
            if (m_isOptimizing) return; // Avoid overlapping cleanup operations

            m_isOptimizing = true;
            m_lastGCTime = Time.unscaledTime;
            m_gcCallCount++;

            Debug.Log($"[MemoryUsageProfiler] 开始内存清理: {reason}, 当前使用: {CurrentMemoryUsage:F2}MB");

            float memoryBefore = CurrentMemoryUsage;

            // Unload unused assets / 卸载未使用的资源
            Resources.UnloadUnusedAssets();

            // Force garbage collection / 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // Update snapshot after cleanup / 清理后更新快照
            StartCoroutine(UpdateSnapshotAfterCleanup(memoryBefore, reason));
        }

        /// <summary>
        /// 清理后更新快照
        /// </summary>
        private System.Collections.IEnumerator UpdateSnapshotAfterCleanup(float memoryBefore, string reason)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // Wait a bit for cleanup to complete

            TakeMemorySnapshot();
            float memoryAfter = CurrentMemoryUsage;
            float memoryFreed = memoryBefore - memoryAfter;

            Debug.Log($"[MemoryUsageProfiler] 内存清理完成: {reason}\n" +
                     $"清理前: {memoryBefore:F2}MB\n" +
                     $"清理后: {memoryAfter:F2}MB\n" +
                     $"释放内存: {memoryFreed:F2}MB");

            m_isOptimizing = false;
        }

        #endregion

        #region Memory Category Tracking / 内存分类跟踪

        /// <summary>
        /// 初始化分类跟踪
        /// </summary>
        private void InitializeCategoryTracking()
        {
            var categories = new[] { "Total", "Mono", "Texture", "Mesh", "Audio", "Script" };
            foreach (var category in categories)
            {
                m_categoryHistory[category] = new Queue<float>();
            }
        }

        /// <summary>
        /// 获取纹理内存使用量
        /// </summary>
        private float GetTextureMemoryUsage()
        {
            // This is a simplified approach - in practice you'd use Unity's memory profiler API
            // 这是简化方法 - 实际使用中会用Unity的内存分析器API
            return Profiler.GetRuntimeMemorySizeLong(null) / (1024f * 1024f) * 0.3f; // Rough estimate
        }

        /// <summary>
        /// 获取网格内存使用量
        /// </summary>
        private float GetMeshMemoryUsage()
        {
            return Profiler.GetRuntimeMemorySizeLong(null) / (1024f * 1024f) * 0.2f; // Rough estimate
        }

        /// <summary>
        /// 获取音频内存使用量
        /// </summary>
        private float GetAudioMemoryUsage()
        {
            return Profiler.GetRuntimeMemorySizeLong(null) / (1024f * 1024f) * 0.1f; // Rough estimate
        }

        /// <summary>
        /// 获取脚本内存使用量
        /// </summary>
        private float GetScriptMemoryUsage()
        {
            return m_currentSnapshot.MonoUsedSize;
        }

        #endregion

        #region HUD Display / HUD显示

        /// <summary>
        /// 初始化内存HUD
        /// </summary>
        private void InitializeMemoryHUD()
        {
            if (m_memoryHudCanvas == null)
            {
                // Create memory HUD canvas / 创建内存HUD画布
                GameObject hudObject = new GameObject("Memory Usage HUD");
                hudObject.transform.SetParent(transform);
                hudObject.transform.localPosition = m_hudPosition;

                m_memoryHudCanvas = hudObject.AddComponent<Canvas>();
                m_memoryHudCanvas.renderMode = RenderMode.WorldSpace;
                m_memoryHudCanvas.worldCamera = Camera.main;

                // Create text component / 创建文本组件
                GameObject textObject = new GameObject("Memory Text");
                textObject.transform.SetParent(m_memoryHudCanvas.transform);

                m_memoryText = textObject.AddComponent<UnityEngine.UI.Text>();
                m_memoryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                m_memoryText.fontSize = 10;
                m_memoryText.color = Color.cyan;
                m_memoryText.alignment = TextAnchor.UpperLeft;

                var rectTransform = textObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(300, 200);
                rectTransform.localPosition = Vector3.zero;
            }

            UpdateMemoryHUDVisibility();
        }

        /// <summary>
        /// 更新内存HUD显示
        /// </summary>
        private void UpdateMemoryHUD()
        {
            if (!m_showMemoryHUD || m_memoryText == null) return;

            string memoryInfo = GetMemoryInfoString();
            m_memoryText.text = memoryInfo;

            // Update text color based on memory usage / 根据内存使用情况更新文本颜色
            MemoryUsageGrade grade = GetMemoryUsageGrade();
            m_memoryText.color = GetMemoryGradeColor(grade);
        }

        /// <summary>
        /// 更新内存HUD可见性
        /// </summary>
        private void UpdateMemoryHUDVisibility()
        {
            if (m_memoryHudCanvas != null)
            {
                m_memoryHudCanvas.gameObject.SetActive(m_showMemoryHUD);
            }
        }

        #endregion

        #region Utility Methods / 实用方法

        /// <summary>
        /// 获取内存使用评级
        /// </summary>
        private MemoryUsageGrade GetMemoryUsageGrade()
        {
            float usage = CurrentMemoryUsage;
            
            if (usage >= m_memoryCriticalThreshold) return MemoryUsageGrade.Critical;
            if (usage >= m_memoryWarningThreshold) return MemoryUsageGrade.Warning;
            if (usage >= m_memoryWarningThreshold * 0.8f) return MemoryUsageGrade.Caution;
            if (usage >= m_memoryWarningThreshold * 0.6f) return MemoryUsageGrade.Good;
            return MemoryUsageGrade.Excellent;
        }

        /// <summary>
        /// 获取内存评级颜色
        /// </summary>
        private Color GetMemoryGradeColor(MemoryUsageGrade grade)
        {
            switch (grade)
            {
                case MemoryUsageGrade.Excellent: return Color.green;
                case MemoryUsageGrade.Good: return Color.yellow;
                case MemoryUsageGrade.Caution: return new Color(1f, 0.5f, 0f); // Orange
                case MemoryUsageGrade.Warning: return Color.red;
                case MemoryUsageGrade.Critical: return Color.magenta;
                default: return Color.cyan;
            }
        }

        /// <summary>
        /// 获取内存信息字符串
        /// </summary>
        private string GetMemoryInfoString()
        {
            var snapshot = m_currentSnapshot;
            return $"Memory Usage Profiler\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"Total: {snapshot.TotalAllocatedMemory:F2}MB\n" +
                   $"Reserved: {snapshot.TotalReservedMemory:F2}MB\n" +
                   $"Mono: {snapshot.MonoUsedSize:F2}MB\n" +
                   $"Texture: {snapshot.TextureMemory:F2}MB\n" +
                   $"Mesh: {snapshot.MeshMemory:F2}MB\n" +
                   $"Audio: {snapshot.AudioMemory:F2}MB\n" +
                   $"Grade: {GetMemoryUsageGrade()}\n" +
                   $"GC Calls: {m_gcCallCount}\n" +
                   $"Warnings: {m_activeMemoryWarnings.Count}";
        }

        #endregion

        #region Public API / 公共API

        /// <summary>
        /// 切换内存HUD显示
        /// </summary>
        public void ToggleMemoryHUD()
        {
            m_showMemoryHUD = !m_showMemoryHUD;
            UpdateMemoryHUDVisibility();
        }

        /// <summary>
        /// 设置内存HUD显示状态
        /// </summary>
        public void SetMemoryHUDVisible(bool visible)
        {
            m_showMemoryHUD = visible;
            UpdateMemoryHUDVisibility();
        }

        /// <summary>
        /// 获取内存使用报告
        /// </summary>
        public MemoryUsageReport GetMemoryUsageReport()
        {
            var categoryAverages = new Dictionary<string, float>();
            foreach (var category in m_categoryHistory)
            {
                categoryAverages[category.Key] = category.Value.Count > 0 ? category.Value.Average() : 0f;
            }

            return new MemoryUsageReport
            {
                CurrentSnapshot = m_currentSnapshot,
                MemoryGrade = GetMemoryUsageGrade(),
                ActiveWarnings = new List<MemoryWarning>(m_activeMemoryWarnings),
                CategoryAverages = categoryAverages,
                GCCallCount = m_gcCallCount,
                MemoryHistory = m_memoryHistory.ToList()
            };
        }

        /// <summary>
        /// 重置内存统计
        /// </summary>
        public void ResetMemoryStatistics()
        {
            m_memoryHistory.Clear();
            foreach (var category in m_categoryHistory.Values)
            {
                category.Clear();
            }
            m_activeMemoryWarnings.Clear();
            m_gcCallCount = 0;
            
            Debug.Log("[MemoryUsageProfiler] 内存统计已重置");
        }

        /// <summary>
        /// 获取分类内存使用趋势
        /// </summary>
        public Dictionary<string, float[]> GetCategoryTrends()
        {
            var trends = new Dictionary<string, float[]>();
            foreach (var category in m_categoryHistory)
            {
                trends[category.Key] = category.Value.ToArray();
            }
            return trends;
        }

        #endregion
    }

    #region Data Structures / 数据结构

    /// <summary>
    /// 内存使用评级枚举
    /// </summary>
    public enum MemoryUsageGrade
    {
        Excellent,  // 优秀 - 内存使用很低
        Good,       // 良好 - 内存使用正常
        Caution,    // 注意 - 内存使用偏高
        Warning,    // 警告 - 内存使用过高
        Critical    // 严重 - 内存使用超标
    }

    /// <summary>
    /// 内存警告类型枚举
    /// </summary>
    public enum MemoryWarningType
    {
        Warning,    // 警告
        Critical,   // 严重
        Leak        // 泄漏
    }

    /// <summary>
    /// 内存快照结构
    /// </summary>
    [System.Serializable]
    public struct MemorySnapshot
    {
        public float Timestamp;
        public float TotalAllocatedMemory;
        public float TotalReservedMemory;
        public float TotalUnusedReservedMemory;
        public float MonoHeapSize;
        public float MonoUsedSize;
        public float TempAllocatorSize;
        public float TextureMemory;
        public float MeshMemory;
        public float AudioMemory;
        public float ScriptMemory;
    }

    /// <summary>
    /// 内存警告结构
    /// </summary>
    [System.Serializable]
    public struct MemoryWarning
    {
        public MemoryWarningType Type;
        public string Message;
        public float CurrentUsage;
        public float Threshold;
    }

    /// <summary>
    /// 内存使用报告结构
    /// </summary>
    [System.Serializable]
    public struct MemoryUsageReport
    {
        public MemorySnapshot CurrentSnapshot;
        public MemoryUsageGrade MemoryGrade;
        public List<MemoryWarning> ActiveWarnings;
        public Dictionary<string, float> CategoryAverages;
        public int GCCallCount;
        public List<MemorySnapshot> MemoryHistory;
    }

    #endregion
}