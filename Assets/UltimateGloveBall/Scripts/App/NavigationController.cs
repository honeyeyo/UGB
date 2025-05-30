// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using Meta.Multiplayer.Core;
using PongHub.Arena.Services;
using PongHub.MainMenu;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// 导航控制器
    /// 处理应用程序中的导航功能
    /// 提供在不同游戏状态之间导航的API：主菜单、游戏、观战模式，或专门加载场景
    /// 基于应用程序状态在不同场景之间进行导航
    /// </summary>
    public class NavigationController
    {
        /// <summary>
        /// 场景加载器
        /// 负责处理场景的加载和卸载操作
        /// </summary>
        private readonly SceneLoader m_sceneLoader = new();

        /// <summary>
        /// 协程运行器
        /// 用于启动和管理协程的MonoBehaviour实例
        /// </summary>
        private MonoBehaviour m_coroutineRunner;

        /// <summary>
        /// 网络层组件
        /// 处理网络连接、房间管理等网络相关功能
        /// </summary>
        private NetworkLayer m_networkLayer;

        /// <summary>
        /// 玩家存在处理器
        /// 管理玩家的在线状态和群组存在信息
        /// </summary>
        private PlayerPresenceHandler m_playerPresenceHandler;

        /// <summary>
        /// 本地玩家状态
        /// 存储当前本地玩家的状态信息
        /// </summary>
        private LocalPlayerState m_localPlayerState;

        /// <summary>
        /// 导航控制器构造函数
        /// 初始化所有必要的组件引用
        /// </summary>
        /// <param name="coroutineRunner">用于运行协程的MonoBehaviour实例</param>
        /// <param name="networkLayer">网络层组件</param>
        /// <param name="localPlayerState">本地玩家状态</param>
        /// <param name="playerPresenceHandler">玩家存在处理器</param>
        public NavigationController(MonoBehaviour coroutineRunner, NetworkLayer networkLayer, LocalPlayerState localPlayerState, PlayerPresenceHandler playerPresenceHandler)
        {
            m_coroutineRunner = coroutineRunner;
            m_networkLayer = networkLayer;
            m_localPlayerState = localPlayerState;
            m_playerPresenceHandler = playerPresenceHandler;
        }

        /// <summary>
        /// 检查场景是否已加载完成
        /// </summary>
        /// <returns>如果场景已加载完成返回true，否则返回false</returns>
        public bool IsSceneLoaded()
        {
            return m_sceneLoader.SceneLoaded;
        }

        /// <summary>
        /// 加载主菜单场景
        /// 直接加载主菜单场景，不使用网络管理器
        /// </summary>
        public void LoadMainMenu()
        {
            m_sceneLoader.LoadScene("MainMenu", false);
        }

        /// <summary>
        /// 加载竞技场场景
        /// 使用网络管理器加载竞技场场景以支持多人游戏
        /// </summary>
        public void LoadArena()
        {
            m_sceneLoader.LoadScene("Arena");
        }

        /// <summary>
        /// 返回主菜单
        /// 离开当前网络会话并返回大厅，然后加载主菜单
        /// 包含淡入淡出效果和连接状态处理
        /// </summary>
        /// <param name="connectionStatus">连接状态，用于显示相应的消息给用户</param>
        public void GoToMainMenu(ArenaApprovalController.ConnectionStatus connectionStatus = ArenaApprovalController.ConnectionStatus.Success)
        {
            // 离开当前网络会话
            m_networkLayer.Leave();

            /// <summary>
            /// 内部协程：离开后返回大厅
            /// 处理屏幕淡出、网络断开、重新连接大厅、场景加载、屏幕淡入的完整流程
            /// </summary>
            IEnumerator GoToLobbyAfterLeaving()
            {
                // 如果屏幕还没有完全淡出，开始淡出
                if (OVRScreenFade.instance.currentAlpha < 1)
                {
                    OVRScreenFade.instance.FadeOut();
                }

                // 等待网络断开且屏幕完全淡出
                yield return new WaitUntil(() => m_networkLayer.CurrentClientState == NetworkLayer.ClientState.Disconnected && OVRScreenFade.instance.currentAlpha >= 1);

                // 连接到大厅
                m_networkLayer.GoToLobby();

                // 等待连接到大厅完成
                yield return new WaitUntil(() => m_networkLayer.CurrentClientState == NetworkLayer.ClientState.ConnectedToLobby);

                // 等待场景加载完成
                yield return new WaitUntil(() => m_sceneLoader.SceneLoaded);

                // 如果屏幕还是淡出状态，开始淡入
                if (OVRScreenFade.instance.currentAlpha >= 1)
                {
                    OVRScreenFade.instance.FadeIn();
                }

                // 生成新的群组存在状态（主菜单）
                yield return GenerateNewGroupPresence("MainMenu");

                // 查找主菜单控制器并通知返回菜单事件
                var menuController = Object.FindObjectOfType<MainMenuController>();
                if (menuController)
                {
                    menuController.OnReturnToMenu(connectionStatus);
                }
            }

            // 启动返回大厅的协程
            _ = StartCoroutine(GoToLobbyAfterLeaving());
        }

        /// <summary>
        /// 导航到比赛
        /// 仅由拥有者本地调用，用于开始新的比赛
        /// </summary>
        /// <param name="isHosting">是否作为主机启动比赛</param>
        public void NavigateToMatch(bool isHosting)
        {
            // 获取当前区域对应的竞技场目标API
            var destinationAPI = m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion());
            var lobbySessionID = m_playerPresenceHandler.GroupPresenceState.LobbySessionID;

            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, lobbySessionID, isHosting));
        }

        /// <summary>
        /// 加入指定的比赛
        /// 作为客户端加入已存在的比赛房间
        /// </summary>
        /// <param name="destinationAPI">目标API字符串</param>
        /// <param name="sessionId">会话ID</param>
        public void JoinMatch(string destinationAPI, string sessionId)
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, sessionId, false));
        }

        /// <summary>
        /// 观看随机比赛
        /// 以观众身份加入随机的比赛房间
        /// </summary>
        public void WatchRandomMatch()
        {
            var destinationAPI = m_playerPresenceHandler.GetArenaDestinationAPI(m_networkLayer.GetRegion());
            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, "", false, true));
        }

        /// <summary>
        /// 观看指定比赛
        /// 以观众身份加入指定的比赛房间
        /// </summary>
        /// <param name="destinationAPI">目标API字符串</param>
        /// <param name="sessionId">会话ID</param>
        public void WatchMatch(string destinationAPI, string sessionId)
        {
            _ = StartCoroutine(SwitchRoomOnPhotonReady(destinationAPI, sessionId, false, true));
        }

        /// <summary>
        /// 从邀请切换房间
        /// 处理通过邀请加入房间的情况
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <param name="lobbySessionID">大厅会话ID</param>
        /// <param name="isHosting">是否作为主机</param>
        /// <param name="isSpectator">是否为观众</param>
        public void SwitchRoomFromInvite(string destination, string lobbySessionID, bool isHosting, bool isSpectator)
        {
            _ = StartCoroutine(SwitchRoom(destination, lobbySessionID, isHosting, isSpectator));
        }

        /// <summary>
        /// 在Photon准备就绪时切换房间
        /// 等待当前帧结束以确保所有系统初始化完成
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="lobbySessionId">大厅会话ID</param>
        /// <param name="isHosting">是否作为主机</param>
        /// <param name="isSpectator">是否为观众</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator SwitchRoomOnPhotonReady(string roomName, string lobbySessionId, bool isHosting, bool isSpectator = false)
        {
            // 等待当前帧结束，确保所有组件都已初始化
            yield return new WaitForEndOfFrame();

            // 开始切换房间
            _ = StartCoroutine(SwitchRoom(roomName, lobbySessionId, isHosting, isSpectator));
        }

        /// <summary>
        /// 切换房间的核心实现
        /// 处理房间切换的完整流程，包括屏幕淡出、群组存在更新、网络房间切换
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <param name="lobbySessionID">大厅会话ID</param>
        /// <param name="isHosting">是否作为主机</param>
        /// <param name="isSpectator">是否为观众</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator SwitchRoom(string destination, string lobbySessionID, bool isHosting, bool isSpectator)
        {
            // 记录房间切换信息
            Debug.Log($"Switching room to {destination} as {(isHosting ? "host" : "client")}{(isSpectator ? " spectator" : "")}");

            // 开始屏幕淡出效果
            OVRScreenFade.instance.FadeOut();

            // 生成新的群组存在状态
            yield return StartCoroutine(GenerateNewGroupPresence(destination, lobbySessionID));

            // 等待屏幕完全淡出
            yield return StartCoroutine(new WaitUntil(() => OVRScreenFade.instance.currentAlpha >= 1));

            // 记录房间切换详细信息
            Debug.LogWarning($"Switching to room {lobbySessionID} in {destination}");

            // 设置本地玩家观众状态
            m_localPlayerState.IsSpectator = isSpectator;

            // 从目标位置获取区域信息并切换Photon房间
            var region = m_playerPresenceHandler.GetRegionFromDestination(destination);
            m_networkLayer.SwitchPhotonRealtimeRoom(lobbySessionID, isHosting, region);
        }

        /// <summary>
        /// 生成新的群组存在状态
        /// 调用玩家存在处理器来更新群组存在信息
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <param name="lobbySessionId">大厅会话ID，可选</param>
        /// <returns>协程枚举器</returns>
        private IEnumerator GenerateNewGroupPresence(string destination, string lobbySessionId = null)
        {
            return m_playerPresenceHandler.GenerateNewGroupPresence(destination, lobbySessionId);
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
    }
}