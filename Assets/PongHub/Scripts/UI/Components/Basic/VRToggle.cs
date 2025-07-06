using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR开关组件
    /// 实现VR环境中的开关控制
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VRToggle : VRUIComponent
    {
        [Header("开关设置")]
        [SerializeField]
        [Tooltip("Is On / 是否开启 - Whether the toggle is on")]
        private bool m_isOn = false;

        [SerializeField]
        [Tooltip("Label / 标签 - Label text for the toggle")]
        private string m_label = "Toggle";

        [SerializeField]
        [Tooltip("Label Position / 标签位置 - Position of the label relative to the toggle")]
        private LabelPosition m_labelPosition = LabelPosition.Right;

        // 标签位置枚举
        public enum LabelPosition
        {
            Left,
            Right,
            Top,
            Bottom
        }

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Toggle background image")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Checkmark / 勾选标记 - Toggle checkmark image")]
        private Image m_checkmark;

        [SerializeField]
        [Tooltip("Label Text / 标签文本 - Text component for toggle label")]
        private TextMeshProUGUI m_labelText;

        [SerializeField]
        [Tooltip("Toggle Width / 开关宽度 - Width of the toggle box")]
        private float m_toggleWidth = 30f;

        [SerializeField]
        [Tooltip("Toggle Height / 开关高度 - Height of the toggle box")]
        private float m_toggleHeight = 30f;

        [SerializeField]
        [Tooltip("Hover Scale / 悬停缩放 - Scale factor when toggle is hovered")]
        [Range(1f, 1.2f)]
        private float m_hoverScale = 1.05f;

        [SerializeField]
        [Tooltip("Animation Speed / 动画速度 - Speed of the toggle animation")]
        [Range(5f, 20f)]
        private float m_animationSpeed = 10f;

        // 事件
        [Header("事件")]
        public UnityEvent<bool> OnValueChanged = new UnityEvent<bool>();

        // 缓存的初始缩放
        private Vector3 m_initialScale;

        // 动画相关
        private float m_checkmarkTargetAlpha = 0f;
        private float m_currentCheckmarkAlpha = 0f;

        // 缓存的RectTransform
        private RectTransform m_rectTransform;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取RectTransform
            m_rectTransform = GetComponent<RectTransform>();

            // 缓存初始缩放
            m_initialScale = transform.localScale;

            // 确保有碰撞器
            EnsureCollider();

            // 初始化组件
            InitializeComponents();

            // 更新视觉状态
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);

            // 设置初始状态
            SetIsOn(m_isOn, false);

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
            // 处理勾选标记动画
            if (m_checkmark != null)
            {
                if (Mathf.Abs(m_currentCheckmarkAlpha - m_checkmarkTargetAlpha) > 0.01f)
                {
                    m_currentCheckmarkAlpha = Mathf.Lerp(m_currentCheckmarkAlpha, m_checkmarkTargetAlpha, Time.deltaTime * m_animationSpeed);
                    Color color = m_checkmark.color;
                    color.a = m_currentCheckmarkAlpha;
                    m_checkmark.color = color;
                }
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置开关状态
        /// </summary>
        public void SetIsOn(bool isOn, bool notify = true)
        {
            if (m_isOn != isOn)
            {
                m_isOn = isOn;
                m_checkmarkTargetAlpha = m_isOn ? 1f : 0f;

                // 触发事件
                if (notify)
                {
                    OnValueChanged.Invoke(m_isOn);
                }
            }
        }

        /// <summary>
        /// 获取开关状态
        /// </summary>
        public bool GetIsOn()
        {
            return m_isOn;
        }

        /// <summary>
        /// 切换开关状态
        /// </summary>
        public void Toggle()
        {
            SetIsOn(!m_isOn);
        }

        /// <summary>
        /// 设置标签文本
        /// </summary>
        public void SetLabel(string label)
        {
            m_label = label;
            if (m_labelText != null)
            {
                m_labelText.text = m_label;
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
            Color checkmarkColor;
            Color textColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = state == InteractionState.Disabled
                    ? m_theme.disabledColor
                    : m_isOn ? m_theme.accentColor : m_theme.backgroundColor;

                checkmarkColor = m_theme.textColor;
                textColor = m_theme.GetTextColor(state);
            }
            else
            {
                // 默认颜色
                switch (state)
                {
                    case InteractionState.Normal:
                        backgroundColor = m_isOn ? new Color(0.227f, 0.525f, 1f) : new Color(0.2f, 0.2f, 0.2f);
                        checkmarkColor = Color.white;
                        textColor = Color.white;
                        break;
                    case InteractionState.Highlighted:
                        backgroundColor = m_isOn ? new Color(0.4f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                        checkmarkColor = Color.white;
                        textColor = Color.white;
                        break;
                    case InteractionState.Pressed:
                        backgroundColor = m_isOn ? new Color(0.2f, 0.4f, 0.8f) : new Color(0.15f, 0.15f, 0.15f);
                        checkmarkColor = Color.white;
                        textColor = Color.white;
                        break;
                    case InteractionState.Disabled:
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        checkmarkColor = new Color(1f, 1f, 1f, 0.5f);
                        textColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                    default:
                        backgroundColor = m_isOn ? new Color(0.227f, 0.525f, 1f) : new Color(0.2f, 0.2f, 0.2f);
                        checkmarkColor = Color.white;
                        textColor = Color.white;
                        break;
                }
            }

            // 应用颜色
            if (m_background != null)
            {
                m_background.color = backgroundColor;
            }

            if (m_checkmark != null)
            {
                Color color = checkmarkColor;
                color.a = m_currentCheckmarkAlpha;
                m_checkmark.color = color;
            }

            if (m_labelText != null)
            {
                m_labelText.color = textColor;
            }

            // 更新缩放
            if (state == InteractionState.Highlighted)
            {
                transform.localScale = m_initialScale * m_hoverScale;
            }
            else
            {
                transform.localScale = m_initialScale;
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

            // 切换状态
            Toggle();

            // 更新视觉状态
            UpdateVisualState(m_isHovered ? InteractionState.Highlighted : InteractionState.Normal);
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
                collider.size = new Vector3(m_toggleWidth + 100f, m_toggleHeight, 0.1f);
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
                m_rectTransform.sizeDelta = new Vector2(m_toggleWidth + 100f, m_toggleHeight);
            }

            // 创建开关背景
            if (m_background == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0, 0.5f);
                bgRect.anchorMax = new Vector2(0, 0.5f);
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.sizeDelta = new Vector2(m_toggleWidth, m_toggleHeight);

                // 根据标签位置调整背景位置
                switch (m_labelPosition)
                {
                    case LabelPosition.Left:
                        bgRect.anchoredPosition = new Vector2(m_rectTransform.sizeDelta.x - m_toggleWidth / 2, 0);
                        break;
                    case LabelPosition.Right:
                        bgRect.anchoredPosition = new Vector2(m_toggleWidth / 2, 0);
                        break;
                    case LabelPosition.Top:
                        bgRect.anchoredPosition = new Vector2(m_rectTransform.sizeDelta.x / 2, -m_toggleHeight / 2);
                        break;
                    case LabelPosition.Bottom:
                        bgRect.anchoredPosition = new Vector2(m_rectTransform.sizeDelta.x / 2, m_toggleHeight / 2);
                        break;
                }

                m_background = bgObj.AddComponent<Image>();
                m_background.color = m_isOn ? new Color(0.227f, 0.525f, 1f) : new Color(0.2f, 0.2f, 0.2f);
            }

            // 创建勾选标记
            if (m_checkmark == null)
            {
                GameObject checkObj = new GameObject("Checkmark");
                checkObj.transform.SetParent(m_background.transform);

                RectTransform checkRect = checkObj.AddComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(0.1f, 0.1f);
                checkRect.anchorMax = new Vector2(0.9f, 0.9f);
                checkRect.offsetMin = Vector2.zero;
                checkRect.offsetMax = Vector2.zero;

                m_checkmark = checkObj.AddComponent<Image>();
                m_checkmark.color = new Color(1f, 1f, 1f, m_isOn ? 1f : 0f);
                m_currentCheckmarkAlpha = m_isOn ? 1f : 0f;
                m_checkmarkTargetAlpha = m_currentCheckmarkAlpha;

                // 尝试加载勾选图标
                Sprite checkSprite = Resources.Load<Sprite>("UI/Checkmark");
                if (checkSprite != null)
                {
                    m_checkmark.sprite = checkSprite;
                    m_checkmark.type = Image.Type.Simple;
                }
                else
                {
                    // 如果没有图标，使用简单的十字形状
                    m_checkmark.type = Image.Type.Simple;
                }
            }

            // 创建标签文本
            if (m_labelText == null)
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(transform);

                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.pivot = new Vector2(0.5f, 0.5f);

                // 根据标签位置调整文本位置和大小
                switch (m_labelPosition)
                {
                    case LabelPosition.Left:
                        labelRect.anchorMin = new Vector2(0, 0.5f);
                        labelRect.anchorMax = new Vector2(0, 0.5f);
                        labelRect.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x - m_toggleWidth - 10, m_toggleHeight);
                        labelRect.anchoredPosition = new Vector2((m_rectTransform.sizeDelta.x - m_toggleWidth) / 2 - 5, 0);
                        break;
                    case LabelPosition.Right:
                        labelRect.anchorMin = new Vector2(1, 0.5f);
                        labelRect.anchorMax = new Vector2(1, 0.5f);
                        labelRect.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x - m_toggleWidth - 10, m_toggleHeight);
                        labelRect.anchoredPosition = new Vector2(-(m_rectTransform.sizeDelta.x - m_toggleWidth) / 2 - 5, 0);
                        break;
                    case LabelPosition.Top:
                        labelRect.anchorMin = new Vector2(0.5f, 1);
                        labelRect.anchorMax = new Vector2(0.5f, 1);
                        labelRect.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x, m_rectTransform.sizeDelta.y - m_toggleHeight - 5);
                        labelRect.anchoredPosition = new Vector2(0, -(m_rectTransform.sizeDelta.y - m_toggleHeight) / 2 - 2.5f);
                        break;
                    case LabelPosition.Bottom:
                        labelRect.anchorMin = new Vector2(0.5f, 0);
                        labelRect.anchorMax = new Vector2(0.5f, 0);
                        labelRect.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x, m_rectTransform.sizeDelta.y - m_toggleHeight - 5);
                        labelRect.anchoredPosition = new Vector2(0, (m_rectTransform.sizeDelta.y - m_toggleHeight) / 2 + 2.5f);
                        break;
                }

                m_labelText = labelObj.AddComponent<TextMeshProUGUI>();
                m_labelText.text = m_label;
                m_labelText.fontSize = 14;
                m_labelText.color = Color.white;

                // 根据标签位置调整文本对齐方式
                switch (m_labelPosition)
                {
                    case LabelPosition.Left:
                        m_labelText.alignment = TextAlignmentOptions.Right;
                        break;
                    case LabelPosition.Right:
                        m_labelText.alignment = TextAlignmentOptions.Left;
                        break;
                    case LabelPosition.Top:
                    case LabelPosition.Bottom:
                        m_labelText.alignment = TextAlignmentOptions.Center;
                        break;
                }
            }
        }

        #endregion
    }
}