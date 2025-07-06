using UnityEngine;
using UnityEngine.UI;
using PongHub.UI.Core;
using System.Collections.Generic;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR布局组组件
    /// 实现VR环境中的UI元素布局功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRLayoutGroup : VRUIComponent
    {
        public enum LayoutType
        {
            Vertical,
            Horizontal,
            Grid
        }

        [Header("布局设置")]
        [SerializeField]
        [Tooltip("Layout Type / 布局类型 - Type of layout to use")]
        private LayoutType m_layoutType = LayoutType.Vertical;

        [SerializeField]
        [Tooltip("Spacing / 间距 - Spacing between elements")]
        private float m_spacing = 10f;

        [SerializeField]
        [Tooltip("Padding / 内边距 - Padding inside the layout")]
        private RectOffset m_padding = new RectOffset(5, 5, 5, 5);

        [SerializeField]
        [Tooltip("Child Alignment / 子对齐 - Alignment of children within the layout")]
        private TextAnchor m_childAlignment = TextAnchor.UpperLeft;

        [SerializeField]
        [Tooltip("Force Expand Width / 强制扩展宽度 - Whether to force children to expand horizontally")]
        private bool m_childForceExpandWidth = true;

        [SerializeField]
        [Tooltip("Force Expand Height / 强制扩展高度 - Whether to force children to expand vertically")]
        private bool m_childForceExpandHeight = false;

        [SerializeField]
        [Tooltip("Control Child Size / 控制子物体大小 - Whether to control child width/height")]
        private bool m_controlChildSize = false;

        [SerializeField]
        [Tooltip("Child Width / 子物体宽度 - Width to set for children")]
        private float m_childWidth = 100f;

        [SerializeField]
        [Tooltip("Child Height / 子物体高度 - Height to set for children")]
        private float m_childHeight = 30f;

        [Header("网格布局设置")]
        [SerializeField]
        [Tooltip("Cell Size / 单元格大小 - Size of each grid cell")]
        private Vector2 m_cellSize = new Vector2(100f, 100f);

        [SerializeField]
        [Tooltip("Constraint / 约束 - Constraint to apply to the grid")]
        private GridLayoutGroup.Constraint m_constraint = GridLayoutGroup.Constraint.Flexible;

        [SerializeField]
        [Tooltip("Constraint Count / 约束数量 - Number of elements in the constrained axis")]
        private int m_constraintCount = 2;

        [SerializeField]
        [Tooltip("Start Corner / 起始角落 - Corner where the first element is placed")]
        private GridLayoutGroup.Corner m_startCorner = GridLayoutGroup.Corner.UpperLeft;

        [SerializeField]
        [Tooltip("Start Axis / 起始轴 - Axis to place elements along first")]
        private GridLayoutGroup.Axis m_startAxis = GridLayoutGroup.Axis.Horizontal;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Background / 背景 - Layout group background")]
        private Image m_background;

        [SerializeField]
        [Tooltip("Use Background / 使用背景 - Whether to display a background")]
        private bool m_useBackground = false;

        [SerializeField]
        [Tooltip("Background Color / 背景颜色 - Color of the layout background")]
        private Color m_backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // 公开属性
        public RectTransform contentArea { get; private set; }

        // 布局组组件
        private LayoutGroup m_layoutGroup;

        // 子组件列表
        private List<VRUIComponent> m_childComponents = new List<VRUIComponent>();

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
                child.transform.SetParent(transform, false);

                // 添加到列表
                m_childComponents.Add(child);

                // 应用主题
                if (m_theme != null)
                {
                    child.SetTheme(m_theme);
                }

                // 如果控制子物体大小
                if (m_controlChildSize)
                {
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        childRect.sizeDelta = new Vector2(m_childWidth, m_childHeight);
                    }
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

            // 销毁所有子物体
            foreach (Transform child in transform)
            {
                // 不销毁背景
                if (m_background != null && child == m_background.transform)
                    continue;

                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 设置布局类型
        /// </summary>
        public void SetLayoutType(LayoutType layoutType)
        {
            if (m_layoutType == layoutType)
                return;

            m_layoutType = layoutType;
            UpdateLayoutGroup();
        }

        /// <summary>
        /// 设置间距
        /// </summary>
        public void SetSpacing(float spacing)
        {
            m_spacing = spacing;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置内边距
        /// </summary>
        public void SetPadding(RectOffset padding)
        {
            m_padding = padding;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置子对齐方式
        /// </summary>
        public void SetChildAlignment(TextAnchor alignment)
        {
            m_childAlignment = alignment;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置是否强制扩展宽度
        /// </summary>
        public void SetChildForceExpandWidth(bool expand)
        {
            m_childForceExpandWidth = expand;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置是否强制扩展高度
        /// </summary>
        public void SetChildForceExpandHeight(bool expand)
        {
            m_childForceExpandHeight = expand;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置是否控制子物体大小
        /// </summary>
        public void SetControlChildSize(bool control)
        {
            m_controlChildSize = control;
            UpdateChildSizes();
        }

        /// <summary>
        /// 设置子物体大小
        /// </summary>
        public void SetChildSize(float width, float height)
        {
            m_childWidth = width;
            m_childHeight = height;

            if (m_controlChildSize)
            {
                UpdateChildSizes();
            }
        }

        /// <summary>
        /// 设置单元格大小（仅网格布局）
        /// </summary>
        public void SetCellSize(Vector2 size)
        {
            m_cellSize = size;
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 设置约束（仅网格布局）
        /// </summary>
        public void SetConstraint(GridLayoutGroup.Constraint constraint, int count)
        {
            m_constraint = constraint;
            m_constraintCount = count;
            UpdateLayoutGroupSettings();
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
                backgroundColor = new Color(
                    m_theme.backgroundColor.r,
                    m_theme.backgroundColor.g,
                    m_theme.backgroundColor.b,
                    m_useBackground ? 0.5f : 0f);
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

            // 如果没有背景组件，创建一个
            if (m_background == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform);
                bgObj.transform.SetAsFirstSibling(); // 确保背景在最底层

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                m_background = bgObj.AddComponent<Image>();
                m_background.color = m_backgroundColor;
                m_background.gameObject.SetActive(m_useBackground);
            }

            // 创建或更新布局组
            UpdateLayoutGroup();
        }

        /// <summary>
        /// 更新布局组
        /// </summary>
        private void UpdateLayoutGroup()
        {
            // 移除现有布局组
            if (m_layoutGroup != null)
            {
                Destroy(m_layoutGroup);
                m_layoutGroup = null;
            }

            // 创建新布局组
            switch (m_layoutType)
            {
                case LayoutType.Vertical:
                    m_layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
                    break;
                case LayoutType.Horizontal:
                    m_layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
                    break;
                case LayoutType.Grid:
                    m_layoutGroup = gameObject.AddComponent<GridLayoutGroup>();
                    break;
            }

            // 更新布局组设置
            UpdateLayoutGroupSettings();
        }

        /// <summary>
        /// 更新布局组设置
        /// </summary>
        private void UpdateLayoutGroupSettings()
        {
            if (m_layoutGroup == null)
                return;

            // 设置通用属性
            m_layoutGroup.padding = m_padding;
            m_layoutGroup.childAlignment = m_childAlignment;

            // 根据布局类型设置特定属性
            if (m_layoutGroup is HorizontalOrVerticalLayoutGroup hvGroup)
            {
                hvGroup.spacing = m_spacing;
                hvGroup.childForceExpandWidth = m_childForceExpandWidth;
                hvGroup.childForceExpandHeight = m_childForceExpandHeight;
                hvGroup.childControlWidth = m_controlChildSize;
                hvGroup.childControlHeight = m_controlChildSize;
            }
            else if (m_layoutGroup is GridLayoutGroup gridGroup)
            {
                gridGroup.cellSize = m_cellSize;
                gridGroup.spacing = new Vector2(m_spacing, m_spacing);
                gridGroup.constraint = m_constraint;
                gridGroup.constraintCount = m_constraintCount;
                gridGroup.startCorner = m_startCorner;
                gridGroup.startAxis = m_startAxis;
            }
        }

        /// <summary>
        /// 收集子组件
        /// </summary>
        private void CollectChildComponents()
        {
            m_childComponents.Clear();

            // 收集所有VRUIComponent
            VRUIComponent[] components = GetComponentsInChildren<VRUIComponent>(true);
            foreach (var component in components)
            {
                if (component != this && !m_childComponents.Contains(component))
                {
                    m_childComponents.Add(component);
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

            // 如果控制子物体大小
            if (m_controlChildSize)
            {
                UpdateChildSizes();
            }
        }

        /// <summary>
        /// 更新子物体大小
        /// </summary>
        private void UpdateChildSizes()
        {
            if (!m_controlChildSize)
                return;

            foreach (var child in m_childComponents)
            {
                if (child != null)
                {
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        childRect.sizeDelta = new Vector2(m_childWidth, m_childHeight);
                    }
                }
            }
        }

        #endregion
    }
}