using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;
using System.Threading.Tasks;
using PongHub.Core.Audio;

namespace PongHub.Gameplay.Table
{
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

        private void Awake()
        {
            if (m_tableCollider == null)
                m_tableCollider = GetComponent<BoxCollider>();
            if (m_netCollider == null)
                m_netCollider = transform.Find("Net").GetComponent<BoxCollider>();
            if (m_edgeCollider == null)
                m_edgeCollider = transform.Find("Edge").GetComponent<BoxCollider>();
            if (m_tableRenderer == null)
                m_tableRenderer = GetComponent<MeshRenderer>();
            if (m_netRenderer == null)
                m_netRenderer = transform.Find("Net").GetComponent<MeshRenderer>();
            if (m_lineRenderer == null)
                m_lineRenderer = transform.Find("Lines").GetComponent<MeshRenderer>();
            if (m_renderer == null)
                m_renderer = GetComponent<MeshRenderer>();
            if (m_collider == null)
                m_collider = GetComponent<Collider>();

            SetupColliders();
            SetupVisuals();
        }

        private void SetupColliders()
        {
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
            m_netCollider.size = new Vector3(m_tableData.Width, m_tableData.NetHeight, 0.1f);
            m_netCollider.center = m_tableData.GetNetPosition();
            m_netCollider.material = new PhysicMaterial("Net")
            {
                bounciness = m_tableData.NetBounce,
                dynamicFriction = m_tableData.NetFriction,
                staticFriction = m_tableData.NetFriction
            };

            // 设置边缘碰撞体
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

            // 更新碰撞区域
            m_tableBounds = m_tableCollider.bounds;
            m_netBounds = m_netCollider.bounds;
            m_edgeBounds = m_edgeCollider.bounds;
        }

        private void SetupVisuals()
        {
            // 设置球桌颜色
            m_tableColor = m_tableData.TableColor;
            m_netColor = m_tableData.NetColor;
            m_lineColor = m_tableData.LineColor;

            // 应用颜色
            m_tableRenderer.material.color = m_tableColor;
            m_netRenderer.material.color = m_netColor;
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

                // 播放击球音效
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayTableHit(contactPoint, m_tableData.HitVolume);
                }
            }
        }

        private float CalculateHitForce(Vector3 ballVelocity)
        {
            // 计算击球力度
            float hitForce = ballVelocity.magnitude * m_tableData.HitMultiplier;
            return hitForce;
        }

        // 检查球是否在有效区域内
        public bool IsBallInValidArea(Vector3 ballPosition)
        {
            return m_tableData.IsPointInTable(ballPosition);
        }

        // 检查球是否在发球区内
        public bool IsBallInServiceArea(Vector3 ballPosition, bool isRightSide)
        {
            return m_tableData.IsPointInServiceArea(ballPosition, isRightSide);
        }

        public void SetTableData(TableData data)
        {
            m_tableData = data;
            SetupColliders();
            SetupVisuals();
        }

        // 属性
        public Color TableColor => m_tableColor;
        public Color NetColor => m_netColor;
        public Color LineColor => m_lineColor;
        public TableData TableData => m_tableData;
        public Transform NetTransform => m_netTransform;

        public void ResetTable()
        {
            // 重置球桌状态
            SetupVisuals();
        }

        public void SetTableColor(Color color)
        {
            m_tableColor = color;
            m_tableRenderer.material.color = m_tableColor;
        }

        public void SetNetColor(Color color)
        {
            m_netColor = color;
            m_netRenderer.material.color = m_netColor;
        }

        public void SetLineColor(Color color)
        {
            m_lineColor = color;
            m_lineRenderer.material.color = m_lineColor;
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            // 初始化桌子
        }
    }
}