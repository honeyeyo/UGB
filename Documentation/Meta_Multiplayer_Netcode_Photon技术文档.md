# Meta Multiplayer Netcode Photon 技术文档

## 概述

`com.meta.multiplayer.netcode-photon` 是Meta公司开发的Unity多人游戏网络包，专门为使用Unity Netcode for GameObjects和Photon作为传输层的VR多人应用程序提供核心功能。该包集成了Avatar系统、连接管理、用户封锁、群组状态和语音通信等完整的多人游戏解决方案。

## 包信息

- **包名**: com.meta.multiplayer.netcode-photon
- **显示名称**: Meta Multiplayer For Netcode and Photon
- **版本**: 1.1.0
- **命名空间**: Meta.Multiplayer

## 主要依赖

- Unity Netcode for GameObjects (1.1.0)
- Meta XR SDK Core
- Photon Realtime Transport (2.0.0)
- Photon Voice2 (2.28.2)
- Meta Utilities (0.0.1)
- Meta XR SDK Avatars (33.0.0)

## 架构组件

### Core 核心模块

#### 1. NetworkLayer 网络层管理器

**文件路径**: `Core/NetworkLayer.cs`

**主要功能**:

- 网络连接状态管理
- 支持主机(Host)和客户端(Client)连接模式
- 自动重连和主机迁移机制
- Photon房间管理和区域切换

**关键特性**:

- **连接状态枚举**: 定义了12种不同的客户端连接状态
- **自动故障恢复**: 当连接失败时自动尝试不同的连接策略
- **主机迁移**: 支持当原主机离开时的无缝主机迁移
- **区域管理**: 支持动态切换Photon服务器区域

**核心方法**:

```csharp
public void Init(string room, string region)  // 初始化网络层
public void GoToLobby()                       // 进入大厅
public void SwitchPhotonRealtimeRoom(string room, bool isHosting, string region)  // 切换房间
public void Leave()                           // 离开当前会话
```

#### 2. NetworkSession 网络会话管理器

**文件路径**: `Core/NetworkSession.cs`

**主要功能**:

- 维护网络会话状态
- 管理备用主机选择
- 同步Photon语音房间信息

**静态属性**:

- `FallbackHostId`: 备用主机客户端ID
- `PhotonVoiceRoom`: 当前语音房间名称

#### 3. ClientNetworkTransform 客户端网络变换

**文件路径**: `Core/ClientNetworkTransform.cs`

**主要功能**:

- 客户端权威的网络变换同步
- 支持忽略网络更新的本地控制
- 提供强制同步功能

**关键特性**:

- **客户端权威**: 由客户端控制变换同步，减少网络延迟
- **忽略更新模式**: 可以暂时忽略网络更新保持本地控制
- **强制同步**: 提供立即同步变换状态的能力

#### 4. SceneLoader 场景加载器

**文件路径**: `Core/SceneLoader.cs`

**主要功能**:

- 网络场景加载管理
- 场景加载状态跟踪
- 多人场景同步

**核心功能**:

- 通过NetworkManager加载场景确保网络同步
- 自动设置新加载的场景为活跃场景
- 提供场景加载完成状态查询

#### 5. BlockUserManager 用户封锁管理器

**主要功能**:

- 集成Meta平台封锁API
- 管理被封锁用户列表
- 提供封锁、解封和查询功能

#### 6. GroupPresenceState 群组状态管理器

**主要功能**:

- 处理Meta平台群组状态API
- 管理用户在线状态
- 支持邀请、名单和加入功能

#### 7. VoipController & VoipHandler 语音通信控制器

**主要功能**:

- 管理本地录音器和远程扬声器
- 处理麦克风权限
- 连接到正确的语音房间
- 支持静音控制

### Avatar 头像模块

#### 1. AvatarEntity 头像实体管理器

**文件路径**: `Avatar/AvatarEntity.cs`

**主要功能**:

- OvrAvatarEntity的完整实现
- 基于用户ID设置头像
- 集成身体跟踪
- 处理本地和远程头像设置

**关键特性**:

- 自动头像加载和配置
- 身体跟踪集成
- 关节加载事件处理
- 头像显示/隐藏控制
- 相机装备跟踪

#### 2. AvatarNetworking 头像网络同步

**文件路径**: `Avatar/AvatarNetworking.cs`

**主要功能**:

- 头像状态网络同步
- 支持不同LOD级别的更新频率
- 本地头像数据发送
- 远程头像数据接收和应用

**关键特性**:

- **LOD频率控制**: 根据距离和重要性调整更新频率
- **流延迟管理**: 优化网络传输性能
- **用户ID网络变量**: 同步头像用户身份信息

## 使用场景

### 1. VR多人游戏开发

- 提供完整的多人VR游戏网络基础设施
- 支持Meta Quest平台的社交功能
- 集成头像系统增强沉浸感

### 2. 社交VR应用

- 群组状态管理支持社交功能
- 语音通信实现实时交流
- 用户封锁保障安全环境

### 3. 企业协作平台

- 场景同步支持多用户协作
- 头像系统提供身份识别
- 网络层确保连接稳定性

## 集成指南

### 1. 基本设置

```csharp
// 初始化网络层
NetworkLayer networkLayer = GetComponent<NetworkLayer>();
networkLayer.Init(roomName, region);

// 设置回调
networkLayer.StartHostCallback += OnHostStarted;
networkLayer.StartClientCallback += OnClientStarted;
```

### 2. 头像系统集成

```csharp
// 配置头像实体
AvatarEntity avatarEntity = GetComponent<AvatarEntity>();
AvatarNetworking avatarNetworking = GetComponent<AvatarNetworking>();

// 头像网络同步会自动处理LOD和数据传输
```

### 3. 语音通信设置

```csharp
// VoipController会自动处理麦克风权限和房间连接
// 通过NetworkSession.PhotonVoiceRoom获取当前语音房间
```

## 性能优化

### 1. 网络优化

- **LOD系统**: 根据距离调整头像更新频率
- **流延迟控制**: 优化网络带宽使用
- **客户端权威**: 减少网络往返延迟

### 2. 连接优化

- **自动重连**: 网络中断时自动恢复连接
- **主机迁移**: 主机离开时无缝切换新主机
- **区域选择**: 支持选择最佳服务器区域

## 注意事项

1. **平台依赖**: 需要Meta XR SDK和Photon SDK的支持
2. **权限管理**: 语音功能需要麦克风权限
3. **网络环境**: 需要稳定的网络连接
4. **版本兼容**: 确保所有依赖包版本兼容

## 常见问题

### Q: 如何处理网络连接失败？

A: NetworkLayer提供自动重连机制，会根据失败原因自动尝试不同的连接策略。

### Q: 头像加载缓慢如何优化？

A: 可以通过调整AvatarNetworking的LOD频率设置来优化性能。

### Q: 如何实现自定义主机迁移逻辑？

A: 可以通过设置NetworkLayer的CanMigrateAsHostFunc回调来实现自定义逻辑。

## 版本历史

- **1.1.0**: 当前版本，提供完整的多人游戏网络解决方案

## 相关资源

- [Unity Netcode for GameObjects 文档](https://docs-multiplayer.unity3d.com/netcode/current/)
- [Photon Unity SDK 文档](https://doc.photonengine.com/pun2/current/getting-started/pun-intro)
- [Meta XR SDK 文档](https://developer.oculus.com/documentation/unity/)

---

*本文档基于 com.meta.multiplayer.netcode-photon v1.1.0 版本编写*