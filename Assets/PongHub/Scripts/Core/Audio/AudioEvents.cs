using UnityEngine;
using System;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 音频事件基类
    /// </summary>
    public abstract class AudioEvent
    {
        public AudioCategory Category { get; set; }
        public float Volume { get; set; } = 1f;
        public float Pitch { get; set; } = 1f;
        public DateTime Timestamp { get; private set; }

        protected AudioEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 音量变化事件
    /// </summary>
    public class VolumeChangeEvent : AudioEvent
    {
        public AudioCategory TargetCategory { get; }
        public float NewVolume { get; }
        public float OldVolume { get; }

        public VolumeChangeEvent(AudioCategory category, float newVolume, float oldVolume)
        {
            TargetCategory = category;
            NewVolume = newVolume;
            OldVolume = oldVolume;
            Category = category;
        }
    }

    /// <summary>
    /// 单次音效播放事件
    /// </summary>
    public class OneShotAudioEvent : AudioEvent
    {
        public AudioClip Clip { get; }
        public Vector3? Position { get; }
        public AudioPlayParams Parameters { get; }

        public OneShotAudioEvent(AudioClip clip, AudioCategory category, AudioPlayParams parameters = null)
        {
            Clip = clip;
            Category = category;
            Parameters = parameters ?? new AudioPlayParams();
            Position = Parameters.Position;
        }

        public OneShotAudioEvent(AudioClip clip, Vector3 position, AudioCategory category, AudioPlayParams parameters = null)
        {
            Clip = clip;
            Position = position;
            Category = category;
            Parameters = parameters ?? AudioPlayParams.At(position);
        }
    }

    /// <summary>
    /// 循环音效播放事件
    /// </summary>
    public class LoopAudioEvent : AudioEvent
    {
        public AudioClip Clip { get; }
        public Transform AttachTo { get; }
        public AudioPlayParams Parameters { get; }
        public string LoopId { get; }

        public LoopAudioEvent(AudioClip clip, AudioCategory category, string loopId = null, AudioPlayParams parameters = null)
        {
            Clip = clip;
            Category = category;
            LoopId = loopId ?? Guid.NewGuid().ToString();
            Parameters = parameters ?? AudioPlayParams.Looped();
            Parameters.Loop = true;
        }

        public LoopAudioEvent(AudioClip clip, Transform attachTo, AudioCategory category, string loopId = null, AudioPlayParams parameters = null)
        {
            Clip = clip;
            AttachTo = attachTo;
            Category = category;
            LoopId = loopId ?? Guid.NewGuid().ToString();
            Parameters = parameters ?? AudioPlayParams.Follow(attachTo);
            Parameters.Loop = true;
        }
    }

    /// <summary>
    /// 音效停止事件
    /// </summary>
    public class StopAudioEvent : AudioEvent
    {
        public string AudioId { get; }
        public bool FadeOut { get; }
        public float FadeTime { get; }

        public StopAudioEvent(string audioId, bool fadeOut = false, float fadeTime = 0.5f)
        {
            AudioId = audioId;
            FadeOut = fadeOut;
            FadeTime = fadeTime;
        }
    }

    /// <summary>
    /// 分类音效停止事件
    /// </summary>
    public class StopCategoryEvent : AudioEvent
    {
        public AudioCategory TargetCategory { get; }
        public bool FadeOut { get; }
        public float FadeTime { get; }

        public StopCategoryEvent(AudioCategory category, bool fadeOut = false, float fadeTime = 0.5f)
        {
            TargetCategory = category;
            Category = category;
            FadeOut = fadeOut;
            FadeTime = fadeTime;
        }
    }

    /// <summary>
    /// 音效暂停/恢复事件
    /// </summary>
    public class PauseResumeEvent : AudioEvent
    {
        public bool IsPause { get; }
        public AudioCategory? TargetCategory { get; }
        public string AudioId { get; }

        // 暂停/恢复特定音频
        public PauseResumeEvent(string audioId, bool isPause)
        {
            AudioId = audioId;
            IsPause = isPause;
        }

        // 暂停/恢复整个分类
        public PauseResumeEvent(AudioCategory category, bool isPause)
        {
            TargetCategory = category;
            Category = category;
            IsPause = isPause;
        }
    }

    /// <summary>
    /// 音频配置变更事件
    /// </summary>
    public class AudioConfigChangeEvent : AudioEvent
    {
        public AudioConfiguration NewConfig { get; }
        public AudioConfiguration OldConfig { get; }

        public AudioConfigChangeEvent(AudioConfiguration newConfig, AudioConfiguration oldConfig)
        {
            NewConfig = newConfig;
            OldConfig = oldConfig;
        }
    }

    /// <summary>
    /// 3D音效位置更新事件
    /// </summary>
    public class SpatialAudioUpdateEvent : AudioEvent
    {
        public string AudioId { get; }
        public Vector3 NewPosition { get; }
        public Vector3? NewVelocity { get; }

        public SpatialAudioUpdateEvent(string audioId, Vector3 newPosition, Vector3? velocity = null)
        {
            AudioId = audioId;
            NewPosition = newPosition;
            NewVelocity = velocity;
        }
    }

    /// <summary>
    /// 音频系统状态事件
    /// </summary>
    public class AudioSystemStateEvent : AudioEvent
    {
        public enum SystemState
        {
            Initializing,
            Ready,
            Error,
            Cleanup
        }

        public SystemState State { get; }
        public string Message { get; }

        public AudioSystemStateEvent(SystemState state, string message = "")
        {
            State = state;
            Message = message;
        }
    }

    /// <summary>
    /// 音频事件总线
    /// 负责音频事件的分发和订阅管理
    /// </summary>
    public static class AudioEventBus
    {
        private static event Action<AudioEvent> s_onAudioEvent;

        /// <summary>
        /// 订阅音频事件
        /// </summary>
        /// <param name="handler">事件处理函数</param>
        public static void Subscribe(Action<AudioEvent> handler)
        {
            s_onAudioEvent += handler;
        }

        /// <summary>
        /// 取消订阅音频事件
        /// </summary>
        /// <param name="handler">事件处理函数</param>
        public static void Unsubscribe(Action<AudioEvent> handler)
        {
            s_onAudioEvent -= handler;
        }

        /// <summary>
        /// 发布音频事件
        /// </summary>
        /// <param name="audioEvent">要发布的事件</param>
        public static void Publish(AudioEvent audioEvent)
        {
            try
            {
                s_onAudioEvent?.Invoke(audioEvent);
            }
            catch (Exception e)
            {
                Debug.LogError($"AudioEventBus: Error publishing event {audioEvent.GetType().Name}: {e.Message}");
            }
        }

        /// <summary>
        /// 订阅特定类型的音频事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        public static void Subscribe<T>(Action<T> handler) where T : AudioEvent
        {
            s_onAudioEvent += (audioEvent) =>
            {
                if (audioEvent is T typedEvent)
                {
                    handler(typedEvent);
                }
            };
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            s_onAudioEvent = null;
        }

        /// <summary>
        /// 获取当前订阅者数量
        /// </summary>
        public static int SubscriberCount => s_onAudioEvent?.GetInvocationList().Length ?? 0;
    }

    /// <summary>
    /// 音频事件帮助类，提供便捷的事件发布方法
    /// </summary>
    public static class AudioEventHelper
    {
        /// <summary>
        /// 播放单次音效
        /// </summary>
        public static void PlayOneShot(AudioClip clip, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var parameters = AudioPlayParams.Simple(volume);
            AudioEventBus.Publish(new OneShotAudioEvent(clip, category, parameters));
        }

        /// <summary>
        /// 在指定位置播放音效
        /// </summary>
        public static void PlayAt(AudioClip clip, Vector3 position, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var parameters = AudioPlayParams.At(position, volume);
            AudioEventBus.Publish(new OneShotAudioEvent(clip, position, category, parameters));
        }

        /// <summary>
        /// 播放跟随对象的音效
        /// </summary>
        public static void PlayFollow(AudioClip clip, Transform target, AudioCategory category = AudioCategory.SFX, float volume = 1f)
        {
            var parameters = AudioPlayParams.Follow(target, volume);
            AudioEventBus.Publish(new LoopAudioEvent(clip, target, category, null, parameters));
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        public static void SetVolume(AudioCategory category, float volume)
        {
            // 这里需要先获取当前音量，为了简化，我们假设当前音量为0
            AudioEventBus.Publish(new VolumeChangeEvent(category, volume, 0f));
        }

        /// <summary>
        /// 停止分类中的所有音效
        /// </summary>
        public static void StopCategory(AudioCategory category, bool fadeOut = false, float fadeTime = 0.5f)
        {
            AudioEventBus.Publish(new StopCategoryEvent(category, fadeOut, fadeTime));
        }

        /// <summary>
        /// 暂停分类中的所有音效
        /// </summary>
        public static void PauseCategory(AudioCategory category)
        {
            AudioEventBus.Publish(new PauseResumeEvent(category, true));
        }

        /// <summary>
        /// 恢复分类中的所有音效
        /// </summary>
        public static void ResumeCategory(AudioCategory category)
        {
            AudioEventBus.Publish(new PauseResumeEvent(category, false));
        }
    }
}