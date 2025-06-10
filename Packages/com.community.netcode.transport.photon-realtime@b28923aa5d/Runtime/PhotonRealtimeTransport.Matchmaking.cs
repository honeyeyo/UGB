using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Unity.Netcode;

namespace Netcode.Transports.PhotonRealtime
{
    /// <summary>
    /// PhotonRealtimeTransport的匹配处理分部类
    /// 实现IMatchmakingCallbacks接口，处理房间创建、加入、离开等匹配相关事件
    /// 负责管理房间状态和主客户端切换逻辑
    /// </summary>
    public partial class PhotonRealtimeTransport : IMatchmakingCallbacks
    {
        /// <summary>
        /// 获取当前房间的主客户端ID
        /// 如果客户端在房间内则返回主客户端ID，否则返回-1
        /// </summary>
        /// <returns>主客户端ID，如果不在房间内则返回-1</returns>
		private int CurrentMasterId => this.m_Client != null && this.m_Client.CurrentRoom != null ? this.m_Client.CurrentRoom.MasterClientId : -1;

        /// <summary>
        /// 原始房间主客户端的Photon ActorNumber
        /// 用于跟踪最初的主机，在主机迁移时保持网络状态一致性
        /// </summary>
        private int m_originalRoomMasterClient = -1;

        /// <summary>
        /// 客户端重试次数
        /// 用于在连接失败时进行有限次数的重试
        /// </summary>
        public int RetriesClient { get; set; }= 0;

        /// <summary>
        /// 房间创建成功时的回调
        /// 当成功创建房间时调用
        /// </summary>
        public void OnCreatedRoom()
        {
            // 房间创建成功，等待其他玩家加入
        }

        /// <summary>
        /// 房间创建失败时的回调
        /// 当房间创建失败时调用，并触发断连事件
        /// </summary>
        /// <param name="returnCode">错误返回代码</param>
        /// <param name="message">错误信息</param>
        public void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Create Room Failed: {message}");
            InvokeTransportEvent(NetworkEvent.Disconnect);
        }

        /// <summary>
        /// 好友列表更新时的回调
        /// 当好友列表发生变化时调用
        /// </summary>
        /// <param name="friendList">更新后的好友列表</param>
        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            // 可以在此处处理好友列表更新逻辑
        }

        /// <summary>
        /// 成功加入房间时的回调
        /// 处理房间加入成功后的初始化逻辑
        /// </summary>
        public void OnJoinedRoom()
        {
            RetriesClient = 0; // 重置重试计数
            Debug.Log($"OnJoinedRoom {m_IsHostOrServer}");
            Debug.LogFormat("Caching Original Master Client: {0}", CurrentMasterId);

            // 缓存原始主客户端ID
            m_originalRoomMasterClient = CurrentMasterId;

            // 除主机/服务器外的任何客户端都需要知道自己的加入事件
            if (!m_IsHostOrServer)
            {
                NetworkEvent netEvent = NetworkEvent.Connect;
                InvokeTransportEvent(netEvent, GetMlapiClientId(m_originalRoomMasterClient, false));
            }

            // 更新房间名称和私人房间状态
            m_RoomName = Client.CurrentRoom.Name;
            // 在主机迁移情况下更新私人房间状态
            m_UsePrivateRoom = !Client.CurrentRoom.IsVisible;
        }

        /// <summary>
        /// 加入随机房间失败时的回调
        /// 当尝试加入随机房间失败时调用
        /// </summary>
        /// <param name="returnCode">错误返回代码</param>
        /// <param name="message">错误信息</param>
        public void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Join Room Failed: {message}");
            InvokeTransportEvent(NetworkEvent.Disconnect);
        }

        /// <summary>
        /// 加入房间失败时的回调
        /// 当尝试加入指定房间失败时调用，支持重试机制
        /// </summary>
        /// <param name="returnCode">错误返回代码</param>
        /// <param name="message">错误信息</param>
        public void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Join Room Failed: {message}");

            // 如果是客户端模式且还有重试次数，则进行重试
            if (RetriesClient > 0 && connectionIntent == ConnectionIntent.Client)
            {
                RetriesClient--;
                Debug.LogWarning($"Retry Client - remaining({RetriesClient})");

                // 延迟重试连接的协程
                IEnumerator RetryClientConnection()
                {
                    yield return new WaitForSeconds(1f);
                    HandleConnectionIntent();
                }
                StartCoroutine(RetryClientConnection());
            }
            else
            {
                // 重试次数用完或其他情况，触发断连事件
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }
        }

        /// <summary>
        /// 离开房间时的回调
        /// 当本地客户端离开房间时调用
        /// </summary>
        public void OnLeftRoom()
        {
            // 除主机/服务器外的任何客户端都需要知道自己的离开事件
            if (!this.m_IsHostOrServer)
            {
                NetworkEvent netEvent = NetworkEvent.Connect;
                InvokeTransportEvent(netEvent, GetMlapiClientId(m_originalRoomMasterClient, false));
            }
        }
    }
}