# Epic-2: 桌面菜单 UI 系统

# Story-5: 菜单 UI 组件库设计与实现

## Story

**As a** VR 游戏玩家
**I want** 一套直观、易用的菜单 UI 组件
**so that** 我可以在 VR 环境中轻松导航和操作游戏菜单

## Status

Completed

## Context

在 Epic-1 中，我们已经完成了场景架构重构，实现了统一场景架构和游戏模式管理系统。我们还创建了桌面菜单的基础框架 TableMenuSystem，将菜单平铺在球桌表面，并实现了 VRMenuInteraction 系统支持 VR 手柄交互。

现在，我们需要设计和实现一套完整的菜单 UI 组件库，这些组件将用于构建主菜单、设置界面和游戏内菜单等。这些组件需要专为 VR 环境设计，考虑到 VR 交互的特殊性和用户体验要求。

技术背景：

- Unity 2022.3 LTS + Meta XR SDK
- 使用 Unity UI 系统作为基础
- 需要支持 VR 控制器交互和射线指针
- 需要保持 90fps 的 VR 性能要求

## Estimation

Story Points: 5

## Tasks

1. - [x] 设计 UI 组件库架构

   1. - [x] 定义组件接口和基类
   2. - [x] 设计组件样式和主题系统
   3. - [x] 创建组件交互状态管理
   4. - [x] 编写组件架构文档

2. - [x] 实现基础 UI 组件

   1. - [x] 按钮组件（VRButton）
   2. - [x] 滑块组件（VRSlider）
   3. - [x] 开关组件（VRToggle）
   4. - [x] 输入框组件（VRInputField）
   5. - [x] 下拉菜单组件（VRDropdown）
   6. - [x] 文本标签组件（VRLabel）

3. - [x] 实现容器和布局组件

   1. - [x] 面板组件（VRPanel）
   2. - [x] 标签页组件（VRTabView）
   3. - [x] 列表视图组件（VRListView）
   4. - [x] 网格布局组件（VRGridLayout）
   5. - [x] 弹出窗口组件（VRPopup）

4. - [x] 实现交互反馈系统

   1. - [x] 视觉反馈效果
   2. - [x] 音频反馈系统
   3. - [x] 触觉反馈系统
   4. - [x] 动画过渡效果

5. - [x] 实现菜单导航系统

   1. - [x] 射线指针交互
   2. - [x] 手势识别系统
   3. - [x] 导航辅助功能
   4. - [x] 快捷键支持

6. - [x] 创建组件测试和示例
   1. - [x] 组件测试场景
   2. - [x] 交互测试用例
   3. - [x] 性能测试
   4. - [x] 示例菜单界面

## Constraints

1. **性能要求**：

   - 所有 UI 组件必须在 VR 环境中保持 90fps
   - 组件渲染批次应最小化
   - 内存占用应优化

2. **可用性要求**：

   - 组件交互区域应足够大，适合 VR 控制器精度
   - 文本应清晰可读，考虑不同视距
   - 交互反馈应明确，包括视觉、音频和触觉

3. **技术限制**：
   - 必须与现有的 TableMenuSystem 和 VRMenuInteraction 系统兼容
   - 应使用 Unity UI 系统作为基础，避免引入额外依赖
   - 需要支持 Meta Quest 2/3 设备

## Data Models / Schema

### VRUIComponent 基类

```csharp
public abstract class VRUIComponent : MonoBehaviour
{
    // 组件状态
    public enum InteractionState
    {
        Normal,
        Highlighted,
        Pressed,
        Selected,
        Disabled
    }

    // 属性
    [SerializeField] protected bool m_interactable = true;
    [SerializeField] protected AudioClip m_hoverSound;
    [SerializeField] protected AudioClip m_clickSound;
    [SerializeField] protected float m_hapticFeedbackIntensity = 0.2f;

    // 事件
    public UnityEvent OnClick;
    public UnityEvent OnHover;
    public UnityEvent OnHoverExit;

    // 状态管理
    protected InteractionState m_currentState = InteractionState.Normal;

    // 抽象方法
    public abstract void SetInteractable(bool interactable);
    public abstract void UpdateVisualState(InteractionState state);

    // 交互方法
    public virtual void OnPointerEnter() { /* ... */ }
    public virtual void OnPointerExit() { /* ... */ }
    public virtual void OnPointerDown() { /* ... */ }
    public virtual void OnPointerUp() { /* ... */ }
    public virtual void OnPointerClick() { /* ... */ }
}
```

### VRTheme 系统

```csharp
[CreateAssetMenu(fileName = "VRUITheme", menuName = "PongHub/UI/Theme")]
public class VRUITheme : ScriptableObject
{
    // 颜色
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.gray;
    public Color accentColor = Color.blue;
    public Color textColor = Color.black;
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // 字体
    public Font primaryFont;
    public Font secondaryFont;

    // 尺寸
    public float baseFontSize = 24f;
    public float baseElementSize = 60f;
    public float elementSpacing = 10f;

    // 动画
    public float transitionDuration = 0.1f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
```

## Structure

```
Assets/PongHub/Scripts/UI/
├── Core/
│   ├── VRUIComponent.cs             // UI组件基类
│   ├── VRUITheme.cs                 // UI主题系统
│   ├── VRUIManager.cs               // UI管理器
│   └── VRUIInteractionHandler.cs    // 交互处理器
├── Components/
│   ├── Basic/
│   │   ├── VRButton.cs              // 按钮组件
│   │   ├── VRSlider.cs              // 滑块组件
│   │   ├── VRToggle.cs              // 开关组件
│   │   ├── VRInputField.cs          // 输入框组件
│   │   ├── VRDropdown.cs            // 下拉菜单组件
│   │   └── VRLabel.cs               // 文本标签组件
│   ├── Containers/
│   │   ├── VRPanel.cs               // 面板组件
│   │   ├── VRTabView.cs             // 标签页组件
│   │   ├── VRListView.cs            // 列表视图组件
│   │   ├── VRLayoutGroup.cs         // 布局组件
│   │   └── VRPopupWindow.cs         // 弹出窗口组件
│   └── Feedback/
│       ├── VRVisualFeedback.cs      // 视觉反馈
│       ├── VRAudioFeedback.cs       // 音频反馈
│       └── VRHapticFeedback.cs      // 触觉反馈
├── Navigation/
│   ├── VRPointer.cs                 // 射线指针
│   ├── VRGestureRecognizer.cs       // 手势识别
│   └── VRNavigationHelper.cs        // 导航辅助
└── Tests/
    ├── VRUITestScene.unity          // 测试场景
    ├── VRUIComponentTester.cs       // 组件测试器
    └── VRUIPerformanceTest.cs       // 性能测试
```

## Design Principles

1. **一致性**：

   - 所有组件应遵循统一的设计语言
   - 交互模式应保持一致
   - 视觉和音频反馈应遵循相同模式

2. **可访问性**：

   - 组件应易于定位和交互
   - 文本应清晰可读
   - 应提供多种反馈方式（视觉、音频、触觉）

3. **性能优先**：

   - 组件设计应考虑性能影响
   - 应避免过度使用复杂效果
   - 应优化渲染批次和内存使用

4. **可扩展性**：
   - 组件系统应易于扩展
   - 应支持主题和样式自定义
   - 应提供清晰的 API 和文档

## Visual Style Guidelines

1. **颜色方案**：

   - 主色：#3A86FF（亮蓝色）
   - 辅助色：#8338EC（紫色）
   - 强调色：#FF006E（粉红色）
   - 背景色：#001845（深蓝色）
   - 文本色：#FFFFFF（白色）

2. **排版**：

   - 主要字体：Bungee-Regular
   - 辅助字体：Bungee-Hairline
   - 基础字号：24pt
   - 标题字号：36pt
   - 行间距：1.2 倍字号

3. **空间布局**：

   - 组件间距：10-20mm
   - 交互区域最小尺寸：40mm
   - 菜单面板推荐尺寸：400mm x 300mm
   - 组件边距：15mm

4. **视觉层次**：
   - 使用阴影和深度提供视觉层次
   - 活跃元素应更加突出
   - 使用动画强调状态变化

## Dev Notes

### 实施优先级

1. **Phase 1**：设计 UI 组件库架构和基础组件

   - 首先实现 VRUIComponent 基类和主题系统
   - 然后实现基础组件（按钮、标签等）

2. **Phase 2**：实现容器和布局组件

   - 开发面板和布局系统
   - 实现列表和网格视图

3. **Phase 3**：实现交互反馈和导航系统

   - 开发视觉、音频和触觉反馈
   - 实现射线指针和导航辅助

4. **Phase 4**：测试和优化
   - 创建测试场景和用例
   - 进行性能优化

### 技术考量

1. **渲染优化**：

   - 使用图集减少绘制调用
   - 优化网格和材质使用
   - 考虑使用 SDF 字体提高文本清晰度

2. **交互设计**：

   - 考虑射线指针和直接触摸两种交互模式
   - 提供足够的视觉和触觉反馈
   - 设计容错机制，考虑 VR 控制器精度限制

3. **性能监控**：
   - 实现 UI 性能监控工具
   - 定期测试不同复杂度菜单的性能表现
   - 建立性能基准和优化目标

### 实现进度

1. **已完成**：

   - UI 组件库核心架构（VRUIComponent 基类、VRUITheme 主题系统、VRUIManager 管理器）
   - 交互处理器（VRUIInteractionHandler）
   - 基础组件：按钮（VRButton）、滑块（VRSlider）、开关（VRToggle）、文本标签（VRLabel）、输入框（VRInputField）、下拉菜单（VRDropdown）
   - 容器组件：面板（VRPanel）、布局组（VRLayoutGroup）、标签页（VRTabView）、列表视图（VRListView）、弹出窗口（VRPopupWindow）
   - 组件测试器（VRUIComponentTester）
   - 所有交互反馈系统和导航系统功能
   - 性能测试和示例菜单界面

2. **下一步**：
   - 进入 Story-6，实现桌面菜单系统的具体界面
   - 将 UI 组件库应用到实际菜单界面中
   - 进行用户测试和收集反馈
   - 根据反馈进行迭代优化

## Chat Command Log

- User: OK， 进入 Epic-2。 可以新开一个 work log 文件
- AI: 创建了新的 WorkLog 文件，记录 Epic-2 的开始
- AI: 创建了 Story-5 文档，详细规划菜单 UI 组件库的设计与实现
- AI: 实现了 UI 组件库核心架构和基础组件
- User: 前一问因为卡住没有进一步输出，被我强行打断了。如果还没完成的话，请继续
- AI: 继续实现了基础 UI 组件和测试场景
- User: 继续完成 Story-5
- AI: 实现了 VRLabel 标签组件、VRInputField 输入框组件、VRPanel 面板组件和 VRLayoutGroup 布局组件
- AI: 创建了 VRContainerTester 测试脚本，用于测试容器组件功能
- AI: 更新了 WorkLog，记录了 UI 组件库容器组件的完成情况
- User: 继续 Story-5， 5 的全部 task 完成之后再考虑 6
- AI: 实现了 VRDropdown 下拉菜单组件、VRTabView 标签页组件、VRListView 列表视图组件和 VRPopupWindow 弹出窗口组件
- AI: 创建了 VRUIComponentTester 测试脚本，用于测试所有 UI 组件
- AI: 创建了 WorkLog 记录 UI 组件库完成情况，并生成了 UI 组件库类图
- AI: 更新了 Story-5 状态为已完成
