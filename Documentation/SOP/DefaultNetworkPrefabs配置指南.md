# DefaultNetworkPrefabs 配置指南

## 概述

`DefaultNetworkPrefabs.asset` 是 **Unity Netcode for GameObjects** 的核心配置文件，用于定义可以在网络会话中动态生成和同步的预制体列表。这个文件对于多人 VR 乒乓球游戏至关重要。

## 文件作用

### 1. **网络预制体注册表**

- 只有在此列表中注册的预制体才能通过 `NetworkManager.Singleton.SpawnManager.SpawnNetworkObject()` 方法生成
- 确保所有客户端都拥有相同的预制体定义和标识符

### 2. **跨客户端同步保证**

- 通过 GUID 和 Hash 确保所有客户端使用相同版本的预制体
- 防止因预制体不匹配导致的网络同步错误

### 3. **动态对象管理**

- 支持在运行时生成和销毁网络对象
- 管理对象的所有权转移和状态同步

## 您的乒乓球 VR 游戏配置

### 当前配置解析

> **⚠️ 重要提醒**: Unity 的 `.asset` 文件是纯 YAML 格式，**不支持 `#` 注释**！
> 直接在 `.asset` 文件中添加注释会导致解析错误。
> 注释仅用于此文档中的说明目的。

**实际文件内容** (无注释):

```yaml
List:
  - Override: 0
    Prefab:
      {
        fileID: 4531446710466789163,
        guid: 9659024587d9c5946a05574857d30bdb,
        type: 3,
      }
  - Override: 0
    Prefab:
      {
        fileID: 6697775241635953393,
        guid: ae8adc48da668a243b09e965993d2c6d,
        type: 3,
      }
  - Override: 0
    Prefab:
      {
        fileID: 2983867211162582607,
        guid: faf18a0a765c8bd4982f73dca48b9189,
        type: 3,
      }
  - Override: 0
    Prefab:
      {
        fileID: 2245231018350186115,
        guid: 83e662091352a9f4ca84c40c9fa53958,
        type: 3,
      }
  # ... 更多球预制体
```

**预制体功能说明**:

1. **NetworkSession** - 核心网络会话管理
2. **PlayerAvatarEntity** - 玩家 VR 化身
3. **SpectatorNet** - 观众网络对象
4. **BallSpawner** - 球生成器和对象池
5. **乒乓球预制体们** - 5 种不同类型的球

## 核心网络预制体详解

### 🎯 **必须包含的预制体**

#### 1. NetworkSession

- **用途**: 网络会话管理
- **包含组件**: `NetworkBehaviour`, `NetworkObject`
- **重要性**: ⭐⭐⭐⭐⭐ (核心基础设施)

#### 2. PlayerAvatarEntity

- **用途**: VR 玩家化身
- **包含组件**:
  - `PlayerControllerNetwork`
  - `AvatarEntity` (Meta Avatar)
  - `NetworkedTeam`
  - `RespawnController`
- **重要性**: ⭐⭐⭐⭐⭐ (玩家表示)

#### 3. BallSpawner

- **用途**: 乒乓球管理系统
- **包含组件**:
  - `BallSpawner` (NetworkBehaviour)
  - `NetworkObjectPool`
- **重要性**: ⭐⭐⭐⭐⭐ (游戏核心)

#### 4. 乒乓球预制体们

- **用途**: 可网络同步的乒乓球
- **包含组件**:
  - `BallNetworking`
  - `BallStateSync`
  - `BallSpin`
  - `BallAttachment`
- **重要性**: ⭐⭐⭐⭐⭐ (游戏对象)

### 🎮 **可选包含的预制体**

#### 5. SpectatorNet

- **用途**: 观众系统
- **包含组件**: `SpectatorNetwork`
- **重要性**: ⭐⭐⭐ (增强功能)

#### 6. 球拍预制体 (如果动态生成)

- **用途**: 网络同步的球拍
- **包含组件**: `PaddleNetworking`
- **重要性**: ⭐⭐⭐⭐ (如果支持动态生成球拍)

## 配置最佳实践

### ✅ **应该包含**

1. **所有动态生成的对象**

   ```csharp
   // 这些对象需要在运行时通过网络生成
   NetworkManager.Singleton.SpawnManager.SpawnNetworkObject(ballPrefab);
   ```

2. **包含 NetworkBehaviour 的预制体**

   - 所有继承自 `NetworkBehaviour` 的脚本所在的预制体

3. **跨场景持久化对象**
   - 设置了 `DontDestroyWithOwner = true` 的对象

### ❌ **不应该包含**

1. **静态场景对象**

   - 桌子、墙壁、装饰物等固定环境
   - 这些对象应该直接放置在场景中

2. **纯本地对象**

   - UI 元素、音效播放器、视觉特效等
   - 不需要网络同步的对象

3. **重复的预制体变种**
   - 除非有明确的网络同步需求

## 常见问题和解决方案

### Q1: "Parser Failure" YAML 解析错误

**错误信息**: `Unable to parse file Assets/DefaultNetworkPrefabs.asset: [Parser Failure at line X: Expect ':' between key and value within mapping]`

**原因**: Unity 的 `.asset` 文件不支持 `#` 注释
**解决**:

```csharp
// ❌ 错误 - 不要在 .asset 文件中添加注释
List:
  # 这是注释 - 会导致解析错误！
  - Override: 0

// ✅ 正确 - 纯净的 YAML 格式
List:
  - Override: 0
    Prefab: {fileID: xxx, guid: xxx, type: 3}
```

### Q2: "NetworkPrefab cannot be null" 错误

**原因**: 列表中存在无效的预制体引用
**解决**: 清理空引用，确保所有 GUID 都指向有效预制体

### Q2: 球无法生成

**原因**: 球预制体未在列表中注册
**解决**: 确保所有 `BallSpawner` 中引用的球预制体都在列表中

### Q3: 玩家头像不同步

**原因**: `PlayerAvatarEntity` 预制体配置错误
**解决**: 检查预制体是否包含所有必要的网络组件

### Q4: 对象池中的预制体无法使用

**原因**: 对象池中的预制体也需要注册到网络预制体列表
**解决**: 将 `NetworkObjectPool` 中的所有预制体都添加到列表

## 调试技巧

### 1. 启用网络调试日志

```csharp
// 在 NetworkManager 上启用详细日志
NetworkManager.Singleton.LogLevel = LogLevel.Developer;
```

### 2. 检查预制体哈希值

```csharp
// 验证预制体是否正确注册
foreach (var prefab in NetworkManager.Singleton.NetworkConfig.Prefabs.m_Prefabs)
{
    Debug.Log($"Registered: {prefab.Prefab.name} - Hash: {prefab.Hash}");
}
```

### 3. 运行时验证

```csharp
// 检查对象是否可以生成
bool canSpawn = NetworkManager.Singleton.SpawnManager.CanSpawn(prefab);
Debug.Log($"Can spawn {prefab.name}: {canSpawn}");
```

## 性能优化建议

### 1. 限制预制体数量

- 只注册真正需要网络生成的预制体
- 过多的预制体会增加网络握手时间

### 2. 使用对象池

- 频繁生成/销毁的对象（如乒乓球）应使用 `NetworkObjectPool`
- 减少内存分配和网络消息数量

### 3. 预制体版本管理

- 确保所有客户端使用相同版本的预制体
- 使用版本控制管理预制体变更

## 总结

`DefaultNetworkPrefabs.asset` 是乒乓球 VR 游戏网络架构的基石。正确配置此文件可以：

- ✅ 确保所有网络对象正常生成和同步
- ✅ 避免 "NetworkPrefab cannot be null" 等错误
- ✅ 支持动态球拍、乒乓球和玩家管理
- ✅ 提供流畅的多人 VR 游戏体验

当前配置已经包含了乒乓球 VR 游戏的所有核心网络预制体，可以支持完整的多人对战功能。
