# 乒乓球 VR 竞技场生命周期设计文档

## 概述

针对乒乓球 VR 游戏的特殊性，重新设计竞技场生命周期管理系统，支持单打(1v1)和双打(2v2)两种游戏模式，优化玩家匹配、生成点布局和游戏流程控制。

## 乒乓球游戏特性分析

### 🏓 **游戏模式限制**

- **单打模式 (1v1)**: 每边 1 人，总共 2 人
- **双打模式 (2v2)**: 每边 2 人，总共 4 人
- **不支持其他人数配置**

### 🎮 **VR 空间布局考虑**

- 球桌为中心的对称布局
- A 边 (负 Z 轴方向) vs B 边 (正 Z 轴方向)
- 玩家需要足够的 VR 活动空间
- 观众区域不能干扰比赛区域

### ⏱️ **匹配时机设计**

- **自动匹配**: 根据房间人数自动决定游戏模式
- **手动选择**: 房主可以强制指定游戏模式
- **等待机制**: 人数不足时显示等待界面

## 系统架构设计

### 📋 **游戏模式管理**

```csharp
public enum PongGameMode
{
    Waiting,        // 等待玩家
    Singles,        // 单打 (1v1)
    Doubles,        // 双打 (2v2)
    Spectator       // 观众模式
}

public enum MatchmakingStrategy
{
    Auto,           // 自动根据人数决定
    ForceSingles,   // 强制单打
    ForceDoubles    // 强制双打
}
```

### 🎯 **生成点布局设计**

#### **球桌周围布局**

```text
      观众席 B         观众席 B
         ↑                ↑
    [B2] ↑ [B1]      [B1] ↑ [B2]
         ↑                ↑
    ================球桌================
         ↓                ↓
    [A1] ↓ [A2]      [A1] ↓ [A2]
         ↓                ↓
      观众席 A         观众席 A

单打模式: 只使用 A1, B1
双打模式: 使用 A1, A2, B1, B2
```

#### **VR 空间配置**

- **玩家安全区域**: 每个生成点周围 2m×2m 空间
- **球桌缓冲区**: 距离球桌边缘至少 1.5m
- **观众安全距离**: 距离比赛区域至少 3m

### 🔄 **游戏流程状态机**

```csharp
public enum GameLobbyState
{
    WaitingForPlayers,      // 等待玩家加入
    ModeSelection,          // 模式选择阶段
    TeamBalancing,          // 队伍平衡
    ReadyCheck,             // 准备确认
    GameStarting,           // 游戏开始
    InGame,                 // 游戏中
    PostGame               // 游戏结束
}
```

## UI 设计规范

### 📱 **大厅界面 (LobbyUI)**

#### **主要信息显示**

```csharp
// 当前房间状态
- 房间人数: "2/4 玩家"
- 当前模式: "等待中 / 单打 / 双打"
- 房主标识: "👑 房主"
- 等待状态: "等待更多玩家..."
```

#### **模式选择面板**

```csharp
// 房主专用控制
[自动匹配] [强制单打] [强制双打]
状态: "当前设置: 自动匹配"

// 普通玩家显示
"房主正在选择游戏模式..."
```

#### **队伍分配显示**

```csharp
A边 (蓝队)          |    B边 (红队)
[玩家1]             |    [玩家3]
[玩家2] (双打)      |    [玩家4] (双打)
                    |
[准备] [未准备]     |    [准备] [未准备]
```

### 🎮 **游戏内 HUD**

#### **比赛信息**

```csharp
游戏模式: 单打 / 双打
比分: A队 11 - 7 B队
发球方: 玩家1 →
```

#### **等待界面**

```csharp
"正在等待玩家加入..."
"需要 X 名玩家开始比赛"
[取消匹配] 按钮
```

## 匹配逻辑设计

### 🤖 **自动匹配策略**

```csharp
private PongGameMode DetermineGameMode(int playerCount, MatchmakingStrategy strategy)
{
    return strategy switch
    {
        MatchmakingStrategy.Auto => playerCount switch
        {
            2 => PongGameMode.Singles,
            3 => PongGameMode.Singles, // 1人观众
            4 => PongGameMode.Doubles,
            > 4 => PongGameMode.Doubles, // 多余的做观众
            _ => PongGameMode.Waiting
        },
        MatchmakingStrategy.ForceSingles => playerCount >= 2
            ? PongGameMode.Singles
            : PongGameMode.Waiting,
        MatchmakingStrategy.ForceDoubles => playerCount >= 4
            ? PongGameMode.Doubles
            : PongGameMode.Waiting,
        _ => PongGameMode.Waiting
    };
}
```

### ⚖️ **队伍平衡算法**

```csharp
private void BalanceTeams(List<PongPlayerData> players, PongGameMode mode)
{
    switch (mode)
    {
        case PongGameMode.Singles:
            // 随机或根据技能评级分配
            AssignSinglesTeams(players);
            break;

        case PongGameMode.Doubles:
            // 确保每队有2人，技能平衡
            AssignDoublesTeams(players);
            break;
    }
}
```

### 🔄 **重连处理**

```csharp
private void HandlePlayerReconnection(string playerId, ulong newClientId)
{
    var playerData = GetPlayerData(playerId);
    if (playerData.HasValue)
    {
        // 恢复原有位置和队伍
        playerData.ClientId = newClientId;
        playerData.IsConnected = true;

        // 如果游戏进行中，恢复到原位置
        if (currentGameState == GameLobbyState.InGame)
        {
            RespawnPlayerAtOriginalPosition(playerData);
        }
    }
}
```

## 生成点管理

### 📍 **生成点配置**

```csharp
[System.Serializable]
public class PongSpawnConfiguration
{
    [Header("A边生成点 (蓝队)")]
    public Transform teamA_Position1;  // A1 单打主位
    public Transform teamA_Position2;  // A2 双打副位

    [Header("B边生成点 (红队)")]
    public Transform teamB_Position1;  // B1 单打主位
    public Transform teamB_Position2;  // B2 双打副位

    [Header("观众区域")]
    public Transform[] spectatorA_Positions;
    public Transform[] spectatorB_Positions;

    [Header("安全区域配置")]
    public float playerSafeRadius = 1.5f;
    public float tableSafeDistance = 1.5f;
}
```

### 🎯 **位置分配逻辑**

```csharp
private Transform GetSpawnPoint(PongGameMode mode, NetworkedTeam.Team team, int playerIndex)
{
    return mode switch
    {
        PongGameMode.Singles => team == NetworkedTeam.Team.TeamA
            ? spawnConfig.teamA_Position1
            : spawnConfig.teamB_Position1,

        PongGameMode.Doubles => team == NetworkedTeam.Team.TeamA
            ? (playerIndex == 0 ? spawnConfig.teamA_Position1 : spawnConfig.teamA_Position2)
            : (playerIndex == 0 ? spawnConfig.teamB_Position1 : spawnConfig.teamB_Position2),

        _ => null
    };
}
```

## 事件系统设计

### 📡 **网络事件**

```csharp
public class PongGameEvents
{
    // 游戏模式事件
    public static Action<PongGameMode> OnGameModeChanged;
    public static Action<int> OnPlayerCountChanged;
    public static Action<MatchmakingStrategy> OnMatchmakingStrategyChanged;

    // 队伍事件
    public static Action<string, NetworkedTeam.Team> OnPlayerTeamAssigned;
    public static Action<NetworkedTeam.Team> OnTeamReadyStateChanged;

    // 游戏状态事件
    public static Action<GameLobbyState> OnLobbyStateChanged;
    public static Action OnGameStartRequested;
    public static Action OnGameEndRequested;
}
```

### 🎮 **UI 响应事件**

```csharp
private void OnEnable()
{
    PongGameEvents.OnGameModeChanged += UpdateModeDisplay;
    PongGameEvents.OnPlayerCountChanged += UpdatePlayerCountDisplay;
    PongGameEvents.OnLobbyStateChanged += HandleLobbyStateChange;
}
```

## 性能优化考虑

### ⚡ **网络优化**

- 减少不必要的 RPC 调用
- 批量处理队伍分配
- 本地缓存玩家状态

### 🎯 **VR 优化**

- 预加载生成点位置
- 优化传送动画
- 减少不必要的位置更新

### 📱 **UI 优化**

- 延迟更新非关键 UI
- 使用对象池管理 UI 元素
- 异步加载重要界面

## 兼容性设计

### 🔄 **向后兼容**

- 保持现有观众系统不变
- 渐进式迁移现有代码
- 保留原有事件接口

### 🆕 **扩展性**

- 支持未来添加新游戏模式
- 模块化的队伍管理
- 可配置的匹配策略

## 测试策略

### 🧪 **单元测试**

- 队伍分配逻辑测试
- 生成点计算测试
- 状态转换测试

### 🎮 **集成测试**

- 多人连接测试
- 重连恢复测试
- 模式切换测试

### 👥 **用户测试**

- VR 空间舒适度测试
- UI 交互体验测试
- 网络延迟影响测试

---

这个设计文档为乒乓球 VR 游戏提供了完整的竞技场生命周期管理方案，确保游戏体验的流畅性和用户友好性。
