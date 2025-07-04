using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;
using System.Threading.Tasks;

namespace PongHub.UI
{
    public class ScoreboardPanel : MonoBehaviour
    {
        [Header("玩家信息")]
        [SerializeField]
        [Tooltip("Player Name Text / 玩家姓名文本 - Text component for displaying player name")]
        private TextMeshProUGUI m_playerNameText;

        [SerializeField]
        [Tooltip("Player Total Score Text / 玩家总分文本 - Text component for displaying player total score")]
        private TextMeshProUGUI m_playerTotalScoreText;

        [SerializeField]
        [Tooltip("Player Round Score Text / 玩家回合分数文本 - Text component for displaying player round score")]
        private TextMeshProUGUI m_playerRoundScoreText;

        [Header("对手信息")]
        [SerializeField]
        [Tooltip("Opponent Name Text / 对手姓名文本 - Text component for displaying opponent name")]
        private TextMeshProUGUI m_opponentNameText;

        [SerializeField]
        [Tooltip("Opponent Total Score Text / 对手总分文本 - Text component for displaying opponent total score")]
        private TextMeshProUGUI m_opponentTotalScoreText;

        [SerializeField]
        [Tooltip("Opponent Round Score Text / 对手回合分数文本 - Text component for displaying opponent round score")]
        private TextMeshProUGUI m_opponentRoundScoreText;

        [Header("游戏状态")]
        [SerializeField]
        [Tooltip("Game Status Text / 游戏状态文本 - Text component for displaying game status")]
        private TextMeshProUGUI m_gameStatusText;

        [SerializeField]
        [Tooltip("Round Status Text / 回合状态文本 - Text component for displaying round status")]
        private TextMeshProUGUI m_roundStatusText;

        [SerializeField]
        [Tooltip("Timer Text / 计时器文本 - Text component for displaying timer")]
        private TextMeshProUGUI m_timerText;

        [Header("按钮")]
        [SerializeField]
        [Tooltip("Rematch Button / 重新比赛按钮 - Button for starting a rematch")]
        private Button m_rematchButton;

        [SerializeField]
        [Tooltip("Main Menu Button / 主菜单按钮 - Button for returning to main menu")]
        private Button m_mainMenuButton;

        [Header("UI引用")]
        [SerializeField]
        [Tooltip("Left Player Score Text / 左方玩家分数文本 - Text component for left player score")]
        private TextMeshProUGUI m_leftPlayerScoreText;

        [SerializeField]
        [Tooltip("Right Player Score Text / 右方玩家分数文本 - Text component for right player score")]
        private TextMeshProUGUI m_rightPlayerScoreText;

        private void Awake()
        {
            UpdateScoreDisplay();
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            UpdateScoreDisplay();
        }

        private void Update()
        {
            if (GameCore.Instance != null)
            {
                UpdateScoreDisplay();
            }
        }

        private void UpdateScoreDisplay()
        {
            if (GameCore.Instance != null)
            {
                m_leftPlayerScoreText.text = GameCore.Instance.LeftPlayerScore.ToString();
                m_rightPlayerScoreText.text = GameCore.Instance.RightPlayerScore.ToString();
                m_gameStatusText.text = GameCore.Instance.IsGameActive ? "游戏进行中" : "游戏暂停";
            }
        }

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            // 重新比赛按钮
            if (m_rematchButton != null)
            {
                m_rematchButton.onClick.AddListener(() =>
                {
                    GameCore.Instance.ResetGame();
                });
            }

            // 主菜单按钮
            if (m_mainMenuButton != null)
            {
                m_mainMenuButton.onClick.AddListener(() =>
                {
                    GameCore.Instance.EndGame();
                });
            }
        }

        // 更新玩家信息
        public void UpdatePlayerInfo(string name, int totalScore, int roundScore)
        {
            if (m_playerNameText != null)
                m_playerNameText.text = name;
            if (m_playerTotalScoreText != null)
                m_playerTotalScoreText.text = totalScore.ToString();
            if (m_playerRoundScoreText != null)
                m_playerRoundScoreText.text = roundScore.ToString();
        }

        // 更新对手信息
        public void UpdateOpponentInfo(string name, int totalScore, int roundScore)
        {
            if (m_opponentNameText != null)
                m_opponentNameText.text = name;
            if (m_opponentTotalScoreText != null)
                m_opponentTotalScoreText.text = totalScore.ToString();
            if (m_opponentRoundScoreText != null)
                m_opponentRoundScoreText.text = roundScore.ToString();
        }

        // 更新游戏状态
        public void UpdateGameStatus(string status)
        {
            if (m_gameStatusText != null)
                m_gameStatusText.text = status;
        }

        // 更新回合状态
        public void UpdateRoundStatus(string status)
        {
            if (m_roundStatusText != null)
                m_roundStatusText.text = status;
        }

        // 更新计时器
        public void UpdateTimer(float timeInSeconds)
        {
            if (m_timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60);
                m_timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        // 显示/隐藏重新比赛按钮
        public void SetRematchButtonVisible(bool visible)
        {
            if (m_rematchButton != null)
                m_rematchButton.gameObject.SetActive(visible);
        }

        // 显示/隐藏主菜单按钮
        public void SetMainMenuButtonVisible(bool visible)
        {
            if (m_mainMenuButton != null)
                m_mainMenuButton.gameObject.SetActive(visible);
        }

        // 显示胜利动画
        public void ShowVictoryAnimation(string winnerName)
        {
            // TODO: 实现胜利动画
            UpdateGameStatus($"{winnerName} 获胜!");
        }

        // 显示失败动画
        public void ShowDefeatAnimation(string winnerName)
        {
            // TODO: 实现失败动画
            UpdateGameStatus($"{winnerName} 获胜!");
        }

        // 显示平局动画
        public void ShowDrawAnimation()
        {
            // TODO: 实现平局动画
            UpdateGameStatus("平局!");
        }
    }
}