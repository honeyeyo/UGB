using UnityEngine;
using TMPro;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR标签组件
    /// 实现VR环境中的文本标签显示
    /// </summary>
    public class VRLabel : VRUIComponent
    {
        [Header("文本设置")]
        [SerializeField]
        [Tooltip("Text / 文本 - Text displayed on the label")]
        private string m_text = "Label";

        [SerializeField]
        [Tooltip("Font Size / 字体大小 - Size of the label text")]
        private float m_fontSize = 24f;

        [SerializeField]
        [Tooltip("Text Alignment / 文本对齐 - Alignment of the text")]
        private TextAlignmentOptions m_textAlignment = TextAlignmentOptions.Center;

        [SerializeField]
        [Tooltip("Auto Size / 自动调整大小 - Whether to automatically adjust font size to fit")]
        private bool m_autoSize = false;

        [SerializeField]
        [Tooltip("Min Font Size / 最小字体大小 - Minimum font size when auto sizing")]
        private float m_minFontSize = 10f;

        [SerializeField]
        [Tooltip("Max Font Size / 最大字体大小 - Maximum font size when auto sizing")]
        private float m_maxFontSize = 36f;

        [SerializeField]
        [Tooltip("Word Wrap / 自动换行 - Whether to wrap text")]
        private bool m_wordWrap = true;

        [SerializeField]
        [Tooltip("Overflow / 溢出处理 - How to handle text overflow")]
        private TextOverflowModes m_overflow = TextOverflowModes.Truncate;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Use Background / 使用背景 - Whether to display a background")]
        private bool m_useBackground = false;

        [SerializeField]
        [Tooltip("Background / 背景 - Label background image")]
        private UnityEngine.UI.Image m_background;

        [SerializeField]
        [Tooltip("Text Component / 文本组件 - Text component for label")]
        private TextMeshProUGUI m_textComponent;

        [SerializeField]
        [Tooltip("Rich Text / 富文本 - Whether to support rich text formatting")]
        private bool m_richText = true;

        [SerializeField]
        [Tooltip("Padding / 内边距 - Padding around the text")]
        private RectOffset m_padding = new RectOffset(5, 5, 5, 5);

        // 缓存的RectTransform
        private RectTransform m_rectTransform;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取RectTransform
            m_rectTransform = GetComponent<RectTransform>();

            // 初始化组件
            InitializeComponents();

            // 更新视觉状态
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);

            // 注册到UI管理器
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.RegisterComponent(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // 从UI管理器注销
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置标签文本
        /// </summary>
        public void SetText(string text)
        {
            m_text = text;
            if (m_textComponent != null)
            {
                m_textComponent.text = m_text;
            }
        }

        /// <summary>
        /// 获取标签文本
        /// </summary>
        public string GetText()
        {
            return m_text;
        }

        /// <summary>
        /// 设置字体大小
        /// </summary>
        public void SetFontSize(float size)
        {
            m_fontSize = size;
            if (m_textComponent != null)
            {
                m_textComponent.fontSize = m_fontSize;
            }
        }

        /// <summary>
        /// 设置文本对齐方式
        /// </summary>
        public void SetTextAlignment(TextAlignmentOptions alignment)
        {
            m_textAlignment = alignment;
            if (m_textComponent != null)
            {
                m_textComponent.alignment = m_textAlignment;
            }
        }

        /// <summary>
        /// 设置自动调整大小
        /// </summary>
        public void SetAutoSize(bool autoSize)
        {
            m_autoSize = autoSize;
            if (m_textComponent != null)
            {
                m_textComponent.enableAutoSizing = m_autoSize;
                m_textComponent.fontSizeMin = m_minFontSize;
                m_textComponent.fontSizeMax = m_maxFontSize;
            }
        }

        /// <summary>
        /// 设置自动换行
        /// </summary>
        public void SetWordWrap(bool wordWrap)
        {
            m_wordWrap = wordWrap;
            if (m_textComponent != null)
            {
                m_textComponent.enableWordWrapping = m_wordWrap;
            }
        }

        /// <summary>
        /// 设置溢出处理方式
        /// </summary>
        public void SetOverflow(TextOverflowModes overflow)
        {
            m_overflow = overflow;
            if (m_textComponent != null)
            {
                m_textComponent.overflowMode = m_overflow;
            }
        }

        /// <summary>
        /// 设置是否使用背景
        /// </summary>
        public void SetUseBackground(bool useBackground)
        {
            m_useBackground = useBackground;
            if (m_background != null)
            {
                m_background.gameObject.SetActive(m_useBackground);
            }
        }

        /// <summary>
        /// 设置内边距
        /// </summary>
        public void SetPadding(RectOffset padding)
        {
            m_padding = padding;
            UpdatePadding();
        }

        #endregion

        #region VRUIComponent实现

        /// <summary>
        /// 设置组件是否可交互
        /// </summary>
        public override void SetInteractable(bool interactable)
        {
            if (m_interactable == interactable)
                return;

            m_interactable = interactable;
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);
            OnInteractableChanged.Invoke(m_interactable);
        }

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        public override void UpdateVisualState(InteractionState state)
        {
            // 保存当前状态
            m_currentState = state;

            // 根据状态设置视觉效果
            Color backgroundColor;
            Color textColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.backgroundColor;
                textColor = m_theme.GetTextColor(state);
            }
            else
            {
                // 默认颜色
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, m_useBackground ? 0.8f : 0f);

                switch (state)
                {
                    case InteractionState.Normal:
                        textColor = Color.white;
                        break;
                    case InteractionState.Highlighted:
                        textColor = new Color(0.4f, 0.6f, 1f);
                        break;
                    case InteractionState.Pressed:
                        textColor = new Color(0.2f, 0.4f, 0.8f);
                        break;
                    case InteractionState.Selected:
                        textColor = new Color(1f, 0f, 0.431f);
                        break;
                    case InteractionState.Disabled:
                        textColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                    default:
                        textColor = Color.white;
                        break;
                }
            }

            // 应用颜色
            if (m_background != null)
            {
                m_background.color = backgroundColor;
                m_background.gameObject.SetActive(m_useBackground);
            }

            if (m_textComponent != null)
            {
                m_textComponent.color = textColor;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有RectTransform，添加一个
            if (m_rectTransform == null)
            {
                m_rectTransform = gameObject.AddComponent<RectTransform>();
                m_rectTransform.sizeDelta = new Vector2(200, 50);
            }

            // 如果使用背景但没有背景组件，创建一个
            if (m_useBackground && m_background == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                m_background = bgObj.AddComponent<UnityEngine.UI.Image>();
                m_background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // 如果没有文本组件，创建一个
            if (m_textComponent == null)
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(transform);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(m_padding.left, m_padding.bottom);
                textRect.offsetMax = new Vector2(-m_padding.right, -m_padding.top);

                m_textComponent = textObj.AddComponent<TextMeshProUGUI>();
                m_textComponent.text = m_text;
                m_textComponent.fontSize = m_fontSize;
                m_textComponent.alignment = m_textAlignment;
                m_textComponent.enableAutoSizing = m_autoSize;
                m_textComponent.fontSizeMin = m_minFontSize;
                m_textComponent.fontSizeMax = m_maxFontSize;
                m_textComponent.enableWordWrapping = m_wordWrap;
                m_textComponent.overflowMode = m_overflow;
                m_textComponent.richText = m_richText;
                m_textComponent.color = Color.white;
            }
            else
            {
                // 更新现有文本组件
                m_textComponent.text = m_text;
                m_textComponent.fontSize = m_fontSize;
                m_textComponent.alignment = m_textAlignment;
                m_textComponent.enableAutoSizing = m_autoSize;
                m_textComponent.fontSizeMin = m_minFontSize;
                m_textComponent.fontSizeMax = m_maxFontSize;
                m_textComponent.enableWordWrapping = m_wordWrap;
                m_textComponent.overflowMode = m_overflow;
                m_textComponent.richText = m_richText;

                // 更新内边距
                UpdatePadding();
            }
        }

        /// <summary>
        /// 更新内边距
        /// </summary>
        private void UpdatePadding()
        {
            if (m_textComponent != null && m_textComponent.rectTransform != null)
            {
                m_textComponent.rectTransform.offsetMin = new Vector2(m_padding.left, m_padding.bottom);
                m_textComponent.rectTransform.offsetMax = new Vector2(-m_padding.right, -m_padding.top);
            }
        }

        #endregion
    }
}