// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using System.Threading.Tasks;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Platform;
using UnityEngine;

namespace UltimateGloveBall.App
{
    /// <summary>
    /// Ultimate Glove Ball应用程序的入口点
    /// 在启动时初始化应用程序核心,加载控制器和处理器
    /// 这个单例类还暴露了可在整个应用程序中使用的控制器和处理器
    /// 初始化Oculus平台,在登录时获取玩家状态并处理加入意图
    /// </summary>
    public class UGBApplication : Singleton<UGBApplication>
    {
        /// <summary>
        /// 网络层组件
        /// </summary>
        public NetworkLayer NetworkLayer;

        /// <summary>
        /// 语音控制器
        /// </summary>
        public VoipController Voip;

        /// <summary>
        /// 网络会话预制体
        /// </summary>
        [SerializeField] private NetworkSession m_sessionPrefab;

        /// <summary>
        /// 启动类型
        /// </summary>
        private LaunchType m_launchType;

        /// <summary>
        /// 本地玩家状态实例
        /// </summary>
        private LocalPlayerState LocalPlayerState => LocalPlayerState.Instance;

        /// <summary>
        /// 导航控制器
        /// </summary>
        public NavigationController NavigationController { get; private set; }

        /// <summary>
        /// 玩家存在处理器
        /// </summary>
        public PlayerPresenceHandler PlayerPresenceHandler { get; private set; }

        /// <summary>
        /// 网络状态处理器
        /// </summary>
        public NetworkStateHandler NetworkStateHandler { get; private set; }

        /// <summary>
        /// 内部Awake方法,确保对象在场景切换时不被销毁
        /// </summary>
        protected override void InternalAwake()
        {
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// 销毁时释放网络状态处理器
        /// </summary>
        private void OnDestroy()
        {
            NetworkStateHandler?.Dispose();
        }

        /// <summary>
        /// 启动时初始化应用程序
        /// </summary>
        private void Start()
        {
            if (UnityEngine.Application.isEditor)
            {
                if (NetworkSettings.Autostart)
                {
                    LocalPlayerState.SetApplicationID(
                        NetworkSettings.UseDeviceRoom ? SystemInfo.deviceUniqueIdentifier : NetworkSettings.RoomName);
                }
            }

            _ = StartCoroutine(Init());
        }

        /// <summary>
        /// 初始化协程
        /// 初始化Oculus模块、玩家存在处理器、导航控制器和网络状态处理器
        /// 获取产品信息和购买记录
        /// 设置群组存在状态并初始化网络层
        /// </summary>
        private IEnumerator Init()
        {
            _ = InitializeOculusModules();

            // 初始化玩家存在处理器
            PlayerPresenceHandler = new PlayerPresenceHandler();
            yield return PlayerPresenceHandler.Init();
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            yield return new WaitUntil(() => !string.IsNullOrWhiteSpace(LocalPlayerState.Username));
#else
            m_launchType = LaunchType.Normal;
#endif
            _ = BlockUserManager.Instance.Initialize();
            NavigationController =
                new NavigationController(this, NetworkLayer, LocalPlayerState, PlayerPresenceHandler);
            NetworkStateHandler = new NetworkStateHandler(this, NetworkLayer, NavigationController, Voip,
                LocalPlayerState, PlayerPresenceHandler, InstantiateSession);

            // 获取当前登录用户的产品和购买记录
            // 获取所有图标产品
            IAPManager.Instance.FetchProducts(UserIconManager.Instance.AllSkus, ProductCategories.ICONS);
            // 获取猫消耗品
            IAPManager.Instance.FetchProducts(new[] { ProductCategories.CAT }, ProductCategories.CONSUMABLES);
            IAPManager.Instance.FetchPurchases();

            if (m_launchType == LaunchType.Normal)
            {
                if (LocalPlayerState.HasCustomAppId)
                {
                    StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence(
                        "Arena",
                        $"{LocalPlayerState.ApplicationID}"));
                }
                else
                {
                    StartCoroutine(
                        PlayerPresenceHandler.GenerateNewGroupPresence(
                            "MainMenu")
                    );
                }
            }

            yield return new WaitUntil(() => PlayerPresenceHandler.GroupPresenceState is { Destination: { } });

            NetworkLayer.Init(
                PlayerPresenceHandler.GroupPresenceState.LobbySessionID,
                PlayerPresenceHandler.GetRegionFromDestination(PlayerPresenceHandler.GroupPresenceState.Destination));
        }

        /// <summary>
        /// 初始化Oculus模块
        /// 初始化Oculus平台SDK,检查用户权限,设置回调函数,获取用户信息
        /// </summary>
        private async Task InitializeOculusModules()
        {
            try
            {
                var coreInit = await Core.AsyncInitialize().Gen();
                if (coreInit.IsError)
                {
                    LogError("Failed to initialize Oculus Platform SDK", coreInit.GetError());
                    return;
                }

                Debug.Log("Oculus Platform SDK initialized successfully");

                var isUserEntitled = await Entitlements.IsUserEntitledToApplication().Gen();
                if (isUserEntitled.IsError)
                {
                    LogError("You are not entitled to use this app", isUserEntitled.GetError());
                    return;
                }

                m_launchType = ApplicationLifecycle.GetLaunchDetails().LaunchType;

                GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);//设置加入意图接收回调
                GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);//设置邀请发送回调

                var getLoggedInuser = await Users.GetLoggedInUser().Gen();
                if (getLoggedInuser.IsError)
                {
                    LogError("Cannot get user info", getLoggedInuser.GetError());
                    return;
                }

                // 临时解决方案
                // 目前Platform.Users.GetLoggedInUser()似乎只返回用户ID
                // 显示名称为空
                // Platform.Users.Get(ulong userID)返回显示名称
                var getUser = await Users.Get(getLoggedInuser.Data.ID).Gen();
                LocalPlayerState.Init(getUser.Data.DisplayName, getUser.Data.ID);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 处理加入意图接收回调
        /// 处理用户通过应用内直接邀请或深度链接启动应用时的加入意图
        /// </summary>
        private void OnJoinIntentReceived(Message<Oculus.Platform.Models.GroupPresenceJoinIntent> message)
        {
            Debug.Log("------JOIN INTENT RECEIVED------");
            Debug.Log("Destination:       " + message.Data.DestinationApiName);
            Debug.Log("Lobby Session ID:  " + message.Data.LobbySessionId);
            Debug.Log("Match Session ID:  " + message.Data.MatchSessionId);
            Debug.Log("Deep Link Message: " + message.Data.DeeplinkMessage);
            Debug.Log("--------------------------------");

            var messageLobbySessionId = message.Data.LobbySessionId;

            // 还没有群组存在状态:
            // 应用正在通过这个加入意图启动,要么
            // 通过应用内直接邀请,要么通过深度链接
            if (PlayerPresenceHandler.GroupPresenceState == null)
            {
                var lobbySessionID = message.Data.DestinationApiName.StartsWith("Arena") && !string.IsNullOrEmpty(messageLobbySessionId)
                    ? messageLobbySessionId
                    : "Arena-" + LocalPlayerState.ApplicationID;

                _ = StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence(
                    message.Data.DestinationApiName,
                    lobbySessionID));
            }
            // 游戏已经在运行,意味着用户已经有群组存在状态,并且
            // 已经是另一个主机的客户端或主机
            else
            {
                NavigationController.SwitchRoomFromInvite(
                    message.Data.DestinationApiName, messageLobbySessionId, false, false);
            }
        }

        /// <summary>
        /// 处理邀请发送回调
        /// 记录被邀请用户的信息
        /// </summary>
        private void OnInvitationsSent(Message<Oculus.Platform.Models.LaunchInvitePanelFlowResult> message)
        {
            Debug.Log("-------INVITED USERS LIST-------");
            Debug.Log("Size: " + message.Data.InvitedUsers.Count);
            foreach (var user in message.Data.InvitedUsers)
            {
                Debug.Log("Username: " + user.DisplayName);
                Debug.Log("User ID:  " + user.ID);
            }

            Debug.Log("--------------------------------");
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError(message);
            Debug.LogError("ERROR MESSAGE:   " + error.Message);
            Debug.LogError("ERROR CODE:      " + error.Code);
            Debug.LogError("ERROR HTTP CODE: " + error.HttpCode);
        }

        private NetworkSession InstantiateSession()
        {
            return Instantiate(m_sessionPrefab);
        }
    }
}