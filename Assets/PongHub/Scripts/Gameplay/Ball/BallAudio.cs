using UnityEngine;
using PongHub.Core;

namespace PongHub.Ball
{
    [RequireComponent(typeof(AudioSource))]
    public class BallAudio : MonoBehaviour
    {
        [Header("音效配置")]
        [SerializeField] private AudioClip m_hitSound;
        [SerializeField] private AudioClip m_spinSound;
        [SerializeField] private AudioClip m_scoreSound;
        [SerializeField] private AudioClip m_netSound;
        [SerializeField] private AudioClip m_edgeSound;

        [Header("音量设置")]
        [SerializeField] private float m_hitVolume = 1f;
        [SerializeField] private float m_spinVolume = 0.5f;
        [SerializeField] private float m_scoreVolume = 1f;
        [SerializeField] private float m_netVolume = 0.8f;
        [SerializeField] private float m_edgeVolume = 0.8f;

        private AudioSource m_audioSource;

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            SetupAudioSource();
        }

        private void SetupAudioSource()
        {
            m_audioSource.spatialBlend = 1f; // 3D音效
            m_audioSource.minDistance = 1f;
            m_audioSource.maxDistance = 20f;
            m_audioSource.rolloffMode = AudioRolloffMode.Linear;
            m_audioSource.playOnAwake = false;
        }

        // 播放击球音效
        public void PlayHitSound(float volume = 1f)
        {
            if (m_hitSound != null)
            {
                m_audioSource.PlayOneShot(m_hitSound, volume * m_hitVolume);
            }
        }

        // 播放旋转音效
        public void PlaySpinSound(float volume = 1f)
        {
            if (m_spinSound != null)
            {
                m_audioSource.PlayOneShot(m_spinSound, volume * m_spinVolume);
            }
        }

        // 播放得分音效
        public void PlayScoreSound()
        {
            if (m_scoreSound != null)
            {
                m_audioSource.PlayOneShot(m_scoreSound, m_scoreVolume);
            }
        }

        // 播放球网音效
        public void PlayNetSound(float volume = 1f)
        {
            if (m_netSound != null)
            {
                m_audioSource.PlayOneShot(m_netSound, volume * m_netVolume);
            }
        }

        // 播放边缘音效
        public void PlayEdgeSound(float volume = 1f)
        {
            if (m_edgeSound != null)
            {
                m_audioSource.PlayOneShot(m_edgeSound, volume * m_edgeVolume);
            }
        }

        // 设置音效
        public void SetHitSound(AudioClip clip)
        {
            m_hitSound = clip;
        }

        public void SetSpinSound(AudioClip clip)
        {
            m_spinSound = clip;
        }

        public void SetScoreSound(AudioClip clip)
        {
            m_scoreSound = clip;
        }

        public void SetNetSound(AudioClip clip)
        {
            m_netSound = clip;
        }

        public void SetEdgeSound(AudioClip clip)
        {
            m_edgeSound = clip;
        }

        // 设置音量
        public void SetHitVolume(float volume)
        {
            m_hitVolume = Mathf.Clamp01(volume);
        }

        public void SetSpinVolume(float volume)
        {
            m_spinVolume = Mathf.Clamp01(volume);
        }

        public void SetScoreVolume(float volume)
        {
            m_scoreVolume = Mathf.Clamp01(volume);
        }

        public void SetNetVolume(float volume)
        {
            m_netVolume = Mathf.Clamp01(volume);
        }

        public void SetEdgeVolume(float volume)
        {
            m_edgeVolume = Mathf.Clamp01(volume);
        }
    }
}