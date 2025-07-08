using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using PongHub.UI.ModeSelection;
using PongHub.UI.Settings.Core;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 设置组件基类
    /// Base class for all setting UI components
    /// </summary>
    public abstract class SettingComponentBase : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField]
        [Tooltip("设置标题文本")]
        protected TextMeshProUGUI titleText;

        [SerializeField]
        [Tooltip("设置描述文本")]
        protected TextMeshProUGUI descriptionText;

        [SerializeField]
        [Tooltip("本地化键名")]
        protected string localizationKey;

        [SerializeField]
        [Tooltip("是否启用触觉反馈")]
        protected bool enableHapticFeedback = true;

        [Header("视觉反馈")]
        [SerializeField]
        [Tooltip("高亮图像")]
        protected Image highlightImage;

        [SerializeField]
        [Tooltip("高亮颜色")]
        protected Color highlightColor = Color.blue;

        [SerializeField]
        [Tooltip("默认颜色")]
        protected Color defaultColor = Color.white;

        // 组件引用
        protected SettingsManager settingsManager;
        protected VRHapticFeedback hapticFeedback;

        // 事件
        public event Action<object> OnValueChanged;
        public event Action OnInteractionStart;
        public event Action OnInteractionEnd;

        // 内部状态
        protected bool isInitialized = false;
        protected bool isInteracting = false;
        protected object currentValue;

        #region Unity 生命周期

        protected virtual void Awake()
        {
            FindComponents();
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            if (isInitialized)
            {
                RefreshValue();
                UpdateUI();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 查找组件引用
        /// </summary>
        protected virtual void FindComponents()
        {
            // 查找设置管理器
            settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                settingsManager = FindObjectOfType<SettingsManager>();
            }

            // 查找触觉反馈组件
            if (enableHapticFeedback)
            {
                hapticFeedback = FindObjectOfType<VRHapticFeedback>();
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        protected virtual void Initialize()
        {
            if (isInitialized) return;

            SetupUI();
            RegisterEvents();
            RefreshValue();
            UpdateUI();

            isInitialized = true;
        }

        /// <summary>
        /// 设置UI组件
        /// </summary>
        protected abstract void SetupUI();

        /// <summary>
        /// 注册事件监听
        /// </summary>
        protected virtual void RegisterEvents()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnSettingsChanged += OnSettingsChanged;
            }
        }

        #endregion

        #region 值管理

        /// <summary>
        /// 刷新当前值
        /// </summary>
        protected abstract void RefreshValue();

        /// <summary>
        /// 应用新值
        /// </summary>
        /// <param name="newValue">新值</param>
        protected abstract void ApplyValue(object newValue);

        /// <summary>
        /// 验证值的有效性
        /// </summary>
        /// <param name="value">要验证的值</param>
        /// <returns>是否有效</returns>
        protected virtual bool ValidateValue(object value)
        {
            return value != null;
        }

        /// <summary>
        /// 设置值（外部调用）
        /// </summary>
        /// <param name="value">新值</param>
        public virtual void SetValue(object value)
        {
            if (!ValidateValue(value) || !isInitialized)
                return;

            if (!Equals(currentValue, value))
            {
                currentValue = value;
                ApplyValue(value);
                UpdateUI();
                OnValueChanged?.Invoke(value);

                // 触觉反馈
                if (enableHapticFeedback && hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Light);
                }
            }
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        /// <returns>当前值</returns>
        public virtual object GetValue()
        {
            return currentValue;
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 更新UI显示
        /// </summary>
        protected abstract void UpdateUI();

        /// <summary>
        /// 更新本地化文本
        /// </summary>
        protected virtual void UpdateLocalizedText()
        {
            if (string.IsNullOrEmpty(localizationKey))
                return;

            // TODO: 集成本地化系统
            // var localizationManager = FindObjectOfType<LocalizationManager>();
            // if (localizationManager != null && titleText != null)
            // {
            //     titleText.text = localizationManager.GetLocalizedString(localizationKey + "_title");
            // }
            // if (localizationManager != null && descriptionText != null)
            // {
            //     descriptionText.text = localizationManager.GetLocalizedString(localizationKey + "_desc");
            // }
        }

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        /// <param name="highlighted">是否高亮</param>
        protected virtual void SetHighlighted(bool highlighted)
        {
            if (highlightImage != null)
            {
                highlightImage.color = highlighted ? highlightColor : defaultColor;
            }
        }

        #endregion

        #region 交互处理

        /// <summary>
        /// 开始交互
        /// </summary>
        protected virtual void StartInteraction()
        {
            if (isInteracting) return;

            isInteracting = true;
            SetHighlighted(true);
            OnInteractionStart?.Invoke();

            // 触觉反馈
            if (enableHapticFeedback && hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeHover);
            }
        }

        /// <summary>
        /// 结束交互
        /// </summary>
        protected virtual void EndInteraction()
        {
            if (!isInteracting) return;

            isInteracting = false;
            SetHighlighted(false);
            OnInteractionEnd?.Invoke();
        }

        /// <summary>
        /// 处理点击事件
        /// </summary>
        protected virtual void OnClick()
        {
            // 触觉反馈
            if (enableHapticFeedback && hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeSelect);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 设置变更事件处理
        /// </summary>
        /// <param name="settings">新的设置</param>
        protected virtual void OnSettingsChanged(GameSettings settings)
        {
            RefreshValue();
            UpdateUI();
        }

        #endregion

        #region 清理

        protected virtual void OnDestroy()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnSettingsChanged -= OnSettingsChanged;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 设置组件的启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public virtual void SetEnabled(bool enabled)
        {
            gameObject.SetActive(enabled);
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public abstract void ResetToDefault();

        /// <summary>
        /// 获取设置的显示名称
        /// </summary>
        /// <returns>显示名称</returns>
        public virtual string GetDisplayName()
        {
            return titleText?.text ?? localizationKey ?? gameObject.name;
        }

        /// <summary>
        /// 获取设置的描述
        /// </summary>
        /// <returns>描述文本</returns>
        public virtual string GetDescription()
        {
            return descriptionText?.text ?? "";
        }

        #endregion
    }
}