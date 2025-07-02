# SharedEnvironment 预制件使用指南

## 🎯 概述

`SharedEnvironment.prefab` 是一个完整的体育馆环境预制件，包含了所有必要的环境组件，可以在不同场景间共享使用，实现一致的视觉体验。

## 📦 预制件结构

```text
📁 SharedEnvironment.prefab
├── 🏗️ Architecture                 // 建筑结构（墙壁、地板、天花板）
├── 🏋️ Gym Equipment               // 体育器材
│   ├── Bench (长椅)
│   ├── Brus (清洁工具)
│   ├── Ladder (梯子)
│   ├── Mattress (垫子)
│   ├── Rack (架子)
│   ├── VaultingHorse (跳马)
│   ├── Trampline (蹦床)
│   └── Basket (篮子)
├── 💡 Lighting System             // 灯光系统
│   ├── Directional Light (主光源)
│   ├── AreaLights (区域光源组)
│   └── Light Probe Group (光照探针)
├── 🔊 Audio Systems               // 音频系统
│   ├── MusicManager (音乐管理器)
│   └── centerAudioSource (中心音源)
├── 🎨 Post Processing             // 后处理效果
│   └── Global Volume (全局音量)
├── 🚧 Collision Boundaries        // 碰撞边界
├── 🪑 Furniture                   // 家具装饰
├── 🚪 Gates & Doors               // 门和入口
├── ⚙️ Technical                   // 技术设备
│   ├── Radiator (散热器)
│   └── LightLum (照明设备)
└── 🗺️ NavMeshPlane               // 导航网格
```

## ⚡ 快速使用步骤

### 1. 在 Startup 场景中使用

```bash
操作步骤：
1. 打开 Assets/PongHub/Scenes/Startup.unity
2. 从Project窗口找到 Assets/PongHub/Prefabs/SharedEnvironment.prefab
3. 拖拽预制件到Hierarchy窗口
4. 设置Transform位置为 (0, 0, 0)
5. 保存场景 (Ctrl+S)
```

### 2. 验证环境组件

运行场景后，检查以下组件是否正常工作：

- ✅ **建筑结构**：墙壁、地板、天花板显示正常
- ✅ **灯光效果**：环境光照明亮且自然
- ✅ **音频系统**：背景音乐和环境音效播放
- ✅ **后处理**：视觉效果（阴影、反射等）正常
- ✅ **碰撞检测**：玩家无法穿越墙壁和障碍物

### 3. 与 GameModeManager 集成

```csharp
// 在GameModeManager中引用环境
public class GameModeManager : MonoBehaviour
{
    [Header("环境引用")]
    [SerializeField] private GameObject sharedEnvironment;

    void Start()
    {
        // 确保环境在模式切换时不被销毁
        if (sharedEnvironment != null)
        {
            DontDestroyOnLoad(sharedEnvironment);
        }
    }
}
```

## 🔧 自定义配置

### 音频系统配置

```csharp
// 访问音频组件
var musicManager = SharedEnvironment.GetComponentInChildren<MusicManager>();
var audioSource = SharedEnvironment.GetComponentInChildren<AudioSource>();

// 调整音量
musicManager.SetVolume(0.7f);
audioSource.volume = 0.5f;
```

### 灯光系统配置

```csharp
// 访问灯光组件
var directionalLight = SharedEnvironment.GetComponentInChildren<Light>();
var lightProbeGroup = SharedEnvironment.GetComponentInChildren<LightProbeGroup>();

// 调整光照强度
directionalLight.intensity = 1.2f;
```

## 🚀 场景切换最佳实践

### 方案 A：预制件实例化（推荐）

```csharp
public class SceneTransitionManager : MonoBehaviour
{
    public GameObject sharedEnvironmentPrefab;
    private GameObject currentEnvironment;

    public void LoadNewScene(string sceneName)
    {
        // 保留环境，只切换游戏逻辑
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 环境实例保持不变
        if (currentEnvironment == null)
        {
            currentEnvironment = Instantiate(sharedEnvironmentPrefab);
            DontDestroyOnLoad(currentEnvironment);
        }
    }
}
```

### 方案 B：组件状态切换

```csharp
public void SwitchToNetworkMode()
{
    // 环境对象保持不变，只切换游戏组件状态
    EnableNetworkComponents();
    DisableLocalComponents();

    // SharedEnvironment预制件无需修改
}
```

## ⚠️ 注意事项

### 性能优化

- ✅ 所有静态环境对象已设置为 Static
- ✅ 光照贴图已预烘焙
- ✅ 材质使用批处理友好的设置

### 兼容性检查

- ✅ 与单机模式兼容
- ✅ 与网络多人模式兼容
- ✅ 支持 VR 设备交互
- ✅ 适配不同平台性能设置

### 内存管理

- 🔸 预制件大小约 20MB（包含所有贴图和模型）
- 🔸 运行时内存占用约 30-50MB
- 🔸 建议在移动设备上监控内存使用

## 🛠️ 故障排除

### 常见问题

**问题 1：环境不显示**

```bash
解决方案：
1. 检查预制件是否正确拖拽到场景
2. 验证Transform位置是否为(0,0,0)
3. 确认Camera位置在环境内部
```

**问题 2：光照异常**

```bash
解决方案：
1. 重新烘焙光照：Window → Rendering → Lighting → Generate Lighting
2. 检查Light Probe Group是否包含在预制件中
3. 验证Post Processing Volume设置
```

**问题 3：音频不播放**

```bash
解决方案：
1. 检查MusicManager脚本是否启用
2. 验证AudioSource组件设置
3. 确认音频文件路径正确
```

## 📋 更新记录

- **v1.0** - 初始版本，包含完整 Gym 环境
- **当前** - 已与 Startup 和 Gym 场景兼容，支持无缝模式切换
