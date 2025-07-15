# Unity Editor VR调试指南

## 概述

本项目提供了完整的Unity Editor模式下VR调试支持，允许开发者在没有VR头显的情况下使用键盘和鼠标测试VR游戏功能。

## 🎯 调试方案架构

### 1. 输入模拟系统

#### EditorInputSimulator 组件

- **位置**: `Assets/PongHub/Scripts/Input/EditorInputSimulator.cs`
- **功能**: 模拟VR控制器和头显输入
- **自动激活**: 当检测到无VR设备时自动启用

#### 主要特性

- ✅ 头显位置和旋转模拟
- ✅ 双手控制器位置追踪
- ✅ 乒乓球专用输入映射
- ✅ 实时输入状态显示
- ✅ 可配置的按键映射

### 2. 调试控制器

#### GameDemo 组件

- **位置**: `Assets/PongHub/Scripts/Core/GameDemo.cs`
- **功能**: 整合所有管理器并提供调试界面
- **集成**: 自动连接现有管理器系统

#### 管理器集成

- ✅ GameModeController - 模式切换
- ✅ MatchManager - 比赛管理
- ✅ ScoreManager - 分数管理
- ✅ ServeValidator - 发球验证
- ✅ EditorInputSimulator - 输入模拟

## 🎮 输入控制映射

### 基本控制

| 输入 | 功能 | 说明 |
|------|------|------|
| **鼠标右键拖拽** | 视角旋转 | 模拟头显旋转 |
| **WASD** | 头部移动 | 前后左右移动 |
| **Q/E** | 头部上下移动 | 垂直移动 |
| **鼠标左键** | 激活左控制器 | 拖拽时移动左控制器 |
| **鼠标中键** | 激活右控制器 | 拖拽时移动右控制器 |

### 乒乓球专用控制

| 输入 | 功能 | VR映射 |
|------|------|--------|
| **Q** | 左拍击球 | 左控制器Trigger |
| **E** | 右拍击球 | 右控制器Trigger |
| **空格** | 发球 | 物理抛球动作 |
| **WASD** | 移动 | 左摇杆移动 |

### 调试功能

| 输入 | 功能 | 说明 |
|------|------|------|
| **P** | 暂停/继续 | 仅离线模式有效 |
| **R** | 重置位置 | 重置到默认位置 |
| **Tab** | 菜单 | VR菜单按钮 |
| **F1** | 切换帮助显示 | 显示/隐藏操作说明 |

### GameDemo专用控制

| 输入 | 功能 | 说明 |
|------|------|------|
| **1/2** | 玩家1/2得分 | 手动触发得分 |
| **O** | 离线模式 | 强制切换到离线 |
| **N** | 在线模式 | 强制切换到在线 |
| **H** | 混合模式 | 启用混合模式 |
| **S** | 测试发球验证 | 测试发球规则 |
| **F2** | 切换自动演示 | 开启/关闭自动演示 |

## 🛠️ 使用方法

### 1. 快速开始

1. **启动Editor**: 在Unity Editor中点击Play
2. **自动检测**: 系统自动检测VR设备状态
3. **激活模拟器**: 无VR设备时自动启用键盘鼠标控制
4. **查看帮助**: 按F1显示实时操作说明

### 2. 场景设置

```csharp
// 在场景中添加GameDemo组件
GameObject demoObject = new GameObject("GameDemo");
GameDemo demo = demoObject.AddComponent<GameDemo>();

// 组件会自动查找并连接现有管理器
// 或创建缺失的组件
```

### 3. 调试界面

#### 实时信息显示

- 比赛状态和分数
- 网络连接状态
- 输入设备状态
- 控制器位置信息

#### 视觉调试

- 发球高度线（黄色）
- 演示区域边界（绿色）
- 坐标轴显示（RGB）

## 📝 配置说明

### EditorInputSimulator配置

```csharp
[Header("模拟器设置")]
public bool enableInEditor = true;        // Editor中启用
public bool enableInBuild = false;        // 打包版本中启用
public bool showInstructions = true;      // 显示操作说明
public bool verboseLogging = false;       // 详细日志

[Header("头显模拟")]
public float headMoveSpeed = 2f;          // 头部移动速度
public float headRotateSpeed = 100f;      // 头部旋转速度

[Header("控制器模拟")]
public float controllerMoveSpeed = 1f;    // 控制器移动速度
public float controllerRotateSpeed = 90f; // 控制器旋转速度
```

### 自定义按键映射

```csharp
[Header("乒乓球输入映射")]
public KeyCode leftPaddleHitKey = KeyCode.Q;    // 左拍击球
public KeyCode rightPaddleHitKey = KeyCode.E;   // 右拍击球
public KeyCode serveKey = KeyCode.Space;        // 发球

// 可以在Inspector中修改按键映射
```

## 🔧 高级功能

### 1. 事件系统

EditorInputSimulator提供完整的事件系统：

```csharp
// 订阅输入事件
EditorInputSimulator.OnLeftPaddleHit += (isPressed) => {
    Debug.Log($"左拍: {isPressed}");
};

EditorInputSimulator.OnServeAction += (position, velocity) => {
    Debug.Log($"发球: {position}, {velocity}");
};
```

### 2. 状态查询

```csharp
// 获取当前输入状态
var inputState = editorInputSimulator.GetCurrentInputState();
Debug.Log($"头部位置: {inputState.HeadPosition}");
Debug.Log($"左手位置: {inputState.LeftHandPosition}");
```

### 3. 动态配置

```csharp
// 运行时修改设置
editorInputSimulator.SetSimulatorEnabled(true);
editorInputSimulator.SetControllerPosition(true, newLeftHandPos);
```

## 🎨 界面说明

### 调试信息面板

游戏运行时左上角显示：

```text
乒乓球游戏演示 - 调试信息
┌─────────────────────────────────┐
│ 比赛状态: Playing                │
│ 当前轮次: 2                     │
│ 轮次分数: 1 - 0                 │
│ 当前比分: 5 - 3                 │
│ 领先玩家: 玩家1                  │
│                                 │
│ 游戏模式: ForceOffline          │
│ 连接状态: Disconnected          │
│ 当前模式: 离线                   │
│ 可暂停: True                    │
│                                 │
│ 发球状态: WaitingForServe       │
│ 最小抛球高度: 0.160m            │
│ 最大抛球角度: 30.0°             │
│                                 │
│ Editor输入模拟器:               │
│ 头部位置: (0.0, 1.6, 0.0)      │
│ 移动输入: (0.0, 0.0)           │
│ 左拍: 释放                      │
│ 右拍: 释放                      │
│                                 │
│ VR状态: 未连接                  │
│ 模拟器状态: 激活                │
└─────────────────────────────────┘
```

## 🚀 最佳实践

### 1. 开发工作流

1. **编写逻辑**: 使用Editor模式快速测试游戏逻辑
2. **键盘调试**: 用键盘验证所有交互功能
3. **VR测试**: 在真实VR设备上进行最终测试

### 2. 调试技巧

```csharp
// 使用详细日志查看输入详情
editorInputSimulator.verboseLogging = true;

// 使用暂停功能分析状态
if (Input.GetKeyDown(KeyCode.P)) {
    Time.timeScale = Time.timeScale == 0 ? 1 : 0;
}

// 使用Gizmos可视化调试
void OnDrawGizmos() {
    if (showDebugGizmos) {
        Gizmos.DrawSphere(servePosition, 0.1f);
    }
}
```

### 3. 性能优化

- 在最终打包时禁用Editor输入模拟器
- 使用条件编译优化Editor专用代码
- 仅在需要时启用详细日志

## 🔍 故障排除

### 常见问题

#### 1. 模拟器未启动

```text
问题: 模拟器显示"未激活"
原因: VR设备已连接或设置被禁用
解决: 断开VR设备或在Inspector中启用enableInEditor
```

#### 2. 按键无响应

```text
问题: 按键输入无效果
原因: 焦点不在Game窗口或按键映射冲突
解决: 点击Game窗口获取焦点，检查按键映射
```

#### 3. 控制器无法移动

```text
问题: 鼠标拖拽无法移动控制器
原因: 控制器Transform未正确设置
解决: 在Inspector中手动指定控制器Transform
```

### 调试日志

启用详细日志查看详细信息：

```csharp
editorInputSimulator.verboseLogging = true;
```

常见日志信息：

```text
[EditorInputSimulator] VR设备已连接或模拟器已禁用
[EditorInputSimulator] Editor输入模拟器已启动
[EditorInputSimulator] 创建虚拟控制器: LeftController
[EditorInputSimulator] 左控制器激活
[EditorInputSimulator] 模拟发球 - 位置: (0,1.6,0), 速度: (0,3,2)
```

## 📚 扩展开发

### 1. 添加新的输入动作

```csharp
// 在EditorInputSimulator中添加新按键
[SerializeField] private KeyCode newActionKey = KeyCode.T;

// 在事件系统中添加新事件
public static System.Action OnNewAction;

// 在HandleGameplayInput中处理
if (Input.GetKeyDown(newActionKey)) {
    OnNewAction?.Invoke();
}
```

### 2. 自定义控制器行为

```csharp
// 继承EditorInputSimulator
public class CustomInputSimulator : EditorInputSimulator {
    protected override void HandleGameplayInput() {
        base.HandleGameplayInput();
        // 添加自定义逻辑
    }
}
```

### 3. 集成其他输入系统

```csharp
// 与其他输入系统桥接
public class InputBridge : MonoBehaviour {
    void Start() {
        EditorInputSimulator.OnServeAction += (pos, vel) => {
            // 转发到其他输入系统
            OtherInputSystem.TriggerServe(pos, vel);
        };
    }
}
```

## 🔄 与现有系统集成

### 1. 输入系统兼容

本调试系统与现有输入系统完全兼容：

- **PongHub.inputactions**: 保持原有VR输入映射
- **XR Input System**: 自动检测并切换
- **Meta XR SDK**: 无冲突，可共存

### 2. 网络同步

在网络模式下：

- Editor输入模拟器自动禁用网络不兼容功能
- 保持与真实VR客户端的兼容性
- 支持混合调试（Editor + VR设备）

## 📈 未来扩展

计划中的功能：

- [ ] 手势识别模拟
- [ ] 触觉反馈模拟
- [ ] 多人调试支持
- [ ] 录制回放功能
- [ ] 自动化测试集成

---

## 📞 技术支持

如有问题或建议，请查看：

1. 游戏内F1帮助信息
2. Unity Console日志
3. 项目文档目录
4. 代码注释和示例

**快速诊断命令**:

```csharp
// 在Console中执行
Debug.Log(FindObjectOfType<EditorInputSimulator>()?.GetCurrentInputState());
Debug.Log(FindObjectOfType<GameDemo>()?.GetDemoInfo());
```
