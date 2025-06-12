// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Multiplayer.Avatar;
using Oculus.Avatar2;
using PongHub.Arena.Services;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 手套球游戏专用的Avatar实体实现。
    /// 当骨骼加载完成时,会将其引用到玩家实体并启用重生特效。
    /// 应用固定的手部姿势,使Avatar的手在游戏过程中始终保持握拳状态。
    /// </summary>
    public class PlayerAvatarEntity : AvatarEntity
    {
        [SerializeField] private OvrAvatarCustomHandPose m_rightHandPose; // 右手姿势
        [SerializeField] private OvrAvatarCustomHandPose m_leftHandPose; // 左手姿势
        [SerializeField] private GameObject m_respawnVfx; // 重生特效对象

        /// <summary>
        /// 骨骼是否已准备就绪
        /// </summary>
        public bool IsSkeletonReady { get; private set; } = false;

        /// <summary>
        /// 当骨骼加载完成时调用
        /// 设置骨骼就绪状态,根据网络所有权配置玩家实体,启用手部姿势和重生特效
        /// </summary>
        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();
            IsSkeletonReady = true;
            var netComp = GetComponent<NetworkObject>();

            // 如果是本地玩家
            if (netComp.IsOwner)
            {
                // 设置本地玩家Avatar并尝试附加手套
                LocalPlayerEntities.Instance.Avatar = this;
                // TODO : 乒乓球手柄attach实现

                // 启用右手姿势
                if (m_rightHandPose != null)
                {
                    m_rightHandPose.enabled = true;
                }

                // 启用左手姿势
                if (m_leftHandPose != null)
                {
                    m_leftHandPose.enabled = true;
                }
            }
            else // 如果是其他玩家
            {
                // 获取并设置玩家对象,尝试附加相关对象
                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(netComp.OwnerClientId);
                playerObjects.Avatar = this;
                playerObjects.TryAttachObjects();
            }

            // 当Avatar加载完成时显示重生特效
            if (m_respawnVfx)
            {
                m_respawnVfx.SetActive(true);
            }
        }
    }
}