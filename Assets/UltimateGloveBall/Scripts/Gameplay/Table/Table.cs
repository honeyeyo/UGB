using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;

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
                HandleBallCollision(collision, ball);
            }
        }

        private void HandleBallCollision(Collision collision, BallPhysics ball)
        {
            var contact = collision.GetContact(0);
            var contactPoint = contact.point;
            var contactNormal = contact.normal;

            // 确定碰撞类型
            if (m_netBounds.Contains(contactPoint))
            {
                // 球网碰撞
                PlayHitSound(m_tableData.NetHitVolume);
                ball.ApplyCollisionForce(contactPoint, contactNormal, m_tableData.NetBounce);
            }
            else if (m_edgeBounds.Contains(contactPoint) && !m_tableBounds.Contains(contactPoint))
            {
                // 边缘碰撞
                PlayHitSound(m_tableData.EdgeHitVolume);
                ball.ApplyCollisionForce(contactPoint, contactNormal, m_tableData.Bounce * 1.2f);
            }
            else
            {
                // 球桌面碰撞
                PlayHitSound(m_tableData.TableHitVolume);
                ball.ApplyCollisionForce(contactPoint, contactNormal, m_tableData.Bounce);
            }
        }

        private void PlayHitSound(float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTableHit(volume);
            }
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

        // 获取球桌数据
        public TableData GetTableData()
        {
            return m_tableData;
        }

        // 设置颜色
        public void SetColors(Color tableColor, Color netColor, Color lineColor)
        {
            m_tableColor = tableColor;
            m_netColor = netColor;
            m_lineColor = lineColor;

            m_tableRenderer.material.color = m_tableColor;
            m_netRenderer.material.color = m_netColor;
            m_lineRenderer.material.color = m_lineColor;
        }

        // 属性
        public Color TableColor => m_tableColor;
        public Color NetColor => m_netColor;
        public Color LineColor => m_lineColor;
        public TableData TableData => m_tableData;
    }
}