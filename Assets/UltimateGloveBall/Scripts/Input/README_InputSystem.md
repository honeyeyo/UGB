# Pong VR游戏输入系统使用说明

## 概述

本输入系统基于Meta Utilities Input包构建，专为Quest控制器设计，提供了完整的Pong VR游戏输入解决方案。

## 主要组件

### 1. PongInputManager
核心输入管理器，处理所有游戏输入逻辑。

**功能包括：**
- 玩家移动和视角控制
- 球拍握持和释放
- 球生成
- 瞬移功能
- 配置模式触发

### 2. PaddleConfigurationManager
球拍配置管理器，提供球拍位置和旋转的精确调整。

**功能包括：**
- 实时预览球拍位置
- 保存和加载配置
- 分别配置左右手球拍
- 重置为默认设置

### 3. UI系统（Scripts/UI/目录）
整合到现有UI架构中的组件：

**UIManager** - 统一UI管理器：
- 游戏状态管理
- 面板切换控制
- 输入系统集成

**InputSettingsPanel** - 输入设置面板：
- 球拍配置按钮
- 瞬移快捷按钮
- 控制参数调整

**GameplayHUD** - 游戏内显示界面：
- 实时状态显示
- 操作提示
- 临时消息显示

### 4. CustomPointableCanvasModule
自定义可指向画布模块（现有组件）。

**功能包括：**
- VR环境中的UI交互
- 射线指向检测
- 按钮点击交互

## 输入映射

### 移动控制
- **左手摇杆**: 前后左右移动
- **右手摇杆左右**: 视角旋转
- **右手摇杆前推**: 瞬移到前方
- **左手A键**: 向上移动
- **左手B键**: 向下移动

### 球拍控制
- **长按Grip键**: 握持/释放球拍
- **同时按A+B键**: 进入配置模式

### 球生成
- **非持拍手Trigger键**: 在掌心生成球

### 瞬移功能
- **长按Meta键**: 回到默认出生点
- **UI按钮**: 瞬移到球桌两侧

## 配置说明

### 球拍位置配置
1. 在未握持球拍时，同时按住对应手的A+B键
2. 或通过设置菜单中的配置按钮进入
3. 使用UI滑条调整位置和旋转
4. 预览球拍会实时显示调整效果
5. 按Save保存配置，按Reset重置为默认

### 配置参数
- **Position X/Y/Z**: 相对于手部锚点的位置偏移
- **Rotation X/Y/Z**: 相对于手部锚点的旋转偏移

### 瞬移点设置
在Inspector中设置teleportPoints数组，每个Transform代表一个瞬移目标点。

## 使用步骤

### 1. 场景设置
1. 在场景中添加XRInputManager组件
2. 确保OVRCameraRig已正确配置
3. 添加PongInputManager、PaddleConfigurationManager和PongUIManager

### 2. 组件配置
**PongInputManager**:
- 设置移动速度、旋转速度等参数
- 指定球拍和球的Prefab
- 设置瞬移点和默认出生点
- 连接XRInputManager和其他组件引用

**PaddleConfigurationManager**:
- 设置配置UI的Canvas和控件
- 指定预览球拍Prefab和材质
- 设置默认位置和旋转值

**UIManager（Scripts/UI/）**:
- 设置各个UI面板引用
- 连接输入管理器
- 配置游戏状态切换逻辑

**InputSettingsPanel**:
- 设置球拍配置按钮
- 配置瞬移快捷按钮
- 连接相关UI控件

**GameplayHUD**:
- 设置状态显示文本
- 配置操作提示界面
- 连接临时消息显示组件

### 3. 预制件设置
- **球拍Prefab**: 应包含Rigidbody和Collider
- **球Prefab**: 应包含Rigidbody、Collider和物理材质

## 输入动作配置

### XRInputControlActions资产
包含所有输入动作的配置：
- ButtonOne/Two/Three: A/B/Menu按钮
- TouchOne/Two: A/B按钮触摸
- AxisIndexTrigger: 食指扳机
- AxisHandTrigger: 握持扳机
- ButtonPrimaryThumbstick: 摇杆按下
- TouchPrimaryThumbstick: 摇杆触摸

## 文件结构

```
Assets/UltimateGloveBall/Scripts/
├── Input/
│   ├── PongInputManager.cs              # 核心输入管理器
│   ├── PaddleConfigurationManager.cs    # 球拍配置管理器
│   ├── CustomPointableCanvasModule.cs   # 自定义可指向画布模块
│   └── README_InputSystem.md            # 本说明文档
└── UI/
    ├── UIManager.cs                      # 统一UI管理器
    ├── InputSettingsPanel.cs            # 输入设置面板
    ├── HUD/
    │   └── GameplayHUD.cs               # 游戏内HUD显示
    ├── MainMenu/
    │   └── MainMenuPanel.cs             # 主菜单面板（已更新）
    └── SettingsPanel.cs                 # 设置面板（已更新）
```

## 调试功能

### 实时状态显示
游戏中会显示当前状态：
- 球拍握持状态
- 握持手信息
- 操作提示

### 控制台输出
系统会输出关键操作的日志：
- 球拍握持/释放
- 球生成
- 瞬移操作
- 配置保存

## 性能优化建议

1. **输入检测频率**: 避免在Update中进行过多的输入检测
2. **UI更新**: 只在必要时更新UI文本
3. **预制件管理**: 合理管理球和球拍的生成与销毁
4. **配置缓存**: 避免频繁的配置文件读写

## 扩展功能

### 添加新的输入动作
1. 在XRInputControlActions中添加新的InputActionProperty
2. 在XRInputControlDelegate中添加对应的处理逻辑
3. 在PongInputManager中实现具体功能

### 自定义握持逻辑
可以在GrabPaddle和ReleasePaddle方法中添加：
- 触觉反馈
- 音效播放
- 动画效果
- 特殊效果

### 高级配置选项
可以扩展配置系统支持：
- 多套配置方案
- 配置导入导出
- 预设配置模板
- 云端配置同步

## 故障排除

### 常见问题
1. **输入无响应**: 检查XRInputManager是否正确初始化
2. **球拍位置错误**: 确认配置是否正确保存和加载
3. **UI无法操作**: 检查Canvas和EventSystem设置
4. **瞬移不生效**: 确认瞬移点Transform是否正确设置

### 调试步骤
1. 检查控制台输出
2. 验证组件引用是否正确
3. 确认输入动作配置
4. 测试各个功能模块

## 版本历史

- **v1.0** - 初始版本，包含基础输入功能
- **v1.1** - 添加球拍配置系统
- **v1.2** - 整合UI管理器和完整的游戏状态管理
- **v1.3** - 整合到现有UI架构，分离UI组件到Scripts/UI目录

## 许可和版权

本输入系统基于Meta Utilities Input包构建，遵循相应的许可协议。