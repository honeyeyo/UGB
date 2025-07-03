# UI架构迁移指南

## 概述

PongHub项目正在从旧的单体UIManager架构迁移到新的模块化VR优化架构。本指南说明如何从旧架构迁移到新架构。

## 架构对比

### 旧架构 (已弃用)
```csharp
// 单体UIManager，包含所有UI逻辑
UIManager.Instance.ShowSettings();
UIManager.Instance.StartGame();
UIManager.Instance.PauseGame();
```

### 新架构 (导入中...)
```csharp
// 模块化组件
MenuCanvasController canvasController;  // Canvas控制
TableMenuSystem menuSystem;            // 菜单逻辑
VRMenuInteraction vrInteraction;       // VR交互
```

## 组件映射

| 旧组件功能 | 新组件 | 备注 |
|-----------|--------|------|
| UIManager.ShowSettings() | TableMenuSystem.ShowPanel() | 使用面板系统 |
| UIManager.StartGame() | GameModeManager.SwitchToMode() | 游戏模式管理 |
| UIManager.PauseGame() | GameModeManager.SwitchToMode() | 游戏模式管理 |
| UIManager.ShowMessage() | VRUIHelper.ShowNotification() | VR优化通知 |
| UIManager.ConfigurePaddle() | 专门的配置组件 | 分离职责 |
| UIManager.TeleportToPoint() | 专门的传送组件 | 分离职责 |

## 迁移步骤

### 1. 面板显示迁移
```csharp
// 旧代码
UIManager.Instance.ShowSettings();

// 新代码
var menuSystem = FindObjectOfType<TableMenuSystem>();
menuSystem?.ShowPanel("SettingsPanel");
```

### 2. 游戏状态管理迁移
```csharp
// 旧代码
UIManager.Instance.StartGame();
UIManager.Instance.PauseGame();

// 新代码
GameModeManager.Instance.SwitchToMode(GameMode.Local);
GameModeManager.Instance.SwitchToMode(GameMode.Menu);
```

### 3. VR交互迁移
```csharp
// 旧代码
UIManager.Instance.ShowMessage("消息");

// 新代码
VRUIHelper.ShowNotification("消息", 3f);
```

## 需要更新的文件

以下文件包含UIManager引用，需要迁移：

### UI脚本
- `Assets/PongHub/Scripts/UI/SettingsPanel.cs`
- `Assets/PongHub/Scripts/UI/InputSettingsPanel.cs`
- `Assets/PongHub/Scripts/UI/MainMenuPanel.cs`
- `Assets/PongHub/Scripts/UI/GameplayHUD.cs`

### 应用脚本
- `Assets/PongHub/Scripts/App/PHApplication.cs`

### 编辑器脚本
- `Assets/PongHub/Scripts/Editor/PongHubDebugHelper.cs`

## 迁移时间表

### 阶段1: 标记弃用 ✅
- [x] 在UIManager添加Obsolete属性
- [x] 添加迁移指南文档

### 阶段2: 功能迁移
- [ ] 更新SettingsPanel.cs
- [ ] 更新InputSettingsPanel.cs
- [ ] 更新MainMenuPanel.cs
- [ ] 更新GameplayHUD.cs
- [ ] 更新PHApplication.cs

### 阶段3: 测试验证
- [ ] VR环境功能测试
- [ ] 向后兼容性测试
- [ ] 性能验证

### 阶段4: 清理
- [ ] 删除UIManager.cs
- [ ] 清理相关引用

## 注意事项

1. **向后兼容**: 在完全迁移前，旧代码仍能正常工作
2. **测试要求**: 所有功能迁移后需要VR环境测试
3. **性能优化**: 新架构针对VR性能进行了优化
4. **模块化**: 新架构支持更好的代码维护和扩展

## 常见问题

### Q: 为什么要迁移？
A: 新架构针对VR进行优化，支持更好的性能和用户体验。

### Q: 迁移是否会破坏现有功能？
A: 不会，我们保持向后兼容性直到完全迁移。

### Q: 何时删除旧UIManager？
A: 在所有引用迁移完成并通过测试后。