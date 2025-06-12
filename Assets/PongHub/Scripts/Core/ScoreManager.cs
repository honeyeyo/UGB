using UnityEngine;
using System;

namespace PongHub.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        // 乒乓球比赛规则相关
        private const int WINNING_SCORE = 11;  // 获胜分数
        private const int MIN_LEAD = 2;        // 最小领先分数

        public int Player1Score { get; private set; }
        public int Player2Score { get; private set; }

        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnPlayerWon;  // 参数为获胜玩家编号(1或2)

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

        public void ResetScores()
        {
            Player1Score = 0;
            Player2Score = 0;
            OnScoreChanged?.Invoke(Player1Score, Player2Score);
        }

        public void AddScore(int playerNumber)
        {
            if (playerNumber == 1)
            {
                Player1Score++;
            }
            else if (playerNumber == 2)
            {
                Player2Score++;
            }

            OnScoreChanged?.Invoke(Player1Score, Player2Score);

            // 检查是否有玩家获胜
            CheckForWinner();
        }

        private void CheckForWinner()
        {
            // 检查是否达到获胜分数
            if (Player1Score >= WINNING_SCORE || Player2Score >= WINNING_SCORE)
            {
                // 检查是否满足最小领先分数要求
                if (Mathf.Abs(Player1Score - Player2Score) >= MIN_LEAD)
                {
                    int winner = Player1Score > Player2Score ? 1 : 2;
                    OnPlayerWon?.Invoke(winner);
                }
            }
        }

        public bool IsGameOver()
        {
            return (Player1Score >= WINNING_SCORE || Player2Score >= WINNING_SCORE) &&
                   Mathf.Abs(Player1Score - Player2Score) >= MIN_LEAD;
        }

        public int GetLeadingPlayer()
        {
            if (Player1Score > Player2Score) return 1;
            if (Player2Score > Player1Score) return 2;
            return 0; // 平局
        }
    }
}