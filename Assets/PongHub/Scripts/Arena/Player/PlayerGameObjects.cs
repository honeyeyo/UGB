// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using Oculus.Avatar2;
using PongHub.Arena.Gameplay;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 保存构成玩家实体的游戏对象引用,并在所有组件分配完成后进行初始化。
    /// 主要功能:
    /// - 存储玩家控制器、Avatar、手套装甲等组件引用
    /// - 管理手套和装甲的附加与跟踪
    /// - 处理团队颜色组件
    /// </summary>
    public class PlayerGameObjects
    {
        // 玩家主要组件引用
        public PlayerControllerNetwork PlayerController; // 玩家网络控制器
        public PlayerAvatarEntity Avatar; // 玩家Avatar实体
        public GloveArmatureNetworking LeftGloveArmature; // 左手套装甲网络组件
        public GloveArmatureNetworking RightGloveArmature; // 右手套装甲网络组件
        public Glove LeftGloveHand; // 左手套组件
        public Glove RightGloveHand; // 右手套组件

        // 团队颜色组件列表
        public List<TeamColoringNetComponent> ColoringComponents = new();

        /// <summary>
        /// 尝试将手套和装甲附加到Avatar上
        /// 主要步骤:
        /// 1. 检查所有必需组件是否就绪
        /// 2. 清除现有的团队颜色组件
        /// 3. 设置左右手套的锚点和跟踪器
        /// 4. 配置手套装甲的电流特效点
        /// 5. 更新玩家控制器的组件引用
        /// 6. 收集所有团队颜色组件
        /// </summary>
        public void TryAttachObjects()
        {
            // 检查所有必需组件是否存在且Avatar骨骼是否就绪
            if (LeftGloveHand == null || RightGloveHand == null ||
                LeftGloveArmature == null || RightGloveArmature == null ||
                Avatar == null || !Avatar.IsSkeletonReady)
            {
                return;
            }
            ColoringComponents.Clear();

            // 设置左手套
            var leftWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);
            LeftGloveHand.HandAnchor = leftWrist;
            var leftTracker = leftWrist.gameObject.AddComponent<GloveTracker>();
            leftTracker.Glove = LeftGloveHand;
            leftTracker.Armature = LeftGloveArmature;
            leftTracker.UpdateTracking();
            LeftGloveArmature.ElectricTetherForHandPoint.SetParent(LeftGloveHand.transform, false);

            // 设置右手套
            var rightWrist = Avatar.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist);
            RightGloveHand.HandAnchor = rightWrist;
            var rightTracker = rightWrist.gameObject.AddComponent<GloveTracker>();
            rightTracker.Glove = RightGloveHand;
            rightTracker.Armature = RightGloveArmature;
            rightTracker.UpdateTracking();
            RightGloveArmature.ElectricTetherForHandPoint.SetParent(RightGloveHand.transform, false);

            // 更新玩家控制器的组件引用
            PlayerController.ArmatureLeft = LeftGloveArmature;
            PlayerController.ArmatureRight = RightGloveArmature;
            PlayerController.GloveLeft = LeftGloveHand.GloveNetworkComponent;
            PlayerController.GloveRight = RightGloveHand.GloveNetworkComponent;

            // 收集所有团队颜色组件
            ColoringComponents.Add(LeftGloveHand.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(LeftGloveArmature.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveHand.GetComponent<TeamColoringNetComponent>());
            ColoringComponents.Add(RightGloveArmature.GetComponent<TeamColoringNetComponent>());
        }
    }
}