using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 设置滑块组件
    /// Setting slider component for float value settings
    /// </summary>
    public class SettingSlider : SettingComponentBase
    {
        [Header("滑块配置")]
        [SerializeField]
        [Tooltip("滑块组件")]
        private Slider slider;

        [SerializeField]
        [Tooltip("数值显示文本")]
        private TextMeshProUGUI valueText;

        [SerializeField]
        [Tooltip("最小值")]
        private float minValue = 0f;

        [SerializeField]
        [Tooltip("最大值")]
        private float maxValue = 1f;

        [SerializeField]
        [Tooltip("步长")]
        private float step = 0.1f;

        [SerializeField]
        [Tooltip("显示格式（如：{0:P0} 表示百分比）")]
        private string displayFormat = "{0:F1}";

        [SerializeField]
        [Tooltip("是否显示百分比")]
        private bool showAsPercentage = false;

        [SerializeField]
        [Tooltip("实时更新（拖拽时实时更新值）")]
        private bool realtimeUpdate = true;

        // 设置类型标识
        public enum SliderSettingType
        {
            MasterVolume,
            MusicVolume,
            SfxVolume,
            VoiceVolume,
            HapticIntensity,
            MouseSensitivity,
            VrControllerSensitivity,
            DeadZone,
            RenderScale,
            UiScale,
            ComfortLevel,
            HandTrackingAccuracy
        }

        [Header("设置绑定")]
        [SerializeField]
        [Tooltip("设置类型")]
        private SliderSettingType settingType;

        // 内部状态
        private float currentFloatValue;
        private bool isUpdatingUI = false;

        #region 重写基类方法

        protected override void SetupUI()
        {
            if (slider == null)
            {
                slider = GetComponentInChildren<Slider>();
            }

            if (valueText == null)
            {
                valueText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (slider != null)
            {
                // 设置滑块范围
                slider.minValue = minValue;
                slider.maxValue = maxValue;

                // 设置步长（如果支持）
                if (step > 0)
                {
                    slider.wholeNumbers = false;
                    // Unity滑块不直接支持步长，需要在值变更时处理
                }

                // 注册事件
                slider.onValueChanged.AddListener(OnSliderValueChanged);

                // 添加交互事件
                var eventTrigger = slider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = slider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
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

            float value = GetCurrentSettingValue();
            currentFloatValue = value;
            currentValue = value;
        }

        protected override void ApplyValue(object newValue)
        {
            if (newValue is float floatValue)
            {
                ApplySettingValue(floatValue);
                currentFloatValue = floatValue;
            }
        }

        protected override void UpdateUI()
        {
            if (isUpdatingUI) return;

            isUpdatingUI = true;

            if (slider != null)
            {
                slider.value = currentFloatValue;
            }

            if (valueText != null)
            {
                string displayValue;
                if (showAsPercentage)
                {
                    displayValue = string.Format(displayFormat, currentFloatValue);
                }
                else
                {
                    displayValue = string.Format(displayFormat, currentFloatValue);
                }
                valueText.text = displayValue;
            }

            isUpdatingUI = false;
        }

        protected override bool ValidateValue(object value)
        {
            if (!(value is float floatValue))
                return false;

            return floatValue >= minValue && floatValue <= maxValue;
        }

        public override void ResetToDefault()
        {
            float defaultValue = GetDefaultValue();
            SetValue(defaultValue);
        }

        #endregion

        #region 滑块事件处理

        /// <summary>
        /// 滑块值变更事件
        /// </summary>
        /// <param name="value">新值</param>
        private void OnSliderValueChanged(float value)
        {
            if (isUpdatingUI) return;

            // 应用步长
            if (step > 0)
            {
                value = Mathf.Round(value / step) * step;
                value = Mathf.Clamp(value, minValue, maxValue);
            }

            currentFloatValue = value;
            currentValue = value;

            // 更新显示
            UpdateValueDisplay();

            // 实时更新设置（如果启用）
            if (realtimeUpdate)
            {
                // 通过基类的SetValue方法处理值变更
                SetValue(value);
            }
        }

        /// <summary>
        /// 更新数值显示
        /// </summary>
        private void UpdateValueDisplay()
        {
            if (valueText != null)
            {
                string displayValue;
                if (showAsPercentage)
                {
                    displayValue = string.Format(displayFormat, currentFloatValue);
                }
                else
                {
                    displayValue = string.Format(displayFormat, currentFloatValue);
                }
                valueText.text = displayValue;
            }
        }

        #endregion

        #region 设置值处理

        /// <summary>
        /// 获取当前设置值
        /// </summary>
        /// <returns>当前设置值</returns>
        private float GetCurrentSettingValue()
        {
            if (settingsManager == null) return GetDefaultValue();

            switch (settingType)
            {
                case SliderSettingType.MasterVolume:
                    return settingsManager.GetAudioSettings().masterVolume;
                case SliderSettingType.MusicVolume:
                    return settingsManager.GetAudioSettings().musicVolume;
                case SliderSettingType.SfxVolume:
                    return settingsManager.GetAudioSettings().sfxVolume;
                case SliderSettingType.VoiceVolume:
                    return settingsManager.GetAudioSettings().voiceVolume;
                case SliderSettingType.HapticIntensity:
                    return settingsManager.GetControlSettings().hapticIntensity;
                case SliderSettingType.MouseSensitivity:
                    return settingsManager.GetControlSettings().mouseSensitivity;
                case SliderSettingType.VrControllerSensitivity:
                    return settingsManager.GetControlSettings().vrControllerSensitivity;
                case SliderSettingType.DeadZone:
                    return settingsManager.GetControlSettings().deadZone;
                case SliderSettingType.RenderScale:
                    return settingsManager.GetVideoSettings().renderScale;
                case SliderSettingType.UiScale:
                    return settingsManager.GetGameplaySettings().uiScale;
                case SliderSettingType.ComfortLevel:
                    return settingsManager.GetVideoSettings().comfortSettings.comfortLevel;
                case SliderSettingType.HandTrackingAccuracy:
                    return settingsManager.GetControlSettings().handTrackingAccuracy;
                default:
                    return GetDefaultValue();
            }
        }

        /// <summary>
        /// 应用设置值
        /// </summary>
        /// <param name="value">新值</param>
        private void ApplySettingValue(float value)
        {
            if (settingsManager == null) return;

            switch (settingType)
            {
                case SliderSettingType.MasterVolume:
                    var audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.masterVolume = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case SliderSettingType.MusicVolume:
                    audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.musicVolume = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case SliderSettingType.SfxVolume:
                    audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.sfxVolume = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case SliderSettingType.VoiceVolume:
                    audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.voiceVolume = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case SliderSettingType.HapticIntensity:
                    var controlSettings = settingsManager.GetControlSettings();
                    controlSettings.hapticIntensity = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case SliderSettingType.MouseSensitivity:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.mouseSensitivity = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case SliderSettingType.VrControllerSensitivity:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.vrControllerSensitivity = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case SliderSettingType.DeadZone:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.deadZone = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case SliderSettingType.RenderScale:
                    var videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.renderScale = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case SliderSettingType.UiScale:
                    var gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.uiScale = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case SliderSettingType.ComfortLevel:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.comfortSettings.comfortLevel = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case SliderSettingType.HandTrackingAccuracy:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.handTrackingAccuracy = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
            }
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <returns>默认值</returns>
        private float GetDefaultValue()
        {
            switch (settingType)
            {
                case SliderSettingType.MasterVolume:
                case SliderSettingType.VoiceVolume:
                case SliderSettingType.HapticIntensity:
                case SliderSettingType.MouseSensitivity:
                case SliderSettingType.VrControllerSensitivity:
                case SliderSettingType.RenderScale:
                case SliderSettingType.UiScale:
                case SliderSettingType.HandTrackingAccuracy:
                    return 1.0f;
                case SliderSettingType.MusicVolume:
                    return 0.8f;
                case SliderSettingType.SfxVolume:
                    return 0.9f;
                case SliderSettingType.DeadZone:
                    return 0.1f;
                case SliderSettingType.ComfortLevel:
                    return 0.5f;
                default:
                    return 0.5f;
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置滑块范围
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public void SetRange(float min, float max)
        {
            minValue = min;
            maxValue = max;

            if (slider != null)
            {
                slider.minValue = min;
                slider.maxValue = max;
            }
        }

        /// <summary>
        /// 设置滑块最小最大值（兼容方法）
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public void SetMinMaxValues(float min, float max)
        {
            SetRange(min, max);
        }

        /// <summary>
        /// 获取或设置当前值
        /// </summary>
        public float value
        {
            get
            {
                return slider != null ? slider.value : currentFloatValue;
            }
            set
            {
                if (slider != null)
                {
                    slider.value = value;
                }
                currentFloatValue = value;
                UpdateUI();
            }
        }

        /// <summary>
        /// 值变更事件
        /// </summary>
        public UnityEngine.Events.UnityEvent<float> onValueChanged
        {
            get
            {
                return slider != null ? slider.onValueChanged : null;
            }
        }

        /// <summary>
        /// 设置步长
        /// </summary>
        /// <param name="stepValue">步长值</param>
        public void SetStep(float stepValue)
        {
            step = stepValue;
        }

        /// <summary>
        /// 设置显示格式
        /// </summary>
        /// <param name="format">格式字符串</param>
        public void SetDisplayFormat(string format)
        {
            displayFormat = format;
            UpdateUI();
        }

        /// <summary>
        /// 设置是否实时更新
        /// </summary>
        /// <param name="realtime">是否实时更新</param>
        public void SetRealtimeUpdate(bool realtime)
        {
            realtimeUpdate = realtime;
        }

        #endregion

        #region 清理

        protected override void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }

            base.OnDestroy();
        }

        #endregion
    }
}