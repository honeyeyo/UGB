// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using System.Threading.Tasks;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Platform;
using UnityEngine;
using PongHub.Core;
using PongHub.Gameplay;
using PongHub.UI;
using PongHub.Networking;
using PongHub.Gameplay.Table;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using Oculus.Platform.Models;

namespace PongHub.App
{
    /// <summary>
    /// PongHub应用的入口点
    /// 在启动时初始化应用程序核心,加载控制器和处理器
    /// 这个单例类还暴露了可在整个应用程序中使用的控制器和处理器
    /// 初始化Oculus平台,在登录时获取玩家状态并处理加入意图
    /// </summary>
    public class PHApplication : Singleton<PHApplication>
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

        [Header("UI系统")]
        [SerializeField] private ScoreboardPanel m_scoreboardPanel;
        [SerializeField] private MainMenuPanel m_mainMenuPanel;
        [SerializeField] private SettingsPanel m_settingsPanel;
        [SerializeField] private PauseMenuPanel m_pauseMenuPanel;

        [Header("游戏系统")]
        [SerializeField] private Table m_table;
        [SerializeField] private BallPhysics m_ball;
        [SerializeField] private Paddle m_leftPaddle;
        [SerializeField] private Paddle m_rightPaddle;

        private bool m_isInitialized = false;

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

            // 清理资源
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Cleanup();
            }

            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.Cleanup();
            }

            if (PongHub.Networking.NetworkManager.Instance != null)
            {
                PongHub.Networking.NetworkManager.Instance.Shutdown();
            }

            if (GameCore.Instance != null)
            {
                GameCore.Instance.Cleanup();
            }
        }

        /// <summary>
        /// 启动时初始化应用程序
        /// </summary>
        private async void Start()
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (m_isInitialized)
                return;

            // 初始化核心系统
            await InitializeCoreSystems();

            // 初始化UI系统
            await InitializeUISystems();

            // 初始化游戏系统
            await InitializeGameSystems();

            m_isInitialized = true;
            Debug.Log("PHApplication初始化完成");
        }

        private async Task InitializeCoreSystems()
        {
            // 初始化音频管理器
            if (AudioManager.Instance != null)
            {
                await AudioManager.Instance.InitializeAsync();
            }

            // 初始化振动管理器
            if (VibrationManager.Instance != null)
            {
                await VibrationManager.Instance.InitializeAsync();
            }

            // 初始化网络管理器
            if (PongHub.Networking.NetworkManager.Instance != null)
            {
                await PongHub.Networking.NetworkManager.Instance.InitializeAsync();
            }

            // 初始化游戏核心
            if (GameCore.Instance != null)
            {
                await GameCore.Instance.InitializeAsync();
            }
        }

        private async Task InitializeUISystems()
        {
            // 使用别名访问我们的 UIManager
            if (PongHub.UI.UIManager.Instance != null)
            {
                await PongHub.UI.UIManager.Instance.InitializeAsync();
            }

            // 初始化记分牌面板
            if (m_scoreboardPanel != null)
            {
                await m_scoreboardPanel.InitializeAsync();
            }

            // 初始化主菜单面板
            if (m_mainMenuPanel != null)
            {
                await m_mainMenuPanel.InitializeAsync();
            }

            // 初始化设置面板
            if (m_settingsPanel != null)
            {
                await m_settingsPanel.InitializeAsync();
            }

            // 初始化暂停菜单面板
            if (m_pauseMenuPanel != null)
            {
                await m_pauseMenuPanel.InitializeAsync();
            }
        }

        private async Task InitializeGameSystems()
        {
            // 初始化球桌
            if (m_table != null)
            {
                await m_table.InitializeAsync();
            }

            // 初始化球
            if (m_ball != null)
            {
                await m_ball.InitializeAsync();
            }

            // 初始化球拍
            if (m_leftPaddle != null)
            {
                await m_leftPaddle.InitializeAsync();
            }

            if (m_rightPaddle != null)
            {
                await m_rightPaddle.InitializeAsync();
            }
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
                var coreInit = await Oculus.Platform.Core.AsyncInitialize().Gen();
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

                GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);
                GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);

                var getLoggedInuser = await Users.GetLoggedInUser().Gen();
                if (getLoggedInuser.IsError)
                {
                    LogError("Cannot get user info", getLoggedInuser.GetError());
                    return;
                }

                // Workaround.
                // At the moment, Platform.Users.GetLoggedInUser() seems to only be returning the user ID.
                // Display name is blank.
                // Platform.Users.Get(ulong userID) returns the display name.
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
            Debug.LogError($"ERROR MESSAGE: {error.Message}");
            Debug.LogError($"ERROR CODE: {error.Code}");
            Debug.LogError($"ERROR HTTP CODE: {error.HttpCode}");
        }

        private NetworkSession InstantiateSession()
        {
            return Instantiate(m_sessionPrefab);
        }
    }
}