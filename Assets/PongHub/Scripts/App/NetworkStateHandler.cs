// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using Meta.Multiplayer.Core;
using PongHub.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PongHub.App
{
    /// <summary>
    /// 网络状态处理器
    /// 处理来自NetworkLayer的回调事件
    /// 基于网络层的连接状态设置应用程序状态
    /// 处理主机连接、客户端连接和大厅连接的各种情况
    /// </summary>
    public class NetworkStateHandler
    {
        /// <summary>
        /// 协程运行器
        /// 用于启动和管理协程的MonoBehaviour实例
        /// </summary>
        private MonoBehaviour m_coroutineRunner;

        /// <summary>
        /// 网络层组件
        /// 处理网络连接和房间管理
        /// </summary>
        private NetworkLayer m_networkLayer;

        /// <summary>
        /// 导航控制器
        /// 处理场景导航和房间切换
        /// </summary>
        private NavigationController m_navigationController;

        /// <summary>
        /// 语音控制器
        /// 管理语音通信功能
        /// </summary>
        private VoipController m_voip;

        /// <summary>
        /// 本地玩家状态
        /// 存储当前本地玩家的状态信息
        /// </summary>
        private LocalPlayerState m_localPlayerState;

        /// <summary>
        /// 玩家存在处理器
        /// 管理玩家在线状态和群组存在
        /// </summary>
        private PlayerPresenceHandler m_playerPresenceHandler;

        /// <summary>
        /// 创建网络会话的函数委托
        /// 用于创建NetworkSession实例的工厂函数
        /// </summary>
        private Func<NetworkSession> m_createSessionFunc;

        /// <summary>
        /// 当前网络会话实例
        /// 管理当前活动的网络会话
        /// </summary>
        private NetworkSession m_session;

        /// <summary>
        /// 检查当前玩家是否为观众
        /// 从本地玩家状态获取观众标志
        /// </summary>
        private bool IsSpectator => m_localPlayerState.IsSpectator;

        /// <summary>
        /// 网络状态处理器构造函数
        /// 初始化所有组件引用并注册网络层回调事件
        /// </summary>
        /// <param name="coroutineRunner">协程运行器</param>
        /// <param name="networkLayer">网络层</param>
        /// <param name="navigationController">导航控制器</param>
        /// <param name="voip">语音控制器</param>
        /// <param name="localPlayerState">本地玩家状态</param>
        /// <param name="playerPresenceHandler">玩家存在处理器</param>
        /// <param name="createSessionFunc">创建会话的函数</param>
        public NetworkStateHandler(
            MonoBehaviour coroutineRunner,
            NetworkLayer networkLayer,
            NavigationController navigationController,
            VoipController voip,
            LocalPlayerState localPlayerState,
            PlayerPresenceHandler playerPresenceHandler,
            Func<NetworkSession> createSessionFunc)
        {
            // 保存组件引用
            m_coroutineRunner = coroutineRunner;
            m_networkLayer = networkLayer;
            m_navigationController = navigationController;
            m_voip = voip;
            m_localPlayerState = localPlayerState;
            m_playerPresenceHandler = playerPresenceHandler;
            m_createSessionFunc = createSessionFunc;

            // 注册网络层回调事件
            m_networkLayer.OnClientConnectedCallback += OnClientConnected;
            m_networkLayer.OnClientDisconnectedCallback += OnClientDisconnected;
            m_networkLayer.OnMasterClientSwitchedCallback += OnMasterClientSwitched;
            m_networkLayer.StartLobbyCallback += OnLobbyStarted;
            m_networkLayer.StartHostCallback += OnHostStarted;
            m_networkLayer.StartClientCallback += OnClientStarted;
            m_networkLayer.RestoreHostCallback += OnHostRestored;
            m_networkLayer.RestoreClientCallback += OnClientRestored;
            m_networkLayer.OnRestoreFailedCallback += OnRestoreFailed;

            // 注册函数委托
            m_networkLayer.GetOnClientConnectingPayloadFunc = GetClientConnectingPayload;
            m_networkLayer.CanMigrateAsHostFunc = CanMigrateAsHost;
        }

        /// <summary>
        /// 释放资源
        /// 取消注册所有网络层回调事件，防止内存泄漏
        /// </summary>
        public void Dispose()
        {
            m_networkLayer.OnClientConnectedCallback -= OnClientConnected;
            m_networkLayer.OnClientDisconnectedCallback -= OnClientDisconnected;
            m_networkLayer.OnMasterClientSwitchedCallback -= OnMasterClientSwitched;
            m_networkLayer.StartLobbyCallback -= OnLobbyStarted;
            m_networkLayer.StartHostCallback -= OnHostStarted;
            m_networkLayer.StartClientCallback -= OnClientStarted;
            m_networkLayer.RestoreHostCallback -= OnHostRestored;
            m_networkLayer.RestoreClientCallback -= OnClientRestored;
            m_networkLayer.OnRestoreFailedCallback -= OnRestoreFailed;
        }

        /// <summary>
        /// 启动协程的辅助方法
        /// 使用协程运行器来启动指定的协程
        /// </summary>
        /// <param name="routine">要启动的协程</param>
        /// <returns>启动的协程实例</returns>
        private Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_coroutineRunner.StartCoroutine(routine);
        }

        /// <summary>
        /// 启动语音系统
        /// 为指定的变换组件启动VoIP语音通信
        /// </summary>
        /// <param name="transform">玩家的变换组件</param>
        private void StartVoip(Transform transform)
        {
            m_voip.StartVoip(transform);
        }

        /// <summary>
        /// 生成网络会话
        /// 创建新的网络会话并设置语音房间，然后在网络中生成
        /// </summary>
        private void SpawnSession()
        {
            // 使用工厂函数创建会话实例
            m_session = m_createSessionFunc.Invoke();

            // 将区域信息附加到大厅ID以确保语音房间的唯一性
            // 因为我们只使用一个区域进行语音通信
            var lobbyId = m_playerPresenceHandler.GroupPresenceState.LobbySessionID;
            m_session.SetPhotonVoiceRoom($"{m_networkLayer.GetRegion()}-{lobbyId}");

            // 在网络中生成会话对象
            m_session.GetComponent<NetworkObject>().Spawn();
        }

        #region Network Layer Callbacks

        /// <summary>
        /// 客户端连接回调
        /// 当有客户端连接到网络时调用
        /// 处理主机和客户端的不同逻辑
        /// </summary>
        /// <param name="clientId">连接的客户端ID</param>
        private void OnClientConnected(ulong clientId)
        {
            // 启动连接处理协程
            _ = StartCoroutine(Impl());

            // 获取当前区域的竞技场目标API
            var destinationAPI = m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion());

            // 生成新的群组存在状态
            _ = StartCoroutine(
                m_playerPresenceHandler.GenerateNewGroupPresence(destinationAPI, m_networkLayer.CurrentRoom));

            /// <summary>
            /// 内部协程：处理客户端连接的具体逻辑
            /// 根据是主机还是客户端执行不同的处理流程
            /// </summary>
            IEnumerator Impl()
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    // 主机逻辑：等待会话创建完成
                    yield return new WaitUntil(() => m_session != null);

                    // 确定备用主机
                    m_session.DetermineFallbackHost(clientId);

                    // 向客户端更新语音房间信息
                    m_session.UpdatePhotonVoiceRoomToClient(clientId);
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    // 客户端逻辑：查找已存在的会话
                    m_session = Object.FindObjectOfType<NetworkSession>();

                    // 确定玩家生成位置
                    var playerPos = m_networkLayer.CurrentClientState == NetworkLayer.ClientState.RestoringClient
                        ? m_localPlayerState.transform.position  // 恢复客户端时使用保存的位置
                        : Vector3.zero;                          // 新连接时使用默认位置

                    // 请求服务器生成玩家
                    SpawningManagerBase.Instance.RequestSpawnServerRpc(
                        clientId, m_localPlayerState.PlayerUid, IsSpectator, playerPos);
                }
            }
        }

        /// <summary>
        /// 客户端断开连接回调
        /// 当客户端断开连接时调用，重新确定备用主机
        /// </summary>
        /// <param name="clientId">断开连接的客户端ID</param>
        private void OnClientDisconnected(ulong clientId)
        {
            if (m_session)
            {
                // 重新确定备用主机
                m_session.RedetermineFallbackHost(clientId);
            }
        }

        /// <summary>
        /// 主机切换回调
        /// 当Photon主机发生切换时调用，返回备用主机ID
        /// </summary>
        /// <returns>备用主机的客户端ID</returns>
        private static ulong OnMasterClientSwitched()
        {
            return NetworkSession.FallbackHostId;
        }

        /// <summary>
        /// 大厅启动回调
        /// 当连接到大厅时调用，加载主菜单场景
        /// </summary>
        private void OnLobbyStarted()
        {
            Debug.Log("OnLobbyStarted");
            m_navigationController.LoadMainMenu();
        }

        /// <summary>
        /// 主机启动回调
        /// 当作为主机启动时调用，加载竞技场并生成玩家
        /// </summary>
        private void OnHostStarted()
        {
            Debug.Log("OnHostStarted");

            // 加载竞技场场景
            m_navigationController.LoadArena();

            // 启动主机初始化协程
            _ = StartCoroutine(Impl());

            /// <summary>
            /// 内部协程：主机启动的具体实现
            /// 等待场景加载，生成会话，创建玩家，启动语音
            /// </summary>
            IEnumerator Impl()
            {
                // 等待场景加载完成
                yield return new WaitUntil(() => m_navigationController.IsSceneLoaded());

                // 生成网络会话
                SpawnSession();

                // 生成本地玩家
                var player = SpawningManagerBase.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId,
                    m_localPlayerState.PlayerUid, false, Vector3.zero);

                // 启动语音系统
                StartVoip(player.transform);
            }
        }

        /// <summary>
        /// 客户端启动回调
        /// 当作为客户端启动时调用，获取本地玩家并启动语音
        /// </summary>
        private void OnClientStarted()
        {
            // 获取本地玩家对象
            var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            // 启动语音系统
            StartVoip(player.transform);
        }

        /// <summary>
        /// 主机恢复回调
        /// 当主机恢复连接时调用，重新生成会话和玩家
        /// </summary>
        private void OnHostRestored()
        {
            // 重新生成会话
            SpawnSession();

            // 在保存的位置重新生成玩家
            var player = SpawningManagerBase.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId,
                m_localPlayerState.PlayerUid, false, m_localPlayerState.transform.position);

            // 重新启动语音系统
            StartVoip(player.transform);
        }

        /// <summary>
        /// 客户端恢复回调
        /// 当客户端恢复连接时调用，重新获取玩家并启动语音
        /// </summary>
        private void OnClientRestored()
        {
            // 重新获取本地玩家对象
            var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            // 重新启动语音系统
            StartVoip(player.transform);
        }

        /// <summary>
        /// 恢复失败回调
        /// 当网络恢复失败时调用，返回主菜单并显示错误状态
        /// </summary>
        /// <param name="failureCode">失败代码</param>
        private void OnRestoreFailed(int failureCode)
        {
            // 将失败代码转换为连接状态并返回主菜单
            m_navigationController.GoToMainMenu((ArenaApprovalController.ConnectionStatus)failureCode);
        }

        /// <summary>
        /// 获取客户端连接负载数据
        /// 创建包含玩家连接信息的JSON负载
        /// </summary>
        /// <returns>JSON格式的连接负载字符串</returns>
        private string GetClientConnectingPayload()
        {
            return JsonUtility.ToJson(new ArenaApprovalController.ConnectionPayload()
            {
                IsPlayer = !IsSpectator,  // 如果不是观众则为玩家
            });
        }

        /// <summary>
        /// 检查是否可以作为主机进行迁移
        /// 只有非观众玩家才能成为主机
        /// </summary>
        /// <returns>如果可以作为主机返回true，否则返回false</returns>
        private bool CanMigrateAsHost()
        {
            return !IsSpectator;
        }

        #endregion // 网络层回调方法
    }
}