# 输入系统使用说明

## 📖 概述

本输入系统基于**Meta Utilities Input**包构建，专为 Quest 控制器优化，提供了完整的 VR 乒乓球游戏输入解决方案。系统采用事件驱动架构，支持高性能的实时输入处理和灵活的配置管理。

## 🏗️ 架构设计

### 输入数据流

```text
VR控制器硬件 → Unity Input System → XRInputControlActions → PongHubInputManager → 游戏逻辑
                                         ↓
                              PaddleConfigurationManager → 配置管理
```

### 核心设计原则

- **句柄模式**：一次获取输入句柄，持续读取实时状态
- **事件驱动**：基于状态变化触发事件，降低系统耦合
- **性能优化**：缓存输入引用，分帧处理，避免重复计算
- **可配置性**：支持运行时配置调整和持久化存储

## 🔧 主要组件

### 1. PongHubInputManager - 核心输入管理器

**架构特点：**

- 优化的事件驱动设计
- 输入句柄缓存机制
- 状态缓存和分帧处理
- 完整的错误处理

**功能模块：**

```csharp
// 事件系统
public static event Action<bool> OnPaddleGrabbed;
public static event Action OnPaddleReleased;
public static event Action<bool> OnBallGenerated;
public static event Action OnTeleportPerformed;

// 输入状态结构
public struct InputState
{
    public Vector2 leftStick, rightStick;
    public bool leftButtonA, leftButtonB, leftButtonMeta;
    public bool rightButtonA, rightButtonB, rightButtonMeta;
    public float leftGrip, rightGrip;
    public float leftTrigger, rightTrigger;
    public bool leftAB, rightAB; // 组合键
}
```

**性能特性：**

- 缓存 Actions 句柄，避免重复获取
- 按配置间隔检查输入（默认 60fps）
- 分离连续输入和离散事件处理
- 支持死区配置减少无效输入

### 2. PaddleConfigurationManager - 球拍配置管理器

**功能特点：**

- 实时预览球拍位置和旋转
- 分别配置左右手球拍
- 配置持久化存储
- 安全的输入处理

**改进点：**

- 使用`WasPressedThisFrame()`避免重复触发
- 增强空引用检查
- 优化预览更新逻辑

### 3. CustomPointableCanvasModule - UI 交互模块

**功能：**

- VR 环境下的 UI 射线交互
- 编辑器环境自动切换鼠标交互
- 与 Meta Interaction 系统集成

## 🎮 输入映射表

### 移动控制

| 输入          | 功能         | 备注         |
| ------------- | ------------ | ------------ |
| 左手摇杆      | 前后左右移动 | 支持死区配置 |
| 右手摇杆 X 轴 | 视角左右旋转 | 可调节灵敏度 |
| 右手摇杆前推  | 向前瞬移     | 一次性触发   |
| 左手 A 键     | 向上移动     | 连续按住生效 |
| 左手 B 键     | 向下移动     | 连续按住生效 |

### 交互控制

| 输入             | 功能          | 触发方式      |
| ---------------- | ------------- | ------------- |
| 长按 Grip        | 握持/释放球拍 | 长按 1 秒触发 |
| 非持拍手 Trigger | 生成球        | 按下时触发    |
| 长按 Meta 键     | 回到出生点    | 长按 2 秒触发 |
| A+B 组合键       | 进入配置模式  | 同时按下触发  |

### 配置模式

| 输入       | 功能          | 备注        |
| ---------- | ------------- | ----------- |
| UI 滑条    | 调整位置/旋转 | 实时预览    |
| B 键       | 退出配置      | 任意手 B 键 |
| Save 按钮  | 保存配置      | UI 按钮     |
| Reset 按钮 | 重置默认      | UI 按钮     |

## ⚙️ 配置参数

### PongHubInputManager 配置

```csharp
[Header("移动设置")]
public float moveSpeed = 3f;                    // 移动速度
public float rotationSpeed = 90f;               // 旋转速度
public float heightChangeSpeed = 1f;            // 高度变化速度
public float teleportDistance = 5f;             // 瞬移距离

[Header("输入设置")]
public float inputCheckInterval = 0.016f;       // 输入检查间隔(60fps)
public float deadZone = 0.1f;                   // 摇杆死区
```

### 计时器配置

```csharp
public float metaKeyHoldTime = 2f;              // Meta键长按时间
public float gripHoldTime = 1f;                 // Grip长按时间
```

## 🔄 事件系统使用

### 订阅事件示例

```csharp
private void OnEnable()
{
    PongHubInputManager.OnPaddleGrabbed += HandlePaddleGrabbed;
    PongHubInputManager.OnPaddleReleased += HandlePaddleReleased;
    PongHubInputManager.OnBallGenerated += HandleBallGenerated;
    PongHubInputManager.OnTeleportPerformed += HandleTeleportPerformed;
}

private void OnDisable()
{
    PongHubInputManager.OnPaddleGrabbed -= HandlePaddleGrabbed;
    PongHubInputManager.OnPaddleReleased -= HandlePaddleReleased;
    PongHubInputManager.OnBallGenerated -= HandleBallGenerated;
    PongHubInputManager.OnTeleportPerformed -= HandleTeleportPerformed;
}

private void HandlePaddleGrabbed(bool isLeftHand)
{
    Debug.Log($"球拍被{(isLeftHand ? "左手" : "右手")}握持");
    // 添加音效、触觉反馈等
}
```

## 📁 文件结构

```text
Assets/PongHub/Scripts/Input/
├── PongHubInputManager.cs               # 核心输入管理器（最新版）
├── PaddleConfigurationManager.cs        # 球拍配置管理器（已优化）
├── CustomPointableCanvasModule.cs       # UI交互模块
└── README_InputSystem.md               # 本文档

相关资源：
├── Packages/com.meta.utilities.input/
│   ├── XRInputControlActions.asset      # 输入动作配置
│   ├── XRInputControlActions.cs         # 输入动作类
│   └── XRInputManager.cs               # XR输入管理器
```

## 🚀 性能优化特性

### 1. 输入句柄缓存

```csharp
// 一次获取，持续使用
private void Start()
{
    leftActions = xrInputManager.GetActions(true);   // 缓存左手句柄
    rightActions = xrInputManager.GetActions(false); // 缓存右手句柄
}

// 反复读取实时状态
float triggerValue = leftActions.AxisIndexTrigger.action.ReadValue<float>();
```

### 2. 分帧输入处理

```csharp
// 离散事件：按配置间隔检查
if (Time.time - lastInputCheckTime >= inputCheckInterval)
{
    UpdateInputState();      // 更新状态缓存
    ProcessInputEvents();    // 处理状态变化事件
}

// 连续输入：每帧处理
HandleContinuousInput();     // 移动、旋转等
```

### 3. 状态缓存机制

```csharp
// 前后帧状态比较，只在变化时触发事件
if (currentState.leftAB && !previousState.leftAB)
{
    OnLeftABPressed(); // 仅在按下瞬间触发一次
}
```

## 🛠️ 使用指南

### 场景设置步骤

1. **基础组件设置**

   ```text
   场景根对象
   ├── XRInputManager
   ├── PongHubInputManager
   ├── PaddleConfigurationManager
   └── OVRCameraRig
   ```

2. **组件配置**

   - **PongHubInputManager**: 设置移动参数、预制件引用、瞬移点
   - **PaddleConfigurationManager**: 配置 UI Canvas、预览材质
   - **XRInputManager**: 连接 XRInputControlActions 资源

3. **预制件准备**
   - **球拍**: 包含 Rigidbody、Collider、Renderer
   - **球**: 包含 Rigidbody、Collider、物理材质

### 配置流程

1. **球拍位置配置**

   ```text
   进入配置 → 调整参数 → 实时预览 → 保存配置
       ↓           ↓          ↓         ↓
   A+B组合键   UI滑条操作   透明预览   Save按钮
   ```

2. **配置数据结构**

   ```csharp
   [System.Serializable]
   public class PaddleConfiguration
   {
       public Vector3 leftHandPosition;   // 左手位置偏移
       public Vector3 leftHandRotation;   // 左手旋转偏移
       public Vector3 rightHandPosition;  // 右手位置偏移
       public Vector3 rightHandRotation;  // 右手旋转偏移
   }
   ```

## 🐛 调试功能

### 控制台日志

```csharp
Debug.Log("PongHubInputManager 已初始化");
Debug.Log($"球拍已握持到{(isLeftHand ? "左手" : "右手")}");
Debug.Log($"球已从{(fromLeftHand ? "左手" : "右手")}生成");
Debug.Log("执行瞬移");
```

### 状态查询接口

```csharp
public bool IsPaddleHeld { get; }                    // 是否握持球拍
public bool IsLeftHandHoldingPaddle { get; }         // 是否左手握持
public InputState CurrentInputState { get; }         // 当前输入状态
```

## 🔧 扩展指南

### 添加新的输入动作

1. **在 XRInputControlActions 中添加**

   ```csharp
   public InputActionProperty NewAction;
   ```

2. **在 Controller 结构中包含**

   ```csharp
   public InputActionProperty[] AllActions => new[] {
       // ... 现有动作
       NewAction,
   };
   ```

3. **在 PongHubInputManager 中使用**

   ```csharp
   bool newActionPressed = leftActions.NewAction.action.ReadValue<float>() > 0.5f;
   ```

### 自定义事件处理

```csharp
// 添加新事件
public static event Action<CustomData> OnCustomEvent;

// 触发事件
OnCustomEvent?.Invoke(customData);

// 订阅处理
PongHubInputManager.OnCustomEvent += HandleCustomEvent;
```

## ⚠️ 注意事项

### 性能建议

- ✅ 缓存输入句柄，避免重复获取
- ✅ 使用`WasPressedThisFrame()`检测按下事件
- ✅ 配置合理的`inputCheckInterval`
- ❌ 避免在 Update 中频繁调用`GetActions()`

### 常见问题

1. **输入无响应**: 检查 XRInputManager 初始化
2. **重复触发**: 使用状态变化检测而非持续检测
3. **配置丢失**: 确认 PlayerPrefs 保存路径
4. **预览错误**: 检查手部锚点引用

## 📊 版本历史

| 版本     | 更新内容     | 性能改进          |
| -------- | ------------ | ----------------- |
| **v1.0** | 基础输入功能 | 初始实现          |
| **v1.1** | 球拍配置系统 | UI 交互优化       |
| **v1.2** | 事件驱动架构 | 减少组件耦合      |
| **v1.3** | 性能优化版本 | 句柄缓存+分帧处理 |
| **v1.4** | 配置管理优化 | 安全检查+按下检测 |
