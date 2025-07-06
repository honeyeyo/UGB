using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR输入框组件
    /// 实现VR环境中的文本输入功能
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VRInputField : VRUIComponent
    {
        [Header("输入设置")]
        [SerializeField]
        [Tooltip("Text / 文本 - Current text in the input field")]
        private string m_text = "";

        [SerializeField]
        [Tooltip("Placeholder / 占位符 - Text displayed when input field is empty")]
        private string m_placeholder = "Enter text...";

        [SerializeField]
        [Tooltip("Character Limit / 字符限制 - Maximum number of characters (0 for unlimited)")]
        private int m_characterLimit = 0;

        [SerializeField]
        [Tooltip("Content Type / 内容类型 - Type of content in the input field")]
        private TMP_InputField.ContentType m_contentType = TMP_InputField.ContentType.Standard;

        [SerializeField]
        [Tooltip("Line Type / 行类型 - How to handle line breaks")]
        private TMP_InputField.LineType m_lineType = TMP_InputField.LineType.SingleLine;

        [SerializeField]
        [Tooltip("Input Type / 输入类型 - Type of input expected")]
        private TMP_InputField.InputType m_inputType = TMP_InputField.InputType.Standard;

        [SerializeField]
        [Tooltip("Keyboard Type / 键盘类型 - Type of virtual keyboard to display")]
        private TouchScreenKeyboardType m_keyboardType = TouchScreenKeyboardType.Default;

        [SerializeField]
        [Tooltip("Validation / 验证 - Validation to apply to input")]
        private TMP_InputField.CharacterValidation m_validation = TMP_InputField.CharacterValidation.None;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Input field background image")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Text Component / 文本组件 - Text component for input field")]
        private TextMeshProUGUI m_textComponent;

        [SerializeField]
        [Tooltip("Placeholder Component / 占位符组件 - Text component for placeholder")]
        private TextMeshProUGUI m_placeholderComponent;

        [SerializeField]
        [Tooltip("Caret / 光标 - Visual caret for text editing")]
        private RectTransform m_caret;

        [SerializeField]
        [Tooltip("Selection Highlight / 选择高亮 - Visual highlight for selected text")]
        private RectTransform m_selectionHighlight;

        [SerializeField]
        [Tooltip("Caret Blink Rate / 光标闪烁速率 - Rate at which the caret blinks (blinks per second)")]
        private float m_caretBlinkRate = 0.85f;

        [SerializeField]
        [Tooltip("Caret Width / 光标宽度 - Width of the caret in pixels")]
        private float m_caretWidth = 2f;

        [SerializeField]
        [Tooltip("Custom Caret Color / 自定义光标颜色 - Whether to use a custom color for the caret")]
        private bool m_customCaretColor = false;

        [SerializeField]
        [Tooltip("Caret Color / 光标颜色 - Color of the caret")]
        private Color m_caretColor = Color.white;

        [SerializeField]
        [Tooltip("Selection Color / 选择颜色 - Color of the selection highlight")]
        private Color m_selectionColor = new Color(0.227f, 0.525f, 1f, 0.5f);

        [SerializeField]
        [Tooltip("Padding / 内边距 - Padding around the text")]
        private RectOffset m_padding = new RectOffset(10, 10, 8, 8);

        // 事件
        [Header("事件")]
        public UnityEvent<string> OnValueChanged = new UnityEvent<string>();
        public UnityEvent<string> OnEndEdit = new UnityEvent<string>();
        public UnityEvent<string> OnSubmit = new UnityEvent<string>();
        public UnityEvent<string> OnSelect = new UnityEvent<string>();
        public UnityEvent<string> OnDeselect = new UnityEvent<string>();

        // 内部TMP输入框组件
        private TMP_InputField m_inputField;

        // 交互状态
#pragma warning disable 0414
        private bool m_isFocused = false; // 保留用于将来实现焦点相关功能
#pragma warning restore 0414
        private bool m_isSelected = false;

        // 缓存的RectTransform
        private RectTransform m_rectTransform;

        // 光标闪烁相关
        private float m_caretBlinkTime = 0f;
        private bool m_caretVisible = true;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取RectTransform
            m_rectTransform = GetComponent<RectTransform>();

            // 确保有碰撞器
            EnsureCollider();

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

        protected virtual void Update()
        {
            // 处理光标闪烁
            if (m_isSelected && m_caret != null)
            {
                m_caretBlinkTime += Time.deltaTime;
                if (m_caretBlinkTime >= 1f / m_caretBlinkRate)
                {
                    m_caretBlinkTime = 0f;
                    m_caretVisible = !m_caretVisible;
                    m_caret.gameObject.SetActive(m_caretVisible);
                }
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置输入框文本
        /// </summary>
        public void SetText(string text)
        {
            m_text = text;
            if (m_inputField != null)
            {
                m_inputField.text = m_text;
            }
            else if (m_textComponent != null)
            {
                m_textComponent.text = m_text;
                UpdatePlaceholderVisibility();
            }
        }

        /// <summary>
        /// 获取输入框文本
        /// </summary>
        public string GetText()
        {
            if (m_inputField != null)
            {
                return m_inputField.text;
            }
            return m_text;
        }

        /// <summary>
        /// 设置占位符文本
        /// </summary>
        public void SetPlaceholder(string placeholder)
        {
            m_placeholder = placeholder;
            if (m_inputField != null && m_inputField.placeholder is TextMeshProUGUI placeholderText)
            {
                placeholderText.text = m_placeholder;
            }
            else if (m_placeholderComponent != null)
            {
                m_placeholderComponent.text = m_placeholder;
            }
        }

        /// <summary>
        /// 设置字符限制
        /// </summary>
        public void SetCharacterLimit(int limit)
        {
            m_characterLimit = limit;
            if (m_inputField != null)
            {
                m_inputField.characterLimit = m_characterLimit;
            }
        }

        /// <summary>
        /// 设置内容类型
        /// </summary>
        public void SetContentType(TMP_InputField.ContentType contentType)
        {
            m_contentType = contentType;
            if (m_inputField != null)
            {
                m_inputField.contentType = m_contentType;
            }
        }

        /// <summary>
        /// 设置行类型
        /// </summary>
        public void SetLineType(TMP_InputField.LineType lineType)
        {
            m_lineType = lineType;
            if (m_inputField != null)
            {
                m_inputField.lineType = m_lineType;
            }
        }

        /// <summary>
        /// 全选文本
        /// </summary>
        public void SelectAll()
        {
            if (m_inputField != null)
            {
                // 由于TMP_InputField.SelectAll()是protected的，我们需要通过其他方式实现全选
                m_inputField.stringPosition = 0;
                // 使用caretPosition和text.Length代替stringSelectPosition
                m_inputField.caretPosition = m_inputField.text.Length;
            }
        }

        /// <summary>
        /// 设置输入框焦点
        /// </summary>
        public void ActivateInputField()
        {
            if (m_inputField != null)
            {
                m_inputField.ActivateInputField();
            }
            else
            {
                SetSelected(true);
            }
        }

        /// <summary>
        /// 取消输入框焦点
        /// </summary>
        public void DeactivateInputField()
        {
            if (m_inputField != null)
            {
                m_inputField.DeactivateInputField();
            }
            else
            {
                SetSelected(false);
            }
        }

        #endregion

        #region VRUIComponent实现

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
            Color placeholderColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = state == InteractionState.Selected ?
                    m_theme.GetStateColor(InteractionState.Selected) :
                    m_theme.GetStateColor(state);

                textColor = m_theme.GetTextColor(state);
                placeholderColor = new Color(textColor.r, textColor.g, textColor.b, 0.5f);

                if (m_customCaretColor)
                {
                    m_caretColor = m_theme.accentColor;
                }
                else
                {
                    m_caretColor = textColor;
                }

                m_selectionColor = new Color(m_theme.accentColor.r, m_theme.accentColor.g, m_theme.accentColor.b, 0.5f);
            }
            else
            {
                // 默认颜色
                switch (state)
                {
                    case InteractionState.Normal:
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                        textColor = Color.white;
                        placeholderColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                    case InteractionState.Highlighted:
                        backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                        textColor = Color.white;
                        placeholderColor = new Color(1f, 1f, 1f, 0.6f);
                        break;
                    case InteractionState.Selected:
                        backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        textColor = Color.white;
                        placeholderColor = new Color(1f, 1f, 1f, 0.6f);
                        break;
                    case InteractionState.Disabled:
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                        textColor = new Color(1f, 1f, 1f, 0.5f);
                        placeholderColor = new Color(1f, 1f, 1f, 0.3f);
                        break;
                    default:
                        backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                        textColor = Color.white;
                        placeholderColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                }
            }

            // 应用颜色
            if (m_background != null)
            {
                m_background.color = backgroundColor;
            }

            if (m_inputField != null)
            {
                // 如果使用TMP_InputField，应用颜色到它
                m_inputField.selectionColor = m_selectionColor;
                if (m_customCaretColor)
                {
                    m_inputField.caretColor = m_caretColor;
                }
            }
            else
            {
                // 否则直接应用到文本组件
                if (m_textComponent != null)
                {
                    m_textComponent.color = textColor;
                }

                if (m_placeholderComponent != null)
                {
                    m_placeholderComponent.color = placeholderColor;
                }

                if (m_caret != null && m_caret.GetComponent<Image>() != null)
                {
                    m_caret.GetComponent<Image>().color = m_caretColor;
                }

                if (m_selectionHighlight != null && m_selectionHighlight.GetComponent<Image>() != null)
                {
                    m_selectionHighlight.GetComponent<Image>().color = m_selectionColor;
                }
            }

            // 更新光标和选择高亮的可见性
            UpdateCaretAndSelectionVisibility();
        }

        /// <summary>
        /// 指针点击
        /// </summary>
        public override void OnPointerClick()
        {
            base.OnPointerClick();

            if (!m_interactable)
                return;

            // 激活输入框
            ActivateInputField();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保有碰撞器
        /// </summary>
        private void EnsureCollider()
        {
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            // 设置碰撞器大小
            if (m_rectTransform != null)
            {
                Vector2 size = m_rectTransform.sizeDelta;
                collider.size = new Vector3(size.x, size.y, 1f);
                collider.center = Vector3.zero;
            }
            else
            {
                collider.size = new Vector3(200f, 40f, 0.1f);
                collider.center = Vector3.zero;
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有RectTransform，添加一个
            if (m_rectTransform == null)
            {
                m_rectTransform = gameObject.AddComponent<RectTransform>();
                m_rectTransform.sizeDelta = new Vector2(200, 40);
            }

            // 检查是否已经有TMP_InputField组件
            m_inputField = GetComponent<TMP_InputField>();

            if (m_inputField == null)
            {
                // 如果没有背景组件，创建一个
                if (m_background == null)
                {
                    GameObject bgObj = new GameObject("Background");
                    bgObj.transform.SetParent(transform);

                    RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;

                    m_background = bgObj.AddComponent<Image>();
                    m_background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                }

                // 创建文本组件
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
                    m_textComponent.color = Color.white;
                    m_textComponent.alignment = TextAlignmentOptions.Left;
                    m_textComponent.enableWordWrapping = m_lineType != TMP_InputField.LineType.SingleLine;
                }

                // 创建占位符组件
                if (m_placeholderComponent == null)
                {
                    GameObject placeholderObj = new GameObject("Placeholder");
                    placeholderObj.transform.SetParent(transform);

                    RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
                    placeholderRect.anchorMin = Vector2.zero;
                    placeholderRect.anchorMax = Vector2.one;
                    placeholderRect.offsetMin = new Vector2(m_padding.left, m_padding.bottom);
                    placeholderRect.offsetMax = new Vector2(-m_padding.right, -m_padding.top);

                    m_placeholderComponent = placeholderObj.AddComponent<TextMeshProUGUI>();
                    m_placeholderComponent.text = m_placeholder;
                    m_placeholderComponent.color = new Color(1f, 1f, 1f, 0.5f);
                    m_placeholderComponent.alignment = TextAlignmentOptions.Left;
                    m_placeholderComponent.enableWordWrapping = m_lineType != TMP_InputField.LineType.SingleLine;
                }

                // 创建光标
                if (m_caret == null)
                {
                    GameObject caretObj = new GameObject("Caret");
                    caretObj.transform.SetParent(transform);

                    m_caret = caretObj.AddComponent<RectTransform>();
                    m_caret.sizeDelta = new Vector2(m_caretWidth, m_textComponent.fontSize);
                    m_caret.anchoredPosition = new Vector2(m_padding.left, 0);

                    Image caretImage = caretObj.AddComponent<Image>();
                    caretImage.color = m_caretColor;

                    // 默认隐藏光标
                    caretObj.SetActive(false);
                }

                // 创建选择高亮
                if (m_selectionHighlight == null)
                {
                    GameObject selectionObj = new GameObject("SelectionHighlight");
                    selectionObj.transform.SetParent(transform);

                    m_selectionHighlight = selectionObj.AddComponent<RectTransform>();
                    m_selectionHighlight.sizeDelta = new Vector2(0, m_textComponent.fontSize);
                    m_selectionHighlight.anchoredPosition = new Vector2(m_padding.left, 0);

                    Image selectionImage = selectionObj.AddComponent<Image>();
                    selectionImage.color = m_selectionColor;

                    // 默认隐藏选择高亮
                    selectionObj.SetActive(false);
                }

                // 创建TMP_InputField组件
                m_inputField = gameObject.AddComponent<TMP_InputField>();
                m_inputField.textComponent = m_textComponent;
                m_inputField.placeholder = m_placeholderComponent;
                m_inputField.text = m_text;
                m_inputField.characterLimit = m_characterLimit;
                m_inputField.contentType = m_contentType;
                m_inputField.lineType = m_lineType;
                m_inputField.inputType = m_inputType;
                m_inputField.keyboardType = m_keyboardType;
                m_inputField.characterValidation = m_validation;
                m_inputField.caretWidth = (int)m_caretWidth;
                m_inputField.customCaretColor = m_customCaretColor;
                m_inputField.caretColor = m_caretColor;
                m_inputField.selectionColor = m_selectionColor;

                // 注册事件
                m_inputField.onValueChanged.AddListener(OnInputValueChanged);
                m_inputField.onEndEdit.AddListener(OnInputEndEdit);
                m_inputField.onSubmit.AddListener(OnInputSubmit);
                m_inputField.onSelect.AddListener(OnInputSelect);
                m_inputField.onDeselect.AddListener(OnInputDeselect);
            }
            else
            {
                // 更新现有TMP_InputField组件
                m_inputField.text = m_text;
                m_inputField.characterLimit = m_characterLimit;
                m_inputField.contentType = m_contentType;
                m_inputField.lineType = m_lineType;
                m_inputField.inputType = m_inputType;
                m_inputField.keyboardType = m_keyboardType;
                m_inputField.characterValidation = m_validation;
                m_inputField.caretWidth = (int)m_caretWidth;
                m_inputField.customCaretColor = m_customCaretColor;
                m_inputField.caretColor = m_caretColor;
                m_inputField.selectionColor = m_selectionColor;

                // 确保事件已注册
                m_inputField.onValueChanged.RemoveListener(OnInputValueChanged);
                m_inputField.onEndEdit.RemoveListener(OnInputEndEdit);
                m_inputField.onSubmit.RemoveListener(OnInputSubmit);
                m_inputField.onSelect.RemoveListener(OnInputSelect);
                m_inputField.onDeselect.RemoveListener(OnInputDeselect);

                m_inputField.onValueChanged.AddListener(OnInputValueChanged);
                m_inputField.onEndEdit.AddListener(OnInputEndEdit);
                m_inputField.onSubmit.AddListener(OnInputSubmit);
                m_inputField.onSelect.AddListener(OnInputSelect);
                m_inputField.onDeselect.AddListener(OnInputDeselect);

                // 获取引用
                if (m_textComponent == null && m_inputField.textComponent != null)
                {
                    m_textComponent = m_inputField.textComponent as TextMeshProUGUI;
                }

                if (m_placeholderComponent == null && m_inputField.placeholder != null)
                {
                    m_placeholderComponent = m_inputField.placeholder as TextMeshProUGUI;
                }
            }

            // 更新占位符可见性
            UpdatePlaceholderVisibility();
        }

        /// <summary>
        /// 更新占位符可见性
        /// </summary>
        private void UpdatePlaceholderVisibility()
        {
            if (m_placeholderComponent != null)
            {
                m_placeholderComponent.gameObject.SetActive(string.IsNullOrEmpty(m_text));
            }
        }

        /// <summary>
        /// 更新光标和选择高亮的可见性
        /// </summary>
        private void UpdateCaretAndSelectionVisibility()
        {
            if (m_inputField == null)
            {
                // 如果没有使用TMP_InputField，手动更新
                if (m_caret != null)
                {
                    m_caret.gameObject.SetActive(m_isSelected && m_caretVisible);
                }

                if (m_selectionHighlight != null)
                {
                    m_selectionHighlight.gameObject.SetActive(false); // 简化版不支持选择
                }
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        private void SetSelected(bool selected)
        {
            if (m_isSelected != selected)
            {
                m_isSelected = selected;
                m_caretBlinkTime = 0f;
                m_caretVisible = true;

                // 更新视觉状态
                UpdateVisualState(m_isSelected ? InteractionState.Selected :
                    (m_isHovered ? InteractionState.Highlighted : InteractionState.Normal));

                // 触发事件
                if (m_isSelected)
                {
                    OnSelect.Invoke(m_text);
                }
                else
                {
                    OnDeselect.Invoke(m_text);
                    OnEndEdit.Invoke(m_text);
                }
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 输入值变化事件处理
        /// </summary>
        private void OnInputValueChanged(string value)
        {
            m_text = value;
            OnValueChanged.Invoke(value);
            UpdatePlaceholderVisibility();
        }

        /// <summary>
        /// 输入结束事件处理
        /// </summary>
        private void OnInputEndEdit(string value)
        {
            m_text = value;
            OnEndEdit.Invoke(value);
        }

        /// <summary>
        /// 输入提交事件处理
        /// </summary>
        private void OnInputSubmit(string value)
        {
            m_text = value;
            OnSubmit.Invoke(value);
        }

        /// <summary>
        /// 输入框选中事件处理
        /// </summary>
        private void OnInputSelect(string value)
        {
            m_isFocused = true;
            m_isSelected = true;
            UpdateVisualState(InteractionState.Selected);
            OnSelect.Invoke(value);
        }

        /// <summary>
        /// 输入框取消选中事件处理
        /// </summary>
        private void OnInputDeselect(string value)
        {
            m_isFocused = false;
            m_isSelected = false;
            UpdateVisualState(m_isHovered ? InteractionState.Highlighted : InteractionState.Normal);
            OnDeselect.Invoke(value);
        }

        #endregion
    }
}