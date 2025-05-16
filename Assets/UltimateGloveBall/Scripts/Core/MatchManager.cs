using UnityEngine;
using System;

namespace PongHub.Core
{
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public enum MatchState
        {
            WaitingForPlayers,
            Countdown,
            Playing,
            PointScored,
            GameOver
        }

        public MatchState CurrentState { get; private set; }
        public int CurrentRound { get; private set; }
        public int RoundsToWin { get; private set; } = 3;  // 默认三局两胜

        public event Action<MatchState> OnMatchStateChanged;
        public event Action<int> OnRoundChanged;
        public event Action<int> OnMatchWon;  // 参数为获胜玩家编号(1或2)

        private int player1RoundsWon;
        private int player2RoundsWon;

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

        private void Start()
        {
            ResetMatch();
        }

        public void ResetMatch()
        {
            CurrentRound = 1;
            player1RoundsWon = 0;
            player2RoundsWon = 0;
            SetMatchState(MatchState.WaitingForPlayers);
        }

        public void SetMatchState(MatchState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnMatchStateChanged?.Invoke(newState);

            switch (newState)
            {
                case MatchState.WaitingForPlayers:
                    HandleWaitingForPlayers();
                    break;
                case MatchState.Countdown:
                    HandleCountdown();
                    break;
                case MatchState.Playing:
                    HandlePlaying();
                    break;
                case MatchState.PointScored:
                    HandlePointScored();
                    break;
                case MatchState.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        private void HandleWaitingForPlayers()
        {
            // TODO: 等待玩家加入
        }

        private void HandleCountdown()
        {
            // TODO: 开始倒计时
        }

        private void HandlePlaying()
        {
            // TODO: 开始比赛
        }

        private void HandlePointScored()
        {
            // TODO: 处理得分后的状态
        }

        private void HandleGameOver()
        {
            // TODO: 处理比赛结束
        }

        public void StartRound()
        {
            SetMatchState(MatchState.Countdown);
        }

        public void EndRound(int winner)
        {
            if (winner == 1)
                player1RoundsWon++;
            else if (winner == 2)
                player2RoundsWon++;

            // 检查是否有玩家赢得比赛
            if (player1RoundsWon >= RoundsToWin || player2RoundsWon >= RoundsToWin)
            {
                int matchWinner = player1RoundsWon > player2RoundsWon ? 1 : 2;
                OnMatchWon?.Invoke(matchWinner);
                SetMatchState(MatchState.GameOver);
            }
            else
            {
                CurrentRound++;
                OnRoundChanged?.Invoke(CurrentRound);
                SetMatchState(MatchState.WaitingForPlayers);
            }
        }

        public void SetRoundsToWin(int rounds)
        {
            RoundsToWin = rounds;
        }

        public (int, int) GetRoundScores()
        {
            return (player1RoundsWon, player2RoundsWon);
        }
    }
}