# 详细迁移步骤指南

## 🎯 **Phase 1: 准备工作**

### 1.1 备份场景文件

```bash
# 在 Unity 项目根目录执行
# 备份 Gym 场景
cp "Assets/TirgamesAssets/SchoolGym/Gym.unity" "Assets/TirgamesAssets/SchoolGym/Gym_Backup.unity"

# 备份 Arena 场景
cp "Assets/UltimateGloveBall/Scenes/Arena.unity" "Assets/UltimateGloveBall/Scenes/Arena_Backup.unity"
```

### 1.2 在 Unity 中的准备

1. 打开 **Gym.unity** 场景
2. 创建新的空 GameObject 作为组织结构：
   - `Game Systems`
   - `Audio Systems`
   - `UI Systems`
   - `Technical`

## 🎮 **Phase 2: 核心游戏系统迁移**

### 2.1 GameManager 迁移

**在 Unity Editor 中操作：**

1. **打开 Arena 场景** → 选择 `GameManager` GameObject
2. **复制** (Ctrl+C)
3. **切换到 Gym 场景** → 选择 `Game Systems` 父对象
4. **粘贴** (Ctrl+V)
5. **检查组件完整性**：
   ```
   ✓ Transform
   ✓ NetworkObject (网络同步)
   ✓ GameManager Script (游戏逻辑)
   ```

### 2.2 ArenaLifeCycle 迁移

**重复上述步骤迁移：**

- 从 Arena 场景复制 `ArenaLifeCycle`
- 粘贴到 Gym 场景的 `Game Systems` 下

### 2.3 ScoreboardController 迁移

1. **复制 ScoreboardController GameObject**
2. **复制关联的 ScoreBoard_art** (计分板艺术资源)
3. **粘贴到 Gym 场景的 `Game Systems` 下**

## 🎵 **Phase 3: 音频系统迁移**

### 3.1 MusicManager 迁移

**操作步骤：**

1. 从 Arena 复制 `MusicManager` GameObject
2. 粘贴到 Gym 场景的 `Audio Systems` 下
3. **验证组件**：
   ```
   ✓ AudioSource (音频播放器)
   ✓ MusicManager Script (音乐控制脚本)
   ✓ FadeVolume Script (音量淡入淡出)
   ```

### 3.2 centerAudioSource 迁移

1. 从 Arena 复制 `centerAudioSource` GameObject
2. 粘贴到 Gym 场景的 `Audio Systems` 下

## 🖥️ **Phase 4: UI 系统迁移**

### 4.1 PointableCanvasModule 迁移

1. 从 Arena 复制 `PointableCanvasModule` GameObject
2. 粘贴到 Gym 场景的 `UI Systems` 下

### 4.2 主要 UI 组件迁移

**按顺序迁移以下 UI 组件：**

1. `StartGame` (已包含 Canvas 组件)
2. `Countdown` UI
3. `InviteFriends` UI

**每个 UI 组件迁移后需要验证：**

```
✓ Canvas 组件配置
✓ CanvasScaler 设置
✓ GraphicRaycaster 配置
✓ PointableElement 交互组件
✓ BoxCollider 交互区域
```

## 🔧 **Phase 5: 技术组件迁移**

### 5.1 NavMeshPlane 迁移

1. 从 Arena 复制 `NavMeshPlane`
2. 粘贴到 Gym 场景的 `Technical` 下
3. **调整尺寸适应体育馆空间**

### 5.2 LightingSetup 迁移

1. 从 Arena 复制 `LightingSetup-ApplyBefore GeneratingLight`
2. 粘贴到 Gym 场景的 `Technical` 下

## 🔗 **Phase 6: 引用关系修复**

### 6.1 GameManager 引用修复

**在 GameManager 组件中重新连接以下引用：**

```csharp
// 需要在 Inspector 中重新连接的字段：
m_gameState: [需要找到对应的游戏状态对象]
m_startGameButtonContainer: [连接到迁移的 StartGame UI]
m_restartGameButtonContainer: [连接到相应的重启按钮]
m_inviteFriendButtonContainer: [连接到迁移的 InviteFriends UI]
m_ballSpawner: [连接到球生成器 - 可能需要创建新的]
m_countdownView: [连接到迁移的 Countdown UI]
m_obstacleManager: [连接到障碍物管理器]
m_postGameView: [连接到游戏结束界面]
m_courtAudioSource: [连接到迁移的 centerAudioSource]
```

### 6.2 MusicManager 引用修复

```csharp
// 在 MusicManager 组件中重新连接：
m_gameManager: [连接到迁移的 GameManager]
m_musicAudioSource: [应该自动连接，检查确认]
```

### 6.3 UI 组件引用修复

**检查所有 UI 按钮的 OnClick 事件：**

- StartGame 按钮 → GameManager.StartGame()
- 其他 UI 交互 → 对应的管理器方法

## 🎨 **Phase 7: 场景整合与优化**

### 7.1 场景层次结构重组

**按照优化方案重新组织现有的 Gym 场景内容：**

```
Gym Scene Root
├── 🎮 Game Systems (新迁移的)
│   ├── GameManager
│   ├── ArenaLifeCycle
│   ├── ScoreboardController
│   └── Ball (保留原有的，移动到此处)
│
├── 🎵 Audio Systems (新迁移的)
│   ├── MusicManager
│   └── centerAudioSource
│
├── 🖥️ UI Systems (新迁移的)
│   ├── PointableCanvasModule
│   ├── StartGame (Canvas)
│   ├── Countdown
│   └── InviteFriends
│
├── 🏢 Environment (重新组织)
│   ├── Architecture
│   │   └── [体育馆建筑结构]
│   ├── Gym Equipment
│   │   ├── Exercise Equipment
│   │   │   ├── SportLadder01b (系列)
│   │   │   ├── SportBench01 (系列)
│   │   │   └── SportBrus1 (系列)
│   │   └── Storage
│   │       └── Door02_1 (系列)
│   └── Furniture
│       ├── Lighting Fixtures
│       │   └── LightLum01_03 (系列)
│       └── Decorative Items
│
├── 💡 Lighting System (保留+优化)
│   ├── Main Lighting
│   │   └── Directional Light (保留 Gym 的)
│   ├── AreaLights (保留 Gym 的)
│   │   ├── Ceiling Lights
│   │   ├── Wall Lights
│   │   └── Accent Lights
│   └── Light Probe Group (保留 Gym 的，迁移后重新烘焙)
│
├── 🎨 Post Processing
│   └── Global Volume (保留 Gym 的)
│
└── 📐 Technical (新迁移的)
    ├── NavMeshPlane
    ├── LightingSetup
    └── ScoreBoard_art
```

### 7.2 性能优化设置

1. **标记静态物件**：

   - 选择所有装饰性物件
   - 在 Inspector 中勾选 `Static`
   - 选择适当的 Static 标记类型

2. **光照优化**：
   - 检查光照设置
   - 重新烘焙光照贴图（如果需要）

## ✅ **Phase 8: 测试验证**

### 8.1 基础功能测试

**在 Unity Play Mode 中测试：**

- [ ] 场景能正常加载
- [ ] 没有 Missing Reference 错误
- [ ] GameManager 初始化正常
- [ ] UI 显示正常

### 8.2 VR 功能测试

**在 VR 模式下测试：**

- [ ] 球的 VR 交互功能
- [ ] UI 的手势交互
- [ ] 音频播放正常
- [ ] 空间追踪正常

### 8.3 游戏流程测试

- [ ] 游戏开始流程
- [ ] 计分功能
- [ ] 音乐切换
- [ ] UI 响应

## 🚨 **常见问题解决**

### Missing Reference 错误

```
问题：迁移后出现 Missing Reference
解决：在相应的组件中重新连接引用
```

### UI 交互不响应

```
问题：VR UI 无法交互
解决：检查 PointableCanvasModule 是否正确配置
```

### 音频不播放

```
问题：音频组件无声音
解决：检查 AudioMixerGroup 引用是否正确
```

### 网络同步问题

```
问题：多人游戏同步异常
解决：检查 NetworkObject 组件配置
```

## 📝 **完成后的检查清单**

- [ ] 所有 Arena 核心功能已迁移
- [ ] Gym 场景环境保持完整
- [ ] 场景层次结构清晰有序
- [ ] 性能优化设置已应用
- [ ] VR 交互功能正常
- [ ] 音频系统工作正常
- [ ] UI 系统响应正常
- [ ] 游戏逻辑运行正常
- [ ] 无 Console 错误
- [ ] 场景文件已保存
