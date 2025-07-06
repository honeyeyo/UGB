using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR下拉菜单组件
    /// 实现VR环境中的下拉选择功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRDropdown : VRUIComponent
    {
        [System.Serializable]
        public class OptionData
        {
            [Tooltip("Option Text / 选项文本")]
            public string text;

            [Tooltip("Option Image / 选项图标")]
            public Sprite image;

            public OptionData()
            {
                text = string.Empty;
                image = null;
            }

            public OptionData(string text)
            {
                this.text = text;
                this.image = null;
            }

            public OptionData(string text, Sprite image)
            {
                this.text = text;
                this.image = image;
            }
        }

        [System.Serializable]
        public class OptionDataList
        {
            [SerializeField]
            [Tooltip("Options / 选项列表")]
            private List<OptionData> m_Options = new List<OptionData>();

            public List<OptionData> options { get { return m_Options; } set { m_Options = value; } }

            public OptionDataList()
            {
                options = new List<OptionData>();
            }
        }

        [Header("下拉菜单设置")]
        [SerializeField]
        [Tooltip("Options / 选项列表")]
        private OptionDataList m_Options = new OptionDataList();

        [SerializeField]
        [Tooltip("Value / 当前值 - Index of the currently selected option")]
        private int m_Value = 0;

        [SerializeField]
        [Tooltip("Caption Text / 标题文本 - Text component for the dropdown caption")]
        private TextMeshProUGUI m_CaptionText;

        [SerializeField]
        [Tooltip("Caption Image / 标题图标 - Image component for the dropdown caption")]
        private Image m_CaptionImage;

        [SerializeField]
        [Tooltip("Item Text / 项目文本 - Text component template for dropdown items")]
        private TextMeshProUGUI m_ItemText;

        [SerializeField]
        [Tooltip("Item Image / 项目图标 - Image component template for dropdown items")]
        private Image m_ItemImage;

        [SerializeField]
        [Tooltip("Arrow / 箭头 - Arrow indicator for the dropdown")]
        private RectTransform m_Arrow;

        [SerializeField]
        [Tooltip("Template / 模板 - Template for the dropdown panel")]
        private RectTransform m_Template;

        [SerializeField]
        [Tooltip("Viewport / 视口 - Viewport for the dropdown panel")]
        private RectTransform m_Viewport;

        [SerializeField]
        [Tooltip("Content / 内容 - Content for the dropdown panel")]
        private RectTransform m_Content;

        [SerializeField]
        [Tooltip("Show Mask / 显示遮罩 - Whether to use a mask for the dropdown panel")]
        private bool m_ShowMask = true;

        [SerializeField]
        [Tooltip("Max Height / 最大高度 - Maximum height of the dropdown panel")]
        private float m_MaxHeight = 300f;

        [SerializeField]
        [Tooltip("Item Height / 项目高度 - Height of each dropdown item")]
        private float m_ItemHeight = 40f;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Background image for the dropdown")]
        private Image m_Background;

        [SerializeField]
        [Tooltip("Item Background / 项目背景 - Background template for dropdown items")]
        private Image m_ItemBackground;

        [SerializeField]
        [Tooltip("Selected Item Background / 选中项目背景 - Background for the selected item")]
        private Image m_SelectedItemBackground;

        [SerializeField]
        [Tooltip("Item Highlight / 项目高亮 - Highlight for hovered items")]
        private Image m_ItemHighlight;

        [SerializeField]
        [Tooltip("Use Animation / 使用动画 - Whether to animate the dropdown panel")]
        private bool m_UseAnimation = true;

        [SerializeField]
        [Tooltip("Animation Duration / 动画持续时间 - Duration of dropdown animations")]
#pragma warning disable 0414
        private float m_AnimationDuration = 0.2f; // 保留用于将来实现动画效果

        // 事件
        [Header("事件")]
        public UnityEvent<int> OnValueChanged = new UnityEvent<int>();

        // 内部状态
        private bool m_IsExpanded = false;
#pragma warning disable 0414
        private int m_SelectedIndex = -1;
#pragma warning disable 0414
        private int m_HighlightedIndex = -1; // 保留用于将来实现高亮效果
        private RectTransform m_DropdownPanel;
        private List<RectTransform> m_Items = new List<RectTransform>();
        private RectTransform m_rectTransform;
        private Coroutine m_AnimationCoroutine;

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

        protected virtual void Start()
        {
            // 确保下拉面板初始状态为关闭
            if (m_Template != null)
            {
                m_Template.gameObject.SetActive(false);
            }

            // 更新标题显示
            RefreshShownValue();
        }

        protected virtual void OnDestroy()
        {
            // 从UI管理器注销
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.UnregisterComponent(this);
            }
        }

        protected virtual void OnDisable()
        {
            // 确保下拉面板关闭
            if (m_IsExpanded)
            {
                Hide();
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 添加选项
        /// </summary>
        public void AddOption(string text, Sprite image = null)
        {
            m_Options.options.Add(new OptionData(text, image));
            RefreshShownValue();
        }

        /// <summary>
        /// 添加选项列表
        /// </summary>
        public void AddOptions(List<string> options)
        {
            foreach (var option in options)
            {
                m_Options.options.Add(new OptionData(option));
            }
            RefreshShownValue();
        }

        /// <summary>
        /// 添加选项列表（带图标）
        /// </summary>
        public void AddOptions(List<OptionData> options)
        {
            m_Options.options.AddRange(options);
            RefreshShownValue();
        }

        /// <summary>
        /// 清除所有选项
        /// </summary>
        public void ClearOptions()
        {
            m_Options.options.Clear();
            m_Value = 0;
            RefreshShownValue();
        }

        /// <summary>
        /// 获取选项数量
        /// </summary>
        public int GetOptionsCount()
        {
            return m_Options.options.Count;
        }

        /// <summary>
        /// 设置当前值
        /// </summary>
        public void SetValue(int value)
        {
            if (m_Options.options.Count == 0)
                return;

            value = Mathf.Clamp(value, 0, m_Options.options.Count - 1);
            if (m_Value != value)
            {
                m_Value = value;
                RefreshShownValue();
                OnValueChanged.Invoke(m_Value);
            }
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        public int GetValue()
        {
            return m_Value;
        }

        /// <summary>
        /// 获取当前选中的选项文本
        /// </summary>
        public string GetSelectedOptionText()
        {
            if (m_Options.options.Count > 0 && m_Value >= 0 && m_Value < m_Options.options.Count)
            {
                return m_Options.options[m_Value].text;
            }
            return string.Empty;
        }

        /// <summary>
        /// 显示下拉面板
        /// </summary>
        public void Show()
        {
            if (m_IsExpanded || !m_interactable || m_Template == null)
                return;

            // 激活模板
            m_Template.gameObject.SetActive(true);

            // 创建下拉面板
            CreateDropdownList();

            // 设置状态
            m_IsExpanded = true;

            // 更新箭头方向
            UpdateArrowDirection(true);

            // 播放动画
            if (m_UseAnimation)
            {
                PlayExpandAnimation();
            }
        }

        /// <summary>
        /// 隐藏下拉面板
        /// </summary>
        public void Hide()
        {
            if (!m_IsExpanded || m_Template == null)
                return;

            // 播放动画
            if (m_UseAnimation)
            {
                PlayCollapseAnimation();
            }
            else
            {
                // 直接关闭
                m_Template.gameObject.SetActive(false);
                m_IsExpanded = false;
                UpdateArrowDirection(false);
            }
        }

        /// <summary>
        /// 切换下拉面板显示状态
        /// </summary>
        public void Toggle()
        {
            if (m_IsExpanded)
            {
                Hide();
            }
            else
            {
                Show();
            }
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

            // 如果禁用，关闭下拉面板
            if (!m_interactable && m_IsExpanded)
            {
                Hide();
            }
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
            Color arrowColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.GetStateColor(state);
                textColor = m_theme.GetTextColor(state);
                arrowColor = textColor;
            }
            else
            {
                // 默认颜色
                switch (state)
                {
                    case InteractionState.Normal:
                        backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        textColor = Color.white;
                        arrowColor = Color.white;
                        break;
                    case InteractionState.Highlighted:
                        backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                        textColor = Color.white;
                        arrowColor = Color.white;
                        break;
                    case InteractionState.Pressed:
                        backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                        textColor = Color.white;
                        arrowColor = Color.white;
                        break;
                    case InteractionState.Selected:
                        backgroundColor = new Color(0.3f, 0.5f, 0.9f, 0.8f);
                        textColor = Color.white;
                        arrowColor = Color.white;
                        break;
                    case InteractionState.Disabled:
                        backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                        textColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                        arrowColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                        break;
                    default:
                        backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                        textColor = Color.white;
                        arrowColor = Color.white;
                        break;
                }
            }

            // 应用颜色
            if (m_Background != null)
            {
                m_Background.color = backgroundColor;
            }

            if (m_CaptionText != null)
            {
                m_CaptionText.color = textColor;
            }

            if (m_Arrow != null && m_Arrow.GetComponent<Image>() != null)
            {
                m_Arrow.GetComponent<Image>().color = arrowColor;
            }
        }

        /// <summary>
        /// 指针点击
        /// </summary>
        public override void OnPointerClick()
        {
            base.OnPointerClick();

            if (!m_interactable)
                return;

            // 切换下拉面板
            Toggle();
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
                m_rectTransform.sizeDelta = new Vector2(200, 40);
            }

            // 如果没有背景组件，创建一个
            if (m_Background == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                m_Background = bgObj.AddComponent<Image>();
                m_Background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            // 如果没有标题文本组件，创建一个
            if (m_CaptionText == null)
            {
                GameObject textObj = new GameObject("CaptionText");
                textObj.transform.SetParent(transform);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-30, 0);

                m_CaptionText = textObj.AddComponent<TextMeshProUGUI>();
                m_CaptionText.alignment = TextAlignmentOptions.Left;
                m_CaptionText.enableWordWrapping = false;
                m_CaptionText.overflowMode = TextOverflowModes.Ellipsis;
                m_CaptionText.color = Color.white;
            }

            // 如果没有箭头组件，创建一个
            if (m_Arrow == null)
            {
                GameObject arrowObj = new GameObject("Arrow");
                arrowObj.transform.SetParent(transform);

                m_Arrow = arrowObj.AddComponent<RectTransform>();
                m_Arrow.anchorMin = new Vector2(1, 0.5f);
                m_Arrow.anchorMax = new Vector2(1, 0.5f);
                m_Arrow.pivot = new Vector2(1, 0.5f);
                m_Arrow.anchoredPosition = new Vector2(-10, 0);
                m_Arrow.sizeDelta = new Vector2(20, 10);

                Image arrowImage = arrowObj.AddComponent<Image>();
                arrowImage.color = Color.white;
                // 使用三角形精灵或创建一个
                // arrowImage.sprite = ...
            }

            // 如果没有模板，创建一个
            if (m_Template == null)
            {
                GameObject templateObj = new GameObject("Template");
                templateObj.transform.SetParent(transform);

                m_Template = templateObj.AddComponent<RectTransform>();
                m_Template.anchorMin = new Vector2(0, 0);
                m_Template.anchorMax = new Vector2(1, 0);
                m_Template.pivot = new Vector2(0.5f, 1);
                m_Template.anchoredPosition = new Vector2(0, 0);
                m_Template.sizeDelta = new Vector2(0, 100);

                // 添加背景
                Image templateBg = templateObj.AddComponent<Image>();
                templateBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                // 添加滚动视图
                ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();

                // 创建视口
                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(templateObj.transform);

                m_Viewport = viewportObj.AddComponent<RectTransform>();
                m_Viewport.anchorMin = Vector2.zero;
                m_Viewport.anchorMax = Vector2.one;
                m_Viewport.offsetMin = new Vector2(0, 0);
                m_Viewport.offsetMax = new Vector2(0, 0);
                m_Viewport.pivot = new Vector2(0.5f, 0.5f);

                // 添加遮罩
                if (m_ShowMask)
                {
                    viewportObj.AddComponent<Mask>().showMaskGraphic = false;
                    Image viewportImage = viewportObj.AddComponent<Image>();
                    viewportImage.color = Color.white;
                }

                // 创建内容
                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform);

                m_Content = contentObj.AddComponent<RectTransform>();
                m_Content.anchorMin = new Vector2(0, 1);
                m_Content.anchorMax = new Vector2(1, 1);
                m_Content.pivot = new Vector2(0.5f, 1);
                m_Content.anchoredPosition = Vector2.zero;
                m_Content.sizeDelta = new Vector2(0, 100);

                // 设置滚动视图
                scrollRect.viewport = m_Viewport;
                scrollRect.content = m_Content;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 30;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                // 创建项目模板
                GameObject itemTemplateObj = new GameObject("ItemTemplate");
                itemTemplateObj.transform.SetParent(contentObj.transform);

                RectTransform itemRect = itemTemplateObj.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, m_ItemHeight);

                // 添加项目背景
                m_ItemBackground = itemTemplateObj.AddComponent<Image>();
                m_ItemBackground.color = new Color(0.2f, 0.2f, 0.2f, 0);

                // 添加项目文本
                GameObject itemTextObj = new GameObject("ItemText");
                itemTextObj.transform.SetParent(itemTemplateObj.transform);

                RectTransform itemTextRect = itemTextObj.AddComponent<RectTransform>();
                itemTextRect.anchorMin = Vector2.zero;
                itemTextRect.anchorMax = Vector2.one;
                itemTextRect.offsetMin = new Vector2(10, 0);
                itemTextRect.offsetMax = new Vector2(-10, 0);

                m_ItemText = itemTextObj.AddComponent<TextMeshProUGUI>();
                m_ItemText.alignment = TextAlignmentOptions.Left;
                m_ItemText.enableWordWrapping = false;
                m_ItemText.overflowMode = TextOverflowModes.Ellipsis;
                m_ItemText.color = Color.white;

                // 隐藏模板
                itemTemplateObj.SetActive(false);
                templateObj.SetActive(false);
            }
        }

        /// <summary>
        /// 刷新显示的值
        /// </summary>
        private void RefreshShownValue()
        {
            if (m_Options.options.Count > 0 && m_Value >= 0 && m_Value < m_Options.options.Count)
            {
                OptionData data = m_Options.options[m_Value];

                if (m_CaptionText != null)
                {
                    m_CaptionText.text = data.text;
                }

                if (m_CaptionImage != null)
                {
                    m_CaptionImage.sprite = data.image;
                    m_CaptionImage.enabled = data.image != null;
                }
            }
            else
            {
                if (m_CaptionText != null)
                {
                    m_CaptionText.text = m_Options.options.Count > 0 ? "选择..." : "无选项";
                }

                if (m_CaptionImage != null)
                {
                    m_CaptionImage.enabled = false;
                }
            }
        }

        /// <summary>
        /// 创建下拉列表
        /// </summary>
        private void CreateDropdownList()
        {
            if (m_Template == null || m_Content == null)
                return;

            // 清除现有项目
            foreach (Transform child in m_Content.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Destroy(child.gameObject);
                }
            }
            m_Items.Clear();

            // 设置内容高度
            float contentHeight = m_Options.options.Count * m_ItemHeight;
            m_Content.sizeDelta = new Vector2(m_Content.sizeDelta.x, contentHeight);

            // 限制模板高度
            float templateHeight = Mathf.Min(contentHeight, m_MaxHeight);
            m_Template.sizeDelta = new Vector2(m_Template.sizeDelta.x, templateHeight);

            // 创建项目
            for (int i = 0; i < m_Options.options.Count; i++)
            {
                CreateItem(i);
            }

            // 滚动到当前选中项
            Canvas.ForceUpdateCanvases();
            if (m_Value >= 0 && m_Value < m_Items.Count)
            {
                ScrollRect scrollRect = m_Template.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    float normalizedPosition = 1f - (float)m_Value / (float)Mathf.Max(1, m_Options.options.Count - 1);
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
                }
            }
        }

        /// <summary>
        /// 创建下拉项目
        /// </summary>
        private void CreateItem(int index)
        {
            OptionData data = m_Options.options[index];

            // 获取项目模板
            GameObject itemTemplate = m_Content.GetChild(0).gameObject;

            // 创建项目
            GameObject item = Instantiate(itemTemplate, m_Content);
            item.name = "Item " + index;
            item.SetActive(true);

            // 设置位置
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchoredPosition = new Vector2(0, -index * m_ItemHeight);

            // 设置文本
            TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.text = data.text;
            }

            // 设置图标
            Image itemImage = item.GetComponentInChildren<Image>();
            if (itemImage != null && itemImage != item.GetComponent<Image>())
            {
                itemImage.sprite = data.image;
                itemImage.enabled = data.image != null;
            }

            // 高亮当前选中项
            if (index == m_Value)
            {
                Image background = item.GetComponent<Image>();
                if (background != null)
                {
                    background.color = m_theme != null ?
                        m_theme.GetStateColor(InteractionState.Selected) :
                        new Color(0.3f, 0.5f, 0.9f, 0.5f);
                }
            }

            // 添加点击事件
            int itemIndex = index;
            VRButton itemButton = item.AddComponent<VRButton>();
            itemButton.OnClick.AddListener(() => OnItemClicked(itemIndex));

            // 添加到列表
            m_Items.Add(itemRect);
        }

        /// <summary>
        /// 项目点击事件处理
        /// </summary>
        private void OnItemClicked(int index)
        {
            SetValue(index);
            Hide();
        }

        /// <summary>
        /// 更新箭头方向
        /// </summary>
        private void UpdateArrowDirection(bool isExpanded)
        {
            if (m_Arrow != null)
            {
                m_Arrow.localRotation = Quaternion.Euler(0, 0, isExpanded ? 180 : 0);
            }
        }

        /// <summary>
        /// 播放展开动画
        /// </summary>
        private void PlayExpandAnimation()
        {
            // 动画实现
            // 在实际项目中，可以使用协程或DoTween等动画库实现平滑过渡
            // 这里简化为直接设置
            if (m_Template != null)
            {
                m_Template.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 播放收起动画
        /// </summary>
        private void PlayCollapseAnimation()
        {
            // 动画实现
            // 在实际项目中，可以使用协程或DoTween等动画库实现平滑过渡
            // 这里简化为直接设置
            if (m_Template != null)
            {
                m_Template.gameObject.SetActive(false);
                m_IsExpanded = false;
                UpdateArrowDirection(false);
            }
        }

        #endregion
    }
}