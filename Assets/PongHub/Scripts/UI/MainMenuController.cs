using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PongHub.Core;

namespace PongHub.UI
{
    /// <summary>
    /// 主菜单控制器，负责管理菜单面板的显示和切换，以及与游戏模式管理器的交互
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("面板引用")]
        [SerializeField] private MenuPanelBase m_mainMenuPanel;
        [SerializeField] private MenuPanelBase m_settingsPanel;
        [SerializeField] private MenuPanelBase m_gameModePanel;
        [SerializeField] private MenuPanelBase m_exitConfirmPanel;

        [Header("系统引用")]
        [SerializeField] private TableMenuSystem m_tableMenuSystem;
        [SerializeField] private VRMenuInteraction m_menuInteraction;
        [SerializeField] private GameModeManager m_gameModeManager;

        [Header("配置")]
        // [SerializeField] private float m_panelTransitionTime = 0.3f;     // 面板切换时间（暂未使用）
        [SerializeField] private bool m_showMenuOnStart = true;

        // 当前活动面板
        private MenuPanelBase m_currentPanel;
        // 面板历史记录，用于返回功能
        private Stack<MenuPanelBase> m_panelHistory = new Stack<MenuPanelBase>();
        // 菜单是否可见
        private bool m_isMenuVisible = false;

        #region Unity生命周期

        private void Awake()
        {
            // 初始化所有面板
            InitializePanels();
        }

        private void Start()
        {
            // 注册事件监听
            RegisterEvents();

            // 如果配置为启动时显示菜单，则显示主菜单
            if (m_showMenuOnStart)
            {
                ShowMenu();
            }
            else
            {
                HideMenu();
            }
        }

        private void OnDestroy()
        {
            // 取消事件监听
            UnregisterEvents();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 显示菜单（默认显示主菜单）
        /// </summary>
        public void ShowMenu()
        {
            if (m_isMenuVisible) return;

            m_isMenuVisible = true;
            m_tableMenuSystem.ShowMenu();
            ShowPanel(m_mainMenuPanel);
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void HideMenu()
        {
            if (!m_isMenuVisible) return;

            m_isMenuVisible = false;
            m_currentPanel?.Hide();
            m_tableMenuSystem.HideMenu();
            m_panelHistory.Clear();
        }

        /// <summary>
        /// 切换菜单显示状态
        /// </summary>
        public void ToggleMenu()
        {
            if (m_isMenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        /// <summary>
        /// 显示主菜单面板
        /// </summary>
        public void ShowMainMenuPanel()
        {
            ShowPanel(m_mainMenuPanel);
        }

        /// <summary>
        /// 显示主面板（兼容性方法）
        /// </summary>
        public void ShowMainPanel()
        {
            ShowMainMenuPanel();
        }

        /// <summary>
        /// 检查菜单是否可见
        /// </summary>
        public bool IsMenuVisible
        {
            get { return m_isMenuVisible; }
        }

        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void ShowSettingsPanel()
        {
            ShowPanel(m_settingsPanel);
        }

        /// <summary>
        /// 显示游戏模式选择面板
        /// </summary>
        public void ShowGameModePanel()
        {
            ShowPanel(m_gameModePanel);
        }

        /// <summary>
        /// 显示退出确认面板
        /// </summary>
        public void ShowExitConfirmPanel()
        {
            ShowPanel(m_exitConfirmPanel);
        }

        /// <summary>
        /// 返回上一个面板
        /// </summary>
        public void GoBack()
        {
            if (m_panelHistory.Count > 0)
            {
                MenuPanelBase previousPanel = m_panelHistory.Pop();
                SwitchToPanel(previousPanel, false);
            }
            else
            {
                // 如果没有历史记录，则返回主菜单
                ShowMainMenuPanel();
            }
        }

        /// <summary>
        /// 切换到单机模式
        /// </summary>
        public void SwitchToLocalMode()
        {
            if (m_gameModeManager != null)
            {
                m_gameModeManager.SwitchToMode(GameMode.Local);
                HideMenu();
            }
        }

        /// <summary>
        /// 切换到网络模式
        /// </summary>
        public void SwitchToNetworkMode()
        {
            if (m_gameModeManager != null)
            {
                m_gameModeManager.SwitchToMode(GameMode.Network);
                HideMenu();
            }
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void ExitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化所有面板
        /// </summary>
        private void InitializePanels()
        {
            // 确保所有面板都已初始化
            if (m_mainMenuPanel != null) m_mainMenuPanel.Initialize();
            if (m_settingsPanel != null) m_settingsPanel.Initialize();
            if (m_gameModePanel != null) m_gameModePanel.Initialize();
            if (m_exitConfirmPanel != null) m_exitConfirmPanel.Initialize();

            // 初始时隐藏所有面板
            if (m_mainMenuPanel != null) m_mainMenuPanel.Hide(true);
            if (m_settingsPanel != null) m_settingsPanel.Hide(true);
            if (m_gameModePanel != null) m_gameModePanel.Hide(true);
            if (m_exitConfirmPanel != null) m_exitConfirmPanel.Hide(true);
        }

        /// <summary>
        /// 显示指定面板
        /// </summary>
        private void ShowPanel(MenuPanelBase panel)
        {
            if (panel == null) return;

            // 如果当前有活动面板，则将其加入历史记录
            if (m_currentPanel != null && m_currentPanel != panel)
            {
                m_panelHistory.Push(m_currentPanel);
                SwitchToPanel(panel, true);
            }
            else if (m_currentPanel == null)
            {
                // 如果没有活动面板，直接显示
                panel.Show();
                m_currentPanel = panel;
            }
        }

        /// <summary>
        /// 切换到指定面板
        /// </summary>
        private void SwitchToPanel(MenuPanelBase panel, bool addToHistory)
        {
            if (panel == null || m_currentPanel == panel) return;

            // 隐藏当前面板
            if (m_currentPanel != null)
            {
                MenuPanelBase previousPanel = m_currentPanel;
                previousPanel.Hide();
            }

            // 显示新面板
            panel.Show();
            m_currentPanel = panel;

            // 如果需要，将当前面板加入历史记录
            if (addToHistory && m_currentPanel != null)
            {
                m_panelHistory.Push(m_currentPanel);
            }
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEvents()
        {
            // 这里可以注册按钮事件、输入事件等
            if (m_menuInteraction != null)
            {
                // 示例：注册菜单按钮事件
                // m_menuInteraction.OnMenuButtonPressed += ToggleMenu;
            }
        }

        /// <summary>
        /// 取消事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            // 取消注册的事件
            if (m_menuInteraction != null)
            {
                // 示例：取消注册菜单按钮事件
                // m_menuInteraction.OnMenuButtonPressed -= ToggleMenu;
            }
        }

        #endregion
    }
}