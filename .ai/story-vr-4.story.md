# Story VR-4: 实现Passthrough混合现实功能

**Story ID**: VR-4  
**Epic**: VR交互系统增强优化  
**状态**: 开始实施  
**优先级**: 高  
**预估时间**: 2天  
**分配给**: AI开发助手  
**创建日期**: 2025-08-01  

## Story概述

基于Meta XR SDK v72.0.0的OVRPassthroughLayer组件，实现混合现实(Mixed Reality)功能，让玩家能够在真实环境中进行乒乓球游戏。支持透视模式切换、环境遮挡、安全边界等功能，为PongHub提供创新的MR游戏体验。

## 用户故事

**作为** VR乒乓球游戏的玩家  
**我希望** 能够在真实环境中看到虚拟乒乓球桌和球  
**以便于** 获得更加真实和安全的游戏体验，可以在自己的房间里进行乒乓球游戏，同时保持对周围环境的感知  

## 验收标准

### 功能要求
- [ ] **Passthrough启用**: 支持启用/禁用摄像头透视功能
- [ ] **透视模式切换**: 支持Full Passthrough和Selective Passthrough模式
- [ ] **环境融合**: 虚拟乒乓球桌与真实环境的自然融合
- [ ] **遮挡处理**: 真实物体对虚拟对象的正确遮挡
- [ ] **安全边界**: 在MR模式下的安全区域提醒
- [ ] **光照匹配**: 虚拟对象与真实环境的光照一致性
- [ ] **性能优化**: MR模式下保持稳定帧率

### 技术要求
- [ ] 基于OVRPassthroughLayer实现
- [ ] 与现有VRInteractionManager集成
- [ ] 支持运行时动态切换VR/MR模式
- [ ] 兼容Hand Tracking和控制器交互
- [ ] 场景遮挡和深度测试正确处理
- [ ] 内存和性能影响最小化

### 用户体验要求
- [ ] MR模式切换平滑自然，无明显卡顿
- [ ] 透视画面清晰，延迟<40ms
- [ ] 虚拟对象在真实环境中稳定显示
- [ ] 提供清晰的模式状态指示
- [ ] 支持不同房间环境的适配

## 技术实现设计

### 1. MRPassthroughManager架构
```csharp
public class MRPassthroughManager : MonoBehaviour
{
    public enum PassthroughMode
    {
        Disabled,           // 关闭透视，纯VR模式
        FullPassthrough,    // 全透视，完全MR模式
        SelectivePassthrough // 选择性透视，混合模式
    }
    
    [Header("Passthrough Settings")]
    [SerializeField] private bool m_enablePassthrough = false;
    [SerializeField] private PassthroughMode m_passthroughMode = PassthroughMode.Disabled;
    [SerializeField] private float m_passthroughOpacity = 1.0f;
    [SerializeField] private bool m_enableColorMapping = true;
    
    // Meta SDK组件
    private OVRPassthroughLayer m_passthroughLayer;
    private OVRCameraRig m_cameraRig;
    
    // 场景管理
    private List<GameObject> m_virtualObjects = new List<GameObject>();
    private Dictionary<GameObject, Material[]> m_originalMaterials = new Dictionary<GameObject, Material[]>();
}
```

### 2. 环境融合系统
```csharp
public class EnvironmentBlendingSystem : MonoBehaviour
{
    [Header("Blending Settings")]
    [SerializeField] private LayerMask m_virtualObjectLayers = -1;
    [SerializeField] private Shader m_mrCompatibleShader;
    [SerializeField] private bool m_enableOcclusion = true;
    
    // 材质管理
    private MaterialPropertyBlock m_propertyBlock;
    private Dictionary<Renderer, Material> m_mrMaterials = new Dictionary<Renderer, Material>();
    
    public void SetupMRMaterials();
    public void RestoreOriginalMaterials();
    public void UpdateEnvironmentLighting();
}
```

### 3. 安全边界系统
```csharp
public class MRSafetyBoundary : MonoBehaviour
{
    [Header("Safety Settings")]
    [SerializeField] private float m_warningDistance = 0.5f;
    [SerializeField] private float m_criticalDistance = 0.3f;
    [SerializeField] private GameObject m_boundaryVisualPrefab;
    
    private OVRBoundary m_boundary;
    private List<Vector3> m_boundaryPoints = new List<Vector3>();
    private bool m_isNearBoundary = false;
    
    public void UpdateBoundaryWarnings();
    public void ShowBoundaryVisualization(bool show);
}
```

## 实现任务分解

### 子任务1: 创建MRPassthroughManager核心系统 (1天)
- [ ] 创建MRPassthroughManager类
- [ ] 集成OVRPassthroughLayer组件
- [ ] 实现透视模式切换功能
- [ ] 添加透视度控制和色彩映射
- [ ] 实现运行时动态启用/禁用

### 子任务2: 环境融合和渲染优化 (0.5天)
- [ ] 创建EnvironmentBlendingSystem
- [ ] 实现MR兼容材质系统
- [ ] 添加环境光照匹配
- [ ] 优化虚拟对象渲染管线

### 子任务3: 安全边界和用户体验 (0.5天)
- [ ] 实现MRSafetyBoundary系统
- [ ] 添加边界警告和可视化
- [ ] 集成到VRInteractionManager
- [ ] 用户界面和状态指示

## 依赖关系

### 前置依赖
- ✅ Story VR-1: VRInteractionManager功能完整
- ✅ Story VR-3: Hand Tracking支持
- ✅ Meta XR SDK v72.0.0可用
- ✅ OVRPassthroughLayer组件可用

### 后置依赖
- Story VR-5: Avatar系统增强
- Story VR-6: Quest性能优化

### 外部依赖
- Meta Quest Pro/Quest 3设备（支持彩色透视）
- Quest 2设备（支持黑白透视）
- Passthrough功能在设备设置中启用

## 验收测试计划

### 功能测试
```csharp
[Test]
public void TestPassthroughModeSwitch()
{
    var mrManager = new MRPassthroughManager();
    
    mrManager.SetPassthroughMode(PassthroughMode.FullPassthrough);
    Assert.AreEqual(PassthroughMode.FullPassthrough, mrManager.CurrentMode);
    
    mrManager.SetPassthroughMode(PassthroughMode.Disabled);
    Assert.AreEqual(PassthroughMode.Disabled, mrManager.CurrentMode);
}

[Test]
public void TestEnvironmentBlending()
{
    var blendingSystem = new EnvironmentBlendingSystem();
    
    Assert.DoesNotThrow(() => blendingSystem.SetupMRMaterials());
    Assert.DoesNotThrow(() => blendingSystem.RestoreOriginalMaterials());
}
```

### VR设备测试
- **Quest 2测试**: 验证黑白透视功能
- **Quest 3测试**: 验证彩色透视和高分辨率
- **Quest Pro测试**: 验证高质量透视和色彩准确性
- **性能测试**: 确保MR模式下稳定帧率

### 环境适应性测试
- **室内环境**: 不同光照条件下的效果
- **物体遮挡**: 桌子、椅子等真实物体的遮挡效果
- **移动范围**: 不同房间大小的适配
- **边界安全**: 安全边界警告的准确性

## 安全考虑

### 用户安全
- **边界检测**: 实时监控用户与物理边界的距离
- **障碍物警告**: 检测并警告潜在的碰撞风险
- **紧急停止**: 提供快速退出MR模式的方法
- **视觉提醒**: 清晰的MR模式状态指示

### 数据隐私
- **摄像头数据**: 确保透视数据仅用于显示，不保存或传输
- **环境扫描**: 限制环境数据的收集和使用范围
- **用户同意**: 首次使用时明确告知MR功能的工作原理

## 性能优化策略

### 渲染优化
- **选择性渲染**: 仅在必要时启用透视渲染
- **LOD系统**: 根据距离调整虚拟对象的细节级别
- **遮挡剔除**: 优化被遮挡对象的渲染
- **着色器优化**: 使用MR优化的着色器

### 内存管理
- **纹理压缩**: 优化透视纹理的内存占用
- **对象池**: 复用MR相关的临时对象
- **垃圾回收**: 最小化MR功能的GC压力

## 验收标准详细

### 功能验收
- [ ] 透视功能在支持的Quest设备上正常启用
- [ ] 三种透视模式间切换流畅无卡顿
- [ ] 虚拟乒乓球桌在真实环境中稳定显示
- [ ] 真实物体正确遮挡虚拟对象
- [ ] 安全边界警告准确及时

### 性能验收
- [ ] MR模式下Quest 2保持稳定72fps+
- [ ] MR模式下Quest 3保持稳定90fps+
- [ ] 透视延迟<40ms，满足MR交互要求
- [ ] 内存使用增长<50MB
- [ ] 模式切换时间<500ms

### 用户体验验收
- [ ] MR模式启用后用户能清晰看到真实环境
- [ ] 虚拟乒乓球与真实环境自然融合
- [ ] 手部追踪在MR模式下正常工作
- [ ] 控制器在MR模式下正确显示
- [ ] 提供清晰的模式状态和控制界面

## 风险和缓解措施

### 技术风险
- **风险**: OVRPassthroughLayer API可能有设备兼容性问题
- **缓解**: 实现设备检测和功能降级，Quest 2使用黑白透视

- **风险**: MR模式可能显著影响性能
- **缓解**: 实现性能监控和自动质量调整

- **风险**: 环境光照变化可能影响虚拟对象的视觉效果
- **缓解**: 实现动态光照匹配和材质调整

### 用户体验风险
- **风险**: 用户可能在MR模式下感到不适或迷失方向
- **缓解**: 提供清晰的模式指示和快速退出选项

- **风险**: 复杂环境下透视效果可能不理想
- **缓解**: 提供环境适配建议和透视质量调整

## 交付物

### 代码文件
- [ ] MRPassthroughManager.cs - 透视管理核心
- [ ] EnvironmentBlendingSystem.cs - 环境融合系统
- [ ] MRSafetyBoundary.cs - 安全边界管理
- [ ] MRMaterialManager.cs - MR材质管理
- [ ] MRPerformanceOptimizer.cs - 性能优化

### 测试文件
- [ ] MRPassthroughTests.cs - 透视功能测试
- [ ] MRPerformanceTests.cs - 性能基准测试
- [ ] MRSafetyTests.cs - 安全功能测试

### 资源文件
- [ ] MR Compatible Shaders - MR兼容着色器
- [ ] Boundary Visualization Prefabs - 边界可视化预制件
- [ ] MR UI Elements - MR模式UI元素

## 实施优先级

### Phase 1 (第1.5天): 核心Passthrough功能
1. 创建MRPassthroughManager
2. 集成OVRPassthroughLayer
3. 实现基础透视模式切换
4. 添加透视度和色彩控制

### Phase 2 (第0.5天): 环境融合和优化
1. 实现EnvironmentBlendingSystem
2. 添加MR兼容材质系统
3. 实现安全边界检测
4. 性能优化和测试

## 成功指标

- ✅ **Passthrough功能**: Quest 2/3设备上透视正常工作
- ✅ **模式切换**: VR/MR模式间平滑切换无卡顿
- ✅ **环境融合**: 虚拟对象与真实环境自然结合
- ✅ **性能达标**: 不影响VR帧率要求
- ✅ **安全保障**: 边界检测和警告系统可靠
- ✅ **用户体验**: 直观易用的MR交互界面

---

**开始实施**: 现在开始Phase 1的开发工作，创建MRPassthroughManager并集成基础透视功能。