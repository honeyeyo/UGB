# PongHub CodeBind 命名规范

**版本**: 1.0  
**创建日期**: 2025-08-06  
**适用范围**: PongHub项目所有UI组件  

---

## 基础规范

### 分隔符配置
```csharp
[MonoCodeBind('_')]
public partial class ExampleUI : MonoBehaviour
{
    // 分隔符使用下划线 '_'
}
```

### 命名格式
**格式**: `功能描述_组件类型简写`

**示例**:
- `Title_Txt` → `TextMeshProUGUI TitleTxt`
- `BackButton_Btn` → `Button BackButtonBtn`
- `Container_Tr` → `Transform ContainerTr`
- `Panel_GO` → `GameObject PanelGO`

---

## 组件类型映射表

### UI组件
| 组件类型 | 简写 | 生成属性类型 | 示例 |
|---------|------|-------------|------|
| Button | `Btn` | `Button` | `ConfirmBtn` |
| TextMeshProUGUI | `Txt` | `TextMeshProUGUI` | `TitleTxt` |
| Image | `Img` | `Image` | `BackgroundImg` |
| Slider | `Sld` | `Slider` | `VolumeSlider` |
| Toggle | `Tgl` | `Toggle` | `EnabledTgl` |
| TMP_InputField | `IF` | `TMP_InputField` | `UsernameIF` |
| TMP_Dropdown | `DD` | `TMP_Dropdown` | `QualityDD` |
| ScrollRect | `SV` | `ScrollRect` | `ContentSV` |
| Canvas | `Canvas` | `Canvas` | `UICanvas` |
| CanvasGroup | `CG` | `CanvasGroup` | `MenuCG` |

### Transform组件
| 组件类型 | 简写 | 生成属性类型 | 示例 |
|---------|------|-------------|------|
| Transform | `Tr` | `Transform` | `ContainerTr` |
| RectTransform | `RT` | `RectTransform` | `PanelRT` |

### 通用组件
| 组件类型 | 简写 | 生成属性类型 | 示例 |
|---------|------|-------------|------|
| GameObject | `GO` | `GameObject` | `PanelGO` |
| Animator | `Anim` | `Animator` | `MenuAnim` |
| AudioSource | `Audio` | `AudioSource` | `BGMAudio` |

---

## PongHub特定规范

### UI面板命名
- 主面板：`MainPanel_GO`
- 子面板：`SettingsSubPanel_GO`
- 弹窗：`ConfirmDialog_GO`
- 容器：`ButtonsContainer_Tr`

### VR相关组件
| 功能 | 命名示例 | 说明 |
|------|----------|------|
| VR交互点 | `InteractionPoint_Tr` | VR射线交互位置 |
| VR菜单距离检测 | `DistanceChecker_GO` | VR菜单距离警告 |
| VR控制器引用 | `LeftController_Tr` | 左控制器Transform |
| VR控制器引用 | `RightController_Tr` | 右控制器Transform |

### 游戏特定组件
| 功能 | 命名示例 | 说明 |
|------|----------|------|
| 模式选择 | `SinglePlayerMode_Btn` | 单人模式按钮 |
| 模式选择 | `MultiplayerMode_Btn` | 多人模式按钮 |
| 设置项 | `AudioVolume_Sld` | 音频音量滑块 |
| 设置项 | `GraphicsQuality_DD` | 图形质量下拉框 |
| 玩家信息 | `PlayerName_Txt` | 玩家姓名文本 |
| 游戏数据 | `Score_Txt` | 分数显示文本 |

---

## 特殊情况处理

### 1. 数组和列表组件
**格式**: 使用数字后缀区分
```
PlayerSlot(1)_GO → GameObject PlayerSlot1GO
PlayerSlot(2)_GO → GameObject PlayerSlot2GO
PlayerSlot(3)_GO → GameObject PlayerSlot3GO
```

### 2. 嵌套UI处理
**父面板**:
```csharp
[MonoCodeBind('_')]
public partial class SettingsMainPanel : MonoBehaviour
{
    // 只绑定直接子对象，不绑定子面板内的组件
    // AudioSubPanel_GO → GameObject AudioSubPanelGO
    // VideoSubPanel_GO → GameObject VideoSubPanelGO
}
```

**子面板独立绑定**:
```csharp
[MonoCodeBind('_')]
public partial class AudioSettingsSubPanel : MonoBehaviour
{
    // 子面板内部组件独立绑定
    // MasterVolume_Sld → Slider MasterVolumeSld
    // MusicVolume_Sld → Slider MusicVolumeSld
}
```

### 3. 动态UI组件
**静态容器绑定**:
```csharp
[MonoCodeBind('_')]
public partial class RoomListPanel : MonoBehaviour
{
    // 静态容器和预制体引用
    // RoomList_Container_Tr → Transform RoomListContainerTr
    // RoomItem_Prefab_GO → GameObject RoomItemPrefabGO
    
    void CreateRoomItem()
    {
        // 动态实例化不使用CodeBind
        var item = Instantiate(RoomItemPrefabGO, RoomListContainerTr);
    }
}
```

---

## 命名最佳实践

### ✅ 推荐做法
- **功能明确**: `ConfirmAction_Btn` 而不是 `Button1_Btn`
- **层级清晰**: `Settings_Audio_Volume_Sld`
- **简洁有意义**: `PlayerName_Txt` 而不是 `PlayerNameDisplay_Txt`
- **一致性**: 同类组件使用相同的命名模式

### ❌ 避免做法
- **使用数字编号**: `Button1_Btn`, `Text2_Txt`
- **过长的名称**: `VeryLongDescriptiveButtonName_Btn`
- **无意义缩写**: `Btn1_Btn`, `Txt2_Txt`
- **混用分隔符**: `Button-Name_Btn` 或 `Button.Name_Btn`

---

## CodeBind配置代码

### 创建配置文件
```csharp
// Assets/PongHub/Scripts/Editor/PongHubCodeBindConfig.cs
using CodeBind;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PongHubCodeBindConfig", menuName = "PongHub/CodeBind Config")]
public class PongHubCodeBindConfig : ScriptableObject
{
    [CodeBindNameType("Btn", typeof(Button))]
    [CodeBindNameType("Txt", typeof(TextMeshProUGUI))]
    [CodeBindNameType("Img", typeof(Image))]
    [CodeBindNameType("Sld", typeof(Slider))]
    [CodeBindNameType("Tgl", typeof(Toggle))]
    [CodeBindNameType("IF", typeof(TMP_InputField))]
    [CodeBindNameType("DD", typeof(TMP_Dropdown))]
    [CodeBindNameType("SV", typeof(ScrollRect))]
    [CodeBindNameType("Tr", typeof(Transform))]
    [CodeBindNameType("RT", typeof(RectTransform))]
    [CodeBindNameType("GO", typeof(GameObject))]
    [CodeBindNameType("Canvas", typeof(Canvas))]
    [CodeBindNameType("CG", typeof(CanvasGroup))]
    [CodeBindNameType("Anim", typeof(Animator))]
    [CodeBindNameType("Audio", typeof(AudioSource))]
    public void ConfigurePongHubTypes()
    {
        // 配置方法体 - 由CodeBind自动处理
    }
}
```

---

## 验证和质量检查

### 自动检查脚本
```csharp
// 命名规范验证器
public static class CodeBindNamingValidator
{
    public static bool ValidateNaming(string nodeName)
    {
        // 检查是否包含分隔符
        if (!nodeName.Contains("_")) return false;
        
        // 检查分隔符数量
        var parts = nodeName.Split('_');
        if (parts.Length < 2) return false;
        
        // 检查组件类型是否在允许列表中
        var componentType = parts[parts.Length - 1];
        var allowedTypes = new[] { "Btn", "Txt", "Img", "Sld", "Tgl", "IF", "DD", "SV", "Tr", "RT", "GO", "Canvas", "CG", "Anim", "Audio" };
        
        return allowedTypes.Contains(componentType);
    }
}
```

---

## 示例UI实现

### 典型的设置面板示例
```csharp
[MonoCodeBind('_')]
public partial class VideoSettingsPanel : MonoBehaviour
{
    // 自动生成的属性:
    // public Button ApplyBtn { get; private set; }
    // public Button ResetBtn { get; private set; }
    // public TMP_Dropdown ResolutionDD { get; private set; }
    // public TMP_Dropdown QualityDD { get; private set; }
    // public Toggle FullscreenTgl { get; private set; }
    // public Slider BrightnessSlider { get; private set; }
    
    private void Start()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        ApplyBtn.onClick.AddListener(OnApplyClicked);
        ResetBtn.onClick.AddListener(OnResetClicked);
        FullscreenTgl.onValueChanged.AddListener(OnFullscreenToggled);
        BrightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
    }
    
    private void OnApplyClicked()
    {
        // 应用设置逻辑
    }
    
    private void OnResetClicked()
    {
        // 重置设置逻辑
    }
    
    private void OnFullscreenToggled(bool enabled)
    {
        // 全屏切换逻辑
    }
    
    private void OnBrightnessChanged(float value)
    {
        // 亮度调整逻辑
    }
}
```

---

## 后续计划

### Phase 1: 试点验证
- [ ] 选择2-3个复杂UI进行命名规范应用
- [ ] 验证自动生成代码的正确性
- [ ] 收集团队反馈并优化规范

### Phase 2: 批量应用
- [ ] 应用到所有核心UI面板
- [ ] 建立自动化检查工具
- [ ] 制定代码审查清单

### Phase 3: 持续优化
- [ ] 根据使用经验调整规范
- [ ] 添加更多组件类型支持
- [ ] 建立最佳实践库

---

**总结**: 这套命名规范为PongHub项目提供了统一、清晰、可维护的UI组件绑定标准。通过严格遵循这些规范，可以显著提升开发效率并降低维护成本。