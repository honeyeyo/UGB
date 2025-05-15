using Unity.Netcode;
using UnityEngine;

namespace PongHub.Ball
{
    [RequireComponent(typeof(PongBall))]
    public class BallStateSync : NetworkBehaviour
    {
        private NetworkVariable<Vector3> m_position = new();
        private NetworkVariable<Vector3> m_velocity = new();
        private NetworkVariable<Vector3> m_angularVelocity = new();

        private PongBall m_ball;
        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_ball = GetComponent<PongBall>();
            m_rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                // 服务器更新网络变量
                m_position.Value = transform.position;
                m_velocity.Value = m_rigidbody.velocity;
                m_angularVelocity.Value = m_rigidbody.angularVelocity;
            }
            else
            {
                // 客户端同步状态
                transform.position = m_position.Value;
                m_rigidbody.velocity = m_velocity.Value;
                m_rigidbody.angularVelocity = m_angularVelocity.Value;
            }
        }
    }
}
