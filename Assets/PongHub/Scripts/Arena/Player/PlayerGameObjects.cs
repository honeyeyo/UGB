// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using Oculus.Avatar2;
using PongHub.Arena.Gameplay;
using PongHub.Gameplay.Paddle;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 保存构成玩家实体的游戏对象引用,并在所有组件分配完成后进行初始化。
    /// 主要功能:
    /// - 存储玩家控制器、Avatar、球拍等组件引用
    /// - 管理球拍的附加与跟踪
    /// - 处理团队颜色组件
    /// </summary>
    public class PlayerGameObjects
    {
        // 玩家主要组件引用
        public PlayerControllerNetwork PlayerController; // 玩家网络控制器
        public PlayerAvatarEntity Avatar; // 玩家Avatar实体

        // 左右球拍引用
        public Paddle LeftPaddle;
        public Paddle RightPaddle;


        // 团队颜色组件列表
        public List<TeamColoringNetComponent> ColoringComponents = new();

        /// <summary>
        /// 尝试将球拍附加到Avatar上
        /// 主要步骤:
        /// 1. 检查所有必需组件是否就绪
        /// 2. 清除现有的团队颜色组件
        /// 3. 设置左右球拍的锚点和跟踪器
        /// 4. 更新玩家控制器的组件引用
        /// 5. 收集所有团队颜色组件
        /// </summary>
        public void TryAttachObjects()
        {
            // 检查所有必需组件是否存在且Avatar骨骼是否就绪
            if (Avatar == null || !Avatar.IsSkeletonReady)
            {
                return;
            }
            ColoringComponents.Clear();
        }
    }
}