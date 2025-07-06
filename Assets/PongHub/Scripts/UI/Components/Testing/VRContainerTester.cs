using UnityEngine;
using PongHub.UI.Core;
using PongHub.UI.Components;
using UnityEngine.UI;

namespace PongHub.UI.Testing
{
    /// <summary>
    /// VR容器测试器
    /// 用于测试VR面板和布局组容器的功能
    /// </summary>
    public class VRContainerTester : MonoBehaviour
    {
        [Header("面板测试")]
        [SerializeField]
        [Tooltip("Test Panel / 测试面板 - Panel to test")]
        private VRPanel m_testPanel;

        [SerializeField]
        [Tooltip("Panel Canvas / 面板画布 - Canvas for the panel")]
        private Canvas m_panelCanvas;

        [SerializeField]
        [Tooltip("Create Test Panel / 创建测试面板 - Whether to create a test panel")]
        private bool m_createTestPanel = true;

        [Header("布局组测试")]
        [SerializeField]
        [Tooltip("Test Layout Group / 测试布局组 - Layout group to test")]
        private VRLayoutGroup m_testLayoutGroup;

        [SerializeField]
        [Tooltip("Layout Group Canvas / 布局组画布 - Canvas for the layout group")]
        private Canvas m_layoutGroupCanvas;

        [SerializeField]
        [Tooltip("Create Test Layout Group / 创建测试布局组 - Whether to create a test layout group")]
        private bool m_createTestLayoutGroup = true;

        [SerializeField]
        [Tooltip("Layout Type / 布局类型 - Type of layout to test")]
        private VRLayoutGroup.LayoutType m_layoutType = VRLayoutGroup.LayoutType.Vertical;

        [Header("测试组件")]
        [SerializeField]
        [Tooltip("Button Count / 按钮数量 - Number of buttons to create for testing")]
        private int m_buttonCount = 5;

        [SerializeField]
        [Tooltip("Toggle Count / 开关数量 - Number of toggles to create for testing")]
        private int m_toggleCount = 3;

        [SerializeField]
        [Tooltip("Slider Count / 滑块数量 - Number of sliders to create for testing")]
        private int m_sliderCount = 2;

        [SerializeField]
        [Tooltip("Label Count / 标签数量 - Number of labels to create for testing")]
        private int m_labelCount = 3;

        [SerializeField]
        [Tooltip("Input Field Count / 输入框数量 - Number of input fields to create for testing")]
        private int m_inputFieldCount = 1;

        [Header("主题设置")]
        [SerializeField]
        [Tooltip("Theme / 主题 - Theme to apply to the test components")]
        private VRUITheme m_theme;

        [SerializeField]
        [Tooltip("Create Default Theme / 创建默认主题 - Whether to create a default theme")]
        private bool m_createDefaultTheme = true;

        // UI管理器
        private VRUIManager m_uiManager;

        // 开始时
        private void Start()
        {
            // 创建UI管理器
            CreateUIManager();

            // 创建主题
            if (m_createDefaultTheme && m_theme == null)
            {
                CreateDefaultTheme();
            }

            // 创建测试面板
            if (m_createTestPanel)
            {
                CreateTestPanel();
            }

            // 创建测试布局组
            if (m_createTestLayoutGroup)
            {
                CreateTestLayoutGroup();
            }
        }

        /// <summary>
        /// 创建UI管理器
        /// </summary>
        private void CreateUIManager()
        {
            if (VRUIManager.Instance == null)
            {
                GameObject managerObj = new GameObject("VRUIManager");
                m_uiManager = managerObj.AddComponent<VRUIManager>();
                DontDestroyOnLoad(managerObj);
            }
            else
            {
                m_uiManager = VRUIManager.Instance;
            }
        }

        /// <summary>
        /// 创建默认主题
        /// </summary>
        private void CreateDefaultTheme()
        {
            m_theme = ScriptableObject.CreateInstance<VRUITheme>();
            m_theme.name = "DefaultTheme";
            m_theme.primaryColor = new Color(0.2f, 0.6f, 1f);
            m_theme.accentColor = new Color(1f, 0.5f, 0f);
            m_theme.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            m_theme.textColor = Color.white;
            // 移除不存在的属性引用
            // m_theme.borderColor = new Color(1f, 1f, 1f, 0.5f);

            // 应用主题到UI管理器
            if (VRUIManager.Instance != null)
            {
                // 使用正确的方法应用主题
                // 检查VRUIManager是否有SetTheme方法
                VRUIManager.Instance.SetTheme(m_theme);
            }
        }

        /// <summary>
        /// 创建测试面板
        /// </summary>
        private void CreateTestPanel()
        {
            // 创建画布
            if (m_panelCanvas == null)
            {
                GameObject canvasObj = new GameObject("PanelCanvas");
                canvasObj.transform.SetParent(transform);

                m_panelCanvas = canvasObj.AddComponent<Canvas>();
                m_panelCanvas.renderMode = RenderMode.WorldSpace;
                m_panelCanvas.worldCamera = Camera.main;

                RectTransform canvasRect = m_panelCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(800, 600);
                canvasRect.localPosition = new Vector3(0, 1.5f, 2f);
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

                // 添加Canvas Scaler
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100;
                scaler.referencePixelsPerUnit = 100;

                // 添加Graphic Raycaster
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 创建面板
            if (m_testPanel == null)
            {
                GameObject panelObj = new GameObject("TestPanel");
                panelObj.transform.SetParent(m_panelCanvas.transform, false);

                RectTransform panelRect = panelObj.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(600, 400);
                panelRect.localPosition = Vector3.zero;

                m_testPanel = panelObj.AddComponent<VRPanel>();
                m_testPanel.SetTheme(m_theme);
                m_testPanel.SetUseBackground(true);
                m_testPanel.SetUseBorder(true);
                m_testPanel.SetDraggable(true);
                m_testPanel.SetUseShadow(true);
                m_testPanel.SetCornerRadius(15f);
                m_testPanel.SetPadding(new RectOffset(20, 20, 60, 20));
            }

            // 创建面板标题
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(m_testPanel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, 0);

            VRLabel titleLabel = titleObj.AddComponent<VRLabel>();
            titleLabel.SetText("测试面板");
            titleLabel.SetFontSize(24);
            titleLabel.SetTheme(m_theme);

            // 添加测试组件到面板
            AddTestComponentsToPanel(m_testPanel);
        }

        /// <summary>
        /// 创建测试布局组
        /// </summary>
        private void CreateTestLayoutGroup()
        {
            // 创建画布
            if (m_layoutGroupCanvas == null)
            {
                GameObject canvasObj = new GameObject("LayoutGroupCanvas");
                canvasObj.transform.SetParent(transform);

                m_layoutGroupCanvas = canvasObj.AddComponent<Canvas>();
                m_layoutGroupCanvas.renderMode = RenderMode.WorldSpace;
                m_layoutGroupCanvas.worldCamera = Camera.main;

                RectTransform canvasRect = m_layoutGroupCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(800, 600);
                canvasRect.localPosition = new Vector3(1.5f, 1.5f, 2f);
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

                // 添加Canvas Scaler
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 100;
                scaler.referencePixelsPerUnit = 100;

                // 添加Graphic Raycaster
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 创建布局组
            if (m_testLayoutGroup == null)
            {
                GameObject layoutObj = new GameObject("TestLayoutGroup");
                layoutObj.transform.SetParent(m_layoutGroupCanvas.transform, false);

                RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
                layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
                layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
                layoutRect.pivot = new Vector2(0.5f, 0.5f);
                layoutRect.sizeDelta = new Vector2(500, 400);
                layoutRect.localPosition = Vector3.zero;

                m_testLayoutGroup = layoutObj.AddComponent<VRLayoutGroup>();
                m_testLayoutGroup.SetTheme(m_theme);
                m_testLayoutGroup.SetUseBackground(true);
                m_testLayoutGroup.SetLayoutType(m_layoutType);
                m_testLayoutGroup.SetSpacing(15f);
                m_testLayoutGroup.SetPadding(new RectOffset(10, 10, 10, 10));
                m_testLayoutGroup.SetChildAlignment(TextAnchor.MiddleCenter);
                m_testLayoutGroup.SetControlChildSize(true);

                if (m_layoutType == VRLayoutGroup.LayoutType.Horizontal)
                {
                    m_testLayoutGroup.SetChildSize(100, 200);
                }
                else if (m_layoutType == VRLayoutGroup.LayoutType.Grid)
                {
                    m_testLayoutGroup.SetCellSize(new Vector2(150, 100));
                    m_testLayoutGroup.SetConstraint(GridLayoutGroup.Constraint.FixedColumnCount, 3);
                }
                else
                {
                    m_testLayoutGroup.SetChildSize(300, 60);
                }
            }

            // 添加测试组件到布局组
            AddTestComponentsToLayoutGroup(m_testLayoutGroup);
        }

        /// <summary>
        /// 添加测试组件到面板
        /// </summary>
        private void AddTestComponentsToPanel(VRPanel panel)
        {
            if (panel == null)
                return;

            // 创建一个垂直布局组作为内容容器
            GameObject contentObj = new GameObject("PanelContent");
            contentObj.transform.SetParent(panel.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(20, 20);
            contentRect.offsetMax = new Vector2(-20, -60);

            VRLayoutGroup contentLayout = contentObj.AddComponent<VRLayoutGroup>();
            contentLayout.SetTheme(m_theme);
            contentLayout.SetLayoutType(VRLayoutGroup.LayoutType.Vertical);
            contentLayout.SetSpacing(15f);
            contentLayout.SetPadding(new RectOffset(10, 10, 10, 10));
            contentLayout.SetChildAlignment(TextAnchor.UpperCenter);
            contentLayout.SetControlChildSize(true);
            contentLayout.SetChildSize(500, 40);

            // 添加标签
            for (int i = 0; i < m_labelCount; i++)
            {
                GameObject labelObj = new GameObject($"Label_{i}");
                labelObj.transform.SetParent(contentLayout.transform, false);

                VRLabel label = labelObj.AddComponent<VRLabel>();
                label.SetText($"标签 {i + 1}");
                label.SetTheme(m_theme);
                label.SetUseBackground(i % 2 == 0);
            }

            // 添加按钮
            for (int i = 0; i < m_buttonCount; i++)
            {
                GameObject buttonObj = new GameObject($"Button_{i}");
                buttonObj.transform.SetParent(contentLayout.transform, false);

                VRButton button = buttonObj.AddComponent<VRButton>();
                button.SetText($"按钮 {i + 1}");
                button.SetTheme(m_theme);

                // 添加点击事件
                int buttonIndex = i;
                button.OnClick.AddListener(() => Debug.Log($"按钮 {buttonIndex + 1} 被点击"));
            }

            // 添加滑块
            for (int i = 0; i < m_sliderCount; i++)
            {
                GameObject sliderObj = new GameObject($"Slider_{i}");
                sliderObj.transform.SetParent(contentLayout.transform, false);

                VRSlider slider = sliderObj.AddComponent<VRSlider>();
                slider.SetTheme(m_theme);
                slider.SetValue(Random.Range(0f, 1f));
                slider.SetShowValue(true);

                // 添加值变化事件
                int sliderIndex = i;
                slider.OnValueChanged.AddListener((value) => Debug.Log($"滑块 {sliderIndex + 1} 值变为 {value}"));
            }

            // 添加开关
            for (int i = 0; i < m_toggleCount; i++)
            {
                GameObject toggleObj = new GameObject($"Toggle_{i}");
                toggleObj.transform.SetParent(contentLayout.transform, false);

                VRToggle toggle = toggleObj.AddComponent<VRToggle>();
                toggle.SetTheme(m_theme);
                toggle.SetIsOn(i % 2 == 0);
                toggle.SetText($"开关 {i + 1}");

                // 添加值变化事件
                int toggleIndex = i;
                toggle.OnValueChanged.AddListener((value) => Debug.Log($"开关 {toggleIndex + 1} 值变为 {value}"));
            }

            // 添加输入框
            for (int i = 0; i < m_inputFieldCount; i++)
            {
                GameObject inputObj = new GameObject($"InputField_{i}");
                inputObj.transform.SetParent(contentLayout.transform, false);

                VRInputField inputField = inputObj.AddComponent<VRInputField>();
                inputField.SetTheme(m_theme);
                inputField.SetPlaceholder($"请输入文本 {i + 1}");

                // 添加值变化事件
                int inputIndex = i;
                inputField.OnEndEdit.AddListener((value) => Debug.Log($"输入框 {inputIndex + 1} 值为 {value}"));
            }
        }

        /// <summary>
        /// 添加测试组件到布局组
        /// </summary>
        private void AddTestComponentsToLayoutGroup(VRLayoutGroup layoutGroup)
        {
            if (layoutGroup == null)
                return;

            // 添加按钮
            for (int i = 0; i < m_buttonCount; i++)
            {
                GameObject buttonObj = new GameObject($"LayoutButton_{i}");
                buttonObj.transform.SetParent(layoutGroup.transform, false);

                VRButton button = buttonObj.AddComponent<VRButton>();
                button.SetText($"按钮 {i + 1}");
                button.SetTheme(m_theme);

                // 添加点击事件
                int buttonIndex = i;
                button.OnClick.AddListener(() => Debug.Log($"布局按钮 {buttonIndex + 1} 被点击"));
            }

            // 添加开关
            for (int i = 0; i < m_toggleCount; i++)
            {
                GameObject toggleObj = new GameObject($"LayoutToggle_{i}");
                toggleObj.transform.SetParent(layoutGroup.transform, false);

                VRToggle toggle = toggleObj.AddComponent<VRToggle>();
                toggle.SetTheme(m_theme);
                toggle.SetIsOn(i % 2 == 0);
                toggle.SetText($"开关 {i + 1}");
            }
        }
    }
}