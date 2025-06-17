// Copyright (c) MagnusLab Inc. and affiliates.

using Oculus.Platform;
using Oculus.Platform.Models;
using TMPro;
using PongHub.App;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 好友列表元素类，用于显示好友信息、在线状态，并处理加入/观战按钮点击事件
    /// </summary>
    public class JoinFriendListElement : MonoBehaviour
    {
        // UI组件引用
        [SerializeField] private TMP_Text m_usernameText;        // 显示好友用户名的文本组件
        [SerializeField] private TMP_Text m_destinationText;     // 显示好友当前状态的文本组件
        [SerializeField] private Button m_joinButton;            // 加入游戏按钮
        [SerializeField] private Button m_watchButton;           // 观战按钮

        // 内部状态变量
        private FriendsMenuController m_friendsMenuController;   // 好友菜单控制器引用
        private User m_user;                                     // 当前好友用户数据

        /// <summary>
        /// 初始化好友列表元素
        /// </summary>
        /// <param name="menuController">好友菜单控制器</param>
        /// <param name="user">好友用户数据</param>
        public void Init(FriendsMenuController menuController, User user)
        {
            // 保存控制器和用户数据引用
            m_friendsMenuController = menuController;
            m_user = user;

            // 设置用户名显示
            m_usernameText.text = user.DisplayName;

            // 检查好友是否在线且有可加入的游戏会话
            var canJoin = user.PresenceStatus == UserPresenceStatus.Online &&
                          (!string.IsNullOrEmpty(user.PresenceMatchSessionId) ||
                           !string.IsNullOrEmpty(user.PresenceLobbySessionId));

            // 根据状态显示/隐藏按钮
            m_joinButton.gameObject.SetActive(canJoin);
            m_watchButton.gameObject.SetActive(canJoin);

            // 设置状态文本显示
            m_destinationText.text = user.PresenceStatus == UserPresenceStatus.Online
                ? string.IsNullOrWhiteSpace(user.PresenceDestinationApiName)
                    ? user.Presence  // 如果没有目标API名称，显示一般状态
                    : PHApplication.Instance.PlayerPresenceHandler.GetDestinationDisplayName(
                        user.PresenceDestinationApiName)  // 显示具体游戏场景名称
                : "Offline";  // 离线状态
        }

        /// <summary>
        /// 处理加入游戏按钮点击事件
        /// </summary>
        public void OnJoinClicked()
        {
            if (m_user != null)
            {
                // 优先使用大厅会话ID，如果没有则使用比赛会话ID
                var sessionId = m_user.PresenceLobbySessionId ?? m_user.PresenceMatchSessionId;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // 通知控制器加入游戏
                    m_friendsMenuController.OnJoinMatchClicked(m_user.PresenceDestinationApiName, sessionId);
                }
            }
        }

        /// <summary>
        /// 处理观战按钮点击事件
        /// </summary>
        public void OnWatchClicked()
        {
            if (m_user != null)
            {
                // 优先使用大厅会话ID，如果没有则使用比赛会话ID
                var sessionId = m_user.PresenceLobbySessionId ?? m_user.PresenceMatchSessionId;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // 通知控制器观战游戏
                    m_friendsMenuController.OnWatchMatchClicked(m_user.PresenceDestinationApiName, sessionId);
                }
            }
        }
    }
}