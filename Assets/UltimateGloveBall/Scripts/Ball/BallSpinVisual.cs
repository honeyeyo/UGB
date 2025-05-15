using UnityEngine;

namespace PongHub.Ball
{
    [RequireComponent(typeof(PongBall))]
    public class BallSpinVisual : MonoBehaviour
    {
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private BallData m_ballData;

        private PongBall m_ball;
        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_ball = GetComponent<PongBall>();
            m_rigidbody = GetComponent<Rigidbody>();
            SetupTrailRenderer();
        }

        private void SetupTrailRenderer()
        {
            m_trailRenderer.startWidth = m_ballData.TrailWidth;
            m_trailRenderer.endWidth = m_ballData.TrailWidth;
            m_trailRenderer.time = m_ballData.TrailTime;
            m_trailRenderer.startColor = m_ballData.TrailColor;
            m_trailRenderer.endColor = m_ballData.TrailColor;
        }

        private void Update()
        {
            // 根据球的速度和旋转更新视觉效果
            var speed = m_rigidbody.velocity.magnitude;
            var spin = m_rigidbody.angularVelocity.magnitude;

            // 速度越大,拖尾越长
            m_trailRenderer.time = m_ballData.TrailTime * (speed / m_ballData.MaxSpeed);

            // 旋转越大,拖尾越宽
            m_trailRenderer.startWidth = m_ballData.TrailWidth * (1 + spin / m_ballData.MaxSpin);
            m_trailRenderer.endWidth = m_ballData.TrailWidth;
        }
    }
}
