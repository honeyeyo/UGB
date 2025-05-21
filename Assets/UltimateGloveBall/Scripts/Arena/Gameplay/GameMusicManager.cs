// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using PongHub.Arena.Services;
using UnityEngine;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 游戏音乐管理器
    /// 根据当前游戏阶段播放相应的背景音乐
    /// 实现了IGamePhaseListener接口以响应游戏阶段变化
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GameMusicManager : MonoBehaviour, IGamePhaseListener
    {
        /// <summary>
        /// 游戏管理器引用
        /// </summary>
        [SerializeField] private GameManager m_gameManager;

        /// <summary>
        /// 音乐播放器组件
        /// 使用AutoSet特性自动获取组件引用
        /// </summary>
        [SerializeField, AutoSet] private AudioSource m_musicAudioSource;

        /// <summary>
        /// 赛前阶段背景音乐
        /// </summary>
        [SerializeField] private AudioClip m_preGameClip;

        /// <summary>
        /// 游戏中阶段背景音乐
        /// </summary>
        [SerializeField] private AudioClip m_inGameClip;

        /// <summary>
        /// 赛后阶段背景音乐
        /// </summary>
        [SerializeField] private AudioClip m_postGameClip;

        /// <summary>
        /// 初始化时注册为游戏阶段监听器
        /// </summary>
        private void Awake()
        {
            m_gameManager.RegisterPhaseListener(this);
        }

        /// <summary>
        /// 销毁时取消注册游戏阶段监听器
        /// </summary>
        private void OnDestroy()
        {
            m_gameManager.UnregisterPhaseListener(this);
        }

        /// <summary>
        /// 游戏阶段变化时的回调
        /// 根据不同的游戏阶段播放对应的背景音乐
        /// </summary>
        /// <param name="phase">新的游戏阶段</param>
        public void OnPhaseChanged(GameManager.GamePhase phase)
        {
            switch (phase)
            {
                case GameManager.GamePhase.PreGame:
                    PlayPreGameMusic();
                    break;
                case GameManager.GamePhase.CountDown:
                    // 倒计时阶段不播放音乐
                    break;
                case GameManager.GamePhase.InGame:
                    PlayInGameMusic();
                    break;
                case GameManager.GamePhase.PostGame:
                    PlayPostGameMusic();
                    break;
                default:
                    StopMusic();
                    break;
            }
        }

        /// <summary>
        /// 游戏阶段时间更新的回调
        /// 当前未使用此功能
        /// </summary>
        /// <param name="timeLeft">剩余时间</param>
        public void OnPhaseTimeUpdate(double timeLeft)
        {
            // 暂未实现
        }

        /// <summary>
        /// 队伍颜色更新的回调
        /// 当前未使用此功能
        /// </summary>
        /// <param name="teamColorA">A队颜色</param>
        /// <param name="teamColorB">B队颜色</param>
        public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)
        {
            // 暂未实现
        }

        /// <summary>
        /// 播放赛前阶段背景音乐
        /// </summary>
        private void PlayPreGameMusic()
        {
            m_musicAudioSource.clip = m_preGameClip;
            m_musicAudioSource.Play();
        }

        /// <summary>
        /// 播放游戏中阶段背景音乐
        /// </summary>
        private void PlayInGameMusic()
        {
            m_musicAudioSource.clip = m_inGameClip;
            m_musicAudioSource.Play();
        }

        /// <summary>
        /// 播放赛后阶段背景音乐
        /// </summary>
        private void PlayPostGameMusic()
        {
            m_musicAudioSource.clip = m_postGameClip;
            m_musicAudioSource.Play();
        }

        /// <summary>
        /// 停止播放背景音乐
        /// </summary>
        private void StopMusic()
        {
            m_musicAudioSource.Stop();
        }
    }
}
