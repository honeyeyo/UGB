# Ultimate Glove Ball - UI 系统架构和使用指南

## 📖 概述

Ultimate Glove Ball 的 UI 系统采用模块化设计，支持 VR 和传统输入方式，提供完整的游戏界面解决方案。系统基于 Unity UGUI 构建，集成 Meta Interaction 框架，确保在 VR 环境下的优良交互体验。

## 🏗️ 系统架构

### 核心设计原则

- **模块化管理**：每个 UI 面板独立管理，降低耦合度
- **统一状态管理**：通过 UIManager 集中控制 UI 状态
- **VR 优化**：专为 VR 交互设计的射线指向系统
- **可扩展性**：支持动态添加新的 UI 组件
- **性能优化**：合理的面板激活/停用管理

### 架构层次

```
UI系统架构
├── UIManager (核心管理器)
│   ├── 面板状态管理
│   ├── 场景切换控制
│   └── 事件分发
├── 功能面板层
│   ├── MainMenuPanel (主菜单)
│   ├── SettingsPanel (设置)
│   ├── PauseMenuPanel (暂停菜单)
│   ├── ScoreboardPanel (计分板)
│   └── InputSettingsPanel (输入设置)
├── 游戏内UI层
│   ├── GameplayHUD (游戏HUD)
│   ├── HUDPanel (通用HUD)
│   └── PongPhysicsDebugUI (调试UI)
└── 交互层
    └── CustomPointableCanvasModule (VR交互)
```

## 🔧 核心组件详解

### 1. UIManager - 统一 UI 管理器

**职责**：

- 管理所有 UI 面板的显示/隐藏
- 处理场景状态切换
- 协调不同 UI 组件间的交互

**关键特性**：

```csharp
public class UIManager : MonoBehaviour
{
    // 面板管理
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private PauseMenuPanel pauseMenuPanel;
    [SerializeField] private GameplayHUD gameplayHUD;

    // 状态管理
    public enum UIState { MainMenu, InGame, Paused, Settings }
    private UIState currentState;

    // 公共接口
    public void ShowPanel(string panelName);
    public void HideAllPanels();
    public void SetUIState(UIState state);
}
```

**使用方式**：

```csharp
// 切换到主菜单
uiManager.SetUIState(UIManager.UIState.MainMenu);

// 显示设置面板
uiManager.ShowPanel("Settings");

// 隐藏所有面板
uiManager.HideAllPanels();
```

### 2. MainMenuPanel - 主菜单面板

**功能**：

- 游戏启动界面
- 模式选择（单人/多人）
- 设置入口
- 退出游戏

**特点**：

- 支持 VR 射线交互
- 响应式布局设计
- 平滑的动画过渡

### 3. SettingsPanel - 设置面板

**功能**：

- 游戏参数配置
- 音频设置
- 图形设置
- 控制器配置

**配置项**：

```csharp
public class SettingsPanel : MonoBehaviour
{
    [Header("音频设置")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("图形设置")]
    public Dropdown qualityDropdown;
    public Toggle vsyncToggle;

    [Header("游戏设置")]
    public Slider difficultySlider;
    public Toggle tutorialToggle;
}
```

### 4. GameplayHUD - 游戏内 HUD

**功能**：

- 实时分数显示
- 游戏时间/回合信息
- 玩家状态指示
- 操作提示

**布局结构**：

```csharp
public class GameplayHUD : MonoBehaviour
{
    [Header("分数显示")]
    public Text leftPlayerScore;
    public Text rightPlayerScore;

    [Header("游戏信息")]
    public Text gameTimer;
    public Text roundInfo;

    [Header("状态指示")]
    public Image leftPlayerIndicator;
    public Image rightPlayerIndicator;

    [Header("提示信息")]
    public Text instructionText;
    public GameObject[] tipPanels;
}
```

### 5. InputSettingsPanel - 输入设置面板

**功能**：

- 球拍配置入口
- 移动参数调整
- 瞬移快捷按钮
- 输入测试工具

**集成特性**：

```csharp
public class InputSettingsPanel : MonoBehaviour
{
    [Header("球拍配置")]
    public Button leftPaddleConfigButton;
    public Button rightPaddleConfigButton;

    [Header("瞬移控制")]
    public Button[] teleportButtons;

    [Header("参数调整")]
    public Slider moveSpeedSlider;
    public Slider rotationSpeedSlider;
}
```

### 6. PongPhysicsDebugUI - 物理调试 UI

**功能**：

- 实时物理参数显示
- 球体状态监控
- 碰撞信息展示
- 性能指标显示

**调试特性**：

```csharp
public class PongPhysicsDebugUI : MonoBehaviour
{
    [Header("物理监控")]
    public Text ballVelocityText;
    public Text ballPositionText;
    public Text collisionCountText;

    [Header("性能监控")]
    public Text fpsText;
    public Text memoryUsageText;

    [Header("参数调整")]
    public Slider gravitySlider;
    public Slider bouncinessSlider;
    public Slider frictionSlider;
}
```

## 🎮 VR 交互系统

### CustomPointableCanvasModule

**功能**：

- VR 环境下的 UI 射线交互
- 自动切换鼠标/射线模式
- 与 Meta Interaction 集成

**工作原理**：

```csharp
public class CustomPointableCanvasModule : PointableCanvasModule
{
    public override bool IsModuleSupported()
    {
        // 检测VR设备状态，自动切换交互模式
        return XRSettings.isDeviceActive && base.IsModuleSupported();
    }
}
```

### 射线交互配置

**Canvas 设置**：

```
Canvas组件配置：
├── Render Mode: World Space
├── Event Camera: 指向XR Camera
├── Graphic Raycaster: 启用
└── Canvas Group: 控制交互性
```

**按钮交互**：

```csharp
// VR按钮配置示例
Button vrButton = GetComponent<Button>();
vrButton.onClick.AddListener(() => {
    // 添加触觉反馈
    OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);

    // 执行按钮功能
    OnButtonClicked();
});
```

## 📁 文件结构

```
Assets/UltimateGloveBall/Scripts/UI/
├── UIManager.cs                      # 统一UI管理器
├── MainMenuPanel.cs                  # 主菜单面板
├── SettingsPanel.cs                  # 设置面板
├── PauseMenuPanel.cs                 # 暂停菜单面板
├── GameplayHUD.cs                    # 游戏内HUD
├── HUDPanel.cs                       # 通用HUD面板
├── ScoreboardPanel.cs                # 计分板面板
├── InputSettingsPanel.cs             # 输入设置面板
└── PongPhysicsDebugUI.cs             # 物理调试UI

相关依赖：
├── Scripts/Input/
│   └── CustomPointableCanvasModule.cs  # VR交互模块
└── Packages/
    └── com.meta.utilities.input/        # Meta输入工具包
```

## 🛠️ 使用指南

### 1. 场景设置

**基础 UI 设置**：

```
UI根对象层次：
GameUI (Canvas)
├── UIManager (脚本)
├── MainMenuPanel
├── SettingsPanel
├── PauseMenuPanel
├── GameplayHUD
└── PhysicsDebugUI
```

**Canvas 配置**：

```csharp
Canvas canvas = GetComponent<Canvas>();
canvas.renderMode = RenderMode.WorldSpace;
canvas.worldCamera = Camera.main; // 或XR Camera
canvas.sortingOrder = 100;

// 设置Canvas尺寸和位置
RectTransform rectTransform = canvas.GetComponent<RectTransform>();
rectTransform.sizeDelta = new Vector2(1920, 1080);
rectTransform.position = new Vector3(0, 2, 5);
```

### 2. 面板管理

**创建新面板**：

```csharp
public class NewPanel : MonoBehaviour
{
    [Header("UI组件")]
    public Button[] buttons;
    public Text[] labels;

    private void Start()
    {
        // 初始化UI组件
        SetupUI();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        // 添加显示动画
    }

    public void Hide()
    {
        // 添加隐藏动画
        gameObject.SetActive(false);
    }
}
```

**注册到 UIManager**：

```csharp
// 在UIManager中添加新面板引用
[SerializeField] private NewPanel newPanel;

public void ShowNewPanel()
{
    HideAllPanels();
    newPanel.Show();
}
```

### 3. VR 适配

**按钮 VR 优化**：

```csharp
public class VRButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("VR反馈")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 射线悬停效果
        transform.localScale = Vector3.one * 1.1f;
        AudioSource.PlayClipAtPoint(hoverSound, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复原始状态
        transform.localScale = Vector3.one;
    }
}
```

**距离自适应**：

```csharp
public class DistanceBasedUI : MonoBehaviour
{
    [Header("距离设置")]
    public float minDistance = 1f;
    public float maxDistance = 5f;
    public AnimationCurve scaleCurve;

    private Transform playerHead;

    private void Update()
    {
        if (playerHead == null) return;

        float distance = Vector3.Distance(transform.position, playerHead.position);
        float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float scale = scaleCurve.Evaluate(normalizedDistance);

        transform.localScale = Vector3.one * scale;
    }
}
```

## 🎨 UI 设计规范

### 1. 视觉风格

**色彩方案**：

```css
主色调: #1E88E5 (蓝色)
辅色调: #FFC107 (琥珀色)
背景色: #263238 (深蓝灰)
文本色: #FFFFFF (白色)
强调色: #4CAF50 (绿色)
警告色: #FF5722 (橙红色)
```

**字体规范**：

```
标题: 24-36px, Bold
副标题: 18-24px, Medium
正文: 14-18px, Regular
说明文字: 12-14px, Light
```

### 2. 布局原则

**间距系统**：

```
基础单位: 8px
小间距: 8px
中间距: 16px
大间距: 24px
超大间距: 32px
```

**组件尺寸**：

```
按钮高度: 48px (最小点击区域)
输入框高度: 40px
图标尺寸: 24px, 32px, 48px
面板圆角: 8px
卡片阴影: 0 2px 8px rgba(0,0,0,0.1)
```

### 3. VR 交互优化

**可点击区域**：

- 最小点击区域：48x48px
- 按钮间距：至少 16px
- 射线交互反馈明显

**文字可读性**：

- VR 环境下字体放大 1.2-1.5 倍
- 高对比度配色
- 避免过小或过密的文字

## 🔧 扩展开发

### 1. 添加新面板

**步骤 1：创建面板脚本**

```csharp
using UnityEngine;
using UnityEngine.UI;
using PongHub.UI;

namespace PongHub.UI
{
    public class CustomPanel : MonoBehaviour
    {
        [Header("UI组件")]
        public Button closeButton;
        public Text titleText;

        private void Start()
        {
            SetupEvents();
        }

        private void SetupEvents()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

**步骤 2：集成到 UIManager**

```csharp
// 在UIManager中添加引用
[SerializeField] private CustomPanel customPanel;

public void ShowCustomPanel()
{
    HideAllPanels();
    customPanel.Show();
}
```

### 2. 自定义 VR 组件

**创建 VR 滑块**：

```csharp
public class VRSlider : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("VR设置")]
    public float hapticStrength = 0.1f;
    public AudioClip dragSound;

    private Slider slider;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 开始拖拽时的触觉反馈
        OVRInput.SetControllerVibration(hapticStrength, hapticStrength, OVRInput.Controller.RTouch);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽过程中的音效
        if (dragSound) AudioSource.PlayClipAtPoint(dragSound, transform.position, 0.3f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 结束拖拽
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }
}
```

## 📊 性能优化

### 1. Canvas 优化

**分层渲染**：

```csharp
// 静态UI使用单独Canvas
Canvas staticCanvas = staticUI.GetComponent<Canvas>();
staticCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

// 动态UI使用世界空间Canvas
Canvas dynamicCanvas = dynamicUI.GetComponent<Canvas>();
dynamicCanvas.renderMode = RenderMode.WorldSpace;
```

**合批优化**：

```csharp
// 使用相同材质的UI元素
[SerializeField] private Material uiMaterial;

private void OptimizeGraphics()
{
    Image[] images = GetComponentsInChildren<Image>();
    foreach (Image img in images)
    {
        img.material = uiMaterial;
    }
}
```

### 2. 内存管理

**对象池管理**：

```csharp
public class UIObjectPool : MonoBehaviour
{
    [Header("池配置")]
    public GameObject prefab;
    public int poolSize = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Start()
    {
        // 预创建对象
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject GetFromPool()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

## 🐛 调试工具

### 1. UI 状态监控

**状态显示器**：

```csharp
public class UIStateDebugger : MonoBehaviour
{
    [Header("调试显示")]
    public Text stateText;
    public Text activeCanvasText;
    public Text performanceText;

    private UIManager uiManager;

    private void Update()
    {
        if (uiManager != null)
        {
            stateText.text = $"UI State: {uiManager.CurrentState}";
            activeCanvasText.text = $"Active Panels: {GetActivePanelCount()}";
            performanceText.text = $"Draw Calls: {UnityStats.drawCalls}";
        }
    }
}
```

### 2. VR 交互测试

**射线可视化**：

```csharp
public class RaycastVisualizer : MonoBehaviour
{
    [Header("可视化设置")]
    public LineRenderer lineRenderer;
    public Color hitColor = Color.red;
    public Color missColor = Color.white;

    private void Update()
    {
        // 显示射线
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, hit.point);
            lineRenderer.color = hitColor;
        }
        else
        {
            lineRenderer.color = missColor;
        }
    }
}
```

## ⚠️ 注意事项

### 1. VR 最佳实践

- **避免快速移动的 UI 元素**：容易引起 VR 晕动症
- **保持 UI 距离适中**：1-3 米之间为最佳观看距离
- **提供充足的视觉反馈**：悬停、点击状态明确
- **使用世界空间 UI**：避免屏幕空间 UI 在 VR 中的问题

### 2. 性能注意事项

- **合理使用 Canvas**：避免所有 UI 在同一 Canvas 上
- **及时销毁不用的 UI**：避免内存泄漏
- **优化图片资源**：使用压缩纹理，合理的分辨率
- **减少 UI 重绘**：避免频繁的布局重计算

### 3. 兼容性考虑

- **支持多种输入方式**：鼠标、触摸、VR 控制器
- **响应式布局**：适配不同分辨率和屏幕比例
- **降级策略**：VR 不可用时的备用方案

## 📖 参考资源

### 相关文档

- [Input 系统实现.md](./Input系统实现.md) - 输入系统详细说明
- [Configuration.md](./Configuration.md) - 配置系统文档
- [CodeStructure.md](./CodeStructure.md) - 代码结构说明

### Unity 文档

- [UGUI 用户指南](https://docs.unity3d.com/Manual/UISystem.html)
- [VR 最佳实践](https://docs.unity3d.com/Manual/VROverview.html)
- [Meta Interaction SDK](https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/)

### 设计参考

- [Material Design](https://material.io/design) - 现代 UI 设计规范
- [VR 界面设计指南](https://developer.oculus.com/design/latest/concepts/design-intro/) - VR 特有的设计考虑

---

## 版本历史

| 版本 | 日期    | 更新内容           |
| ---- | ------- | ------------------ |
| v1.0 | 2024-01 | 初始 UI 系统架构   |
| v1.1 | 2024-02 | 添加 VR 交互支持   |
| v1.2 | 2024-03 | 性能优化和调试工具 |
| v1.3 | 2024-06 | 目录结构扁平化优化 |

本文档将随着 UI 系统的发展持续更新，确保与实际代码实现保持同步。
