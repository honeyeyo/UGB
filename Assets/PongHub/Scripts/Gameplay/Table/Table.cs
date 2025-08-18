using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;
using System.Threading.Tasks;
using PongHub.Core.Audio;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 乒乓球桌 - 作为本地VR空间锚点
    /// 提供空间参考，碰撞检测由TablePart系统管理，不进行网络同步
    /// </summary>
    public class Table : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Table Renderer / 球桌渲染器 - Mesh renderer for table surface")]
        private MeshRenderer m_tableRenderer;
        [SerializeField]
        [Tooltip("Net Renderer / 网渲染器 - Mesh renderer for net")]
        private MeshRenderer m_netRenderer;
        [SerializeField]
        [Tooltip("Line Renderer / 线渲染器 - Mesh renderer for table lines")]
        private MeshRenderer m_lineRenderer;
        [SerializeField]
        [Tooltip("Renderer / 渲染器 - Main mesh renderer component")]
        private MeshRenderer m_renderer;
        [SerializeField]
        [Tooltip("Collider / 碰撞体 - Main collider component")]
        private Collider m_collider;
        [SerializeField]
        [Tooltip("Net Transform / 网变换 - Transform of the net")]
        private Transform m_netTransform;

        [Header("本地锚点设置")]
        [SerializeField]
        [Tooltip("Is Local Anchor / 是否本地锚点 - Whether this table is a local anchor")]
        private bool m_isLocalAnchor = true; // 始终为true
        [SerializeField]
        [Tooltip("Table Center / 球桌中心 - Transform for table center point")]
        private Transform m_tableCenter; // 球桌中心点
        [SerializeField]
        [Tooltip("Left Service Area / 左发球区 - Transform for left service area")]
        private Transform m_leftServiceArea; // 左发球区
        [SerializeField]
        [Tooltip("Right Service Area / 右发球区 - Transform for right service area")]
        private Transform m_rightServiceArea; // 右发球区

        [Header("配置")]
        [SerializeField]
        [Tooltip("Table Data / 球桌数据 - Table configuration data")]
        private TableData m_tableData;

        // TablePartManager引用 - 管理碰撞检测
        private TablePartManager m_tablePartManager;

        // 颜色属性
        private Color m_tableColor;
        private Color m_netColor;
        private Color m_lineColor;

        // 本地锚点状态
        private Vector3 m_originalPosition;
        private Quaternion m_originalRotation;
        private Vector3 m_originalScale;

        private void Awake()
        {
            InitializeComponents();
            SetupColliders();
            SetupVisuals();
            SaveOriginalTransform();
        }

        private void InitializeComponents()
        {
            // 获取TablePartManager组件
            m_tablePartManager = GetComponent<TablePartManager>();
            
            // 自动获取渲染器组件引用
            if (m_tableRenderer == null)
                m_tableRenderer = GetComponent<MeshRenderer>();
            if (m_netRenderer == null)
                m_netRenderer = transform.Find("Net")?.GetComponent<MeshRenderer>();
            if (m_lineRenderer == null)
                m_lineRenderer = transform.Find("Lines")?.GetComponent<MeshRenderer>();
            if (m_renderer == null)
                m_renderer = GetComponent<MeshRenderer>();
            if (m_collider == null)
                m_collider = GetComponent<Collider>();

            // 设置关键变换点
            if (m_tableCenter == null)
                m_tableCenter = transform;
            if (m_netTransform == null)
                m_netTransform = transform.Find("Net");
        }

        private void SaveOriginalTransform()
        {
            // 保存原始变换状态，用于本地重置
            m_originalPosition = transform.position;
            m_originalRotation = transform.rotation;
            m_originalScale = transform.localScale;
        }

        private void SetupColliders()
        {
            if (m_tableData == null) return;

            // 如果有TablePartManager，让它负责设置碰撞体
            if (m_tablePartManager != null)
            {
                // TablePartManager会根据TableData配置各个TablePart组件
                m_tablePartManager.ConfigureFromTableData(m_tableData);
            }
            else
            {
                Debug.LogWarning("TablePartManager not found. Colliders will not be configured automatically.");
            }
        }

        private void SetupVisuals()
        {
            if (m_tableData == null) return;

            // 设置球桌颜色
            m_tableColor = m_tableData.TableColor;
            m_netColor = m_tableData.NetColor;
            m_lineColor = m_tableData.LineColor;

            // 应用颜色
            if (m_tableRenderer != null)
                m_tableRenderer.material.color = m_tableColor;
            if (m_netRenderer != null)
                m_netRenderer.material.color = m_netColor;
            if (m_lineRenderer != null)
                m_lineRenderer.material.color = m_lineColor;
        }

        // 碰撞检测现在由TablePart系统管理
        // OnCollisionEnter方法已移除，碰撞处理由各个TablePart组件处理

        #region 本地锚点功能
        /// <summary>
        /// 获取球桌中心位置
        /// </summary>
        public Vector3 GetCenterPosition()
        {
            return m_tableCenter != null ? m_tableCenter.position : transform.position;
        }

        /// <summary>
        /// 获取球网位置
        /// </summary>
        public Vector3 GetNetPosition()
        {
            return m_netTransform != null ? m_netTransform.position : transform.position;
        }

        /// <summary>
        /// 获取左发球区位置
        /// </summary>
        public Vector3 GetLeftServiceAreaPosition()
        {
            return m_leftServiceArea != null ? m_leftServiceArea.position : transform.position;
        }

        /// <summary>
        /// 获取右发球区位置
        /// </summary>
        public Vector3 GetRightServiceAreaPosition()
        {
            return m_rightServiceArea != null ? m_rightServiceArea.position : transform.position;
        }

        /// <summary>
        /// 将世界坐标转换为相对于Table的本地坐标
        /// 用于网络同步计算
        /// </summary>
        public Vector3 WorldToTableSpace(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        /// <summary>
        /// 将Table本地坐标转换为世界坐标
        /// </summary>
        public Vector3 TableToWorldSpace(Vector3 tablePosition)
        {
            return transform.TransformPoint(tablePosition);
        }

        /// <summary>
        /// 获取世界空间到Table本地空间的变换矩阵
        /// </summary>
        public Matrix4x4 GetWorldToTableMatrix()
        {
            return transform.worldToLocalMatrix;
        }

        /// <summary>
        /// 获取Table本地空间到世界空间的变换矩阵
        /// </summary>
        public Matrix4x4 GetTableToWorldMatrix()
        {
            return transform.localToWorldMatrix;
        }

        /// <summary>
        /// 本地重置Table（不影响其他玩家）
        /// </summary>
        public void ResetLocalTable()
        {
            // 重置到原始位置
            transform.position = m_originalPosition;
            transform.rotation = m_originalRotation;
            transform.localScale = m_originalScale;

            // 重置视觉效果
            SetupVisuals();

            Debug.Log("Table本地重置完成");
        }

        /// <summary>
        /// 设置Table作为本地锚点位置
        /// </summary>
        public void SetLocalAnchorPosition(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;

            // 更新原始状态
            m_originalPosition = position;
            m_originalRotation = rotation;

            Debug.Log($"设置Table本地锚点: 位置={position}, 旋转={rotation.eulerAngles}");
        }
        #endregion

        #region 游戏逻辑功能
        // 检查球是否在有效区域内
        public bool IsBallInValidArea(Vector3 ballPosition)
        {
            return m_tableData != null && m_tableData.IsPointInTable(ballPosition);
        }

        // 检查球是否在发球区内
        public bool IsBallInServiceArea(Vector3 ballPosition, bool isRightSide)
        {
            return m_tableData != null && m_tableData.IsPointInServiceArea(ballPosition, isRightSide);
        }

        public void SetTableData(TableData data)
        {
            m_tableData = data;
            SetupColliders();
            SetupVisuals();
        }
        #endregion

        #region 属性
        public Color TableColor => m_tableColor;
        public Color NetColor => m_netColor;
        public Color LineColor => m_lineColor;
        public TableData TableData => m_tableData;
        public Transform NetTransform => m_netTransform;
        public Transform TableCenter => m_tableCenter;
        public bool IsLocalAnchor => m_isLocalAnchor;

        // 锚点状态
        public Vector3 OriginalPosition => m_originalPosition;
        public Quaternion OriginalRotation => m_originalRotation;
        public Vector3 OriginalScale => m_originalScale;
        #endregion

        #region 颜色设置（本地功能）
        public void SetTableColor(Color color)
        {
            m_tableColor = color;
            if (m_tableRenderer != null)
                m_tableRenderer.material.color = m_tableColor;
        }

        public void SetNetColor(Color color)
        {
            m_netColor = color;
            if (m_netRenderer != null)
                m_netRenderer.material.color = m_netColor;
        }

        public void SetLineColor(Color color)
        {
            m_lineColor = color;
            if (m_lineRenderer != null)
                m_lineRenderer.material.color = m_lineColor;
        }
        #endregion

        public void ResetTable()
        {
            // 重置球桌状态
            SetupVisuals();
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            // 初始化桌子
        }
    }
}