using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PongHub.UI.Settings
{
    /// <summary>
    /// 设置菜单面板
    /// 管理各种设置选项和子面板
    /// </summary>
    public class SettingsMenuPanel : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject m_audioPanel;
        [SerializeField] private GameObject m_visualPanel;
        [SerializeField] private GameObject m_controlsPanel;
        [SerializeField] private GameObject m_languagePanel;

        [Header("按钮")]
        [SerializeField] private Button m_audioButton;
        [SerializeField] private Button m_visualButton;
        [SerializeField] private Button m_controlsButton;
        [SerializeField] private Button m_languageButton;
        [SerializeField] private Button m_backButton;

        [Header("本地化文本")]
        [SerializeField] private TextMeshProUGUI m_titleText;

        // 当前活动面板
        private GameObject m_activePanel;

        // 语言设置面板
        private LanguageSettingsPanel m_languageSettingsPanel;

        #region Unity生命周期

        private void Awake()
        {
            // 获取语言设置面板
            if (m_languagePanel != null)
            {
                m_languageSettingsPanel = m_languagePanel.GetComponent<LanguageSettingsPanel>();
            }
        }

        private void OnEnable()
        {
            // 注册按钮事件
            if (m_audioButton != null) m_audioButton.onClick.AddListener(() => ShowPanel(m_audioPanel));
            if (m_visualButton != null) m_visualButton.onClick.AddListener(() => ShowPanel(m_visualPanel));
            if (m_controlsButton != null) m_controlsButton.onClick.AddListener(() => ShowPanel(m_controlsPanel));
            if (m_languageButton != null) m_languageButton.onClick.AddListener(() => ShowPanel(m_languagePanel));
            if (m_backButton != null) m_backButton.onClick.AddListener(OnBackButtonClicked);

            // 默认显示音频面板
            ShowPanel(m_audioPanel);
        }

        private void OnDisable()
        {
            // 取消注册按钮事件
            if (m_audioButton != null) m_audioButton.onClick.RemoveListener(() => ShowPanel(m_audioPanel));
            if (m_visualButton != null) m_visualButton.onClick.RemoveListener(() => ShowPanel(m_visualPanel));
            if (m_controlsButton != null) m_controlsButton.onClick.RemoveListener(() => ShowPanel(m_controlsPanel));
            if (m_languageButton != null) m_languageButton.onClick.RemoveListener(() => ShowPanel(m_languagePanel));
            if (m_backButton != null) m_backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示指定面板
        /// </summary>
        /// <param name="panel">要显示的面板</param>
        public void ShowPanel(GameObject panel)
        {
            // 隐藏当前活动面板
            if (m_activePanel != null)
            {
                m_activePanel.SetActive(false);
            }

            // 显示新面板
            if (panel != null)
            {
                panel.SetActive(true);
                m_activePanel = panel;

                // 如果是语言面板，刷新语言列表
                if (panel == m_languagePanel && m_languageSettingsPanel != null)
                {
                    m_languageSettingsPanel.RefreshLanguageList();
                }
            }

            // 更新按钮状态
            UpdateButtonStates();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            // 设置按钮选中状态
            if (m_audioButton != null) SetButtonSelected(m_audioButton, m_activePanel == m_audioPanel);
            if (m_visualButton != null) SetButtonSelected(m_visualButton, m_activePanel == m_visualPanel);
            if (m_controlsButton != null) SetButtonSelected(m_controlsButton, m_activePanel == m_controlsPanel);
            if (m_languageButton != null) SetButtonSelected(m_languageButton, m_activePanel == m_languagePanel);
        }

        /// <summary>
        /// 设置按钮选中状态
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="selected">是否选中</param>
        private void SetButtonSelected(Button button, bool selected)
        {
            // 获取按钮的颜色块
            ColorBlock colors = button.colors;

            // 设置按钮颜色
            if (selected)
            {
                button.interactable = false;
                colors.disabledColor = colors.selectedColor;
            }
            else
            {
                button.interactable = true;
            }

            // 应用颜色
            button.colors = colors;
        }

        /// <summary>
        /// 返回按钮点击事件
        /// </summary>
        private void OnBackButtonClicked()
        {
            // 如果有父菜单控制器，则调用其返回方法
            MainMenu.MainMenuController menuController = GetComponentInParent<MainMenu.MainMenuController>();
            if (menuController != null)
            {
                menuController.ShowMainPanel();
            }
        }

        #endregion
    }
}