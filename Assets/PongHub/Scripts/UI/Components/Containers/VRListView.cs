using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using PongHub.UI.Core;

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR列表视图组件
    /// 实现VR环境中的列表项显示和选择功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRListView : VRUIComponent
    {
        [System.Serializable]
        public class ListItemData
        {
            [Tooltip("Item Text / 项目文本")]
            public string text;

            [Tooltip("Item Icon / 项目图标")]
            public Sprite icon;

            [Tooltip("Item Data / 项目数据 - Custom data associated with this item")]
            public object data;

            [Tooltip("Is Selected / 是否选中")]
            public bool isSelected;

            public ListItemData()
            {
                text = "Item";
                icon = null;
                data = null;
                isSelected = false;
            }

            public ListItemData(string text)
            {
                this.text = text;
                this.icon = null;
                this.data = null;
                this.isSelected = false;
            }

            public ListItemData(string text, Sprite icon)
            {
                this.text = text;
                this.icon = icon;
                this.data = null;
                this.isSelected = false;
            }

            public ListItemData(string text, Sprite icon, object data)
            {
                this.text = text;
                this.icon = icon;
                this.data = data;
                this.isSelected = false;
            }
        }

        [Header("列表设置")]
        [SerializeField]
        [Tooltip("Items / 项目列表")]
        private List<ListItemData> m_Items = new List<ListItemData>();

        [SerializeField]
        [Tooltip("Selected Index / 选中的索引 - Index of the currently selected item")]
        private int m_SelectedIndex = -1;

        [SerializeField]
        [Tooltip("Item Height / 项目高度 - Height of each list item")]
        private float m_ItemHeight = 40f;

        [SerializeField]
        [Tooltip("Item Spacing / 项目间距 - Spacing between list items")]
        private float m_ItemSpacing = 2f;

        [SerializeField]
        [Tooltip("Allow Multiple Selection / 允许多选 - Whether multiple items can be selected")]
        private bool m_AllowMultipleSelection = false;

        [SerializeField]
        [Tooltip("Show Icons / 显示图标 - Whether to show icons in list items")]
        private bool m_ShowIcons = true;

        [SerializeField]
        [Tooltip("Auto Scroll / 自动滚动 - Whether to automatically scroll to selected items")]
        private bool m_AutoScroll = true;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Scroll View / 滚动视图 - Scroll view for the list")]
        private ScrollRect m_ScrollView;

        [SerializeField]
        [Tooltip("Content / 内容 - Content container for list items")]
        private RectTransform m_Content;

        [SerializeField]
        [Tooltip("Viewport / 视口 - Viewport for the scroll view")]
        private RectTransform m_Viewport;

        [SerializeField]
        [Tooltip("Item Prefab / 项目预制体 - Prefab for list items")]
        private GameObject m_ItemPrefab;

        [SerializeField]
        [Tooltip("Selected Item Color / 选中项目颜色 - Color for selected items")]
        private Color m_SelectedItemColor = new Color(0.3f, 0.5f, 0.9f, 1f);

        [SerializeField]
        [Tooltip("Unselected Item Color / 未选中项目颜色 - Color for unselected items")]
        private Color m_UnselectedItemColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [SerializeField]
        [Tooltip("Background / 背景 - Background for the list view")]
        private Image m_Background;

        [SerializeField]
        [Tooltip("Show Scrollbar / 显示滚动条 - Whether to show the scrollbar")]
        private bool m_ShowScrollbar = true;

        [SerializeField]
        [Tooltip("Scrollbar / 滚动条 - Scrollbar for the scroll view")]
        private Scrollbar m_Scrollbar;

        [SerializeField]
        [Tooltip("Scrollbar Width / 滚动条宽度 - Width of the scrollbar")]
        private float m_ScrollbarWidth = 20f;

        // 事件
        [Header("事件")]
        public UnityEvent<int> OnItemSelected = new UnityEvent<int>();
        public UnityEvent<int> OnItemClicked = new UnityEvent<int>();
        public UnityEvent<List<int>> OnSelectionChanged = new UnityEvent<List<int>>();

        // 内部变量
        private List<GameObject> m_ItemObjects = new List<GameObject>();
        private RectTransform m_rectTransform;
        private List<int> m_SelectedIndices = new List<int>();

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
            // 刷新列表项
            RefreshItems();
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
        /// 添加项目
        /// </summary>
        public void AddItem(string text, Sprite icon = null, object data = null)
        {
            ListItemData item = new ListItemData(text, icon, data);
            m_Items.Add(item);
            RefreshItems();
        }

        /// <summary>
        /// 添加项目列表
        /// </summary>
        public void AddItems(List<string> items)
        {
            foreach (var item in items)
            {
                m_Items.Add(new ListItemData(item));
            }
            RefreshItems();
        }

        /// <summary>
        /// 添加项目列表（带图标）
        /// </summary>
        public void AddItems(List<ListItemData> items)
        {
            m_Items.AddRange(items);
            RefreshItems();
        }

        /// <summary>
        /// 移除项目
        /// </summary>
        public void RemoveItem(int index)
        {
            if (index < 0 || index >= m_Items.Count)
                return;

            // 如果移除的是当前选中的项目，清除选择
            if (m_SelectedIndices.Contains(index))
            {
                m_SelectedIndices.Remove(index);

                // 调整其他选中索引
                for (int i = 0; i < m_SelectedIndices.Count; i++)
                {
                    if (m_SelectedIndices[i] > index)
                    {
                        m_SelectedIndices[i]--;
                    }
                }

                // 更新主选择索引
                m_SelectedIndex = m_SelectedIndices.Count > 0 ? m_SelectedIndices[0] : -1;

                // 触发事件
                OnSelectionChanged.Invoke(m_SelectedIndices);
            }

            // 移除项目
            m_Items.RemoveAt(index);
            RefreshItems();
        }

        /// <summary>
        /// 清除所有项目
        /// </summary>
        public void ClearItems()
        {
            m_Items.Clear();
            m_SelectedIndices.Clear();
            m_SelectedIndex = -1;
            RefreshItems();

            // 触发事件
            OnSelectionChanged.Invoke(m_SelectedIndices);
        }

        /// <summary>
        /// 选择项目
        /// </summary>
        public void SelectItem(int index, bool notify = true)
        {
            if (index < 0 || index >= m_Items.Count)
                return;

            if (!m_AllowMultipleSelection)
            {
                // 单选模式
                // 清除之前的选择
                foreach (var item in m_Items)
                {
                    item.isSelected = false;
                }
                m_SelectedIndices.Clear();

                // 设置新选择
                m_Items[index].isSelected = true;
                m_SelectedIndices.Add(index);
                m_SelectedIndex = index;
            }
            else
            {
                // 多选模式
                if (m_Items[index].isSelected)
                {
                    // 取消选择
                    m_Items[index].isSelected = false;
                    m_SelectedIndices.Remove(index);

                    // 更新主选择索引
                    m_SelectedIndex = m_SelectedIndices.Count > 0 ? m_SelectedIndices[0] : -1;
                }
                else
                {
                    // 添加选择
                    m_Items[index].isSelected = true;
                    m_SelectedIndices.Add(index);

                    // 如果是第一个选中项，设置为主选择
                    if (m_SelectedIndex == -1)
                    {
                        m_SelectedIndex = index;
                    }
                }
            }

            // 更新列表项外观
            UpdateItemsAppearance();

            // 自动滚动到选中项
            if (m_AutoScroll && m_ScrollView != null)
            {
                ScrollToItem(index);
            }

            // 触发事件
            if (notify)
            {
                OnItemSelected.Invoke(index);
                OnSelectionChanged.Invoke(m_SelectedIndices);
            }
        }

        /// <summary>
        /// 获取选中的索引
        /// </summary>
        public int GetSelectedIndex()
        {
            return m_SelectedIndex;
        }

        /// <summary>
        /// 获取所有选中的索引
        /// </summary>
        public List<int> GetSelectedIndices()
        {
            return new List<int>(m_SelectedIndices);
        }

        /// <summary>
        /// 获取选中的项目数据
        /// </summary>
        public ListItemData GetSelectedItem()
        {
            if (m_SelectedIndex >= 0 && m_SelectedIndex < m_Items.Count)
            {
                return m_Items[m_SelectedIndex];
            }
            return null;
        }

        /// <summary>
        /// 获取所有选中的项目数据
        /// </summary>
        public List<ListItemData> GetSelectedItems()
        {
            List<ListItemData> selectedItems = new List<ListItemData>();
            foreach (int index in m_SelectedIndices)
            {
                if (index >= 0 && index < m_Items.Count)
                {
                    selectedItems.Add(m_Items[index]);
                }
            }
            return selectedItems;
        }

        /// <summary>
        /// 获取项目数量
        /// </summary>
        public int GetItemCount()
        {
            return m_Items.Count;
        }

        /// <summary>
        /// 滚动到指定项目
        /// </summary>
        public void ScrollToItem(int index)
        {
            if (index < 0 || index >= m_Items.Count || m_ScrollView == null)
                return;

            // 计算项目位置
            float normalizedPosition = 1f - (float)index / Mathf.Max(1, m_Items.Count - 1);
            m_ScrollView.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        }

        /// <summary>
        /// 设置允许多选
        /// </summary>
        public void SetAllowMultipleSelection(bool allow)
        {
            if (m_AllowMultipleSelection == allow)
                return;

            m_AllowMultipleSelection = allow;

            // 如果从多选切换到单选，只保留第一个选中项
            if (!m_AllowMultipleSelection && m_SelectedIndices.Count > 1)
            {
                int firstSelected = m_SelectedIndices[0];

                // 清除所有选择
                foreach (var item in m_Items)
                {
                    item.isSelected = false;
                }
                m_SelectedIndices.Clear();

                // 只选择第一个
                if (firstSelected >= 0 && firstSelected < m_Items.Count)
                {
                    m_Items[firstSelected].isSelected = true;
                    m_SelectedIndices.Add(firstSelected);
                    m_SelectedIndex = firstSelected;
                }
                else
                {
                    m_SelectedIndex = -1;
                }

                // 更新列表项外观
                UpdateItemsAppearance();

                // 触发事件
                OnSelectionChanged.Invoke(m_SelectedIndices);
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

            // 更新滚动视图可交互性
            if (m_ScrollView != null)
            {
                m_ScrollView.enabled = m_interactable;
            }

            // 更新滚动条可交互性
            if (m_Scrollbar != null)
            {
                m_Scrollbar.interactable = m_interactable;
            }

            // 更新列表项可交互性
            foreach (GameObject itemObj in m_ItemObjects)
            {
                VRButton itemButton = itemObj.GetComponent<VRButton>();
                if (itemButton != null)
                {
                    itemButton.SetInteractable(m_interactable);
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
                m_SelectedItemColor = m_theme.accentColor;
                m_UnselectedItemColor = m_theme.GetStateColor(InteractionState.Normal);
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

            // 更新列表项外观
            UpdateItemsAppearance();
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
                m_rectTransform.sizeDelta = new Vector2(300, 400);
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

            // 如果没有滚动视图，创建一个
            if (m_ScrollView == null)
            {
                GameObject scrollObj = new GameObject("ScrollView");
                scrollObj.transform.SetParent(transform);

                RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
                scrollRect.anchorMin = Vector2.zero;
                scrollRect.anchorMax = Vector2.one;
                scrollRect.offsetMin = Vector2.zero;
                scrollRect.offsetMax = Vector2.zero;

                m_ScrollView = scrollObj.AddComponent<ScrollRect>();
                m_ScrollView.horizontal = false;
                m_ScrollView.vertical = true;
                m_ScrollView.scrollSensitivity = 30;
                m_ScrollView.movementType = ScrollRect.MovementType.Elastic;
                m_ScrollView.elasticity = 0.1f;
                m_ScrollView.inertia = true;
                m_ScrollView.decelerationRate = 0.135f;

                // 创建视口
                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollObj.transform);

                m_Viewport = viewportObj.AddComponent<RectTransform>();
                m_Viewport.anchorMin = Vector2.zero;
                m_Viewport.anchorMax = Vector2.one;

                // 根据是否显示滚动条调整视口大小
                if (m_ShowScrollbar)
                {
                    m_Viewport.offsetMin = new Vector2(0, 0);
                    m_Viewport.offsetMax = new Vector2(-m_ScrollbarWidth, 0);
                }
                else
                {
                    m_Viewport.offsetMin = Vector2.zero;
                    m_Viewport.offsetMax = Vector2.zero;
                }

                // 添加遮罩
                Mask mask = viewportObj.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                Image viewportImage = viewportObj.AddComponent<Image>();
                viewportImage.color = Color.white;

                // 创建内容
                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform);

                m_Content = contentObj.AddComponent<RectTransform>();
                m_Content.anchorMin = new Vector2(0, 1);
                m_Content.anchorMax = new Vector2(1, 1);
                m_Content.pivot = new Vector2(0.5f, 1);
                m_Content.anchoredPosition = Vector2.zero;
                m_Content.sizeDelta = new Vector2(0, 0);

                // 添加垂直布局组
                VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.spacing = m_ItemSpacing;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.padding = new RectOffset(5, 5, 5, 5);

                // 添加内容大小适配器
                ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 设置滚动视图引用
                m_ScrollView.viewport = m_Viewport;
                m_ScrollView.content = m_Content;

                // 创建滚动条
                if (m_ShowScrollbar)
                {
                    GameObject scrollbarObj = new GameObject("Scrollbar");
                    scrollbarObj.transform.SetParent(scrollObj.transform);

                    RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
                    scrollbarRect.anchorMin = new Vector2(1, 0);
                    scrollbarRect.anchorMax = new Vector2(1, 1);
                    scrollbarRect.pivot = new Vector2(1, 0.5f);
                    scrollbarRect.sizeDelta = new Vector2(m_ScrollbarWidth, 0);
                    scrollbarRect.anchoredPosition = Vector2.zero;

                    m_Scrollbar = scrollbarObj.AddComponent<Scrollbar>();
                    m_Scrollbar.direction = Scrollbar.Direction.BottomToTop;

                    // 创建滚动条背景
                    Image scrollbarImage = scrollbarObj.AddComponent<Image>();
                    scrollbarImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                    // 创建滚动条滑块
                    GameObject slidingAreaObj = new GameObject("SlidingArea");
                    slidingAreaObj.transform.SetParent(scrollbarObj.transform);

                    RectTransform slidingAreaRect = slidingAreaObj.AddComponent<RectTransform>();
                    slidingAreaRect.anchorMin = Vector2.zero;
                    slidingAreaRect.anchorMax = Vector2.one;
                    slidingAreaRect.offsetMin = Vector2.zero;
                    slidingAreaRect.offsetMax = Vector2.zero;

                    GameObject handleObj = new GameObject("Handle");
                    handleObj.transform.SetParent(slidingAreaObj.transform);

                    RectTransform handleRect = handleObj.AddComponent<RectTransform>();
                    handleRect.anchorMin = Vector2.zero;
                    handleRect.anchorMax = new Vector2(1, 0.2f);
                    handleRect.offsetMin = new Vector2(2, 0);
                    handleRect.offsetMax = new Vector2(-2, 0);

                    Image handleImage = handleObj.AddComponent<Image>();
                    handleImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

                    // 设置滚动条引用
                    m_Scrollbar.targetGraphic = handleImage;
                    m_Scrollbar.handleRect = handleRect;
                    m_ScrollView.verticalScrollbar = m_Scrollbar;
                }
            }

            // 创建项目预制体
            if (m_ItemPrefab == null)
            {
                m_ItemPrefab = new GameObject("ItemTemplate");

                RectTransform itemRect = m_ItemPrefab.AddComponent<RectTransform>();
                itemRect.sizeDelta = new Vector2(0, m_ItemHeight);

                VRButton itemButton = m_ItemPrefab.AddComponent<VRButton>();

                // 设置主题
                if (m_theme != null)
                {
                    itemButton.SetTheme(m_theme);
                }

                // 隐藏预制体
                m_ItemPrefab.SetActive(false);
            }
        }

        /// <summary>
        /// 刷新列表项
        /// </summary>
        private void RefreshItems()
        {
            // 清除现有项目
            foreach (GameObject itemObj in m_ItemObjects)
            {
                if (itemObj != null)
                {
                    Destroy(itemObj);
                }
            }
            m_ItemObjects.Clear();

            // 如果没有内容容器，返回
            if (m_Content == null)
                return;

            // 创建新项目
            for (int i = 0; i < m_Items.Count; i++)
            {
                CreateItem(i);
            }

            // 更新内容高度
            UpdateContentHeight();

            // 更新列表项外观
            UpdateItemsAppearance();

            // 自动滚动到选中项
            if (m_AutoScroll && m_SelectedIndex >= 0 && m_ScrollView != null)
            {
                ScrollToItem(m_SelectedIndex);
            }
        }

        /// <summary>
        /// 创建列表项
        /// </summary>
        private void CreateItem(int index)
        {
            ListItemData item = m_Items[index];

            // 创建项目对象
            GameObject itemObj;
            VRButton itemButton;

            if (m_ItemPrefab != null)
            {
                // 使用预制体
                itemObj = Instantiate(m_ItemPrefab, m_Content);
                itemObj.SetActive(true);
                itemButton = itemObj.GetComponent<VRButton>();
            }
            else
            {
                // 创建新项目
                itemObj = new GameObject("Item_" + index);
                itemObj.transform.SetParent(m_Content);

                RectTransform itemRect = itemObj.AddComponent<RectTransform>();
                itemRect.sizeDelta = new Vector2(0, m_ItemHeight);

                itemButton = itemObj.AddComponent<VRButton>();

                // 设置主题
                if (m_theme != null)
                {
                    itemButton.SetTheme(m_theme);
                }
            }

            // 设置项目文本和图标
            itemButton.SetText(item.text);
            if (m_ShowIcons && item.icon != null)
            {
                itemButton.SetIcon(item.icon);
            }

            // 添加点击事件
            int itemIndex = index;
            itemButton.OnClick.AddListener(() => OnItemClicked.Invoke(itemIndex));
            itemButton.OnClick.AddListener(() => SelectItem(itemIndex));

            // 添加到列表
            m_ItemObjects.Add(itemObj);
        }

        /// <summary>
        /// 更新内容高度
        /// </summary>
        private void UpdateContentHeight()
        {
            if (m_Content == null)
                return;

            // 计算内容高度
            float contentHeight = m_Items.Count * (m_ItemHeight + m_ItemSpacing) + m_ItemSpacing;
            m_Content.sizeDelta = new Vector2(m_Content.sizeDelta.x, contentHeight);
        }

        /// <summary>
        /// 更新列表项外观
        /// </summary>
        private void UpdateItemsAppearance()
        {
            for (int i = 0; i < m_ItemObjects.Count; i++)
            {
                if (i < m_Items.Count && m_ItemObjects[i] != null)
                {
                    VRButton itemButton = m_ItemObjects[i].GetComponent<VRButton>();
                    if (itemButton != null)
                    {
                        // 设置颜色
                        itemButton.UpdateVisualState(m_Items[i].isSelected ? InteractionState.Selected : InteractionState.Normal);
                    }
                }
            }
        }

        #endregion
    }
}