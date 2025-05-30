// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 阻止用户管理器
    /// 管理当前用户的被阻止用户列表
    /// 使用Oculus平台的阻止用户API获取被阻止用户列表并处理阻止流程
    /// https://developer.oculus.com/documentation/unity/ps-blockingsdk/
    /// </summary>
    public class BlockUserManager
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static BlockUserManager s_instance;

        /// <summary>
        /// 获取单例实例
        /// 如果实例不存在则创建新实例
        /// </summary>
        public static BlockUserManager Instance
        {
            get
            {
                s_instance ??= new BlockUserManager();

                return s_instance;
            }
        }

        /// <summary>
        /// 被阻止用户的ID集合
        /// 使用HashSet确保快速查找和唯一性
        /// </summary>
        private HashSet<ulong> m_blockedUsers = new();

        /// <summary>
        /// 私有构造函数，确保单例模式
        /// </summary>
        private BlockUserManager()
        {
        }

        /// <summary>
        /// 初始化阻止用户管理器
        /// 从Oculus平台获取当前用户的被阻止用户列表
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task Initialize()
        {
            // 获取被阻止用户列表
            var message = await Users.GetBlockedUsers().Gen();
            Debug.Log("EXTRACTING BLOCKED USER DATA");

            if (message.IsError)
            {
                Debug.Log("Could not get the list of users blocked!");
                Debug.LogError(message.GetError());
                return;
            }

            // 遍历所有被阻止用户页面
            while (message != null)
            {
                var blockedUsers = message.GetBlockedUserList();

                // 将被阻止的用户添加到集合中
                foreach (var blockedUser in blockedUsers)
                {
                    m_blockedUsers.Add(blockedUser.ID);
                    Debug.Log($"User {blockedUser.DisplayName} (ID: {blockedUser.ID}) is blocked");
                }

                // 检查是否有下一页
                if (blockedUsers.HasNextPage)
                {
                    // 获取下一页被阻止用户
                    message = await Users.GetNextBlockedUserListPage(blockedUsers).Gen();
                    if (message.IsError)
                    {
                        Debug.LogError("Error getting next page of blocked users: " + message.GetError());
                        break;
                    }
                }
                else
                {
                    // 没有更多页面，退出循环
                    message = null;
                }
            }

            Debug.Log($"Total blocked users loaded: {m_blockedUsers.Count}");
        }

        /// <summary>
        /// 检查指定用户是否被阻止
        /// </summary>
        /// <param name="userId">要检查的用户ID</param>
        /// <returns>如果用户被阻止返回true，否则返回false</returns>
        public bool IsUserBlocked(ulong userId)
        {
            return m_blockedUsers.Contains(userId);
        }

        public async void BlockUser(ulong userId, Action<ulong> onUserBlockedSuccessful = null)
        {
            if (m_blockedUsers.Contains(userId))
            {
                Debug.LogError($"{userId} is already blocked");
                return;
            }

            var message = await Users.LaunchBlockFlow(userId).Gen();
            if (message.IsError)
            {
                Debug.Log("Error when trying to block the user");
                Debug.LogError(message.Data);
            }
            else
            {
                Debug.Log("Got result: DidBlock = " + message.Data.DidBlock + " DidCancel = " + message.Data.DidCancel);
                if (message.Data.DidBlock)
                {
                    _ = m_blockedUsers.Add(userId);
                    onUserBlockedSuccessful?.Invoke(userId);
                }
            }
        }

        public async void UnblockUser(ulong userId, Action<ulong> onUserUnblockedSuccessful = null)
        {
            if (!m_blockedUsers.Contains(userId))
            {
                Debug.LogError($"{userId} is already unblocked");
                return;
            }

            var message = await Users.LaunchUnblockFlow(userId).Gen();
            if (message.IsError)
            {
                Debug.Log("Error when trying to block the user");
                Debug.LogError(message.Data);
            }
            else
            {
                Debug.Log("Got result: DidUnblock = " + message.Data.DidUnblock + " DidCancel = " + message.Data.DidCancel);
                if (message.Data.DidUnblock)
                {
                    _ = m_blockedUsers.Remove(userId);
                    onUserUnblockedSuccessful?.Invoke(userId);
                }
            }
        }
    }
}