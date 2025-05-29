using UnityEngine;
using System.Threading.Tasks;
using PongHub.Gameplay;
using PongHub.UI;

#pragma warning disable CS0414

namespace PongHub.Core
{
    public enum GameState
    {
        None,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public class GameCore : MonoBehaviour
    {
        private static GameCore s_instance;
        public static GameCore Instance => s_instance;

        [Header("游戏设置")]
        [SerializeField] private int m_maxScore = 11;
        [SerializeField] private float m_gameStartDelay = 3f;
        [SerializeField] private float m_pointDelay = 1f;

        [Header("游戏状态")]
        private bool m_isGameActive;
        private int m_leftPlayerScore;
        private int m_rightPlayerScore;
        private GameState m_currentState;

        public GameState CurrentState => m_currentState;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            ResetGame();
        }

        public void SetState(GameState newState)
        {
            m_currentState = newState;
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void Cleanup()
        {
            // 清理游戏资源
        }

        public void StartGame()
        {
            m_isGameActive = true;
            ResetGame();
        }

        public void EndGame()
        {
            m_isGameActive = false;
        }

        public void AddPoint(bool isLeftPlayer)
        {
            if (!m_isGameActive) return;

            if (isLeftPlayer)
                m_leftPlayerScore++;
            else
                m_rightPlayerScore++;

            // 检查是否达到胜利条件
            if (m_leftPlayerScore >= m_maxScore || m_rightPlayerScore >= m_maxScore)
            {
                EndGame();
            }
        }

        public void ResetGame()
        {
            m_leftPlayerScore = 0;
            m_rightPlayerScore = 0;
            SetState(GameState.Playing);
        }

        // 属性
        public bool IsGameActive => m_isGameActive;
        public int LeftPlayerScore => m_leftPlayerScore;
        public int RightPlayerScore => m_rightPlayerScore;
        public int MaxScore => m_maxScore;
    }
}

#pragma warning restore CS0414