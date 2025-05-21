// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 处理游戏开始时的倒计时视图
    /// 包括数字变化时的视觉和音频效果
    /// 当倒计时完成时会触发回调
    /// </summary>
    public class CountdownView : MonoBehaviour
    {
        /// <summary>
        /// 倒计时文本组件
        /// </summary>
        [SerializeField] private TMP_Text m_text;

        /// <summary>
        /// 音频源组件,用于播放倒计时音效
        /// </summary>
        [SerializeField] private AudioSource m_audioSource;

        /// <summary>
        /// 普通倒计时音效
        /// </summary>
        [SerializeField] private AudioClip m_beep1;

        /// <summary>
        /// 倒计时结束时的特殊音效
        /// </summary>
        [SerializeField] private AudioClip m_beep2;

        /// <summary>
        /// 倒计时结束时间
        /// </summary>
        private double m_endTime;

        /// <summary>
        /// 倒计时完成时的回调函数
        /// </summary>
        private Action m_onComplete;

        /// <summary>
        /// 上一次显示的倒计时数值
        /// </summary>
        private int m_previous = -1;

        /// <summary>
        /// 是否正在显示倒计时
        /// </summary>
        private bool m_showing;

        /// <summary>
        /// 每帧更新倒计时显示
        /// 计算剩余时间,更新文本显示,播放音效
        /// 当倒计时结束时触发回调
        /// </summary>
        private void Update()
        {
            if (m_showing)
            {
                // 计算剩余时间
                var time = m_endTime - NetworkManager.Singleton.ServerTime.Time;
                var seconds = Math.Max(0, (int)Math.Floor(time));

                // 更新显示文本
                m_text.text = seconds == 0 ? "GO" : seconds.ToString();

                // 当数值变化时播放音效
                if (m_previous != seconds)
                {
                    TriggerBeep(seconds);
                }

                m_previous = seconds;

                // 倒计时结束时隐藏视图并触发回调
                if (time < 0)
                {
                    Hide();
                    m_onComplete?.Invoke();
                }
            }
        }

        /// <summary>
        /// 显示倒计时视图
        /// </summary>
        /// <param name="endTime">倒计时结束时间</param>
        /// <param name="onComplete">倒计时完成时的回调函数</param>
        public void Show(double endTime, Action onComplete = null)
        {
            m_text.gameObject.SetActive(true);
            m_showing = true;
            m_endTime = endTime;
            if (onComplete != null)
            {
                m_onComplete = onComplete;
            }
        }

        /// <summary>
        /// 隐藏倒计时视图
        /// 重置状态
        /// </summary>
        public void Hide()
        {
            m_text.gameObject.SetActive(false);
            m_showing = false;
            m_previous = -1;
        }

        /// <summary>
        /// 根据倒计时数值播放对应的音效
        /// </summary>
        /// <param name="val">当前倒计时数值</param>
        private void TriggerBeep(int val)
        {
            if (val == 0)
            {
                m_audioSource.PlayOneShot(m_beep2);
            }
            else
            {
                m_audioSource.PlayOneShot(m_beep1);
            }
        }
    }
}