// Copyright (c) Meta Platforms, Inc. and affiliates.

using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 语音IP处理器
    /// 保持对音频扬声器和语音录制器的引用以处理静音功能
    /// 管理本地和远程玩家的语音通信状态
    /// </summary>
    public class VoipHandler : MonoBehaviour
    {
        /// <summary>
        /// 扬声器音频源
        /// 用于播放远程玩家语音的音频源组件
        /// </summary>
        private AudioSource m_speakerAudioSource;

        /// <summary>
        /// 语音连接组件
        /// 用于录制和传输本地玩家语音的连接器
        /// </summary>
        private VoiceConnection m_voiceRecorder;

        /// <summary>
        /// 是否静音状态
        /// 控制本地玩家的静音状态
        /// </summary>
        private bool m_isMuted = false;

        /// <summary>
        /// 静音状态属性
        /// 获取或设置当前的静音状态，会同步影响扬声器和录制器
        /// </summary>
        public bool IsMuted
        {
            get => m_isMuted;
            set
            {
                m_isMuted = value;

                // 如果有扬声器音频源，设置其静音状态
                if (m_speakerAudioSource)
                {
                    m_speakerAudioSource.mute = m_isMuted;
                }

                // 如果有语音录制器，设置其传输启用状态
                if (m_voiceRecorder)
                {
                    // 静音时禁用传输，非静音时启用传输
                    m_voiceRecorder.PrimaryRecorder.TransmitEnabled = !m_isMuted;
                }
            }
        }

        /// <summary>
        /// 设置语音录制器
        /// 为本地玩家设置语音录制组件
        /// </summary>
        /// <param name="recorder">语音连接组件</param>
        public void SetRecorder(VoiceConnection recorder)
        {
            m_voiceRecorder = recorder;
            // 根据当前静音状态设置传输启用状态
            m_voiceRecorder.PrimaryRecorder.TransmitEnabled = !m_isMuted;
        }

        /// <summary>
        /// 设置扬声器
        /// 为远程玩家设置语音播放组件
        /// </summary>
        /// <param name="speaker">扬声器组件</param>
        public void SetSpeaker(Speaker speaker)
        {
            // 获取扬声器的音频源组件
            m_speakerAudioSource = speaker.GetComponent<AudioSource>();

            // 确保扬声器有音频源组件
            Assert.IsNotNull(m_speakerAudioSource, "Voip Speaker should have an AudioSource component.");

            // 根据当前静音状态设置扬声器静音
            m_speakerAudioSource.mute = m_isMuted;
        }
    }
}