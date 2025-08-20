# Meta Quest Unity-UtilityPackages 导入标准操作程序 (SOP)

## 概述
本文档提供将 Meta Quest Unity-UtilityPackages 导入 PongHub 项目的详细操作步骤。该包集合提供了完整的 VR 开发基础设施，包括单例管理、输入抽象、自动化工具等核心功能。

## 包功能概览

### 🎯 **核心价值**
- **单例管理系统**：解决 Unity 跨场景对象生命周期问题
- **VR 输入抽象**：统一 VR 输入接口，简化跨平台开发
- **自动化工具**：AutoSet 特性消除手动组件引用
- **开发效率工具**：编辑器扩展、网络调试、场景管理等

### 📦 **主要包组成**
1. **com.meta.utilities** - 核心工具包
2. **com.meta.utilities.input** - VR 输入系统
3. **com.meta.utilities.networking** - 网络开发工具  
4. **com.meta.utilities.android** - Android 平台集成

## 当前项目状态

### 已有包状态
```
✅ Packages/com.meta.utilities (v1.0.0) - 已安装
✅ Packages/com.meta.utilities.input (v1.1.0) - 已安装
❌ 其他 Meta Utilities 包 - 未安装
```

### 依赖关系检查
当前项目依赖：
- `com.meta.utilities`: 32 个文件使用
- `com.meta.utilities.input`: 5 个文件使用
- Meta.Utilities 命名空间广泛使用

## 导入操作步骤

### 阶段 1：包管理器配置

#### 1.1 添加 GitHub 包源
在 `Packages/manifest.json` 中添加 GitHub 包引用：

```json
{
  "dependencies": {
    // 现有依赖保持不变
    "com.meta.utilities": "1.0.0",
    "com.meta.utilities.input": "1.1.0",
    
    // 新增 GitHub 包引用
    "com.meta.utilities.networking": "https://github.com/meta-quest/Unity-UtilityPackages.git?path=/Packages/Networking",
    "com.meta.utilities.android": "https://github.com/meta-quest/Unity-UtilityPackages.git?path=/Packages/Android",
    "com.meta.utilities.editor": "https://github.com/meta-quest/Unity-UtilityPackages.git?path=/Packages/Editor"
  },
  "scopedRegistries": [
    {
      "name": "Meta Quest",
      "url": "https://npm.pkg.github.com/meta-quest",
      "scopes": ["com.meta.utilities"]
    }
  ]
}
```

#### 1.2 验证包导入
1. 打开 Unity Package Manager
2. 切换到 "In Project" 视图
3. 确认所有 Meta Utilities 包已正确导入
4. 检查依赖关系是否正确解析

### 阶段 2：现有代码兼容性验证

#### 2.1 编译检查
```bash
# 确保项目无编译错误
# Unity 会自动解析新包的依赖关系
```

#### 2.2 现有 Singleton 类检查
检查项目中使用 Singleton 模式的类：
```csharp
// 需要更新的类（示例）
public class GameModeManager : Singleton<GameModeManager>  // ✅ 已使用
public class AudioManager : Singleton<AudioManager>       // ❓ 需验证
public class SettingsManager : MonoBehaviour               // ❌ 可改进
```

#### 2.3 生命周期管理更新
根据源项目 commit，需要更新以下模式：

```csharp
// 旧模式 - 需要更新
public class MyManager : Singleton<MyManager>
{
    private void OnDestroy()
    {
        // 直接销毁处理
    }
}

// 新模式 - 推荐使用
public class MyManager : Singleton<MyManager>
{
    protected override void OnDestroy()
    {
        // 自定义销毁逻辑
        base.OnDestroy(); // ✅ 必须调用基类方法
    }
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // ✅ 添加场景保持
    }
}
```

### 阶段 3：核心功能集成

#### 3.1 AutoSet 特性应用

**识别改进机会：**
```csharp
// 当前手动引用模式（需要改进）
public class PongHubInputManager : MonoBehaviour
{
    [SerializeField] private Camera m_camera;              // 手动拖拽
    [SerializeField] private AudioSource m_audioSource;   // 手动拖拽
    [SerializeField] private Transform m_centerEye;        // 手动拖拽
    
    private void Start()
    {
        // 运行时查找
        if (m_camera == null) m_camera = Camera.main;
    }
}
```

**使用 AutoSet 改进：**
```csharp
// 自动化引用模式（推荐）
public class PongHubInputManager : MonoBehaviour
{
    [AutoSet] private Camera m_camera;                     // ✅ 自动设置
    [AutoSet] private AudioSource m_audioSource;          // ✅ 自动设置
    [AutoSetFromParent] private Transform m_centerEye;     // ✅ 从父对象查找
    
    // 无需 Start() 方法进行手动查找
}
```

#### 3.2 扩展方法应用

**向量计算改进：**
```csharp
// 当前代码
if (Vector3.Distance(handPos, targetPos) < 0.01f)
{
    // 手势识别逻辑
}

// 使用扩展方法改进
if (handPos.IsCloseTo(targetPos, 0.01f))  // ✅ 更清晰的语义
{
    // 手势识别逻辑
}
```

**集合处理改进：**
```csharp
// 当前代码
var validPlayers = new List<Player>();
foreach (var player in allPlayers)
{
    if (player != null) validPlayers.Add(player);
}

// 使用扩展方法改进
var validPlayers = allPlayers.WhereNonNull().ToList();  // ✅ 简洁明了
```

### 阶段 4：VR 输入系统增强

#### 4.1 XRDeviceFpsSimulator 集成

**创建模拟器预制件：**
1. 在场景中添加 `XRDeviceFpsSimulator` 预制件
2. 配置鼠标键盘控制映射
3. 设置自动检测真实设备

```csharp
// 集成到 PongHubInputManager
public class PongHubInputManager : MonoBehaviour
{
    [SerializeField] private XRDeviceFpsSimulator m_simulator;
    
    private void Start()
    {
        // 自动检测并启用模拟器
        if (!XRDevice.isPresent && m_simulator != null)
        {
            m_simulator.enabled = true;
            Debug.Log("启用 VR 设备模拟器");
        }
    }
}
```

#### 4.2 统一输入接口

**增强现有输入管理：**
```csharp
// 整合 XRInputManager 功能
public class PongHubInputManager : MonoBehaviour
{
    [AutoSet] private XRInputManager m_xrInputManager;
    
    public Vector2 GetTeleportInput()
    {
        // 使用统一接口获取输入
        var leftActions = m_xrInputManager.GetActions(true);   // 左手
        var rightActions = m_xrInputManager.GetActions(false); // 右手
        
        return leftActions.Thumbstick.action.ReadValue<Vector2>();
    }
}
```

### 阶段 5：网络开发工具集成

#### 5.1 NetworkSettings 配置

**添加到项目设置：**
```csharp
// 在 Startup 场景添加网络配置
public class NetworkBootstrap : MonoBehaviour
{
    private void Start()
    {
        #if UNITY_EDITOR
        // 开发环境自动配置
        NetworkSettings.Autostart = true;
        NetworkSettings.UseDeviceRoom = false;
        NetworkSettings.RoomName = "PongHub_Dev";
        #endif
    }
}
```

#### 5.2 ParrelSync 支持增强

**多实例测试优化：**
```csharp
// 检测并配置多实例环境
public class MultiInstanceManager : MonoBehaviour
{
    private void Awake()
    {
        if (IsParrelSyncClone())
        {
            // 克隆实例特殊配置
            NetworkSettings.RoomName += "_Clone";
            QualitySettings.SetQualityLevel(1); // 降低质量提高性能
        }
    }
    
    private bool IsParrelSyncClone()
    {
        return System.Environment.GetCommandLineArgs()
            .Any(arg => arg.Contains("ParrelSync"));
    }
}
```

### 阶段 6：编辑器工具集成

#### 6.1 Settings Warnings Toolbar

**自动检查项目配置：**
- VR 设备设置检查
- 构建平台兼容性验证
- 必要组件缺失警告

#### 6.2 Build Tools 集成

**持续集成支持：**
- 自动化构建脚本
- 版本号管理
- 资源依赖检查

## 验证测试

### 功能验证清单

#### ✅ **Singleton 系统**
- [ ] GameModeManager 正确初始化
- [ ] 跨场景保持功能正常
- [ ] 销毁顺序无空引用异常

#### ✅ **AutoSet 系统**
- [ ] 组件自动引用正确
- [ ] 编辑时自动更新
- [ ] 预制件变体支持

#### ✅ **VR 输入系统**
- [ ] 真实设备输入正常
- [ ] 模拟器功能可用
- [ ] 输入事件正确分发

#### ✅ **扩展方法**
- [ ] Vector3.IsCloseTo() 精度正确
- [ ] 集合过滤方法有效
- [ ] 反射扩展安全可靠

#### ✅ **网络工具**
- [ ] 多实例测试顺畅
- [ ] 房间管理正常
- [ ] 开发配置自动应用

### 性能验证

#### 📊 **基准测试**
- 启动时间影响：< 100ms
- 运行时开销：< 1ms/frame
- 内存占用增加：< 10MB

#### 🎮 **VR 性能**
- 帧率维持：90fps
- 输入延迟：< 20ms
- 手部追踪精度：与原生一致

## 常见问题解决

### Q1: 编译错误 "Singleton already exists"

**解决方案：**
```csharp
// 检查是否存在自定义 Singleton 实现
#if !USES_META_UTILITIES
public class Singleton<T> : MonoBehaviour where T : Component
{
    // 自定义实现
}
#endif
```

### Q2: AutoSet 不生效

**解决方案：**
1. 确保 Unity 2021.3+ 版本
2. 检查 AutoSetPostprocessor 是否正确导入
3. 重新导入预制件

### Q3: VR 模拟器控制异常

**解决方案：**
1. 检查输入映射配置
2. 确认鼠标锁定设置
3. 验证相机组件配置

### Q4: 网络设置不持久

**解决方案：**
```csharp
// 添加 EditorPrefs 持久化
NetworkSettings.RoomName = EditorPrefs.GetString("NetworkRoomName", "Default");
```

## 最佳实践建议

### 🎯 **开发阶段**
1. **渐进式采用**：逐步替换现有手动配置
2. **兼容性保持**：保留原有功能作为 fallback
3. **团队培训**：确保团队了解新工具使用方法

### 🚀 **生产环境**
1. **性能监控**：定期检查新工具对性能的影响
2. **错误处理**：完善异常处理和回退机制
3. **文档维护**：及时更新开发文档

### 🔧 **长期维护**
1. **版本跟踪**：关注包更新和变更日志
2. **社区参与**：参与 Meta Quest 开发者社区
3. **经验分享**：记录最佳实践和踩坑经验

## 预期收益

### 📈 **开发效率提升**
- **配置时间减少**：50-70%（通过 AutoSet）
- **调试效率提升**：30-50%（通过模拟器和工具）
- **代码维护成本降低**：20-40%（通过扩展方法和统一接口）

### 🎮 **VR 开发体验改善**
- **无设备开发**：支持完整的桌面开发流程
- **多人测试简化**：一键多实例测试
- **跨平台兼容性**：为 OpenXR 迁移做准备

### 🏗️ **架构质量提升**
- **单例管理规范化**：统一的生命周期管理
- **依赖管理自动化**：减少手动配置错误
- **代码复用性增强**：通用工具和扩展方法

---

*本 SOP 基于 Meta Quest Unity-UtilityPackages 的官方最佳实践制定，旨在为 PongHub 项目提供完整的 VR 开发基础设施支持。通过这些工具的集成，项目将获得更高的开发效率、更好的代码质量和更强的可维护性。*