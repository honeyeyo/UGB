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
            StartCoroutine(VictoryAnimationCoroutine(winnerName));
        }

        // 显示失败动画
        public void ShowDefeatAnimation(string winnerName)
        {
            StartCoroutine(DefeatAnimationCoroutine(winnerName));
        }

        // 显示平局动画
        public void ShowDrawAnimation()
        {
            StartCoroutine(DrawAnimationCoroutine());
        }

        /// <summary>
        /// 胜利动画协程 - 实现胜利特效和文本动画
        /// </summary>
        private System.Collections.IEnumerator VictoryAnimationCoroutine(string winnerName)
        {
            // 更新游戏状态文本
            UpdateGameStatus($"🏆 {winnerName} 获胜!");
            
            // 闪烁效果
            Color originalColor = m_gameStatusText.color;
            Color victoryColor = Color.yellow;
            
            for (int i = 0; i < 6; i++)
            {
                m_gameStatusText.color = (i % 2 == 0) ? victoryColor : originalColor;
                yield return new WaitForSeconds(0.3f);
            }
            
            // 恢复原色
            m_gameStatusText.color = originalColor;
            
            // 显示操作按钮
            SetRematchButtonVisible(true);
            SetMainMenuButtonVisible(true);
        }

        /// <summary>
        /// 失败动画协程 - 实现失败提示和按钮显示
        /// </summary>
        private System.Collections.IEnumerator DefeatAnimationCoroutine(string winnerName)
        {
            // 更新游戏状态文本
            UpdateGameStatus($"😔 {winnerName} 获胜!");
            
            // 淡入淡出效果
            Color originalColor = m_gameStatusText.color;
            Color defeatColor = Color.red;
            
            float duration = 2f;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float t = Mathf.PingPong(elapsedTime * 2f, 1f);
                m_gameStatusText.color = Color.Lerp(originalColor, defeatColor, t * 0.5f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原色
            m_gameStatusText.color = originalColor;
            
            // 显示操作按钮
            SetRematchButtonVisible(true);
            SetMainMenuButtonVisible(true);
        }

        /// <summary>
        /// 平局动画协程 - 实现平局提示动画
        /// </summary>
        private System.Collections.IEnumerator DrawAnimationCoroutine()
        {
            // 更新游戏状态文本
            UpdateGameStatus("🤝 平局!");
            
            // 脉冲缩放效果
            Vector3 originalScale = m_gameStatusText.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            
            float duration = 1f;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float t = Mathf.PingPong(elapsedTime * 2f, 1f);
                m_gameStatusText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原大小
            m_gameStatusText.transform.localScale = originalScale;
            
            // 显示操作按钮
            SetRematchButtonVisible(true);
            SetMainMenuButtonVisible(true);
        }
    }
}