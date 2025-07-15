# GloveBall游戏流程控制分析与乒乓球VR实现方案

## 📋 概述

本文档分析了原始GloveBall项目的游戏流程控制机制，并为乒乓球VR游戏提供了完整的流程控制实现方案。

## 🔍 GloveBall游戏流程控制分析

### 1. 整体架构设计

GloveBall采用了多层次的状态机架构来管理游戏流程：

#### 1.1 核心状态管理器

```text
游戏流程控制层次:
├── GameManager (游戏阶段管理)
│   ├── PreGame (赛前准备)
│   ├── CountDown (倒计时)
│   ├── InGame (比赛中)
│   └── PostGame (赛后)
├── PongSessionManager (会话状态管理)
│   ├── WaitingForPlayers (等待玩家)
│   ├── ModeSelection (模式选择)
│   ├── TeamBalancing (队伍平衡)
│   ├── ReadyCheck (准备确认)
│   ├── GameStarting (游戏开始)
│   ├── InGame (游戏中)
│   └── PostGame (游戏结束)
└── MatchManager (比赛管理)
    ├── WaitingForPlayers
    ├── Countdown
    ├── Playing
    ├── PointScored
    └── GameOver
```

#### 1.2 状态机流转逻辑

**大厅状态流转 (PongSessionManager)**:

```text
WaitingForPlayers → ModeSelection → TeamBalancing → ReadyCheck → GameStarting → InGame → PostGame
     ↑                                                                                        ↓
     ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
```

**游戏状态流转 (GameManager)**:

```text
PreGame → CountDown → InGame → PostGame
   ↑                            ↓
   ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
```

### 2. 关键组件功能分析

#### 2.1 GameManager.cs 核心功能

```csharp
// 游戏阶段枚举
public enum GamePhase
{
    PreGame,    // 赛前准备 - 玩家选择队伍、等待开始
    CountDown,  // 倒计时 - 4秒倒计时准备
    InGame,     // 比赛中 - 180秒游戏时间
    PostGame,   // 赛后 - 显示结果、选择下一步
}

// 核心时间控制
private const double GAME_START_COUNTDOWN_TIME_SEC = 4;  // 倒计时4秒
private const double GAME_DURATION_SEC = 180;           // 游戏时长3分钟

// 网络同步变量
private NetworkVariable<GamePhase> m_currentGamePhase;   // 当前游戏阶段
private NetworkVariable<double> m_gameStartTime;         // 游戏开始时间
private NetworkVariable<double> m_gameEndTime;           // 游戏结束时间
```

**主要职责**:

- 管理游戏的时间流程（倒计时、游戏时间、结束时间）
- 控制玩家移动权限（只有PreGame和InGame阶段可移动）
- 处理队伍颜色分配和锁定
- 管理PostGame界面显示

#### 2.2 PongSessionManager.cs 核心功能

```csharp
// 大厅状态枚举
public enum GameLobbyState
{
    WaitingForPlayers,      // 等待玩家加入 (需要≥2人)
    ModeSelection,          // 模式选择阶段 (自动或手动)
    TeamBalancing,          // 队伍平衡 (分配队伍)
    ReadyCheck,             // 准备确认 (30秒超时)
    GameStarting,           // 游戏开始
    InGame,                 // 游戏中
    PostGame               // 游戏结束
}

// 游戏模式枚举
public enum PongGameMode
{
    Waiting,        // 等待玩家
    Singles,        // 单打 (1v1)
    Doubles,        // 双打 (2v2)
    Spectator       // 观众模式
}
```

**主要职责**:

- 管理玩家加入/离开逻辑
- 自动模式选择（根据人数确定单打/双打）
- 队伍平衡算法（基于技能评级）
- 准备确认机制（30秒超时保护）
- 房主权限管理

#### 2.3 MatchManager.cs 核心功能

```csharp
public enum MatchState
{
    WaitingForPlayers,  // 等待玩家
    Countdown,          // 倒计时
    Playing,            // 比赛中
    PointScored,        // 得分后
    GameOver            // 比赛结束
}

// 比赛规则
public int RoundsToWin { get; private set; } = 3;  // 默认三局两胜
```

**主要职责**:

- 管理多局比赛逻辑（三局两胜）
- 追踪每局胜负
- 判断整场比赛胜负

### 3. 事件驱动架构

#### 3.1 事件系统设计

```csharp
// PongGameEvents - 全局事件系统
public static class PongGameEvents
{
    // 游戏模式事件
    public static Action<PongGameMode> OnGameModeChanged;
    public static Action<int> OnPlayerCountChanged;

    // 队伍事件
    public static Action<string, NetworkedTeam.Team> OnPlayerTeamAssigned;
    public static Action<string, bool> OnPlayerReadyStateChanged;

    // 大厅事件
    public static Action<GameLobbyState> OnLobbyStateChanged;
    public static Action OnGameStartRequested;
    public static Action<string> OnPlayerJoined;
    public static Action<string> OnPlayerLeft;
}
```

#### 3.2 UI响应机制

```csharp
// PongLobbyUI.cs - UI响应状态变化
private void OnLobbyStateChanged(GameLobbyState newState)
{
    switch (newState)
    {
        case GameLobbyState.WaitingForPlayers:
            ShowWaitingPanel();
            break;
        case GameLobbyState.ModeSelection:
            ShowGameModePanel();
            break;
        case GameLobbyState.TeamBalancing:
            ShowTeamPanel();
            break;
        case GameLobbyState.ReadyCheck:
            ShowReadyPanel();
            break;
        case GameLobbyState.InGame:
            ShowGamePanel();
            break;
    }
}
```

## 🎯 乒乓球VR游戏流程控制实现方案

### 方案一：简化单机版流程控制

#### 1.1 适用场景

- 单机练习模式
- AI对战模式
- 本地多人游戏

#### 1.2 状态机设计

```csharp
public enum GameState
{
    MainMenu,           // 主菜单
    ModeSelection,      // 模式选择 (单机/AI/本地多人)
    GameSetup,          // 游戏设置 (难度/规则)
    Countdown,          // 倒计时准备
    Serving,            // 发球阶段
    Playing,            // 比赛中
    PointScored,        // 得分暂停
    SetComplete,        // 一局结束
    MatchComplete,      // 比赛结束
    Paused              // 暂停 (仅单机模式)
}
```

#### 1.3 实现特点

- **暂停功能**: 仅在单机模式下可用，多人模式不支持暂停
- **简化流程**: 减少网络同步复杂度
- **快速开始**: 无需等待其他玩家

#### 1.4 核心控制器

```csharp
public class GameController : MonoBehaviour
{
    [Header("游戏设置")]
    public int maxScore = 11;           // 每局最高分
    public int setsToWin = 3;           // 获胜所需局数
    public float serveTimeLimit = 30f;   // 发球时间限制

    [Header("AI设置")]
    public PongAIDifficulty aiDifficulty = PongAIDifficulty.Medium;

    private GameState currentState;
    private bool isPaused;
    private bool canPause => IsOfflineMode(); // 只有离线模式可暂停

    // 状态转换逻辑
    public void TransitionToState(GameState newState)
    {
        if (currentState == newState) return;

        ExitState(currentState);
        currentState = newState;
        EnterState(newState);

        OnStateChanged?.Invoke(newState);
    }

    // 暂停控制
    public void TogglePause()
    {
        if (!canPause) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
            TransitionToState(GameState.Paused);
        else
            TransitionToState(previousState);
    }
}
```

### 方案二：完整网络版流程控制

#### 2.1 适用场景

- 在线多人对战
- 房间匹配系统
- 观众模式

#### 2.2 扩展状态机设计

```csharp
public enum NetworkState
{
    // 大厅阶段
    Disconnected,           // 未连接
    Connecting,             // 连接中
    LobbyWaiting,          // 大厅等待
    RoomCreation,          // 创建房间
    RoomJoining,           // 加入房间
    PlayerMatching,        // 玩家匹配

    // 准备阶段
    RoomSetup,             // 房间设置
    TeamSelection,         // 队伍选择
    ReadyCheck,            // 准备确认

    // 游戏阶段
    GameStarting,          // 游戏开始
    ServePreparation,      // 发球准备
    Serving,               // 发球中
    Rally,                 // 对打中
    PointDecision,         // 得分判定
    SetTransition,         // 局间休息
    MatchComplete,         // 比赛结束

    // 特殊状态
    Spectating,            // 观众模式
    Reconnecting,          // 重连中
    Error                  // 错误状态
}
```

#### 2.3 网络同步机制

```csharp
public class NetworkManager : NetworkBehaviour
{
    // 网络同步变量
    private NetworkVariable<NetworkState> networkState =
        new(NetworkState.Disconnected);

    private NetworkVariable<float> gameTimer = new(0f);
    private NetworkVariable<int> currentSet = new(1);
    private NetworkVariable<bool> isServing = new(false);

    // 状态同步
    [ServerRpc(RequireOwnership = false)]
    public void RequestStateTransitionServerRpc(NetworkState newState,
        ServerRpcParams rpcParams = default)
    {
        if (ValidateStateTransition(networkState.Value, newState))
        {
            networkState.Value = newState;
            BroadcastStateChangeClientRpc(newState);
        }
    }

    [ClientRpc]
    private void BroadcastStateChangeClientRpc(NetworkState newState)
    {
        OnNetworkStateChanged?.Invoke(newState);
    }
}
```

### 方案三：混合模式流程控制

#### 3.1 设计理念

结合单机和网络模式的优点，提供无缝切换体验。

#### 3.2 架构设计

```csharp
public class FlowController : MonoBehaviour
{
    [Header("模式设置")]
    public GameMode gameMode = GameMode.Auto;

    // 子控制器
    private GameController offlineController;
    private NetworkManager networkController;
    private GameController currentController;

    public enum GameMode
    {
        Auto,           // 自动检测（优先离线）
        ForceOffline,   // 强制离线模式
        ForceOnline,    // 强制在线模式
        Hybrid          // 混合模式（可切换）
    }

    // 模式切换
    public void SwitchToMode(GameMode newMode)
    {
        switch (newMode)
        {
            case GameMode.ForceOffline:
                EnableOfflineMode();
                break;
            case GameMode.ForceOnline:
                EnableNetworkMode();
                break;
            case GameMode.Hybrid:
                EnableHybridMode();
                break;
        }
    }

    private void EnableHybridMode()
    {
        // 根据网络状态动态切换
        if (NetworkManager.Singleton.IsConnectedClient)
            currentController = networkController;
        else
            currentController = offlineController;
    }
}
```

## 🎮 发球系统特殊设计

### 1. 发球阶段状态机

```csharp
public enum ServeState
{
    WaitingForServer,       // 等待发球方准备
    BallGenerated,          // 球已生成在手中
    ServingMotion,          // 发球动作中
    BallReleased,           // 球已释放
    ServeValidation,        // 发球有效性检查
    ServeComplete,          // 发球完成
    ServeFault              // 发球失误
}
```

### 2. 发球规则验证

```csharp
public class ServeValidator : MonoBehaviour
{
    [Header("发球规则")]
    public float minThrowHeight = 0.16f;        // 最小抛球高度16cm
    public float maxThrowAngle = 45f;           // 最大偏离垂直角度
    public float throwValidationTime = 2f;      // 抛球动作验证时间

    public bool ValidateServe(Vector3 releasePosition, Vector3 releaseVelocity)
    {
        // 检查抛球高度
        float throwHeight = CalculateMaxHeight(releasePosition, releaseVelocity);
        if (throwHeight < minThrowHeight)
        {
            return false; // 抛球过低
        }

        // 检查抛球角度
        float throwAngle = Vector3.Angle(Vector3.up, releaseVelocity);
        if (throwAngle > maxThrowAngle)
        {
            return false; // 抛球角度过大
        }

        return true;
    }
}
```

## 🚀 推荐实现方案

### 首选方案：渐进式实现

1. **第一阶段**: 实现方案一（简化单机版）
   - 快速验证核心玩法
   - 建立基础状态机框架
   - 实现发球系统和基本规则

2. **第二阶段**: 扩展为方案三（混合模式）
   - 保持单机模式稳定性
   - 添加网络模式支持
   - 实现无缝模式切换

3. **第三阶段**: 优化为完整方案二（网络版）
   - 完善多人游戏体验
   - 添加观众模式
   - 实现高级匹配系统

### 关键实现要点

1. **暂停机制明确性**

   - 单机模式：完全支持暂停（Time.timeScale = 0）
   - 网络模式：不支持暂停，只能退出或断线重连

2. **发球系统物理验证**

   - 基于物理检测而非按键触发
   - 符合乒乓球规则的抛球验证
   - 自然的VR交互体验

3. **状态持久化**

   - 保存游戏进度（仅单机模式）
   - 网络重连状态恢复
   - 设置和偏好保存

4. **错误处理机制**

   - 网络断线恢复
   - 状态不一致修复
   - 优雅的错误提示

这个渐进式方案既能快速实现可玩的原型，又为后续扩展留下了充足的架构空间。
