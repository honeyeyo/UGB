// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using Meta.Utilities;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using PongHub.App;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Player;
using PongHub.Gameplay.Paddle;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 跟踪玩家实体。
    /// 在根级别,我们有本地玩家实体(玩家控制器、球拍、头像),
    /// 然后我们跟踪每个其他玩家的游戏对象以便于引用。
    /// 它实现了一个自定义逻辑来设置本地玩家实体,当所有实体都加载完成时。
    /// 这是必要的,因为不同的实体是可以按任意顺序加载的不同网络对象,
    /// 我们需要在所有实体都生成和加载后才能设置玩家。
    /// </summary>
    public class LocalPlayerEntities : Singleton<LocalPlayerEntities>
    {
        // 本地玩家的网络控制器组件
        public PlayerControllerNetwork LocalPlayerController;

        // 玩家头像实体
        public PlayerAvatarEntity Avatar;

        // 左右球拍引用
        public Paddle LeftPaddle;
        public Paddle RightPaddle;

        // 本地玩家的游戏对象集合
        private readonly PlayerGameObjects m_localPlayerGameObjects = new();

        // 存储其他玩家的游戏对象集合,键为客户端ID
        private readonly Dictionary<ulong, PlayerGameObjects> m_playerObjects = new();
        // 所有玩家的客户端ID列表
        public List<ulong> PlayerIds { get; } = new();

        /// <summary>
        /// 启动时初始化,注册网络回调
        /// </summary>
        private void Start()
        {
            DontDestroyOnLoad(this);

            // 延迟初始化网络回调，确保PHApplication完全加载
            StartCoroutine(InitializeNetworkCallbacks());
        }

        /// <summary>
        /// 延迟初始化网络回调的协程
        /// </summary>
        private System.Collections.IEnumerator InitializeNetworkCallbacks()
        {
            // 等待PHApplication实例初始化
            while (PHApplication.Instance == null)
            {
                yield return null;
            }

            // 等待NetworkLayer初始化
            while (PHApplication.Instance.NetworkLayer == null)
            {
                yield return null;
            }

            // 安全地注册网络回调
            var networkLayer = PHApplication.Instance.NetworkLayer;
            networkLayer.OnClientDisconnectedCallback += OnClientDisconnected;
            networkLayer.StartHostCallback += OnHostStarted;
            networkLayer.RestoreHostCallback += OnHostStarted;
        }

        /// <summary>
        /// 销毁时取消注册网络回调
        /// </summary>
        private void OnDestroy()
        {
            // 安全地取消注册网络回调
            if (PHApplication.Instance != null && PHApplication.Instance.NetworkLayer != null)
            {
                var networkLayer = PHApplication.Instance.NetworkLayer;
                networkLayer.OnClientDisconnectedCallback -= OnClientDisconnected;
                networkLayer.StartHostCallback -= OnHostStarted;
                networkLayer.RestoreHostCallback -= OnHostStarted;
            }
        }

        /// <summary>
        /// 获取指定客户端ID的玩家游戏对象集合
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <returns>玩家游戏对象集合</returns>
        public PlayerGameObjects GetPlayerObjects(ulong clientId)
        {
            // 检查NetworkManager是否可用
            if (NetworkManager.Singleton == null)
            {
                // 如果NetworkManager不可用，创建新的玩家对象集合
                if (!m_playerObjects.TryGetValue(clientId, out var fallbackPlayerData))
                {
                    fallbackPlayerData = new();
                    m_playerObjects[clientId] = fallbackPlayerData;
                    if (!PlayerIds.Contains(clientId))
                    {
                        PlayerIds.Add(clientId);
                    }
                }
                return fallbackPlayerData;
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                return m_localPlayerGameObjects;
            }

            if (!m_playerObjects.TryGetValue(clientId, out var playerData))
            {
                playerData = new();
                m_playerObjects[clientId] = playerData;
                if (!PlayerIds.Contains(clientId))
                {
                    PlayerIds.Add(clientId);
                }
            }

            return playerData;
        }

        /// <summary>
        /// 尝试附加球拍到玩家身上
        /// 只有当所有必要组件都加载完成时才会执行
        /// </summary>
        public void TryAttachPaddles()
        {
            // 检查所有必要组件是否都已加载
            if (LeftPaddle == null || RightPaddle == null ||
                Avatar == null || !Avatar.IsSkeletonReady)
            {
                return;
            }

            // 检查NetworkManager是否可用
            if (NetworkManager.Singleton == null)
            {
                UnityEngine.Debug.LogWarning("NetworkManager.Singleton is null, cannot attach paddles properly");
                return;
            }

            // 确保本地玩家ID在列表中
            if (!PlayerIds.Contains(NetworkManager.Singleton.LocalClientId))
            {
                PlayerIds.Add(NetworkManager.Singleton.LocalClientId);
            }

            // 设置本地玩家的所有游戏对象
            m_localPlayerGameObjects.Avatar = Avatar;
            m_localPlayerGameObjects.PlayerController = LocalPlayerController;
            m_localPlayerGameObjects.LeftPaddle = LeftPaddle;
            m_localPlayerGameObjects.RightPaddle = RightPaddle;
            m_localPlayerGameObjects.TryAttachObjects();

            // 根据玩家队伍设置移动限制范围
            var teamComponent = Avatar.GetComponent<NetworkedTeam>();
            if (teamComponent == null)
            {
                UnityEngine.Debug.LogWarning("NetworkedTeam component not found on Avatar");
                // 设置默认移动限制
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, -9, 9);
                return;
            }

            var team = teamComponent.MyTeam;
            if (team == NetworkedTeam.Team.TeamA)
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, -9, -1f);
            }
            else if (team == NetworkedTeam.Team.TeamB)
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, 1f, 9);
            }
            else
            {
                PlayerMovement.Instance.SetLimits(-4.5f, 4.5f, -9, 9);
            }

            // 本地玩家加载完成,淡入画面
            if (OVRScreenFade.instance != null)
            {
                OVRScreenFade.instance.FadeIn();
            }
        }

        /// <summary>
        /// 检查玩家移动是否启用
        /// </summary>
        /// <returns>如果玩家移动启用则返回true</returns>
        private static bool IsMovementEnabled()
        {
            return PlayerInputController.Instance.MovementEnabled;
        }

        /// <summary>
        /// 主机启动时清理玩家数据
        /// </summary>
        private void OnHostStarted()
        {
            PlayerIds.Clear();
            m_playerObjects.Clear();
        }

        /// <summary>
        /// 客户端断开连接时移除相关玩家数据
        /// </summary>
        /// <param name="clientId">断开连接的客户端ID</param>
        private void OnClientDisconnected(ulong clientId)
        {
            _ = PlayerIds.Remove(clientId);
            _ = m_playerObjects.Remove(clientId);
        }
    }
}