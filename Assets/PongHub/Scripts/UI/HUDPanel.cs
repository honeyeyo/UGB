using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;

namespace PongHub.UI
{
    public class HUDPanel : MonoBehaviour
    {
        [Header("分数显示")]
        [SerializeField]
        [Tooltip("Player Score Text / 玩家分数文本 - Text component for displaying player score")]
        private TextMeshProUGUI m_playerScoreText;

        [SerializeField]
        [Tooltip("Opponent Score Text / 对手分数文本 - Text component for displaying opponent score")]
        private TextMeshProUGUI m_opponentScoreText;

        [SerializeField]
        [Tooltip("Timer Text / 计时器文本 - Text component for displaying game timer")]
        private TextMeshProUGUI m_timerText;

        [Header("球的状态")]
        [SerializeField]
        [Tooltip("Ball Speed Text / 球速文本 - Text component for displaying ball speed")]
        private TextMeshProUGUI m_ballSpeedText;

        [SerializeField]
        [Tooltip("Ball Spin Text / 球旋转文本 - Text component for displaying ball spin")]
        private TextMeshProUGUI m_ballSpinText;

        [SerializeField]
        [Tooltip("Ball Speed Bar / 球速条 - Image component for ball speed bar")]
        private Image m_ballSpeedBar;

        [SerializeField]
        [Tooltip("Ball Spin Bar / 球旋转条 - Image component for ball spin bar")]
        private Image m_ballSpinBar;

        [Header("提示信息")]
        [SerializeField]
        [Tooltip("Message Text / 消息文本 - Text component for displaying messages")]
        private TextMeshProUGUI m_messageText;

        [SerializeField]
        [Tooltip("Message Duration / 消息持续时间 - Duration for message display in seconds")]
        private float m_messageDuration = 2f;

        private float m_messageTimer;
        private bool m_isMessageVisible;

        private void Update()
        {
            if (m_isMessageVisible)
            {
                m_messageTimer -= Time.deltaTime;
                if (m_messageTimer <= 0f)
                {
                    HideMessage();
                }
            }
        }

        // 更新分数
        public void UpdateScores(int playerScore, int opponentScore)
        {
            if (m_playerScoreText != null)
                m_playerScoreText.text = playerScore.ToString();
            if (m_opponentScoreText != null)
                m_opponentScoreText.text = opponentScore.ToString();
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

        // 更新球的状态
        public void UpdateBallStatus(float speed, float maxSpeed, float spin, float maxSpin)
        {
            if (m_ballSpeedText != null)
                m_ballSpeedText.text = speed.ToString("F1");
            if (m_ballSpinText != null)
                m_ballSpinText.text = spin.ToString("F1");
            if (m_ballSpeedBar != null)
                m_ballSpeedBar.fillAmount = speed / maxSpeed;
            if (m_ballSpinBar != null)
                m_ballSpinBar.fillAmount = spin / maxSpin;
        }

        // 显示消息
        public void ShowMessage(string message)
        {
            if (m_messageText != null)
            {
                m_messageText.text = message;
                m_messageText.gameObject.SetActive(true);
                m_messageTimer = m_messageDuration;
                m_isMessageVisible = true;
            }
        }

        // 隐藏消息
        private void HideMessage()
        {
            if (m_messageText != null)
            {
                m_messageText.gameObject.SetActive(false);
                m_isMessageVisible = false;
            }
        }
    }
}