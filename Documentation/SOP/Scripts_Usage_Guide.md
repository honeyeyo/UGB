# 测试/编辑器工具类脚本使用指南

<version>1.0.0</version>

本文档提供项目中测试脚本、Editor脚本和工具类的详细使用指南。

## 📋 目录

- [测试脚本](#测试脚本)
  - [TableMenuSystemTest](#tablemenuSystemtest)
- [Editor脚本](#editor脚本)
- [工具类](#工具类)
  - [VRUIHelper](#vruihelper)
- [开发工作流程](#开发工作流程)

---

## 🧪 测试脚本

### TableMenuSystemTest

**文件路径**: `Assets/PongHub/Scripts/Core/Tests/TableMenuSystemTest.cs`

#### 功能概述

桌面菜单系统的综合测试脚本，提供键盘快捷键和自动化测试功能。

#### 使用方法

##### 1. 创建测试对象

```csharp
// 方法1: 通过Unity菜单创建
Unity菜单 -> PongHub -> Test -> Table Menu System

// 方法2: 手动添加
在场景中创建空GameObject，添加TableMenuSystemTest组件
```

##### 2. 配置测试参数

在Inspector中配置以下参数：

| 参数 | 描述 | 推荐值 |
|------|------|--------|
| Enable Keyboard Testing | 启用键盘快捷键测试 | true |
| Enable Auto Testing | 启用自动化测试循环 | false |
| Auto Test Interval | 自动测试间隔时间 | 3.0秒 |
| Table Menu System | 要测试的菜单系统 | 自动查找 |
| VR Menu Interaction | VR交互组件 | 自动查找 |
| Game Mode Manager | 游戏模式管理器 | 自动查找 |

##### 3. 键盘快捷键

| 按键 | 功能 | 说明 |
|------|------|------|
| **M** | 切换菜单显示/隐藏 | Toggle menu visibility |
| **1** | 显示主菜单面板 | Show main menu panel |
| **2** | 显示游戏模式面板 | Show game mode panel |
| **3** | 显示设置面板 | Show settings panel |
| **4** | 显示音频面板 | Show audio panel |
| **5** | 显示退出面板 | Show exit panel |
| **L** | 切换到单机模式 | Switch to Local mode |
| **N** | 切换到多人模式 | Switch to Network mode |
| **T** | 运行完整测试序列 | Run full test sequence |

##### 4. 自动化测试

启用`Enable Auto Testing`后，脚本会自动执行以下测试序列：
1. 显示菜单
2. 切换到设置面板
3. 切换到主面板
4. 隐藏菜单

##### 5. 事件监听

测试脚本会监听并记录以下事件：
- 菜单可见性改变
- 面板切换
- 游戏模式改变

#### 调试输出示例

```text
TableMenuSystemTest: Test script started
=== Table Menu System Test Instructions ===
Keyboard shortcuts:
M - Toggle menu display/hide
...
Event: Menu visibility changed - Visible
Event: Panel changed - Settings
Event: Game mode changed - Menu -> Local
```

#### 故障排除

**问题**: 键盘快捷键不响应

- **解决**: 确保测试对象处于激活状态，`Enable Keyboard Testing`已启用

**问题**: 找不到组件引用

- **解决**: 手动在Inspector中指定组件引用，或确保场景中存在对应组件

**问题**: 自动测试不执行

- **解决**: 检查`Enable Auto Testing`是否启用，`Auto Test Interval`是否大于0

---

## 🛠️ Editor脚本

### Unity菜单扩展

#### PongHub菜单项

通过Unity菜单栏访问：`PongHub -> Test -> Table Menu System`

**功能**: 快速创建TableMenuSystemTest对象并选中

**使用场景**:

- 新场景中快速添加测试功能
- 调试菜单系统问题
- 验证新功能

---

## 🔧 工具类

### VRUIHelper

**文件路径**: `Assets/PongHub/Scripts/UI/VRUIHelper.cs`

#### 功能概述

VR UI优化的静态工具类，提供统一的VR友好UI设置。

#### 常量定义

```csharp
// 字体大小常量
HEADER_FONT_SIZE = 36    // 标题字体
TITLE_FONT_SIZE = 32     // 副标题字体
BODY_FONT_SIZE = 24      // 正文字体
SMALL_FONT_SIZE = 20     // 小字体

// 按钮尺寸常量
MIN_BUTTON_WIDTH = 120f   // 最小按钮宽度
MIN_BUTTON_HEIGHT = 80f   // 最小按钮高度
LINE_SPACING = 1.5f       // 行间距
```

#### 颜色方案

```csharp
VR_WHITE              // 白色文本
VR_CYAN_HIGHLIGHT     // 青色高亮
VR_BLUE_PRESSED       // 蓝色按压
VR_YELLOW_SELECTED    // 黄色选中
VR_RED_DANGER         // 红色危险
VR_GREEN_SAFE         // 绿色安全
```

#### 核心方法

##### 1. ApplyVRFontSettings

```csharp
// 应用VR优化字体设置
VRUIHelper.ApplyVRFontSettings(textComponent, fontSize, fontStyle);

// 示例
VRUIHelper.ApplyVRFontSettings(titleText, VRUIHelper.TITLE_FONT_SIZE, FontStyle.Bold);
```

##### 2. ApplyVRButtonSettings

```csharp
// 应用VR友好按钮设置
VRUIHelper.ApplyVRButtonSettings(button, buttonType);

// 示例
VRUIHelper.ApplyVRButtonSettings(confirmButton, VRButtonType.Danger);
VRUIHelper.ApplyVRButtonSettings(cancelButton, VRButtonType.Safe);
```

##### 3. ApplyVRPanelSettings

```csharp
// 为整个面板应用VR设置
VRUIHelper.ApplyVRPanelSettings(panelGameObject);
```

##### 4. 自适应对比度

```csharp
// 获取自适应文本颜色
Color textColor = VRUIHelper.GetAdaptiveTextColor();

// 获取自适应背景颜色
Color backgroundColor = VRUIHelper.GetAdaptiveBackgroundColor();
```

#### 使用示例

##### 在MonoBehaviour中使用

```csharp
public class MyVRPanel : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Button actionButton;

    private void Start()
    {
        // 应用VR字体设置
        VRUIHelper.ApplyVRFontSettings(titleText, VRUIHelper.TITLE_FONT_SIZE, FontStyle.Bold);

        // 应用VR按钮设置
        VRUIHelper.ApplyVRButtonSettings(actionButton, VRButtonType.Normal);

        // 或者一次性应用到整个面板
        VRUIHelper.ApplyVRPanelSettings(gameObject);
    }
}
```

##### 按钮类型选择

```csharp
// 普通按钮
VRUIHelper.ApplyVRButtonSettings(playButton, VRButtonType.Normal);

// 危险操作按钮（红色主题）
VRUIHelper.ApplyVRButtonSettings(deleteButton, VRButtonType.Danger);

// 安全操作按钮（绿色主题）
VRUIHelper.ApplyVRButtonSettings(saveButton, VRButtonType.Safe);
```

---

## 🔄 开发工作流程

### 新UI面板开发流程

#### 1. 创建面板脚本

```csharp
public class NewPanel : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    [Tooltip("Description of the component")]
    private Button myButton;

    private void Start()
    {
        // 应用VR UI设置
        VRUIHelper.ApplyVRPanelSettings(gameObject);
    }
}
```

#### 2. 添加测试支持

- 在TableMenuSystemTest中添加新面板的测试快捷键
- 创建专门的测试方法

#### 3. 验证VR友好性

- 使用VRUIHelper确保按钮尺寸符合VR要求
- 测试射线交互的准确性
- 验证文本在VR中的可读性

### 调试最佳实践

#### 1. 使用测试脚本

```csharp
// 在开发过程中保持TableMenuSystemTest激活
// 使用键盘快捷键快速测试功能
```

#### 2. 日志输出

```csharp
Debug.Log("Panel action - 面板操作"); // 使用中英文结合的日志
```

#### 3. VR测试

- 在VR模式下验证菜单位置和尺寸
- 测试手柄射线交互的精确度
- 确认文本在头显中的清晰度

---

## 📝 注意事项

### 性能考虑

- VRUIHelper的方法设计为轻量级，可在运行时调用
- 自适应对比度会查找场景中的渲染器，避免频繁调用

### VR兼容性

- 所有UI设置都针对VR环境优化
- 按钮尺寸符合手柄射线交互要求
- 字体大小适合VR头显显示

### 测试建议

- 开发阶段保持测试脚本激活
- 定期运行完整测试序列
- 在真实VR设备上验证最终效果

---

## 🔗 相关文档

- [VR UI设计规则](../.cursor/rules/106-vr-table-menu-ui-design.mdc)
- [Unity Editor Tooltips规则](../.cursor/rules/107-unity-editor-tooltips.mdc)
- [英文优先语言规则](../.cursor/rules/105-english-first-language.mdc)
- [桌面菜单系统架构文档](../.ai/arch.md)

---
