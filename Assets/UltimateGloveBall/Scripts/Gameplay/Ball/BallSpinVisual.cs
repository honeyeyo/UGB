using UnityEngine;

namespace PongHub.Gameplay.Ball
{
    [RequireComponent(typeof(BallPhysics))]
    public class BallSpinVisual : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private BallPhysics m_ballPhysics;
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private ParticleSystem m_spinParticles;

        [Header("视觉效果参数")]
        [SerializeField] private float m_trailUpdateInterval = 0.02f;
        [SerializeField] private float m_particleEmissionRate = 10f;
        [SerializeField] private float m_minSpinThreshold = 10f;

        private float m_lastTrailUpdateTime;
        private Vector3 m_lastTrailPosition;
        private Quaternion m_lastTrailRotation;

        private void Awake()
        {
            if (m_ballPhysics == null)
                m_ballPhysics = GetComponent<BallPhysics>();
            if (m_trailRenderer == null)
                m_trailRenderer = GetComponent<TrailRenderer>();
            if (m_spinParticles == null)
                m_spinParticles = GetComponentInChildren<ParticleSystem>();

            SetupTrailRenderer();
            SetupParticleSystem();
        }

        private void SetupTrailRenderer()
        {
            var ballData = m_ballPhysics.BallData;
            m_trailRenderer.startWidth = ballData.TrailWidth;
            m_trailRenderer.endWidth = ballData.TrailWidth * 0.5f;
            m_trailRenderer.time = ballData.TrailTime;
            m_trailRenderer.startColor = ballData.TrailColor;
            m_trailRenderer.endColor = new Color(
                ballData.TrailColor.r,
                ballData.TrailColor.g,
                ballData.TrailColor.b,
                0f
            );
            m_trailRenderer.emitting = false;
        }

        private void SetupParticleSystem()
        {
            if (m_spinParticles != null)
            {
                var emission = m_spinParticles.emission;
                emission.rateOverTime = m_particleEmissionRate;
                m_spinParticles.Stop();
            }
        }

        private void Update()
        {
            UpdateTrailRenderer();
            UpdateParticleSystem();
        }

        private void UpdateTrailRenderer()
        {
            var ballData = m_ballPhysics.BallData;
            var velocity = m_ballPhysics.Velocity;
            var angularVelocity = m_ballPhysics.AngularVelocity;

            // 根据速度和旋转更新拖尾
            if (velocity.magnitude > ballData.MinSpeed)
            {
                // 计算拖尾宽度
                float spinMagnitude = angularVelocity.magnitude * ballData.SpinVisualMultiplier;
                float trailWidth = ballData.TrailWidth * (1f + spinMagnitude / ballData.MaxSpin);

                // 更新拖尾参数
                m_trailRenderer.startWidth = trailWidth;
                m_trailRenderer.endWidth = trailWidth * 0.5f;

                // 更新拖尾颜色
                Color spinColor = GetSpinColor(angularVelocity);
                m_trailRenderer.startColor = spinColor;
                m_trailRenderer.endColor = new Color(spinColor.r, spinColor.g, spinColor.b, 0f);

                // 启用拖尾
                m_trailRenderer.emitting = true;
            }
            else
            {
                // 禁用拖尾
                m_trailRenderer.emitting = false;
            }
        }

        private void UpdateParticleSystem()
        {
            if (m_spinParticles != null)
            {
                var angularVelocity = m_ballPhysics.AngularVelocity;
                float spinMagnitude = angularVelocity.magnitude;

                if (spinMagnitude > m_minSpinThreshold)
                {
                    // 根据旋转方向设置粒子系统旋转
                    var rotation = Quaternion.LookRotation(angularVelocity.normalized);
                    m_spinParticles.transform.rotation = rotation;

                    // 根据旋转速度调整粒子发射率
                    var emission = m_spinParticles.emission;
                    emission.rateOverTime = m_particleEmissionRate * (spinMagnitude / m_minSpinThreshold);

                    // 播放粒子效果
                    if (!m_spinParticles.isPlaying)
                    {
                        m_spinParticles.Play();
                    }
                }
                else
                {
                    // 停止粒子效果
                    if (m_spinParticles.isPlaying)
                    {
                        m_spinParticles.Stop();
                    }
                }
            }
        }

        private Color GetSpinColor(Vector3 angularVelocity)
        {
            var ballData = m_ballPhysics.BallData;
            float spinMagnitude = angularVelocity.magnitude;
            float normalizedSpin = Mathf.Clamp01(spinMagnitude / ballData.MaxSpin);

            // 根据旋转方向计算颜色
            Color spinColor = ballData.TrailColor;
            if (Mathf.Abs(angularVelocity.x) > 0.1f)
            {
                // 侧旋 - 偏红色
                spinColor = Color.Lerp(spinColor, Color.red, normalizedSpin);
            }
            else if (angularVelocity.y > 0.1f)
            {
                // 上旋 - 偏绿色
                spinColor = Color.Lerp(spinColor, Color.green, normalizedSpin);
            }
            else if (angularVelocity.y < -0.1f)
            {
                // 下旋 - 偏蓝色
                spinColor = Color.Lerp(spinColor, Color.blue, normalizedSpin);
            }

            return spinColor;
        }

        // 重置视觉效果
        public void ResetVisuals()
        {
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
                m_trailRenderer.emitting = false;
            }

            if (m_spinParticles != null)
            {
                m_spinParticles.Stop();
                m_spinParticles.Clear();
            }
        }
    }
}