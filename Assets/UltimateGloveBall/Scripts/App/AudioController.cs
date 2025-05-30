// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace PongHub.App
{
    /// <summary>
    /// 音频控制器
    /// 控制游戏音频，可以设置音乐、音效和人群音量
    /// 引用AudioMixer并对每个音量滑块的暴露属性应用更改
    /// 从设置中获取和设置每个音量属性，以在应用程序启动之间保存状态
    /// 动态生成的音效音频源可以分配给音效混音器组
    /// </summary>
    public class AudioController : Singleton<AudioController>
    {
        /// <summary>
        /// 音乐音量参数名称常量
        /// </summary>
        private const string MUSIC_VOL = "MusicVol";

        /// <summary>
        /// 音效音量参数名称常量
        /// </summary>
        private const string SFX_VOL = "SfxVol";

        /// <summary>
        /// 人群音量参数名称常量
        /// </summary>
        private const string CROWD_VOL = "CrowdVol";

        /// <summary>
        /// 主音频混音器
        /// 控制所有音频通道的主混音器
        /// </summary>
        [SerializeField] private AudioMixer m_audioMixer;

        /// <summary>
        /// 音效混音器组
        /// 用于动态创建的音效音频源
        /// </summary>
        [SerializeField] private AudioMixerGroup m_sfxGroup;

        /// <summary>
        /// 获取音效混音器组
        /// 供其他脚本使用来设置音频源的输出组
        /// </summary>
        public AudioMixerGroup SfxGroup => m_sfxGroup;

        /// <summary>
        /// 获取音乐音量（0-1范围）
        /// </summary>
        public float MusicVolume => GameSettings.Instance.MusicVolume;

        /// <summary>
        /// 获取音乐音量百分比（0-100范围）
        /// </summary>
        public int MusicVolumePct => Mathf.RoundToInt(MusicVolume * 100);

        /// <summary>
        /// 获取音效音量（0-1范围）
        /// </summary>
        public float SfxVolume => GameSettings.Instance.SfxVolume;

        /// <summary>
        /// 获取音效音量百分比（0-100范围）
        /// </summary>
        public int SfxVolumePct => Mathf.RoundToInt(SfxVolume * 100);

        /// <summary>
        /// 获取人群音量（0-1范围）
        /// </summary>
        public float CrowdVolume => GameSettings.Instance.CrowdVolume;

        /// <summary>
        /// 获取人群音量百分比（0-100范围）
        /// </summary>
        public int CrowdVolumePct => Mathf.RoundToInt(CrowdVolume * 100);

        /// <summary>
        /// 启动时初始化
        /// 从游戏设置中加载保存的音量设置并应用到混音器
        /// </summary>
        private void Start()
        {
            // 从设置中恢复各种音量设置
            SetMusicVolume(GameSettings.Instance.MusicVolume);
            SetSfxVolume(GameSettings.Instance.SfxVolume);
            SetCrowdVolume(GameSettings.Instance.CrowdVolume);
        }

        /// <summary>
        /// 设置音乐音量
        /// 将音量值保存到设置中并应用到音频混音器
        /// </summary>
        /// <param name="val">音量值（0-1范围）</param>
        public void SetMusicVolume(float val)
        {
            // 保存到游戏设置
            GameSettings.Instance.MusicVolume = val;
            // 转换为分贝值并应用到混音器
            // 使用对数转换：dB = 20 * log10(linear)
            _ = m_audioMixer.SetFloat(MUSIC_VOL, Mathf.Log10(val) * 20);
        }

        /// <summary>
        /// 设置音效音量
        /// 将音量值保存到设置中并应用到音频混音器
        /// </summary>
        /// <param name="val">音量值（0-1范围）</param>
        public void SetSfxVolume(float val)
        {
            // 保存到游戏设置
            GameSettings.Instance.SfxVolume = val;
            // 转换为分贝值并应用到混音器
            _ = m_audioMixer.SetFloat(SFX_VOL, Mathf.Log10(val) * 20);
        }

        /// <summary>
        /// 设置人群音量
        /// 将音量值保存到设置中并应用到音频混音器
        /// </summary>
        /// <param name="val">音量值（0-1范围）</param>
        public void SetCrowdVolume(float val)
        {
            // 保存到游戏设置
            GameSettings.Instance.CrowdVolume = val;
            // 转换为分贝值并应用到混音器
            _ = m_audioMixer.SetFloat(CROWD_VOL, Mathf.Log10(val) * 20);
        }
    }
}