using UnityEngine;
using PongHub.UI.Core;
using PongHub.UI.Components;
using TMPro;

namespace PongHub.UI.Tests
{
    /// <summary>
    /// VR UI组件测试器
    /// 用于测试VR UI组件库的功能
    /// </summary>
    public class VRUIComponentTester : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField]
        [Tooltip("Test Panel / 测试面板 - Panel containing the test UI components")]
        private Transform m_testPanel;

        [SerializeField]
        [Tooltip("Theme / 主题 - Theme to apply to the test components")]
        private VRUITheme m_theme;

        [SerializeField]
        [Tooltip("Status Text / 状态文本 - Text component for displaying component status")]
        private TextMeshProUGUI m_statusText;

        [Header("测试组件")]
        [SerializeField]
        [Tooltip("Test Button / 测试按钮 - Button component to test")]
        private VRButton m_testButton;

        [SerializeField]
        [Tooltip("Test Toggle / 测试开关 - Toggle component to test")]
        private VRToggle m_testToggle;

        [SerializeField]
        [Tooltip("Test Slider / 测试滑块 - Slider component to test")]
        private VRSlider m_testSlider;

        private void Start()
        {
            // 初始化UI管理器
            InitializeUIManager();

            // 初始化测试组件
            InitializeTestComponents();

            // 注册事件处理
            RegisterEventHandlers();

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        private void InitializeUIManager()
        {
            // 查找或创建UI管理器
            VRUIManager uiManager = FindObjectOfType<VRUIManager>();
            if (uiManager == null)
            {
                GameObject managerObj = new GameObject("VRUIManager");
                uiManager = managerObj.AddComponent<VRUIManager>();
            }

            // 设置主题
            if (m_theme != null)
            {
                uiManager.SetTheme(m_theme);
            }
        }

        /// <summary>
        /// 初始化测试组件
        /// </summary>
        private void InitializeTestComponents()
        {
            // 如果没有测试面板，创建一个
            if (m_testPanel == null)
            {
                GameObject panelObj = new GameObject("TestPanel");
                panelObj.transform.SetParent(transform);
                panelObj.transform.localPosition = Vector3.zero;
                panelObj.transform.localRotation = Quaternion.identity;
                m_testPanel = panelObj.transform;
            }

            // 创建测试按钮
            if (m_testButton == null)
            {
                m_testButton = CreateTestButton();
            }

            // 创建测试开关
            if (m_testToggle == null)
            {
                m_testToggle = CreateTestToggle();
            }

            // 创建测试滑块
            if (m_testSlider == null)
            {
                m_testSlider = CreateTestSlider();
            }

            // 创建状态文本
            if (m_statusText == null)
            {
                m_statusText = CreateStatusText();
            }
        }

        /// <summary>
        /// 注册事件处理
        /// </summary>
        private void RegisterEventHandlers()
        {
            // 按钮点击事件
            if (m_testButton != null)
            {
                m_testButton.OnClick.AddListener(OnButtonClick);
            }

            // 开关值变化事件
            if (m_testToggle != null)
            {
                m_testToggle.OnValueChanged.AddListener(OnToggleValueChanged);
            }

            // 滑块值变化事件
            if (m_testSlider != null)
            {
                m_testSlider.OnValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        /// <summary>
        /// 创建测试按钮
        /// </summary>
        private VRButton CreateTestButton()
        {
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(m_testPanel);
            buttonObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            buttonObj.transform.localRotation = Quaternion.identity;
            buttonObj.transform.localScale = Vector3.one;

            // 添加RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            // 添加背景图像
            UnityEngine.UI.Image background = buttonObj.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.227f, 0.525f, 1f);

            // 添加按钮组件
            VRButton button = buttonObj.AddComponent<VRButton>();
            button.SetText("测试按钮");

            return button;
        }

        /// <summary>
        /// 创建测试开关
        /// </summary>
        private VRToggle CreateTestToggle()
        {
            GameObject toggleObj = new GameObject("TestToggle");
            toggleObj.transform.SetParent(m_testPanel);
            toggleObj.transform.localPosition = new Vector3(0, 0, 0);
            toggleObj.transform.localRotation = Quaternion.identity;
            toggleObj.transform.localScale = Vector3.one;

            // 添加RectTransform
            RectTransform rectTransform = toggleObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);

            // 添加开关组件
            VRToggle toggle = toggleObj.AddComponent<VRToggle>();
            toggle.SetLabel("测试开关");

            return toggle;
        }

        /// <summary>
        /// 创建测试滑块
        /// </summary>
        private VRSlider CreateTestSlider()
        {
            GameObject sliderObj = new GameObject("TestSlider");
            sliderObj.transform.SetParent(m_testPanel);
            sliderObj.transform.localPosition = new Vector3(0, -0.1f, 0);
            sliderObj.transform.localRotation = Quaternion.identity;
            sliderObj.transform.localScale = Vector3.one;

            // 添加RectTransform
            RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);

            // 添加滑块组件
            VRSlider slider = sliderObj.AddComponent<VRSlider>();
            slider.SetRange(0, 100);
            slider.SetValue(50);
            slider.SetLabel("测试滑块");

            return slider;
        }

        /// <summary>
        /// 创建状态文本
        /// </summary>
        private TextMeshProUGUI CreateStatusText()
        {
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(m_testPanel);
            textObj.transform.localPosition = new Vector3(0, -0.2f, 0);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;

            // 添加RectTransform
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 100);

            // 添加文本组件
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = "组件状态将显示在这里";

            return text;
        }

        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        private void OnButtonClick()
        {
            Debug.Log("按钮被点击");

            // 随机改变开关状态
            if (m_testToggle != null)
            {
                m_testToggle.SetIsOn(!m_testToggle.GetIsOn());
            }

            // 随机改变滑块值
            if (m_testSlider != null)
            {
                m_testSlider.SetValue(Random.Range(0, 100));
            }

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 开关值变化事件处理
        /// </summary>
        private void OnToggleValueChanged(bool isOn)
        {
            Debug.Log($"开关状态变为: {isOn}");

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 滑块值变化事件处理
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            Debug.Log($"滑块值变为: {value}");

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText()
        {
            if (m_statusText == null)
                return;

            string buttonState = m_testButton != null ? "可用" : "未创建";
            string toggleState = m_testToggle != null ? (m_testToggle.GetIsOn() ? "开启" : "关闭") : "未创建";
            string sliderValue = m_testSlider != null ? m_testSlider.GetValue().ToString("F2") : "N/A";

            m_statusText.text = $"按钮: {buttonState}\n开关: {toggleState}\n滑块: {sliderValue}";
        }
    }
}