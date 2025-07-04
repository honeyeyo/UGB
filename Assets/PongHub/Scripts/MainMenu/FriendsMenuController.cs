// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using PongHub.App;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 好友菜单控制器类
    /// 负责加载和显示用户的好友列表，处理加入或观战好友游戏的功能
    /// 使用Platform API获取好友数据，并管理好友列表的UI显示
    /// </summary>
    public class FriendsMenuController : BaseMenuController
    {
        // 加载动画的旋转速度（负值表示逆时针旋转）
        private const float LOADING_ROTATION_SPEED = -100f;

        [Header("UI预制体和容器")]
        [SerializeField]
        [Tooltip("Friend List Element Prefab / 好友列表项预制体 - Prefab for friend list UI elements")]
        private JoinFriendListElement m_friendListElementPrefab;  // 好友列表项预制体

        [SerializeField]
        [Tooltip("Content Transform / 内容容器变换 - Transform container for friend list layout")]
        private Transform m_contentTransform;                      // 好友列表的容器Transform

        [Header("运行时数据")]
        [SerializeField]
        [Tooltip("Spawned Elements / 已生成元素列表 - List of instantiated friend list elements")]
        private List<JoinFriendListElement> m_spawnedElements;    // 已生成的好友列表项列表

        [Header("其他引用")]
        [SerializeField]
        [Tooltip("Main Menu Controller / 主菜单控制器 - Reference to main menu controller")]
        private MainMenuController m_mainMenuController;          // 主菜单控制器引用

        [SerializeField]
        [Tooltip("No Friends Message / 无好友消息 - GameObject shown when no friends available")]
        private GameObject m_noFriendsMessage;                    // 无好友时显示的提示信息

        [SerializeField]
        [Tooltip("Loading Image / 加载图片 - Image component for loading animation")]
        private Image m_loadingImage;                             // 加载动画图片

        // 是否正在加载好友列表的标志
        private bool m_isLoadingFriendsList = false;

        /// <summary>
        /// 当菜单启用时调用
        /// 初始化好友列表并开始加载数据
        /// </summary>
        public void OnEnable()
        {
            HideAllFriends();  // 隐藏所有已存在的好友项
            StartLoadingFriendsList();  // 开始加载动画
            _ = Users.GetLoggedInUserFriends().OnComplete(OnFriendListReceived);  // 获取好友列表
        }

        /// <summary>
        /// 处理加入好友游戏的请求
        /// </summary>
        /// <param name="destinationAPI">目标API名称</param>
        /// <param name="sessionId">游戏会话ID</param>
        public void OnJoinMatchClicked(string destinationAPI, string sessionId)
        {
            m_mainMenuController.DisableButtons();  // 禁用主菜单按钮
            PHApplication.Instance.NavigationController.JoinMatch(destinationAPI, sessionId);  // 加入游戏
        }

        /// <summary>
        /// 处理观战好友游戏的请求
        /// </summary>
        /// <param name="destinationAPI">目标API名称</param>
        /// <param name="sessionId">游戏会话ID</param>
        public void OnWatchMatchClicked(string destinationAPI, string sessionId)
        {
            m_mainMenuController.DisableButtons();  // 禁用主菜单按钮
            PHApplication.Instance.NavigationController.WatchMatch(destinationAPI, sessionId);  // 观战游戏
        }

        /// <summary>
        /// 开始加载好友列表
        /// 显示加载动画并启动旋转协程
        /// </summary>
        private void StartLoadingFriendsList()
        {
            m_isLoadingFriendsList = true;
            m_loadingImage.enabled = true;
            _ = StartCoroutine(RotateLoadingImage());
        }

        /// <summary>
        /// 旋转加载图片的协程
        /// 在加载过程中持续旋转加载图标
        /// </summary>
        private IEnumerator RotateLoadingImage()
        {
            while (m_isLoadingFriendsList)
            {
                m_loadingImage.transform.Rotate(0, 0, LOADING_ROTATION_SPEED * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// 处理好友列表数据接收完成的回调
        /// 创建并初始化好友列表项
        /// </summary>
        /// <param name="users">包含好友列表数据的消息对象</param>
        private void OnFriendListReceived(Message<Oculus.Platform.Models.UserList> users)
        {
            m_isLoadingFriendsList = false;
            m_loadingImage.enabled = false;

            var i = 0;
            foreach (var user in users.Data)
            {
                // 如果现有列表项不足，创建新的列表项
                if (i >= m_spawnedElements.Count)
                {
                    SpawnNewFriendElement();
                }

                // 初始化并显示好友列表项
                m_spawnedElements[i].Init(this, user);
                m_spawnedElements[i].gameObject.SetActive(true);
                i++;
            }

            // 如果没有好友，显示提示信息
            m_noFriendsMessage.SetActive(users.Data.Count == 0);
        }

        /// <summary>
        /// 隐藏所有好友列表项
        /// </summary>
        private void HideAllFriends()
        {
            foreach (var friendElem in m_spawnedElements)
            {
                friendElem.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 创建新的好友列表项
        /// 实例化预制体并添加到列表中
        /// </summary>
        private void SpawnNewFriendElement()
        {
            m_spawnedElements.Add(Instantiate(m_friendListElementPrefab, m_contentTransform, false));
        }
    }
}