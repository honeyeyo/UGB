# PongHub Meta XR SDK v76 升级标准操作程序 (SOP)

## 概述
本文档基于参考项目 Unity-UltimateGloveBall commit 5f20383 的更新经验，提供 PongHub 项目从 Meta XR SDK v72 升级到 v76 的详细操作步骤。

## 当前项目状态
- 当前 Meta XR SDK 版本：v72.0.0
- 目标 Meta XR SDK 版本：v76.0.0
- Unity 版本：2022.3.52f1（或 Unity 6.0 如已升级）
- 项目类型：VR 乒乓球游戏（Meta Quest）

## 升级前准备工作

### 1. 备份与分支管理
```bash
# 创建升级分支
git checkout -b meta-xr-sdk-v76-upgrade

# 提交当前状态
git add .
git commit -m "backup: Pre-Meta XR SDK v76 upgrade state"

# 创建备份标签
git tag pre-meta-xr-v76-backup
```

### 2. 文档当前状态
- 记录当前 Meta XR SDK 包版本
- 记录当前 Samples 文件夹内容
- 检查当前 VR 功能工作状态

### 3. 检查当前 Meta XR SDK 包
当前项目中的 Meta XR SDK 相关包：
```json
"com.meta.xr.sdk.audio": "72.0.0",
"com.meta.xr.sdk.avatars": "33.0.0",
"com.meta.xr.sdk.core": "72.0.0",
"com.meta.xr.sdk.interaction": "72.0.0",
"com.meta.xr.sdk.interaction.ovr": "72.0.0",
"com.meta.xr.sdk.platform": "72.0.0"
```

## 升级操作步骤

### 阶段 1：Meta XR SDK 包更新

#### 1.1 通过 Package Manager 更新核心包
1. 打开 Window > Package Manager
2. 从 "In Project" 切换到 "Unity Registry"
3. 搜索并更新以下包到 v76.0.0：
   - Meta XR Core SDK (`com.meta.xr.sdk.core`)
   - Meta XR Interaction SDK (`com.meta.xr.sdk.interaction`)
   - Meta XR Interaction SDK OVR Integration (`com.meta.xr.sdk.interaction.ovr`)
   - Meta XR Audio SDK (`com.meta.xr.sdk.audio`)
   - Meta XR Platform SDK (`com.meta.xr.sdk.platform`)

#### 1.2 Avatar SDK 兼容性检查
- 检查 `com.meta.xr.sdk.avatars` 是否需要更新
- 确认 Avatar SDK 与 Meta XR SDK v76 的兼容性
- 如需要，更新到兼容版本

#### 1.3 验证包依赖关系
- 让 Unity 自动解析包依赖关系
- 确认没有包冲突警告
- 检查 Console 中是否有兼容性错误

### 阶段 2：Samples 资源更新

#### 2.1 清理旧版本 Samples
删除 `Assets/Samples/` 下的旧版本文件夹：
- `Meta XR Audio SDK/72.0.0/`
- `Meta XR Core SDK/72.0.0/`
- `Meta XR Interaction SDK Essentials/72.0.0/`
- `Meta XR Interaction ​SDK/72.0.0/`
- `Meta XR Platform SDK/72.0.0/`

#### 2.2 导入新版本 Samples
1. 在 Package Manager 中选择各个 Meta XR SDK 包
2. 展开 Samples 部分
3. 重新导入必要的 Sample 资源：
   - **Core SDK Samples**：基础 VR 功能示例
   - **Interaction SDK Essentials**：基本交互组件
   - **Audio SDK Samples**：空间音频示例
   - **Platform SDK Samples**：平台功能示例

### 阶段 3：项目配置更新

#### 3.1 OVR 配置验证
1. 检查 `Assets/Oculus/OculusProjectConfig.asset` 设置
2. 验证 XR Plugin Management 设置：
   - Edit > Project Settings > XR Plug-in Management
   - 确认 Oculus 提供商已启用
3. 检查 Oculus 设置：
   - Assets/XR/Settings/Oculus Settings.asset
   - 验证目标设备和功能设置

#### 3.2 Audio 配置更新
1. 检查 Meta XR Audio 设置：
   - `Assets/Resources/MetaXRAudioSettings.asset`
   - `Assets/Resources/MetaXRAcousticSettings.asset`
   - `Assets/Resources/MetaXRAcousticMaterialMapping.asset`
2. 验证音频管理器配置是否需要更新
3. 测试空间音频功能

#### 3.3 Platform 配置检查
1. 验证 Oculus Platform 设置：
   - `Assets/Resources/OculusPlatformSettings.asset`
   - `Assets/Resources/OVRPlatformToolSettings.asset`
2. 检查应用 ID 和平台配置

### 阶段 4：代码适配和 API 更新

#### 4.1 检查 Breaking Changes
根据 Meta XR SDK v76 发布说明，检查以下可能的 API 变更：

**OVR 组件 API 变更：**
```csharp
// 可能需要更新的 API 调用
// 检查 OVRCameraRig 相关代码
// 检查 OVRInput 输入处理
// 检查 Avatar 相关 API
```

**XR Interaction Toolkit 集成：**
```csharp
// 检查 XR 交互组件的兼容性
// 验证手部追踪 API
// 检查控制器输入映射
```

#### 4.2 搜索并更新过时代码
在项目中搜索可能过时的 API：
- 搜索 `OVR` 相关类的使用
- 检查 `Meta.XR` 命名空间的使用
- 验证 Avatar 相关代码

#### 4.3 编译验证
- 确保项目无编译错误
- 解决任何 API 兼容性问题
- 更新引用过时方法的代码

### 阶段 5：VR 功能验证

#### 5.1 基础 VR 功能测试
1. **头部追踪**：验证头戴设备位置追踪正常
2. **控制器输入**：测试左右控制器输入响应
3. **手部追踪**：如使用，验证手部追踪精度
4. **边界系统**：检查游戏区域边界显示

#### 5.2 PongHub 特定功能测试
1. **球拍控制**：验证 VR 球拍操控响应
2. **球物理**：测试乒乓球与 VR 交互
3. **空间音频**：验证 3D 音效定位
4. **UI 交互**：测试 VR 菜单系统交互

#### 5.3 网络功能验证
1. **Avatar 同步**：测试多人模式下 Avatar 显示
2. **位置同步**：验证玩家位置网络同步
3. **交互同步**：测试球拍和球的网络同步

### 阶段 6：性能优化验证

#### 6.1 帧率检查
- 使用 Unity Profiler 检查帧率
- 确保维持 90fps VR 标准
- 检查新版本 SDK 的性能影响

#### 6.2 内存使用分析
- 检查内存使用是否有显著变化
- 验证 Audio SDK 内存占用
- 监控 Avatar 系统内存使用

#### 6.3 渲染性能
- 验证渲染管线兼容性
- 检查新 SDK 对 URP 的影响
- 测试复杂场景的渲染性能

### 阶段 7：设备兼容性测试

#### 7.1 Quest 设备测试
1. **Quest 2**：基础兼容性验证
2. **Quest 3/3S**：新功能支持测试
3. **Quest Pro**：企业功能验证

#### 7.2 功能特性测试
1. **混合现实 (MR)**：如支持，测试透视功能
2. **新输入方法**：测试新版本 SDK 的输入增强
3. **性能提升**：验证官方声称的性能改进

## 升级后清理工作

### 1. 配置文件更新
- 更新 CLAUDE.md 中的 Meta XR SDK 版本信息
- 更新项目依赖文档
- 记录升级过程中的问题和解决方案

### 2. 文档更新
```bash
# 更新 Packages/manifest.json 引用
# 更新开发文档中的 SDK 版本
# 更新团队开发指南
```

### 3. 提交更改
```bash
# 提交升级更改
git add .
git commit -m "feat: upgrade Meta XR SDK to v76.0.0

- Update Meta XR Core SDK to 76.0.0
- Update Meta XR Interaction SDK to 76.0.0  
- Update Meta XR Audio SDK to 76.0.0
- Update Meta XR Platform SDK to 76.0.0
- Refresh Samples to v76 versions
- Verify VR functionality compatibility
- Update project configurations
- Performance optimization verification"
```

## 潜在问题和解决方案

### 常见问题

#### 1. **包依赖冲突**
```
解决方案：
- 清理 Library/ 文件夹
- 重新导入所有包
- 检查 packages-lock.json
```

#### 2. **Avatar SDK 兼容性**
```
解决方案：
- 检查 Avatar SDK 版本兼容性
- 更新到最新兼容版本
- 重新配置 Avatar 设置
```

#### 3. **VR 交互异常**
```
解决方案：
- 重新配置 XR Interaction Toolkit
- 检查输入映射设置
- 验证控制器预制件
```

#### 4. **性能回归**
```
解决方案：
- 使用 Unity Profiler 分析
- 检查新版本的优化设置
- 调整渲染设置
```

#### 5. **编译错误**
```
解决方案：
- 检查 API 变更文档
- 更新过时的 API 调用
- 重新配置项目设置
```

### 回滚计划
如果升级失败：
```bash
# 回滚到升级前状态
git checkout pre-meta-xr-v76-backup
git checkout -b rollback-meta-xr-v76

# 恢复旧版本包
# 在 Package Manager 中降级到 v72.0.0
```

## 验收标准

升级成功的标准：
- [ ] 所有 Meta XR SDK 包已更新到 v76.0.0
- [ ] 项目编译无错误和警告
- [ ] VR 基本功能正常（头部追踪、控制器输入）
- [ ] PongHub 特定功能正常（球拍控制、球物理）
- [ ] 空间音频系统工作正常
- [ ] 网络多人模式 Avatar 同步正常
- [ ] 帧率维持 90fps VR 标准
- [ ] 内存使用无异常增长
- [ ] 可成功构建并部署到 Quest 设备
- [ ] 所有自动化测试通过

## Meta XR SDK v76 新功能探索

### 1. 性能改进
- 渲染优化
- 内存管理改进
- CPU/GPU 负载平衡

### 2. 新 API 功能
- 增强的手部追踪
- 改进的空间音频
- 新的交互模式

### 3. 开发者工具
- 改进的调试工具
- 新的性能分析器
- 增强的编辑器集成

## 时间估算

- 准备工作：30 分钟
- 包更新：1 小时
- 配置验证：1 小时
- 代码适配：1-2 小时
- 功能测试：2-3 小时
- 性能验证：1 小时
- **总计：6.5-8.5 小时**

## 最佳实践建议

1. **分步骤升级**：不要一次性更新所有包
2. **及时测试**：每个阶段完成后立即测试
3. **保留备份**：确保可以快速回滚
4. **文档记录**：记录遇到的问题和解决方案
5. **团队沟通**：升级过程中保持团队同步

## 风险评估

### 高风险项
- Avatar 系统兼容性
- 网络同步功能
- 自定义 VR 交互代码

### 中风险项
- 音频系统配置
- 性能优化设置
- UI 交互系统

### 低风险项
- 基础 VR 追踪
- 标准控制器输入
- 基本渲染功能

## 后续优化建议

1. **利用新功能**：探索 v76 的新 API 和功能
2. **性能调优**：基于新版本优化项目性能
3. **用户体验**：利用新功能改善 VR 体验
4. **代码现代化**：更新到最新的最佳实践

---

*本 SOP 基于 PongHub 项目特点和 Meta XR SDK v76 特性制定，升级过程中如遇特殊情况请参考 Meta 官方迁移指南和发布说明。*