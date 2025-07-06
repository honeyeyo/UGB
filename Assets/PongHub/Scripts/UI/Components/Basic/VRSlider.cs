using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR滑块组件
    /// 实现VR环境中的滑块控制
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VRSlider : VRUIComponent
    {
        [Header("滑块设置")]
        [SerializeField]
        [Tooltip("Min Value / 最小值 - Minimum value of the slider")]
        private float m_minValue = 0f;

        [SerializeField]
        [Tooltip("Max Value / 最大值 - Maximum value of the slider")]
        private float m_maxValue = 1f;

        [SerializeField]
        [Tooltip("Current Value / 当前值 - Current value of the slider")]
        private float m_value = 0.5f;

        [SerializeField]
        [Tooltip("Step Size / 步长 - Step size for value changes (0 for continuous)")]
        private float m_stepSize = 0f;

        [SerializeField]
        [Tooltip("Show Value Text / 显示数值 - Whether to display the current value as text")]
        private bool m_showValueText = true;

        [SerializeField]
        [Tooltip("Value Format / 数值格式 - Format string for the value text (e.g. {0:0.00})")]
        private string m_valueFormat = "{0:0.00}";

        [SerializeField]
        [Tooltip("Label / 标签 - Label text for the slider")]
        private string m_label = "Slider";

        [SerializeField]
        [Tooltip("Show Label / 显示标签 - Whether to display the label")]
        private bool m_showLabel = true;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Slider background image")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Fill / 填充 - Slider fill image")]
        private Image m_fill;

        [SerializeField]
        [Tooltip("Handle / 滑块 - Slider handle image")]
        private RectTransform m_handle;

        [SerializeField]
        [Tooltip("Label Text / 标签文本 - Text component for slider label")]
        private TextMeshProUGUI m_labelText;

        [SerializeField]
        [Tooltip("Value Text / 数值文本 - Text component for slider value")]
        private TextMeshProUGUI m_valueText;

        [SerializeField]
        [Tooltip("Handle Hover Scale / 滑块悬停缩放 - Scale factor when handle is hovered")]
        [Range(1f, 1.5f)]
        private float m_handleHoverScale = 1.2f;

        [SerializeField]
        [Tooltip("Slider Direction / 滑块方向 - Direction of the slider")]
        private Direction m_direction = Direction.LeftToRight;

        // 滑块方向枚举
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom
        }

        // 事件
        [Header("事件")]
        public UnityEvent<float> OnValueChanged = new UnityEvent<float>();

        // 交互状态
        private bool m_isDragging = false;
        private Vector3 m_initialHandleScale;

        // 缓存的RectTransform
        private RectTransform m_rectTransform;

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

            // 缓存初始滑块缩放
            if (m_handle != null)
            {
                m_initialHandleScale = m_handle.localScale;
            }

            // 更新视觉状态
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);

            // 设置初始值
            SetValue(m_value, false);

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
        /// 设置滑块值
        /// </summary>
        public void SetValue(float value, bool notify = true)
        {
            // 限制值在范围内
            float newValue = Mathf.Clamp(value, m_minValue, m_maxValue);

            // 应用步长
            if (m_stepSize > 0)
            {
                newValue = Mathf.Round((newValue - m_minValue) / m_stepSize) * m_stepSize + m_minValue;
            }

            // 检查值是否变化
            if (m_value != newValue)
            {
                m_value = newValue;
                UpdateVisuals();

                // 触发事件
                if (notify)
                {
                    OnValueChanged.Invoke(m_value);
                }
            }
        }

        /// <summary>
        /// 获取滑块值
        /// </summary>
        public float GetValue()
        {
            return m_value;
        }

        /// <summary>
        /// 设置滑块范围
        /// </summary>
        public void SetRange(float minValue, float maxValue)
        {
            m_minValue = minValue;
            m_maxValue = maxValue;

            // 确保当前值在新范围内
            m_value = Mathf.Clamp(m_value, m_minValue, m_maxValue);

            // 更新滑块位置和视觉效果
            UpdateVisuals();
        }

        /// <summary>
        /// 设置滑块标签
        /// </summary>
        public void SetLabel(string label)
        {
            m_label = label;
            if (m_labelText != null)
            {
                m_labelText.text = m_label;
            }
        }

        /// <summary>
        /// 设置是否显示数值
        /// </summary>
        public void SetShowValue(bool showValue)
        {
            m_showValueText = showValue;

            if (m_valueText != null)
            {
                m_valueText.gameObject.SetActive(showValue);
            }

            UpdateVisuals();
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
            Color fillColor;
            Color handleColor;
            Color textColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.backgroundColor;
                fillColor = m_theme.GetStateColor(state);
                handleColor = m_theme.GetStateColor(state);
                textColor = m_theme.GetTextColor(state);
            }
            else
            {
                // 默认颜色
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                switch (state)
                {
                    case InteractionState.Normal:
                        fillColor = new Color(0.227f, 0.525f, 1f);
                        handleColor = new Color(0.227f, 0.525f, 1f);
                        textColor = Color.white;
                        break;
                    case InteractionState.Highlighted:
                        fillColor = new Color(0.4f, 0.6f, 1f);
                        handleColor = new Color(0.4f, 0.6f, 1f);
                        textColor = Color.white;
                        break;
                    case InteractionState.Pressed:
                        fillColor = new Color(0.2f, 0.4f, 0.8f);
                        handleColor = new Color(0.2f, 0.4f, 0.8f);
                        textColor = Color.white;
                        break;
                    case InteractionState.Selected:
                        fillColor = new Color(1f, 0f, 0.431f);
                        handleColor = new Color(1f, 0f, 0.431f);
                        textColor = Color.white;
                        break;
                    case InteractionState.Disabled:
                        fillColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        handleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        textColor = new Color(1f, 1f, 1f, 0.5f);
                        break;
                    default:
                        fillColor = new Color(0.227f, 0.525f, 1f);
                        handleColor = new Color(0.227f, 0.525f, 1f);
                        textColor = Color.white;
                        break;
                }
            }

            // 应用颜色
            if (m_background != null)
            {
                m_background.color = backgroundColor;
            }

            if (m_fill != null)
            {
                m_fill.color = fillColor;
            }

            if (m_handle != null && m_handle.GetComponent<Image>() != null)
            {
                m_handle.GetComponent<Image>().color = handleColor;
            }

            if (m_labelText != null)
            {
                m_labelText.color = textColor;
            }

            if (m_valueText != null)
            {
                m_valueText.color = textColor;
            }

            // 更新滑块缩放
            if (m_handle != null)
            {
                if (state == InteractionState.Highlighted || state == InteractionState.Pressed)
                {
                    m_handle.localScale = m_initialHandleScale * m_handleHoverScale;
                }
                else
                {
                    m_handle.localScale = m_initialHandleScale;
                }
            }
        }

        /// <summary>
        /// 指针按下
        /// </summary>
        public override void OnPointerDown()
        {
            base.OnPointerDown();

            if (!m_interactable)
                return;

            m_isDragging = true;
            UpdateValueFromPointer();
        }

        /// <summary>
        /// 指针抬起
        /// </summary>
        public override void OnPointerUp()
        {
            base.OnPointerUp();

            if (!m_interactable)
                return;

            m_isDragging = false;
        }

        /// <summary>
        /// 指针进入
        /// </summary>
        public override void OnPointerEnter()
        {
            base.OnPointerEnter();

            if (!m_interactable)
                return;

            // 如果正在拖动，更新值
            if (m_isDragging)
            {
                UpdateValueFromPointer();
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
            if (m_rectTransform != null)
            {
                Vector2 size = m_rectTransform.sizeDelta;
                collider.size = new Vector3(size.x, size.y, 1f);
                collider.center = Vector3.zero;
            }
            else
            {
                collider.size = new Vector3(200f, 30f, 0.1f);
                collider.center = Vector3.zero;
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有指定背景组件，尝试查找或创建
            if (m_background == null)
            {
                m_background = GetComponent<Image>();

                if (m_background == null && m_rectTransform != null)
                {
                    m_background = gameObject.AddComponent<Image>();
                    m_background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                }
            }

            // 如果没有指定填充组件，尝试查找或创建
            if (m_fill == null)
            {
                Transform fillTransform = transform.Find("Fill");
                if (fillTransform != null)
                {
                    m_fill = fillTransform.GetComponent<Image>();
                }

                if (m_fill == null && m_rectTransform != null)
                {
                    GameObject fillObj = new GameObject("Fill");
                    fillObj.transform.SetParent(transform);

                    RectTransform fillRect = fillObj.AddComponent<RectTransform>();
                    fillRect.anchorMin = new Vector2(0, 0);
                    fillRect.anchorMax = new Vector2(0, 1);
                    fillRect.pivot = new Vector2(0, 0.5f);
                    fillRect.offsetMin = new Vector2(0, 0);
                    fillRect.offsetMax = new Vector2(0, 0);

                    m_fill = fillObj.AddComponent<Image>();
                    m_fill.color = new Color(0.227f, 0.525f, 1f);
                }
            }

            // 如果没有指定滑块组件，尝试查找或创建
            if (m_handle == null)
            {
                Transform handleTransform = transform.Find("Handle");
                if (handleTransform != null)
                {
                    m_handle = handleTransform as RectTransform;
                }

                if (m_handle == null && m_rectTransform != null)
                {
                    GameObject handleObj = new GameObject("Handle");
                    handleObj.transform.SetParent(transform);

                    m_handle = handleObj.AddComponent<RectTransform>();
                    m_handle.anchorMin = new Vector2(0, 0.5f);
                    m_handle.anchorMax = new Vector2(0, 0.5f);
                    m_handle.pivot = new Vector2(0.5f, 0.5f);

                    float handleSize = m_rectTransform.sizeDelta.y * 1.2f;
                    m_handle.sizeDelta = new Vector2(handleSize, handleSize);

                    Image handleImage = handleObj.AddComponent<Image>();
                    handleImage.color = new Color(0.227f, 0.525f, 1f);
                }
            }

            // 如果没有指定标签文本组件，尝试查找或创建
            if (m_labelText == null && m_showLabel)
            {
                Transform labelTransform = transform.Find("Label");
                if (labelTransform != null)
                {
                    m_labelText = labelTransform.GetComponent<TextMeshProUGUI>();
                }

                if (m_labelText == null && m_rectTransform != null)
                {
                    GameObject labelObj = new GameObject("Label");
                    labelObj.transform.SetParent(transform);

                    RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                    labelRect.anchorMin = new Vector2(0, 1);
                    labelRect.anchorMax = new Vector2(1, 1);
                    labelRect.pivot = new Vector2(0.5f, 0);
                    labelRect.sizeDelta = new Vector2(0, 20);
                    labelRect.anchoredPosition = new Vector2(0, 5);

                    m_labelText = labelObj.AddComponent<TextMeshProUGUI>();
                    m_labelText.text = m_label;
                    m_labelText.fontSize = 14;
                    m_labelText.alignment = TextAlignmentOptions.Center;
                    m_labelText.color = Color.white;
                }
            }

            // 如果没有指定数值文本组件，尝试查找或创建
            if (m_valueText == null && m_showValueText)
            {
                Transform valueTransform = transform.Find("Value");
                if (valueTransform != null)
                {
                    m_valueText = valueTransform.GetComponent<TextMeshProUGUI>();
                }

                if (m_valueText == null && m_rectTransform != null)
                {
                    GameObject valueObj = new GameObject("Value");
                    valueObj.transform.SetParent(transform);

                    RectTransform valueRect = valueObj.AddComponent<RectTransform>();
                    valueRect.anchorMin = new Vector2(1, 0.5f);
                    valueRect.anchorMax = new Vector2(1, 0.5f);
                    valueRect.pivot = new Vector2(0, 0.5f);
                    valueRect.sizeDelta = new Vector2(50, 20);
                    valueRect.anchoredPosition = new Vector2(5, 0);

                    m_valueText = valueObj.AddComponent<TextMeshProUGUI>();
                    m_valueText.fontSize = 14;
                    m_valueText.alignment = TextAlignmentOptions.Left;
                    m_valueText.color = Color.white;
                }
            }

            // 更新标签和数值显示
            UpdateVisuals();
        }

        /// <summary>
        /// 更新视觉效果
        /// </summary>
        private void UpdateVisuals()
        {
            // 计算填充比例
            float fillAmount = (m_value - m_minValue) / (m_maxValue - m_minValue);

            // 根据方向调整填充和滑块位置
            if (m_rectTransform != null)
            {
                Vector2 size = m_rectTransform.sizeDelta;

                switch (m_direction)
                {
                    case Direction.LeftToRight:
                        if (m_fill != null)
                        {
                            RectTransform fillRect = m_fill.rectTransform;
                            fillRect.anchorMin = new Vector2(0, 0);
                            fillRect.anchorMax = new Vector2(0, 1);
                            fillRect.pivot = new Vector2(0, 0.5f);
                            fillRect.sizeDelta = new Vector2(size.x * fillAmount, 0);
                        }

                        if (m_handle != null)
                        {
                            m_handle.anchoredPosition = new Vector2(size.x * fillAmount, 0);
                        }
                        break;

                    case Direction.RightToLeft:
                        if (m_fill != null)
                        {
                            RectTransform fillRect = m_fill.rectTransform;
                            fillRect.anchorMin = new Vector2(1, 0);
                            fillRect.anchorMax = new Vector2(1, 1);
                            fillRect.pivot = new Vector2(1, 0.5f);
                            fillRect.sizeDelta = new Vector2(size.x * fillAmount, 0);
                        }

                        if (m_handle != null)
                        {
                            m_handle.anchoredPosition = new Vector2(-size.x * fillAmount, 0);
                        }
                        break;

                    case Direction.BottomToTop:
                        if (m_fill != null)
                        {
                            RectTransform fillRect = m_fill.rectTransform;
                            fillRect.anchorMin = new Vector2(0, 0);
                            fillRect.anchorMax = new Vector2(1, 0);
                            fillRect.pivot = new Vector2(0.5f, 0);
                            fillRect.sizeDelta = new Vector2(0, size.y * fillAmount);
                        }

                        if (m_handle != null)
                        {
                            m_handle.anchoredPosition = new Vector2(0, size.y * fillAmount);
                        }
                        break;

                    case Direction.TopToBottom:
                        if (m_fill != null)
                        {
                            RectTransform fillRect = m_fill.rectTransform;
                            fillRect.anchorMin = new Vector2(0, 1);
                            fillRect.anchorMax = new Vector2(1, 1);
                            fillRect.pivot = new Vector2(0.5f, 1);
                            fillRect.sizeDelta = new Vector2(0, size.y * fillAmount);
                        }

                        if (m_handle != null)
                        {
                            m_handle.anchoredPosition = new Vector2(0, -size.y * fillAmount);
                        }
                        break;
                }
            }

            // 更新数值文本
            if (m_valueText != null && m_showValueText)
            {
                m_valueText.text = string.Format(m_valueFormat, m_value);
            }
        }

        /// <summary>
        /// 更新滑块位置
        /// </summary>
        private void UpdateHandlePosition()
        {
            if (m_handle == null || m_rectTransform == null)
                return;

            // 计算填充比例
            float fillAmount = (m_value - m_minValue) / (m_maxValue - m_minValue);
            Vector2 size = m_rectTransform.sizeDelta;

            // 根据方向设置滑块位置
            switch (m_direction)
            {
                case Direction.LeftToRight:
                    m_handle.anchoredPosition = new Vector2(size.x * fillAmount, 0);
                    break;
                case Direction.RightToLeft:
                    m_handle.anchoredPosition = new Vector2(-size.x * fillAmount, 0);
                    break;
                case Direction.BottomToTop:
                    m_handle.anchoredPosition = new Vector2(0, size.y * fillAmount);
                    break;
                case Direction.TopToBottom:
                    m_handle.anchoredPosition = new Vector2(0, -size.y * fillAmount);
                    break;
            }
        }

        /// <summary>
        /// 更新填充区域
        /// </summary>
        private void UpdateFillArea()
        {
            if (m_fill == null || m_rectTransform == null)
                return;

            // 计算填充比例
            float fillAmount = (m_value - m_minValue) / (m_maxValue - m_minValue);
            Vector2 size = m_rectTransform.sizeDelta;

            // 根据方向设置填充区域
            switch (m_direction)
            {
                case Direction.LeftToRight:
                    RectTransform fillRect = m_fill.rectTransform;
                    fillRect.anchorMin = new Vector2(0, 0);
                    fillRect.anchorMax = new Vector2(0, 1);
                    fillRect.pivot = new Vector2(0, 0.5f);
                    fillRect.sizeDelta = new Vector2(size.x * fillAmount, 0);
                    break;
                case Direction.RightToLeft:
                    RectTransform fillRectRL = m_fill.rectTransform;
                    fillRectRL.anchorMin = new Vector2(1, 0);
                    fillRectRL.anchorMax = new Vector2(1, 1);
                    fillRectRL.pivot = new Vector2(1, 0.5f);
                    fillRectRL.sizeDelta = new Vector2(size.x * fillAmount, 0);
                    break;
                case Direction.BottomToTop:
                    RectTransform fillRectBT = m_fill.rectTransform;
                    fillRectBT.anchorMin = new Vector2(0, 0);
                    fillRectBT.anchorMax = new Vector2(1, 0);
                    fillRectBT.pivot = new Vector2(0.5f, 0);
                    fillRectBT.sizeDelta = new Vector2(0, size.y * fillAmount);
                    break;
                case Direction.TopToBottom:
                    RectTransform fillRectTB = m_fill.rectTransform;
                    fillRectTB.anchorMin = new Vector2(0, 1);
                    fillRectTB.anchorMax = new Vector2(1, 1);
                    fillRectTB.pivot = new Vector2(0.5f, 1);
                    fillRectTB.sizeDelta = new Vector2(0, size.y * fillAmount);
                    break;
            }
        }

        /// <summary>
        /// 更新数值文本
        /// </summary>
        private void UpdateValueText()
        {
            if (m_valueText != null && m_showValueText)
            {
                m_valueText.text = string.Format(m_valueFormat, m_value);
            }
        }

        /// <summary>
        /// 从指针位置更新值
        /// </summary>
        private void UpdateValueFromPointer()
        {
            if (m_rectTransform == null)
                return;

            // 获取VR指针位置
            Vector3 pointerPosition = GetVRPointerPosition();

            // 转换为本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_rectTransform, pointerPosition, Camera.main, out Vector2 localPoint);

            // 根据方向计算值
            float normalizedValue = 0f;
            Vector2 size = m_rectTransform.sizeDelta;

            switch (m_direction)
            {
                case Direction.LeftToRight:
                    normalizedValue = Mathf.Clamp01((localPoint.x + size.x / 2) / size.x);
                    break;

                case Direction.RightToLeft:
                    normalizedValue = Mathf.Clamp01(1 - (localPoint.x + size.x / 2) / size.x);
                    break;

                case Direction.BottomToTop:
                    normalizedValue = Mathf.Clamp01((localPoint.y + size.y / 2) / size.y);
                    break;

                case Direction.TopToBottom:
                    normalizedValue = Mathf.Clamp01(1 - (localPoint.y + size.y / 2) / size.y);
                    break;
            }

            // 设置值
            SetValue(m_minValue + normalizedValue * (m_maxValue - m_minValue));
        }

        /// <summary>
        /// 获取VR指针位置
        /// </summary>
        private Vector3 GetVRPointerPosition()
        {
            // 尝试从VR交互管理器获取射线位置
            var vrInteractionManager = FindObjectOfType<PongHub.VR.VRInteractionManager>();
            if (vrInteractionManager != null)
            {
                // 检查左右手射线交互器
                if (vrInteractionManager.LeftRayInteractor != null &&
                    vrInteractionManager.LeftRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit leftHit))
                {
                    return leftHit.point;
                }

                if (vrInteractionManager.RightRayInteractor != null &&
                    vrInteractionManager.RightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rightHit))
                {
                    return rightHit.point;
                }
            }

            // 如果无法获取VR指针位置，回退到鼠标位置（用于非VR测试）
            return UnityEngine.Input.mousePosition;
        }

        #endregion
    }
}