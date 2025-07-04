// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using Random = UnityEngine.Random;

namespace PongHub.Arena.VFX
{
    /// <summary>
    /// Controls the end game fireworks to play them randomly inside the winning and losing side colliders, used as
    /// editable boxes in editor to define where the fireworks can be spawned.
    /// It plays fireworks randomly from the array of firewaorks provided as well as play the firework audio sound.
    /// </summary>
    public class FireworkController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Fireworks Array / 烟花数组 - Array of firework particle systems to randomly play")]
        private ParticleSystem[] m_fireworks;

        [SerializeField]
        [Tooltip("Winner Collider / 获胜者碰撞体 - Collider defining the area where winner fireworks can spawn")]
        private Collider m_winnerCollider;

        [SerializeField]
        [Tooltip("Loser Collider / 失败者碰撞体 - Collider defining the area where loser fireworks can spawn")]
        private Collider m_loserCollider;

        [SerializeField]
        [Tooltip("Min Time / 最小时间间隔 - Minimum time in seconds between firework spawns")]
        private float m_minTime = 0.2f;

        [SerializeField]
        [Tooltip("Max Time / 最大时间间隔 - Maximum time in seconds between firework spawns")]
        private float m_maxTime = 0.6f;

        [SerializeField]
        [Tooltip("Audio Sources / 音频源数组 - Array of audio sources for playing firework explosion sounds")]
        private AudioSource[] m_audioSources;

        [SerializeField]
        [Tooltip("Explosion Sound / 爆炸音效 - Audio clip to play when firework explodes")]
        private AudioClip m_explosionSound;

        private int m_nextAudioSourceIndex = 0;

        private float m_timer = 0;
        private float m_nextAt = 0;

        private void OnEnable()
        {
            SetNextTime();
        }

        private void Update()
        {
            m_timer += Time.deltaTime;
            if (m_timer >= m_nextAt)
            {
                m_timer -= m_nextAt;
                SetNextTime();
                PlayFirework();
            }
        }

        private void PlayFirework()
        {
            var index = Random.Range(0, m_fireworks.Length);
            var winner = Random.Range(0, 2) == 0;
            var box = winner ? m_winnerCollider : m_loserCollider;
            var min = box.bounds.min;
            var max = box.bounds.max;
            var x = Random.Range(min.x, max.x);
            var y = Random.Range(min.y, max.y);
            var z = Random.Range(min.z, max.z);
            var pos = new Vector3(x, y, z);
            m_fireworks[index].transform.position = pos;
            m_fireworks[index].Play();

            var audioSource = m_audioSources[m_nextAudioSourceIndex];
            audioSource.transform.position = pos;
            audioSource.PlayOneShot(m_explosionSound);
            if (++m_nextAudioSourceIndex >= m_audioSources.Length)
            {
                m_nextAudioSourceIndex = 0;
            }
        }

        private void SetNextTime()
        {
            m_nextAt = Random.Range(m_minTime, m_maxTime);
        }
    }
}