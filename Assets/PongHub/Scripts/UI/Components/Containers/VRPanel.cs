using UnityEngine;
using UnityEngine.UI;
using PongHub.UI.Core;
using System.Collections.Generic;
using TMPro;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR面板组件
    /// 实现VR环境中的面板容器功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRPanel : VRUIComponent
    {
        [Header("面板设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Panel background image")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Use Background / 使用背景 - Whether to display a background")]
        private bool m_useBackground = true;

        [SerializeField]
        [Tooltip("Background Color / 背景颜色 - Color of the panel background")]
        private Color m_backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        [SerializeField]
        [Tooltip("Border / 边框 - Whether to display a border")]
        private bool m_useBorder = false;

        [SerializeField]
        [Tooltip("Border Color / 边框颜色 - Color of the panel border")]
        private Color m_borderColor = new Color(1f, 1f, 1f, 0.5f);

        [SerializeField]
        [Tooltip("Border Width / 边框宽度 - Width of the panel border")]
        private float m_borderWidth = 2f;

        [SerializeField]
        [Tooltip("Corner Radius / 圆角半径 - Radius of the panel corners")]
        private float m_cornerRadius = 10f;

        [SerializeField]
        [Tooltip("Padding / 内边距 - Padding inside the panel")]
        private RectOffset m_padding = new RectOffset(10, 10, 10, 10);

        [SerializeField]
        [Tooltip("Layout Group / 布局组 - Layout group for arranging child elements")]
        private UnityEngine.UI.LayoutGroup m_layoutGroup;

        [SerializeField]
        [Tooltip("Content Transform / 内容变换 - Transform for panel content")]
        private RectTransform m_contentTransform;

        [SerializeField]
        [Tooltip("Draggable / 可拖动 - Whether the panel can be dragged")]
        private bool m_draggable = false;

        [SerializeField]
        [Tooltip("Drag Handle / 拖动把手 - Area that can be used to drag the panel")]
        private RectTransform m_dragHandle;

        [Header("视觉效果")]
        [SerializeField]
        [Tooltip("Shadow / 阴影 - Whether to display a shadow")]
        private bool m_useShadow = false;

        [SerializeField]
        [Tooltip("Shadow Color / 阴影颜色 - Color of the shadow")]
        private Color m_shadowColor = new Color(0f, 0f, 0f, 0.5f);

        [SerializeField]
        [Tooltip("Shadow Offset / 阴影偏移 - Offset of the shadow")]
        private Vector2 m_shadowOffset = new Vector2(5f, -5f);

        [SerializeField]
        [Tooltip("Shadow Blur / 阴影模糊 - Blur radius of the shadow")]
        private float m_shadowBlur = 5f;

        // 缓存的RectTransform
        private RectTransform m_rectTransform;

        // 子组件列表
        private List<VRUIComponent> m_childComponents = new List<VRUIComponent>();

        // 拖动相关
        private bool m_isDragging = false;
        private Vector3 m_dragStartPosition;
        private Vector3 m_pointerStartPosition;

        // 阴影组件
        private Image m_shadowImage;

        // 公开属性
        public TextMeshProUGUI titleText { get; private set; }
        public RectTransform contentArea { get; private set; }

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

            // 收集子组件
            CollectChildComponents();
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
            // 处理拖动
            if (m_isDragging)
            {
                UpdateDragPosition();
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 添加子组件
        /// </summary>
        public void AddChild(VRUIComponent child)
        {
            if (child != null && !m_childComponents.Contains(child))
            {
                // 设置父级
                if (m_contentTransform != null)
                {
                    child.transform.SetParent(m_contentTransform, false);
                }
                else
                {
                    child.transform.SetParent(transform, false);
                }

                // 添加到列表
                m_childComponents.Add(child);

                // 应用主题
                if (m_theme != null)
                {
                    child.SetTheme(m_theme);
                }
            }
        }

        /// <summary>
        /// 移除子组件
        /// </summary>
        public void RemoveChild(VRUIComponent child)
        {
            if (child != null && m_childComponents.Contains(child))
            {
                m_childComponents.Remove(child);
            }
        }

        /// <summary>
        /// 清除所有子组件
        /// </summary>
        public void ClearChildren()
        {
            // 清除子组件列表
            m_childComponents.Clear();

            // 销毁内容下的所有子物体
            if (m_contentTransform != null)
            {
                foreach (Transform child in m_contentTransform)
                {
                    Destroy(child.gameObject);
                }
            }
            else
            {
                foreach (Transform child in transform)
                {
                    // 不销毁背景和内容
                    if ((m_background != null && child == m_background.transform) ||
                        (m_shadowImage != null && child == m_shadowImage.transform) ||
                        (m_dragHandle != null && child == m_dragHandle))
                        continue;

                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            m_backgroundColor = color;
            if (m_background != null)
            {
                m_background.color = m_backgroundColor;
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
        /// 设置是否使用边框
        /// </summary>
        public void SetUseBorder(bool useBorder)
        {
            m_useBorder = useBorder;
            UpdateBorder();
        }

        /// <summary>
        /// 设置边框颜色
        /// </summary>
        public void SetBorderColor(Color color)
        {
            m_borderColor = color;
            UpdateBorder();
        }

        /// <summary>
        /// 设置边框宽度
        /// </summary>
        public void SetBorderWidth(float width)
        {
            m_borderWidth = width;
            UpdateBorder();
        }

        /// <summary>
        /// 设置圆角半径
        /// </summary>
        public void SetCornerRadius(float radius)
        {
            m_cornerRadius = radius;
            UpdateCornerRadius();
        }

        /// <summary>
        /// 设置内边距
        /// </summary>
        public void SetPadding(RectOffset padding)
        {
            m_padding = padding;
            UpdatePadding();
        }

        /// <summary>
        /// 设置是否可拖动
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            m_draggable = draggable;
        }

        /// <summary>
        /// 设置是否使用阴影
        /// </summary>
        public void SetUseShadow(bool useShadow)
        {
            m_useShadow = useShadow;
            UpdateShadow();
        }

        /// <summary>
        /// 设置阴影颜色
        /// </summary>
        public void SetShadowColor(Color color)
        {
            m_shadowColor = color;
            UpdateShadow();
        }

        /// <summary>
        /// 设置阴影偏移
        /// </summary>
        public void SetShadowOffset(Vector2 offset)
        {
            m_shadowOffset = offset;
            UpdateShadow();
        }

        /// <summary>
        /// 设置面板标题
        /// </summary>
        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        #endregion

        #region VRUIComponent实现

        /// <summary>
        /// 设置主题
        /// </summary>
        public override void SetTheme(VRUITheme theme)
        {
            base.SetTheme(theme);

            // 应用主题到所有子组件
            foreach (var child in m_childComponents)
            {
                if (child != null)
                {
                    child.SetTheme(theme);
                }
            }

            // 更新视觉状态
            UpdateVisualState(m_currentState);
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

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.backgroundColor;

                // 使用主题的accentColor作为边框颜色
                if (m_borderImage != null)
                {
                    m_borderImage.color = m_theme.accentColor;
                }

                m_shadowColor = new Color(0f, 0f, 0f, 0.5f);
            }
            else
            {
                // 默认颜色
                backgroundColor = m_backgroundColor;
            }

            // 应用颜色
            if (m_background != null)
            {
                m_background.color = backgroundColor;
                m_background.gameObject.SetActive(m_useBackground);
            }

            // 更新边框
            UpdateBorder();

            // 更新阴影
            UpdateShadow();
        }

        /// <summary>
        /// 指针按下
        /// </summary>
        public override void OnPointerDown()
        {
            base.OnPointerDown();

            if (!m_interactable || !m_draggable)
                return;

            // 获取VR指针位置
            Vector3 position = GetVRPointerPosition();

            // 检查是否点击在拖动把手上
            if (m_dragHandle != null)
            {
                // 转换位置到把手的本地坐标
                Vector3 localPos;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    m_dragHandle, position, Camera.main, out localPos);

                // 检查是否在把手的矩形内
                Rect rect = m_dragHandle.rect;
                Vector2 localPos2D = m_dragHandle.InverseTransformPoint(localPos);
                if (!rect.Contains(localPos2D))
                    return;
            }

            // 开始拖动
            m_isDragging = true;
            m_dragStartPosition = transform.position;
            m_pointerStartPosition = position;
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

        /// <summary>
        /// 指针抬起
        /// </summary>
        public override void OnPointerUp()
        {
            base.OnPointerUp();

            // 结束拖动
            m_isDragging = false;
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
                m_rectTransform.sizeDelta = new Vector2(300, 200);
            }

            // 创建阴影
            if (m_useShadow && m_shadowImage == null)
            {
                GameObject shadowObj = new GameObject("Shadow");
                shadowObj.transform.SetParent(transform);
                shadowObj.transform.SetAsFirstSibling(); // 确保阴影在最底层

                RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = new Vector2(-m_shadowBlur, -m_shadowBlur) + m_shadowOffset;
                shadowRect.offsetMax = new Vector2(m_shadowBlur, m_shadowBlur) + m_shadowOffset;

                m_shadowImage = shadowObj.AddComponent<Image>();
                m_shadowImage.color = m_shadowColor;
                m_shadowImage.sprite = GetRoundedSprite(m_cornerRadius + m_shadowBlur);
            }

            // 如果没有背景组件，创建一个
            if (m_background == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);
                bgObj.transform.SetAsFirstSibling(); // 确保背景在最底层（但在阴影之上）

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                m_background = bgObj.AddComponent<Image>();
                m_background.color = m_backgroundColor;

                // 设置圆角
                if (m_cornerRadius > 0)
                {
                    m_background.sprite = GetRoundedSprite(m_cornerRadius);
                }
            }

            // 如果没有内容变换，创建一个
            if (m_contentTransform == null)
            {
                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(transform);

                m_contentTransform = contentObj.AddComponent<RectTransform>();
                m_contentTransform.anchorMin = Vector2.zero;
                m_contentTransform.anchorMax = Vector2.one;
                m_contentTransform.offsetMin = new Vector2(m_padding.left, m_padding.bottom);
                m_contentTransform.offsetMax = new Vector2(-m_padding.right, -m_padding.top);
            }

            // 如果没有布局组，但需要一个，创建一个垂直布局组
            if (m_layoutGroup == null && m_contentTransform != null)
            {
                m_layoutGroup = m_contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
                ((VerticalLayoutGroup)m_layoutGroup).childAlignment = TextAnchor.UpperCenter;
                ((VerticalLayoutGroup)m_layoutGroup).spacing = 10;
                ((VerticalLayoutGroup)m_layoutGroup).childForceExpandWidth = true;
                ((VerticalLayoutGroup)m_layoutGroup).childForceExpandHeight = false;
            }

            // 如果没有拖动把手，但面板可拖动，创建一个
            if (m_draggable && m_dragHandle == null)
            {
                GameObject handleObj = new GameObject("DragHandle");
                handleObj.transform.SetParent(transform);

                m_dragHandle = handleObj.AddComponent<RectTransform>();
                m_dragHandle.anchorMin = new Vector2(0, 1);
                m_dragHandle.anchorMax = new Vector2(1, 1);
                m_dragHandle.pivot = new Vector2(0.5f, 1);
                m_dragHandle.sizeDelta = new Vector2(0, 30);

                // 添加一个透明的图像组件，用于检测点击
                Image handleImage = handleObj.AddComponent<Image>();
                handleImage.color = new Color(0, 0, 0, 0);
            }

            // 更新边框
            UpdateBorder();

            // 更新阴影
            UpdateShadow();

            // 更新内边距
            UpdatePadding();
        }

        /// <summary>
        /// 收集子组件
        /// </summary>
        private void CollectChildComponents()
        {
            m_childComponents.Clear();

            // 收集内容下的所有VRUIComponent
            if (m_contentTransform != null)
            {
                VRUIComponent[] components = m_contentTransform.GetComponentsInChildren<VRUIComponent>(true);
                foreach (var component in components)
                {
                    if (component != this && !m_childComponents.Contains(component))
                    {
                        m_childComponents.Add(component);
                    }
                }
            }
            else
            {
                VRUIComponent[] components = GetComponentsInChildren<VRUIComponent>(true);
                foreach (var component in components)
                {
                    if (component != this && !m_childComponents.Contains(component))
                    {
                        m_childComponents.Add(component);
                    }
                }
            }

            // 应用主题到所有子组件
            if (m_theme != null)
            {
                foreach (var child in m_childComponents)
                {
                    if (child != null)
                    {
                        child.SetTheme(m_theme);
                    }
                }
            }
        }

        /// <summary>
        /// 更新边框
        /// </summary>
        private void UpdateBorder()
        {
            if (m_background != null)
            {
                if (m_useBorder)
                {
                    m_background.material = new Material(Shader.Find("UI/Default"));
                    m_background.material.SetColor("_BorderColor", m_borderColor);
                    m_background.material.SetFloat("_BorderWidth", m_borderWidth);
                }
                else
                {
                    m_background.material = null;
                }
            }
        }

        /// <summary>
        /// 更新圆角半径
        /// </summary>
        private void UpdateCornerRadius()
        {
            if (m_background != null && m_cornerRadius > 0)
            {
                m_background.sprite = GetRoundedSprite(m_cornerRadius);
            }
        }

        /// <summary>
        /// 更新内边距
        /// </summary>
        private void UpdatePadding()
        {
            if (m_contentTransform != null)
            {
                m_contentTransform.offsetMin = new Vector2(m_padding.left, m_padding.bottom);
                m_contentTransform.offsetMax = new Vector2(-m_padding.right, -m_padding.top);
            }

            // 如果有布局组，更新其内边距
            if (m_layoutGroup is VerticalLayoutGroup verticalLayout)
            {
                verticalLayout.padding = m_padding;
            }
            else if (m_layoutGroup is HorizontalLayoutGroup horizontalLayout)
            {
                horizontalLayout.padding = m_padding;
            }
            else if (m_layoutGroup is GridLayoutGroup gridLayout)
            {
                gridLayout.padding = m_padding;
            }
        }

        /// <summary>
        /// 更新阴影
        /// </summary>
        private void UpdateShadow()
        {
            if (m_shadowImage != null)
            {
                m_shadowImage.gameObject.SetActive(m_useShadow);

                if (m_useShadow)
                {
                    m_shadowImage.color = m_shadowColor;

                    RectTransform shadowRect = m_shadowImage.rectTransform;
                    shadowRect.offsetMin = new Vector2(-m_shadowBlur, -m_shadowBlur) + m_shadowOffset;
                    shadowRect.offsetMax = new Vector2(m_shadowBlur, m_shadowBlur) + m_shadowOffset;

                    // 更新圆角
                    m_shadowImage.sprite = GetRoundedSprite(m_cornerRadius + m_shadowBlur);
                }
            }
        }

        /// <summary>
        /// 更新拖动位置
        /// </summary>
        private void UpdateDragPosition()
        {
            if (!m_isDragging)
                return;

            // 获取当前VR指针位置
            Vector3 pointerPosition = GetVRPointerPosition();

            // 计算偏移
            Vector3 offset = pointerPosition - m_pointerStartPosition;

            // 应用新位置
            transform.position = m_dragStartPosition + offset;
        }

        /// <summary>
        /// 获取圆角精灵
        /// </summary>
        private Sprite GetRoundedSprite(float radius)
        {
            // 在实际项目中，应该使用一个圆角精灵或者自定义着色器
            // 这里简单返回null，实际使用时需要替换为真实实现
            return null;
        }

        #endregion
    }
}