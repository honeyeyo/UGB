# PongHub 音频系统技术规范

## 📋 概述

PongHub 音频系统是一个模块化、分层设计的音频解决方案，专为 VR 乒乓球游戏设计。系统提供完整的音频管理功能，包括 3D 空间音频、音量控制、音频分类管理和事件驱动的音频播放。

## 🏗️ 系统架构

### 核心组件层次结构

```text
📁 Core/Audio/
├── 🎯 AudioService (核心服务)
├── 🎛️ AudioConfiguration (配置管理)
├── 🎵 AudioManager (游戏音频管理)
├── 🎚️ AudioController (高级音频控制)
├── 🔄 AudioEventBus (事件总线)
├── 🎪 AudioSourcePool (音源池)
├── 🎛️ AudioMixerController (混音器控制)
├── 🌍 SpatialAudioManager (空间音频)
└── 📋 Types & Events (类型定义)
```

## 🧩 核心组件详解

### 1. AudioService (单例核心服务)

**职责**: 音频系统的核心服务，管理所有音频播放和控制
**继承**: `Singleton<AudioService>`

**主要功能**:

- 音频播放管理（一次性、循环、淡入淡出）
- 音量分类控制（Master, Music, SFX, Voice, Ambient, Crowd, UI）
- 音频句柄管理
- 空间音频支持
- 系统生命周期管理

**关键API**:

```csharp
// 播放音效
AudioHandle PlayOneShot(AudioClip clip, AudioCategory category = AudioCategory.SFX, float volume = 1f)
AudioHandle PlayOneShot(AudioClip clip, Vector3 position, AudioCategory category = AudioCategory.SFX, float volume = 1f)

// 循环播放
AudioHandle PlayLooped(AudioClip clip, AudioCategory category = AudioCategory.Music, float volume = 1f)

// 音量控制
void SetCategoryVolume(AudioCategory category, float volume)
float GetCategoryVolume(AudioCategory category)

// 停止控制
void StopAllInCategory(AudioCategory category)
void StopAllAudio()
```

### 2. AudioManager (游戏专用管理器)

**职责**: 游戏特定的音频功能，专注于乒乓球相关音效
**继承**: `MonoBehaviour` (单例模式)

**专用功能**:

- 球拍击球音效 (`PlayPaddleHit`)
- 球桌碰撞音效 (`PlayTableHit`)
- 球网碰撞音效 (`PlayNetHit`)
- 比赛音效 (开始、结束、得分)
- 观众反应音效
- 环境音效管理

### 3. AudioController (高级控制器)

**职责**: 提供高级音频控制功能
**特色功能**:

- 音频序列播放
- 自定义淡入淡出
- 音频闪避 (Ducking)
- 应用程序状态音频管理
- 快捷音效管理

### 4. 配置与管理组件

#### AudioConfiguration (ScriptableObject)

- 音频混音器配置
- 音量范围设置
- 3D音效参数
- VR特定设置
- 性能优化参数

#### AudioSourcePool

- 音频源对象池管理
- 性能优化
- 自动回收机制

#### SpatialAudioManager

- VR空间音频处理
- 头部跟踪
- 3D定位音效

## 🎯 音频分类系统

系统支持以下音频分类，每个分类都有独立的音量控制：

| 分类 | 用途 | 默认音量 |
|------|------|----------|
| **Master** | 主音量控制 | 1.0 |
| **Music** | 背景音乐 | 0.8 |
| **SFX** | 音效 | 1.0 |
| **Voice** | 语音聊天 | 1.0 |
| **Ambient** | 环境音 | 0.6 |
| **Crowd** | 观众音效 | 0.8 |
| **UI** | 用户界面音效 | 0.9 |

## 🎪 事件驱动架构

### AudioEventBus

音频系统使用事件总线模式，支持以下事件类型：

```csharp
// 一次性音频事件
OneShotAudioEvent(AudioClip, AudioCategory, AudioPlayParams)

// 循环音频事件
LoopAudioEvent(AudioClip, AudioCategory, AudioPlayParams)

// 音量更改事件
VolumeChangeEvent(AudioCategory, float volume)

// 停止音频事件
StopAudioEvent(string handleId)
StopCategoryEvent(AudioCategory)

// 暂停/恢复事件
PauseResumeEvent(bool isPaused)

// 空间音频更新事件
SpatialAudioUpdateEvent(Vector3 position, Transform target)

// 系统状态事件
AudioSystemStateEvent(SystemState state)
```

## 🎵 音频句柄系统

### AudioHandle

每个播放的音频都会返回一个 `AudioHandle`，提供精确控制：

```csharp
public class AudioHandle
{
    public AudioSource Source { get; }
    public AudioCategory Category { get; }
    public bool IsValid { get; }
    public bool IsPlaying { get; }
    public float Volume { get; }

    // 控制方法
    public void Stop()
    public void Pause()
    public void Resume()
    public void SetVolume(float volume)
    public void SetPitch(float pitch)
}
```

## 🔧 配置系统

### 音频质量级别

```csharp
public enum AudioQualityLevel
{
    Low = 0,     // 22050 Hz
    Medium = 1,  // 44100 Hz
    High = 2     // 48000 Hz
}
```

### 音频播放参数

```csharp
public class AudioPlayParams
{
    // 基础参数
    public float Volume = 1f;
    public float Pitch = 1f;
    public bool Loop = false;

    // 3D音效参数
    public Vector3? Position = null;
    public Transform AttachTo = null;
    public float SpatialBlend = 1f;
    public float MinDistance = 1f;
    public float MaxDistance = 50f;

    // 淡入淡出
    public float FadeInTime = 0f;
    public float FadeOutTime = 0f;
    public AudioFadeType FadeType = AudioFadeType.Linear;

    // 优先级
    public AudioPriority Priority = AudioPriority.Normal;
}
```

## 🚀 性能优化特性

### 1. 对象池管理

- AudioSourcePool 管理音频源对象池
- 减少内存分配和垃圾回收
- 可配置池大小和最大并发音频数量

### 2. 音频裁剪

- 基于距离的音频裁剪
- 配置最大播放距离
- 自动停止超出范围的音效

### 3. LOD系统

- 可选的音频细节级别系统
- 根据距离调整音频质量
- 性能优化配置

## 🔄 生命周期管理

### 初始化流程

1. AudioService 初始化 (Singleton Awake)
2. 验证 AudioConfiguration
3. 初始化子组件 (Pool, Mixer, Spatial)
4. 加载音量设置
5. 应用音频质量设置
6. 发布系统就绪事件

### 销毁流程

1. 停止所有音频
2. 取消事件订阅
3. 清理协程
4. 发布清理事件

## 🎯 VR 特定功能

### 空间音频

- 完整的 3D 音频支持
- 头部跟踪集成
- 动态音频衰减
- VR 控制器音效支持

### 性能优化

- VR 帧率优化的音频处理
- 低延迟音频管道
- 空间音频精度配置

## 📊 调试与监控

### 调试功能

- 详细的调试日志系统
- 音频统计信息
- 实时性能监控
- 配置验证

### 监控API

```csharp
// 获取音频统计
GetAudioStats(out int totalActive, out Dictionary<AudioCategory, int> byCategory)

// 详细调试信息
string GetDetailedDebugInfo()

// 系统状态
bool IsInitialized { get; }
int ActiveAudioCount { get; }
```

## 🔌 扩展性

### 自定义音频事件

系统支持扩展自定义音频事件类型，只需继承 `AudioEvent` 基类。

### 音频效果器

可以轻松集成自定义音频效果器和滤镜。

### 混音器扩展

支持运行时动态创建和配置混音器组。

## 📝 最佳实践

### 1. 音频分类使用

- **Music**: 仅用于背景音乐
- **SFX**: 游戏音效的主要分类
- **UI**: 界面交互音效
- **Voice**: 语音聊天和语音提示
- **Ambient**: 环境音和氛围音效
- **Crowd**: 观众反应和人群音效

### 2. 性能优化

- 使用对象池避免频繁创建销毁
- 合理设置音频裁剪距离
- 避免同时播放过多相同音效
- 使用适当的音频压缩格式

### 3. 空间音频

- 为重要的交互音效使用 3D 定位
- 合理设置最小/最大距离
- 考虑 VR 环境的音频传播特性

## 🔧 故障排除

### 常见问题

1. **AudioService 未初始化**: 确保场景中有 AudioService 实例
2. **音效不播放**: 检查 AudioConfiguration 配置
3. **音量控制无效**: 验证混音器组分配
4. **空间音频问题**: 检查 SpatialAudioManager 设置

### 调试步骤

1. 检查 AudioService.IsInitialized 状态
2. 验证 AudioConfiguration.ValidateConfiguration() 返回值
3. 查看调试日志输出
4. 使用 GetDetailedDebugInfo() 获取详细信息
