using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;
using System.Threading.Tasks;
using PongHub.Core.Audio;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 乒乓球桌 - 作为本地VR空间锚点
    /// 提供空间参考和碰撞检测，不进行网络同步
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Table : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private BoxCollider m_tableCollider;
        [SerializeField] private BoxCollider m_netCollider;
        [SerializeField] private BoxCollider m_edgeCollider;
        [SerializeField] private MeshRenderer m_tableRenderer;
        [SerializeField] private MeshRenderer m_netRenderer;
        [SerializeField] private MeshRenderer m_lineRenderer;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Collider m_collider;
        [SerializeField] private Transform m_netTransform;

        [Header("本地锚点设置")]
        [SerializeField] private bool m_isLocalAnchor = true; // 始终为true
        [SerializeField] private Transform m_tableCenter; // 球桌中心点
        [SerializeField] private Transform m_leftServiceArea; // 左发球区
        [SerializeField] private Transform m_rightServiceArea; // 右发球区

        [Header("配置")]
        [SerializeField] private TableData m_tableData;

        // 碰撞检测区域
        private Bounds m_tableBounds;
        private Bounds m_netBounds;
        private Bounds m_edgeBounds;

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
            // 自动获取组件引用
            if (m_tableCollider == null)
                m_tableCollider = GetComponent<BoxCollider>();
            if (m_netCollider == null)
                m_netCollider = transform.Find("Net")?.GetComponent<BoxCollider>();
            if (m_edgeCollider == null)
                m_edgeCollider = transform.Find("Edge")?.GetComponent<BoxCollider>();
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

            // 设置球桌碰撞体
            m_tableCollider.size = new Vector3(m_tableData.Width, 0.1f, m_tableData.Length);
            m_tableCollider.center = m_tableData.GetTableCenter();
            m_tableCollider.material = new PhysicMaterial("Table")
            {
                bounciness = m_tableData.Bounce,
                dynamicFriction = m_tableData.Friction,
                staticFriction = m_tableData.Friction
            };

            // 设置球网碰撞体
            if (m_netCollider != null)
            {
                m_netCollider.size = new Vector3(m_tableData.Width, m_tableData.NetHeight, 0.1f);
                m_netCollider.center = m_tableData.GetNetPosition();
                m_netCollider.material = new PhysicMaterial("Net")
                {
                    bounciness = m_tableData.NetBounce,
                    dynamicFriction = m_tableData.NetFriction,
                    staticFriction = m_tableData.NetFriction
                };
            }

            // 设置边缘碰撞体
            if (m_edgeCollider != null)
            {
                m_edgeCollider.size = new Vector3(
                    m_tableData.Width + m_tableData.EdgeWidth * 2,
                    m_tableData.NetHeight,
                    m_tableData.Length + m_tableData.EdgeWidth * 2
                );
                m_edgeCollider.center = m_tableData.GetTableCenter();
                m_edgeCollider.material = new PhysicMaterial("Edge")
                {
                    bounciness = m_tableData.Bounce * 1.2f,
                    dynamicFriction = m_tableData.Friction * 1.5f,
                    staticFriction = m_tableData.Friction * 1.5f
                };
            }

            // 更新碰撞区域
            m_tableBounds = m_tableCollider.bounds;
            if (m_netCollider != null) m_netBounds = m_netCollider.bounds;
            if (m_edgeCollider != null) m_edgeBounds = m_edgeCollider.bounds;
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

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent<BallPhysics>(out var ball))
            {
                var contact = collision.GetContact(0);
                var contactPoint = contact.point;
                var contactNormal = contact.normal;

                // 计算击球力度
                float hitForce = CalculateHitForce(ball.Velocity);

                // 应用碰撞力
                ball.ApplyCollisionForce(contactPoint, contactNormal, hitForce, HitType.Table);

                // 播放击球音效 - 由Ball统一处理，这里可以移除
                // if (AudioManager.Instance != null)
                // {
                //     AudioManager.Instance.PlayTableHit(contactPoint, m_tableData.HitVolume);
                // }
            }
        }

        private float CalculateHitForce(Vector3 ballVelocity)
        {
            if (m_tableData == null) return 0f;

            // 计算击球力度
            float hitForce = ballVelocity.magnitude * m_tableData.HitMultiplier;
            return hitForce;
        }

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