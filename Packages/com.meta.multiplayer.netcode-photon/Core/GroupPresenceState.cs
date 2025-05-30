// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using System.Threading.Tasks;
using Oculus.Platform;
using Meta.Utilities;
#endif

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 群组存在状态管理器
    /// 跟踪当前群组存在状态并使用Oculus平台的GroupPresence API
    /// 用途：设置玩家的群组存在状态，支持社交功能如邀请和加入
    /// https://developer.oculus.com/documentation/unity/ps-group-presence-overview/
    /// </summary>
    public class GroupPresenceState
    {
        /// <summary>
        /// 目标位置
        /// 当前群组存在的目标API名称，如"MainMenu"或"Arena"
        /// </summary>
        public string Destination { get; private set; }

        /// <summary>
        /// 大厅会话ID
        /// 用于标识特定的游戏大厅或房间
        /// </summary>
        public string LobbySessionID { get; private set; }

        /// <summary>
        /// 比赛会话ID
        /// 用于标识特定的比赛或游戏实例
        /// </summary>
        public string MatchSessionID { get; private set; }

        /// <summary>
        /// 是否可加入
        /// 指示其他玩家是否可以加入当前会话
        /// </summary>
        public bool IsJoinable { get; private set; }

        /// <summary>
        /// 设置群组存在状态
        /// 使用Oculus平台API更新玩家的群组存在信息
        /// </summary>
        /// <param name="dest">目标位置API名称</param>
        /// <param name="lobbyID">大厅会话ID</param>
        /// <param name="matchID">比赛会话ID</param>
        /// <param name="joinable">是否可加入</param>
        /// <returns>协程枚举器</returns>
        public IEnumerator Set(string dest, string lobbyID, string matchID, bool joinable)
        {
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            // 在实际平台上使用Oculus平台API
            return Impl().ToRoutine();

            /// <summary>
            /// 内部异步实现方法
            /// 处理与Oculus平台的API交互
            /// </summary>
            async Task Impl()
            {
                // 创建群组存在选项
                var groupPresenceOptions = new GroupPresenceOptions();

                // 设置目标API名称
                if (dest is not null)
                    groupPresenceOptions.SetDestinationApiName(dest);

                // 设置大厅会话ID
                if (lobbyID is not null)
                    groupPresenceOptions.SetLobbySessionId(lobbyID);

                // 设置比赛会话ID
                if (matchID is not null)
                    groupPresenceOptions.SetMatchSessionId(matchID);

                // 设置是否可加入
                groupPresenceOptions.SetIsJoinable(joinable);

                // 临时解决方案，直到bug修复
                // GroupPresence.Set() 有时可能失败，等待完成，如果失败则重试
                while (true)
                {
                    Debug.Log("Setting Group Presence...");

                    // 根据是否有大厅ID决定是清除还是设置群组存在
                    var request = lobbyID is null ?
                        GroupPresence.Clear() :
                        GroupPresence.Set(groupPresenceOptions);
                    var message = await request.Gen();

                    if (message.IsError)
                    {
                        LogError("Failed to setup Group Presence", message.GetError());
                        continue;
                    }

                    OnSetComplete();
                    break;
                }
            }
#else
            OnSetComplete();
            yield break;
#endif

            void OnSetComplete()
            {
                Destination = dest;
                LobbySessionID = lobbyID;
                MatchSessionID = matchID;
                IsJoinable = joinable;

                Debug.Log("Group Presence set successfully");
                Print();
            }
        }

        public void Print()
        {
            Debug.Log(@$"------GROUP PRESENCE STATE------
Destination:      {Destination}
Lobby Session ID: {LobbySessionID}
Match Session ID: {MatchSessionID}
Joinable?:        {IsJoinable}
--------------------------------");
        }

        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError(@"{message}
ERROR MESSAGE:   {error.Message}
ERROR CODE:      {error.Code}
ERROR HTTP CODE: {error.HttpCode}");
        }
    }
}