// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using PongHub.Arena.Services;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// 游戏阶段监听器接口
    /// 实现此接口的类可以注册到游戏管理器中,以接收游戏阶段变化、时间更新和队伍颜色更新的通知
    /// </summary>
    public interface IGamePhaseListener
    {
        /// <summary>
        /// 当游戏阶段发生变化时调用
        /// </summary>
        /// <param name="phase">新的游戏阶段</param>
        void OnPhaseChanged(GameManager.GamePhase phase);

        /// <summary>
        /// 当游戏阶段剩余时间更新时调用
        /// </summary>
        /// <param name="timeLeft">当前阶段的剩余时间(秒)</param>
        void OnPhaseTimeUpdate(double timeLeft);

        /// <summary>
        /// 当队伍颜色更新时调用
        /// </summary>
        /// <param name="teamColorA">A队的颜色</param>
        /// <param name="teamColorB">B队的颜色</param>
        void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB);
    }
}