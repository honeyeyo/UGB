# PongHub Unity 6 升级标准操作程序 (SOP)

## 概述
本文档基于参考项目 Commit b842cdb 的修改点，提供 PongHub 项目从 Unity 2022.3.52f1 升级到 Unity 6000.0.50f1 的详细操作步骤。

## 当前项目状态
- 当前版本：Unity 2022.3.52f1
- 目标版本：Unity 6000.0.50f1
- 项目类型：VR 乒乓球游戏（Meta Quest）
- 使用 URP + Meta XR SDK + Unity Netcode

## 升级前准备工作

### 1. 备份与分支管理
```bash
# 创建升级分支
git checkout -b unity6-upgrade

# 提交当前状态
git add .
git commit -m "backup: Pre-Unity6 upgrade state"

# 创建备份标签
git tag pre-unity6-backup
```

### 2. 文档当前状态
- 记录当前 Unity 版本：2022.3.52f1
- 记录当前包版本（参考 Packages/manifest.json）
- 记录项目设置（ProjectSettings/）

## 升级操作步骤

### 阶段 1：Unity 编辑器升级

#### 1.1 安装 Unity 6
- 下载并安装 Unity 6000.0.50f1
- 确保包含 Meta Quest 支持模块
- 安装 Android Build Support

#### 1.2 项目转换
- 使用 Unity 6 打开项目
- 接受项目升级提示
- 等待自动转换完成

### 阶段 2：包依赖更新

#### 2.1 核心包自动更新
Unity 6 会自动更新以下包到兼容版本：
- Universal Render Pipeline (URP)
- TextMeshPro
- XR Interaction Toolkit
- Input System
- Netcode for GameObjects

#### 2.2 Meta XR SDK 兼容性检查
检查 Meta XR SDK 包是否需要更新：
- `com.meta.xr.sdk.core`
- `com.meta.xr.sdk.interaction`
- `com.meta.xr.sdk.audio`
- `com.meta.xr.sdk.avatars`

访问 Meta 开发者文档确认 Unity 6 兼容版本。

#### 2.3 第三方包验证
验证以下自定义包的 Unity 6 兼容性：
- `com.alexeyperov.unity-dependencies-hunter`
- `com.gamelovers.mcp-unity`
- `com.marijnzwemmer.unity-toolbar-extender`
- `com.veriorpies.parrelsync`

### 阶段 3：材质和资源更新

#### 3.1 URP 材质自动更新
- Unity 会自动转换 URP 材质
- 检查转换日志确认无错误
- 验证材质渲染正确性

#### 3.2 TextMeshPro 资源更新
- Window > TextMeshPro > Import TMP Essential Resources
- 更新现有 TextMeshPro 设置
- 检查字体和样式是否正常

#### 3.3 光照烘焙
重新烘焙场景光照：
- Arena 场景光照烘焙
- MainMenu 场景光照烘焙
- Window > Rendering > Lighting > Generate Lighting

### 阶段 4：代码修改

#### 4.1 过时 API 更新
根据 Unity 6 迁移指南更新以下代码：

**Rigidbody API 更改：**
```csharp
// 旧代码
rigidbody.velocity = newVelocity;
rigidbody.angularVelocity = newAngularVelocity;

// 新代码  
rigidbody.linearVelocity = newVelocity;
rigidbody.angularVelocity = newAngularVelocity;
```

**FindObjectOfType 更新：**
```csharp
// 旧代码
FindObjectOfType<ComponentType>()

// 新代码
FindFirstObjectByType<ComponentType>()
```

#### 4.2 搜索并替换过时代码
在项目中搜索并替换：
- `FindObjectOfType` → `FindFirstObjectByType`
- `FindObjectsOfType` → `FindObjectsByType`
- `rigidbody.velocity` → `rigidbody.linearVelocity`

### 阶段 5：项目设置修复

#### 5.1 应用项目设置工具修复
- Edit > Project Settings > XR Plug-in Management
- 应用推荐的 VR 设置
- 验证 Meta Quest 配置

#### 5.2 Android 清单重新生成
- Player Settings > Android > Publishing Settings
- 重新生成 AndroidManifest.xml
- 验证权限和配置正确

### 阶段 6：忽略文件更新

#### 6.1 更新 .gitignore
添加 Unity 6 特定的忽略项：
```gitignore
# Unity 6 specific
*.utmp
*.tmp

# Unity 6 cache
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
```

### 阶段 7：测试验证

#### 7.1 编译测试
- 确保项目无编译错误
- 运行 Unity Test Runner 中的所有测试
- 验证核心功能：
  - GameModeManager 模式切换
  - VR 输入系统
  - 网络连接
  - 音频系统

#### 7.2 VR 功能测试
- Quest Link 连接测试
- XR Device Simulator 测试
- 基本 VR 交互验证
- 网络多人模式测试

#### 7.3 性能验证
- 检查帧率是否符合 VR 要求（90fps）
- 验证内存使用情况
- 测试输入系统性能

## 升级后清理工作

### 1. 文档更新
- 更新 CLAUDE.md 中的 Unity 版本信息
- 更新 README.md
- 重新生成编辑器内教程

### 2. 提交更改
```bash
# 提交升级更改
git add .
git commit -m "feat: upgrade to Unity 6000.0.50f1

- Update Unity Editor to 6000.0.50f1
- Update packages for Unity 6 compatibility
- Automatic URP material updates
- Update TextMeshPro assets
- Rebake Arena and MainMenu lighting
- Update deprecated APIs (velocity -> linearVelocity)
- Apply Project Setup tools fixes
- Regenerate AndroidManifest
- Update obsolete code (FindObjectOfType -> FindFirstObjectByType)
- Add .utmp to .gitignore
- Update documentation"
```

### 3. 测试部署
- 构建 Meta Quest APK
- 进行完整的 VR 测试
- 验证网络功能正常

## 潜在问题和解决方案

### 常见问题
1. **包兼容性问题**
   - 检查包发布者的 Unity 6 兼容性声明
   - 寻找替代包或等待更新

2. **VR 功能异常**
   - 重新配置 XR 设置
   - 验证 Meta XR SDK 版本

3. **网络功能问题**
   - 检查 Netcode for GameObjects 兼容性
   - 验证 Photon 集成

4. **性能回归**
   - 使用 Unity Profiler 分析
   - 调整 VR 渲染设置

### 回滚计划
如果升级失败：
```bash
# 回滚到升级前状态
git checkout pre-unity6-backup
git checkout -b rollback-unity6
```

## 验收标准

升级成功的标准：
- [ ] 项目在 Unity 6 中无编译错误
- [ ] 所有自动化测试通过
- [ ] VR 基本功能正常（头部追踪、控制器输入）
- [ ] 网络多人模式可用
- [ ] 帧率达到 VR 标准（90fps）
- [ ] 音频系统正常工作
- [ ] UI 系统响应正常
- [ ] 可成功构建并部署到 Quest 设备

## 时间估算

- 准备工作：30 分钟
- Unity 升级：1 小时
- 代码修改：2-3 小时
- 测试验证：2-3 小时
- **总计：5.5-7.5 小时**

## 注意事项

1. **备份重要性**：始终保持完整备份
2. **分步验证**：每个阶段完成后进行测试
3. **文档更新**：及时更新项目文档
4. **团队沟通**：升级过程中保持团队沟通

---

*本 SOP 基于 PongHub 项目特点制定，升级过程中如遇特殊情况请参考 Unity 官方迁移指南。*