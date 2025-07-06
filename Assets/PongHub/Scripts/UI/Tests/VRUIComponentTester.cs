using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PongHub.UI.Components;
using PongHub.UI.Core;

namespace PongHub.UI.Tests
{
    /// <summary>
    /// VR UI组件测试脚本
    /// 用于测试所有UI组件的功能
    /// </summary>
    public class VRUIComponentTester : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField]
        [Tooltip("Canvas / 画布 - Canvas for UI components")]
        private Canvas m_Canvas;

        [SerializeField]
        [Tooltip("Test Panel / 测试面板 - Panel for testing UI components")]
        private RectTransform m_TestPanel;

        [SerializeField]
        [Tooltip("Theme / 主题 - Theme for UI components")]
        private VRUITheme m_Theme;

        [SerializeField]
        [Tooltip("Test Basic Components / 测试基础组件 - Whether to test basic components")]
        private bool m_TestBasicComponents = true;

        [SerializeField]
        [Tooltip("Test Container Components / 测试容器组件 - Whether to test container components")]
        private bool m_TestContainerComponents = true;

        [SerializeField]
        [Tooltip("Test All Components / 测试所有组件 - Whether to test all components")]
        private bool m_TestAllComponents = false;

        [SerializeField]
        [Tooltip("Spacing / 间距 - Spacing between test components")]
        private float m_Spacing = 20f;

        // 内部变量
        private List<VRUIComponent> m_TestComponents = new List<VRUIComponent>();

        private void Awake()
        {
            // 确保有Canvas
            if (m_Canvas == null)
            {
                m_Canvas = GetComponentInParent<Canvas>();
                if (m_Canvas == null)
                {
                    m_Canvas = gameObject.AddComponent<Canvas>();
                    m_Canvas.renderMode = RenderMode.WorldSpace;

                    // 添加Canvas Scaler
                    CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                    scaler.dynamicPixelsPerUnit = 100;
                    scaler.referencePixelsPerUnit = 100;

                    // 添加Graphic Raycaster
                    gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            // 确保有测试面板
            if (m_TestPanel == null)
            {
                GameObject panelObj = new GameObject("TestPanel");
                panelObj.transform.SetParent(transform);

                m_TestPanel = panelObj.AddComponent<RectTransform>();
                m_TestPanel.anchorMin = Vector2.zero;
                m_TestPanel.anchorMax = Vector2.one;
                m_TestPanel.offsetMin = Vector2.zero;
                m_TestPanel.offsetMax = Vector2.zero;

                // 添加背景
                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // 确保有主题
            if (m_Theme == null)
            {
                m_Theme = ScriptableObject.CreateInstance<VRUITheme>();
            }
        }

        private void Start()
        {
            // 测试所有组件
            if (m_TestAllComponents)
            {
                m_TestBasicComponents = true;
                m_TestContainerComponents = true;
            }

            // 创建测试组件
            CreateTestComponents();
        }

        /// <summary>
        /// 创建测试组件
        /// </summary>
        private void CreateTestComponents()
        {
            // 清除现有组件
            foreach (Transform child in m_TestPanel)
            {
                Destroy(child.gameObject);
            }
            m_TestComponents.Clear();

            // 创建垂直布局组
            GameObject layoutObj = new GameObject("TestLayout");
            layoutObj.transform.SetParent(m_TestPanel);

            RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.anchorMin = new Vector2(0, 0);
            layoutRect.anchorMax = new Vector2(1, 1);
            layoutRect.offsetMin = new Vector2(20, 20);
            layoutRect.offsetMax = new Vector2(-20, -20);

            VerticalLayoutGroup layoutGroup = layoutObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = m_Spacing;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            // 添加内容大小适配器
            ContentSizeFitter sizeFitter = layoutObj.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 测试基础组件
            if (m_TestBasicComponents)
            {
                // 添加标题
                CreateSectionTitle(layoutObj.transform, "基础组件测试");

                // 测试按钮
                CreateTestButton(layoutObj.transform);

                // 测试开关
                CreateTestToggle(layoutObj.transform);

                // 测试滑块
                CreateTestSlider(layoutObj.transform);

                // 测试标签
                CreateTestLabel(layoutObj.transform);

                // 测试输入框
                CreateTestInputField(layoutObj.transform);

                // 测试下拉菜单
                CreateTestDropdown(layoutObj.transform);
            }

            // 测试容器组件
            if (m_TestContainerComponents)
            {
                // 添加标题
                CreateSectionTitle(layoutObj.transform, "容器组件测试");

                // 测试面板
                CreateTestPanel(layoutObj.transform);

                // 测试布局组
                CreateTestLayoutGroup(layoutObj.transform);

                // 测试标签页
                CreateTestTabView(layoutObj.transform);

                // 测试列表视图
                CreateTestListView(layoutObj.transform);

                // 测试弹出窗口
                CreateTestPopupWindow(layoutObj.transform);
            }

            // 添加滚动视图
            ScrollRect scrollRect = layoutObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.content = layoutRect;
        }

        /// <summary>
        /// 创建章节标题
        /// </summary>
        private void CreateSectionTitle(Transform parent, string title)
        {
            GameObject titleObj = new GameObject("Title_" + title);
            titleObj.transform.SetParent(parent);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 50);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        #region 基础组件测试

        /// <summary>
        /// 创建测试按钮
        /// </summary>
        private void CreateTestButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(parent);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);

            VRButton button = buttonObj.AddComponent<VRButton>();
            button.SetTheme(m_Theme);
            button.SetText("测试按钮");
            button.OnClick.AddListener(() => Debug.Log("按钮被点击"));

            m_TestComponents.Add(button);
        }

        /// <summary>
        /// 创建测试开关
        /// </summary>
        private void CreateTestToggle(Transform parent)
        {
            GameObject toggleObj = new GameObject("TestToggle");
            toggleObj.transform.SetParent(parent);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(200, 50);

            VRToggle toggle = toggleObj.AddComponent<VRToggle>();
            toggle.SetTheme(m_Theme);
            toggle.SetText("测试开关");
            toggle.OnValueChanged.AddListener((value) => Debug.Log("开关值变化: " + value));

            m_TestComponents.Add(toggle);
        }

        /// <summary>
        /// 创建测试滑块
        /// </summary>
        private void CreateTestSlider(Transform parent)
        {
            GameObject sliderObj = new GameObject("TestSlider");
            sliderObj.transform.SetParent(parent);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 50);

            VRSlider slider = sliderObj.AddComponent<VRSlider>();
            slider.SetTheme(m_Theme);
            slider.SetMinMaxValues(0, 100);
            slider.SetValue(50);
            slider.OnValueChanged.AddListener((value) => Debug.Log("滑块值变化: " + value));

            m_TestComponents.Add(slider);
        }

        /// <summary>
        /// 创建测试标签
        /// </summary>
        private void CreateTestLabel(Transform parent)
        {
            GameObject labelObj = new GameObject("TestLabel");
            labelObj.transform.SetParent(parent);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(300, 50);

            VRLabel label = labelObj.AddComponent<VRLabel>();
            label.SetTheme(m_Theme);
            label.SetText("这是一个测试标签");

            m_TestComponents.Add(label);
        }

        /// <summary>
        /// 创建测试输入框
        /// </summary>
        private void CreateTestInputField(Transform parent)
        {
            GameObject inputObj = new GameObject("TestInputField");
            inputObj.transform.SetParent(parent);

            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(300, 50);

            VRInputField inputField = inputObj.AddComponent<VRInputField>();
            inputField.SetTheme(m_Theme);
            inputField.SetPlaceholderText("请输入文本...");
            inputField.OnValueChanged.AddListener((text) => Debug.Log("输入值变化: " + text));

            m_TestComponents.Add(inputField);
        }

        /// <summary>
        /// 创建测试下拉菜单
        /// </summary>
        private void CreateTestDropdown(Transform parent)
        {
            GameObject dropdownObj = new GameObject("TestDropdown");
            dropdownObj.transform.SetParent(parent);

            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(300, 50);

            VRDropdown dropdown = dropdownObj.AddComponent<VRDropdown>();
            dropdown.SetTheme(m_Theme);

            // 添加选项
            dropdown.AddOption("选项 1");
            dropdown.AddOption("选项 2");
            dropdown.AddOption("选项 3");
            dropdown.AddOption("选项 4");
            dropdown.AddOption("选项 5");

            dropdown.OnValueChanged.AddListener((index) => Debug.Log("下拉菜单值变化: " + index));

            m_TestComponents.Add(dropdown);
        }

        #endregion

        #region 容器组件测试

        /// <summary>
        /// 创建测试面板
        /// </summary>
        private void CreateTestPanel(Transform parent)
        {
            GameObject panelObj = new GameObject("TestPanel");
            panelObj.transform.SetParent(parent);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(300, 200);

            VRPanel panel = panelObj.AddComponent<VRPanel>();
            panel.SetTheme(m_Theme);
            panel.SetTitle("测试面板");
            panel.SetDraggable(true);

            // 添加内容
            GameObject contentObj = new GameObject("PanelContent");
            contentObj.transform.SetParent(panel.GetContentArea());

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -10);

            TextMeshProUGUI contentText = contentObj.AddComponent<TextMeshProUGUI>();
            contentText.text = "这是面板内容\n可以拖动此面板";
            contentText.alignment = TextAlignmentOptions.Center;
            contentText.color = Color.white;

            m_TestComponents.Add(panel);
        }

        /// <summary>
        /// 创建测试布局组
        /// </summary>
        private void CreateTestLayoutGroup(Transform parent)
        {
            GameObject layoutObj = new GameObject("TestLayoutGroup");
            layoutObj.transform.SetParent(parent);

            RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.sizeDelta = new Vector2(300, 200);

            VRLayoutGroup layoutGroup = layoutObj.AddComponent<VRLayoutGroup>();
            layoutGroup.SetTheme(m_Theme);
            layoutGroup.SetLayoutType(VRLayoutGroup.LayoutType.Horizontal);
            layoutGroup.SetSpacing(10);

            // 添加内容
            for (int i = 0; i < 3; i++)
            {
                GameObject itemObj = new GameObject("Item_" + i);
                itemObj.transform.SetParent(layoutGroup.GetContentArea());

                RectTransform itemRect = itemObj.AddComponent<RectTransform>();
                itemRect.sizeDelta = new Vector2(80, 80);

                Image itemImage = itemObj.AddComponent<Image>();
                itemImage.color = new Color(0.3f + i * 0.2f, 0.3f, 0.8f - i * 0.2f, 1);

                TextMeshProUGUI itemText = itemObj.AddComponent<TextMeshProUGUI>();
                itemText.text = (i + 1).ToString();
                itemText.alignment = TextAlignmentOptions.Center;
                itemText.color = Color.white;
            }

            m_TestComponents.Add(layoutGroup);
        }

        /// <summary>
        /// 创建测试标签页
        /// </summary>
        private void CreateTestTabView(Transform parent)
        {
            GameObject tabViewObj = new GameObject("TestTabView");
            tabViewObj.transform.SetParent(parent);

            RectTransform tabViewRect = tabViewObj.AddComponent<RectTransform>();
            tabViewRect.sizeDelta = new Vector2(300, 200);

            VRTabView tabView = tabViewObj.AddComponent<VRTabView>();
            tabView.SetTheme(m_Theme);

            // 创建标签内容
            for (int i = 0; i < 3; i++)
            {
                GameObject tabContentObj = new GameObject("TabContent_" + i);

                RectTransform tabContentRect = tabContentObj.AddComponent<RectTransform>();
                tabContentRect.sizeDelta = new Vector2(300, 150);

                Image tabContentImage = tabContentObj.AddComponent<Image>();
                tabContentImage.color = new Color(0.2f, 0.2f + i * 0.2f, 0.4f + i * 0.1f, 0.5f);

                TextMeshProUGUI tabContentText = tabContentObj.AddComponent<TextMeshProUGUI>();
                tabContentText.text = "标签 " + (i + 1) + " 内容";
                tabContentText.alignment = TextAlignmentOptions.Center;
                tabContentText.color = Color.white;

                // 添加标签
                tabView.AddTab("标签 " + (i + 1), tabContentObj);
            }

            tabView.OnTabChanged.AddListener((index) => Debug.Log("标签切换: " + index));

            m_TestComponents.Add(tabView);
        }

        /// <summary>
        /// 创建测试列表视图
        /// </summary>
        private void CreateTestListView(Transform parent)
        {
            GameObject listViewObj = new GameObject("TestListView");
            listViewObj.transform.SetParent(parent);

            RectTransform listViewRect = listViewObj.AddComponent<RectTransform>();
            listViewRect.sizeDelta = new Vector2(300, 200);

            VRListView listView = listViewObj.AddComponent<VRListView>();
            listView.SetTheme(m_Theme);

            // 添加列表项
            for (int i = 0; i < 10; i++)
            {
                listView.AddItem("列表项 " + (i + 1));
            }

            listView.OnItemSelected.AddListener((index) => Debug.Log("列表项选中: " + index));

            m_TestComponents.Add(listView);
        }

        /// <summary>
        /// 创建测试弹出窗口
        /// </summary>
        private void CreateTestPopupWindow(Transform parent)
        {
            // 创建按钮打开窗口
            GameObject buttonObj = new GameObject("OpenPopupButton");
            buttonObj.transform.SetParent(parent);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);

            VRButton button = buttonObj.AddComponent<VRButton>();
            button.SetTheme(m_Theme);
            button.SetText("打开弹出窗口");

            // 创建弹出窗口
            GameObject popupObj = new GameObject("TestPopupWindow");
            popupObj.transform.SetParent(transform);

            VRPopupWindow popup = popupObj.AddComponent<VRPopupWindow>();
            popup.SetTheme(m_Theme);
            popup.SetTitle("测试弹出窗口");

            // 创建窗口内容
            GameObject popupContentObj = new GameObject("PopupContent");

            RectTransform popupContentRect = popupContentObj.AddComponent<RectTransform>();
            popupContentRect.sizeDelta = new Vector2(300, 200);

            TextMeshProUGUI popupContentText = popupContentObj.AddComponent<TextMeshProUGUI>();
            popupContentText.text = "这是弹出窗口内容\n可以拖动此窗口\n点击窗口外部或关闭按钮关闭窗口";
            popupContentText.alignment = TextAlignmentOptions.Center;
            popupContentText.color = Color.white;

            // 设置内容
            popup.SetContent(popupContentObj);

            // 设置按钮事件
            button.OnClick.AddListener(() => popup.Open());

            m_TestComponents.Add(button);
            m_TestComponents.Add(popup);
        }

        #endregion
    }
}