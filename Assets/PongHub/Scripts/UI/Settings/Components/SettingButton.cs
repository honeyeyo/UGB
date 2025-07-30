using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection; // 添加VRHapticFeedback命名空间
using PongHub.UI.Settings.Integration;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 设置按钮组件
    /// Setting button component for actions and navigation
    /// </summary>
    public class SettingButton : SettingComponentBase
    {
        [Header("按钮配置")]
        [SerializeField]
        [Tooltip("按钮组件")]
        private Button button;

        [SerializeField]
        [Tooltip("按钮图标")]
        private Image iconImage;

        [SerializeField]
        [Tooltip("确认对话框预制件")]
        private GameObject confirmationDialogPrefab;

        // 按钮类型枚举
        public enum ButtonSettingType
        {
            ResetSettings,
            ResetAudio,
            ResetVideo,
            ResetControls,
            ResetGameplay,
            ResetProfile,
            ImportSettings,
            ExportSettings,
            CalibrateVR,
            TestAudio,
            TestVideo,
            TestHaptics,
            OpenManual,
            ContactSupport,
            CheckUpdates,
            ClearCache,
            RestoreDefaults,
            SaveProfile,
            LoadProfile,
            DeleteProfile
        }

        [Header("设置绑定")]
        [SerializeField]
        [Tooltip("按钮类型")]
        private ButtonSettingType buttonType;

        [SerializeField]
        [Tooltip("需要确认执行")]
        private bool requireConfirmation = false;

        [SerializeField]
        [Tooltip("确认消息文本")]
        private string confirmationMessage = "确定要执行此操作吗？";

        [Header("视觉状态")]
        [SerializeField]
        [Tooltip("正常状态颜色")]
        private Color normalColor = Color.white;

        [SerializeField]
        [Tooltip("悬停状态颜色")]
        private Color hoverColor = Color.cyan;

        [SerializeField]
        [Tooltip("按下状态颜色")]
        private Color pressedColor = Color.blue;

        [SerializeField]
        [Tooltip("禁用状态颜色")]
        private Color disabledColor = Color.gray;

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("启用动画效果")]
        private bool enableAnimation = true;

        [SerializeField]
        [Tooltip("动画持续时间")]
        private float animationDuration = 0.2f;

        // 内部状态
        private bool isProcessing = false;
        private ColorBlock originalColors;
        private Vector3 originalScale;

        // 事件
        public event Action<ButtonSettingType> OnButtonClicked;

        #region 重写基类方法

        protected override void SetupUI()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }

            if (button != null)
            {
                // 保存原始颜色
                originalColors = button.colors;
                originalScale = transform.localScale;

                // 设置按钮颜色
                var colors = button.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = hoverColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                button.colors = colors;

                // 注册点击事件
                button.onClick.AddListener(OnButtonClick);

                // 设置事件触发器（用于音效和触觉反馈）
                // SetupEventTrigger();
            }

            UpdateButtonState();
        }

        protected override void RefreshValue()
        {
            // 按钮不需要刷新值
        }

        protected override void ApplyValue(object newValue)
        {
            // 按钮不需要应用值
        }

        protected override void UpdateUI()
        {
            UpdateButtonState();
            UpdateButtonText();
        }

        protected override bool ValidateValue(object value)
        {
            return true; // 按钮总是有效
        }

        public override void ResetToDefault()
        {
            // 按钮不需要重置
        }

        #endregion

        #region 按钮状态管理

        private void UpdateButtonState()
        {
            if (button == null) return;

            // 根据按钮类型和当前状态更新可用性
            bool isInteractable = GetButtonInteractable();
            button.interactable = isInteractable && !isProcessing;

            // 更新图标
            UpdateIcon();
        }

        private bool GetButtonInteractable()
        {
            // 根据按钮类型判断是否可交互
            switch (buttonType)
            {
                case ButtonSettingType.ResetSettings:
                case ButtonSettingType.ResetAudio:
                case ButtonSettingType.ResetVideo:
                case ButtonSettingType.ResetControls:
                case ButtonSettingType.ResetGameplay:
                case ButtonSettingType.ResetProfile:
                    return SettingsManager.Instance != null;

                case ButtonSettingType.CalibrateVR:
                case ButtonSettingType.TestHaptics:
                    return UnityEngine.XR.XRSettings.enabled;

                case ButtonSettingType.TestAudio:
                    return FindObjectOfType<AudioSource>() != null;

                case ButtonSettingType.ImportSettings:
                case ButtonSettingType.ExportSettings:
                    return SettingsManager.Instance != null;

                default:
                    return true;
            }
        }

        private void UpdateIcon()
        {
            if (iconImage == null) return;

            // 根据按钮类型设置图标
            // 这里可以设置不同的图标精灵
        }

        private void UpdateButtonText()
        {
            if (titleText == null) return;

            // 更新按钮文本（如果需要动态文本）
            string buttonText = GetButtonText();
            titleText.text = buttonText;
        }

        private string GetButtonText()
        {
            // 根据按钮类型返回对应文本
            switch (buttonType)
            {
                case ButtonSettingType.ResetSettings: return "重置所有设置";
                case ButtonSettingType.ResetAudio: return "重置音频设置";
                case ButtonSettingType.ResetVideo: return "重置视频设置";
                case ButtonSettingType.ResetControls: return "重置控制设置";
                case ButtonSettingType.ResetGameplay: return "重置游戏设置";
                case ButtonSettingType.ResetProfile: return "重置用户资料";
                case ButtonSettingType.ImportSettings: return "导入设置";
                case ButtonSettingType.ExportSettings: return "导出设置";
                case ButtonSettingType.CalibrateVR: return "VR校准";
                case ButtonSettingType.TestAudio: return "音频测试";
                case ButtonSettingType.TestVideo: return "视频测试";
                case ButtonSettingType.TestHaptics: return "触觉测试";
                case ButtonSettingType.OpenManual: return "打开手册";
                case ButtonSettingType.ContactSupport: return "联系支持";
                case ButtonSettingType.CheckUpdates: return "检查更新";
                case ButtonSettingType.ClearCache: return "清除缓存";
                case ButtonSettingType.RestoreDefaults: return "恢复默认";
                case ButtonSettingType.SaveProfile: return "保存配置";
                case ButtonSettingType.LoadProfile: return "加载配置";
                case ButtonSettingType.DeleteProfile: return "删除配置";
                default: return "按钮";
            }
        }

        #endregion

        #region 按钮点击处理

        private void OnButtonClick()
        {
            if (isProcessing) return;

            // 播放点击音效
            PlayClickSound();

            // 播放触觉反馈
            PlayClickHaptic();

            // 执行点击动画
            if (enableAnimation)
            {
                StartCoroutine(PlayClickAnimation());
            }

            // 检查是否需要确认
            if (requireConfirmation)
            {
                ShowConfirmationDialog();
            }
            else
            {
                ExecuteButtonAction();
            }
        }

        private void ExecuteButtonAction()
        {
            isProcessing = true;
            UpdateButtonState();

            try
            {
                // 执行对应的按钮操作
                switch (buttonType)
                {
                    case ButtonSettingType.ResetSettings:
                        ResetAllSettings();
                        break;
                    case ButtonSettingType.ResetAudio:
                        ResetAudioSettings();
                        break;
                    case ButtonSettingType.ResetVideo:
                        ResetVideoSettings();
                        break;
                    case ButtonSettingType.ResetControls:
                        ResetControlSettings();
                        break;
                    case ButtonSettingType.ResetGameplay:
                        ResetGameplaySettings();
                        break;
                    case ButtonSettingType.ResetProfile:
                        ResetUserProfile();
                        break;
                    case ButtonSettingType.ImportSettings:
                        ImportSettings();
                        break;
                    case ButtonSettingType.ExportSettings:
                        ExportSettings();
                        break;
                    case ButtonSettingType.CalibrateVR:
                        CalibrateVR();
                        break;
                    case ButtonSettingType.TestAudio:
                        TestAudio();
                        break;
                    case ButtonSettingType.TestVideo:
                        TestVideo();
                        break;
                    case ButtonSettingType.TestHaptics:
                        TestHaptics();
                        break;
                    case ButtonSettingType.OpenManual:
                        OpenManual();
                        break;
                    case ButtonSettingType.ContactSupport:
                        ContactSupport();
                        break;
                    case ButtonSettingType.CheckUpdates:
                        CheckUpdates();
                        break;
                    case ButtonSettingType.ClearCache:
                        ClearCache();
                        break;
                    default:
                        Debug.LogWarning($"Button action not implemented: {buttonType}");
                        break;
                }

                // 触发事件
                OnButtonClicked?.Invoke(buttonType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing button action {buttonType}: {e.Message}");
            }
            finally
            {
                isProcessing = false;
                UpdateButtonState();
            }
        }

        #endregion

        #region 按钮操作实现

        private void ResetAllSettings()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetAllSettings();
                Debug.Log("All settings reset to defaults");
            }
        }

        private void ResetAudioSettings()
        {
            if (SettingsManager.Instance != null)
            {
                var defaultSettings = new PongHub.UI.Settings.Core.AudioSettings();
                SettingsManager.Instance.SaveAudioSettings(defaultSettings);
                Debug.Log("Audio settings reset to defaults");
            }
        }

        private void ResetVideoSettings()
        {
            if (SettingsManager.Instance != null)
            {
                var defaultSettings = new VideoSettings();
                SettingsManager.Instance.SaveVideoSettings(defaultSettings);
                Debug.Log("Video settings reset to defaults");
            }
        }

        private void ResetControlSettings()
        {
            if (SettingsManager.Instance != null)
            {
                var defaultSettings = new ControlSettings();
                SettingsManager.Instance.SaveControlSettings(defaultSettings);
                Debug.Log("Control settings reset to defaults");
            }
        }

        private void ResetGameplaySettings()
        {
            if (SettingsManager.Instance != null)
            {
                var defaultSettings = new GameplaySettings();
                SettingsManager.Instance.SaveGameplaySettings(defaultSettings);
                Debug.Log("Gameplay settings reset to defaults");
            }
        }

        private void ResetUserProfile()
        {
            if (SettingsManager.Instance != null)
            {
                var defaultProfile = new UserProfile();
                SettingsManager.Instance.SaveUserProfile(defaultProfile);
                Debug.Log("User profile reset to defaults");
            }
        }

        private void ImportSettings()
        {
            Debug.Log("Import settings functionality - to be implemented");
            // 实现设置导入功能
        }

        private void ExportSettings()
        {
            Debug.Log("Export settings functionality - to be implemented");
            // 实现设置导出功能
        }

        private void CalibrateVR()
        {
            // 触发VR校准
            var vrIntegration = FindObjectOfType<VRSettingsIntegration>();
            if (vrIntegration != null)
            {
                vrIntegration.TriggerCalibration();
                Debug.Log("VR calibration started");
            }
        }

        private void TestAudio()
        {
            // 播放音频测试
            var audioIntegration = FindObjectOfType<AudioSystemIntegration>();
            if (audioIntegration != null)
            {
                audioIntegration.TestAudioSettings();
                Debug.Log("Audio test played");
            }
        }

        private void TestVideo()
        {
            // 显示视频测试图像
            Debug.Log("Video test - to be implemented");
        }

        private void TestHaptics()
        {
            // 播放触觉反馈测试
            var vrIntegration = FindObjectOfType<VRSettingsIntegration>();
            if (vrIntegration != null)
            {
                vrIntegration.PlayHaptic(HandPreference.Ambidextrous, 1.0f, 0.5f);
                Debug.Log("Haptic test played");
            }
        }

        private void OpenManual()
        {
            // 打开用户手册
            Application.OpenURL("https://example.com/manual");
            Debug.Log("Manual opened");
        }

        private void ContactSupport()
        {
            // 打开支持页面
            Application.OpenURL("https://example.com/support");
            Debug.Log("Support page opened");
        }

        private void CheckUpdates()
        {
            // 检查应用更新
            Debug.Log("Checking for updates...");
        }

        private void ClearCache()
        {
            // 清除应用缓存
            Debug.Log("Cache cleared");
        }

        #endregion

        #region 确认对话框

        private void ShowConfirmationDialog()
        {
            if (confirmationDialogPrefab != null)
            {
                var dialog = Instantiate(confirmationDialogPrefab);
                var dialogComponent = dialog.GetComponent<ConfirmationDialog>();
                if (dialogComponent != null)
                {
                    dialogComponent.SetMessage(confirmationMessage);
                    dialogComponent.OnConfirmed += ExecuteButtonAction;
                }
            }
            else
            {
                // 简单确认（暂时使用Debug输出）
                Debug.Log($"Confirmation: {confirmationMessage}");
                ExecuteButtonAction();
            }
        }

        #endregion

        #region 视觉和音频反馈

        private void PlayClickSound()
        {
            // 播放按钮点击音效
            if (enableHapticFeedback)
            {
                // 通过音频管理器播放点击音效
                Debug.Log("Button click sound played");
            }
        }

        private void PlayClickHaptic()
        {
            // 播放触觉反馈
            if (enableHapticFeedback && hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
            }
        }

        private System.Collections.IEnumerator PlayClickAnimation()
        {
            if (!enableAnimation) yield break;

            // 缩放动画
            Vector3 targetScale = originalScale * 0.95f;
            float elapsed = 0f;

            // 缩小
            while (elapsed < animationDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (animationDuration / 2f);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
                yield return null;
            }

            // 恢复
            elapsed = 0f;
            while (elapsed < animationDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (animationDuration / 2f);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置按钮可交互状态
        /// </summary>
        /// <param name="interactable">是否可交互</param>
        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        /// <summary>
        /// 手动触发按钮点击
        /// </summary>
        public void TriggerClick()
        {
            OnButtonClick();
        }

        /// <summary>
        /// 设置确认消息
        /// </summary>
        /// <param name="message">确认消息</param>
        public void SetConfirmationMessage(string message)
        {
            confirmationMessage = message;
        }

        #endregion
    }

    /// <summary>
    /// 确认对话框组件（简化版）
    /// </summary>
    public class ConfirmationDialog : MonoBehaviour
    {
        public event Action OnConfirmed;
        public event Action OnCancelled;

        public void SetMessage(string message)
        {
            // 设置对话框消息
            Debug.Log($"Confirmation dialog: {message}");
        }

        public void Confirm()
        {
            OnConfirmed?.Invoke();
            Destroy(gameObject);
        }

        public void Cancel()
        {
            OnCancelled?.Invoke();
            Destroy(gameObject);
        }
    }
}