using UnityEngine;
using PongHub.Ball;

namespace PongHub.Table
{
    // 球台类
    [RequireComponent(typeof(BoxCollider))]
    public class Table : MonoBehaviour
    {
        [SerializeField] private TableData m_tableData;
        [SerializeField] private BoxCollider m_tableCollider;
        [SerializeField] private BoxCollider m_netCollider;

        private void Awake()
        {
            SetupColliders();
        }

        private void SetupColliders()
        {
            // 设置台面碰撞体
            m_tableCollider.size = new Vector3(m_tableData.Width, 0.1f, m_tableData.Length);
            m_tableCollider.center = new Vector3(0, 0, 0);

            // 设置球网碰撞体
            m_netCollider.size = new Vector3(0.1f, m_tableData.NetHeight, m_tableData.Width);
            m_netCollider.center = new Vector3(0, m_tableData.NetHeight / 2, 0);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent<PongBall>(out var ball))
            {
                // 获取碰撞信息
                var contact = collision.GetContact(0);
                var contactPoint = contact.point;
                var contactNormal = contact.normal;
                var relativeVelocity = ball.Velocity;

                // 判断是台面还是球网碰撞
                var isNetCollision = contact.thisCollider == m_netCollider;
                var bounce = isNetCollision ? m_tableData.NetBounce : m_tableData.Bounce;
                var friction = isNetCollision ? m_tableData.NetFriction : m_tableData.Friction;

                // 计算反弹力
                var normalForce = Vector3.Dot(relativeVelocity, contactNormal) * contactNormal;
                var tangentialForce = relativeVelocity - normalForce;

                // 应用反弹和摩擦
                var bounceForce = -normalForce * bounce;
                var frictionForce = -tangentialForce * friction;

                // 应用最终力
                var impactForce = bounceForce + frictionForce;
                ball.ApplyCollisionForce(contactPoint, contactNormal, impactForce);
            }
        }

        // 检查球是否在有效区域内
        public bool IsBallInValidArea(Vector3 ballPosition)
        {
            var localPos = transform.InverseTransformPoint(ballPosition);
            return Mathf.Abs(localPos.x) <= m_tableData.Width / 2 &&
                   Mathf.Abs(localPos.z) <= m_tableData.Length / 2 &&
                   localPos.y >= 0 && localPos.y <= m_tableData.Height;
        }

        // 检查球是否过网
        public bool IsBallOverNet(Vector3 ballPosition)
        {
            var localPos = transform.InverseTransformPoint(ballPosition);
            return localPos.z > 0; // 假设z轴正方向为过网方向
        }
    }
}
}