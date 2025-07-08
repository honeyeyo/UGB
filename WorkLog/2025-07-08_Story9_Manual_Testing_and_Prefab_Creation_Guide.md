# Story-9 手动测试和预制件制作操作指南

**日期**: 2025年7月8日
**Story**: Epic-2 Story-9 创建模式切换界面
**状态**: 代码实现完成，需要手动测试和预制件制作

## 概述

Story-9的核心代码实现已完成，包括：
- ✅ SinglePlayerModePanel（单机模式面板）
- ✅ MultiplayerModePanel（多人模式面板）
- ✅ ModeTransitionEffect（模式切换动画）
- ✅ ModeSelectionAudioHaptics（音效和触觉反馈）

现在需要进行手动测试和预制件制作来完成Story-9的完整实现。

## 第一阶段：预制件创建

### 1.1 创建模式选择主面板预制件

**操作步骤：**

1. **创建基础Canvas**
   - 在Hierarchy中右键 → UI → Canvas
   - 命名为 "ModeSelectionCanvas"
   - 设置Canvas Scaler：
     - UI Scale Mode: Scale With Screen Size
     - Reference Resolution: 1920x1080
     - Screen Match Mode: Match Width Or Height
     - Match: 0.5

2. **创建模式选择主面板**
   - 在ModeSelectionCanvas下创建空GameObject，命名为 "ModeSelectionPanel"
   - 添加RectTransform组件，设置Anchor: Center-Middle
   - 添加Image组件作为背景
   - 添加Canvas Group组件用于统一控制透明度

3. **创建标题区域**
   - 在ModeSelectionPanel下创建空GameObject，命名为 "TitleArea"
   - 添加TextMeshPro - Text (UI)组件，命名为 "TitleText"
   - 设置字体、大小、颜色等属性
   - 本地化键：设置为 "mode_selection.title"

4. **创建模式卡片容器**
   - 创建空GameObject，命名为 "ModesContainer"
   - 添加GridLayoutGroup组件：
     - Cell Size: (300, 200)
     - Spacing: (20, 20)
     - Start Corner: Upper Left
     - Start Axis: Horizontal
     - Child Alignment: Upper Center
     - Constraint: Fixed Column Count, Count: 2

5. **创建模式卡片预制件**
   - 在Project面板中创建新的预制件文件夹：Prefabs/UI/ModeSelection
   - 在ModesContainer下创建第一个模式卡片作为模板：
     ```
     ModeCard (GameObject)
     ├── Background (Image) - 卡片背景
     ├── Icon (Image) - 模式图标
     ├── Title (TextMeshPro) - 模式标题
     ├── Description (TextMeshPro) - 模式描述
     ├── Button (Button) - 点击按钮
     └── HoverEffect (Image) - 悬停效果
     ```
   - 添加ModeCard脚本组件
   - 配置所有UI元素的引用
   - 创建预制件：将ModeCard拖到Project面板

6. **添加控制脚本**
   - 为ModeSelectionPanel添加ModeSelectionPanel脚本
   - 配置所有序列化字段的引用

### 1.2 创建单机模式面板预制件

**操作步骤：**

1. **创建单机模式主容器**
   - 在ModeSelectionCanvas下创建 "SinglePlayerModePanel"
   - 设置为默认不激活状态

2. **创建练习模式区域**
   - 创建 "PracticePanel" 容器
   - 添加练习模式选项按钮：
     - "FreePracticeButton" - 自由练习
     - "TargetPracticeButton" - 目标练习
     - "SkillChallengeButton" - 技能挑战

3. **创建AI对战区域**
   - 创建 "AIPanel" 容器
   - 添加难度选择界面：
     - "DifficultyContainer" - 难度按钮容器
     - "DifficultySlider" - 难度滑块
     - "DifficultyText" - 难度显示文本

4. **创建个人统计区域**
   - 创建 "StatsPanel" 容器
   - 添加统计显示文本：
     - "TotalGamesText" - 总场次
     - "WinRateText" - 胜率
     - "BestScoreText" - 最高分
     - "PlayTimeText" - 游戏时间
     - "LastPlayedText" - 上次游戏时间

5. **添加控制脚本和配置**
   - 添加SinglePlayerModePanel脚本
   - 配置所有UI元素引用
   - 创建预制件

### 1.3 创建多人模式面板预制件

**操作步骤：**

1. **创建多人模式主容器**
   - 创建 "MultiplayerModePanel"
   - 设置主菜单按钮：
     - "CreateRoomMenuButton"
     - "JoinRoomMenuButton"
     - "FriendsMenuButton"
     - "QuickMatchButton"

2. **创建房间创建界面**
   - 创建 "CreateRoomPanel"
   - 添加输入字段：
     - "RoomNameInput" - 房间名称
     - "PlayerNameInput" - 玩家名称
     - "PrivateRoomToggle" - 私有房间开关
     - "MaxPlayersDropdown" - 最大玩家数
     - "DifficultyDropdown" - 难度选择

3. **创建房间浏览界面**
   - 创建 "RoomBrowserPanel"
   - 设置Scroll View用于房间列表
   - 创建房间项预制件 "RoomItemPrefab"
   - 添加搜索和刷新功能

4. **创建好友列表界面**
   - 创建 "FriendsPanel"
   - 设置Scroll View用于好友列表
   - 创建好友项预制件 "FriendItemPrefab"
   - 添加好友搜索功能

5. **添加控制脚本和配置**
   - 添加MultiplayerModePanel脚本
   - 配置所有UI元素引用
   - 创建预制件

### 1.4 创建过渡效果预制件

**操作步骤：**

1. **创建过渡效果容器**
   - 创建空GameObject，命名为 "ModeTransitionEffect"
   - 添加ModeTransitionEffect脚本

2. **创建过渡遮罩**
   - 在ModeTransitionEffect下创建 "TransitionOverlay"
   - 添加Image组件，设置为全屏覆盖
   - 设置颜色为黑色，初始透明度为0

3. **配置动画参数**
   - 设置过渡时间、淡入淡出时间
   - 配置动画曲线
   - 设置音效剪辑引用

4. **创建预制件**
   - 将ModeTransitionEffect保存为预制件

## 第二阶段：Scene集成测试

### 2.1 场景设置

**操作步骤：**

1. **打开测试场景**
   - 打开 Assets/PongHub/Scenes/Startup.unity
   - 或创建新的测试场景

2. **添加模式选择系统**
   - 将ModeSelectionCanvas预制件拖入场景
   - 确保Canvas设置正确，能在VR环境中正常显示

3. **配置相机和VR设置**
   - 确保场景中有VR Camera Rig
   - 验证Canvas的Render Mode设置为 World Space
   - 调整Canvas位置和旋转，使其在VR中可见

4. **添加必要的管理器**
   - 确保场景中有LocalizationManager
   - 确保场景中有AudioManager
   - 如果没有，从现有场景中复制或创建新的

### 2.2 脚本配置测试

**操作步骤：**

1. **验证脚本引用**
   - 选择ModeSelectionPanel，检查所有序列化字段是否正确赋值
   - 验证SinglePlayerModePanel的引用
   - 验证MultiplayerModePanel的引用
   - 验证ModeTransitionEffect的引用

2. **测试事件绑定**
   - 在Inspector中验证Button的OnClick事件是否正确绑定
   - 检查所有UI组件的事件监听器

3. **本地化测试**
   - 验证所有文本组件的本地化键是否正确
   - 在运行时切换语言测试本地化功能

## 第三阶段：功能测试

### 3.1 基础界面测试

**测试用例TC-9.1：模式选择主界面显示**

**操作步骤：**
1. 启动测试场景
2. 观察模式选择主界面是否正确显示
3. 验证所有模式卡片是否正确加载
4. 检查标题和描述文本是否正确显示

**预期结果：**
- 主界面正常显示
- 所有模式卡片可见
- 文本内容正确且支持本地化

**测试用例TC-9.2：模式卡片交互**

**操作步骤：**
1. 使用VR控制器悬停在模式卡片上
2. 观察悬停效果（高亮、缩放、音效）
3. 点击模式卡片
4. 观察选择效果和触觉反馈

**预期结果：**
- 悬停时有视觉和音效反馈
- 点击时有动画、音效和触觉反馈
- 正确切换到对应的子界面

### 3.2 单机模式测试

**测试用例TC-9.3：单机模式界面切换**

**操作步骤：**
1. 从主界面选择单机模式相关选项
2. 验证是否正确显示单机模式面板
3. 测试练习模式、AI对战、统计界面之间的切换
4. 验证返回按钮功能

**预期结果：**
- 界面切换流畅
- 所有子界面正常显示
- 返回功能正常

**测试用例TC-9.4：AI难度选择**

**操作步骤：**
1. 进入AI对战界面
2. 使用滑块调整难度
3. 点击难度按钮
4. 验证难度显示和选择功能

**预期结果：**
- 难度选择功能正常
- 显示文本正确更新
- 选择有音效和触觉反馈

### 3.3 多人模式测试

**测试用例TC-9.5：房间创建功能**

**操作步骤：**
1. 选择多人模式
2. 点击创建房间
3. 填写房间信息
4. 测试创建房间功能

**预期结果：**
- 创建房间界面正常显示
- 输入验证功能正常
- 创建过程有适当的反馈

**测试用例TC-9.6：房间浏览功能**

**操作步骤：**
1. 进入房间浏览界面
2. 测试刷新房间列表
3. 测试房间搜索功能
4. 测试加入房间功能

**预期结果：**
- 房间列表正确显示
- 搜索功能正常
- 加入房间有正确反馈

### 3.4 动画和过渡效果测试

**测试用例TC-9.7：模式切换动画**

**操作步骤：**
1. 在不同面板间切换
2. 观察过渡动画效果
3. 测试不同的过渡类型（淡入淡出、滑动、缩放等）
4. 验证动画流畅性

**预期结果：**
- 过渡动画流畅自然
- 没有卡顿或闪烁
- 动画时间合理

**测试用例TC-9.8：音效和触觉反馈**

**操作步骤：**
1. 测试各种UI交互的音效
2. 测试触觉反馈功能
3. 调整音效和触觉设置
4. 验证不同操作的反馈差异

**预期结果：**
- 音效播放正常
- 触觉反馈工作正常
- 设置调整生效

## 第四阶段：性能测试

### 4.1 VR性能测试

**测试用例TC-9.9：帧率稳定性**

**操作步骤：**
1. 启动VR设备
2. 进入测试场景
3. 使用性能监控工具监测帧率
4. 在不同界面间快速切换
5. 记录帧率数据

**预期结果：**
- 帧率保持在90fps以上
- 切换时无明显掉帧
- 内存使用稳定

### 4.2 内存使用测试

**测试用例TC-9.10：内存优化验证**

**操作步骤：**
1. 使用Unity Profiler监控内存使用
2. 长时间运行模式切换界面
3. 检查是否有内存泄漏
4. 验证对象池的工作效果

**预期结果：**
- 内存使用在合理范围内
- 无明显内存泄漏
- GC调用频率低

## 第五阶段：兼容性测试

### 5.1 设备兼容性测试

**测试用例TC-9.11：VR设备兼容性**

**操作步骤：**
1. 在Meta Quest 2上测试
2. 在Meta Quest 3上测试（如有设备）
3. 测试不同控制器的交互
4. 验证触觉反馈的兼容性

**预期结果：**
- 在支持的VR设备上正常工作
- 控制器交互正常
- 触觉反馈适配设备特性

### 5.2 本地化兼容性测试

**测试用例TC-9.12：多语言支持**

**操作步骤：**
1. 切换到中文界面
2. 切换到英文界面
3. 验证字体显示
4. 检查布局适应性

**预期结果：**
- 多语言切换正常
- 字体显示正确
- 布局不出现错位

## 第六阶段：集成测试

### 6.1 与现有系统集成

**测试用例TC-9.13：GameModeManager集成**

**操作步骤：**
1. 测试模式选择与GameModeManager的交互
2. 验证模式切换是否正确触发游戏逻辑
3. 测试统计数据的保存和加载

**预期结果：**
- 与GameModeManager正常集成
- 模式切换触发正确的游戏逻辑
- 数据持久化正常

### 6.2 网络功能集成

**测试用例TC-9.14：网络多人功能**

**操作步骤：**
1. 测试房间创建是否调用网络管理器
2. 验证好友列表是否从网络获取数据
3. 测试网络连接状态的显示

**预期结果：**
- 网络功能调用正确
- 连接状态显示准确
- 错误处理机制正常

## 故障排除指南

### 常见问题及解决方案

**问题1：界面在VR中不可见**
- 检查Canvas的Render Mode设置
- 确认Canvas位置和旋转
- 验证Camera设置

**问题2：按钮点击无响应**
- 检查Button组件的Interactable设置
- 验证事件绑定
- 确认VR射线检测设置

**问题3：音效不播放**
- 检查AudioSource组件配置
- 验证AudioClip引用
- 确认音频混合器设置

**问题4：触觉反馈不工作**
- 确认VR设备支持触觉反馈
- 检查OVR设置
- 验证控制器连接

**问题5：本地化文本不显示**
- 检查LocalizationManager是否存在
- 验证本地化键是否正确
- 确认语言文件是否加载

## 完成标准

Story-9被认为完成当满足以下条件：

### 功能完整性
- [ ] 所有4个主要组件的预制件已创建
- [ ] 所有界面正常显示和交互
- [ ] 模式切换功能完全正常
- [ ] 音效和触觉反馈系统工作正常

### 性能要求
- [ ] VR环境下帧率稳定在90fps以上
- [ ] 界面切换响应时间小于0.5秒
- [ ] 内存使用在合理范围内

### 用户体验
- [ ] 所有交互直观易懂
- [ ] 视觉反馈丰富且合适
- [ ] 音效和触觉反馈增强体验

### 集成要求
- [ ] 与现有系统完全兼容
- [ ] 本地化功能正常
- [ ] 网络功能集成正常

## 后续工作

完成Story-9后，下一步工作包括：

1. **Story-8完成**：完成VR控制器交互优化的剩余部分
2. **Story-10规划**：开始实现设置菜单功能
3. **Epic-3准备**：为输入系统整合优化做准备

## 备注

- 所有测试应在实际VR设备上进行
- 记录测试过程中发现的问题和解决方案
- 保持文档更新，反映最新的实现状态
- 考虑用户反馈，持续优化体验

---

**文档创建者**: AI Assistant
**最后更新**: 2025年7月8日
**版本**: 1.0