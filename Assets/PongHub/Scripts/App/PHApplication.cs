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

                Debug.Log("步骤5: 创建NavigationController...");
                NavigationController = new NavigationController(this, NetworkLayer, LocalPlayerState, PlayerPresenceHandler);

                Debug.Log("步骤6: 创建NetworkStateHandler...");
                NetworkStateHandler = new NetworkStateHandler(this, NetworkLayer, NavigationController, Voip,
                    LocalPlayerState, PlayerPresenceHandler, InstantiateSession);

                Debug.Log("步骤7: 获取IAP产品信息...");
                IAPManager.Instance.FetchProducts(UserIconManager.Instance.AllSkus, ProductCategories.ICONS);
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
        /// 改进 InitializeOculusModules，添加更详细的错误处理
        /// </summary>
        private async Task InitializeOculusModules()
        {
            Debug.Log("=== InitializeOculusModules() 开始 ===");

            try
            {
                Debug.Log("正在初始化Oculus Platform SDK...");
                var coreInit = await Oculus.Platform.Core.AsyncInitialize().Gen();
                if (coreInit.IsError)
                {
                    LogError("Oculus Platform SDK初始化失败", coreInit.GetError());
                    Debug.LogError("这可能导致应用卡在开屏！");
                    return;
                }
                Debug.Log("✓ Oculus Platform SDK初始化成功");

                Debug.Log("正在检查用户权限...");
                var isUserEntitled = await Entitlements.IsUserEntitledToApplication().Gen();
                if (isUserEntitled.IsError)
                {
                    LogError("用户权限验证失败", isUserEntitled.GetError());
                    Debug.LogError("这可能导致应用无法继续！");
                    return;
                }
                Debug.Log("✓ 用户权限验证通过");

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
            catch (System.Exception exception)
            {
                Debug.LogError($"InitializeOculusModules() 发生严重异常: {exception.Message}");
                Debug.LogError("这很可能是导致应用卡在开屏的原因！");
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