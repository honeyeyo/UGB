# Epic-1: 场景架构重构
# Story-4: 实现环境组件的动态模式切换

## Story

**As a** 游戏开发者
**I want** 环境组件能够根据游戏模式（单机/多人）动态切换状态
**so that** 玩家可以在统一场景中无缝切换游戏模式，无需重新加载场景

## Status

In Progress

## Context

基于Story-2和Story-3的成果，现在需要实现环境组件的智能模式切换。当前系统已具备：

- **GameModeManager**: 核心游戏模式管理器，支持Local/Network/Menu模式
- **IGameModeComponent接口**: 统一的组件模式切换接口
- **LocalModeComponent**: 单机模式组件管理
- **TableMenuSystem**: 桌面菜单系统

需要完善的功能：
1. 网络模式组件(NetworkModeComponent)的完整实现
2. 环境组件的动态启用/禁用逻辑
3. 模式切换时的状态保持和恢复
4. 性能优化的组件管理策略

技术背景：
- Unity 2022.3 LTS + Meta XR SDK
- 使用Unity Netcode for GameObjects进行网络同步
- Photon Realtime作为传输层
- 需要维持90fps的VR性能要求

## Estimation

Story Points: 3

## Tasks

1. - [ ] 完善NetworkModeComponent实现
   1. - [ ] 实现网络模式专用组件管理
   2. - [ ] 添加网络状态同步逻辑
   3. - [ ] 实现网络模式下的物理同步
   4. - [ ] 编写NetworkModeComponent测试

2. - [ ] 优化GameModeManager组件管理
   1. - [ ] 实现组件自动发现和注册
   2. - [ ] 添加模式切换的事务性保证
   3. - [ ] 优化组件状态切换性能
   4. - [ ] 编写GameModeManager增强测试

3. - [ ] 实现环境状态保持机制
   1. - [ ] 设计状态保存/恢复接口
   2. - [ ] 实现游戏状态在模式切换间的保持
   3. - [ ] 添加用户设置的持久化
   4. - [ ] 编写状态保持机制测试

4. - [ ] 创建模式切换动画和过渡效果
   1. - [ ] 设计流畅的模式切换视觉反馈
   2. - [ ] 实现渐进式组件启用/禁用
   3. - [ ] 添加音效和触觉反馈
   4. - [ ] 优化切换过程的用户体验

5. - [ ] 性能优化和测试
   1. - [ ] 分析模式切换的性能影响
   2. - [ ] 优化组件生命周期管理
   3. - [ ] 确保VR 90fps性能要求
   4. - [ ] 进行多种设备的兼容性测试

## Data Models / Schema

### NetworkModeComponent类结构

```csharp
public class NetworkModeComponent : NetworkBehaviour, IGameModeComponent
{
    [Header("网络模式设置")]
    public bool enableNetworkSync = true;
    public GameObject[] networkOnlyObjects;
    public MonoBehaviour[] networkOnlyComponents;

    [Header("同步设置")]
    public bool syncTransform = true;
    public bool syncAnimation = true;
    public float syncRate = 60f;

    public void OnGameModeChanged(GameMode newMode, GameMode previousMode);
    public bool IsActiveInMode(GameMode mode);
    public void SyncComponentState();
}
```

## Structure

```text
Assets/PongHub/Scripts/Core/
├── Components/
│   ├── LocalModeComponent.cs         // 已存在，需要小幅优化
│   ├── NetworkModeComponent.cs       // 新建，网络模式组件
│   ├── EnvironmentComponent.cs       // 新建，环境组件基类
│   └── ModeTransitionEffect.cs       // 新建，切换动画效果
├── GameModeManager.cs                // 优化现有实现
└── Tests/
    ├── NetworkModeComponentTest.cs   // 新建
    └── TransitionEffectTest.cs       // 新建
```

## Dev Notes

### 实施优先级

1. **Phase 1**: 完善NetworkModeComponent，确保基础的网络模式切换
2. **Phase 2**: 实现状态保持机制，保证切换时游戏状态的连续性
3. **Phase 3**: 添加切换动画和用户体验优化
4. **Phase 4**: 性能优化和设备兼容性测试

## Chat Command Log

- User: 我切换回了以前的500次高级请求的方案. 试一下是否可以使用高级模型了? 如果可以的话, 继续 story-4的工作
- AI: 创建Story-4文档，开始实施环境组件的动态模式切换功能