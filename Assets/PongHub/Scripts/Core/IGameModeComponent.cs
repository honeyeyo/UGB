using System;

namespace PongHub.Core
{
    /// <summary>
    /// 游戏模式枚举
    /// </summary>
    public enum GameMode
    {
        Local,      // 单机练习模式
        Network,    // 多人网络模式 (保持与现有代码兼容)
        Multiplayer = Network, // 别名，指向Network
        Menu        // 菜单模式（临时状态）
    }

    /// <summary>
    /// 游戏模式组件接口
    /// 实现此接口的组件可以响应游戏模式变化
    /// </summary>
    public interface IGameModeComponent
    {
        /// <summary>
        /// 当游戏模式改变时调用
        /// </summary>
        /// <param name="newMode">新的游戏模式</param>
        /// <param name="previousMode">之前的游戏模式</param>
        void OnGameModeChanged(GameMode newMode, GameMode previousMode);

        /// <summary>
        /// 检查组件是否在指定模式下激活
        /// </summary>
        /// <param name="mode">要检查的游戏模式</param>
        /// <returns>如果在该模式下激活返回true</returns>
        bool IsActiveInMode(GameMode mode);
    }
}