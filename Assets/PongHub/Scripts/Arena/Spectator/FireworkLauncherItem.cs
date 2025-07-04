// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using UnityEngine;

namespace PongHub.Arena.Spectator
{
    /// <summary>
    /// This spectator item has custom logic to launch a firework in the arena.
    /// </summary>
    public class FireworkLauncherItem : SpectatorItem
    {
        private const float FIREWORK_RECHARGE = 5.0f;

        [SerializeField]
        [Tooltip("Muzzle Location / 发射口位置 - Transform position where fireworks are launched from")]
        private Transform m_muzzleLocation;

        [SerializeField]
        [Tooltip("Muzzle VFX / 发射口特效 - Particle system for muzzle flash effect")]
        private ParticleSystem m_muzzleVFX;

        [SerializeField]
        [Tooltip("Audio Source / 音频源 - AudioSource component for sound effects")]
        private AudioSource m_audioSource;

        [SerializeField]
        [Tooltip("Launch Sound / 发射音效 - Audio clip for successful launch sound")]
        private AudioClip m_launchSound;

        [SerializeField]
        [Tooltip("Failed Launch Sound / 发射失败音效 - Audio clip for failed launch attempt")]
        private AudioClip m_failedLaunchSound;

        [SerializeField]
        [Tooltip("Projectile Visual / 弹体可视化 - GameObject showing the loaded projectile")]
        private GameObject m_projectileVisual;

        [SerializeField]
        [Tooltip("Projectile Prefab / 弹体预制体 - Prefab for the firework projectile")]
        private Projectile m_projectilePrefab;

        private Projectile m_projectile;

        private bool m_readyToLaunch = true;
        private float m_rechargeTimer;

        public Action<Vector3, float> OnLaunch;

        private void Awake()
        {
            m_projectile = Instantiate(m_projectilePrefab);
            m_projectile.gameObject.SetActive(false);
        }

        public void TryLaunch()
        {
            if (!m_readyToLaunch)
            {
                m_audioSource.PlayOneShot(m_failedLaunchSound);
                return;
            }

            var destination =
                SpectatorFireworkController.Instance.LaunchFirework(m_muzzleLocation.position, m_muzzleLocation.forward,
                    out var travelTime);
            m_audioSource.PlayOneShot(m_launchSound);
            m_readyToLaunch = false;
            _ = StartCoroutine(Recharge());

            OnLaunch?.Invoke(destination, travelTime);
            m_muzzleVFX.Play();
            m_projectileVisual.SetActive(false);
            m_projectile.Launch(m_muzzleLocation.position, destination, travelTime);
        }

        private IEnumerator Recharge()
        {
            m_rechargeTimer = 0;
            while (m_rechargeTimer < FIREWORK_RECHARGE)
            {
                yield return null;
                m_rechargeTimer += Time.deltaTime;
            }

            m_readyToLaunch = true;
            m_projectileVisual.SetActive(true);
        }
    }
}