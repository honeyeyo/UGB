using UnityEngine;
using Unity.Netcode;

namespace PongHub.Ball
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BallPhysics))]
    [RequireComponent(typeof(BallStateSync))]
    [RequireComponent(typeof(BallSpinVisual))]
    public class PongBall : NetworkBehaviour
    {
        [SerializeField] private BallData m_ballData;

        private Rigidbody m_rigidbody;
        private BallPhysics m_ballPhysics;
        private BallStateSync m_stateSync;
        private BallSpinVisual m_spinVisual;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_ballPhysics = GetComponent<BallPhysics>();
            m_stateSync = GetComponent<BallStateSync>();
            m_spinVisual = GetComponent<BallSpinVisual>();
        }

        // 应用碰撞力
        public void ApplyCollisionForce(Vector3 contactPoint, Vector3 contactNormal, Vector3 impactForce)
        {
            m_ballPhysics.ApplyCollisionForce(contactPoint, contactNormal, impactForce);
        }

        // 属性
        public Vector3 Velocity => m_rigidbody.velocity;
        public Vector3 AngularVelocity => m_rigidbody.angularVelocity;
        public ulong NetworkObjectId => NetworkObjectId;
    }
}
