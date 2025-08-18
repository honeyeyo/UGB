using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 球桌部件管理器
    /// 管理球桌所有TablePart组件，提供统一的碰撞处理接口
    /// </summary>
    public class TablePartManager : MonoBehaviour
    {
        [Header("部件管理")]
        [SerializeField]
        [Tooltip("Table Parts / 球桌部件 - All table parts in this table")]
        private TablePart[] m_tableParts;

        [SerializeField]
        [Tooltip("Auto Find Parts / 自动查找部件 - Automatically find all TablePart components")]
        private bool m_autoFindParts = true;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        // 部件字典，按类型分组
        private Dictionary<TablePartType, List<TablePart>> m_partsByType = new Dictionary<TablePartType, List<TablePart>>();

        // 统计数据
        private Dictionary<TablePartType, int> m_hitCounts = new Dictionary<TablePartType, int>();

        #region Properties
        /// <summary>
        /// 所有球桌部件
        /// </summary>
        public TablePart[] TableParts => m_tableParts;

        /// <summary>
        /// 按类型分组的部件字典
        /// </summary>
        public IReadOnlyDictionary<TablePartType, List<TablePart>> PartsByType => m_partsByType;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeTableParts();
            BuildPartsDictionary();
            InitializeStatistics();
        }

        private void OnValidate()
        {
            if (m_autoFindParts && Application.isPlaying)
            {
                RefreshTableParts();
            }
        }
        #endregion

        #region Initialization
        private void InitializeTableParts()
        {
            if (m_autoFindParts || m_tableParts == null || m_tableParts.Length == 0)
            {
                RefreshTableParts();
            }

            LogDebug($"Initialized {m_tableParts.Length} table parts");
        }

        private void RefreshTableParts()
        {
            // 获取所有子对象中的TablePart组件
            m_tableParts = GetComponentsInChildren<TablePart>();
            
            if (Application.isPlaying)
            {
                BuildPartsDictionary();
            }
        }

        private void BuildPartsDictionary()
        {
            m_partsByType.Clear();

            foreach (var part in m_tableParts)
            {
                if (part == null) continue;

                if (!m_partsByType.ContainsKey(part.PartType))
                {
                    m_partsByType[part.PartType] = new List<TablePart>();
                }

                m_partsByType[part.PartType].Add(part);
            }

            LogDebug($"Built parts dictionary with {m_partsByType.Count} types");
        }

        private void InitializeStatistics()
        {
            m_hitCounts.Clear();
            
            foreach (TablePartType partType in System.Enum.GetValues(typeof(TablePartType)))
            {
                m_hitCounts[partType] = 0;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 获取碰撞体对应的部件类型
        /// </summary>
        /// <param name="hitCollider">被击中的碰撞体</param>
        /// <returns>部件类型</returns>
        public TablePartType GetHitPartType(Collider hitCollider)
        {
            var tablePart = hitCollider.GetComponent<TablePart>();
            return tablePart != null ? tablePart.PartType : TablePartType.Surface;
        }

        /// <summary>
        /// 获取碰撞体对应的TablePart组件
        /// </summary>
        /// <param name="hitCollider">被击中的碰撞体</param>
        /// <returns>TablePart组件</returns>
        public TablePart GetTablePart(Collider hitCollider)
        {
            return hitCollider.GetComponent<TablePart>();
        }

        /// <summary>
        /// 处理球碰撞事件
        /// </summary>
        /// <param name="collision">碰撞信息</param>
        /// <param name="ball">球对象</param>
        /// <returns>是否成功处理碰撞</returns>
        public bool HandleBallCollision(Collision collision, GameObject ball)
        {
            var tablePart = GetTablePart(collision.collider);
            
            if (tablePart == null)
            {
                LogDebug($"No TablePart found on {collision.collider.name}");
                return false;
            }

            // 更新统计
            m_hitCounts[tablePart.PartType]++;

            // 委托给TablePart处理
            tablePart.HandleBallHit(collision, ball);

            LogDebug($"Handled collision with {tablePart.PartType} (Total hits: {m_hitCounts[tablePart.PartType]})");
            
            return true;
        }

        /// <summary>
        /// 获取指定类型的所有部件
        /// </summary>
        /// <param name="partType">部件类型</param>
        /// <returns>部件列表</returns>
        public List<TablePart> GetPartsByType(TablePartType partType)
        {
            return m_partsByType.ContainsKey(partType) ? m_partsByType[partType] : new List<TablePart>();
        }

        /// <summary>
        /// 获取部件统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== Table Part Statistics ===");
            
            foreach (var kvp in m_hitCounts)
            {
                if (kvp.Value > 0)
                {
                    stats.AppendLine($"{kvp.Key}: {kvp.Value} hits");
                }
            }

            return stats.ToString();
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStatistics()
        {
            InitializeStatistics();
            LogDebug("Statistics reset");
        }

        /// <summary>
        /// 设置所有部件的调试模式
        /// </summary>
        /// <param name="debugMode">是否启用调试模式</param>
        public void SetDebugMode(bool debugMode)
        {
            foreach (var part in m_tableParts)
            {
                if (part != null)
                {
                    // 这里需要TablePart暴露SetDebugMode方法
                    // part.SetDebugMode(debugMode);
                }
            }
        }

        /// <summary>
        /// 根据TableData配置所有TablePart组件
        /// </summary>
        /// <param name="tableData">球桌数据</param>
        public void ConfigureFromTableData(TableData tableData)
        {
            if (tableData == null)
            {
                LogDebug("TableData is null, cannot configure parts");
                return;
            }

            LogDebug($"Configuring table parts from TableData: {tableData.name}");

            // 配置桌面部件
            if (m_partsByType.ContainsKey(TablePartType.Surface))
            {
                foreach (var surfacePart in m_partsByType[TablePartType.Surface])
                {
                    if (surfacePart?.Collider is BoxCollider boxCollider)
                    {
                        boxCollider.size = new Vector3(tableData.Width, 0.1f, tableData.Length);
                        boxCollider.center = tableData.GetTableCenter();
                        
                        // 设置物理材质
                        if (boxCollider.material == null)
                        {
                            boxCollider.material = new PhysicMaterial("TableSurface")
                            {
                                bounciness = tableData.Bounce,
                                dynamicFriction = tableData.Friction,
                                staticFriction = tableData.Friction
                            };
                        }
                    }
                }
            }

            // 配置球网部件
            if (m_partsByType.ContainsKey(TablePartType.Net))
            {
                foreach (var netPart in m_partsByType[TablePartType.Net])
                {
                    if (netPart?.Collider is BoxCollider boxCollider)
                    {
                        boxCollider.size = new Vector3(tableData.Width, tableData.NetHeight, 0.1f);
                        boxCollider.center = tableData.GetNetPosition();
                        
                        // 设置物理材质
                        if (boxCollider.material == null)
                        {
                            boxCollider.material = new PhysicMaterial("TableNet")
                            {
                                bounciness = tableData.NetBounce,
                                dynamicFriction = tableData.NetFriction,
                                staticFriction = tableData.NetFriction
                            };
                        }
                    }
                }
            }

            LogDebug("Table parts configuration completed");
        }

        /// <summary>
        /// 验证所有部件配置
        /// </summary>
        /// <returns>验证结果</returns>
        public bool ValidatePartConfiguration()
        {
            bool isValid = true;
            var issues = new List<string>();

            // 检查是否有桌面部件
            if (!m_partsByType.ContainsKey(TablePartType.Surface) || m_partsByType[TablePartType.Surface].Count == 0)
            {
                issues.Add("Missing table surface part");
                isValid = false;
            }

            // 检查是否有球网部件
            if (!m_partsByType.ContainsKey(TablePartType.Net) || m_partsByType[TablePartType.Net].Count == 0)
            {
                issues.Add("Missing table net part");
                isValid = false;
            }

            // 检查重复的碰撞体
            var colliders = new HashSet<Collider>();
            foreach (var part in m_tableParts)
            {
                if (part?.Collider != null)
                {
                    if (!colliders.Add(part.Collider))
                    {
                        issues.Add($"Duplicate collider found on part: {part.name}");
                        isValid = false;
                    }
                }
            }

            if (!isValid)
            {
                Debug.LogWarning($"Table part configuration issues:\n{string.Join("\n", issues)}", this);
            }

            return isValid;
        }
        #endregion

        #region Private Methods
        private void LogDebug(string message)
        {
            if (m_showDebugInfo)
            {
                Debug.Log($"[TablePartManager] {message}", this);
            }
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        [ContextMenu("Refresh Table Parts")]
        private void RefreshTablePartsEditor()
        {
            RefreshTableParts();
            Debug.Log($"Refreshed table parts: Found {m_tableParts.Length} parts");
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfigurationEditor()
        {
            RefreshTableParts();
            BuildPartsDictionary();
            bool isValid = ValidatePartConfiguration();
            
            if (isValid)
            {
                Debug.Log("Table part configuration is valid!", this);
            }
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatisticsEditor()
        {
            Debug.Log(GetStatistics(), this);
        }
#endif
        #endregion
    }
}