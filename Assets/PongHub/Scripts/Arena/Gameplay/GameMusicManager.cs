// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Utilities;
using PongHub.Arena.Services;
using PongHub.Core.Audio;
using UnityEngine;
using System.Collections;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 游戏音乐管理器
    /// 根据当前游戏阶段播放相应的背景音乐
    /// 实现了IGamePhaseListener接口以响应游戏阶段变化
    /// 重构为使用新的AudioService系统
    /// </summary>
    public class GameMusicManager : MonoBehaviour, IGamePhaseListener
    {
        /// <summary>
        /// 游戏管理器引用
        /// </summary>
        [SerializeField] private GameManager m_gameManager;

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
        /// 音乐淡入淡出时间
        /// </summary>
        [SerializeField] private float m_fadeTime = 2.0f;

        /// <summary>
        /// 音乐音量
        /// </summary>
        [SerializeField] private float m_musicVolume = 0.7f;

        /// <summary>
        /// 当前播放的音乐句柄
        /// </summary>
        private AudioHandle m_currentMusicHandle;

        /// <summary>
        /// 音频服务引用
        /// </summary>
        private AudioService AudioService => AudioService.Instance;

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

            // 停止当前播放的音乐
            StopMusic();
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
                    // 倒计时阶段降低音乐音量
                    DuckMusic();
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
            if (m_preGameClip == null) return;

            StopMusic();
            m_currentMusicHandle = AudioService.PlayWithFade(
                m_preGameClip,
                m_fadeTime,
                m_fadeTime,
                AudioCategory.Music
            );

            if (m_currentMusicHandle != null)
            {
                m_currentMusicHandle.SetVolume(m_musicVolume);
            }
        }

        /// <summary>
        /// 播放游戏中阶段背景音乐
        /// </summary>
        private void PlayInGameMusic()
        {
            if (m_inGameClip == null) return;

            StopMusic();
            m_currentMusicHandle = AudioService.PlayWithFade(
                m_inGameClip,
                m_fadeTime,
                m_fadeTime,
                AudioCategory.Music
            );

            if (m_currentMusicHandle != null)
            {
                m_currentMusicHandle.SetVolume(m_musicVolume);
            }
        }

        /// <summary>
        /// 播放赛后阶段背景音乐
        /// </summary>
        private void PlayPostGameMusic()
        {
            if (m_postGameClip == null) return;

            StopMusic();
            m_currentMusicHandle = AudioService.PlayWithFade(
                m_postGameClip,
                m_fadeTime,
                m_fadeTime,
                AudioCategory.Music
            );

            if (m_currentMusicHandle != null)
            {
                m_currentMusicHandle.SetVolume(m_musicVolume);
            }
        }

        /// <summary>
        /// 停止播放背景音乐
        /// </summary>
        private void StopMusic()
        {
            if (m_currentMusicHandle != null && m_currentMusicHandle.IsValid)
            {
                // 使用协程实现音量淡出后停止
                StartCoroutine(FadeOutAndStop(m_currentMusicHandle, m_fadeTime));
                m_currentMusicHandle = null;
            }
        }

        /// <summary>
        /// 协程：淡出音量后停止播放
        /// </summary>
        private IEnumerator FadeOutAndStop(AudioHandle handle, float fadeTime)
        {
            if (handle == null || !handle.IsValid) yield break;

            float startVolume = handle.Volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime && handle.IsValid)
            {
                elapsedTime += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
                handle.SetVolume(newVolume);
                yield return null;
            }

            if (handle.IsValid)
            {
                handle.Stop();
            }
        }

                /// <summary>
        /// 降低音乐音量（用于倒计时等场景）
        /// </summary>
        private void DuckMusic()
        {
            if (m_currentMusicHandle != null && m_currentMusicHandle.IsValid)
            {
                // 直接降低当前播放音乐的音量
                m_currentMusicHandle.SetVolume(m_musicVolume * 0.3f);

                // 3秒后恢复原音量
                StartCoroutine(RestoreMusicVolumeAfterDelay(3.0f));
            }
        }

        /// <summary>
        /// 延迟恢复音乐音量
        /// </summary>
        private IEnumerator RestoreMusicVolumeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (m_currentMusicHandle != null && m_currentMusicHandle.IsValid)
            {
                m_currentMusicHandle.SetVolume(m_musicVolume);
            }
        }

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetMusicVolume(float volume)
        {
            m_musicVolume = Mathf.Clamp01(volume);

            if (m_currentMusicHandle != null && m_currentMusicHandle.IsValid)
            {
                m_currentMusicHandle.SetVolume(m_musicVolume);
            }
        }

        /// <summary>
        /// 获取当前音乐音量
        /// </summary>
        public float GetMusicVolume()
        {
            return m_musicVolume;
        }

        /// <summary>
        /// 检查是否正在播放音乐
        /// </summary>
        public bool IsPlayingMusic()
        {
            return m_currentMusicHandle != null && m_currentMusicHandle.IsValid && m_currentMusicHandle.IsPlaying;
        }
    }
}
