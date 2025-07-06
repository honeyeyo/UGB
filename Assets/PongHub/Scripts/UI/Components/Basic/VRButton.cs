using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR按钮组件
    /// 实现VR环境中的交互按钮
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VRButton : VRUIComponent
    {
        [Header("按钮设置")]
        [SerializeField]
        [Tooltip("Button Text / 按钮文本 - Text displayed on the button")]
        private string m_text = "Button";

        [SerializeField]
        [Tooltip("Text Size / 文本大小 - Size of the button text")]
        private float m_textSize = 24f;

        [SerializeField]
        [Tooltip("Use Icon / 使用图标 - Whether to display an icon")]
        private bool m_useIcon = false;

        [SerializeField]
        [Tooltip("Icon / 图标 - Icon displayed on the button")]
        private Sprite m_icon;

        [SerializeField]
        [Tooltip("Icon Size / 图标大小 - Size of the icon as a fraction of button height")]
        [Range(0.1f, 0.9f)]
        private float m_iconSize = 0.6f;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Button background image")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Text Component / 文本组件 - Text component for button label")]
        private TextMeshProUGUI m_textComponent;

        [SerializeField]
        [Tooltip("Icon Component / 图标组件 - Image component for button icon")]
        private Image m_iconComponent;

        [SerializeField]
        [Tooltip("Hover Scale / 悬停缩放 - Scale factor when button is hovered")]
        [Range(1f, 1.2f)]
        private float m_hoverScale = 1.05f;

        [SerializeField]
        [Tooltip("Press Scale / 按下缩放 - Scale factor when button is pressed")]
        [Range(0.8f, 1f)]
        private float m_pressScale = 0.95f;

        // 缓存的初始缩放
        private Vector3 m_initialScale;

        // 动画相关
        private float m_animationTime = 0f;
        private Vector3 m_targetScale;
        private Color m_targetBackgroundColor;
        private Color m_targetTextColor;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 缓存初始缩放
            m_initialScale = transform.localScale;
            m_targetScale = m_initialScale;

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
            // 处理动画
            if (m_useAnimation && m_animationTime < m_animationDuration)
            {
                m_animationTime += Time.deltaTime;
                float t = Mathf.Clamp01(m_animationTime / m_animationDuration);

                // 应用缩放动画
                transform.localScale = Vector3.Lerp(transform.localScale, m_targetScale, t);

                // 应用颜色动画
                if (m_background != null)
                {
                    m_background.color = Color.Lerp(m_background.color, m_targetBackgroundColor, t);
                }

                if (m_textComponent != null)
                {
                    m_textComponent.color = Color.Lerp(m_textComponent.color, m_targetTextColor, t);
                }
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置按钮文本
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
        /// 获取按钮文本
        /// </summary>
        public string GetText()
        {
            return m_text;
        }

        /// <summary>
        /// 设置按钮图标
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            m_icon = icon;
            m_useIcon = (m_icon != null);

            if (m_iconComponent != null)
            {
                m_iconComponent.sprite = m_icon;
                m_iconComponent.gameObject.SetActive(m_useIcon);
            }
        }

        /// <summary>
        /// 获取按钮图标
        /// </summary>
        public Sprite GetIcon()
        {
            return m_icon;
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

            // 重置动画计时器
            m_animationTime = 0f;

            // 根据状态设置目标缩放
            switch (state)
            {
                case InteractionState.Normal:
                    m_targetScale = m_initialScale;
                    break;
                case InteractionState.Highlighted:
                    m_targetScale = m_initialScale * m_hoverScale;
                    break;
                case InteractionState.Pressed:
                    m_targetScale = m_initialScale * m_pressScale;
                    break;
                case InteractionState.Selected:
                    m_targetScale = m_initialScale * m_hoverScale;
                    break;
                case InteractionState.Disabled:
                    m_targetScale = m_initialScale;
                    break;
            }

            // 根据主题设置颜色
            if (m_theme != null)
            {
                m_targetBackgroundColor = m_theme.GetStateColor(state);
                m_targetTextColor = m_theme.GetTextColor(state);
            }
            else
            {
                // 默认颜色
                switch (state)
                {
                    case InteractionState.Normal:
                        m_targetBackgroundColor = new Color(0.227f, 0.525f, 1f);
                        m_targetTextColor = Color.white;
                        break;
                    case InteractionState.Highlighted:
                        m_targetBackgroundColor = new Color(0.4f, 0.6f, 1f);
                        m_targetTextColor = Color.white;
                        break;
                    case InteractionState.Pressed:
                        m_targetBackgroundColor = new Color(0.2f, 0.4f, 0.8f);
                        m_targetTextColor = Color.white;
                        break;
                    case InteractionState.Selected:
                        m_targetBackgroundColor = new Color(1f, 0f, 0.431f);
                        m_targetTextColor = Color.white;
                        break;
                    case InteractionState.Disabled:
                        m_targetBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        m_targetTextColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                }
            }

            // 如果不使用动画，直接应用状态
            if (!m_useAnimation)
            {
                transform.localScale = m_targetScale;

                if (m_background != null)
                {
                    m_background.color = m_targetBackgroundColor;
                }

                if (m_textComponent != null)
                {
                    m_textComponent.color = m_targetTextColor;
                }
            }
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
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                collider.size = new Vector3(size.x, size.y, 1f);
                collider.center = Vector3.zero;
            }
            else
            {
                collider.size = new Vector3(1f, 1f, 0.1f);
                collider.center = Vector3.zero;
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有指定背景组件，尝试查找
            if (m_background == null)
            {
                m_background = GetComponent<Image>();
            }

            // 如果没有指定文本组件，尝试查找
            if (m_textComponent == null)
            {
                m_textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }

            // 如果没有找到文本组件，创建一个
            if (m_textComponent == null && m_text != null)
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(transform);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                m_textComponent = textObj.AddComponent<TextMeshProUGUI>();
                m_textComponent.text = m_text;
                m_textComponent.fontSize = m_textSize;
                m_textComponent.alignment = TextAlignmentOptions.Center;
                m_textComponent.color = Color.white;
            }
            else if (m_textComponent != null)
            {
                // 更新现有文本组件
                m_textComponent.text = m_text;
                m_textComponent.fontSize = m_textSize;
            }

            // 处理图标
            if (m_useIcon && m_icon != null)
            {
                // 如果没有指定图标组件，尝试查找
                if (m_iconComponent == null)
                {
                    m_iconComponent = transform.Find("Icon")?.GetComponent<Image>();
                }

                // 如果没有找到图标组件，创建一个
                if (m_iconComponent == null)
                {
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(transform);
                    RectTransform iconRect = iconObj.AddComponent<RectTransform>();

                    // 设置图标大小和位置
                    float iconSizeValue = m_iconSize;
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);

                    // 获取按钮大小
                    RectTransform buttonRect = GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        Vector2 buttonSize = buttonRect.sizeDelta;
                        float size = Mathf.Min(buttonSize.x, buttonSize.y) * iconSizeValue;
                        iconRect.sizeDelta = new Vector2(size, size);
                    }
                    else
                    {
                        iconRect.sizeDelta = new Vector2(50f * iconSizeValue, 50f * iconSizeValue);
                    }

                    iconRect.anchoredPosition = Vector2.zero;

                    m_iconComponent = iconObj.AddComponent<Image>();
                    m_iconComponent.sprite = m_icon;
                    m_iconComponent.preserveAspect = true;
                }
                else
                {
                    // 更新现有图标组件
                    m_iconComponent.sprite = m_icon;
                    m_iconComponent.gameObject.SetActive(true);
                }
            }
            else if (m_iconComponent != null)
            {
                // 如果不使用图标，隐藏图标组件
                m_iconComponent.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}