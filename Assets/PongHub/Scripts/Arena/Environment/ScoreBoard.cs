// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using TMPro;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Services;
using UnityEngine;

namespace PongHub.Arena.Environment
{
    /// <summary>
    /// 记分牌视觉效果控制类。
    /// 主要功能:
    /// 1. 维护动态文本的引用
    /// 2. 处理记分牌变化时的渲染纹理更新
    /// 3. 监听游戏管理器的阶段变化来显示不同阶段的消息
    /// </summary>
    public class ScoreBoard : MonoBehaviour, IGamePhaseListener
    {
        [SerializeField] private GameManager m_gameManager;          // 游戏管理器引用
        [SerializeField] private Camera m_scoreCamera;               // 分数显示相机
        [SerializeField] private Camera m_phaseCamera;               // 游戏阶段显示相机
        [SerializeField] private TMP_Text m_teamATitle;             // A队标题文本
        [SerializeField] private TMP_Text m_teamAScore;             // A队分数文本
        [SerializeField] private TMP_Text m_teamBTitle;             // B队标题文本
        [SerializeField] private TMP_Text m_teamBScore;             // B队分数文本

        [SerializeField] private TMP_Text m_stateText;              // 游戏状态文本

        private long m_lastTimeLeftShown = 0;                       // 上次显示的剩余时间

        /// <summary>
        /// 初始化时注册分数更新和阶段监听器
        /// </summary>
        private void Start()
        {
            GameState.Instance.Score.OnScoreUpdated += OnScoreUpdated;
            m_gameManager.RegisterPhaseListener(this);

            m_scoreCamera.enabled = m_phaseCamera.enabled = false;
        }

        /// <summary>
        /// 销毁时取消注册监听器
        /// </summary>
        private void OnDestroy()
        {
            m_gameManager.UnregisterPhaseListener(this);
            GameState.Instance.Score.OnScoreUpdated -= OnScoreUpdated;
        }

        /// <summary>
        /// 更新队伍颜色时的回调
        /// </summary>
        /// <param name="teamAColor">A队颜色</param>
        /// <param name="teamBColor">B队颜色</param>
        public void OnTeamColorUpdated(TeamColor teamAColor, TeamColor teamBColor)
        {
            var colorA = TeamColorProfiles.Instance.GetColorForKey(teamAColor);
            var colorB = TeamColorProfiles.Instance.GetColorForKey(teamBColor);
            m_teamAScore.color = colorA;
            m_teamATitle.color = colorA;
            m_teamBScore.color = colorB;
            m_teamBTitle.color = colorB;

            m_scoreCamera.Render();
        }

        /// <summary>
        /// 游戏阶段变化时的回调
        /// </summary>
        /// <param name="phase">新的游戏阶段</param>
        public void OnPhaseChanged(GameManager.GamePhase phase)
        {
            string msg = null;
            switch (phase)
            {
                case GameManager.GamePhase.PreGame:
                    msg = "Host press start";
                    break;
                case GameManager.GamePhase.CountDown:
                    msg = "Ready!";
                    break;
                case GameManager.GamePhase.InGame:
                    msg = "PLAY!";
                    break;
                case GameManager.GamePhase.PostGame:
                    msg = "Game Over";
                    break;
                default:
                    break;
            }

            m_stateText.text = msg;

            m_phaseCamera.Render();
        }

        /// <summary>
        /// 更新阶段剩余时间的回调
        /// </summary>
        /// <param name="timeLeft">剩余时间(秒)</param>
        public void OnPhaseTimeUpdate(double timeLeft)
        {
            var timeFloored = (long)Math.Floor(timeLeft);
            if (m_lastTimeLeftShown == timeFloored)
            {
                return;
            }
            var time = TimeSpan.FromSeconds(timeFloored);
            var timeStr = time.ToString("mm':'ss");
            if (timeStr == m_stateText.text)
            {
                return;
            }

            m_stateText.text = timeStr;
            m_lastTimeLeftShown = timeFloored;
            m_phaseCamera.Render();
        }

        /// <summary>
        /// 分数更新时的回调
        /// </summary>
        /// <param name="teamA">A队分数</param>
        /// <param name="teamB">B队分数</param>
        private void OnScoreUpdated(int teamA, int teamB)
        {
            m_teamAScore.text = teamA.ToString();
            m_teamBScore.text = teamB.ToString();

            m_scoreCamera.Render();
        }
    }
}