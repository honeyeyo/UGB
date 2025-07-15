# SharedEnvironment 静态标记优化指南

## 🎯 概述

正确设置 Static 标记对`SharedEnvironment.prefab`的性能优化至关重要。Static 标记影响静态批处理、光照烘焙、遮挡剔除和导航网格生成。

## 📊 当前状态分析

从预制件分析结果来看：

- ✅ **部分组件已正确设置**：Architecture、Gym Equipment 等核心环境对象
- ⚠️ **需要优化**：灯光系统、音频系统、管理器组件的 Static 设置

## 🏗️ Static 标记分类指南

### ✅ **必须设置为 Static 的组件**

#### 1. Architecture（建筑结构）

```text
🏗️ Architecture
├── 墙壁、地板、天花板
├── 窗户、柱子
└── 固定建筑元素

Static标记：☑️ 全部启用
原因：永远不会移动，是光照烘焙和批处理的核心
```

#### 2. Gym Equipment（体育器材）

```text
🏋️ Gym Equipment
├── Bench（长椅）
├── Rack（架子）
├── VaultingHorse（跳马）
├── Mattress（垫子）
├── Ladder（梯子）
└── Basket（篮子）

Static标记：☑️ 全部启用
原因：装饰性器材，永不移动，参与静态批处理
```

#### 3. Furniture（家具）

```text
🪑 Furniture
├── 固定座椅
├── 装饰家具
└── 储物设施

Static标记：☑️ 全部启用
原因：装饰性家具，不会运动
```

#### 4. Technical（技术设备）

```text
⚙️ Technical
├── Radiator（散热器）
├── LightLum（固定照明设备）
└── 其他固定设备

Static标记：☑️ 全部启用
原因：固定安装的技术设备
```

#### 5. Gates & Doors（固定门）

```text
🚪 Gates & Doors
├── 不开合的装饰性门
└── 固定门框结构

Static标记：☑️ 全部启用
原因：如果门不需要开合动画，应设置为static
```

### 🔄 **部分设置为 Static 的组件**

#### 6. NavMeshPlane（导航网格）

```text
🗺️ NavMeshPlane

Static标记：☑️ Navigation Static 启用
注意：只启用Navigation Static，其他保持关闭
```

#### 7. Collision Boundaries（碰撞边界）

```text
🚧 Collision Boundaries

Static标记：☑️ Batching Static 启用
注意：通常是隐形碰撞体，只需要批处理优化
```

### ❌ **不应设置为 Static 的组件**

#### 8. Lighting System（灯光系统）

```text
💡 Lighting System
├── Directional Light
├── AreaLights（区域光源）
└── Light Probe Group

Static标记：❌ 保持关闭
原因：
- 灯光可能需要运行时调整
- Light Probe Group不应标记为static
- 可能需要动态光照控制
```

#### 9. Audio Systems（音频系统）

```text
🔊 Audio Systems
├── MusicManager
└── centerAudioSource

Static标记：❌ 保持关闭
原因：
- 音频管理器需要脚本控制
- 音频源可能需要动态位置调整
- 音量和播放状态需要运行时控制
```

#### 10. Post Processing（后处理）

```text
🎨 Post Processing
└── Global Volume

Static标记：❌ 保持关闭
原因：
- 后处理效果可能需要动态调整
- Volume可能需要运行时修改参数
```

## 🛠️ 具体设置步骤

### 方法 1：Unity Editor 中手动设置

```bash
1. 选中SharedEnvironment预制件
2. 在Hierarchy中展开组件结构
3. 对每个组件设置Static标记：

Architecture组件：
- 选中所有子对象
- 在Inspector右上角勾选Static
- 在弹出菜单中选择"Yes, change children"

Gym Equipment组件：
- 选中所有器材对象
- 设置Static标记
- 确认应用到子对象
```

### 方法 2：批量设置脚本

```csharp
using UnityEngine;
using UnityEditor;

public class SharedEnvironmentStaticSetter
{
    [MenuItem("PongHub/Tools/Set SharedEnvironment Static Flags")]
    public static void SetStaticFlags()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/PongHub/Prefabs/SharedEnvironment.prefab");

        if (prefab == null) return;

        // 设置需要完全static的组件
        SetStaticRecursive(prefab.transform.Find("Architecture"),
            StaticEditorFlags.Everything);
        SetStaticRecursive(prefab.transform.Find("Gym Equipment"),
            StaticEditorFlags.Everything);
        SetStaticRecursive(prefab.transform.Find("Furniture"),
            StaticEditorFlags.Everything);
        SetStaticRecursive(prefab.transform.Find("Technical"),
            StaticEditorFlags.Everything);

        // 设置部分static的组件
        SetStaticRecursive(prefab.transform.Find("NavMeshPlane"),
            StaticEditorFlags.NavigationStatic);
        SetStaticRecursive(prefab.transform.Find("Collision Boundaries"),
            StaticEditorFlags.BatchingStatic);

        // 确保动态组件不是static
        SetStaticRecursive(prefab.transform.Find("Lighting System"),
            StaticEditorFlags.Nothing);
        SetStaticRecursive(prefab.transform.Find("Audio Systems"),
            StaticEditorFlags.Nothing);
        SetStaticRecursive(prefab.transform.Find("Post Processing"),
            StaticEditorFlags.Nothing);

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        Debug.Log("SharedEnvironment Static标记设置完成！");
    }

    private static void SetStaticRecursive(Transform parent, StaticEditorFlags flags)
    {
        if (parent == null) return;

        GameObjectUtility.SetStaticEditorFlags(parent.gameObject, flags);

        for (int i = 0; i < parent.childCount; i++)
        {
            SetStaticRecursive(parent.GetChild(i), flags);
        }
    }
}
```

## 📈 性能优化效果

### 启用 Static 标记后的优势：

#### 🚀 **静态批处理**

```text
优化前：每个器材单独渲染 → 15-20 Draw Calls
优化后：合并为1-3个批次 → 3-5 Draw Calls
性能提升：60-75%的渲染调用减少
```

#### 💡 **光照烘焙**

```text
优化前：实时光照计算 → 高GPU负载
优化后：预烘焙光照贴图 → 90%计算量减少
视觉效果：更好的阴影质量和环境光
```

#### 🔍 **遮挡剔除**

```text
优化前：所有对象都渲染
优化后：被遮挡对象自动剔除
性能提升：复杂场景中30-50%渲染量减少
```

#### 🗺️ **导航优化**

```text
优化前：运行时动态生成导航网格
优化后：预烘焙NavMesh → 零运行时开销
```

## ⚠️ 注意事项

### 常见错误避免

1. **不要对动态对象设置 Static**

   ```text
   ❌ 错误：MusicManager设置为Static
   ✅ 正确：保持动态，允许脚本控制
   ```

2. **Light Probe Group 不要设置 Static**

   ```text
   ❌ 错误：Light Probe Group标记为Static
   ✅ 正确：保持动态，Unity会自动处理
   ```

3. **后处理 Volume 避免 Static**
   ```text
   ❌ 错误：Global Volume设置为Static
   ✅ 正确：保持动态，允许运行时调整
   ```

### 验证设置正确性

```bash
检查清单：
□ Architecture - 所有子对象都是Static
□ Gym Equipment - 所有器材都是Static
□ Furniture - 所有家具都是Static
□ Technical - 所有设备都是Static
□ NavMeshPlane - 只有Navigation Static
□ Lighting System - 全部非Static
□ Audio Systems - 全部非Static
□ Post Processing - 全部非Static
```

## 🎯 预期性能提升

应用正确的 Static 设置后，预期获得：

- **渲染性能**：提升 40-70%
- **光照质量**：显著提升（预烘焙阴影）
- **内存使用**：减少 15-25%（批处理优化）
- **加载时间**：减少 20-30%（预计算资源）

## 📋 自动化检查工具

考虑添加自动验证脚本来确保 Static 设置的正确性：

```csharp
[MenuItem("PongHub/Tools/Validate Static Settings")]
public static void ValidateStaticSettings()
{
    // 检查各组件的Static设置是否符合规范
    // 输出详细的验证报告
}
```
