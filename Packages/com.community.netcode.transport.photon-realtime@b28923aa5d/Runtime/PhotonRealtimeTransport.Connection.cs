using System.Collections.Generic;
using Photon.Realtime;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Netcode.Transports.PhotonRealtime
{
    /// <summary>
    /// PhotonRealtimeTransport的连接处理分部类
    /// 实现IConnectionCallbacks接口，处理与Photon服务器的连接状态变化
    /// 包括连接建立、断开连接、房间创建和加入等功能
    /// </summary>
    public partial class PhotonRealtimeTransport : IConnectionCallbacks
    {
        /// <summary>
        /// 获取主机房间选项的委托函数
        /// 允许外部自定义房间创建参数
        /// </summary>
        public Func<bool, byte, RoomOptions> GetHostRoomOptionsFunc;

        /// <summary>
        /// 获取随机房间参数的委托函数
        /// 允许外部自定义随机房间加入参数
        /// </summary>
        public Func<byte, OpJoinRandomRoomParams> GetRandomRoomParamsFunc;

        /// <summary>
        /// 连接到Photon服务器时的回调
        /// 此时尚未连接到主服务器
        /// </summary>
        public void OnConnected()
        {
            // 连接建立，但还需要等待连接到主服务器
        }

        /// <summary>
        /// 连接到主服务器时的回调
        /// 此时可以执行房间相关操作
        /// </summary>
        public void OnConnectedToMaster()
        {
            HandleConnectionIntent();
        }

        /// <summary>
        /// 处理连接意图
        /// 根据当前的连接目的执行相应的操作
        /// </summary>
        /// <returns>操作是否成功</returns>
        private bool HandleConnectionIntent()
        {
            // 如果当前在大厅中，先离开大厅
            if (Client.InLobby)
            {
                Client.OpLeaveLobby();
            }

            // 如果当前在房间中，先离开房间
            if (Client.InRoom)
            {
                Client.OpLeaveRoom(false);
            }

            // 根据连接意图执行相应操作
            switch (connectionIntent)
            {
                case ConnectionIntent.Lobby:
                    return ConnectToLobby();

                case ConnectionIntent.HostOrServer:
                case ConnectionIntent.Client:
                    return ConnectToRoom();

                default:
                case ConnectionIntent.None:
                    // 无特定操作需要执行
                    break;
            }

            return false;
        }

        /// <summary>
        /// 连接到Photon大厅
        /// 用于浏览和发现可用的房间
        /// </summary>
        /// <returns>连接是否成功</returns>
        private bool ConnectToLobby()
        {
            var success = m_Client.OpJoinLobby(null);

            if (!success)
            {
                Debug.LogWarning("Unable to connect to Photon Lobby.");
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }

            return success;
        }

        /// <summary>
        /// 连接到房间
        /// 根据是否为主机/服务器执行创建房间或加入房间操作
        /// </summary>
        /// <returns>操作是否成功</returns>
        private bool ConnectToRoom()
        {
            var randomRoom = string.IsNullOrEmpty(m_RoomName);

            // 连接到主服务器后立即重定向到房间
            bool success = false;
            if (m_IsHostOrServer)
            {
                // 主机/服务器模式：创建房间
                RoomOptions roomOptions;
                if (GetHostRoomOptionsFunc != null)
                {
                    // 使用自定义房间选项函数
                    roomOptions = GetHostRoomOptionsFunc(m_UsePrivateRoom, m_MaxPlayers);
                }
                else
                {
                    // 使用默认房间选项
                    roomOptions = new RoomOptions();
                    roomOptions.MaxPlayers = m_MaxPlayers;
                }

                var enterRoomParams = new EnterRoomParams()
                {
                    RoomName = m_RoomName,
                    RoomOptions = roomOptions,
                };
                success = m_Client.OpCreateRoom(enterRoomParams);
            }
            else if (randomRoom)
            {
                // 客户端模式：加入随机房间
                OpJoinRandomRoomParams opJoinRandomRoomParams;
                if (GetRandomRoomParamsFunc != null)
                {
                    // 使用自定义随机房间参数函数
                    opJoinRandomRoomParams = GetRandomRoomParamsFunc(m_MaxPlayers);
                }
                else
                {
                    // 使用默认随机房间参数
                    opJoinRandomRoomParams = new OpJoinRandomRoomParams();
                    opJoinRandomRoomParams.ExpectedMaxPlayers = m_MaxPlayers;
                }
                success = m_Client.OpJoinRandomRoom(opJoinRandomRoomParams);
            }
            else
            {
                // 客户端模式：通过名称加入房间
                var enterRoomParams = new EnterRoomParams()
                {
                    RoomName = m_RoomName,
                    RoomOptions = new RoomOptions()
                    {
                        MaxPlayers = m_MaxPlayers,
                    },
                };
                success = m_Client.OpJoinRoom(enterRoomParams);
            }

            if (!success)
            {
                Debug.LogWarning("Unable to create or join room.");
                InvokeTransportEvent(NetworkEvent.Disconnect);
            }
            return success;
        }

        /// <summary>
        /// 自定义身份验证失败时的回调
        /// 当使用自定义身份验证且验证失败时调用
        /// </summary>
        /// <param name="debugMessage">调试信息</param>
        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            // 可以在此处处理身份验证失败的逻辑
        }

        /// <summary>
        /// 自定义身份验证响应回调
        /// 当收到自定义身份验证响应时调用
        /// </summary>
        /// <param name="data">验证响应数据</param>
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            // 可以在此处处理身份验证响应数据
        }

        /// <summary>
        /// 连接断开时的回调
        /// 处理各种原因导致的连接断开
        /// </summary>
        /// <param name="cause">断开连接的原因</param>
        public void OnDisconnected(DisconnectCause cause)
        {
            // 通知网络管理器连接已断开
            InvokeTransportEvent(NetworkEvent.Disconnect);
            // 清理资源
            this.DeInitialize();
        }

        /// <summary>
        /// 接收到区域列表时的回调
        /// 当从Photon服务器接收到可用区域列表时调用
        /// </summary>
        /// <param name="regionHandler">区域处理器</param>
        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            // 可以在此处处理区域选择逻辑
        }
    }
}
