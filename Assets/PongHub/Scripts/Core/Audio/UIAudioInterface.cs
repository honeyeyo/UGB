using UnityEngine;
using Meta.Utilities;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// UI音频接口
    /// 为UI系统提供现代化的音频控制方法，兼容原有的AudioController接口
    /// </summary>
    public class UIAudioInterface : Singleton<UIAudioInterface>
    {
        /// <summary>
        /// 音频服务引用
        /// </summary>
        private AudioService AudioService => AudioService.Instance;

        /// <summary>
        /// 音频控制器引用
        /// </summary>
        private AudioController AudioController => GetComponent<AudioController>();

        #region 音量控制属性（兼容原接口）

        /// <summary>
        /// 获取或设置音乐音量 (0-1)
        /// </summary>
        public float MusicVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.Music) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.Music, value);
        }

        /// <summary>
        /// 获取音乐音量百分比 (0-100)
        /// </summary>
        public int MusicVolumePct => Mathf.RoundToInt(MusicVolume * 100f);

        /// <summary>
        /// 获取或设置音效音量 (0-1)
        /// </summary>
        public float SfxVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.SFX) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.SFX, value);
        }

        /// <summary>
        /// 获取音效音量百分比 (0-100)
        /// </summary>
        public int SfxVolumePct => Mathf.RoundToInt(SfxVolume * 100f);

        /// <summary>
        /// 获取或设置观众音量 (0-1)
        /// </summary>
        public float CrowdVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.Crowd) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.Crowd, value);
        }

        /// <summary>
        /// 获取观众音量百分比 (0-100)
        /// </summary>
        public int CrowdVolumePct => Mathf.RoundToInt(CrowdVolume * 100f);

        /// <summary>
        /// 获取或设置语音音量 (0-1)
        /// </summary>
        public float VoiceVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.Voice) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.Voice, value);
        }

        /// <summary>
        /// 获取语音音量百分比 (0-100)
        /// </summary>
        public int VoiceVolumePct => Mathf.RoundToInt(VoiceVolume * 100f);

        /// <summary>
        /// 获取或设置环境音量 (0-1)
        /// </summary>
        public float AmbientVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.Ambient) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.Ambient, value);
        }

        /// <summary>
        /// 获取环境音量百分比 (0-100)
        /// </summary>
        public int AmbientVolumePct => Mathf.RoundToInt(AmbientVolume * 100f);

        /// <summary>
        /// 获取或设置UI音量 (0-1)
        /// </summary>
        public float UIVolume
        {
            get => AudioService?.GetCategoryVolume(AudioCategory.UI) ?? 0f;
            set => AudioService?.SetCategoryVolume(AudioCategory.UI, value);
        }

        /// <summary>
        /// 获取UI音量百分比 (0-100)
        /// </summary>
        public int UIVolumePct => Mathf.RoundToInt(UIVolume * 100f);

        #endregion

        #region 兼容方法（向后兼容原AudioController接口）

        /// <summary>
        /// 设置音乐音量（兼容方法）
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            MusicVolume = volume;
        }

        /// <summary>
        /// 设置音效音量（兼容方法）
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = volume;
        }

        /// <summary>
        /// 设置观众音量（兼容方法）
        /// </summary>
        public void SetCrowdVolume(float volume)
        {
            CrowdVolume = volume;
        }

        /// <summary>
        /// 设置语音音量（兼容方法）
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            VoiceVolume = volume;
        }

        /// <summary>
        /// 设置环境音量（兼容方法）
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            AmbientVolume = volume;
        }

        /// <summary>
        /// 设置UI音量（兼容方法）
        /// </summary>
        public void SetUIVolume(float volume)
        {
            UIVolume = volume;
        }

        #endregion

        #region UI专用音效方法

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        public AudioHandle PlayButtonClick(float volume = 1f)
        {
            return AudioController?.PlayButtonClick(volume);
        }

        /// <summary>
        /// 播放按钮悬停音效
        /// </summary>
        public AudioHandle PlayButtonHover(float volume = 0.7f)
        {
            return AudioController?.PlayButtonHover(volume);
        }

        /// <summary>
        /// 播放错误音效
        /// </summary>
        public AudioHandle PlayError(float volume = 1f)
        {
            return AudioController?.PlayError(volume);
        }

        /// <summary>
        /// 播放成功音效
        /// </summary>
        public AudioHandle PlaySuccess(float volume = 1f)
        {
            return AudioController?.PlaySuccess(volume);
        }

        /// <summary>
        /// 播放通知音效
        /// </summary>
        public AudioHandle PlayNotification(float volume = 0.8f)
        {
            return AudioController?.PlayNotification(volume);
        }

        /// <summary>
        /// 播放滑块变化音效
        /// </summary>
        public AudioHandle PlaySliderChanged(float volume = 0.5f)
        {
            return AudioController?.PlayQuickSound("click", volume * 0.5f);
        }

        /// <summary>
        /// 播放开关切换音效
        /// </summary>
        public AudioHandle PlayToggleChanged(float volume = 0.6f)
        {
            return AudioController?.PlayQuickSound("click", volume * 0.7f);
        }

        /// <summary>
        /// 播放下拉框变化音效
        /// </summary>
        public AudioHandle PlayDropdownChanged(float volume = 0.6f)
        {
            return AudioController?.PlayQuickSound("click", volume * 0.8f);
        }

        #endregion

        #region 高级UI音频控制

        /// <summary>
        /// 平滑调整分类音量（带反馈音效）
        /// </summary>
        public void SmoothSetCategoryVolume(AudioCategory category, float targetVolume, bool playFeedback = true)
        {
            if (AudioController != null)
            {
                AudioController.SmoothSetCategoryVolume(category, targetVolume, 0.5f);

                if (playFeedback)
                {
                    PlaySliderChanged(0.3f);
                }
            }
        }

        /// <summary>
        /// 临时降低背景音乐音量（用于对话或重要提示）
        /// </summary>
        public void DuckBackgroundMusic(float duckVolume = 0.3f, float duration = 3f)
        {
            AudioController?.DuckCategory(AudioCategory.Music, duckVolume, duration);
        }

        /// <summary>
        /// 临时降低音效音量（用于语音聊天）
        /// </summary>
        public void DuckSoundEffects(float duckVolume = 0.4f, float duration = 2f)
        {
            AudioController?.DuckCategory(AudioCategory.SFX, duckVolume, duration);
        }

        /// <summary>
        /// 播放UI音频序列（用于复杂的UI反馈）
        /// </summary>
        public void PlayUISequence(params AudioSequenceItem[] items)
        {
            AudioController?.PlayAudioSequence(items);
        }

        #endregion

        #region 音频设置实用方法

        /// <summary>
        /// 获取所有音频分类的音量设置
        /// </summary>
        public AudioVolumeSettings GetAllVolumeSettings()
        {
            return new AudioVolumeSettings
            {
                MusicVolume = MusicVolume,
                SfxVolume = SfxVolume,
                CrowdVolume = CrowdVolume,
                VoiceVolume = VoiceVolume,
                AmbientVolume = AmbientVolume,
                UIVolume = UIVolume
            };
        }

        /// <summary>
        /// 应用所有音频分类的音量设置
        /// </summary>
        public void ApplyAllVolumeSettings(AudioVolumeSettings settings)
        {
            MusicVolume = settings.MusicVolume;
            SfxVolume = settings.SfxVolume;
            CrowdVolume = settings.CrowdVolume;
            VoiceVolume = settings.VoiceVolume;
            AmbientVolume = settings.AmbientVolume;
            UIVolume = settings.UIVolume;
        }

        /// <summary>
        /// 重置所有音量到默认值
        /// </summary>
        public void ResetToDefaultVolumes()
        {
            var config = AudioService?.Configuration;
            if (config == null) return;

            MusicVolume = config.DefaultMusicVolume;
            SfxVolume = config.DefaultSfxVolume;
            CrowdVolume = config.DefaultCrowdVolume;
            VoiceVolume = config.DefaultVoiceVolume;
            AmbientVolume = config.DefaultAmbientVolume;
            UIVolume = config.DefaultUIVolume;

            PlaySuccess(0.7f);
        }

        /// <summary>
        /// 静音所有音频
        /// </summary>
        public void MuteAll()
        {
            MusicVolume = 0f;
            SfxVolume = 0f;
            CrowdVolume = 0f;
            VoiceVolume = 0f;
            AmbientVolume = 0f;
            UIVolume = 0f;
        }

        /// <summary>
        /// 检查是否所有音频都已静音
        /// </summary>
        public bool IsAllMuted()
        {
            return MusicVolume <= 0f && SfxVolume <= 0f && CrowdVolume <= 0f &&
                   VoiceVolume <= 0f && AmbientVolume <= 0f && UIVolume <= 0f;
        }

        #endregion

        #region 调试信息

        /// <summary>
        /// 获取UI音频接口的调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            var info = "UIAudioInterface Debug Info:\n";
            info += $"- Music: {MusicVolumePct}%\n";
            info += $"- SFX: {SfxVolumePct}%\n";
            info += $"- Crowd: {CrowdVolumePct}%\n";
            info += $"- Voice: {VoiceVolumePct}%\n";
            info += $"- Ambient: {AmbientVolumePct}%\n";
            info += $"- UI: {UIVolumePct}%\n";
            info += $"- All Muted: {IsAllMuted()}\n";

            if (AudioService != null)
            {
                info += $"- AudioService Available: Yes\n";
                info += $"- AudioService Active Audio: {AudioService.ActiveAudioCount}\n";
            }
            else
            {
                info += $"- AudioService Available: No\n";
            }

            return info;
        }

        #endregion
    }

    /// <summary>
    /// 音频音量设置数据结构
    /// 用于保存和恢复所有音频分类的音量设置
    /// </summary>
    [System.Serializable]
    public struct AudioVolumeSettings
    {
        public float MusicVolume;
        public float SfxVolume;
        public float CrowdVolume;
        public float VoiceVolume;
        public float AmbientVolume;
        public float UIVolume;

        /// <summary>
        /// 创建默认音量设置
        /// </summary>
        public static AudioVolumeSettings Default => new()
        {
            MusicVolume = 0.7f,
            SfxVolume = 0.8f,
            CrowdVolume = 0.6f,
            VoiceVolume = 1.0f,
            AmbientVolume = 0.5f,
            UIVolume = 0.8f
        };
    }
}