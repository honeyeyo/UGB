// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using PongHub.App;
using PongHub.Arena.Player;
using PongHub.Arena.Services;
using PongHub.Utils;
using UnityEngine;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 主菜单场景控制器
    /// 负责管理主菜单场景的状态，处理不同菜单之间的导航和状态变化
    /// 实现各种按钮点击事件的处理函数
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>
        /// 菜单状态枚举
        /// 定义了所有可能的菜单状态
        /// </summary>
        private enum MenuState
        {
            Main,       // 主菜单
            Options,    // 选项菜单
            Controls,   // 控制菜单
            Friends,    // 好友菜单
            Settings,   // 设置菜单
            BallTypes,  // 球类型菜单
            Store,      // 商店菜单
        }

        [Header("主画布")]
        [SerializeField]
        [Tooltip("Canvas Collider / 画布碰撞体 - Main canvas collider for UI interaction")]
        private BoxCollider m_canvasCollider;  // 主画布的碰撞体

        [Header("菜单控制器")]
        [SerializeField]
        [Tooltip("Main Menu Controller / 主菜单控制器 - Controller for main menu view")]
        private BaseMenuController m_mainMenuController;      // 主菜单控制器

        [SerializeField]
        [Tooltip("Option Menu Controller / 选项菜单控制器 - Controller for option menu view")]
        private BaseMenuController m_optionMenuController;    // 选项菜单控制器

        [SerializeField]
        [Tooltip("Controls Menu Controller / 控制菜单控制器 - Controller for controls menu view")]
        private BaseMenuController m_controlsMenuController;  // 控制菜单控制器

        [SerializeField]
        [Tooltip("Friends Menu Controller / 好友菜单控制器 - Controller for friends menu view")]
        private BaseMenuController m_friendsMenuController;   // 好友菜单控制器

        [SerializeField]
        [Tooltip("Settings Menu Controller / 设置菜单控制器 - Controller for settings menu view")]
        private BaseMenuController m_settingsMenuController;  // 设置菜单控制器

        [SerializeField]
        [Tooltip("Ball Types Menu Controller / 球类型菜单控制器 - Controller for ball types menu view")]
        private BaseMenuController m_ballTypesMenuController; // 球类型菜单控制器

        [SerializeField]
        [Tooltip("Store Menu Controller / 商店菜单控制器 - Controller for store menu view")]
        private BaseMenuController m_storeMenuController;     // 商店菜单控制器

        [Header("错误面板")]
        [SerializeField]
        [Tooltip("Error Panel / 错误面板 - Panel for displaying error messages")]
        private MenuErrorPanel m_errorPanel;  // 错误信息显示面板

        [Header("锚点")]
        [SerializeField]
        [Tooltip("Start Position / 起始位置 - Starting position transform for player")]
        private Transform m_startPosition;    // 起始位置

        [SerializeField]
        [Tooltip("Avatar Transform / 玩家头像变换 - Player avatar transform component")]
        private Transform m_avatarTransform;  // 玩家头像变换

        [SerializeField]
        [Tooltip("Menu Music Fader / 菜单音乐淡入淡出 - Audio fade controller for menu music")]
        private AudioFadeInOut m_menuMusicFader;  // 菜单音乐淡入淡出控制器

        private BaseMenuController m_currentMenu;  // 当前活动的菜单控制器
        private float m_baseMenuMusicVolume;      // 基础菜单音乐音量
        private MenuState m_currentState;         // 当前菜单状态

        /// <summary>
        /// 初始化函数
        /// 设置玩家位置和初始菜单状态
        /// </summary>
        private void Awake()
        {
            // 将玩家位置设置到起始位置
            PlayerMovement.Instance.SnapPositionToTransform(m_startPosition);
            // 立即设置头像位置以避免延迟
            m_avatarTransform.SetPositionAndRotation(m_startPosition.position, m_startPosition.rotation);
            // 设置初始菜单为主菜单
            m_currentMenu = m_mainMenuController;
        }

        /// <summary>
        /// 启动函数
        /// 执行场景淡入效果并重置猫咪状态
        /// </summary>
        private void Start()
        {
            // 执行屏幕淡入效果
            OVRScreenFade.instance.FadeIn();
            // 重置猫咪状态
            LocalPlayerState.Instance.SpawnCatInNextGame = false;
        }

        /// <summary>
        /// 快速匹配按钮点击处理
        /// </summary>
        public void OnQuickMatchClicked()
        {
            Debug.Log("QUICK MATCH");
            DisableButtons();
            PHApplication.Instance.NavigationController.NavigateToMatch(false);
            m_menuMusicFader.FadeOut();
        }

        /// <summary>
        /// 创建房间按钮点击处理
        /// </summary>
        public void OnHostMatchClicked()
        {
            Debug.Log("HOST MATCH");
            DisableButtons();
            PHApplication.Instance.NavigationController.NavigateToMatch(true);
            m_menuMusicFader.FadeOut();
        }

        /// <summary>
        /// 观战按钮点击处理
        /// </summary>
        public void OnWatchMatchClicked()
        {
            Debug.Log("WATCH MATCH");
            DisableButtons();
            PHApplication.Instance.NavigationController.WatchRandomMatch();
            m_menuMusicFader.FadeOut();
        }

        /// <summary>
        /// 好友菜单按钮点击处理
        /// </summary>
        public void OnFriendsClicked()
        {
            ChangeMenuState(MenuState.Friends);
        }

        /// <summary>
        /// 好友菜单返回按钮点击处理
        /// </summary>
        public void OnFriendsBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        /// <summary>
        /// 选项菜单按钮点击处理
        /// </summary>
        public void OnOptionsClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        /// <summary>
        /// 设置菜单按钮点击处理
        /// </summary>
        public void OnSettingsClicked()
        {
            ChangeMenuState(MenuState.Settings);
        }

        /// <summary>
        /// 设置菜单返回按钮点击处理
        /// </summary>
        public void OnSettingsBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        /// <summary>
        /// 商店按钮点击处理
        /// </summary>
        public void OnStoreClicked()
        {
            ChangeMenuState(MenuState.Store);
        }

        /// <summary>
        /// 商店返回按钮点击处理
        /// </summary>
        public void OnStoreBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        /// <summary>
        /// 球类型按钮点击处理
        /// </summary>
        public void OnBallTypesClicked()
        {
            ChangeMenuState(MenuState.BallTypes);
        }

        /// <summary>
        /// 球类型返回按钮点击处理
        /// </summary>
        public void OnBallTypesBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        /// <summary>
        /// 退出按钮点击处理
        /// </summary>
        public void OnExitClicked()
        {
            DisableButtons();
            Application.Quit();
        }

        /// <summary>
        /// 选项菜单返回按钮点击处理
        /// </summary>
        public void OnOptionsBackClicked()
        {
            ChangeMenuState(MenuState.Main);
        }

        /// <summary>
        /// 控制菜单按钮点击处理
        /// </summary>
        public void OnControlsClicked()
        {
            ChangeMenuState(MenuState.Controls);
        }

        /// <summary>
        /// 控制菜单返回按钮点击处理
        /// </summary>
        public void OnControlsBackClicked()
        {
            ChangeMenuState(MenuState.Options);
        }

        /// <summary>
        /// 启用所有按钮
        /// </summary>
        public void EnableButtons()
        {
            m_canvasCollider.enabled = true;
            m_currentMenu.EnableButtons();
        }

        /// <summary>
        /// 禁用所有按钮
        /// </summary>
        public void DisableButtons()
        {
            m_canvasCollider.enabled = false;
            m_currentMenu.DisableButtons();
        }

        /// <summary>
        /// 错误面板关闭按钮点击处理
        /// </summary>
        public void OnErrorPanelCloseClicked()
        {
            m_errorPanel.Close();
            ChangeMenuState(m_currentState);
        }

        /// <summary>
        /// 返回主菜单处理
        /// </summary>
        /// <param name="connectionStatus">连接状态</param>
        public void OnReturnToMenu(ArenaApprovalController.ConnectionStatus connectionStatus)
        {
            EnableButtons();
            m_menuMusicFader.FadeIn();
            if (connectionStatus != ArenaApprovalController.ConnectionStatus.Success)
            {
                var errorMsg = connectionStatus switch
                {
                    ArenaApprovalController.ConnectionStatus.Undefined => LocalPlayerState.Instance.IsSpectator
                                                ? "No match found to spectate, please try later."
                                                : "An error occured when trying to join.",
                    ArenaApprovalController.ConnectionStatus.PlayerFull =>
                        "No more space for players, you can try to join a different arena.",
                    ArenaApprovalController.ConnectionStatus.SpectatorFull =>
                        "This arena has reached it's spectator limit.",
                    ArenaApprovalController.ConnectionStatus.Success => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException(nameof(connectionStatus), connectionStatus, null),
                };
                ShowErrorMessage(errorMsg);
            }
        }

        /// <summary>
        /// 显示错误信息事件处理
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        public void OnShowErrorMsgEvent(string errorMsg)
        {
            ShowErrorMessage(errorMsg);
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        private void ShowErrorMessage(string errorMsg)
        {
            m_currentMenu.Hide();
            m_errorPanel.ShowMessage(errorMsg);
        }

        /// <summary>
        private void ChangeMenuState(MenuState newState)
        {
            m_currentState = newState;
            m_currentMenu.Hide();

            m_currentMenu = newState switch
            {
                MenuState.Main => m_mainMenuController,
                MenuState.Options => m_optionMenuController,
                MenuState.Controls => m_controlsMenuController,
                MenuState.Friends => m_friendsMenuController,
                MenuState.Settings => m_settingsMenuController,
                MenuState.BallTypes => m_ballTypesMenuController,
                MenuState.Store => m_storeMenuController,
                _ => throw new ArgumentOutOfRangeException(nameof(newState), newState, null)
            };
            m_currentMenu.Show();
            EnableButtons();
        }
    }
}