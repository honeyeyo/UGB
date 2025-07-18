using UnityEngine;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    public class BallParticles : MonoBehaviour
    {
        [Header("粒子系统引用")]
        [SerializeField]
        [Tooltip("Hit Particles / 碰撞粒子 - Particle system for ball hit effects")]
        private ParticleSystem m_hitParticles;

        [SerializeField]
        [Tooltip("Spin Particles / 旋转粒子 - Particle system for ball spin effects")]
        private ParticleSystem m_spinParticles;

        [SerializeField]
        [Tooltip("Trail Particles / 拖尾粒子 - Particle system for ball trail effects")]
        private ParticleSystem m_trailParticles;

        [SerializeField]
        [Tooltip("Score Particles / 得分粒子 - Particle system for scoring effects")]
        private ParticleSystem m_scoreParticles;

        [Header("粒子配置")]
        [SerializeField]
        [Tooltip("Hit Color / 碰撞颜色 - Color for hit particle effects")]
        private Color m_hitColor = Color.white;

        [SerializeField]
        [Tooltip("Spin Color / 旋转颜色 - Color for spin particle effects")]
        private Color m_spinColor = Color.cyan;

        [SerializeField]
        [Tooltip("Trail Color / 拖尾颜色 - Color for trail particle effects")]
        private Color m_trailColor = Color.yellow;

        [SerializeField]
        [Tooltip("Score Color / 得分颜色 - Color for score particle effects")]
        private Color m_scoreColor = Color.green;

        [SerializeField]
        [Tooltip("Hit Size / 碰撞大小 - Size for hit particles")]
        private float m_hitSize = 0.1f;

        [SerializeField]
        [Tooltip("Spin Size / 旋转大小 - Size for spin particles")]
        private float m_spinSize = 0.05f;

        [SerializeField]
        [Tooltip("Trail Size / 拖尾大小 - Size for trail particles")]
        private float m_trailSize = 0.02f;

        [SerializeField]
        [Tooltip("Score Size / 得分大小 - Size for score particles")]
        private float m_scoreSize = 0.2f;

        private void Awake()
        {
            SetupParticleSystems();
        }

        private void SetupParticleSystems()
        {
            // 设置碰撞粒子
            if (m_hitParticles != null)
            {
                var main = m_hitParticles.main;
                main.startColor = m_hitColor;
                main.startSize = m_hitSize;
                main.startLifetime = 0.5f;
                main.maxParticles = 20;
            }

            // 设置旋转粒子
            if (m_spinParticles != null)
            {
                var main = m_spinParticles.main;
                main.startColor = m_spinColor;
                main.startSize = m_spinSize;
                main.startLifetime = 1f;
                main.maxParticles = 50;
            }

            // 设置拖尾粒子
            if (m_trailParticles != null)
            {
                var main = m_trailParticles.main;
                main.startColor = m_trailColor;
                main.startSize = m_trailSize;
                main.startLifetime = 0.3f;
                main.maxParticles = 100;
            }

            // 设置得分粒子
            if (m_scoreParticles != null)
            {
                var main = m_scoreParticles.main;
                main.startColor = m_scoreColor;
                main.startSize = m_scoreSize;
                main.startLifetime = 1f;
                main.maxParticles = 30;
            }
        }

        // 播放碰撞粒子
        public void PlayHitParticles(Vector3 position, Vector3 normal)
        {
            if (m_hitParticles != null)
            {
                m_hitParticles.transform.position = position;
                m_hitParticles.transform.forward = normal;
                m_hitParticles.Play();
            }
        }

        // 播放旋转粒子
        public void PlaySpinParticles(Vector3 position, Vector3 axis)
        {
            if (m_spinParticles != null)
            {
                m_spinParticles.transform.position = position;
                m_spinParticles.transform.forward = axis;
                m_spinParticles.Play();
            }
        }

        // 播放拖尾粒子
        public void PlayTrailParticles(bool play)
        {
            if (m_trailParticles != null)
            {
                if (play)
                    m_trailParticles.Play();
                else
                    m_trailParticles.Stop();
            }
        }

        // 播放得分粒子
        public void PlayScoreParticles(Vector3 position)
        {
            if (m_scoreParticles != null)
            {
                m_scoreParticles.transform.position = position;
                m_scoreParticles.Play();
            }
        }

        // 设置粒子颜色
        public void SetHitColor(Color color)
        {
            m_hitColor = color;
            if (m_hitParticles != null)
            {
                var main = m_hitParticles.main;
                main.startColor = color;
            }
        }

        public void SetSpinColor(Color color)
        {
            m_spinColor = color;
            if (m_spinParticles != null)
            {
                var main = m_spinParticles.main;
                main.startColor = color;
            }
        }

        public void SetTrailColor(Color color)
        {
            m_trailColor = color;
            if (m_trailParticles != null)
            {
                var main = m_trailParticles.main;
                main.startColor = color;
            }
        }

        public void SetScoreColor(Color color)
        {
            m_scoreColor = color;
            if (m_scoreParticles != null)
            {
                var main = m_scoreParticles.main;
                main.startColor = color;
            }
        }
    }
}