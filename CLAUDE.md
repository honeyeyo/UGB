# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

PongHub是一个为Meta Quest设备构建的VR乒乓球游戏。基于Unity 2022.3.52f1+开发，使用Unity Netcode + Photon Realtime实现多人网络功能，具有完整的VR交互系统和模块化架构，支持单机和多人模式。

## 重要命令

### Unity项目命令
- **打开项目**: 这是Unity 2022.3.52f1+项目 - 在Unity编辑器中打开
- **构建**: 使用Unity的Build Settings (File > Build Settings) 设置目标平台
- **测试**: 使用Unity Test Runner (Window > General > Test Runner)
- **代码检查**: 遵循PongHub编码风格规则，无明确的linting配置

### 开发环境
- **Commitizen**: 使用传统变更日志格式，运行 `npm run commit` (需要Node.js)
- **MCP Unity集成**: 项目包含MCP Unity包，用于Cursor IDE集成
- **VR测试**: 支持Quest Link和XR Device Simulator进行测试

### 包管理
- **Unity包**: 通过Package Manager管理 (Window > Package Manager)  
- **自定义包**: 位于`Packages/`文件夹，包括Meta工具包
- **依赖关系**: 检查`Packages/manifest.json`了解包依赖

## 核心架构

### 高层系统设计
```
应用层 (启动、导航、设置)
├── 核心系统 (GameModeManager、SceneManager、AudioManager)
├── 竞技场系统 (游戏玩法、玩家、网络、赛后)
├── UI系统 (组件、本地化、面板)
├── 输入系统 (VR控制器、球拍控制)
└── 游戏玩法系统 (球物理、球桌、球拍)
```

### 关键组件

#### GameModeManager (`Assets/PongHub/Scripts/Core/GameModeManager.cs`)
- **作用**: 在Local/Network/Menu模式间切换的中央协调器
- **关键方法**: `SwitchToMode()`, `RegisterComponent()`, `UnregisterComponent()`
- **架构**: 使用`IGameModeComponent`接口管理模式感知组件
- **依赖**: 所有游戏系统必须注册到此管理器

#### 网络架构
- **传输层**: Unity Netcode for GameObjects + Photon Realtime
- **关键类**: `PongHubNetworkManager`, `NetworkBehaviour`实现
- **命名空间**: 使用`PongHub.Arena.*`存放多人游戏逻辑（非场景特定）
- **模式**: `NetworkVariable<T>`进行状态同步，`[ServerRpc]`/`[ClientRpc]`进行远程调用

#### 输入系统
- **管理器**: `PongHubInputManager` - 中央输入协调
- **VR集成**: 使用XR Interaction Toolkit + Meta工具包
- **控制器**: `PaddleController`, `TeleportController`, `ServeBallController`
- **性能**: 输入系统包含性能监控和优化

#### 音频系统
- **核心**: `AudioManager`单例与`AudioController`组件
- **空间音频**: 集成Meta XR Audio SDK用于VR空间声音
- **配置**: `AudioConfiguration` ScriptableObjects用于设置

### 命名空间约定
- `PongHub.Core` - 核心系统（GameModeManager、SceneManager等）
- `PongHub.Arena.*` - 多人游戏房间逻辑（跨场景复用）
- `PongHub.UI.*` - 用户界面组件和面板
- `PongHub.Gameplay.*` - 游戏机制（球、球拍、球桌）
- `PongHub.Input` - 输入处理和VR控制器

### 组件注册模式
所有模式感知组件必须实现`IGameModeComponent`:
```csharp
public interface IGameModeComponent
{
    void OnGameModeChanged(GameMode newMode, GameMode previousMode);
    bool IsActiveInMode(GameMode mode);
}
```

## 编码规范

### 语言和风格
- **主要语言**: 所有UI文本、变量名和公共API使用英语
- **注释**: 允许中英文结合以便更好理解
- **命名**: 公共成员使用PascalCase，私有字段使用`m_`前缀
- **网络代码**: 使用Unity Netcode模式，避免Photon PUN命名空间

### 组件结构
- 使用`[RequireComponent]`声明依赖
- 在`Awake()`中获取组件引用
- 使用`[SerializeField]`配合`[Header]`在Inspector中组织
- 遵循生命周期顺序：字段 → 属性 → Unity回调 → 公共方法 → 私有方法

### Unity Editor Tooltips规范
- 为所有`[SerializeField]`和公共字段添加`[Tooltip]`属性
- 提供清晰简洁的描述（理想长度50-100字符）
- 在适用时包含数值范围、单位或预期格式
- 使用一致的语言和术语
- 解释字段的用途及其对游戏玩法/功能的影响

```csharp
[Header("VR设置")]
[SerializeField]
[Tooltip("VR射线投射交互的最小按钮大小（像素）")]
private float minButtonSize = 80f;

[SerializeField]
[Tooltip("VR菜单标题字体大小（推荐：32-48以保证可读性）")]
private int titleFontSize = 36;
```

### VR桌面菜单UI设计规范
- **字体文本**: 使用大号粗体字体（正文最小24pt，标题32pt+）
- **控件布局**: 最小按钮尺寸80x80像素，元素间最小20px间距
- **颜色对比**: 基于桌面表面颜色动态调整对比度（最小4.5:1比率）
- **图标表情**: 使用简单易识别的表情符号，最小32x32px尺寸

### 性能考虑
- 缓存组件引用（避免在Update中使用`GetComponent`）
- 为频繁生成销毁的对象使用对象池
- 通过内置基准测试监控输入系统性能
- 组件适当地向GameModeManager注册/注销

## 开发工作流

### Agile工作流程规范
1. **首先检查** `.ai/prd.md` 文件是否存在，如果没有则与用户协作创建
2. **改进PRD文档** 确保包含详细的目的、架构模式、技术决策和限制条件
3. **生成架构文档** `.ai/arch.md` 草稿并等待批准
4. **创建Story文件** 使用903-story.mdc模板在.ai文件夹中创建
5. **TDD开发** 每个子任务包含至少80%覆盖率的单元测试
6. **更新Story状态** 随着子任务完成及时更新文档

### MCP Unity集成
- 项目已配置Cursor IDE的MCP Unity包
- 使用"Tools > MCP Unity > Server Window"启动MCP服务器
- AI可直接操作Unity场景、GameObject并运行测试
- 支持自然语言Unity编辑器自动化

### 场景架构
- **Startup.unity**: 入口点和初始化
- **MainMenu.unity**: 主菜单与UI系统演示
- **游戏场景**: 使用模块化方法 - 环境与游戏逻辑分离
- **共享环境**: 静态对象标记为跨模式复用

### 资源组织
- **脚本**: 按功能区域组织在`Assets/PongHub/Scripts/`下
- **预制件**: 基于组件的预制件在`Assets/PongHub/Prefabs/`
- **场景**: 核心场景在`Assets/PongHub/Scenes/`
- **第三方**: 外部资源在专用文件夹（TirgamesAssets等）

### 测试策略
- Unity Test Runner进行自动化测试
- 通过Quest Link或XR Simulator进行手动VR测试
- 输入系统和网络的性能基准测试
- 通过GameModeManager完整性检查进行场景验证

## 当前项目状态和后续计划

### 当前完成情况（约75-80%）
- **Epic-1 场景架构重构**: ✅ 100%完成
- **Epic-2 桌面菜单UI系统**: 🔄 75%完成（进行中）
- **Epic-3 输入系统整合优化**: ⏳ 计划中
- **Epic-4 性能优化和测试**: ⏳ 计划中

### AI已完成的主要功能（可自动生成）
1. **核心架构系统** - GameModeManager、StartupController等
2. **VR UI组件库** - 13个基础组件 + 5个容器组件  
3. **本地化系统** - 完整的多语言管理
4. **模式切换界面** - 包含动画效果和音效系统
5. **设置系统** - 从50+编译错误到零错误的完整解决
6. **音频系统** - 83个方法的完整音频管理

### 需要手动完成的任务类型
1. **Unity Editor操作** - 预制件创建、场景集成、组件引用分配
2. **VR设备测试** - Meta Quest真机测试、交互体验验证
3. **资源创建** - 3D模型、材质贴图、音频资源导入
4. **网络功能集成** - Photon网络测试、Avatar同步调试

### 推荐工作配合模式
1. **AI先行**: 设计架构，生成核心代码和文档
2. **手动验证**: Unity中集成，功能测试
3. **AI优化**: 基于测试结果进行代码调整  
4. **手动完善**: 视觉效果，用户体验微调
5. **AI文档**: 生成使用指南，维护文档

## 关键依赖和集成

### Meta/Oculus集成
- Meta XR SDK配备Avatar、Audio和Interaction组件
- OVR Integration用于VR特定功能
- Meta Utilities包（包含在`Packages/`中）

### 网络堆栈
- Unity Netcode for GameObjects（权威服务器）
- Photon Realtime传输层（自定义实现）
- Meta Multiplayer blocks用于VR特定网络

### 音频和图形
- Meta XR Audio SDK用于VR空间音频
- Universal Render Pipeline (URP)配备VR优化
- DOTween用于UI动画

### 开发工具
- ParrelSync用于多客户端测试
- Dependencies Hunter用于资源依赖分析
- Unity Toolbar Extender用于编辑器UI增强

## 常见模式和最佳实践

### 模式切换模式
```csharp
// 向GameModeManager注册
GameModeManager.Instance.RegisterComponent(this);

// 实现模式感知
public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
{
    switch(newMode)
    {
        case GameMode.Local: /* 启用本地功能 */ break;
        case GameMode.Network: /* 启用网络功能 */ break;
        case GameMode.Menu: /* 禁用游戏功能 */ break;
    }
}
```

### VR UI模式
- 使用`VRUIComponent`基类创建VR感知UI元素
- 实现基于距离的交互阈值
- 支持手部跟踪和控制器输入
- 包含交互的音频/触觉反馈

### 网络同步模式
```csharp
public class NetworkedComponent : NetworkBehaviour
{
    private NetworkVariable<Vector3> m_networkPosition = new();
    
    [ServerRpc(RequireOwnership = false)]
    public void UpdatePositionServerRpc(Vector3 position)
    {
        m_networkPosition.Value = position;
    }
}
```

此架构支持快速开发，同时保持VR性能和网络可靠性。模块化设计允许轻松扩展游戏模式和功能。