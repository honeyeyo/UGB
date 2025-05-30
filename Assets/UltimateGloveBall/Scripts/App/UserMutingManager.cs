// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;

namespace PongHub.App
{
    /// <summary>
    /// 用户静音管理器
    /// 管理用户语音静音状态，跟踪用户的静音状态
    /// 注册回调以在用户静音状态改变时接收通知
    /// </summary>
    public class UserMutingManager
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static UserMutingManager s_instance;

        /// <summary>
        /// 获取单例实例
        /// 如果实例不存在则创建新实例
        /// </summary>
        public static UserMutingManager Instance
        {
            get
            {
                // 如果实例为空，创建新实例
                s_instance ??= new UserMutingManager();
                return s_instance;
            }
        }

        /// <summary>
        /// 被静音用户的ID集合
        /// 使用HashSet确保快速查找和唯一性
        /// </summary>
        private HashSet<ulong> m_mutedUsers = new();

        /// <summary>
        /// 用户静音状态改变时的回调函数
        /// 参数：用户ID，是否静音
        /// </summary>
        private Action<ulong, bool> m_onUserMutedStateCallback;

        /// <summary>
        /// 私有构造函数，确保单例模式
        /// </summary>
        private UserMutingManager()
        {
        }

        /// <summary>
        /// 注册静音状态改变回调
        /// 当用户静音状态发生变化时会调用此回调
        /// </summary>
        /// <param name="mutedStateCallback">回调函数，接收用户ID和静音状态</param>
        public void RegisterCallback(Action<ulong, bool> mutedStateCallback)
        {
            m_onUserMutedStateCallback += mutedStateCallback;
        }

        /// <summary>
        /// 取消注册静音状态改变回调
        /// 移除之前注册的回调函数
        /// </summary>
        /// <param name="mutedStateCallback">要移除的回调函数</param>
        public void UnregisterCallback(Action<ulong, bool> mutedStateCallback)
        {
            m_onUserMutedStateCallback -= mutedStateCallback;
        }

        /// <summary>
        /// 检查指定用户是否被静音
        /// </summary>
        /// <param name="userId">要检查的用户ID</param>
        /// <returns>如果用户被静音返回true，否则返回false</returns>
        public bool IsUserMuted(ulong userId)
        {
            return m_mutedUsers.Contains(userId);
        }

        /// <summary>
        /// 静音指定用户
        /// 将用户添加到静音列表并触发回调
        /// </summary>
        /// <param name="userId">要静音的用户ID</param>
        public void MuteUser(ulong userId)
        {
            // 添加用户到静音集合
            _ = m_mutedUsers.Add(userId);
            // 触发静音状态改变回调
            m_onUserMutedStateCallback?.Invoke(userId, true);
        }

        /// <summary>
        /// 取消静音指定用户
        /// 从静音列表中移除用户并触发回调
        /// </summary>
        /// <param name="userId">要取消静音的用户ID</param>
        public void UnmuteUser(ulong userId)
        {
            // 从静音集合中移除用户
            _ = m_mutedUsers.Remove(userId);
            // 触发静音状态改变回调
            m_onUserMutedStateCallback?.Invoke(userId, false);
        }
    }
}