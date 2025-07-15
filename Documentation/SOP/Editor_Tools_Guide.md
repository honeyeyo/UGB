# Editor 工具指南

<version>1.0.0</version>

本文档详细介绍PongHub VR项目中的Unity Editor扩展工具和开发辅助功能。

## 📋 目录

- [Unity菜单扩展](#unity菜单扩展)
- [Inspector增强](#inspector增强)
- [Scene视图工具](#scene视图工具)
- [自定义窗口](#自定义窗口)
- [开发辅助工具](#开发辅助工具)

---

## 🎛️ Unity菜单扩展

### PongHub菜单

通过Unity菜单栏 `PongHub` 访问项目专用工具。

#### 测试工具子菜单

**路径**: `PongHub -> Test`

##### Table Menu System

- **功能**: 创建桌面菜单系统测试对象
- **快捷键**: 无
- **位置**: `PongHub/Test/Table Menu System`

```csharp
[UnityEditor.MenuItem("PongHub/Test/Table Menu System")]
public static void CreateTestObject()
{
    var testObj = new GameObject("TableMenuSystemTest");
    testObj.AddComponent<TableMenuSystemTest>();
    UnityEditor.Selection.activeGameObject = testObj;
    Debug.Log("TableMenuSystemTest object created");
}
```

**使用场景**:

- 新建场景后快速添加测试功能
- 调试菜单系统问题时创建测试环境
- 验证新功能是否正常工作

**操作步骤**:

1. 打开需要测试的场景
2. 点击 `PongHub -> Test -> Table Menu System`
3. 自动创建并选中测试对象
4. 在Inspector中配置测试参数
5. 使用键盘快捷键进行测试

---

## 🔍 Inspector增强

### Tooltips系统

所有SerializeField字段都配备了详细的Tooltips，提供以下信息：

#### 信息类型

- **功能描述**: 字段的作用和用途
- **数值范围**: 推荐的数值范围或限制
- **单位说明**: 时间、距离、角度等单位
- **使用建议**: 最佳实践和推荐设置

#### 示例Tooltips

```csharp
[Tooltip("Animation duration for show/hide transitions in seconds")]
private float animationDuration = 0.3f;

[Tooltip("Master volume slider (0.0 = mute, 1.0 = full volume)")]
private Slider masterVolumeSlider;

[Tooltip("Minimum button size for VR ray-casting interaction (pixels)")]
private float minButtonSize = 80f;
```

#### 使用技巧

- **悬停查看**: 将鼠标悬停在字段标签上查看Tooltip
- **快速理解**: 无需查阅文档即可理解字段用途
- **配置指导**: 根据Tooltip建议设置合适的数值

---

## 🎨 Scene视图工具

### Gizmos可视化

#### TableMenuSystem Gizmos

**功能**: 在Scene视图中可视化菜单位置和尺寸

**显示内容**:

- 青色线框: 菜单在桌面上的投影区域
- 黄色连线: 从桌子中心到菜单位置的连接线

```csharp
private void OnDrawGizmos()
{
    if (tableTransform != null)
    {
        // Draw menu position preview
        Vector3 menuPosition = GetTableMenuPosition();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(menuPosition, new Vector3(menuSize.x, 0.01f, menuSize.y));

        // Draw connection line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(tableTransform.position, menuPosition);
    }
}
```

**使用方法**:

1. 在Scene视图中选中TableMenuSystem对象
2. 确保Gizmos显示已启用（Scene视图右上角Gizmos按钮）
3. 调整`menuOffset`和`menuSize`参数
4. 实时查看菜单位置和尺寸变化

**调试用途**:

- 验证菜单位置是否合适
- 确认菜单不会与其他对象重叠
- 优化菜单尺寸和偏移量

---

## 🛠️ 开发辅助工具

### VRUIHelper工具类

#### 快速应用VR设置

**在编辑器中使用**:

```csharp
[UnityEditor.MenuItem("PongHub/Tools/Apply VR Settings to Selected")]
public static void ApplyVRSettingsToSelected()
{
    foreach (GameObject obj in UnityEditor.Selection.gameObjects)
    {
        VRUIHelper.ApplyVRPanelSettings(obj);
    }
}
```

#### 批量字体大小调整

**功能**: 为选中的所有Text组件应用VR友好的字体设置

```csharp
[UnityEditor.MenuItem("PongHub/Tools/Fix Text Sizes for VR")]
public static void FixTextSizesForVR()
{
    Text[] allTexts = UnityEngine.Object.FindObjectsOfType<Text>();
    foreach (Text text in allTexts)
    {
        if (text.fontSize < VRUIHelper.BODY_FONT_SIZE)
        {
            text.fontSize = VRUIHelper.BODY_FONT_SIZE;
            UnityEditor.EditorUtility.SetDirty(text);
        }
    }
}
```

### 组件验证工具

#### 检查VR兼容性

**功能**: 验证UI组件是否符合VR设计标准

```csharp
[UnityEditor.MenuItem("PongHub/Validation/Check VR Compatibility")]
public static void CheckVRCompatibility()
{
    Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();

    foreach (Button button in buttons)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect.sizeDelta.x < VRUIHelper.MIN_BUTTON_WIDTH ||
            rect.sizeDelta.y < VRUIHelper.MIN_BUTTON_HEIGHT)
        {
            Debug.LogWarning($"Button {button.name} is too small for VR interaction", button);
        }
    }
}
```

---

## 📊 自定义窗口

### VR设置面板

**创建自定义Editor窗口**:

```csharp
public class VRSettingsWindow : UnityEditor.EditorWindow
{
    [UnityEditor.MenuItem("PongHub/Windows/VR Settings")]
    public static void ShowWindow()
    {
        GetWindow<VRSettingsWindow>("VR Settings");
    }

    private void OnGUI()
    {
        GUILayout.Label("VR UI Settings", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("Apply VR Font Sizes"))
        {
            // 应用VR字体设置到所有文本
        }

        if (GUILayout.Button("Validate Button Sizes"))
        {
            // 验证按钮尺寸
        }

        if (GUILayout.Button("Generate VR Test Scene"))
        {
            // 生成VR测试场景
        }
    }
}
```

---

## 🔧 开发工作流程

### 新功能开发流程

#### 1. 创建组件

```csharp
// 使用Tooltips标准
[SerializeField]
[Tooltip("Component description with usage guidelines")]
private ComponentType componentField;
```

#### 2. 添加Editor支持

```csharp
#if UNITY_EDITOR
[UnityEditor.MenuItem("PongHub/Test/New Feature")]
public static void TestNewFeature()
{
    // 创建测试环境
}
#endif
```

#### 3. 集成VR优化

```csharp
private void Start()
{
    VRUIHelper.ApplyVRPanelSettings(gameObject);
}
```

#### 4. 添加可视化

```csharp
private void OnDrawGizmos()
{
    // 绘制调试信息
}
```

### 测试和验证

#### 快速测试流程

1. 使用`PongHub -> Test -> Table Menu System`创建测试环境
2. 配置测试参数
3. 使用键盘快捷键测试功能
4. 检查Console输出和Gizmos显示

#### VR兼容性检查

1. 运行`PongHub -> Validation -> Check VR Compatibility`
2. 修复报告的问题
3. 在VR设备上验证最终效果

---

## 📝 最佳实践

### Editor脚本开发

#### 1. 使用条件编译

```csharp
#if UNITY_EDITOR
// Editor专用代码
#endif
```

#### 2. 提供撤销支持

```csharp
UnityEditor.Undo.RecordObject(target, "Action Description");
```

#### 3. 标记对象为Dirty

```csharp
UnityEditor.EditorUtility.SetDirty(target);
```

#### 4. 安全的对象查找

```csharp
if (target != null && target.gameObject != null)
{
    // 安全操作
}
```

### Gizmos绘制

#### 1. 使用合适的颜色

```csharp
Gizmos.color = Color.cyan;    // 信息性显示
Gizmos.color = Color.yellow;  // 连接线
Gizmos.color = Color.red;     // 警告或错误
```

#### 2. 条件性绘制

```csharp
private void OnDrawGizmos()
{
    if (Application.isPlaying) return; // 仅在编辑器中绘制
    // 绘制代码
}
```

### 菜单项组织

#### 1. 使用层级结构

```csharp
[UnityEditor.MenuItem("PongHub/Category/SubCategory/Action")]
```

#### 2. 添加快捷键

```csharp
[UnityEditor.MenuItem("PongHub/Action %t")] // Ctrl+T
```

#### 3. 验证菜单项

```csharp
[UnityEditor.MenuItem("PongHub/Action", true)]
public static bool ValidateAction()
{
    return Selection.activeGameObject != null;
}
```

---

## 🔗 相关资源

### Unity官方文档

- [Editor Scripting](https://docs.unity3d.com/Manual/ExtendingTheEditor.html)
- [Custom Editors](https://docs.unity3d.com/Manual/editor-CustomEditors.html)
- [Gizmos](https://docs.unity3d.com/ScriptReference/Gizmos.html)

### 项目相关文档

- [脚本使用指南](Scripts_Usage_Guide.md)
- [VR UI设计规则](../.cursor/rules/106-vr-table-menu-ui-design.mdc)
- [Unity Editor Tooltips规则](../.cursor/rules/107-unity-editor-tooltips.mdc)

---
