// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using System.Threading.Tasks;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using Oculus.Platform;
using UnityEngine;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Gameplay;
using PongHub.UI;
using PongHub.Networking;
using PongHub.Gameplay.Table;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using PongHub.Utils;
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
        [SerializeField]
        [Tooltip("Session Prefab / 会话预制体 - Network session prefab for multiplayer functionality")]
        private NetworkSession m_sessionPrefab;

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

        [Header("运行时状态 (只读)")]
        [SerializeField, ReadOnly]
        [Tooltip("Navigation Controller Initialized / 导航控制器已初始化 - Runtime status indicator")]
        private bool m_navigationControllerInitialized;

        [SerializeField, ReadOnly]
        [Tooltip("Player Presence Handler Initialized / 玩家存在处理器已初始化 - Runtime status indicator")]
        private bool m_playerPresenceHandlerInitialized;

        [SerializeField, ReadOnly]
        [Tooltip("Network State Handler Initialized / 网络状态处理器已初始化 - Runtime status indicator")]
        private bool m_networkStateHandlerInitialized;

        [Header("UI系统")]
        [SerializeField]
        [Tooltip("Scoreboard Panel / 记分板面板 - UI component for displaying game scores")]
        private ScoreboardPanel m_scoreboardPanel;

        [SerializeField]
        [Tooltip("Main Menu Panel / 主菜单面板 - UI component for main menu interface")]
        private MainMenuPanel m_mainMenuPanel;

        [SerializeField]
        [Tooltip("Settings Panel / 设置面板 - UI component for game settings")]
        private SettingsPanel m_settingsPanel;

        [SerializeField]
        [Tooltip("Pause Menu Panel / 暂停菜单面板 - UI component for pause menu")]
        private PauseMenuPanel m_pauseMenuPanel;

        [Header("游戏系统")]
        [SerializeField]
        [Tooltip("Table / 球桌 - Ping pong table component")]
        private Table m_table;

        [SerializeField]
        [Tooltip("Ball Physics / 球物理 - Ball physics component for gameplay")]
        private BallPhysics m_ball;

        [SerializeField]
        [Tooltip("Left Paddle / 左球拍 - Left player's paddle component")]
        private Paddle m_leftPaddle;

        [SerializeField]
        [Tooltip("Right Paddle / 右球拍 - Right player's paddle component")]
        private Paddle m_rightPaddle;

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
            Debug.Log("=== PHApplication.Start() 开始 ===");

            try
            {
                // 第一步：启动原始的Oculus初始化协程
                Debug.Log("启动原始InitOculusAndNetwork()协程 - Oculus平台初始化...");
                StartCoroutine(InitOculusAndNetwork());

                // 第二步：并行执行游戏系统初始化
                Debug.Log("开始游戏系统初始化...");
                await InitializeAsync();

                Debug.Log("=== PHApplication.Start() 完成 ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PHApplication.Start() 发生异常: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 重命名并保持原始的Oculus初始化流程
        /// </summary>
        private IEnumerator InitOculusAndNetwork()
        {
            Debug.Log("=== InitOculusAndNetwork() 协程开始 ===");

            // 步骤1: 初始化Oculus模块
            Debug.Log("步骤1: 初始化Oculus模块...");
            _ = InitializeOculusModules();

            // 步骤2: 初始化PlayerPresenceHandler
            Debug.Log("步骤2: 初始化PlayerPresenceHandler...");
            PlayerPresenceHandler = new PlayerPresenceHandler();

            // 将yield语句移出try-catch块
            var presenceInitCoroutine = PlayerPresenceHandler.Init();
            if (presenceInitCoroutine != null)
            {
                yield return presenceInitCoroutine;
                m_playerPresenceHandlerInitialized = PlayerPresenceHandler != null;
                Debug.Log("PlayerPresenceHandler初始化完成");
            }
            else
            {
                Debug.LogError("PlayerPresenceHandler.Init() 返回null");
                yield break;
            }

            // 步骤3: 等待用户名
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            Debug.Log("步骤3: 等待获取用户名...");

            // 将yield语句移出try-catch块
            var waitForUsername = new WaitUntil(() => !string.IsNullOrWhiteSpace(LocalPlayerState.Username));
            yield return waitForUsername;

            if (string.IsNullOrWhiteSpace(LocalPlayerState.Username))
            {
                Debug.LogError("无法获取用户名");
                yield break;
            }
            Debug.Log($"获取到用户名: {LocalPlayerState.Username}");
#else
            Debug.Log("Editor模式，设置启动类型为Normal");
            m_launchType = LaunchType.Normal;
#endif

            // 步骤4-7: 不需要yield的初始化
            if (!InitializeManagers())
            {
                Debug.LogError("管理器初始化失败");
                yield break;
            }

            // 步骤8: 处理启动类型
            if (!HandleLaunchType())
            {
                Debug.LogError("启动类型处理失败");
                yield break;
            }

            // 步骤9: 等待群组存在状态
            Debug.Log("步骤9: 等待群组存在状态...");
            var waitForGroupPresence = new WaitUntil(() => PlayerPresenceHandler.GroupPresenceState is { Destination: { } });
            yield return waitForGroupPresence;

            if (PlayerPresenceHandler.GroupPresenceState?.Destination == null)
            {
                Debug.LogError("群组存在状态获取失败");
                yield break;
            }
            Debug.Log($"群组存在状态就绪: {PlayerPresenceHandler.GroupPresenceState.Destination}");

            // 步骤10: 初始化网络层
            if (!InitializeNetworkLayer())
            {
                Debug.LogError("网络层初始化失败");
                yield break;
            }

            Debug.Log("=== InitOculusAndNetwork() 协程完成 ===");
        }

        /// <summary>
        /// 初始化管理器（不需要yield的部分）
        /// </summary>
        private bool InitializeManagers()
        {
            try
            {
                Debug.Log("步骤4: 初始化BlockUserManager...");
                _ = BlockUserManager.Instance.Initialize();

                // 验证必要的组件是否已分配
                if (NetworkLayer == null)
                {
                    Debug.LogError("❌ NetworkLayer字段为null！请在Unity Editor的PHApplication组件中分配NetworkLayer引用");
                    Debug.LogError("提示：在Hierarchy中找到PHApplication GameObject，在Inspector中将NetworkLayer字段拖拽分配");
                    return false;
                }

                if (Voip == null)
                {
                    Debug.LogError("❌ Voip字段为null！请在Unity Editor的PHApplication组件中分配Voip引用");
                    Debug.LogError("提示：在Hierarchy中找到PHApplication GameObject，在Inspector中将Voip字段拖拽分配");
                    return false;
                }

                Debug.Log("✓ NetworkLayer和Voip组件验证通过");

                Debug.Log("步骤5: 创建NavigationController...");
                NavigationController = new NavigationController(this, NetworkLayer, LocalPlayerState, PlayerPresenceHandler);
                m_navigationControllerInitialized = NavigationController != null;

                Debug.Log("步骤6: 创建NetworkStateHandler...");
                NetworkStateHandler = new NetworkStateHandler(this, NetworkLayer, NavigationController, Voip,
                    LocalPlayerState, PlayerPresenceHandler, InstantiateSession);
                m_networkStateHandlerInitialized = NetworkStateHandler != null;

                Debug.Log("步骤7: 获取IAP产品信息...");

                // 检查 UserIconManager 是否已初始化
                if (UserIconManager.Instance != null && UserIconManager.Instance.AllSkus != null)
                {
                    IAPManager.Instance.FetchProducts(UserIconManager.Instance.AllSkus, ProductCategories.ICONS);
                }
                else
                {
                    Debug.LogWarning("UserIconManager.Instance 或 AllSkus 为 null，跳过图标产品获取");
                    // 在开发模式下使用默认SKU
                    if (DevelopmentConfig.IsDevelopmentBuild)
                    {
                        var defaultIconSkus = new[] { "icon_1", "icon_2", "icon_3" };
                        IAPManager.Instance.FetchProducts(defaultIconSkus, ProductCategories.ICONS);
                    }
                }

                IAPManager.Instance.FetchProducts(new[] { ProductCategories.CAT }, ProductCategories.CONSUMABLES);
                IAPManager.Instance.FetchPurchases();

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InitializeManagers() 发生异常: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// 处理启动类型
        /// </summary>
        private bool HandleLaunchType()
        {
            try
            {
                Debug.Log($"步骤8: 处理启动类型 - {m_launchType}");
                if (m_launchType == LaunchType.Normal)
                {
                    if (LocalPlayerState.HasCustomAppId)
                    {
                        Debug.Log($"生成群组存在状态 - Arena: {LocalPlayerState.ApplicationID}");
                        StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence(
                            "Arena",
                            $"{LocalPlayerState.ApplicationID}"));
                    }
                    else
                    {
                        Debug.Log("生成群组存在状态 - MainMenu");
                        StartCoroutine(PlayerPresenceHandler.GenerateNewGroupPresence("MainMenu"));
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"HandleLaunchType() 发生异常: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// 初始化网络层
        /// </summary>
        private bool InitializeNetworkLayer()
        {
            try
            {
                Debug.Log("步骤10: 初始化网络层...");
                NetworkLayer.Init(
                    PlayerPresenceHandler.GroupPresenceState.LobbySessionID,
                    PlayerPresenceHandler.GetRegionFromDestination(PlayerPresenceHandler.GroupPresenceState.Destination));
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InitializeNetworkLayer() 发生异常: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// 保持原有的 InitializeAsync() 方法不变，但添加更多日志
        /// </summary>
        private async Task InitializeAsync()
        {
            Debug.Log("=== InitializeAsync() 开始 ===");

            if (m_isInitialized)
            {
                Debug.Log("已经初始化过，跳过");
                return;
            }

            try
            {
                // 等待一下确保Oculus初始化开始
                await Task.Delay(100);

                Debug.Log("开始初始化核心系统...");
                await InitializeCoreSystems();

                Debug.Log("开始初始化UI系统...");
                await InitializeUISystems();

                Debug.Log("开始初始化游戏系统...");
                await InitializeGameSystems();

                m_isInitialized = true;
                Debug.Log("=== InitializeAsync() 完成 ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InitializeAsync() 发生异常: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private async Task InitializeCoreSystems()
        {
            Debug.Log("=== InitializeCoreSystems() 开始 ===");

            try
            {
                // 初始化音频管理器
                if (AudioManager.Instance != null)
                {
                    Debug.Log("初始化 AudioManager...");
                    await AudioManager.Instance.InitializeAsync();
                    Debug.Log("AudioManager 初始化完成");
                }
                else
                {
                    Debug.LogWarning("AudioManager.Instance 为 null");
                }

                // 初始化振动管理器
                if (VibrationManager.Instance != null)
                {
                    Debug.Log("初始化 VibrationManager...");
                    await VibrationManager.Instance.InitializeAsync();
                    Debug.Log("VibrationManager 初始化完成");
                }
                else
                {
                    Debug.LogWarning("VibrationManager.Instance 为 null");
                }

                // 初始化网络管理器
                if (PongHub.Networking.NetworkManager.Instance != null)
                {
                    Debug.Log("初始化 NetworkManager...");
                    await PongHub.Networking.NetworkManager.Instance.InitializeAsync();
                    Debug.Log("NetworkManager 初始化完成");
                }
                else
                {
                    Debug.LogWarning("NetworkManager.Instance 为 null");
                }

                // 初始化游戏核心
                if (GameCore.Instance != null)
                {
                    Debug.Log("初始化 GameCore...");
                    await GameCore.Instance.InitializeAsync();
                    Debug.Log("GameCore 初始化完成");
                }
                else
                {
                    Debug.LogWarning("GameCore.Instance 为 null");
                }

                Debug.Log("=== InitializeCoreSystems() 完成 ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InitializeCoreSystems() 发生异常: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }

        private async Task InitializeUISystems()
        {
            // 使用新的菜单系统替代过时的UIManager
            MenuCanvasController menuCanvasController = FindObjectOfType<MenuCanvasController>();
            TableMenuSystem tableMenuSystem = FindObjectOfType<TableMenuSystem>();

            if (menuCanvasController != null)
            {
                await menuCanvasController.InitializeAsync();
            }

            if (tableMenuSystem != null)
            {
                await tableMenuSystem.InitializeAsync();
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
        /// 改进 InitializeOculusModules，添加开发模式支持和更详细的错误处理
        /// </summary>
        private async Task InitializeOculusModules()
        {
            Debug.Log("=== InitializeOculusModules() 开始 ===");

            try
            {
                // 检查是否在开发环境中运行
                DevelopmentConfig.LogDevelopmentMode($"开发环境检测: {(DevelopmentConfig.IsDevelopmentBuild ? "是" : "否")}");

                Debug.Log("正在初始化Oculus Platform SDK...");
                var coreInit = await Oculus.Platform.Core.AsyncInitialize().Gen();
                if (coreInit.IsError)
                {
                    var error = coreInit.GetError();
                    LogError("Oculus Platform SDK初始化失败", error);

                    // 在开发环境中，某些错误可以忽略
                    if (DevelopmentConfig.ShouldIgnoreOculusError(error.Code))
                    {
                        DevelopmentConfig.LogDevelopmentWarning("忽略Platform SDK初始化错误，继续运行...");
                        InitializeDevelopmentMode();
                        return;
                    }

                    Debug.LogError("这可能导致应用卡在开屏！");
                    return;
                }
                Debug.Log("✓ Oculus Platform SDK初始化成功");

                Debug.Log("正在检查用户权限...");
                var isUserEntitled = await Entitlements.IsUserEntitledToApplication().Gen();
                if (isUserEntitled.IsError)
                {
                    var error = isUserEntitled.GetError();
                    LogError("用户权限验证失败", error);

                    // 在开发环境中，权限验证失败可以跳过
                    if (DevelopmentConfig.SkipOculusEntitlementCheck)
                    {
                        DevelopmentConfig.LogDevelopmentWarning("跳过权限验证，使用模拟用户数据...");
                        InitializeDevelopmentMode();
                        return;
                    }

                    Debug.LogError("这可能导致应用无法继续！");
                    return;
                }
                Debug.Log("✓ 用户权限验证通过");

                // 正常初始化流程
                await InitializeOculusNormalMode();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"InitializeOculusModules() 发生严重异常: {exception.Message}");
                DevelopmentConfig.LogDevelopmentMode("尝试在开发模式下初始化...");
                Debug.LogException(exception);

                // 异常情况下尝试开发模式
                if (DevelopmentConfig.IsDevelopmentBuild)
                {
                    InitializeDevelopmentMode();
                }
            }
        }

        /// <summary>
        /// 开发模式初始化
        /// </summary>
        private void InitializeDevelopmentMode()
        {
            DevelopmentConfig.LogDevelopmentMode("=== 开发模式初始化 ===");

            try
            {
                // 模拟启动类型
                m_launchType = LaunchType.Normal;
                DevelopmentConfig.LogDevelopmentMode($"✓ 模拟启动类型: {m_launchType}");

                // 在开发模式下，我们跳过大部分Oculus特定的回调
                DevelopmentConfig.LogDevelopmentMode("跳过群组存在回调设置");

                // 确保LocalPlayerState实例存在
                if (LocalPlayerState == null)
                {
                    DevelopmentConfig.LogDevelopmentWarning("LocalPlayerState.Instance 为 null，尝试查找场景中的实例");
                    var localPlayerState = FindObjectOfType<LocalPlayerState>();
                    if (localPlayerState == null)
                    {
                        DevelopmentConfig.LogDevelopmentError("场景中未找到LocalPlayerState组件");
                        return;
                    }
                }

                // 使用DevelopmentConfig中的模拟用户数据初始化
                var devUsername = DevelopmentConfig.DevelopmentUserName;
                var devUserId = DevelopmentConfig.DevelopmentUserId;

                DevelopmentConfig.LogDevelopmentMode($"✓ 开发模式用户ID: {devUserId}");
                DevelopmentConfig.LogDevelopmentMode($"✓ 开发模式用户名: {devUsername}");

                // 初始化LocalPlayerState
                LocalPlayerState.Init(devUsername, devUserId);
                DevelopmentConfig.LogDevelopmentMode("✓ LocalPlayerState 初始化完成");

                DevelopmentConfig.LogDevelopmentMode("=== 开发模式初始化完成 ===");
            }
            catch (System.Exception ex)
            {
                DevelopmentConfig.LogDevelopmentError($"开发模式初始化失败: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 正常模式初始化
        /// </summary>
        private async Task InitializeOculusNormalMode()
        {
            m_launchType = ApplicationLifecycle.GetLaunchDetails().LaunchType;
            Debug.Log($"✓ 启动类型: {m_launchType}");

            Debug.Log("正在设置群组存在回调...");
            GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentReceived);
            GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);
            Debug.Log("✓ 群组存在回调设置完成");

            Debug.Log("正在获取登录用户信息...");
            var getLoggedInuser = await Users.GetLoggedInUser().Gen();
            if (getLoggedInuser.IsError)
            {
                LogError("无法获取用户信息", getLoggedInuser.GetError());
                return;
            }
            Debug.Log($"✓ 用户ID: {getLoggedInuser.Data.ID}");

            Debug.Log("正在获取用户详细信息...");
            var getUser = await Users.Get(getLoggedInuser.Data.ID).Gen();
            if (getUser.IsError)
            {
                LogError("无法获取用户详细信息", getUser.GetError());
                return;
            }

            Debug.Log($"✓ 用户显示名称: {getUser.Data.DisplayName}");
            LocalPlayerState.Init(getUser.Data.DisplayName, getUser.Data.ID);

            Debug.Log("=== InitializeOculusModules() 完成 ===");
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