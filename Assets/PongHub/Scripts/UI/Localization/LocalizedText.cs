using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace PongHub.UI.Localization
{
    /// <summary>
    /// 本地化文本组件
    /// 用于在UI中显示本地化文本，自动响应语言变更
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("本地化设置")]
        [Tooltip("本地化键，用于查找对应的本地化文本")]
        [SerializeField] private string m_localizationKey;

        [Tooltip("是否在语言变更时自动更新字体")]
        [SerializeField] private bool m_autoUpdateFont = true;

        [Header("文本格式化")]
        [Tooltip("是否启用文本格式化")]
        [SerializeField] private bool m_enableFormatting = false;

        [Tooltip("格式化参数，用于替换文本中的占位符")]
        [SerializeField] private string[] m_formatArgs;

        [Header("字体大小调整")]
        [Tooltip("是否自动调整字体大小以适应文本长度")]
        [SerializeField] private bool m_autoResizeFont = false;

        [Tooltip("最小字体大小")]
        [SerializeField] private float m_minFontSize = 10f;

        [Tooltip("最大字体大小")]
        [SerializeField] private float m_maxFontSize = 40f;

        // 文本组件引用
        private TextMeshProUGUI m_textComponent;

        // 原始字体大小
        private float m_originalFontSize;

        // 是否已初始化
        private bool m_initialized = false;

        #region Unity生命周期

        private void Awake()
        {
            // 获取文本组件
            m_textComponent = GetComponent<TextMeshProUGUI>();

            // 保存原始字体大小
            if (m_textComponent != null)
            {
                m_originalFontSize = m_textComponent.fontSize;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // 注册语言变更事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

                // 如果已经初始化，则更新文本
                if (m_initialized)
                {
                    UpdateLocalizedText();
                }
            }
        }

        private void OnDisable()
        {
            // 取消注册语言变更事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置本地化键
        /// </summary>
        /// <param name="key">本地化键</param>
        public void SetLocalizationKey(string key)
        {
            if (m_localizationKey != key)
            {
                m_localizationKey = key;
                UpdateLocalizedText();
            }
        }

        /// <summary>
        /// 设置格式化参数
        /// </summary>
        /// <param name="args">格式化参数</param>
        public void SetFormatArgs(params string[] args)
        {
            m_formatArgs = args;
            m_enableFormatting = true;
            UpdateLocalizedText();
        }

        /// <summary>
        /// 更新本地化文本
        /// </summary>
        public void UpdateLocalizedText()
        {
            if (string.IsNullOrEmpty(m_localizationKey) || m_textComponent == null)
            {
                return;
            }

            if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsInitialized)
            {
                // 如果本地化管理器未初始化，则使用键作为文本
                m_textComponent.text = m_localizationKey;
                return;
            }

            // 获取本地化文本
            string localizedText;
            if (m_enableFormatting && m_formatArgs != null && m_formatArgs.Length > 0)
            {
                // 将字符串数组转换为对象数组
                object[] formatArgs = new object[m_formatArgs.Length];
                for (int i = 0; i < m_formatArgs.Length; i++)
                {
                    formatArgs[i] = m_formatArgs[i];
                }

                localizedText = LocalizationManager.Instance.GetLocalizedText(m_localizationKey, formatArgs);
            }
            else
            {
                localizedText = LocalizationManager.Instance.GetLocalizedText(m_localizationKey);
            }

            // 设置文本
            m_textComponent.text = localizedText;

            // 更新字体
            if (m_autoUpdateFont)
            {
                UpdateFont();
            }

            // 调整字体大小
            if (m_autoResizeFont)
            {
                StartCoroutine(AdjustFontSize());
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            if (m_initialized)
            {
                return;
            }

            // 如果本地化管理器已初始化，则立即更新文本
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsInitialized)
            {
                UpdateLocalizedText();
                m_initialized = true;
            }
            else if (LocalizationManager.Instance != null)
            {
                // 否则，等待本地化管理器初始化
                LocalizationManager.Instance.OnInitialized += OnLocalizationManagerInitialized;
            }
        }

        /// <summary>
        /// 本地化管理器初始化完成回调
        /// </summary>
        private void OnLocalizationManagerInitialized()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnInitialized -= OnLocalizationManagerInitialized;
            }

            UpdateLocalizedText();
            m_initialized = true;
        }

        /// <summary>
        /// 语言变更回调
        /// </summary>
        private void OnLanguageChanged(string languageCode)
        {
            UpdateLocalizedText();
        }

        /// <summary>
        /// 更新字体
        /// </summary>
        private void UpdateFont()
        {
            if (LocalizationManager.Instance == null || m_textComponent == null)
            {
                return;
            }

            Font font = LocalizationManager.Instance.GetLanguageFont(LocalizationManager.Instance.CurrentLanguage);
            if (font != null && m_textComponent.font != null)
            {
                // 注意：TextMeshPro使用TMP_FontAsset而不是Font
                // 这里需要根据项目实际情况调整
                // 如果使用TMP，可能需要在LocalizationManager中管理TMP_FontAsset
                Debug.LogWarning("LocalizedText: 自动更新字体功能需要根据项目实际情况调整");
            }
        }

        /// <summary>
        /// 调整字体大小
        /// </summary>
        private IEnumerator AdjustFontSize()
        {
            // 等待一帧，确保文本已经更新
            yield return null;

            if (m_textComponent == null)
            {
                yield break;
            }

            // 获取文本框的宽度
            float textWidth = m_textComponent.rectTransform.rect.width;

            // 如果文本宽度为0，则可能还未布局完成
            if (textWidth <= 0)
            {
                yield return null;
                textWidth = m_textComponent.rectTransform.rect.width;

                // 如果仍然为0，则使用默认字体大小
                if (textWidth <= 0)
                {
                    m_textComponent.fontSize = m_originalFontSize;
                    yield break;
                }
            }

            // 重置字体大小
            m_textComponent.fontSize = m_originalFontSize;
            m_textComponent.enableAutoSizing = false;

            // 计算文本的首选宽度
            m_textComponent.ForceMeshUpdate();
            float preferredWidth = m_textComponent.preferredWidth;

            // 如果首选宽度超过文本框宽度，则缩小字体
            if (preferredWidth > textWidth)
            {
                float ratio = textWidth / preferredWidth;
                float newSize = Mathf.Max(m_minFontSize, m_originalFontSize * ratio);
                m_textComponent.fontSize = newSize;
            }
            // 如果首选宽度远小于文本框宽度，则放大字体
            else if (preferredWidth < textWidth * 0.7f)
            {
                float ratio = textWidth / preferredWidth;
                float newSize = Mathf.Min(m_maxFontSize, m_originalFontSize * ratio);
                m_textComponent.fontSize = newSize;
            }

            // 强制更新网格
            m_textComponent.ForceMeshUpdate();
        }

        #endregion
    }
}