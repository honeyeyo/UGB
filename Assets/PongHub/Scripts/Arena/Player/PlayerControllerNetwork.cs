// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Utilities;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Player.Respawning;
using PongHub.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 控制玩家状态。处理护盾状态、无敌状态、队伍状态,并引用重生控制器。
    /// 主要功能:
    /// - 管理护盾的激活、充能和消耗
    /// - 处理玩家的无敌状态
    /// - 控制玩家的队伍归属
    /// - 管理玩家重生
    /// </summary>
    public class PlayerControllerNetwork : NetworkBehaviour
    {
        // 护盾相关常量
        private const float SHIELD_USAGE_RATE = 20f; // 护盾使用消耗速率
        private const float SHIELD_CHARGE_RATE = 32f; // 护盾充能速率
        private const float SHIELD_MAX_CHARGE = 100f; // 护盾最大充能值
        private const float SHIELD_RESET_TIME = 0.5f; // 护盾重置时间

        [SerializeField] private Collider m_collider; // 玩家碰撞体
        [SerializeField] private PlayerAvatarEntity m_avatar; // 玩家Avatar实体
        [SerializeField, AutoSet] private NetworkedTeam m_networkedTeam; // 网络同步的队伍组件
        [SerializeField, AutoSet] private RespawnController m_respawnController; // 重生控制器

        public NetworkedTeam NetworkedTeamComp => m_networkedTeam; // 获取网络队伍组件
        public RespawnController RespawnController => m_respawnController; // 获取重生控制器

        /// <summary>
        /// 当网络对象生成时调用
        /// 初始化本地玩家引用和事件监听
        /// </summary>
        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
            if (IsOwner)
            {
                LocalPlayerEntities.Instance.LocalPlayerController = this;
            }
            else
            {
                LocalPlayerEntities.Instance.GetPlayerObjects(OwnerClientId).PlayerController = this;
            }
        }

        /// <summary>
        /// 设置Avatar的状态效果
        /// </summary>
        private IEnumerator SetAvatarState()
        {
            if (!m_avatar.IsSkeletonReady)
            {
                yield return new WaitUntil(() => m_avatar.IsSkeletonReady);
            }

            var material = m_avatar.Material;
            // 移除了无敌状态相关代码
            m_avatar.ApplyMaterial();


        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        private void Update()
        {
            if (!IsServer)
            {
                return;
            }
        }
    }
}
