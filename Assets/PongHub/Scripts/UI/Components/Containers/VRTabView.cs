using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR标签页组件
    /// 实现VR环境中的标签页切换功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRTabView : VRUIComponent
    {
        [System.Serializable]
        public class TabData
        {
            [Tooltip("Tab Title / 标签标题")]
            public string title;

            [Tooltip("Tab Icon / 标签图标")]
            public Sprite icon;

            [Tooltip("Tab Content / 标签内容")]
            public GameObject content;

            [Tooltip("Is Active / 是否激活")]
            public bool isActive;

            public TabData()
            {
                title = "Tab";
                icon = null;
                content = null;
                isActive = false;
            }

            public TabData(string title, GameObject content)
            {
                this.title = title;
                this.icon = null;
                this.content = content;
                this.isActive = false;
            }

            public TabData(string title, Sprite icon, GameObject content)
            {
                this.title = title;
                this.icon = icon;
                this.content = content;
                this.isActive = false;
            }
        }

        [Header("标签页设置")]
        [SerializeField]
        [Tooltip("Tabs / 标签列表")]
        private List<TabData> m_Tabs = new List<TabData>();

        [SerializeField]
        [Tooltip("Selected Tab / 选中的标签 - Index of the currently selected tab")]
        private int m_SelectedTab = 0;

        [SerializeField]
        [Tooltip("Tab Position / 标签位置 - Position of the tab buttons")]
        private TabPosition m_TabPosition = TabPosition.Top;

        [SerializeField]
        [Tooltip("Tab Size / 标签大小 - Size of each tab button")]
        private Vector2 m_TabSize = new Vector2(100, 40);

        [SerializeField]
        [Tooltip("Tab Spacing / 标签间距 - Spacing between tab buttons")]
        private float m_TabSpacing = 2f;

        [SerializeField]
        [Tooltip("Use Equal Tab Widths / 使用等宽标签 - Whether all tabs should have the same width")]
        private bool m_UseEqualTabWidths = true;

        [SerializeField]
        [Tooltip("Show Icons / 显示图标 - Whether to show icons in tab buttons")]
        private bool m_ShowIcons = true;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Tab Bar / 标签栏 - Container for tab buttons")]
        private RectTransform m_TabBar;

        [SerializeField]
        [Tooltip("Content Area / 内容区域 - Container for tab content")]
        private RectTransform m_ContentArea;

        [SerializeField]
        [Tooltip("Tab Button Prefab / 标签按钮预制体 - Prefab for tab buttons")]
        private VRButton m_TabButtonPrefab;

        [SerializeField]
        [Tooltip("Active Tab Color / 激活标签颜色 - Color for the active tab")]
        private Color m_ActiveTabColor = new Color(0.3f, 0.5f, 0.9f, 1f);

        [SerializeField]
        [Tooltip("Inactive Tab Color / 未激活标签颜色 - Color for inactive tabs")]
        private Color m_InactiveTabColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [SerializeField]
        [Tooltip("Tab Transition Duration / 标签过渡时长 - Duration of tab transition animation")]
        private float m_TabTransitionDuration = 0.3f;

        [SerializeField]
        [Tooltip("Use Fade Transition / 使用淡入淡出过渡 - Whether to use fade transition for tab content")]
        private bool m_UseFadeTransition = true;

        [SerializeField]
        [Tooltip("Use Slide Transition / 使用滑动过渡 - Whether to use slide transition for tab content")]
        private bool m_UseSlideTransition = false;

        [SerializeField]
        [Tooltip("Background / 背景 - Background for the tab view")]
        private Image m_Background;

        [SerializeField]
        [Tooltip("Tab Indicator / 标签指示器 - Visual indicator for the active tab")]
        private RectTransform m_TabIndicator;

        // 事件
        [Header("事件")]
        public UnityEvent<int> OnTabChanged = new UnityEvent<int>();

        // 内部变量
        private List<VRButton> m_TabButtons = new List<VRButton>();
        private RectTransform m_rectTransform;
        private Coroutine m_TransitionCoroutine;
        private int m_PreviousTab = -1;

        // 标签位置枚举
        public enum TabPosition
        {
            Top,
            Bottom,
            Left,
            Right
        }

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
            // 创建标签按钮
            CreateTabButtons();

            // 选择初始标签
            SelectTab(m_SelectedTab, false);
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
        /// 添加标签
        /// </summary>
        public void AddTab(string title, GameObject content, Sprite icon = null)
        {
            TabData tab = new TabData(title, icon, content);
            m_Tabs.Add(tab);

            // 如果是第一个标签，设置为激活状态
            if (m_Tabs.Count == 1)
            {
                tab.isActive = true;
                m_SelectedTab = 0;
            }

            // 重新创建标签按钮
            CreateTabButtons();

            // 如果只有一个标签，选择它
            if (m_Tabs.Count == 1)
            {
                SelectTab(0, false);
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public void RemoveTab(int index)
        {
            if (index < 0 || index >= m_Tabs.Count)
                return;

            // 如果移除的是当前选中的标签，选择另一个标签
            if (index == m_SelectedTab)
            {
                int newIndex = index > 0 ? index - 1 : (m_Tabs.Count > 1 ? 1 : -1);
                if (newIndex >= 0)
                {
                    SelectTab(newIndex, false);
                }
            }
            else if (index < m_SelectedTab)
            {
                // 如果移除的标签在当前选中标签之前，调整选中索引
                m_SelectedTab--;
            }

            // 移除标签
            m_Tabs.RemoveAt(index);

            // 重新创建标签按钮
            CreateTabButtons();
        }

        /// <summary>
        /// 选择标签
        /// </summary>
        public void SelectTab(int index, bool animate = true)
        {
            if (index < 0 || index >= m_Tabs.Count || index == m_SelectedTab)
                return;

            // 保存之前的标签索引
            m_PreviousTab = m_SelectedTab;

            // 更新标签状态
            for (int i = 0; i < m_Tabs.Count; i++)
            {
                m_Tabs[i].isActive = (i == index);

                // 更新内容可见性
                if (m_Tabs[i].content != null)
                {
                    if (animate && i == index && m_UseFadeTransition)
                    {
                        // 使用淡入效果显示新标签
                        m_Tabs[i].content.SetActive(true);
                        CanvasGroup canvasGroup = m_Tabs[i].content.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                        {
                            canvasGroup = m_Tabs[i].content.AddComponent<CanvasGroup>();
                        }
                        canvasGroup.alpha = 0f;
                        // 在实际项目中，可以使用协程或DoTween等动画库实现平滑过渡
                        canvasGroup.alpha = 1f;
                    }
                    else if (animate && i == m_PreviousTab && m_UseFadeTransition)
                    {
                        // 使用淡出效果隐藏旧标签
                        CanvasGroup canvasGroup = m_Tabs[i].content.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                        {
                            canvasGroup = m_Tabs[i].content.AddComponent<CanvasGroup>();
                        }
                        canvasGroup.alpha = 1f;
                        // 在实际项目中，可以使用协程或DoTween等动画库实现平滑过渡
                        canvasGroup.alpha = 0f;
                        m_Tabs[i].content.SetActive(false);
                    }
                    else
                    {
                        // 直接切换可见性
                        m_Tabs[i].content.SetActive(i == index);
                    }
                }
            }

            // 更新选中的标签索引
            m_SelectedTab = index;

            // 更新标签按钮外观
            UpdateTabButtonsAppearance();

            // 移动标签指示器
            if (m_TabIndicator != null && m_TabButtons.Count > index)
            {
                // 在实际项目中，可以使用协程或DoTween等动画库实现平滑过渡
                m_TabIndicator.SetParent(m_TabButtons[index].transform);
                m_TabIndicator.anchoredPosition = Vector2.zero;
            }

            // 触发事件
            OnTabChanged.Invoke(m_SelectedTab);
        }

        /// <summary>
        /// 获取当前选中的标签索引
        /// </summary>
        public int GetSelectedTabIndex()
        {
            return m_SelectedTab;
        }

        /// <summary>
        /// 获取标签数量
        /// </summary>
        public int GetTabCount()
        {
            return m_Tabs.Count;
        }

        /// <summary>
        /// 设置标签位置
        /// </summary>
        public void SetTabPosition(TabPosition position)
        {
            if (m_TabPosition == position)
                return;

            m_TabPosition = position;
            UpdateLayout();
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

            // 更新标签按钮的可交互状态
            foreach (var button in m_TabButtons)
            {
                if (button != null)
                {
                    button.SetInteractable(m_interactable);
                }
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

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.backgroundColor;
                m_ActiveTabColor = m_theme.accentColor;
                m_InactiveTabColor = m_theme.GetStateColor(InteractionState.Normal);
            }
            else
            {
                // 默认颜色
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // 应用颜色
            if (m_Background != null)
            {
                m_Background.color = backgroundColor;
            }

            // 更新标签按钮外观
            UpdateTabButtonsAppearance();
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
                m_rectTransform.sizeDelta = new Vector2(400, 300);
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
                m_Background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // 如果没有标签栏，创建一个
            if (m_TabBar == null)
            {
                GameObject tabBarObj = new GameObject("TabBar");
                tabBarObj.transform.SetParent(transform);

                m_TabBar = tabBarObj.AddComponent<RectTransform>();

                // 根据标签位置设置锚点
                UpdateTabBarAnchors();

                // 添加布局组
                HorizontalLayoutGroup layoutGroup = tabBarObj.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = m_TabSpacing;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = true;
                layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            }

            // 如果没有内容区域，创建一个
            if (m_ContentArea == null)
            {
                GameObject contentObj = new GameObject("ContentArea");
                contentObj.transform.SetParent(transform);

                m_ContentArea = contentObj.AddComponent<RectTransform>();

                // 根据标签位置设置锚点
                UpdateContentAreaAnchors();
            }

            // 如果没有标签指示器，创建一个
            if (m_TabIndicator == null)
            {
                GameObject indicatorObj = new GameObject("TabIndicator");
                indicatorObj.transform.SetParent(m_TabBar);

                m_TabIndicator = indicatorObj.AddComponent<RectTransform>();
                m_TabIndicator.anchorMin = Vector2.zero;
                m_TabIndicator.anchorMax = Vector2.one;
                m_TabIndicator.offsetMin = new Vector2(-2, -2);
                m_TabIndicator.offsetMax = new Vector2(2, 2);

                Image indicatorImage = indicatorObj.AddComponent<Image>();
                indicatorImage.color = m_ActiveTabColor;
            }

            // 更新布局
            UpdateLayout();
        }

        /// <summary>
        /// 创建标签按钮
        /// </summary>
        private void CreateTabButtons()
        {
            // 清除现有按钮
            foreach (var button in m_TabButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            m_TabButtons.Clear();

            // 如果没有标签栏，返回
            if (m_TabBar == null)
                return;

            // 创建新按钮
            for (int i = 0; i < m_Tabs.Count; i++)
            {
                CreateTabButton(i);
            }

            // 更新按钮外观
            UpdateTabButtonsAppearance();
        }

        /// <summary>
        /// 创建标签按钮
        /// </summary>
        private void CreateTabButton(int index)
        {
            TabData tab = m_Tabs[index];

            // 创建按钮
            GameObject buttonObj;
            VRButton button;

            if (m_TabButtonPrefab != null)
            {
                // 使用预制体
                buttonObj = Instantiate(m_TabButtonPrefab.gameObject, m_TabBar);
                button = buttonObj.GetComponent<VRButton>();
            }
            else
            {
                // 创建新按钮
                buttonObj = new GameObject("Tab_" + index);
                buttonObj.transform.SetParent(m_TabBar);

                RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
                buttonRect.sizeDelta = m_TabSize;

                button = buttonObj.AddComponent<VRButton>();

                // 设置主题
                if (m_theme != null)
                {
                    button.SetTheme(m_theme);
                }
            }

            // 设置按钮文本和图标
            button.SetText(tab.title);
            if (m_ShowIcons && tab.icon != null)
            {
                button.SetIcon(tab.icon);
            }

            // 设置按钮大小
            RectTransform existingButtonRect = button.GetComponent<RectTransform>();
            if (m_UseEqualTabWidths)
            {
                existingButtonRect.sizeDelta = m_TabSize;
            }
            else
            {
                // 根据内容调整大小
                existingButtonRect.sizeDelta = new Vector2(Mathf.Max(m_TabSize.x, tab.title.Length * 10 + 20), m_TabSize.y);
            }

            // 添加点击事件
            int tabIndex = index;
            button.OnClick.AddListener(() => SelectTab(tabIndex));

            // 添加到列表
            m_TabButtons.Add(button);
        }

        /// <summary>
        /// 更新标签按钮外观
        /// </summary>
        private void UpdateTabButtonsAppearance()
        {
            for (int i = 0; i < m_TabButtons.Count; i++)
            {
                if (m_TabButtons[i] != null)
                {
                    // 设置颜色
                    Color color = (i == m_SelectedTab) ? m_ActiveTabColor : m_InactiveTabColor;
                    m_TabButtons[i].UpdateVisualState(i == m_SelectedTab ? InteractionState.Selected : InteractionState.Normal);
                }
            }
        }

        /// <summary>
        /// 更新布局
        /// </summary>
        private void UpdateLayout()
        {
            // 更新标签栏锚点
            UpdateTabBarAnchors();

            // 更新内容区域锚点
            UpdateContentAreaAnchors();

            // 更新标签按钮布局
            UpdateTabButtonLayout();
        }

        /// <summary>
        /// 更新标签栏锚点
        /// </summary>
        private void UpdateTabBarAnchors()
        {
            if (m_TabBar == null)
                return;

            switch (m_TabPosition)
            {
                case TabPosition.Top:
                    m_TabBar.anchorMin = new Vector2(0, 1);
                    m_TabBar.anchorMax = new Vector2(1, 1);
                    m_TabBar.pivot = new Vector2(0.5f, 1);
                    m_TabBar.sizeDelta = new Vector2(0, m_TabSize.y);
                    m_TabBar.anchoredPosition = Vector2.zero;
                    break;
                case TabPosition.Bottom:
                    m_TabBar.anchorMin = new Vector2(0, 0);
                    m_TabBar.anchorMax = new Vector2(1, 0);
                    m_TabBar.pivot = new Vector2(0.5f, 0);
                    m_TabBar.sizeDelta = new Vector2(0, m_TabSize.y);
                    m_TabBar.anchoredPosition = Vector2.zero;
                    break;
                case TabPosition.Left:
                    m_TabBar.anchorMin = new Vector2(0, 0);
                    m_TabBar.anchorMax = new Vector2(0, 1);
                    m_TabBar.pivot = new Vector2(0, 0.5f);
                    m_TabBar.sizeDelta = new Vector2(m_TabSize.x, 0);
                    m_TabBar.anchoredPosition = Vector2.zero;
                    break;
                case TabPosition.Right:
                    m_TabBar.anchorMin = new Vector2(1, 0);
                    m_TabBar.anchorMax = new Vector2(1, 1);
                    m_TabBar.pivot = new Vector2(1, 0.5f);
                    m_TabBar.sizeDelta = new Vector2(m_TabSize.x, 0);
                    m_TabBar.anchoredPosition = Vector2.zero;
                    break;
            }

            // 更新布局组方向
            if (m_TabBar.GetComponent<HorizontalLayoutGroup>() != null &&
                (m_TabPosition == TabPosition.Left || m_TabPosition == TabPosition.Right))
            {
                // 替换为垂直布局
                Destroy(m_TabBar.GetComponent<HorizontalLayoutGroup>());
                VerticalLayoutGroup verticalLayout = m_TabBar.gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.spacing = m_TabSpacing;
                verticalLayout.childForceExpandWidth = true;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.padding = new RectOffset(5, 5, 5, 5);
            }
            else if (m_TabBar.GetComponent<VerticalLayoutGroup>() != null &&
                    (m_TabPosition == TabPosition.Top || m_TabPosition == TabPosition.Bottom))
            {
                // 替换为水平布局
                Destroy(m_TabBar.GetComponent<VerticalLayoutGroup>());
                HorizontalLayoutGroup horizontalLayout = m_TabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
                horizontalLayout.spacing = m_TabSpacing;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = true;
                horizontalLayout.padding = new RectOffset(5, 5, 5, 5);
            }
        }

        /// <summary>
        /// 更新内容区域锚点
        /// </summary>
        private void UpdateContentAreaAnchors()
        {
            if (m_ContentArea == null)
                return;

            switch (m_TabPosition)
            {
                case TabPosition.Top:
                    m_ContentArea.anchorMin = new Vector2(0, 0);
                    m_ContentArea.anchorMax = new Vector2(1, 1);
                    m_ContentArea.offsetMin = new Vector2(0, 0);
                    m_ContentArea.offsetMax = new Vector2(0, -m_TabSize.y);
                    break;
                case TabPosition.Bottom:
                    m_ContentArea.anchorMin = new Vector2(0, 0);
                    m_ContentArea.anchorMax = new Vector2(1, 1);
                    m_ContentArea.offsetMin = new Vector2(0, m_TabSize.y);
                    m_ContentArea.offsetMax = new Vector2(0, 0);
                    break;
                case TabPosition.Left:
                    m_ContentArea.anchorMin = new Vector2(0, 0);
                    m_ContentArea.anchorMax = new Vector2(1, 1);
                    m_ContentArea.offsetMin = new Vector2(m_TabSize.x, 0);
                    m_ContentArea.offsetMax = new Vector2(0, 0);
                    break;
                case TabPosition.Right:
                    m_ContentArea.anchorMin = new Vector2(0, 0);
                    m_ContentArea.anchorMax = new Vector2(1, 1);
                    m_ContentArea.offsetMin = new Vector2(0, 0);
                    m_ContentArea.offsetMax = new Vector2(-m_TabSize.x, 0);
                    break;
            }
        }

        /// <summary>
        /// 更新标签按钮布局
        /// </summary>
        private void UpdateTabButtonLayout()
        {
            // 调整标签按钮大小
            for (int i = 0; i < m_TabButtons.Count; i++)
            {
                if (m_TabButtons[i] != null)
                {
                    RectTransform buttonRect = m_TabButtons[i].GetComponent<RectTransform>();

                    if (m_TabPosition == TabPosition.Left || m_TabPosition == TabPosition.Right)
                    {
                        // 垂直布局
                        buttonRect.sizeDelta = new Vector2(m_TabSize.x, m_TabSize.y);
                    }
                    else
                    {
                        // 水平布局
                        if (m_UseEqualTabWidths)
                        {
                            buttonRect.sizeDelta = m_TabSize;
                        }
                        else
                        {
                            // 根据内容调整大小
                            buttonRect.sizeDelta = new Vector2(Mathf.Max(m_TabSize.x, m_Tabs[i].title.Length * 10 + 20), m_TabSize.y);
                        }
                    }
                }
            }
        }

        #endregion
    }
}