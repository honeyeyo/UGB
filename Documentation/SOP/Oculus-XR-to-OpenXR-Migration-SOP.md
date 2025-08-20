# PongHub Oculus XR 到 OpenXR 迁移标准操作程序 (SOP)

## 概述
本文档提供 PongHub 项目从 Oculus XR 插件迁移到 OpenXR 的详细操作步骤。

⚠️ **重要说明**：经分析发现，PongHub 项目与参考项目 Unity-UltimateGloveBall 在 VR 架构上存在根本差异。UltimateGloveBall 使用了 Meta Utilities 抽象层，因此其 OpenXR 迁移相对简单；而 PongHub 项目大量直接使用 OVR API，迁移复杂度显著更高。

### 架构差异对比

| 项目 | VR 架构模式 | OpenXR 迁移复杂度 |
|------|-------------|-------------------|
| **UltimateGloveBall** | Meta Utilities 抽象层 | 🟢 **低** - 主要是配置变更 |
| **PongHub** | 直接 OVR API 调用 | 🔴 **高** - 需要大量代码重构 |

## 迁移动机与优势

### OpenXR 优势
- **跨平台标准**：支持多种 VR/AR 设备（Quest、PICO、HTC Vive等）
- **未来扩展性**：行业标准化接口，更好的长期支持
- **性能优化**：统一的渲染管线和优化机制
- **生态兼容**：与更多第三方工具和SDK兼容

### 迁移必要性与挑战
- Oculus XR 插件将逐步被 OpenXR 取代
- Meta 官方推荐使用 OpenXR 进行新项目开发
- 更好的设备兼容性和未来扩展能力

⚠️ **PongHub 特殊挑战**：
- 项目深度依赖 OVR 特定 API（OVRHand、OVRInput、OVRPassthroughLayer 等）
- 缺乏统一的 VR 抽象层，业务逻辑与 Oculus SDK 紧耦合
- 需要重构多个核心 VR 功能模块

## 当前项目状态分析

### 当前 Oculus 依赖概览
- **Unity 包**：`com.unity.xr.oculus: 4.4.0`
- **汇编引用**：Unity.XR.Oculus, Oculus.VR, Oculus.Platform 等
- **核心组件**：OVRCameraRig, OVRInput, OVRHand, OVRPassthroughLayer
- **平台服务**：Oculus.Platform IAP 和社交功能

### 影响范围评估

#### 🔴 高影响区域（需要重大重构）
1. **手部追踪系统**
   - `EnhancedXRInputManager.cs` - OVRHand/OVRSkeleton API
   - `HandGestureRecognizer.cs` - 手势识别逻辑
   
2. **MR透视功能**
   - `MRPassthroughManager.cs` - OVRPassthroughLayer
   - `MRSafetyBoundary.cs` - OVRBoundary API

3. **平台服务集成**
   - `PHApplication.cs` - Oculus.Platform.Core
   - `IAPManager.cs` - Oculus IAP 系统

#### 🟡 中等影响区域（需要适配）
1. **输入系统**
   - `ScrollViewController.cs` - OVRInput 控制器
   - 所有使用 OVRInput.Controller 的代码

2. **场景管理**
   - `NavigationController.cs` - OVRScreenFade
   - `LocalPlayerEntities.cs` - 场景转换效果

3. **相机系统**
   - `CameraRig.prefab` - OVRCameraRig 组件
   - 预制件中的相机配置

#### 🟢 低影响区域（可能无需更改）
1. **Avatar 系统** - Meta Avatar SDK 2.0 可能继续支持
2. **音频系统** - Meta XR Audio SDK 维持兼容
3. **网络系统** - 基于 Unity Netcode，不受影响

## 迁移前准备工作

### 1. 备份与分支管理
```bash
# 创建迁移分支
git checkout -b openxr-migration

# 提交当前状态
git add .
git commit -m "backup: Pre-OpenXR migration state"

# 创建备份标签
git tag pre-openxr-backup
```

### 2. 环境准备
- 确保 Unity 2022.3.52f1+ 或 Unity 6
- 安装最新版本的 Meta XR SDK（保持向后兼容）
- 准备测试设备：Quest 2/3/Pro

### 3. 依赖关系分析
- 记录当前所有 Oculus 相关功能
- 制作功能测试清单
- 准备回滚计划

## 迁移操作步骤

### 阶段 1：包管理和插件更换

#### 1.1 安装 OpenXR 插件
1. 打开 Package Manager
2. 安装以下包：
   ```
   XR Plugin Management (已有)
   OpenXR Plugin
   XR Interaction Toolkit (已有)
   XR Hands (已有)
   ```

#### 1.2 配置 OpenXR 提供商
1. Edit > Project Settings > XR Plug-in Management
2. 启用 OpenXR 提供商（替代 Oculus）
3. 配置 OpenXR Feature Groups：
   - Meta Quest Support
   - Hand Tracking Support
   - Mixed Reality Support (如需要)

#### 1.3 OpenXR 功能配置
1. OpenXR > Feature Groups 设置：
   ```
   ✓ Meta Quest Support
   ✓ Hand Tracking Subsystem  
   ✓ XR Interaction Toolkit
   ✓ Eye Gaze Interaction (可选)
   ✓ Passthrough (MR功能)
   ```

### 阶段 2：核心组件迁移

#### 2.1 相机系统迁移

**原始配置 (OVRCameraRig)：**
```csharp
// 旧代码 - OVRCameraRig
public class CameraController : MonoBehaviour
{
    private OVRCameraRig m_cameraRig;
    private OVRManager m_ovrManager;
}
```

**新配置 (XR Origin)：**
```csharp
// 新代码 - XR Origin
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class CameraController : MonoBehaviour
{
    private XROrigin m_xrOrigin;
    private Camera m_xrCamera;
}
```

**预制件迁移步骤：**
1. 替换 `Assets/PongHub/Prefabs/App/CameraRig.prefab`
2. 删除 OVRCameraRig 组件
3. 添加 XR Origin 组件
4. 重新配置相机层次结构：
   ```
   XR Origin
   ├── Camera Offset
   │   ├── Main Camera
   │   ├── LeftHand Controller
   │   └── RightHand Controller
   └── XR Interaction Manager
   ```

#### 2.2 输入系统迁移

**OVRInput 替换：**
```csharp
// 旧代码 - OVRInput
private void UpdateInput()
{
    float thumbstickX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;
    bool buttonPressed = OVRInput.GetDown(OVRInput.Button.One);
    OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
}
```

**新代码 - XR Input System：**
```csharp
// 新代码 - XR Input System
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

private void UpdateInput()
{
    // 通过 Input Actions 或 XR Controller 获取输入
    Vector2 thumbstick = rightController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value) ? value : Vector2.zero;
    bool buttonPressed = rightController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed;
    
    // 震动通过 XR Input
    rightController.SendHapticImpulse(0.5f, 0.2f);
}
```

### 阶段 3：高影响功能重构

#### 3.1 手部追踪系统重构

**文件：`EnhancedXRInputManager.cs`**

```csharp
// 旧代码 - OVR Hand Tracking
using Oculus.Avatar2;

public class EnhancedXRInputManager : MonoBehaviour
{
    private OVRHand m_leftHand, m_rightHand;
    private OVRSkeleton m_leftHandSkeleton, m_rightHandSkeleton;
    
    private void UpdateHandTracking()
    {
        if (m_leftHand.IsTracked)
        {
            var confidence = m_leftHand.HandConfidence;
            // 处理手部数据
        }
    }
}
```

**新代码 - OpenXR Hand Tracking：**
```csharp
// 新代码 - OpenXR Hand Tracking
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class EnhancedXRInputManager : MonoBehaviour
{
    private XRHandSubsystem m_handSubsystem;
    private XRHand m_leftHand, m_rightHand;
    
    private void Start()
    {
        m_handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRHandSubsystem>();
    }
    
    private void UpdateHandTracking()
    {
        if (m_handSubsystem != null && m_handSubsystem.running)
        {
            m_handSubsystem.TryGetHand(Handedness.Left, out m_leftHand);
            if (m_leftHand.isTracked)
            {
                // 处理手部数据
                XRHandJoint thumbTip = m_leftHand.GetJoint(XRHandJointID.ThumbTip);
                if (thumbTip.TryGetPose(out Pose thumbPose))
                {
                    // 使用手部关节数据
                }
            }
        }
    }
}
```

**文件：`HandGestureRecognizer.cs`**

```csharp
// 旧代码 - OVR Gesture Recognition
public EnhancedXRInputManager.HandGesture RecognizeGesture(OVRHand hand, OVRSkeleton skeleton)
{
    if (!hand.IsTracked) return HandGesture.None;
    
    var indexFinger = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Index1];
    // 手势识别逻辑
}
```

```csharp
// 新代码 - OpenXR Gesture Recognition  
public EnhancedXRInputManager.HandGesture RecognizeGesture(XRHand hand)
{
    if (!hand.isTracked) return HandGesture.None;
    
    if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose indexPose))
    {
        // 使用新的手部关节API进行手势识别
    }
    return HandGesture.None;
}
```

#### 3.2 MR透视功能重构

**文件：`MRPassthroughManager.cs`**

```csharp
// 旧代码 - OVR Passthrough
using OVRPassthroughLayer = Meta.XR.Depth.OVRPassthroughLayer; // 假设引用

private OVRPassthroughLayer m_passthroughLayer;
private OVRManager m_ovrManager;

private void EnablePassthrough()
{
    if (m_passthroughLayer != null)
    {
        m_passthroughLayer.enabled = true;
        OVRManager.isInsightPassthroughEnabled = true;
    }
}
```

**新代码 - OpenXR Passthrough：**
```csharp
// 新代码 - OpenXR Passthrough
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

private void EnablePassthrough()
{
    // 使用 OpenXR Passthrough API
    var passthroughFeature = OpenXRSettings.Instance.GetFeature<MetaQuestFeature>();
    if (passthroughFeature != null && passthroughFeature.enabled)
    {
        // 启用透视模式
        // 注意：具体API可能因OpenXR版本而异
    }
}
```

#### 3.3 平台服务适配

**文件：`PHApplication.cs`**

```csharp
// 旧代码 - Oculus Platform
using Oculus.Platform;

private async void InitializePlatform()
{
    var coreInit = await Oculus.Platform.Core.AsyncInitialize().Gen();
    if (coreInit.IsError)
    {
        Debug.LogError("Platform init failed");
    }
}
```

**新代码 - Meta Platform SDK：**
```csharp
// 新代码 - 保持 Meta Platform SDK
// Meta Platform SDK 应该继续与 OpenXR 兼容
using Oculus.Platform;

private async void InitializePlatform()
{
    // Meta Platform SDK 可能仍然可用
    // 需要验证与 OpenXR 的兼容性
    var coreInit = await Oculus.Platform.Core.AsyncInitialize().Gen();
    if (coreInit.IsError)
    {
        Debug.LogError("Platform init failed");
    }
}
```

### 阶段 4：汇编定义文件更新

**文件：`Assets/PongHub/Scripts/PongHub.Runtime.asmdef`**

```json
// 移除 Oculus 相关引用
{
    "references": [
        // "Unity.XR.Oculus",           // ❌ 移除
        // "Oculus.VR",                 // ❌ 移除  
        "Unity.XR.OpenXR",              // ✅ 新增
        "Unity.XR.Hands",               // ✅ 新增
        "Unity.XR.CoreUtils",           // ✅ 新增
        "Unity.XR.Interaction.Toolkit", // ✅ 保留
        "Unity.XR.Management",          // ✅ 保留
        "Unity.InputSystem",            // ✅ 保留
        
        // Meta SDK 可能保留的部分
        "Oculus.Platform",              // ❓ 需验证兼容性
        "Oculus.AvatarSDK2",           // ❓ 需验证兼容性
        "Oculus.Interaction"           // ❓ 可能需要更新
    ]
}
```

### 阶段 5：配置文件迁移

#### 5.1 XR 设置迁移
1. 删除 `Assets/XR/Settings/Oculus Settings.asset`
2. 创建新的 OpenXR 配置
3. 更新 `Assets/XR/XRGeneralSettings.asset`：
   ```yaml
   # 替换 Oculus Loader 为 OpenXR Loader
   m_Loaders:
   - {fileID: 11400000, guid: [OpenXR Loader GUID]}
   ```

#### 5.2 项目设置更新
1. **XR Plug-in Management**：
   - 禁用 Oculus 提供商
   - 启用 OpenXR 提供商
   
2. **OpenXR Feature Groups**：
   ```
   Meta Quest Support: ✓
   Hand Tracking: ✓  
   Eye Tracking: ✓ (可选)
   Passthrough: ✓ (MR功能)
   ```

### 阶段 6：预制件和场景更新

#### 6.1 主要预制件迁移

**CameraRig.prefab 重构：**
1. 移除所有 OVR 组件
2. 添加 XR Origin 结构
3. 配置 XR Interaction Manager
4. 设置手部控制器预制件

**更新清单：**
```
Assets/PongHub/Prefabs/App/CameraRig.prefab          // 🔴 重大修改
Assets/PongHub/Prefabs/Input/[Controller Prefabs]   // 🟡 适配修改
Packages/com.meta.utilities.input/CameraRig.prefab  // 🔴 需要替换
```

#### 6.2 场景文件更新

**需要更新的场景：**
- `Startup.unity` - 相机装备配置
- `MainMenu.unity` - VR 交互组件

### 阶段 7：代码重构清单

#### 7.1 必须修改的文件

**🔴 高优先级（必须修改）：**
```
Assets/PongHub/Scripts/VR/EnhancedXRInputManager.cs     // 手部追踪API
Assets/PongHub/Scripts/VR/HandGestureRecognizer.cs      // 手势识别
Assets/PongHub/Scripts/MR/MRPassthroughManager.cs       // 透视功能
Assets/PongHub/Scripts/MR/MRSafetyBoundary.cs          // 边界检测
Assets/PongHub/Scripts/App/NavigationController.cs     // 场景转换
Assets/PongHub/Scripts/MainMenu/ScrollViewController.cs // 输入处理
```

**🟡 中优先级（可能需要修改）：**
```
Assets/PongHub/Scripts/App/PHApplication.cs            // 平台初始化
Assets/PongHub/Scripts/App/IAPManager.cs               // IAP集成
Assets/PongHub/Scripts/Arena/Services/LocalPlayerEntities.cs // 场景管理
```

#### 7.2 API 映射表

| Oculus XR API | OpenXR 替代 | 迁移复杂度 |
|---------------|-------------|-----------|
| `OVRCameraRig` | `XROrigin` | 🟡 中等 |
| `OVRInput.Get()` | `XR Input System` | 🟡 中等 |
| `OVRHand` | `XRHand` | 🔴 高 |
| `OVRSkeleton` | `XRHandJoint` | 🔴 高 |
| `OVRScreenFade` | 自定义实现 | 🟡 中等 |
| `OVRPassthroughLayer` | OpenXR Passthrough | 🔴 高 |
| `OVRBoundary` | XR Boundary | 🟡 中等 |
| `Oculus.Platform` | 保持不变 | 🟢 低 |

## 测试验证计划

### 阶段测试

#### 1. 基础 VR 功能验证
- [ ] 头戴设备位置追踪
- [ ] 控制器输入响应
- [ ] 6DOF 移动和旋转
- [ ] 边界系统显示

#### 2. 输入系统验证
- [ ] 控制器按钮映射
- [ ] 摇杆/触摸板输入
- [ ] 震动反馈功能
- [ ] 手部追踪精度

#### 3. PongHub 特定功能
- [ ] VR 球拍控制
- [ ] 乒乓球物理交互
- [ ] 菜单系统导航
- [ ] 模式切换功能

#### 4. 高级功能验证
- [ ] MR 透视功能
- [ ] Avatar 显示同步
- [ ] 空间音频定位
- [ ] 网络多人模式

#### 5. 性能验证
- [ ] 帧率维持 90fps
- [ ] 延迟测试
- [ ] 内存使用监控
- [ ] 电池续航影响

## 风险评估与缓解

### 高风险项

#### 1. 手部追踪精度丢失
**风险**：OpenXR 手部追踪可能与 OVR 有精度差异
**缓解**：
- 建立详细的手势识别测试用例
- 保留 OVR 版本作为对比基准
- 考虑手势识别算法调优

#### 2. MR 透视功能兼容性
**风险**：OpenXR Passthrough API 可能功能受限
**缓解**：
- 验证 Meta Quest 对 OpenXR Passthrough 的支持程度
- 准备功能降级方案
- 测试不同设备的兼容性

#### 3. 平台服务中断
**风险**：Oculus Platform SDK 与 OpenXR 兼容性问题
**缓解**：
- 验证 Meta Platform SDK 的 OpenXR 兼容性
- 准备替代的社交和 IAP 解决方案
- 测试所有平台功能

### 中风险项

#### 1. 性能回归
**风险**：OpenXR 可能有性能开销
**缓解**：
- 建立性能基准测试
- 使用 Unity Profiler 持续监控
- 优化渲染设置

#### 2. 输入延迟增加
**风险**：XR Input System 可能增加输入延迟
**缓解**：
- 测量输入响应时间
- 优化输入处理管线
- 验证 VR 交互的流畅性

### 回滚计划

```bash
# 如果迁移失败，快速回滚
git checkout pre-openxr-backup
git checkout -b rollback-openxr

# 恢复 Oculus XR 插件配置
# 重新启用 Oculus Provider
# 恢复原始汇编定义文件
```

## 验收标准

### 功能完整性
- [ ] 所有原有 VR 功能正常工作
- [ ] 手部追踪精度满足游戏需求
- [ ] MR 透视功能可用（如需要）
- [ ] 平台服务（IAP、社交）正常

### 性能指标
- [ ] 帧率维持 90fps（与原版本一致）
- [ ] 输入延迟 < 20ms
- [ ] 内存使用无显著增长
- [ ] 电池续航无明显下降

### 兼容性
- [ ] Quest 2/3/Pro 设备支持
- [ ] Unity 编辑器 XR 模拟器正常
- [ ] 网络多人模式稳定
- [ ] 构建和部署成功

### 代码质量
- [ ] 无编译错误和警告
- [ ] 代码审查通过
- [ ] 单元测试覆盖关键功能
- [ ] 文档更新完整

## 时间估算

### 分阶段时间安排

⚠️ **基于 PongHub 项目的深度 OVR 依赖重新评估**：

- **准备阶段**：2-3 天（包括抽象层设计）
- **包管理和基础配置**：1-2 天
- **核心组件迁移**：5-7 天（相机、输入系统完全重构）
- **手部追踪重构**：5-7 天（OVRHand → XRHand 完全重写）
- **MR 功能适配**：3-5 天（OVRPassthroughLayer 替换）
- **抽象层实现**：3-5 天（新增 VR 抽象层）
- **测试验证**：5-7 天（全面功能回归测试）
- **优化和调试**：3-5 天

**修正总计：25-40 工作日**

### 与参考项目对比
| 项目 | 迁移工作量 | 原因 |
|------|-----------|------|
| **UltimateGloveBall** | 1-3 天 | 有 Meta Utilities 抽象层 |
| **PongHub** | 25-40 天 | 深度 OVR API 依赖，需要重构 |

### 关键里程碑
1. **Week 1-2**：基础配置、抽象层设计和核心组件迁移
2. **Week 3-4**：手部追踪和 MR 功能重构
3. **Week 5-6**：全面测试、性能优化和文档更新
4. **Week 7-8**：预留缓冲时间和最终验收

## 迁移后优化建议

### 1. 利用 OpenXR 新功能
- 探索跨设备兼容性
- 集成新的 XR 功能扩展
- 优化多平台支持

### 2. 代码现代化
- 采用最新的 XR Interaction Toolkit 模式
- 优化输入系统架构
- 改进错误处理和日志记录

### 3. 性能优化
- 基于 OpenXR 的渲染优化
- 减少 API 调用开销
- 优化手部追踪算法

### 4. 扩展性准备
- 为其他 VR 设备做准备
- 设计可插拔的 VR 适配层
- 建立设备特性检测机制

## 结论与建议

### 为什么 PongHub 迁移比参考项目复杂？

1. **架构模式差异**
   - **UltimateGloveBall**：使用 Meta Utilities 抽象层，OpenXR 迁移主要是配置变更
   - **PongHub**：直接调用 OVR API，需要大量代码重构

2. **依赖深度差异**
   - **UltimateGloveBall**：松耦合，通过抽象接口使用 VR 功能
   - **PongHub**：紧耦合，业务逻辑直接绑定 Oculus SDK

3. **设计理念差异**
   - **UltimateGloveBall**：从设计之初考虑跨平台兼容性
   - **PongHub**：针对 Oculus 平台深度优化，单一平台设计

### 推荐策略

考虑到 PongHub 项目的复杂性，建议：

1. **评估 ROI**：权衡迁移成本（25-40 工作日）与收益
2. **分阶段迁移**：先实现抽象层，再逐步替换 OVR API
3. **并行开发**：保持 Oculus 版本同时开发 OpenXR 版本
4. **长期规划**：将此作为架构重构的机会，建立更好的抽象层

---

*本 SOP 基于对 PongHub 项目深度 Oculus 集成的详细分析制定。与参考项目 UltimateGloveBall 相比，PongHub 的迁移复杂度显著更高，需要充分的时间和资源投入。建议在执行前仔细评估投入产出比。*