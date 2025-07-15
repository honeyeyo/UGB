# 设置菜单搭建指导

## 概述

本指导文档帮助你在 Unity Editor 中设置 Story-10 的设置菜单系统。由于预制件和场景相关内容需要在 Unity Editor 中手动制作，本指导提供详细的步骤说明。

## 前提条件

- 已完成 Settings 系统脚本的创建（任务 1-4）
- Unity 2021.3 LTS 或更高版本
- TextMeshPro 包已导入
- URP 渲染管线设置

## 第一步：创建设置菜单 Canvas

### 1.1 创建主 Canvas

```
1. 在 Hierarchy 中右键 -> UI -> Canvas
2. 重命名为 "SettingsMenuCanvas"
3. 设置 Canvas Scaler：
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920x1080
   - Screen Match Mode: Match Width Or Height
   - Match: 0.5
```

### 1.2 设置 Canvas 组件

```
Canvas 组件设置：
- Render Mode: World Space (VR项目)
- World Camera: 指向 Main Camera
- Sorting Order: 100
```

## 第二步：创建设置主面板

### 2.1 创建主面板容器

```
1. 在 SettingsMenuCanvas 下创建空GameObject，命名为 "SettingsMainPanel"
2. 添加 RectTransform 组件（自动添加）
3. 设置 RectTransform：
   - Anchor Presets: Stretch (Alt+Shift+点击)
   - Left: 0, Top: 0, Right: 0, Bottom: 0
4. 添加 Image 组件作为背景
   - Color: (0, 0, 0, 200) 半透明黑色
```

### 2.2 添加主面板脚本

```
1. 选择 SettingsMainPanel GameObject
2. Add Component -> PongHub.UI.Settings.Panels.SettingsMainPanel
3. 在Inspector中配置字段（稍后连接）
```

## 第三步：创建分类导航按钮

### 3.1 创建按钮容器

```
1. 在 SettingsMainPanel 下创建空GameObject，命名为 "CategoryButtons"
2. 添加 Horizontal Layout Group 组件：
   - Spacing: 20
   - Child Alignment: Middle Center
   - Child Controls Size: 勾选 Width 和 Height
   - Child Force Expand: 勾选 Width
```

### 3.2 创建分类按钮

为每个分类创建按钮（音频、视频、控制、游戏、资料）：

```
1. 在 CategoryButtons 下右键 -> UI -> Button - TextMeshPro
2. 重命名为对应分类（如 "AudioButton"）
3. 设置 Button 组件：
   - Transition: Color Tint
   - Normal Color: (1, 1, 1, 1)
   - Highlighted Color: (0.8, 0.8, 0.8, 1)
   - Pressed Color: (0.6, 0.6, 0.6, 1)
   - Selected Color: (0, 0.5, 1, 1) 蓝色
4. 配置 TextMeshPro 文本：
   - Text: 对应分类名称（如 "音频设置"）
   - Font Size: 18
   - Alignment: Center and Middle
```

按钮列表：

- AudioButton -> "音频设置"
- VideoButton -> "视频设置"
- ControlButton -> "控制设置"
- GameplayButton -> "游戏设置"
- ProfileButton -> "用户资料"

## 第四步：创建设置内容区域

### 4.1 创建内容容器

```
1. 在 SettingsMainPanel 下创建空GameObject，命名为 "ContentArea"
2. 设置 RectTransform：
   - Anchor: Top Left
   - Anchor Min: (0, 0)
   - Anchor Max: (1, 0.85)
   - Offset Min: (20, 20)
   - Offset Max: (-20, -20)
3. 添加 Scroll Rect 组件（可选，用于滚动）
```

## 第五步：创建音频设置面板

### 5.1 创建音频面板容器

```
1. 在 ContentArea 下创建空GameObject，命名为 "AudioSettingsPanel"
2. 设置为填充父容器的 RectTransform
3. 添加 Vertical Layout Group：
   - Spacing: 15
   - Child Alignment: Upper Center
   - Child Controls Size: 勾选 Width
   - Use Child Scale: 勾选
   - Child Force Expand: 勾选 Width
4. 添加 Content Size Fitter：
   - Vertical Fit: Preferred Size
```

### 5.2 添加音频面板脚本

```
1. Add Component -> PongHub.UI.Settings.Panels.AudioSettingsPanel
2. 在 Inspector 中配置 AudioMixer 引用
```

### 5.3 创建音量设置组

创建主音量滑块示例：

```
1. 在 AudioSettingsPanel 下创建空GameObject，命名为 "MasterVolumeGroup"
2. 添加 Horizontal Layout Group
3. 创建子对象：
   a. Label (TextMeshPro): "主音量"
   b. Slider GameObject:
      - 添加 SettingSlider 脚本
      - 设置 Setting Type: MasterVolume
      - 配置 Min Value: 0, Max Value: 1
      - Whole Numbers: 关闭
   c. ValueText (TextMeshPro): 显示百分比
```

重复此过程创建其他音量滑块：

- MusicVolumeGroup -> "音乐音量"
- SfxVolumeGroup -> "音效音量"
- VoiceVolumeGroup -> "语音音量"
- AudioRangeGroup -> "音频距离"

### 5.4 创建音频开关组

创建空间音频开关示例：

```
1. 在 AudioSettingsPanel 下创建空GameObject，命名为 "SpatialAudioGroup"
2. 添加 Horizontal Layout Group
3. 创建子对象：
   a. Label (TextMeshPro): "空间音频"
   b. Toggle GameObject:
      - 添加 SettingToggle 脚本
      - 设置 Setting Type: SpatialAudio
   c. StatusText (TextMeshPro): 显示"开启/关闭"
```

### 5.5 创建音频下拉框

创建音频质量下拉框：

```
1. 创建空GameObject，命名为 "AudioQualityGroup"
2. 添加子对象：
   a. Label: "音频质量"
   b. Dropdown (TMP_Dropdown):
      - 添加 SettingDropdown 脚本
      - 设置 Setting Type: AudioQuality
      - Options: 低质量, 中等质量, 高质量, 超高质量
```

### 5.6 创建音频测试按钮

```
1. 创建按钮组 "AudioTestGroup"
2. 添加三个按钮：
   - TestAudioButton: "测试音频"
   - TestMusicButton: "测试音乐"
   - TestSfxButton: "测试音效"
3. 配置按钮点击事件指向 AudioSettingsPanel 的对应方法
```

## 第六步：创建其他设置面板

按照音频面板的模式，创建其他设置面板：

### 6.1 视频设置面板

主要组件：

- RenderQualityDropdown: 渲染质量下拉框
- AntiAliasingDropdown: 抗锯齿下拉框
- RenderScaleSlider: 渲染缩放滑块
- PostProcessingToggle: 后处理开关

### 6.2 控制设置面板

主要组件：

- LeftHandSensitivitySlider: 左手灵敏度
- RightHandSensitivitySlider: 右手灵敏度
- HapticFeedbackToggle: 触觉反馈开关
- MovementTypeDropdown: 移动方式下拉框

### 6.3 游戏设置面板

主要组件：

- DifficultyDropdown: 难度选择
- AIDifficultySlider: AI 难度滑块
- AutoAimToggle: 自动瞄准开关
- ShowUIToggle: 显示 UI 开关

### 6.4 用户资料面板

主要组件：

- PlayerNameInput: 玩家名称输入框
- LanguageDropdown: 语言选择
- AvatarImage: 头像显示
- 统计信息文本组件

## 第七步：连接组件引用

### 7.1 连接 SettingsMainPanel 引用

在 SettingsMainPanel 脚本的 Inspector 中：

```
分类按钮：
- Audio Button: 拖拽 AudioButton
- Video Button: 拖拽 VideoButton
- Control Button: 拖拽 ControlButton
- Gameplay Button: 拖拽 GameplayButton
- Profile Button: 拖拽 ProfileButton

设置面板：
- Audio Panel: 拖拽 AudioSettingsPanel
- Video Panel: 拖拽 VideoSettingsPanel
- Control Panel: 拖拽 ControlSettingsPanel
- Gameplay Panel: 拖拽 GameplaySettingsPanel
- Profile Panel: 拖拽 UserProfilePanel

操作按钮：
- Reset Button: 创建并连接重置按钮
- Import Button: 创建并连接导入按钮
- Export Button: 创建并连接导出按钮
- Close Button: 创建并连接关闭按钮
```

### 7.2 连接 AudioSettingsPanel 引用

```
AudioMixer组件：
- Master Mixer Group: 拖拽主音频混合器组
- Music Mixer Group: 拖拽音乐混合器组
- SFX Mixer Group: 拖拽音效混合器组
- Voice Mixer Group: 拖拽语音混合器组

设置组件：
- Master Volume Slider: 拖拽对应SettingSlider
- Music Volume Slider: 拖拽对应SettingSlider
- ... (其他组件)

测试组件：
- Test Audio Button: 拖拽测试按钮
- Test Audio Source: 拖拽AudioSource组件
- Test Audio Clips: 拖拽测试音频文件
```

## 第八步：配置 AudioMixer

### 8.1 创建 AudioMixer 资源

```
1. 在 Project 窗口右键 -> Create -> Audio Mixer
2. 命名为 "SettingsAudioMixer"
3. 创建混合器组：
   - Master
     - Music
     - SFX
     - Voice
```

### 8.2 设置 Exposed Parameters

```
在 AudioMixer 窗口：
1. 右键每个组的 Volume -> Expose to script
2. 在 Exposed Parameters 中重命名：
   - MasterVolume
   - MusicVolume
   - SFXVolume
   - VoiceVolume
```

## 第九步：创建设置菜单 Prefab

### 9.1 创建 Prefab

```
1. 将完整的 SettingsMenuCanvas 拖拽到 Project 窗口
2. 保存为 "SettingsMenuCanvas.prefab"
3. 放置在 Assets/PongHub/Prefabs/UI/ 目录
```

### 9.2 在场景中使用

```
1. 将 SettingsMenuCanvas Prefab 拖拽到主菜单场景
2. 确保Canvas的World Camera引用正确
3. 初始状态设置为隐藏 (SetActive(false))
```

## 第十步：VR 配置优化

### 10.1 VR Canvas 设置

```
Canvas 组件 VR 优化：
- Event Camera: 设置为VR相机
- Planer Distance: 2.0 (适合VR观看距离)
- 添加 VR Raycaster 组件用于VR交互
```

### 10.2 VR UI 缩放

```
Canvas Scaler VR 设置：
- UI Scale Mode: Constant Physical Size
- Physical Unit: Centimeters
- Fallback Screen DPI: 96
- Default Sprite DPI: 96
```

## 测试清单

### 基础功能测试

- [ ] 设置菜单可以正常打开/关闭
- [ ] 分类按钮切换正常工作
- [ ] 各个设置组件能正确显示当前值
- [ ] 设置更改能实时生效
- [ ] 设置数据能正确保存和加载

### 音频系统测试

- [ ] 音量滑块实时调整 AudioMixer 音量
- [ ] 测试音频按钮播放对应音效
- [ ] 空间音频开关生效
- [ ] 音频质量设置生效
- [ ] 失去焦点静音功能正常

### VR 交互测试

- [ ] VR 手柄可以正常操作 UI
- [ ] 触觉反馈工作正常
- [ ] 设置菜单在 VR 中显示清晰
- [ ] 按钮点击有视觉和触觉反馈

### 数据持久化测试

- [ ] 重启游戏后设置保持不变
- [ ] 重置功能正常工作
- [ ] 导入/导出功能正常
- [ ] 设置验证和修复功能正常

## 故障排除

### 常见问题

1. **设置组件不显示当前值**

   - 检查 SettingsManager 是否正确初始化
   - 确认 RefreshPanel()在 Start()中被调用

2. **AudioMixer 不响应音量变化**

   - 检查 Exposed Parameters 名称是否正确
   - 确认 AudioMixerGroup 引用是否连接

3. **VR 交互不工作**

   - 检查 Canvas Event Camera 设置
   - 确认 VR Raycaster 组件已添加

4. **设置不保存**
   - 检查文件写入权限
   - 确认 persistentDataPath 可访问

### 调试工具

使用 Unity Console 查看设置系统日志：

```csharp
// 在SettingsManager中启用调试日志
[SerializeField] private bool enableDebugLog = true;
```

## 下一步

完成基础设置菜单搭建后，可以：

1. 添加更多自定义设置选项
2. 实现设置预设系统
3. 添加设置搜索功能
4. 集成多语言支持
5. 添加设置导入/导出界面

---

_本指导文档是 Story-10 设置菜单功能的一部分，确保按照步骤操作以获得最佳效果。_
