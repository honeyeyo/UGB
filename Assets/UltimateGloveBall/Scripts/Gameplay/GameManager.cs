using UnityEngine;
using PongHub.Core;

namespace PongHub.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        [SerializeField] private float m_gameTime = 180f; // 3分钟
        [SerializeField] private int m_winScore = 11;

        private int m_playerScore;
        private int m_opponentScore;
        private float m_currentGameTime;
        private bool m_isGameActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartGame(bool isSinglePlayer)
        {
            m_playerScore = 0;
            m_opponentScore = 0;
            m_currentGameTime = m_gameTime;
            m_isGameActive = true;
        }

        public void RestartGame()
        {
            m_playerScore = 0;
            m_opponentScore = 0;
            m_currentGameTime = m_gameTime;
            m_isGameActive = true;
        }

        public void AddPlayerScore()
        {
            m_playerScore++;
            CheckGameEnd();
        }

        public void AddOpponentScore()
        {
            m_opponentScore++;
            CheckGameEnd();
        }

        private void CheckGameEnd()
        {
            if (m_playerScore >= m_winScore || m_opponentScore >= m_winScore)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            m_isGameActive = false;
        }

        private void Update()
        {
            if (m_isGameActive)
            {
                m_currentGameTime -= Time.deltaTime;
                if (m_currentGameTime <= 0)
                {
                    EndGame();
                }
            }
        }

        // 属性
        public bool IsGameActive => m_isGameActive;
        public int PlayerScore => m_playerScore;
        public int OpponentScore => m_opponentScore;
        public float GameTime => m_currentGameTime;
    }
} 