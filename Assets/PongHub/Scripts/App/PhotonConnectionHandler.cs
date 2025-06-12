// Copyright (c) MagnusLab Inc. and affiliates.

using ExitGames.Client.Photon;
using Meta.Utilities;
using Netcode.Transports.PhotonRealtime;
using Photon.Realtime;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// Photon连接处理器
    /// 实现Photon连接相关的功能,根据应用程序状态设置正确的房间选项
    /// 暴露房间属性用于玩家槽位和观战者槽位的开放状态
    /// </summary>
    public class PhotonConnectionHandler : MonoBehaviour
    {
        /// <summary>
        /// 观战者槽位开放的属性键
        /// </summary>
        public const string SPECTATOR_SLOT_OPEN = "spec";

        /// <summary>
        /// 玩家槽位开放的属性键
        /// </summary>
        public const string PLAYER_SLOT_OPEN = "ps";

        /// <summary>
        /// 房间是否公开的属性键
        /// </summary>
        private const string OPEN_ROOM = "vis";

        /// <summary>
        /// 当前玩家是否为观战者
        /// </summary>
        private static bool IsSpectator => LocalPlayerState.Instance.IsSpectator;

        /// <summary>
        /// Photon实时传输组件
        /// </summary>
        [SerializeField, AutoSet] private PhotonRealtimeTransport m_photonRealtimeTransport;

        /// <summary>
        /// 初始化时设置Photon传输组件的回调函数
        /// </summary>
        private void Start()
        {
            m_photonRealtimeTransport.GetHostRoomOptionsFunc = GetHostRoomOptions;
            m_photonRealtimeTransport.GetRandomRoomParamsFunc = GetRandomRoomParams;
        }

        /// <summary>
        /// 销毁时清除Photon传输组件的回调函数
        /// </summary>
        private void OnDestroy()
        {
            m_photonRealtimeTransport.GetHostRoomOptionsFunc = null;
            m_photonRealtimeTransport.GetRandomRoomParamsFunc = null;
        }

        /// <summary>
        /// 获取创建房间时的房间选项
        /// </summary>
        /// <param name="usePrivateRoom">是否使用私密房间</param>
        /// <param name="maxPlayers">最大玩家数</param>
        /// <returns>房间选项配置</returns>
        private RoomOptions GetHostRoomOptions(bool usePrivateRoom, byte maxPlayers)
        {
            var roomOptions = new RoomOptions
            {
                // 设置在大厅中可见的自定义属性
                CustomRoomPropertiesForLobby =
                        new[] { PLAYER_SLOT_OPEN, SPECTATOR_SLOT_OPEN, OPEN_ROOM },
                // 设置房间的自定义属性
                CustomRoomProperties = new Hashtable
                    {
                        { PLAYER_SLOT_OPEN, 1 },    // 玩家槽位开放
                        { SPECTATOR_SLOT_OPEN, 1 }, // 观战者槽位开放
                        { OPEN_ROOM, usePrivateRoom ? 0 : 1 } // 房间是否公开
                    },
                MaxPlayers = maxPlayers, // 设置最大玩家数
            };

            return roomOptions;
        }

        /// <summary>
        /// 获取加入随机房间时的参数
        /// </summary>
        /// <param name="maxPlayers">最大玩家数</param>
        /// <returns>加入随机房间的参数配置</returns>
        private OpJoinRandomRoomParams GetRandomRoomParams(byte maxPlayers)
        {
            var opJoinRandomRoomParams = new OpJoinRandomRoomParams();

            // 根据是否为观战者设置不同的房间属性要求
            if (IsSpectator)
            {
                // 观战者需要房间有观战者槽位且房间公开
                var expectedCustomRoomProperties = new Hashtable { { SPECTATOR_SLOT_OPEN, 1 }, { OPEN_ROOM, 1 } };
                opJoinRandomRoomParams.ExpectedMaxPlayers = maxPlayers;
                opJoinRandomRoomParams.ExpectedCustomRoomProperties = expectedCustomRoomProperties;
            }
            else
            {
                // 普通玩家需要房间有玩家槽位且房间公开
                var expectedCustomRoomProperties = new Hashtable { { PLAYER_SLOT_OPEN, 1 }, { OPEN_ROOM, 1 } };
                opJoinRandomRoomParams.ExpectedMaxPlayers = maxPlayers;
                opJoinRandomRoomParams.ExpectedCustomRoomProperties = expectedCustomRoomProperties;
            }

            return opJoinRandomRoomParams;
        }
    }
}