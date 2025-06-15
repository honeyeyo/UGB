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
        [SerializeField] private BallPhysics m_ballPhysics;
        [SerializeField] private BallSpinVisual m_ballVisual;
        [SerializeField] private BallNetworking m_ballNetworking;
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private ParticleSystem m_hitParticles;
        [SerializeField] private ParticleSystem m_spinParticles;
        [SerializeField] private AudioSource m_audioSource;

        [Header("配置")]
        [SerializeField] private BallData m_ballData;

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