// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using PongHub.Arena.Gameplay;
using Unity.Netcode;

namespace PongHub.Arena.PostGame
{
    /// <summary>
    /// 游戏统计数据结构
    /// 用于收集和存储单局或整场比赛的统计信息
    /// </summary>
        [Serializable]
    public class GameStatistics
    {
        // 基础比分
        public int PlayerAScore;              // A队分数
        public int PlayerBScore;              // B队分数
        public int CurrentSet;                // 当前局数
        public int MaxSets;                   // 最大局数
        public int PlayerASetsWon;            // A队获胜局数
        public int PlayerBSetsWon;            // B队获胜局数

        // 技术统计 - A队
        public int PlayerAWinners;            // A队制胜球
        public int PlayerAErrors;             // A队失误
        public int PlayerAServeAces;          // A队发球得分
        public int PlayerAReturnWins;         // A队接发球得分

        // 技术统计 - B队
        public int PlayerBWinners;            // B队制胜球
        public int PlayerBErrors;             // B队失误
        public int PlayerBServeAces;          // B队发球得分
        public int PlayerBReturnWins;         // B队接发球得分

        // 比赛时长
        public float SetDuration;             // 本局用时（秒）
        public float TotalGameTime;           // 总比赛时间（秒）

        // 回合统计
        public int LongestRally;              // 最长回合（击球次数）
        public float AverageRallyLength;      // 平均回合长度
        public int TotalRallies;              // 总回合数

        // 获胜信息
        public NetworkedTeam.Team CurrentSetWinner;  // 本局获胜方
        public NetworkedTeam.Team MatchWinner;       // 整场比赛获胜方
        public bool IsMatchComplete;                 // 是否完成整场比赛

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameStatistics()
        {
            Reset();
        }

        /// <summary>
        /// 重置所有统计数据
        /// </summary>
        public void Reset()
        {
            PlayerAScore = 0;
            PlayerBScore = 0;
            CurrentSet = 1;
            MaxSets = 5;
            PlayerASetsWon = 0;
            PlayerBSetsWon = 0;

            PlayerAWinners = 0;
            PlayerAErrors = 0;
            PlayerAServeAces = 0;
            PlayerAReturnWins = 0;

            PlayerBWinners = 0;
            PlayerBErrors = 0;
            PlayerBServeAces = 0;
            PlayerBReturnWins = 0;

            SetDuration = 0f;
            TotalGameTime = 0f;

            LongestRally = 0;
            AverageRallyLength = 0f;
            TotalRallies = 0;

            CurrentSetWinner = NetworkedTeam.Team.NoTeam;
            MatchWinner = NetworkedTeam.Team.NoTeam;
            IsMatchComplete = false;
        }

        /// <summary>
        /// 重置单局统计（保留总体统计）
        /// </summary>
        public void ResetSetStatistics()
        {
            PlayerAScore = 0;
            PlayerBScore = 0;
            SetDuration = 0f;
            CurrentSetWinner = NetworkedTeam.Team.NoTeam;

            // 重置单局技术统计
            PlayerAWinners = 0;
            PlayerAErrors = 0;
            PlayerAServeAces = 0;
            PlayerAReturnWins = 0;

            PlayerBWinners = 0;
            PlayerBErrors = 0;
            PlayerBServeAces = 0;
            PlayerBReturnWins = 0;

            LongestRally = 0;
            TotalRallies = 0;
        }

        /// <summary>
        /// 获取获胜方文本
        /// </summary>
        public string GetWinnerText()
        {
            switch (CurrentSetWinner)
            {
                case NetworkedTeam.Team.TeamA:
                    return "玩家A获胜";
                case NetworkedTeam.Team.TeamB:
                    return "玩家B获胜";
                default:
                    return "平局";
            }
        }

        /// <summary>
        /// 获取比分文本
        /// </summary>
        public string GetScoreText()
        {
            return $"{PlayerAScore} - {PlayerBScore}";
        }

        /// <summary>
        /// 获取局数进度文本
        /// </summary>
        public string GetSetProgressText()
        {
            return $"第{CurrentSet}局 (总比分 {PlayerASetsWon}-{PlayerBSetsWon})";
        }

        /// <summary>
        /// 计算平均回合长度
        /// </summary>
        public void CalculateAverageRally()
        {
            if (TotalRallies > 0)
            {
                AverageRallyLength = (float)(PlayerAWinners + PlayerBWinners + PlayerAErrors + PlayerBErrors) / TotalRallies;
            }
            else
            {
                AverageRallyLength = 0f;
            }
        }
    }

    /// <summary>
    /// 网络同步的游戏统计数据结构
    /// </summary>
    [Serializable]
    public struct NetworkedGameStatistics : INetworkSerializable
    {
        public int playerAScore;
        public int playerBScore;
        public int currentSet;
        public int playerASetsWon;
        public int playerBSetsWon;

        public int playerAWinners;
        public int playerAErrors;
        public int playerBWinners;
        public int playerBErrors;

        public float setDuration;
        public int longestRally;
        public int totalRallies;

        public NetworkedTeam.Team currentSetWinner;
        public bool isMatchComplete;

        public NetworkedGameStatistics(GameStatistics stats)
        {
            playerAScore = stats.PlayerAScore;
            playerBScore = stats.PlayerBScore;
            currentSet = stats.CurrentSet;
            playerASetsWon = stats.PlayerASetsWon;
            playerBSetsWon = stats.PlayerBSetsWon;

            playerAWinners = stats.PlayerAWinners;
            playerAErrors = stats.PlayerAErrors;
            playerBWinners = stats.PlayerBWinners;
            playerBErrors = stats.PlayerBErrors;

            setDuration = stats.SetDuration;
            longestRally = stats.LongestRally;
            totalRallies = stats.TotalRallies;

            currentSetWinner = stats.CurrentSetWinner;
            isMatchComplete = stats.IsMatchComplete;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerAScore);
            serializer.SerializeValue(ref playerBScore);
            serializer.SerializeValue(ref currentSet);
            serializer.SerializeValue(ref playerASetsWon);
            serializer.SerializeValue(ref playerBSetsWon);

            serializer.SerializeValue(ref playerAWinners);
            serializer.SerializeValue(ref playerAErrors);
            serializer.SerializeValue(ref playerBWinners);
            serializer.SerializeValue(ref playerBErrors);

            serializer.SerializeValue(ref setDuration);
            serializer.SerializeValue(ref longestRally);
            serializer.SerializeValue(ref totalRallies);

            serializer.SerializeValue(ref currentSetWinner);
            serializer.SerializeValue(ref isMatchComplete);
        }

        public GameStatistics ToGameStatistics()
        {
            var stats = new GameStatistics
            {
                PlayerAScore = playerAScore,
                PlayerBScore = playerBScore,
                CurrentSet = currentSet,
                PlayerASetsWon = playerASetsWon,
                PlayerBSetsWon = playerBSetsWon,

                PlayerAWinners = playerAWinners,
                PlayerAErrors = playerAErrors,
                PlayerBWinners = playerBWinners,
                PlayerBErrors = playerBErrors,

                SetDuration = setDuration,
                LongestRally = longestRally,
                TotalRallies = totalRallies,

                CurrentSetWinner = currentSetWinner,
                IsMatchComplete = isMatchComplete
            };

            stats.CalculateAverageRally();
            return stats;
        }
    }
}