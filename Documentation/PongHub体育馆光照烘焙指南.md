# PongHub 体育馆光照烘焙指南

为了在PongHub项目中实现最佳性能，我们使用光照烘焙技术将光照信息烘焙到光照贴图、光照探针和反射探针中。

使用烘焙方向光为我们在GPU方面带来了显著的性能提升，因为生成阴影的实时计算成本被消除。

## 体育馆场景特点

PongHub的体育馆场景是一个室内环境，具有以下光照特征：

- **室内环境**：封闭的体育馆空间，主要依赖人工照明
- **多层光照**：天花板主光源 + 墙面辅助光源 + 重点照明
- **大空间**：体育馆的大空间需要合理的光照分布和阴影处理
- **装饰照明**：多个LightLum01_03系列的装饰灯具提供环境光

与原始Arena的户外半开放环境不同，体育馆的室内特性使我们能够更好地控制光照条件，获得更稳定的视觉效果。

## 光照系统架构

### 主要光源

**方向光 (Directional Light)**

- **位置**：(0, 3.545, 0)
- **旋转**：(28.247°, -82.492°, -4.368°)
- **强度**：2.0
- **颜色**：暖白色 (1, 0.957, 0.839)
- **光照模式**：烘焙 (Lightmapping: 1)
- **阴影**：硬阴影 (Shadow Type: 2)
- **间接光强度**：5.0

### 区域光源 (Area Lights)

体育馆配置了多个区域光源模拟天花板和墙面的照明设备：

**Area Light 配置示例**

- **类型**：区域光 (Type: 3)
- **强度**：5.0
- **颜色**：暖白色 (1, 0.999, 0.863)
- **尺寸**：3.2 x 3.57 单位
- **光照模式**：烘焙 (Lightmapping: 2)
- **间接光强度**：1.5
- **阴影**：关闭 (无实时阴影计算)

### 装饰照明

**LightLum01_03 系列灯具**

- 分布在体育馆各个位置
- 提供环境氛围照明
- 标记为Static以参与光照烘焙
- 优化性能的同时保持视觉效果

## 光照烘焙设置

### 当前烘焙配置

```text
Light Settings:
- GI Workflow Mode: 迭代式 (1)
- Enable Baked Lightmaps: ✓
- Enable Realtime Lightmaps: ✗

Lightmap Settings:
- Resolution: 2
- Bake Resolution: 40
- Atlas Size: 1024
- Mixed Bake Mode: 间接光照 (2)
- Backend: 渐进式GPU (1)
- Sampling: 1
- Direct Sample Count: 32
- Sample Count: 512
- Bounces: 2
```

### 光照探针配置

- **Light Probe Group** 已配置在场景中
- 为动态对象提供间接光照
- 优化VR环境中的光照一致性

## 如何烘焙体育馆光照

### 1. 加载场景

加载体育馆场景 [Assets/TirgamesAssets/SchoolGym/Gym.unity](../Assets/TirgamesAssets/SchoolGym/Gym.unity)

### 2. 找到LightingSetup对象

在场景层次结构中，您将看到一个名为 `LightingSetup-ApplyBefore GeneratingLight` 的禁用游戏对象。

![LightingSetup游戏对象](./Media/editor/baking_gameobject_location.png)

### 3. 配置烘焙设置

点击LightingSetup对象后，在检查器中使用上下文菜单来设置场景。

![LightingSetup上下文菜单](./Media/editor/baking_lightingsetup.png)

### 4. 烘焙前准备

选择 `Setup for Lighting` 将执行以下操作：

- **启用装饰物件**：激活参与光照计算的装饰对象
- **设置Static标记**：为所有静态物件设置适当的Static标记
- **配置体育器材**：SportLadder、SportBench、SportBrus等器材参与GI计算
- **优化照明设备**：LightLum01_03系列灯具的发射光设置

### 5. 执行光照烘焙

1. 打开 **Window → Rendering → Lighting**
2. 在Lighting窗口中点击 **Generate Lighting**
3. 等待烘焙完成

### 6. 烘焙后处理

完成后执行以下操作之一：

- **重新加载场景**：重新打开Gym.unity场景
- **使用还原选项**：选择 `Revert after lighting` 菜单选项

## 性能优化建议

### Static 标记优化

确保以下对象正确设置Static标记：

```text
建筑结构：
✓ 墙壁、地板、天花板
✓ 门窗结构

体育器材：
✓ SportLadder01b 系列
✓ SportBench01 系列
✓ SportBrus1 系列
✓ Door02_1 系列

装饰物件：
✓ LightLum01_03 系列灯具
✓ 其他静态装饰品
```

### 光照贴图优化

- **分辨率**：体育馆的大空间建议使用适中的光照贴图分辨率
- **图集大小**：根据场景复杂度调整到1024或更高
- **间接光照**：利用2次光线反弹获得真实的间接光照效果

### VR 性能考虑

- **阴影优化**：主要使用烘焙阴影，减少实时阴影计算
- **光照探针密度**：在玩家活动区域适当增加光照探针密度
- **LOD系统**：配合使用LOD系统优化远距离对象的渲染

## 故障排除

### 常见问题

**光照贴图出现接缝**

- 检查UV2通道是否正确展开
- 调整Lightmap Parameters设置
- 增加边缘填充(Padding)值

**阴影质量不佳**

- 提高Bake Resolution值
- 检查阴影投射设置
- 优化光源位置和强度

**性能问题**

- 检查是否有过多的实时光源
- 确认Static标记设置正确
- 考虑降低光照贴图分辨率

### 调试技巧

- 使用Scene视图的光照模式查看烘焙结果
- 检查Lighting窗口的统计信息
- 利用Frame Debugger分析光照计算

## 与Arena场景的差异

| 特性 | Arena (户外) | Gym (室内) |
|------|-------------|------------|
| 环境类型 | 半开放户外 | 封闭室内 |
| 主光源 | 模拟太阳光 | 室内顶灯 |
| 阴影复杂度 | 高(大面积) | 中等(受控) |
| 光照稳定性 | 受天气影响 | 稳定可控 |
| 性能需求 | 更高 | 相对较低 |

体育馆环境的室内特性为我们提供了更好的光照控制能力，使得烘焙结果更加可预测和稳定。