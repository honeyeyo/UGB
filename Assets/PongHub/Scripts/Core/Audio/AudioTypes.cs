using UnityEngine;
using System;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// 音频分类枚举
    /// </summary>
    public enum AudioCategory
    {
        /// <summary>主音量</summary>
        Master = 0,
        /// <summary>背景音乐</summary>
        Music = 1,
        /// <summary>音效</summary>
        SFX = 2,
        /// <summary>语音</summary>
        Voice = 3,
        /// <summary>环境音</summary>
        Ambient = 4,
        /// <summary>人群音效</summary>
        Crowd = 5,
        /// <summary>UI音效</summary>
        UI = 6
    }

    /// <summary>
    /// 音频播放优先级
    /// </summary>
    public enum AudioPriority
    {
        Low = 64,
        Normal = 128,
        High = 192,
        Critical = 256
    }

    /// <summary>
    /// 音频淡入淡出类型
    /// </summary>
    public enum AudioFadeType
    {
        None,
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut
    }

    /// <summary>
    /// 音频质量级别
    /// </summary>
    public enum AudioQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// 浮点数范围结构
    /// </summary>
    [Serializable]
    public struct FloatRange
    {
        [SerializeField] private float m_min;
        [SerializeField] private float m_max;

        public float Min => m_min;
        public float Max => m_max;

        public FloatRange(float min, float max)
        {
            m_min = min;
            m_max = max;
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, m_min, m_max);
        }

        public float GetRandomValue()
        {
            return UnityEngine.Random.Range(m_min, m_max);
        }
    }

    /// <summary>
    /// 音频播放句柄，用于控制正在播放的音频
    /// </summary>
    public class AudioHandle
    {
        public AudioSource Source { get; private set; }
        public AudioCategory Category { get; private set; }
        public bool IsValid => Source != null && Source.gameObject != null;
        public bool IsPlaying => IsValid && Source.isPlaying;

        /// <summary>
        /// 获取当前音量
        /// </summary>
        public float Volume => IsValid ? Source.volume : 0f;

        internal AudioHandle(AudioSource source, AudioCategory category)
        {
            Source = source;
            Category = category;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            if (IsValid)
            {
                Source.Stop();
            }
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (IsValid)
            {
                Source.Pause();
            }
        }

        /// <summary>
        /// 继续播放
        /// </summary>
        public void Resume()
        {
            if (IsValid)
            {
                Source.UnPause();
            }
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        public void SetVolume(float volume)
        {
            if (IsValid)
            {
                Source.volume = Mathf.Clamp01(volume);
            }
        }

        /// <summary>
        /// 设置音调
        /// </summary>
        public void SetPitch(float pitch)
        {
            if (IsValid)
            {
                Source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            }
        }
    }

    /// <summary>
    /// 音频播放参数
    /// </summary>
    [Serializable]
    public class AudioPlayParams
    {
        [Header("基础参数")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Range(0.1f, 3f)]
        public float Pitch = 1f;

        public bool Loop = false;

        [Header("3D音效参数")]
        public Vector3? Position = null;
        public Transform AttachTo = null;

        [Range(0f, 1f)]
        public float SpatialBlend = 1f;

        public float MinDistance = 1f;
        public float MaxDistance = 50f;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

        [Header("淡入淡出")]
        public float FadeInTime = 0f;
        public float FadeOutTime = 0f;
        public AudioFadeType FadeType = AudioFadeType.Linear;

        [Header("优先级")]
        public AudioPriority Priority = AudioPriority.Normal;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public AudioPlayParams() { }

        /// <summary>
        /// 便捷构造函数 - 基础音效
        /// </summary>
        public static AudioPlayParams Simple(float volume = 1f, float pitch = 1f)
        {
            return new AudioPlayParams { Volume = volume, Pitch = pitch };
        }

        /// <summary>
        /// 便捷构造函数 - 3D位置音效
        /// </summary>
        public static AudioPlayParams At(Vector3 position, float volume = 1f)
        {
            return new AudioPlayParams
            {
                Volume = volume,
                Position = position,
                SpatialBlend = 1f
            };
        }

        /// <summary>
        /// 便捷构造函数 - 跟随对象音效
        /// </summary>
        public static AudioPlayParams Follow(Transform target, float volume = 1f)
        {
            return new AudioPlayParams
            {
                Volume = volume,
                AttachTo = target,
                SpatialBlend = 1f
            };
        }

        /// <summary>
        /// 便捷构造函数 - 循环音效
        /// </summary>
        public static AudioPlayParams Looped(float volume = 1f)
        {
            return new AudioPlayParams { Volume = volume, Loop = true };
        }
    }
}