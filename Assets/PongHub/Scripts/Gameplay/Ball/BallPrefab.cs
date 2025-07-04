using UnityEngine;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    [RequireComponent(typeof(BallPhysics))]
    [RequireComponent(typeof(BallSpinVisual))]
    [RequireComponent(typeof(BallNetworking))]
    public class BallPrefab : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Ball Physics / 球物理 - Ball physics component for movement and collision")]
        private BallPhysics m_ballPhysics;
        [SerializeField]
        [Tooltip("Ball Visual / 球视觉效果 - Ball visual effects component")]
        private BallSpinVisual m_ballVisual;
        [SerializeField]
        [Tooltip("Ball Networking / 球网络 - Ball networking component for multiplayer")]
        private BallNetworking m_ballNetworking;
        [SerializeField]
        [Tooltip("Trail Renderer / 轨迹渲染器 - Trail renderer for ball movement trail")]
        private TrailRenderer m_trailRenderer;
        [SerializeField]
        [Tooltip("Hit Particles / 击球粒子 - Particle system for hit effects")]
        private ParticleSystem m_hitParticles;
        [SerializeField]
        [Tooltip("Spin Particles / 旋转粒子 - Particle system for spin effects")]
        private ParticleSystem m_spinParticles;
        [SerializeField]
        [Tooltip("Audio Source / 音频源 - Audio source for ball sounds")]
        private AudioSource m_audioSource;

        [Header("配置")]
        [SerializeField]
        [Tooltip("Ball Data / 球数据 - Ball configuration data")]
        private BallData m_ballData;

        private void Awake()
        {
            // 获取组件引用
            if (m_ballPhysics == null)
                m_ballPhysics = GetComponent<BallPhysics>();
            if (m_ballVisual == null)
                m_ballVisual = GetComponent<BallSpinVisual>();
            if (m_ballNetworking == null)
                m_ballNetworking = GetComponent<BallNetworking>();
            if (m_trailRenderer == null)
                m_trailRenderer = GetComponentInChildren<TrailRenderer>();
            if (m_hitParticles == null)
                m_hitParticles = transform.Find("HitParticles")?.GetComponent<ParticleSystem>();
            if (m_spinParticles == null)
                m_spinParticles = transform.Find("SpinParticles")?.GetComponent<ParticleSystem>();
            if (m_audioSource == null)
                m_audioSource = GetComponent<AudioSource>();

            // 设置组件
            SetupComponents();
        }

        private void SetupComponents()
        {
            // 设置球体物理
            m_ballPhysics.SetBallData(m_ballData);

            // 设置拖尾效果
            if (m_trailRenderer != null)
            {
                m_trailRenderer.startWidth = m_ballData.TrailWidth;
                m_trailRenderer.time = m_ballData.TrailTime;
                m_trailRenderer.startColor = m_ballData.TrailColor;
            }

            // 设置粒子系统
            if (m_hitParticles != null)
            {
                var main = m_hitParticles.main;
                main.startColor = m_ballData.TrailColor;
                main.startSize = m_ballData.Radius * 0.5f;
            }

            if (m_spinParticles != null)
            {
                var main = m_spinParticles.main;
                main.startColor = m_ballData.TrailColor;
                main.startSize = m_ballData.Radius * 0.3f;
            }

            // 设置音频源
            if (m_audioSource != null)
            {
                m_audioSource.spatialBlend = 1f; // 3D音效
                m_audioSource.minDistance = 1f;
                m_audioSource.maxDistance = 20f;
                m_audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        // 播放碰撞音效
        public void PlayHitSound(float volume = 1f)
        {
            if (m_audioSource != null)
            {
                m_audioSource.volume = volume;
                m_audioSource.Play();
            }
        }

        // 播放旋转音效
        public void PlaySpinSound(float volume = 1f)
        {
            if (m_audioSource != null)
            {
                m_audioSource.volume = volume * 0.5f;
                m_audioSource.Play();
            }
        }

        // 播放粒子效果
        public void PlayHitParticles()
        {
            if (m_hitParticles != null)
            {
                m_hitParticles.Play();
            }
        }

        public void PlaySpinParticles()
        {
            if (m_spinParticles != null)
            {
                m_spinParticles.Play();
            }
        }

        // 属性
        public BallData BallData => m_ballData;
    }
}