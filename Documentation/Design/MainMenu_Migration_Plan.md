---
title: 主菜单系统迁移计划
description: 从旧MainMenu架构迁移到新UI系统的详细计划
version: 1.0.0
date: 2025-07-07
tags: [迁移, UI, 菜单系统, 重构]
---

# 主菜单系统迁移计划

## 概述

本文档详细描述了从旧的MainMenu架构迁移到新的UI系统的计划。新UI系统基于MenuPanelBase和MainMenuController设计，提供了更加模块化、可扩展的菜单系统架构。

## 冲突分析

### 旧系统组件

#### 1. MainMenu目录下的脚本

| 文件名 | 功能 | 冲突分析 |
|:-------|:-----|:---------|
| MainMenuController.cs | 主菜单控制器 | **直接冲突**：与新的MainMenuController.cs功能重叠 |
| BaseMenuController.cs | 菜单控制器基类 | **直接冲突**：与新的MenuPanelBase.cs功能重叠 |
| FriendsMenuController.cs | 好友菜单控制器 | **部分冲突**：功能将被整合到新的面板系统 |
| JoinFriendListElement.cs | 好友列表元素 | **可复用**：可迁移到新系统 |
| MainMenuButton.cs | 菜单按钮 | **可替代**：使用新的UI组件库中的按钮组件 |
| ScrollViewController.cs | 滚动视图控制器 | **可复用**：可迁移到新系统 |
| StoreMenuController.cs | 商店菜单控制器 | **部分冲突**：功能将被整合到新的面板系统 |
| StoreIconButton.cs | 商店图标按钮 | **可替代**：使用新的UI组件库中的按钮组件 |
| SettingsMenu.cs | 设置菜单 | **直接冲突**：与新的SettingsPanel功能重叠 |
| MenuErrorPanel.cs | 错误面板 | **可复用**：可迁移到新系统 |

#### 2. Design目录下的脚本

| 文件名 | 功能 | 冲突分析 |
|:-------|:-----|:---------|
| PaddleData.cs | 球拍数据 | **无冲突**：与Arena/Gameplay下的实现不重叠 |
| TableData.cs | 桌子数据 | **无冲突**：与Arena/Gameplay下的实现不重叠 |
| PaddleSetting.cs | 球拍设置 | **部分重叠**：与ServePermissionManager有一些功能重叠 |

### 新系统组件

#### 1. UI目录下的脚本

| 文件名 | 功能 | 迁移策略 |
|:-------|:-----|:---------|
| MainMenuController.cs | 新的主菜单控制器 | 替换旧的MainMenuController.cs |
| MenuPanelBase.cs | 菜单面板基类 | 替换旧的BaseMenuController.cs |
| MenuInputHandler.cs | 菜单输入处理器 | 新增功能，整合VR输入处理 |
| TableMenuSystem.cs | 桌面菜单系统 | 保留并增强功能 |

## 迁移策略

### 1. 文件替换计划

以下文件将被删除或替换：

1. **删除**：`Assets/PongHub/Scripts/MainMenu/MainMenuController.cs`
2. **删除**：`Assets/PongHub/Scripts/MainMenu/BaseMenuController.cs`
3. **删除**：`Assets/PongHub/Scripts/MainMenu/SettingsMenu.cs`
4. **删除**：`Assets/PongHub/Scripts/MainMenu/MainMenuButton.cs`
5. **删除**：`Assets/PongHub/Scripts/MainMenu/StoreIconButton.cs`

### 2. 功能迁移计划

| 旧功能 | 迁移目标 | 迁移策略 |
|:-------|:---------|:---------|
| 菜单状态管理 | MainMenuController | 使用新的面板导航系统替代旧的状态枚举 |
| 按钮点击处理 | MainMenuController | 使用Unity事件系统和委托替代直接方法调用 |
| 菜单面板显示/隐藏 | MenuPanelBase | 使用新的动画系统替代简单的GameObject激活/禁用 |
| 错误消息显示 | 专用错误面板 | 将错误处理逻辑迁移到新的错误处理系统 |
| 好友系统 | 专用好友面板 | 将好友功能封装到单独的面板中 |
| 商店系统 | 专用商店面板 | 将商店功能封装到单独的面板中 |

### 3. 数据迁移计划

Design目录下的脚本不需要删除，因为它们与Arena/Gameplay下的实现没有直接冲突：

1. **保留**：`Assets/PongHub/Scripts/Design/PaddleData.cs` - 用于球拍数据定义
2. **保留**：`Assets/PongHub/Scripts/Design/TableData.cs` - 用于桌子数据定义
3. **保留**：`Assets/PongHub/Scripts/Design/PaddleSetting.cs` - 用于球拍设置

这些脚本将继续用于数据定义，而实际的游戏逻辑将由Arena/Gameplay下的脚本处理。

## 实施步骤

### 阶段1：准备工作

1. 创建新的UI脚本
   - ✅ 创建MainMenuController.cs
   - ✅ 创建MenuPanelBase.cs
   - ✅ 创建MenuInputHandler.cs

2. 创建菜单面板预制件
   - 创建MainMenuPanel预制件
   - 创建SettingsPanel预制件
   - 创建GameModePanel预制件
   - 创建ExitConfirmPanel预制件

### 阶段2：功能迁移

1. 将旧系统的核心功能迁移到新系统
   - 菜单导航逻辑
   - 按钮事件处理
   - 游戏模式切换

2. 迁移可复用组件
   - 错误面板
   - 好友列表元素
   - 滚动视图控制器

### 阶段3：清理和测试

1. 删除冲突文件
   - 删除旧的MainMenuController.cs
   - 删除旧的BaseMenuController.cs
   - 删除其他冲突文件

2. 测试新系统功能
   - 菜单导航
   - 面板切换动画
   - 游戏模式选择
   - 设置调整

### 阶段4：优化和完善

1. 优化性能
   - 减少渲染批次
   - 优化UI布局

2. 完善用户体验
   - 添加音频反馈
   - 添加触觉反馈
   - 优化交互流程

## 风险评估

### 潜在风险

1. **功能丢失**：旧系统中的某些特殊功能可能在迁移过程中被遗漏
   - **缓解措施**：详细记录旧系统功能，确保新系统完全覆盖

2. **兼容性问题**：新系统可能与其他系统组件不兼容
   - **缓解措施**：增加集成测试，确保系统间正确交互

3. **性能退化**：新系统可能引入额外的性能开销
   - **缓解措施**：进行性能测试，优化关键路径

## 结论

通过实施这一迁移计划，我们将用更加模块化、可扩展的UI系统替换旧的主菜单架构。新系统将提供更好的用户体验、更清晰的代码结构和更高的可维护性。迁移过程将分阶段进行，确保功能的平滑过渡和系统稳定性。