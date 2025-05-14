// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Utilities;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player.Respawning;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltimateGloveBall.Arena.Player
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

        private bool m_shieldActivated = false; // 护盾是否激活
        private Glove.GloveSide m_shieldSide = Glove.GloveSide.Left; // 当前激活护盾的手套侧

        public GloveArmatureNetworking ArmatureRight; // 右手装甲网络组件
        public GloveArmatureNetworking ArmatureLeft; // 左手装甲网络组件

        public GloveNetworking GloveRight; // 右手手套网络组件
        public GloveNetworking GloveLeft; // 左手手套网络组件

        private NetworkVariable<float> m_shieldCharge = new(SHIELD_MAX_CHARGE); // 护盾充能值
        private NetworkVariable<float> m_shieldOffTimer = new(); // 护盾关闭计时器
        private NetworkVariable<bool> m_shieldInResetMode = new(false); // 护盾是否处于重置模式
        private NetworkVariable<bool> m_shieldDisabled = new(false); // 护盾是否被禁用

        public NetworkVariable<bool> IsInvulnerable = new(); // 是否处于无敌状态
        private readonly HashSet<Object> m_invulnerabilityActors = new(); // 赋予无敌状态的对象集合

        public NetworkedTeam NetworkedTeamComp => m_networkedTeam; // 获取网络队伍组件
        public RespawnController RespawnController => m_respawnController; // 获取重生控制器

        public Action<bool> OnInvulnerabilityStateUpdatedEvent; // 无敌状态更新事件

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

            IsInvulnerable.OnValueChanged += OnInvulnerabilityStateChanged;
            OnInvulnerabilityStateChanged(IsInvulnerable.Value, IsInvulnerable.Value);
        }

        /// <summary>
        /// 设置玩家无敌状态
        /// </summary>
        /// <param name="setter">设置无敌状态的对象</param>
        public void SetInvulnerability(Object setter)
        {
            if (IsServer)
            {
                _ = m_invulnerabilityActors.Add(setter);
                if (!IsInvulnerable.Value)
                {
                    IsInvulnerable.Value = true;
                }
            }
        }

        /// <summary>
        /// 移除玩家无敌状态
        /// </summary>
        /// <param name="setter">移除无敌状态的对象</param>
        public void RemoveInvulnerability(Object setter)
        {
            if (IsServer)
            {
                _ = m_invulnerabilityActors.Remove(setter);
                if (IsInvulnerable.Value && m_invulnerabilityActors.Count == 0)
                {
                    IsInvulnerable.Value = false;
                }
            }
        }

        /// <summary>
        /// 清除所有无敌状态
        /// </summary>
        public void ClearInvulnerability()
        {
            if (IsServer)
            {
                m_invulnerabilityActors.Clear();
                IsInvulnerable.Value = false;
            }
        }

        /// <summary>
        /// 当无敌状态改变时的处理
        /// </summary>
        private void OnInvulnerabilityStateChanged(bool previousValue, bool newValue)
        {
            m_collider.enabled = !newValue;
            _ = StartCoroutine(SetAvatarState());
            OnInvulnerabilityStateUpdatedEvent?.Invoke(newValue);
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
            material.SetKeyword("ENABLE_GHOST_EFFECT", IsInvulnerable.Value);
            m_avatar.ApplyMaterial();

            ArmatureLeft.SetGhostEffect(IsInvulnerable.Value);
            ArmatureRight.SetGhostEffect(IsInvulnerable.Value);

            GloveLeft.SetGhostEffect(IsInvulnerable.Value);
            GloveRight.SetGhostEffect(IsInvulnerable.Value);
        }

        /// <summary>
        /// 触发护盾
        /// </summary>
        /// <param name="side">护盾激活的手套侧</param>
        public void TriggerShield(Glove.GloveSide side)
        {
            if (m_shieldDisabled.Value)
            {
                if (side == Glove.GloveSide.Right)
                {
                    ArmatureRight.OnShieldNotAvailable();
                }
                else
                {
                    ArmatureLeft.OnShieldNotAvailable();
                }
            }
            else
            {
                TriggerShieldServerRPC(side);
            }
        }

        /// <summary>
        /// 处理护盾被击中的效果
        /// </summary>
        /// <param name="side">被击中的护盾侧</param>
        public void OnShieldHit(Glove.GloveSide side)
        {
            m_shieldCharge.Value = 0;
            StopShield(side);
            m_shieldInResetMode.Value = true;
            m_shieldDisabled.Value = true;
            ArmatureLeft.DisableShield();
            ArmatureRight.DisableShield();
            ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
            ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
        }

        /// <summary>
        /// 服务器RPC:触发护盾
        /// </summary>
        /// <param name="side">护盾激活的手套侧</param>
        [ServerRpc]
        public void TriggerShieldServerRPC(Glove.GloveSide side)
        {
            if (m_shieldActivated)
            {
                if (m_shieldSide == side)
                {
                    return;
                }
                // 切换护盾侧时,先停用当前侧的护盾
                {
                    if (m_shieldSide == Glove.GloveSide.Right)
                    {
                        ArmatureRight.DeactivateShield();
                    }
                    else
                    {
                        ArmatureLeft.DeactivateShield();
                    }
                }
            }

            m_shieldActivated = true;
            m_shieldSide = side;

            if (m_shieldSide == Glove.GloveSide.Right)
            {
                ArmatureRight.ActivateShield();
            }
            else
            {
                ArmatureLeft.ActivateShield();
            }
        }

        /// <summary>
        /// 服务器RPC:停止护盾
        /// </summary>
        /// <param name="side">要停止的护盾侧</param>
        [ServerRpc]
        public void StopShieldServerRPC(Glove.GloveSide side)
        {
            StopShield(side);
        }

        /// <summary>
        /// 停止指定侧的护盾
        /// </summary>
        /// <param name="side">要停止的护盾侧</param>
        private void StopShield(Glove.GloveSide side)
        {
            if (!IsServer)
            {
                return;
            }

            if (!m_shieldActivated || side != m_shieldSide)
            {
                return;
            }

            m_shieldActivated = false;

            if (m_shieldSide == Glove.GloveSide.Right)
            {
                ArmatureRight.DeactivateShield();
            }
            else
            {
                ArmatureLeft.DeactivateShield();
            }
        }

        /// <summary>
        /// 每帧更新
        /// 处理护盾的充能、消耗和重置逻辑
        /// </summary>
        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            if (m_shieldActivated)
            {
                m_shieldCharge.Value -= SHIELD_USAGE_RATE * Time.deltaTime;
                if (m_shieldCharge.Value <= 0)
                {
                    m_shieldCharge.Value = 0;
                    StopShield(m_shieldSide);
                    m_shieldInResetMode.Value = true;
                    m_shieldDisabled.Value = true;
                    ArmatureLeft.DisableShield();
                    ArmatureRight.DisableShield();
                }

                ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
                ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
            }
            else if (m_shieldInResetMode.Value)
            {
                m_shieldOffTimer.Value += Time.deltaTime;
                if (m_shieldOffTimer.Value >= SHIELD_RESET_TIME)
                {
                    m_shieldOffTimer.Value = 0;
                    m_shieldInResetMode.Value = false;
                }
            }
            else if (m_shieldCharge.Value < SHIELD_MAX_CHARGE)
            {
                m_shieldCharge.Value += SHIELD_CHARGE_RATE * Time.deltaTime;
                if (m_shieldCharge.Value >= SHIELD_MAX_CHARGE)
                {
                    m_shieldCharge.Value = SHIELD_MAX_CHARGE;
                    if (m_shieldDisabled.Value)
                    {
                        m_shieldDisabled.Value = false;
                        ArmatureLeft.EnableShield();
                        ArmatureRight.EnableShield();
                    }
                }
                ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
                ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
            }
        }
    }
}
