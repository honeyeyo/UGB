using UnityEngine;

namespace PongHub.Ball
{
    public class BallPhysics : MonoBehaviour
    {
        [SerializeField] private BallData m_ballData;
        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            SetupRigidbody();
        }

        private void SetupRigidbody()
        {
            m_rigidbody.mass = m_ballData.Mass;
            m_rigidbody.drag = m_ballData.AirResistance;
            m_rigidbody.angularDrag = m_ballData.AirResistance * 2f;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            // 限制最大速度
            if (m_rigidbody.velocity.magnitude > m_ballData.MaxSpeed)
            {
                m_rigidbody.velocity = m_rigidbody.velocity.normalized * m_ballData.MaxSpeed;
            }

            // 限制最小速度
            if (m_rigidbody.velocity.magnitude < m_ballData.MinSpeed && m_rigidbody.velocity.magnitude > 0)
            {
                m_rigidbody.velocity = Vector3.zero;
            }

            // 限制最大旋转
            if (m_rigidbody.angularVelocity.magnitude > m_ballData.MaxSpin)
            {
                m_rigidbody.angularVelocity = m_rigidbody.angularVelocity.normalized * m_ballData.MaxSpin;
            }

            // 应用旋转衰减
            m_rigidbody.angularVelocity = Vector3.Scale(m_rigidbody.angularVelocity, m_ballData.SpinDecay);
        }

        // 应用碰撞力
        public void ApplyCollisionForce(Vector3 contactPoint, Vector3 contactNormal, Vector3 impactForce)
        {
            // 计算碰撞后的速度
            var newVelocity = impactForce / m_ballData.Mass;

            // 计算旋转
            var relativePoint = contactPoint - transform.position;
            var angularImpulse = Vector3.Cross(relativePoint, impactForce);
            var newAngularVelocity = angularImpulse / (m_rigidbody.mass * m_rigidbody.mass);

            // 应用力和旋转
            m_rigidbody.velocity = newVelocity;
            m_rigidbody.angularVelocity = newAngularVelocity;
        }
    }
}
