using Photon.Realtime;
using Unity.Netcode;

namespace Netcode.Transports.PhotonRealtime
{
    /// <summary>
    /// PhotonRealtimeTransport的房间事件处理分部类
    /// 实现IInRoomCallbacks接口，处理房间内的玩家进入、离开等事件
    /// 负责维护房间内玩家状态并通知网络管理器相关的连接变化
    /// </summary>
    public partial class PhotonRealtimeTransport : IInRoomCallbacks
    {
        /// <summary>
        /// 主客户端切换时的回调
        /// 当房间的主客户端发生变化时调用（例如原主机离开房间）
        /// </summary>
        /// <param name="newMasterClient">新的主客户端</param>
        public void OnMasterClientSwitched(Player newMasterClient)
        {
            // 可以在此处处理主机迁移逻辑
        }

        /// <summary>
        /// 远程玩家进入房间时的回调
        /// 当有新玩家加入房间时调用，该玩家已被添加到玩家列表中
        /// </summary>
        /// <param name="newPlayer">新加入的玩家</param>
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            // 只有主机/服务器需要处理玩家加入事件（其他客户端不需要此处理）

            if (m_IsHostOrServer)
            {
                // 将Photon玩家ID转换为MLAPI客户端ID
                var senderId = GetMlapiClientId(newPlayer.ActorNumber, false);
                //Debug.Log("Host got OnPlayerEnteredRoom() with senderId: "+senderId);

                // 通知网络管理器有新客户端连接
                NetworkEvent netEvent = NetworkEvent.Connect;
                InvokeTransportEvent(netEvent, senderId);
            }
        }

        /// <summary>
        /// 远程玩家离开房间或变为非活动状态时的回调
        /// 可以通过otherPlayer.IsInactive检查玩家是否为非活动状态
        /// </summary>
        /// <param name="otherPlayer">离开的玩家</param>
        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            // 主机/服务器接收任何玩家的离开事件
            // 当主机/服务器离开时，所有客户端都会断开连接

            if (m_IsHostOrServer)
            {
                // 主机/服务器处理客户端离开
                var senderId = GetMlapiClientId(otherPlayer.ActorNumber, false);
                //Debug.Log("Host got OnPlayerLeftRoom() with senderId: "+senderId);

                // 通知网络管理器客户端已断开连接
                NetworkEvent netEvent = NetworkEvent.Disconnect;
                InvokeTransportEvent(netEvent, senderId);
            }
            else if (otherPlayer.ActorNumber == m_originalRoomMasterClient)
            {
                // 客户端检测到原始主机离开房间
                // 通知网络管理器与主机的连接已断开
                NetworkEvent netEvent = NetworkEvent.Disconnect;
                InvokeTransportEvent(netEvent, GetMlapiClientId(m_originalRoomMasterClient, false));
            }
        }

        /// <summary>
        /// 玩家属性更新时的回调
        /// 当房间内任何玩家的自定义属性发生变化时调用
        /// </summary>
        /// <param name="targetPlayer">属性发生变化的玩家</param>
        /// <param name="changedProps">发生变化的属性</param>
        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // 可以在此处处理玩家属性变化逻辑
        }

        /// <summary>
        /// 房间属性更新时的回调
        /// 当房间的自定义属性发生变化时调用
        /// </summary>
        /// <param name="propertiesThatChanged">发生变化的房间属性</param>
        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // 可以在此处处理房间属性变化逻辑
        }
    }
}