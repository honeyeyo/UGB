using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 设置开关组件
    /// Setting toggle component for boolean value settings
    /// </summary>
    public class SettingToggle : SettingComponentBase
    {
        [Header("开关配置")]
        [SerializeField]
        [Tooltip("开关组件")]
        private Toggle toggle;

        [SerializeField]
        [Tooltip("状态文本")]
        private TextMeshProUGUI statusText;

        [SerializeField]
        [Tooltip("开启状态文本")]
        private string onText = "开启";

        [SerializeField]
        [Tooltip("关闭状态文本")]
        private string offText = "关闭";

        // 设置类型标识
        public enum ToggleSettingType
        {
            MuteOnFocusLoss,
            SpatialAudio,
            EnablePostProcessing,
            EnableVSync,
            FoveatedRendering,
            InvertY,
            HapticFeedback,
            AutoSave,
            ShowTutorials,
            EnableAssistMode,
            ShowStatistics,
            EnableDebugInfo,
            HighContrast,
            EnableSubtitles,
            SnapTurn,
            MotionSicknessReduction
        }

        [Header("设置绑定")]
        [SerializeField]
        [Tooltip("设置类型")]
        private ToggleSettingType settingType;

        // 内部状态
        private bool currentBoolValue;
        private bool isUpdatingUI = false;

        #region 重写基类方法

        protected override void SetupUI()
        {
            if (toggle == null)
            {
                toggle = GetComponentInChildren<Toggle>();
            }

            if (statusText == null)
            {
                statusText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (toggle != null)
            {
                // 注册事件
                toggle.onValueChanged.AddListener(OnToggleValueChanged);

                // 添加交互事件
                var eventTrigger = toggle.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = toggle.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
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

            bool value = GetCurrentSettingValue();
            currentBoolValue = value;
            currentValue = value;
        }

        protected override void ApplyValue(object newValue)
        {
            if (newValue is bool boolValue)
            {
                ApplySettingValue(boolValue);
                currentBoolValue = boolValue;
            }
        }

        protected override void UpdateUI()
        {
            if (isUpdatingUI) return;

            isUpdatingUI = true;

            if (toggle != null)
            {
                toggle.isOn = currentBoolValue;
            }

            if (statusText != null)
            {
                statusText.text = currentBoolValue ? onText : offText;

                // 可选：设置状态文本颜色
                statusText.color = currentBoolValue ? Color.green : Color.gray;
            }

            isUpdatingUI = false;
        }

        protected override bool ValidateValue(object value)
        {
            return value is bool;
        }

        public override void ResetToDefault()
        {
            bool defaultValue = GetDefaultValue();
            SetValue(defaultValue);
        }

        #endregion

        #region 开关事件处理

        /// <summary>
        /// 开关值变更事件
        /// </summary>
        /// <param name="value">新值</param>
        private void OnToggleValueChanged(bool value)
        {
            if (isUpdatingUI) return;

            currentBoolValue = value;

            // 更新状态显示
            UpdateStatusDisplay();

            // 通过基类的SetValue方法处理值变更
            SetValue(value);
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (statusText != null)
            {
                statusText.text = currentBoolValue ? onText : offText;
                statusText.color = currentBoolValue ? Color.green : Color.gray;
            }
        }

        #endregion

        #region 设置值处理

        /// <summary>
        /// 获取当前设置值
        /// </summary>
        /// <returns>当前设置值</returns>
        private bool GetCurrentSettingValue()
        {
            if (settingsManager == null) return GetDefaultValue();

            switch (settingType)
            {
                case ToggleSettingType.MuteOnFocusLoss:
                    return settingsManager.GetAudioSettings().muteOnFocusLoss;
                case ToggleSettingType.SpatialAudio:
                    return settingsManager.GetAudioSettings().spatialAudio;
                case ToggleSettingType.EnablePostProcessing:
                    return settingsManager.GetVideoSettings().enablePostProcessing;
                case ToggleSettingType.EnableVSync:
                    return settingsManager.GetVideoSettings().enableVSync;
                case ToggleSettingType.FoveatedRendering:
                    return settingsManager.GetVideoSettings().foveatedRendering;
                case ToggleSettingType.InvertY:
                    return settingsManager.GetControlSettings().invertY;
                case ToggleSettingType.HapticFeedback:
                    return settingsManager.GetControlSettings().hapticFeedback;
                case ToggleSettingType.AutoSave:
                    return settingsManager.GetGameplaySettings().autoSave;
                case ToggleSettingType.ShowTutorials:
                    return settingsManager.GetGameplaySettings().showTutorials;
                case ToggleSettingType.EnableAssistMode:
                    return settingsManager.GetGameplaySettings().enableAssistMode;
                case ToggleSettingType.ShowStatistics:
                    return settingsManager.GetGameplaySettings().showStatistics;
                case ToggleSettingType.EnableDebugInfo:
                    return settingsManager.GetGameplaySettings().enableDebugInfo;
                case ToggleSettingType.HighContrast:
                    return settingsManager.GetGameplaySettings().highContrast;
                case ToggleSettingType.EnableSubtitles:
                    return settingsManager.GetGameplaySettings().enableSubtitles;
                case ToggleSettingType.SnapTurn:
                    return settingsManager.GetVideoSettings().comfortSettings.snapTurn;
                case ToggleSettingType.MotionSicknessReduction:
                    return settingsManager.GetVideoSettings().comfortSettings.motionSicknessReduction;
                default:
                    return GetDefaultValue();
            }
        }

        /// <summary>
        /// 应用设置值
        /// </summary>
        /// <param name="value">新值</param>
        private void ApplySettingValue(bool value)
        {
            if (settingsManager == null) return;

            switch (settingType)
            {
                case ToggleSettingType.MuteOnFocusLoss:
                    var audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.muteOnFocusLoss = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case ToggleSettingType.SpatialAudio:
                    audioSettings = settingsManager.GetAudioSettings();
                    audioSettings.spatialAudio = value;
                    settingsManager.UpdateAudioSettings(audioSettings);
                    break;
                case ToggleSettingType.EnablePostProcessing:
                    var videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.enablePostProcessing = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case ToggleSettingType.EnableVSync:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.enableVSync = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case ToggleSettingType.FoveatedRendering:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.foveatedRendering = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case ToggleSettingType.InvertY:
                    var controlSettings = settingsManager.GetControlSettings();
                    controlSettings.invertY = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case ToggleSettingType.HapticFeedback:
                    controlSettings = settingsManager.GetControlSettings();
                    controlSettings.hapticFeedback = value;
                    settingsManager.UpdateControlSettings(controlSettings);
                    break;
                case ToggleSettingType.AutoSave:
                    var gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.autoSave = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.ShowTutorials:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.showTutorials = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.EnableAssistMode:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.enableAssistMode = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.ShowStatistics:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.showStatistics = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.EnableDebugInfo:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.enableDebugInfo = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.HighContrast:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.highContrast = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.EnableSubtitles:
                    gameplaySettings = settingsManager.GetGameplaySettings();
                    gameplaySettings.enableSubtitles = value;
                    settingsManager.UpdateGameplaySettings(gameplaySettings);
                    break;
                case ToggleSettingType.SnapTurn:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.comfortSettings.snapTurn = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
                case ToggleSettingType.MotionSicknessReduction:
                    videoSettings = settingsManager.GetVideoSettings();
                    videoSettings.comfortSettings.motionSicknessReduction = value;
                    settingsManager.UpdateVideoSettings(videoSettings);
                    break;
            }
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <returns>默认值</returns>
        private bool GetDefaultValue()
        {
            switch (settingType)
            {
                case ToggleSettingType.MuteOnFocusLoss:
                case ToggleSettingType.SpatialAudio:
                case ToggleSettingType.EnablePostProcessing:
                case ToggleSettingType.EnableVSync:
                case ToggleSettingType.FoveatedRendering:
                case ToggleSettingType.HapticFeedback:
                case ToggleSettingType.AutoSave:
                case ToggleSettingType.ShowTutorials:
                case ToggleSettingType.ShowStatistics:
                case ToggleSettingType.MotionSicknessReduction:
                    return true;
                case ToggleSettingType.InvertY:
                case ToggleSettingType.EnableAssistMode:
                case ToggleSettingType.EnableDebugInfo:
                case ToggleSettingType.HighContrast:
                case ToggleSettingType.EnableSubtitles:
                case ToggleSettingType.SnapTurn:
                    return false;
                default:
                    return false;
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取或设置开关状态
        /// </summary>
        public bool isOn
        {
            get
            {
                return toggle != null ? toggle.isOn : currentBoolValue;
            }
            set
            {
                if (toggle != null)
                {
                    toggle.isOn = value;
                }
                currentBoolValue = value;
                UpdateUI();
            }
        }

        /// <summary>
        /// 值变更事件
        /// </summary>
        public UnityEngine.Events.UnityEvent<bool> onValueChanged
        {
            get
            {
                return toggle != null ? toggle.onValueChanged : null;
            }
        }

        /// <summary>
        /// 设置状态文本
        /// </summary>
        /// <param name="onTextValue">开启状态文本</param>
        /// <param name="offTextValue">关闭状态文本</param>
        public void SetStatusText(string onTextValue, string offTextValue)
        {
            onText = onTextValue;
            offText = offTextValue;
            UpdateUI();
        }

        /// <summary>
        /// 切换开关状态
        /// </summary>
        public void ToggleValue()
        {
            SetValue(!currentBoolValue);
        }

        #endregion

        #region 清理

        protected override void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }

            base.OnDestroy();
        }

        #endregion
    }
}