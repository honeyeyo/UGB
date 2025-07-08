using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 设置主面板
    /// Main settings panel that manages navigation between different setting categories
    /// </summary>
    public class SettingsMainPanel : MonoBehaviour
    {
        [Header("面板配置")]
        [SerializeField]
        [Tooltip("面板标题文本")]
        private TextMeshProUGUI titleText;

        [SerializeField]
        [Tooltip("设置分类按钮组")]
        private Transform categoryButtonsParent;

        [SerializeField]
        [Tooltip("设置内容区域")]
        private Transform contentArea;

        [Header("分类按钮")]
        [SerializeField]
        [Tooltip("音频设置按钮")]
        private Button audioButton;

        [SerializeField]
        [Tooltip("视频设置按钮")]
        private Button videoButton;

        [SerializeField]
        [Tooltip("控制设置按钮")]
        private Button controlButton;

        [SerializeField]
        [Tooltip("游戏设置按钮")]
        private Button gameplayButton;

        [SerializeField]
        [Tooltip("用户资料按钮")]
        private Button profileButton;

        [Header("设置面板")]
        [SerializeField]
        [Tooltip("音频设置面板")]
        private AudioSettingsPanel audioPanel;

        [SerializeField]
        [Tooltip("视频设置面板")]
        private VideoSettingsPanel videoPanel;

        [SerializeField]
        [Tooltip("控制设置面板")]
        private ControlSettingsPanel controlPanel;

        [SerializeField]
        [Tooltip("游戏设置面板")]
        private GameplaySettingsPanel gameplayPanel;

        [SerializeField]
        [Tooltip("用户资料面板")]
        private UserProfilePanel profilePanel;

        [Header("操作按钮")]
        [SerializeField]
        [Tooltip("重置按钮")]
        private Button resetButton;

        [SerializeField]
        [Tooltip("导入按钮")]
        private Button importButton;

        [SerializeField]
        [Tooltip("导出按钮")]
        private Button exportButton;

        [SerializeField]
        [Tooltip("关闭按钮")]
        private Button closeButton;

        [Header("视觉效果")]
        [SerializeField]
        [Tooltip("选中按钮的高亮颜色")]
        private Color selectedButtonColor = Color.blue;

        [SerializeField]
        [Tooltip("默认按钮颜色")]
        private Color defaultButtonColor = Color.white;

        // 设置类型枚举
        public enum SettingsCategory
        {
            Audio,
            Video,
            Control,
            Gameplay,
            Profile
        }

        // 内部状态
        private SettingsCategory currentCategory = SettingsCategory.Audio;
        private SettingsManager settingsManager;
        private VRHapticFeedback hapticFeedback;
        private Dictionary<SettingsCategory, GameObject> categoryPanels;
        private Dictionary<SettingsCategory, Button> categoryButtons;

        // 事件
        public System.Action<SettingsCategory> OnCategoryChanged;
        public System.Action OnSettingsClosed;

        #region Unity 生命周期

        private void Awake()
        {
            InitializeComponents();
            SetupCategoryMappings();
        }

        private void Start()
        {
            SetupUI();
            ShowCategory(SettingsCategory.Audio);
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                settingsManager = FindObjectOfType<SettingsManager>();
            }

            hapticFeedback = FindObjectOfType<VRHapticFeedback>();

            // 查找面板引用
            if (audioPanel == null)
                audioPanel = GetComponentInChildren<AudioSettingsPanel>();
            if (videoPanel == null)
                videoPanel = GetComponentInChildren<VideoSettingsPanel>();
            if (controlPanel == null)
                controlPanel = GetComponentInChildren<ControlSettingsPanel>();
            if (gameplayPanel == null)
                gameplayPanel = GetComponentInChildren<GameplaySettingsPanel>();
            if (profilePanel == null)
                profilePanel = GetComponentInChildren<UserProfilePanel>();
        }

        /// <summary>
        /// 设置分类映射
        /// </summary>
        private void SetupCategoryMappings()
        {
            categoryPanels = new Dictionary<SettingsCategory, GameObject>
            {
                { SettingsCategory.Audio, audioPanel?.gameObject },
                { SettingsCategory.Video, videoPanel?.gameObject },
                { SettingsCategory.Control, controlPanel?.gameObject },
                { SettingsCategory.Gameplay, gameplayPanel?.gameObject },
                { SettingsCategory.Profile, profilePanel?.gameObject }
            };

            categoryButtons = new Dictionary<SettingsCategory, Button>
            {
                { SettingsCategory.Audio, audioButton },
                { SettingsCategory.Video, videoButton },
                { SettingsCategory.Control, controlButton },
                { SettingsCategory.Gameplay, gameplayButton },
                { SettingsCategory.Profile, profileButton }
            };
        }

        /// <summary>
        /// 设置UI组件
        /// </summary>
        private void SetupUI()
        {
            // 设置标题
            if (titleText != null)
            {
                titleText.text = "游戏设置";
            }

            // 设置分类按钮
            if (audioButton != null)
                audioButton.onClick.AddListener(() => ShowCategory(SettingsCategory.Audio));
            if (videoButton != null)
                videoButton.onClick.AddListener(() => ShowCategory(SettingsCategory.Video));
            if (controlButton != null)
                controlButton.onClick.AddListener(() => ShowCategory(SettingsCategory.Control));
            if (gameplayButton != null)
                gameplayButton.onClick.AddListener(() => ShowCategory(SettingsCategory.Gameplay));
            if (profileButton != null)
                profileButton.onClick.AddListener(() => ShowCategory(SettingsCategory.Profile));

            // 设置操作按钮
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetSettings);
            if (importButton != null)
                importButton.onClick.AddListener(OnImportSettings);
            if (exportButton != null)
                exportButton.onClick.AddListener(OnExportSettings);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseSettings);

            // 初始化面板状态
            HideAllPanels();
        }

        #endregion

        #region 分类导航

        /// <summary>
        /// 显示指定分类的设置面板
        /// </summary>
        /// <param name="category">设置分类</param>
        public void ShowCategory(SettingsCategory category)
        {
            if (currentCategory == category) return;

            // 隐藏当前面板
            HideAllPanels();

            // 显示目标面板
            if (categoryPanels.ContainsKey(category) && categoryPanels[category] != null)
            {
                categoryPanels[category].SetActive(true);
                currentCategory = category;

                // 更新按钮状态
                UpdateButtonStates();

                // 触发事件
                OnCategoryChanged?.Invoke(category);

                // 触觉反馈
                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.PageChange);
                }
            }
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        private void HideAllPanels()
        {
            foreach (var panel in categoryPanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            foreach (var kvp in categoryButtons)
            {
                if (kvp.Value != null)
                {
                    var isSelected = kvp.Key == currentCategory;
                    SetButtonSelected(kvp.Value, isSelected);
                }
            }
        }

        /// <summary>
        /// 设置按钮选中状态
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="selected">是否选中</param>
        private void SetButtonSelected(Button button, bool selected)
        {
            var colors = button.colors;
            if (selected)
            {
                colors.normalColor = selectedButtonColor;
                colors.highlightedColor = selectedButtonColor;
                colors.selectedColor = selectedButtonColor;
            }
            else
            {
                colors.normalColor = defaultButtonColor;
                colors.highlightedColor = Color.Lerp(defaultButtonColor, Color.white, 0.2f);
                colors.selectedColor = defaultButtonColor;
            }
            button.colors = colors;
        }

        #endregion

        #region 操作按钮事件

        /// <summary>
        /// 重置设置
        /// </summary>
        private void OnResetSettings()
        {
            if (settingsManager != null)
            {
                // 显示确认对话框（简化版）
                if (Application.isEditor)
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("重置设置", "确定要重置所有设置为默认值吗？", "确定", "取消"))
                    {
                        settingsManager.ResetToDefaults();
                    }
                }
                else
                {
                    // 在VR中可以使用自定义确认UI
                    settingsManager.ResetToDefaults();
                }

                // 触觉反馈
                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Warning);
                }
            }
        }

        /// <summary>
        /// 导入设置
        /// </summary>
        private void OnImportSettings()
        {
            // TODO: 实现文件选择器或预定义路径
            if (settingsManager != null)
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "exported_settings.json");
                _ = settingsManager.ImportSettingsAsync(filePath);

                // 触觉反馈
                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
                }
            }
        }

        /// <summary>
        /// 导出设置
        /// </summary>
        private void OnExportSettings()
        {
            if (settingsManager != null)
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "exported_settings.json");
                _ = settingsManager.ExportSettingsAsync(filePath);

                // 触觉反馈
                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
                }
            }
        }

        /// <summary>
        /// 关闭设置面板
        /// </summary>
        private void OnCloseSettings()
        {
            // 保存设置
            if (settingsManager != null)
            {
                settingsManager.SaveSettings();
            }

            // 触发关闭事件
            OnSettingsClosed?.Invoke();

            // 触觉反馈
            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Back);
            }

            // 隐藏面板
            gameObject.SetActive(false);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 注册事件
        /// </summary>
        private void RegisterEvents()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnSettingsChanged += OnSettingsChanged;
            }
        }

        /// <summary>
        /// 取消注册事件
        /// </summary>
        private void UnregisterEvents()
        {
            if (settingsManager != null)
            {
                SettingsManager.OnSettingsChanged -= OnSettingsChanged;
            }
        }

        /// <summary>
        /// 设置变更事件处理
        /// </summary>
        /// <param name="settings">新设置</param>
        private void OnSettingsChanged(GameSettings settings)
        {
            // 通知所有子面板刷新
            RefreshAllPanels();
        }

        /// <summary>
        /// 刷新所有面板
        /// </summary>
        private void RefreshAllPanels()
        {
            audioPanel?.RefreshPanel();
            videoPanel?.RefreshPanel();
            controlPanel?.RefreshPanel();
            gameplayPanel?.RefreshPanel();
            profilePanel?.RefreshPanel();
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 初始化设置面板
        /// </summary>
        public void Initialize()
        {
            InitializeComponents();
            SetupCategoryMappings();
            SetupUI();
        }

        /// <summary>
        /// 显示设置面板
        /// </summary>
        /// <param name="initialCategory">初始显示的分类</param>
        public void ShowPanel(SettingsCategory initialCategory = SettingsCategory.Audio)
        {
            gameObject.SetActive(true);
            ShowCategory(initialCategory);
        }

        /// <summary>
        /// 隐藏设置面板
        /// </summary>
        public void HidePanel()
        {
            OnCloseSettings();
        }

        /// <summary>
        /// 获取当前分类
        /// </summary>
        /// <returns>当前设置分类</returns>
        public SettingsCategory GetCurrentCategory()
        {
            return currentCategory;
        }

        #endregion

        #region 清理

        private void OnDestroy()
        {
            UnregisterEvents();

            // 清理按钮事件
            if (audioButton != null)
                audioButton.onClick.RemoveAllListeners();
            if (videoButton != null)
                videoButton.onClick.RemoveAllListeners();
            if (controlButton != null)
                controlButton.onClick.RemoveAllListeners();
            if (gameplayButton != null)
                gameplayButton.onClick.RemoveAllListeners();
            if (profileButton != null)
                profileButton.onClick.RemoveAllListeners();

            if (resetButton != null)
                resetButton.onClick.RemoveAllListeners();
            if (importButton != null)
                importButton.onClick.RemoveAllListeners();
            if (exportButton != null)
                exportButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}