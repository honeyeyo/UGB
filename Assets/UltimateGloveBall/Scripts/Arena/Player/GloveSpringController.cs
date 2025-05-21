// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 控制手套弹簧的动画，根据玩家的按压状态进行弹簧的压缩与释放。
    /// 负责驱动BlendShape动画、蒸汽特效和音效。
    /// </summary>
    public class GloveSpringController : MonoBehaviour
    {
        // 弹簧压缩速率（每秒增加的压缩值）
        private const float COMPRESSION_RATE = 300f;
        // 弹簧释放（解压）速率（每秒减少的压缩值）
        private const float DECOMPRESSION_RATE = 1200f;

        // 需要驱动BlendShape的SkinnedMeshRenderer列表
        [SerializeField] private List<SkinnedMeshRenderer> m_meshes;
        // 弹簧释放时的蒸汽粒子特效
        [SerializeField] private ParticleSystem m_steamVFX;

        // 弹簧音效播放器
        [SerializeField] private AudioSource m_springAudioSource;
        // 弹簧充能音效
        [SerializeField] private AudioClip m_springChargeAudio;
        // 弹簧释放音效
        [SerializeField] private AudioClip m_springReleaseAudio;

        // 当前弹簧是否处于激活（压缩）状态
        private bool m_activated = false;
        // 当前弹簧的压缩程度（0-100）
        private float m_compression = 0;
        // 是否正在播放压缩/释放动画
        private bool m_animating = false;

        /// <summary>
        /// 弹簧压缩程度（归一化到0-1），1为完全压缩，0为未压缩
        /// </summary>
        public float Compression => Mathf.Clamp01(m_compression / 100f);

        /// <summary>
        /// 激活弹簧（开始压缩），播放充能音效
        /// </summary>
        public void Activate()
        {
            if (!m_activated)
            {
                // 停止当前音效，切换到充能音效并播放
                m_springAudioSource.Stop();
                m_springAudioSource.clip = m_springChargeAudio;
                m_springAudioSource.Play();
            }
            m_activated = true;
            m_animating = true;
        }

        /// <summary>
        /// 释放弹簧（开始解压），播放释放音效和蒸汽特效
        /// </summary>
        public void Deactivate()
        {
            if (m_activated)
            {
                // 播放蒸汽特效
                m_steamVFX.Play(true);
                // 停止当前音效，切换到释放音效并播放
                m_springAudioSource.Stop();
                m_springAudioSource.clip = m_springReleaseAudio;
                m_springAudioSource.Play();
            }
            m_activated = false;
            m_animating = true;
        }

        /// <summary>
        /// 每帧更新弹簧的压缩/释放动画
        /// </summary>
        private void Update()
        {
            if (m_animating)
            {
                if (m_activated)
                {
                    // 压缩弹簧
                    m_compression += Time.deltaTime * COMPRESSION_RATE;
                    if (m_compression >= 100f)
                    {
                        m_compression = 100f;
                        m_animating = false; // 到达最大压缩，停止动画
                    }
                }
                else
                {
                    // 释放弹簧
                    m_compression -= Time.deltaTime * DECOMPRESSION_RATE;
                    if (m_compression <= 0f)
                    {
                        m_compression = 0f;
                        m_animating = false; // 完全释放，停止动画
                    }
                }

                // 更新BlendShape权重
                UpdateCompression();
            }
        }

        /// <summary>
        /// 根据当前压缩程度，设置所有SkinnedMeshRenderer的BlendShape权重
        /// </summary>
        private void UpdateCompression()
        {
            foreach (var mesh in m_meshes)
            {
                // 只控制索引为0的BlendShape（假设弹簧压缩BlendShape在索引0）
                mesh.SetBlendShapeWeight(0, m_compression);
            }
        }
    }
}