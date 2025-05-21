// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using PongHub.Arena.Gameplay;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 竞技场玩家数据容器
    /// 用于存储连接到竞技场的玩家数据。保存玩家的连接状态,
    /// 当玩家尝试重新连接到同一竞技场时可以使用这些数据。
    /// </summary>
    public struct ArenaPlayerData
    {
        /// <summary>
        /// 客户端唯一标识ID
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// 玩家唯一标识ID
        /// </summary>
        public string PlayerId;

        /// <summary>
        /// 玩家选择的队伍
        /// </summary>
        public NetworkedTeam.Team SelectedTeam;

        /// <summary>
        /// 玩家是否已连接
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// 是否为观众模式
        /// </summary>
        public bool IsSpectator;

        /// <summary>
        /// 玩家出生点索引
        /// </summary>
        public int SpawnPointIndex;

        /// <summary>
        /// 游戏结束时是否在获胜方
        /// </summary>
        public bool PostGameWinnerSide;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="isSpectator">是否为观众,默认为false</param>
        public ArenaPlayerData(ulong clientId, string playerId, bool isSpectator = false)
        {
            ClientId = clientId;
            PlayerId = playerId;
            SelectedTeam = NetworkedTeam.Team.NoTeam;
            IsConnected = true;
            IsSpectator = isSpectator;
            SpawnPointIndex = -1;
            PostGameWinnerSide = false;
        }
    }
}