// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using TMPro;

namespace PongHub.Arena.PostGame
{
    /// <summary>
    /// 技术统计面板
    /// 负责显示详细的技术统计数据，如制胜球、失误、发球统计等
    /// </summary>
    public class TechnicalStatsPanel : MonoBehaviour
    {
        [Header("基础统计显示")]
        [SerializeField] private TMP_Text m_winnersStatsText;        // 制胜球统计
        [SerializeField] private TMP_Text m_errorsStatsText;         // 失误统计
        [SerializeField] private TMP_Text m_serveStatsText;          // 发球统计
        [SerializeField] private TMP_Text m_rallyStatsText;          // 回合统计
        [SerializeField] private TMP_Text m_timeStatsText;           // 时间统计

        [Header("高级统计显示")]
        [SerializeField] private TMP_Text m_averageRallyText;        // 平均回合长度
        [SerializeField] private TMP_Text m_totalRalliesText;        // 总回合数
        [SerializeField] private TMP_Text m_winPercentageText;       // 获胜率

        [Header("可视化元素")]
        [SerializeField] private GameObject m_playerAStatsContainer; // A队统计容器
        [SerializeField] private GameObject m_playerBStatsContainer; // B队统计容器
        [SerializeField] private Color m_playerAColor = Color.blue;   // A队颜色
        [SerializeField] private Color m_playerBColor = Color.red;    // B队颜色

        /// <summary>
        /// 更新统计数据显示
        /// </summary>
        /// <param name="stats">游戏统计数据</param>
        public void UpdateStats(GameStatistics stats)
        {
            UpdateBasicStats(stats);
            UpdateAdvancedStats(stats);
            UpdateVisualElements(stats);
        }

        /// <summary>
        /// 更新基础统计数据
        /// </summary>
        private void UpdateBasicStats(GameStatistics stats)
        {
            // 制胜球统计
            if (m_winnersStatsText != null)
            {
                m_winnersStatsText.text = $"制胜球: <color=#{ColorUtility.ToHtmlStringRGB(m_playerAColor)}>{stats.PlayerAWinners}</color> - <color=#{ColorUtility.ToHtmlStringRGB(m_playerBColor)}>{stats.PlayerBWinners}</color>";
            }

            // 失误统计
            if (m_errorsStatsText != null)
            {
                m_errorsStatsText.text = $"失误: <color=#{ColorUtility.ToHtmlStringRGB(m_playerAColor)}>{stats.PlayerAErrors}</color> - <color=#{ColorUtility.ToHtmlStringRGB(m_playerBColor)}>{stats.PlayerBErrors}</color>";
            }

            // 发球统计
            if (m_serveStatsText != null)
            {
                m_serveStatsText.text = $"发球得分: <color=#{ColorUtility.ToHtmlStringRGB(m_playerAColor)}>{stats.PlayerAServeAces}</color> - <color=#{ColorUtility.ToHtmlStringRGB(m_playerBColor)}>{stats.PlayerBServeAces}</color>";
            }

            // 回合统计
            if (m_rallyStatsText != null)
            {
                m_rallyStatsText.text = $"最长回合: {stats.LongestRally} 次";
            }

            // 时间统计
            if (m_timeStatsText != null)
            {
                m_timeStatsText.text = $"本局用时: {FormatTime(stats.SetDuration)}";
            }
        }

        /// <summary>
        /// 更新高级统计数据
        /// </summary>
        private void UpdateAdvancedStats(GameStatistics stats)
        {
            // 平均回合长度
            if (m_averageRallyText != null)
            {
                m_averageRallyText.text = $"平均回合: {stats.AverageRallyLength:F1} 次";
            }

            // 总回合数
            if (m_totalRalliesText != null)
            {
                m_totalRalliesText.text = $"总回合数: {stats.TotalRallies}";
            }

            // 获胜率计算
            if (m_winPercentageText != null)
            {
                UpdateWinPercentage(stats);
            }
        }

        /// <summary>
        /// 更新获胜率显示
        /// </summary>
        private void UpdateWinPercentage(GameStatistics stats)
        {
            int totalPointsA = stats.PlayerAWinners + stats.PlayerAErrors;
            int totalPointsB = stats.PlayerBWinners + stats.PlayerBErrors;

            if (totalPointsA > 0 && totalPointsB > 0)
            {
                float winPercentageA = (float)stats.PlayerAWinners / totalPointsA * 100f;
                float winPercentageB = (float)stats.PlayerBWinners / totalPointsB * 100f;

                m_winPercentageText.text = $"获胜率: <color=#{ColorUtility.ToHtmlStringRGB(m_playerAColor)}>{winPercentageA:F1}%</color> - <color=#{ColorUtility.ToHtmlStringRGB(m_playerBColor)}>{winPercentageB:F1}%</color>";
            }
            else
            {
                m_winPercentageText.text = "获胜率: - - -";
            }
        }

        /// <summary>
        /// 更新可视化元素
        /// </summary>
        private void UpdateVisualElements(GameStatistics stats)
        {
            // 根据获胜方高亮显示对应的统计容器
            if (m_playerAStatsContainer != null)
            {
                bool isPlayerAWinner = stats.CurrentSetWinner == PongHub.Arena.Gameplay.NetworkedTeam.Team.TeamA;
                HighlightContainer(m_playerAStatsContainer, isPlayerAWinner);
            }

            if (m_playerBStatsContainer != null)
            {
                bool isPlayerBWinner = stats.CurrentSetWinner == PongHub.Arena.Gameplay.NetworkedTeam.Team.TeamB;
                HighlightContainer(m_playerBStatsContainer, isPlayerBWinner);
            }
        }

        /// <summary>
        /// 高亮显示容器
        /// </summary>
        private void HighlightContainer(GameObject container, bool isWinner)
        {
            var outline = container.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = isWinner;
                outline.effectColor = isWinner ? Color.yellow : Color.white;
            }

            // 可以添加更多的视觉效果，比如缩放、发光等
            if (isWinner)
            {
                var rectTransform = container.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 轻微放大获胜方的统计容器
                    rectTransform.localScale = Vector3.one * 1.05f;
                }
            }
            else
            {
                var rectTransform = container.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 60f)
            {
                return $"{timeInSeconds:F0}秒";
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                return $"{minutes}分{seconds:00}秒";
            }
        }

        /// <summary>
        /// 重置面板显示
        /// </summary>
        public void ResetPanel()
        {
            if (m_winnersStatsText != null)
                m_winnersStatsText.text = "制胜球: 0 - 0";

            if (m_errorsStatsText != null)
                m_errorsStatsText.text = "失误: 0 - 0";

            if (m_serveStatsText != null)
                m_serveStatsText.text = "发球得分: 0 - 0";

            if (m_rallyStatsText != null)
                m_rallyStatsText.text = "最长回合: 0 次";

            if (m_timeStatsText != null)
                m_timeStatsText.text = "本局用时: 00:00";

            if (m_averageRallyText != null)
                m_averageRallyText.text = "平均回合: 0.0 次";

            if (m_totalRalliesText != null)
                m_totalRalliesText.text = "总回合数: 0";

            if (m_winPercentageText != null)
                m_winPercentageText.text = "获胜率: - - -";

            // 重置容器高亮
            if (m_playerAStatsContainer != null)
                HighlightContainer(m_playerAStatsContainer, false);

            if (m_playerBStatsContainer != null)
                HighlightContainer(m_playerBStatsContainer, false);
        }

        /// <summary>
        /// 设置玩家颜色
        /// </summary>
        public void SetPlayerColors(Color playerAColor, Color playerBColor)
        {
            m_playerAColor = playerAColor;
            m_playerBColor = playerBColor;
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}