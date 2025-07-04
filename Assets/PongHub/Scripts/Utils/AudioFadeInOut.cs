// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using UnityEngine;

namespace PongHub.Utils
{
    public class AudioFadeInOut : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Audio Source / 音频源 - AudioSource component to control fade in/out")]
        private AudioSource m_audioSource;

        [Range(0, 1)]
        [SerializeField]
        [Tooltip("Max Volume / 最大音量 - Maximum volume level when fully faded in")]
        private float m_maxVolume = 1f;

        [SerializeField]
        [Tooltip("Fade Duration / 淡入淡出时长 - Duration in seconds for fade in/out effect")]
        private float m_secondsFade = 2f;

        [SerializeField]
        [Tooltip("Fade In On Start / 开始时淡入 - Automatically fade in when the component starts")]
        private bool m_fadeInOnStart = true;

        private Coroutine m_coroutine;

        private void Start()
        {
            if (m_fadeInOnStart)
            {
                m_audioSource.volume = 0f;
                FadeIn();
            }
        }

        public void FadeIn()
        {
            StopFade();
            m_coroutine = StartCoroutine(FadeAudio(true));
        }

        public void FadeOut()
        {
            StopFade();
            m_coroutine = StartCoroutine(FadeAudio(false));
        }

        public void StopFade()
        {
            if (m_coroutine != null)
            {
                StopCoroutine(m_coroutine);
            }
        }

        private IEnumerator FadeAudio(bool fadeIn)
        {
            var pos = m_audioSource.volume / m_maxVolume;
            var toMove = fadeIn ? pos : 1 - pos;
            var time = toMove * m_secondsFade;
            while (time <= m_secondsFade)
            {
                time += Time.deltaTime;
                m_audioSource.volume = Mathf.Lerp(0, 1, (fadeIn ? time : m_secondsFade - time) / m_secondsFade) * m_maxVolume;
                yield return null;
            }

            m_audioSource.volume = fadeIn ? m_maxVolume : 0;
            m_coroutine = null;
        }
    }
}