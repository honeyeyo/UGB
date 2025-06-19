# PongHub Audio System Usage Guide

Comprehensive guide for using the PongHub Audio System.

## Quick Start

Basic audio playback examples and common usage patterns.

## Game-Specific Audio

Ping pong game audio effects and match state management.

## Advanced Audio Control

Complex audio operations, sequences, and effects.

## Volume Control

Audio category management and settings integration.

## Event-Driven Audio

Audio event bus usage and custom events.

## AudioHandle Control

Precise audio manipulation and control.

## Configuration

System setup and initialization.

## Performance Optimization

Best practices for audio performance.

## Debugging

Tools and troubleshooting guide.

## Best Practices

Recommended usage patterns and common pitfalls.

## 🚀 快速开始

### 基础音效播放

```csharp
using PongHub.Core.Audio;

public class ExampleAudioUsage : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip backgroundMusic;

    private void Start()
    {
        // 播放一次性音效
        AudioService.Instance.PlayOneShot(buttonClickSound, AudioCategory.UI);

        // 播放背景音乐（循环）
        AudioService.Instance.PlayLooped(backgroundMusic, AudioCategory.Music, 0.8f);
    }
}
```

### 3D 空间音效

```csharp
public class SpatialAudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip explosionSound;

    private void OnCollisionEnter(Collision collision)
    {
        // 在碰撞点播放3D音效
        Vector3 hitPosition = collision.contacts[0].point;
        AudioService.Instance.PlayOneShot(explosionSound, hitPosition, AudioCategory.SFX);
    }
}
```

## 🎮 游戏特定音效

### 乒乓球游戏音效

```csharp
public class PaddleController : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Ball>(out var ball))
        {
            Vector3 contactPoint = collision.contacts[0].point;

            // 使用AudioManager播放专用音效
            AudioManager.Instance.PlayPaddleHit(contactPoint, 1.0f);
        }
    }
}

public class TableController : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Ball>(out var ball))
        {
            Vector3 contactPoint = collision.contacts[0].point;
            AudioManager.Instance.PlayTableHit(contactPoint, 0.8f);
        }
    }
}
```

### 比赛状态音效

```csharp
public class GameManager : MonoBehaviour
{
    private void OnGameStart()
    {
        AudioManager.Instance.PlayMatchStart();
        AudioManager.Instance.StartCrowdAmbient();
    }

    private void OnPlayerScore(bool isPlayerPoint)
    {
        AudioManager.Instance.PlayPointScored(isPlayerPoint);

        if (isPlayerPoint)
        {
            AudioManager.Instance.TriggerCrowdReaction(CrowdReactionType.Cheer, 1.0f);
        }
    }

    private void OnMatchEnd(bool playerWon)
    {
        AudioManager.Instance.PlayMatchEnd();
        AudioManager.Instance.StopCrowdAmbient();
    }
}
```

## 🎛️ 高级音频控制

### 使用AudioController的高级功能

```csharp
public class AdvancedAudioExample : MonoBehaviour
{
    [SerializeField] private AudioController audioController;
    [SerializeField] private AudioClip[] sequenceClips;

    private void Start()
    {
        // 播放音频序列
        PlayIntroSequence();

        // 设置音频闪避
        audioController.DuckCategory(AudioCategory.Music, 0.3f, 2.0f);
    }

    private void PlayIntroSequence()
    {
        var sequence = new AudioSequenceItem[]
        {
            new AudioSequenceItem(sequenceClips[0], AudioCategory.UI, 1.0f, 1.0f),
            AudioSequenceItem.Delay(0.5f),
            new AudioSequenceItem(sequenceClips[1], AudioCategory.UI, 1.0f, 2.0f),
            new AudioSequenceItem(sequenceClips[2], AudioCategory.UI, 1.0f, 0f)
        };

        audioController.PlayAudioSequence(sequence);
    }
}
```

### 淡入淡出效果

```csharp
public class FadeAudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip ambientSound;

    private AudioHandle ambientHandle;

    private void StartAmbient()
    {
        // 播放带淡入效果的环境音
        ambientHandle = AudioService.Instance.PlayWithFade(
            ambientSound,
            fadeInTime: 3.0f,
            fadeOutTime: 2.0f,
            AudioCategory.Ambient
        );
    }

    private void StopAmbient()
    {
        if (ambientHandle != null && ambientHandle.IsValid)
        {
            // 使用AudioController进行自定义淡出
            audioController.StartFadeOut(ambientHandle, 2.0f, AudioFadeType.EaseOut);
        }
    }
}
```

## 🎚️ 音量控制

### 分类音量控制

```csharp
public class VolumeControlExample : MonoBehaviour
{
    public void SetMasterVolume(float volume)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.Master, volume);
    }

    public void SetMusicVolume(float volume)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.Music, volume);
    }

    public void SetSFXVolume(float volume)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.SFX, volume);
    }

    // 获取当前音量
    public float GetCurrentMusicVolume()
    {
        return AudioService.Instance.GetCategoryVolume(AudioCategory.Music);
    }
}
```

### 设置面板集成

```csharp
public class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        // 初始化滑块值
        masterVolumeSlider.value = AudioService.Instance.GetCategoryVolume(AudioCategory.Master);
        musicVolumeSlider.value = AudioService.Instance.GetCategoryVolume(AudioCategory.Music);
        sfxVolumeSlider.value = AudioService.Instance.GetCategoryVolume(AudioCategory.SFX);

        // 绑定事件
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.Master, value);
        AudioController.Instance.PlayButtonClick(0.5f); // 播放反馈音效
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.Music, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioService.Instance.SetCategoryVolume(AudioCategory.SFX, value);
    }
}
```

## 🎪 事件驱动音频

### 使用AudioEventBus

```csharp
public class EventDrivenAudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip powerUpSound;

    private void Start()
    {
        // 订阅音频事件
        AudioEventBus.Subscribe(OnAudioEvent);
    }

    private void OnDestroy()
    {
        // 取消订阅
        AudioEventBus.Unsubscribe(OnAudioEvent);
    }

    private void OnAudioEvent(AudioEvent audioEvent)
    {
        if (audioEvent is AudioSystemStateEvent stateEvent)
        {
            Debug.Log($"Audio System State: {stateEvent.State}");
        }
    }

    public void TriggerPowerUp()
    {
        // 发布音频事件
        var playParams = AudioPlayParams.At(transform.position, 1.0f);
        var audioEvent = new OneShotAudioEvent(powerUpSound, AudioCategory.SFX, playParams);
        AudioEventBus.Publish(audioEvent);
    }
}
```

### 自定义音频事件

```csharp
public class CustomAudioEvent : AudioEvent
{
    public string EventType { get; }
    public float Intensity { get; }

    public CustomAudioEvent(string eventType, float intensity)
    {
        EventType = eventType;
        Intensity = intensity;
    }
}

public class CustomAudioHandler : MonoBehaviour
{
    private void Start()
    {
        AudioEventBus.Subscribe(HandleCustomEvent);
    }

    private void HandleCustomEvent(AudioEvent audioEvent)
    {
        if (audioEvent is CustomAudioEvent customEvent)
        {
            Debug.Log($"Custom Audio Event: {customEvent.EventType}, Intensity: {customEvent.Intensity}");
        }
    }
}
```

## 🎵 AudioHandle 控制

### 精确音频控制

```csharp
public class AudioHandleExample : MonoBehaviour
{
    private AudioHandle musicHandle;
    private AudioHandle engineHandle;

    public void StartEngine()
    {
        engineHandle = AudioService.Instance.PlayLooped(engineSound, AudioCategory.SFX);
    }

    public void UpdateEngineSpeed(float speed)
    {
        if (engineHandle != null && engineHandle.IsValid)
        {
            // 根据速度调整音调
            float pitch = Mathf.Lerp(0.8f, 2.0f, speed);
            engineHandle.SetPitch(pitch);

            // 根据速度调整音量
            float volume = Mathf.Lerp(0.3f, 1.0f, speed);
            engineHandle.SetVolume(volume);
        }
    }

    public void StopEngine()
    {
        engineHandle?.Stop();
    }

    // 暂停/恢复音频
    public void PauseMusic()
    {
        musicHandle?.Pause();
    }

    public void ResumeMusic()
    {
        musicHandle?.Resume();
    }
}
```

## 🔧 配置与初始化

### AudioConfiguration 设置

```csharp
[CreateAssetMenu(fileName = "MyAudioConfig", menuName = "Audio/Configuration")]
public class MyAudioConfiguration : ScriptableObject
{
    private void OnValidate()
    {
        // 创建自定义音频配置
        var config = CreateInstance<AudioConfiguration>();

        // 配置音量范围
        config.MasterVolumeRange = new FloatRange(0f, 1f);
        config.MusicVolumeRange = new FloatRange(0f, 1f);

        // 配置默认音量
        config.DefaultMasterVolume = 1.0f;
        config.DefaultMusicVolume = 0.8f;
        config.DefaultSfxVolume = 1.0f;

        // 配置3D音效参数
        config.DefaultMinDistance = 1f;
        config.DefaultMaxDistance = 50f;
        config.DefaultRolloffMode = AudioRolloffMode.Logarithmic;

        // 配置性能参数
        config.AudioSourcePoolSize = 20;
        config.MaxConcurrentAudio = 32;
        config.AudioCullingDistance = 100f;

        // 配置VR特定设置
        config.EnableSpatialAudio = true;
        config.SpatialAudioPrecision = 3;
        config.HeadTrackingStrength = 1f;
    }
}
```

### 场景初始化

```csharp
public class AudioSceneSetup : MonoBehaviour
{
    [SerializeField] private AudioConfiguration audioConfig;

    private async void Start()
    {
        // 等待音频系统初始化
        while (!AudioService.Instance.IsInitialized)
        {
            await Task.Delay(100);
        }

        Debug.Log("Audio System Ready!");

        // 设置初始音量
        LoadAudioSettings();

        // 开始播放环境音
        StartAmbientAudio();
    }

    private void LoadAudioSettings()
    {
        // 从PlayerPrefs加载保存的音量设置
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        AudioService.Instance.SetCategoryVolume(AudioCategory.Master, masterVolume);
        AudioService.Instance.SetCategoryVolume(AudioCategory.Music, musicVolume);
        AudioService.Instance.SetCategoryVolume(AudioCategory.SFX, sfxVolume);
    }

    private void StartAmbientAudio()
    {
        AudioManager.Instance.StartLobbyAmbient();
    }
}
```

## 🚀 性能优化技巧

### 对象池使用

```csharp
public class OptimizedAudioPlayer : MonoBehaviour
{
    private const int MAX_CONCURRENT_SOUNDS = 5;
    private Queue<AudioHandle> activeSounds = new();

    public void PlayLimitedSound(AudioClip clip)
    {
        // 限制同时播放的音效数量
        if (activeSounds.Count >= MAX_CONCURRENT_SOUNDS)
        {
            var oldestSound = activeSounds.Dequeue();
            oldestSound?.Stop();
        }

        var newSound = AudioService.Instance.PlayOneShot(clip, AudioCategory.SFX);
        activeSounds.Enqueue(newSound);

        // 自动清理完成的音效
        StartCoroutine(CleanupSound(newSound));
    }

    private IEnumerator CleanupSound(AudioHandle handle)
    {
        while (handle.IsValid && handle.IsPlaying)
        {
            yield return null;
        }

        activeSounds = new Queue<AudioHandle>(activeSounds.Where(h => h != handle));
    }
}
```

### 距离优化

```csharp
public class DistanceOptimizedAudio : MonoBehaviour
{
    [SerializeField] private float maxAudioDistance = 50f;
    private Transform playerTransform;

    private void Start()
    {
        playerTransform = Camera.main.transform;
    }

    public void PlaySoundWithDistanceCheck(AudioClip clip, Vector3 position)
    {
        float distance = Vector3.Distance(playerTransform.position, position);

        if (distance <= maxAudioDistance)
        {
            // 根据距离调整音量
            float volumeMultiplier = 1f - (distance / maxAudioDistance);
            AudioService.Instance.PlayOneShot(clip, position, AudioCategory.SFX, volumeMultiplier);
        }
    }
}
```

## 🐛 调试与故障排除

### 调试工具

```csharp
public class AudioDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowAudioStats();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            ShowDetailedDebugInfo();
        }
    }

    private void ShowAudioStats()
    {
        AudioService.Instance.GetAudioStats(out int totalActive, out var byCategory);

        Debug.Log($"总活动音频: {totalActive}");
        foreach (var kvp in byCategory)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }

    private void ShowDetailedDebugInfo()
    {
        string debugInfo = AudioService.Instance.GetDetailedDebugInfo();
        Debug.Log(debugInfo);
    }
}
```

### 常见问题解决

```csharp
public class AudioTroubleshooter : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(DiagnoseAudioSystem());
    }

    private IEnumerator DiagnoseAudioSystem()
    {
        // 等待系统初始化
        yield return new WaitForSeconds(1f);

        // 检查AudioService
        if (AudioService.Instance == null)
        {
            Debug.LogError("AudioService.Instance is null!");
            yield break;
        }

        if (!AudioService.Instance.IsInitialized)
        {
            Debug.LogError("AudioService is not initialized!");
            yield break;
        }

        // 检查配置
        if (AudioService.Instance.Configuration == null)
        {
            Debug.LogError("AudioConfiguration is missing!");
            yield break;
        }

        if (!AudioService.Instance.Configuration.ValidateConfiguration())
        {
            Debug.LogError("AudioConfiguration validation failed!");
            yield break;
        }

        Debug.Log("✅ Audio System is working correctly!");
    }
}
```

## 📝 最佳实践总结

### 1. 音频分类最佳实践

```csharp
// ✅ 正确使用
AudioService.Instance.PlayOneShot(buttonSound, AudioCategory.UI);
AudioService.Instance.PlayOneShot(footstepSound, AudioCategory.SFX);
AudioService.Instance.PlayLooped(backgroundMusic, AudioCategory.Music);

// ❌ 避免混用分类
AudioService.Instance.PlayOneShot(backgroundMusic, AudioCategory.SFX); // 错误
```

### 2. 性能优化最佳实践

```csharp
// ✅ 使用对象池和限制
private const int MAX_BALL_SOUNDS = 3;
private float lastSoundTime;
private const float SOUND_COOLDOWN = 0.1f;

public void PlayBallSound()
{
    if (Time.time - lastSoundTime < SOUND_COOLDOWN) return;

    AudioManager.Instance.PlayBallHitPaddle(transform.position);
    lastSoundTime = Time.time;
}

// ❌ 避免频繁播放
public void PlayBallSound()
{
    AudioManager.Instance.PlayBallHitPaddle(transform.position); // 可能导致性能问题
}
```

### 3. 空间音频最佳实践

```csharp
// ✅ 合理设置3D音频参数
var playParams = AudioPlayParams.At(hitPosition);
playParams.MinDistance = 1f;
playParams.MaxDistance = 20f;
playParams.SpatialBlend = 1f; // 完全3D
AudioService.Instance.PlayOneShot(clip, AudioCategory.SFX, playParams);

// ✅ UI音效使用2D
var uiParams = AudioPlayParams.Simple(0.8f);
uiParams.SpatialBlend = 0f; // 2D音效
AudioService.Instance.PlayOneShot(buttonSound, AudioCategory.UI, uiParams);
```
