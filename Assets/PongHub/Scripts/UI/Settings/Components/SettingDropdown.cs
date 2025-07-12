using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 设置下拉框组件
    /// Setting dropdown component for enum value settings
    /// </summary>
    public class SettingDropdown : SettingComponentBase
    {
        [Header("下拉框配置")]
        [SerializeField]
        [Tooltip("下拉框组件")]
        private TMP_Dropdown dropdown;

        // 设置类型标识
        public enum DropdownSettingType
        {
            AudioQuality,
            RenderQuality,
            AntiAliasing,
            ShadowQuality,
            ControlScheme,
            DominantHand,
            HandPreference,
            DefaultDifficulty,
            ExperienceLevel,
            Language,
            TargetFrameRate
        }

        [Header("设置绑定")]
        [SerializeField]
        [Tooltip("设置类型")]
        private DropdownSettingType settingType;

        [Header("自定义选项")]
        [SerializeField]
        [Tooltip("自定义选项列表（如果为空则使用枚举）")]
        private List<string> customOptions = new List<string>();

        // 内部状态
        private int currentIntValue;
        private bool isUpdatingUI = false;

        #region 重写基类方法

        protected override void SetupUI()
        {
            if (dropdown == null)
            {
                dropdown = GetComponentInChildren<TMP_Dropdown>();
            }

            if (dropdown != null)
            {
                // 设置选项
                SetupDropdownOptions();

                // 注册事件
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

                // 添加交互事件
                var eventTrigger = dropdown.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = dropdown.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }

                // 添加鼠标进入/离开事件
                var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
                };
                pointerEnter.callback.AddListener((data) => StartInteraction());
                eventTrigger.triggers.Add(pointerEnter);

                var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
                };
                pointerExit.callback.AddListener((data) => EndInteraction());
                eventTrigger.triggers.Add(pointerExit);
            }
        }

        protected override void RefreshValue()
        {
            if (settingsManager == null) return;

            int value = GetCurrentSettingValue();
            currentIntValue = value;
            currentValue = value;
        }

        protected override void ApplyValue(object newValue)
        {
            if (newValue is int intValue)
            {
                ApplySettingValue(intValue);
                currentIntValue = intValue;
            }
        }

        protected override void UpdateUI()
        {
            if (isUpdatingUI) return;

            isUpdatingUI = true;

            if (dropdown != null)
            {
                dropdown.value = currentIntValue;
            }

            isUpdatingUI = false;
        }

        protected override bool ValidateValue(object value)
        {
            if (!(value is int intValue))
                return false;

            return intValue >= 0 && intValue < GetMaxValue();
        }

        public override void ResetToDefault()
        {
            int defaultValue = GetDefaultValue();
            SetValue(defaultValue);
        }

        #endregion

        #region 下拉框设置

        /// <summary>
        /// 设置下拉框选项
        /// </summary>
        private void SetupDropdownOptions()
        {
            if (dropdown == null) return;

            List<string> options;

            if (customOptions.Count > 0)
            {
                options = customOptions;
            }
            else
            {
                options = GetEnumOptions();
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }

        /// <summary>
        /// 获取枚举选项
        /// </summary>
        /// <returns>选项列表</returns>
        private List<string> GetEnumOptions()
        {
            switch (settingType)
            {
                case DropdownSettingType.AudioQuality:
                    return System.Enum.GetNames(typeof(AudioQuality)).Select(LocalizeEnumValue).ToList();
                case DropdownSettingType.RenderQuality:
                    return System.Enum.GetNames(typeof(RenderQuality)).Select(LocalizeEnumValue).ToList();
                case DropdownSettingType.AntiAliasing:
                    return new List<string> { "关闭", "MSAA 2x", "MSAA 4x", "MSAA 8x" };
                case DropdownSettingType.ShadowQuality:
                    return System.Enum.GetNames(typeof(ShadowQualityLevel)).Select(LocalizeEnumValue).ToList();
                case DropdownSettingType.ControlScheme:
                    return new List<string> { "默认", "左手", "自定义", "无障碍" };
                case DropdownSettingType.DominantHand:
                case DropdownSettingType.HandPreference:
                    return new List<string> { "左手", "右手", "双手" };
                case DropdownSettingType.DefaultDifficulty:
                    return new List<string> { "简单", "普通", "困难", "专家" };
                case DropdownSettingType.ExperienceLevel:
                    return new List<string> { "新手", "中级", "高级", "专家" };
                case DropdownSettingType.Language:
                    return new List<string> { "English", "中文", "日本語", "한국어", "Français", "Deutsch", "Español" };
                case DropdownSettingType.TargetFrameRate:
                    return new List<string> { "72 FPS", "90 FPS", "120 FPS" };
                default:
                    return new List<string> { "选项1", "选项2", "选项3" };
            }
        }

        /// <summary>
        /// 本地化枚举值
        /// </summary>
        /// <param name="enumValue">枚举值</param>
        /// <returns>本地化字符串</returns>
        private string LocalizeEnumValue(string enumValue)
        {
            // TODO: 实现本地化
            switch (enumValue)
            {
                case "Low": return "低";
                case "Medium": return "中";
                case "High": return "高";
                case "Ultra": return "超高";
                case "Disabled": return "禁用";
                default: return enumValue;
            }
        }

        #endregion

        #region 下拉框事件处理

        /// <summary>
        /// 下拉框值变更事件
        /// </summary>
        /// <param name="value">新值</param>
        private void OnDropdownValueChanged(int value)
        {
            if (isUpdatingUI) return;

            // 通过基类的SetValue方法处理值变更
            SetValue(value);
        }

        #endregion

        #region 设置值处理

        /// <summary>
        /// 获取当前设置值
        /// </summary>
        /// <returns>当前设置值</returns>
        private int GetCurrentSettingValue()
        {
            if (settingsManager == null) return GetDefaultValue();

            switch (settingType)
            {
                case DropdownSettingType.AudioQuality:
                    return (int)settingsManager.GetAudioSettings().audioQuality;
                case DropdownSettingType.RenderQuality:
                    return (int)settingsManager.GetVideoSettings().renderQuality;
                case DropdownSettingType.AntiAliasing:
                    return (int)settingsManager.GetVideoSettings().antiAliasing;
                case DropdownSettingType.ShadowQuality:
                    return (int)settingsManager.GetVideoSettings().shadowQuality;
                case DropdownSettingType.ControlScheme:
                    return (int)settingsManager.GetControlSettings().controlScheme;
                case DropdownSettingType.DominantHand:
                    return (int)settingsManager.GetControlSettings().dominantHand;
                case DropdownSettingType.HandPreference:
                    return (int)settingsManager.GetUserProfile().handPreference;
                case DropdownSettingType.DefaultDifficulty:
                    return (int)settingsManager.GetGameplaySettings().defaultDifficulty;
                case DropdownSettingType.ExperienceLevel:
                    return (int)settingsManager.GetUserProfile().experience;
                case DropdownSettingType.Language:
                    return (int)settingsManager.GetGameplaySettings().language;
                case DropdownSettingType.TargetFrameRate:
                    int frameRate = settingsManager.GetVideoSettings().targetFrameRate;
                    switch (frameRate)
                    {
                        case 72: return 0;
                        case 90: return 1;
                        case 120: return 2;
                        default: return 1;
                    }
                default:
                    return GetDefaultValue();
            }
        }

        /// <summary>
        /// 应用设置值
        /// </summary>
        /// <param name="value">新值</param>
        private void ApplySettingValue(int value)
        {
            if (settingsManager == null) return;

            switch (settingType)
            {
                case DropdownSettingType.AudioQuality:
                    var audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.audioQuality = (AudioQuality)value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case DropdownSettingType.RenderQuality:
                    var videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.renderQuality = (RenderQuality)value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case DropdownSettingType.AntiAliasing:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.antiAliasing = (AntiAliasing)value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case DropdownSettingType.ShadowQuality:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.shadowQuality = (ShadowQualityLevel)value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case DropdownSettingType.ControlScheme:
                    var controlSettings = settingsManager.GetControlSettings();
                    controlSettings.controlScheme = (ControlScheme)value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case DropdownSettingType.DominantHand:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.dominantHand = (HandPreference)value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case DropdownSettingType.HandPreference:
                    var userProfile = settingsManager.GetUserProfile();
                    userProfile.handPreference = (HandPreference)value;
                    settingsManager.UpdateUserProfile(userProfile);
                    break;
                case DropdownSettingType.DefaultDifficulty:
                    var gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.defaultDifficulty = (PongHub.UI.Settings.Core.DifficultyLevel)value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case DropdownSettingType.ExperienceLevel:
                    userProfile = settingsManager.GetUserProfile();
                    userProfile.experience = (ExperienceLevel)value;
                    settingsManager.UpdateUserProfile(userProfile);
                    break;
                case DropdownSettingType.Language:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.language = (LanguageCode)value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case DropdownSettingType.TargetFrameRate:
                    videoSettings = settingsManager.GetVideoSettings();
                    int[] frameRates = { 72, 90, 120 };
                    if (value >= 0 && value < frameRates.Length)
                    {
                        videoSettings.targetFrameRate = frameRates[value];
                    }
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
            }
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <returns>默认值</returns>
        private int GetDefaultValue()
        {
            switch (settingType)
            {
                case DropdownSettingType.AudioQuality:
                case DropdownSettingType.RenderQuality:
                    return 2; // High
                case DropdownSettingType.AntiAliasing:
                    return 2; // MSAA 4x
                case DropdownSettingType.ShadowQuality:
                    return 2; // High
                case DropdownSettingType.ControlScheme:
                    return 0; // Default
                case DropdownSettingType.DominantHand:
                case DropdownSettingType.HandPreference:
                    return 1; // Right
                case DropdownSettingType.DefaultDifficulty:
                case DropdownSettingType.ExperienceLevel:
                    return 1; // Normal/Intermediate
                case DropdownSettingType.Language:
                    return 1; // Chinese
                case DropdownSettingType.TargetFrameRate:
                    return 1; // 90 FPS
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取最大值
        /// </summary>
        /// <returns>最大值</returns>
        private int GetMaxValue()
        {
            if (customOptions.Count > 0)
                return customOptions.Count;

            switch (settingType)
            {
                case DropdownSettingType.AudioQuality:
                case DropdownSettingType.RenderQuality:
                case DropdownSettingType.AntiAliasing:
                case DropdownSettingType.ShadowQuality:
                case DropdownSettingType.ControlScheme:
                case DropdownSettingType.DefaultDifficulty:
                case DropdownSettingType.ExperienceLevel:
                    return 4;
                case DropdownSettingType.DominantHand:
                case DropdownSettingType.HandPreference:
                case DropdownSettingType.TargetFrameRate:
                    return 3;
                case DropdownSettingType.Language:
                    return 7;
                default:
                    return 3;
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置自定义选项
        /// </summary>
        /// <param name="options">选项列表</param>
        public void SetCustomOptions(List<string> options)
        {
            customOptions = options;
            SetupDropdownOptions();
        }

        /// <summary>
        /// 清空所有选项
        /// </summary>
        public void ClearOptions()
        {
            customOptions.Clear();
            if (dropdown != null)
            {
                dropdown.options.Clear();
            }
        }

        /// <summary>
        /// 添加多个选项
        /// </summary>
        /// <param name="options">选项数组</param>
        public void AddOptions(string[] options)
        {
            foreach (string option in options)
            {
                AddOption(option);
            }
        }

        /// <summary>
        /// 添加选项
        /// </summary>
        /// <param name="option">选项</param>
        public void AddOption(string option)
        {
            customOptions.Add(option);
            if (dropdown != null)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }
        }

        /// <summary>
        /// 获取或设置当前值
        /// </summary>
        public int value
        {
            get
            {
                return dropdown != null ? dropdown.value : currentIntValue;
            }
            set
            {
                if (dropdown != null)
                {
                    dropdown.value = value;
                }
                currentIntValue = value;
                UpdateUI();
            }
        }

        /// <summary>
        /// 值变更事件
        /// </summary>
        public UnityEngine.Events.UnityEvent<int> onValueChanged
        {
            get
            {
                return dropdown != null ? dropdown.onValueChanged : null;
            }
        }

        /// <summary>
        /// 移除选项
        /// </summary>
        /// <param name="index">索引</param>
        public void RemoveOption(int index)
        {
            if (index >= 0 && index < customOptions.Count)
            {
                customOptions.RemoveAt(index);
                if (dropdown != null && index < dropdown.options.Count)
                {
                    dropdown.options.RemoveAt(index);
                }
            }
        }

        #endregion

        #region 清理

        protected override void OnDestroy()
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }

            base.OnDestroy();
        }

        #endregion
    }
}