using ExitGames.Client.Photon;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netcode.Transports.PhotonRealtime
{
    /// <summary>
    /// Photon Realtime网络传输实现
    /// 继承自Unity Netcode的NetworkTransport，提供基于Photon Cloud的网络传输层
    /// 支持房间创建、连接、数据传输和事件处理，实现多人游戏的网络通信
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public partial class PhotonRealtimeTransport : NetworkTransport, IOnEventCallback
    {
        /// <summary>
        /// 连接意图枚举
        /// 定义不同的连接目的和行为模式
        /// </summary>
        public enum ConnectionIntent
        {
            /// <summary>无连接意图</summary>
            None,
            /// <summary>连接到大厅</summary>
            Lobby,
            /// <summary>作为主机或服务器</summary>
            HostOrServer,
            /// <summary>作为客户端</summary>
            Client,
        }

        /// <summary>
        /// 空的数组段缓存，避免重复分配
        /// </summary>
        private static readonly ArraySegment<byte> s_EmptyArraySegment = new ArraySegment<byte>(Array.Empty<byte>());

        /// <summary>
        /// 玩家昵称
        /// 在Photon房间中显示的玩家名称，为空时会生成随机名称
        /// </summary>
        [Tooltip("The nickname of the player in the Photon Room. This value is only relevant for other Photon Realtime features. Leaving it empty generates a random name.")]
        [SerializeField]
        private string m_NickName;

        [Header("Server Settings")]
        /// <summary>
        /// 房间名称
        /// 用于创建或加入的房间的唯一标识符
        /// </summary>
        [Tooltip("Unique name of the room for this session.")]
        [SerializeField]
        private string m_RoomName;

        /// <summary>
        /// 房间最大玩家数量
        /// 限制房间中允许的最大玩家数
        /// </summary>
        [Tooltip("The maximum amount of players allowed in the room.")]
        [SerializeField]
        private byte m_MaxPlayers = 16;

        [FormerlySerializedAs("m_ChannelIdCodesStartRange")]
        [Header("Advanced Settings")]
        /// <summary>
        /// 网络传输事件代码起始范围
        /// 为非批处理消息保留的Photon事件代码范围的第一个字节
        /// 应设置为小于200的数字以免与Photon内部事件冲突
        /// </summary>
        [Tooltip("The first byte of the range of photon event codes which this transport will reserve for unbatched messages. Should be set to a number lower then 200 to not interfere with photon internal events. Approximately 8 events will be reserved.")]
        [SerializeField]
        private byte m_NetworkDeliveryEventCodesStartRange = 0;

        /// <summary>
        /// 是否附加Photon支持日志记录器
        /// 用于调试连接断开或其他问题
        /// </summary>
        [Tooltip("Attaches the photon support logger to the transport. Useful for debugging disconnects or other issues.")]
        [SerializeField]
        private bool m_AttachSupportLogger = false;

        /// <summary>
        /// 批处理模式
        /// 传输层应用于MLAPI事件的批处理方式
        /// </summary>
        [Tooltip("The batching this transport should apply to MLAPI events. None only works for very simple scenes.")]
        [SerializeField]
        private BatchMode m_BatchMode = BatchMode.SendAllReliable;

        /// <summary>
        /// 发送队列批处理大小
        /// 将MLAPI事件批处理为Photon事件的发送队列最大大小
        /// </summary>
        [Tooltip("The maximum size of the send queue which batches MLAPI events into Photon events.")]
        [SerializeField]
        private int m_SendQueueBatchSize = 4096;

        /// <summary>
        /// 批处理传输事件代码
        /// 用于发送批处理数据的Photon事件代码
        /// </summary>
        [Tooltip("The Photon event code which will be used to send batched data.")]
        [SerializeField]
        [Range(129, 199)]
        private byte m_BatchedTransportEventCode = 129;

        /// <summary>
        /// 踢出事件代码
        /// 用于发送踢出操作的Photon事件代码
        /// </summary>
        [Tooltip("The Photon event code which will be used to send a kick.")]
        [SerializeField]
        [Range(129, 199)]
        private byte m_KickEventCode = 130;

        /// <summary>
        /// 区域覆盖设置
        /// 要连接的区域，为空时使用应用设置
        /// </summary>
        [Tooltip("The Region to connect to, empty will take the App settings")]
        [SerializeField]
        private string m_RegionOverride = null;

        /// <summary>
        /// 是否使用私人房间
        /// 决定连接到私人房间还是公共房间
        /// </summary>
        [Tooltip("If we want to connect to a private room or public")]
        [SerializeField]
        private bool m_UsePrivateRoom = false;

        /// <summary>
        /// Photon负载均衡客户端
        /// 处理所有与Photon Cloud的网络通信
        /// </summary>
        private LoadBalancingClient m_Client;

        /// <summary>
        /// 是否为主机或服务器
        /// 标识当前实例的角色
        /// </summary>
        private bool m_IsHostOrServer;

        /// <summary>
        /// 当前连接意图
        /// 记录当前的连接目的
        /// </summary>
        private ConnectionIntent connectionIntent = ConnectionIntent.None;

        /// <summary>
        /// 发送队列字典
        /// 用于批处理事件而不是立即发送
        /// 键为发送目标，值为对应的发送队列
        /// </summary>
        private readonly Dictionary<SendTarget, SendQueue> m_SendQueue = new Dictionary<SendTarget, SendQueue>();

        /// <summary>
        /// 缓存的提升事件选项
        /// 在调用RaisePhotonEvent时使用，避免每次发送时分配内存
        /// </summary>
        private RaiseEventOptions m_CachedRaiseEventOptions = new RaiseEventOptions() { TargetActors = new int[1] };

        /// <summary>
        /// 单帧内发送数据包的最大数量限制
        /// 用于控制网络流量，避免单帧发送过多数据
        /// </summary>
        private const int MAX_DGRAM_PER_FRAME = 4;

        /// <summary>
        /// 获取或设置要创建或加入的房间名称
        /// </summary>
        public string RoomName
        {
            get => m_RoomName;
            set => m_RoomName = value;
        }

        /// <summary>
        /// 获取或设置区域覆盖
        /// </summary>
        public string RegionOverride
        {
            get => m_RegionOverride;
            set => m_RegionOverride = value;
        }

        /// <summary>
        /// 获取或设置是否使用私人房间
        /// </summary>
        public bool UsePrivateRoom
        {
            get => m_UsePrivateRoom;
            set => m_UsePrivateRoom = value;
        }

        /// <summary>
        /// 此传输用于所有网络相关操作的Photon负载均衡客户端
        /// </summary>
        public LoadBalancingClient Client => m_Client;

        /// <summary>
        /// 服务器客户端ID
        /// 返回服务器的MLAPI客户端ID
        /// </summary>
        public override ulong ServerClientId => GetMlapiClientId(0, true);

        // -------------- MonoBehaviour Handlers --------------------------------------------------------------------------

        /// <summary>
        /// Update方法在其他脚本运行之前调度传入的命令
        /// 确保网络消息能够及时处理
        /// </summary>
        void Update()
        {
            if (m_Client != null)
            {
                // 循环处理所有传入的命令直到队列为空
                do { } while (m_Client.LoadBalancingPeer.DispatchIncomingCommands());
            }
        }

        /// <summary>
        /// LateUpdate中发送批处理消息
        /// 在帧的最后阶段发送所有排队的数据
        /// </summary>
        void LateUpdate()
        {
            if (m_Client != null)
            {
                // 刷新所有发送队列
                FlushAllSendQueues();

                // 限制每帧发送的数据包数量
                for (int i = 0; i < MAX_DGRAM_PER_FRAME; i++)
                {
                    bool anythingLeftToSend = m_Client.LoadBalancingPeer.SendOutgoingCommands();
                    if (!anythingLeftToSend)
                    {
                        break;
                    }
                }
            }
        }

        // -------------- Transport Utils -----------------------------------------------------------------------------

        /// <summary>
        /// 创建并初始化用于与Photon Cloud中继数据的内部LoadBalancingClient
        /// </summary>
        private void InitializeClient()
        {
            if (m_Client == null)
            {
                // 从Photon Realtime示例中获取随机用户名的逻辑
                var nickName = string.IsNullOrEmpty(m_NickName) ? "usr" + SupportClass.ThreadSafeRandom.Next() % 99 : m_NickName;

                m_Client = new LoadBalancingClient
                {
                    LocalPlayer = { NickName = nickName },
                };

                // 注册回调
                m_Client.AddCallbackTarget(this);

                // 这两个设置启用字节数组内容的（几乎）零分配发送和接收
                m_Client.LoadBalancingPeer.ReuseEventInstance = true;
                m_Client.LoadBalancingPeer.UseByteArraySlicePoolForEvents = true;

                // 附加日志记录器
                if (m_AttachSupportLogger)
                {
                    var logger = gameObject.GetComponent<SupportLogger>() ?? gameObject.AddComponent<SupportLogger>();
                    logger.Client = m_Client;
                }
            }
        }

        /// <summary>
        /// 同步创建并连接对等端到区域主服务器，返回包含结果的布尔值
        /// </summary>
        /// <returns>连接是否成功</returns>
        private bool ConnectPeer()
        {
            InitializeClient();
            if (!Client.IsConnected)
            {
                var appSettings = PhotonAppSettings.Instance.AppSettings;
                if (!string.IsNullOrEmpty(m_RegionOverride))
                {
                    appSettings = new AppSettings();
                    appSettings = PhotonAppSettings.Instance.AppSettings.CopyTo(appSettings);
                    appSettings.FixedRegion = m_RegionOverride;
                }
                return m_Client.ConnectUsingSettings(appSettings);
            }
            else
            {
                return HandleConnectionIntent();
            }
        }

        /// <summary>
        /// 发送数据到指定客户端
        /// 根据批处理模式决定是立即发送还是加入发送队列
        /// </summary>
        /// <param name="clientId">目标客户端ID</param>
        /// <param name="data">要发送的数据</param>
        /// <param name="networkDelivery">网络传输方式</param>
        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkDelivery networkDelivery)
        {
            var isReliable = DeliveryModeToReliable(networkDelivery);

            if (m_BatchMode == BatchMode.None)
            {
                RaisePhotonEvent(clientId, isReliable, data, (byte)(m_NetworkDeliveryEventCodesStartRange + networkDelivery));
                return;
            }

            SendQueue queue;
            SendTarget sendTarget = new SendTarget(clientId, isReliable);

            if (m_BatchMode == BatchMode.SendAllReliable)
            {
                sendTarget.IsReliable = true;
            }

            if (!m_SendQueue.TryGetValue(sendTarget, out queue))
            {
                queue = new SendQueue(m_SendQueueBatchSize);
                m_SendQueue.Add(sendTarget, queue);
            }

            if (!queue.AddEvent(data))
            {
                // If we are in here data exceeded remaining queue size. This should not happen under normal operation.
                if (data.Count > queue.Size)
                {
                    // If data is too large to be batched, flush it out immediately. This happens with large initial spawn packets from MLAPI.
                    Debug.LogWarning($"Sent {data.Count} bytes on NetworkDelivery: {networkDelivery}. Event size exceeds sendQueueBatchSize: ({m_SendQueueBatchSize}).");
                    RaisePhotonEvent(sendTarget.ClientId, sendTarget.IsReliable, data, (byte)(m_NetworkDeliveryEventCodesStartRange + networkDelivery));
                }
                else
                {
                    var sendBuffer = queue.GetData();
                    RaisePhotonEvent(sendTarget.ClientId, sendTarget.IsReliable, sendBuffer, m_BatchedTransportEventCode);
                    queue.Clear();
                    queue.AddEvent(data);
                }
            }
        }

        /// <summary>
        /// 刷新所有发送队列
        /// 将队列中的数据打包并发送到Photon网络
        /// </summary>
        private void FlushAllSendQueues()
        {
            foreach (var kvp in m_SendQueue)
            {
                if (kvp.Value.IsEmpty()) continue;

                var sendBuffer = kvp.Value.GetData();
                RaisePhotonEvent(kvp.Key.ClientId, kvp.Key.IsReliable, sendBuffer, m_BatchedTransportEventCode);
                kvp.Value.Clear();
            }
        }

        /// <summary>
        /// 发送Photon事件
        /// 封装了向Photon网络发送事件的底层操作
        /// </summary>
        /// <param name="clientId">目标客户端ID</param>
        /// <param name="isReliable">是否可靠传输</param>
        /// <param name="data">事件数据</param>
        /// <param name="eventCode">事件代码</param>
        private void RaisePhotonEvent(ulong clientId, bool isReliable, ArraySegment<byte> data, byte eventCode)
        {
            if (m_Client == null || !m_Client.InRoom)
            {
                // the local client is set to null or it's not in a room. can't send events, so it makes sense to disconnect MLAPI layer.
                this.InvokeTransportEvent(NetworkEvent.Disconnect);
                return;
            }

            m_CachedRaiseEventOptions.TargetActors[0] = GetPhotonRealtimeId(clientId);
            var sendOptions = isReliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;

            // This allocates because data gets boxed to object.
            m_Client.OpRaiseEvent(eventCode, data, m_CachedRaiseEventOptions, sendOptions);
        }

        // -------------- Transport Handlers --------------------------------------------------------------------------

        /// <summary>
        /// 初始化传输层
        /// NetworkTransport接口的实现，用于设置传输层
        /// </summary>
        /// <param name="networkManager">网络管理器实例</param>
        public override void Initialize(NetworkManager networkManager = null) { }

        /// <summary>
        /// 关闭传输层
        /// 清理资源并断开所有连接
        /// </summary>
        public override void Shutdown()
        {
            if (m_Client != null && m_Client.IsConnected)
            {
                m_Client.Disconnect();
            }
            else
            {
                this.DeInitialize();
            }
        }

        /// <summary>
        /// 启动大厅模式
        /// 连接到Photon大厅以浏览和加入房间
        /// </summary>
        /// <returns>启动是否成功</returns>
        public bool StartLobby()
        {
            connectionIntent = ConnectionIntent.Lobby;
            bool connected = ConnectPeer();
            return connected;
        }

        /// <summary>
        /// 启动客户端模式
        /// 作为客户端连接到现有房间
        /// </summary>
        /// <returns>启动是否成功</returns>
        public override bool StartClient()
        {
            connectionIntent = ConnectionIntent.Client;
            bool connected = ConnectPeer();
            return connected;
        }

        /// <summary>
        /// 启动服务器模式
        /// 作为主机创建房间并等待客户端连接
        /// </summary>
        /// <returns>启动是否成功</returns>
        public override bool StartServer()
        {
            m_IsHostOrServer = true;
            connectionIntent = ConnectionIntent.HostOrServer;
            var result = ConnectPeer();
            if (result == false)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 轮询网络事件
        /// 检查并返回待处理的网络事件
        /// </summary>
        /// <param name="clientId">输出客户端ID</param>
        /// <param name="payload">输出事件数据</param>
        /// <param name="receiveTime">输出接收时间</param>
        /// <returns>网络事件类型</returns>
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            receiveTime = Time.realtimeSinceStartup;
            payload = default;
            return NetworkEvent.Nothing;
        }

        /// <summary>
        /// 获取当前往返时间
        /// 返回与指定客户端的网络延迟
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <returns>往返时间（毫秒）</returns>
        public override ulong GetCurrentRtt(ulong clientId)
        {
            // This is only an approximate value based on the own client's rtt to the server and could cause issues, maybe use a similar approach as the Steamworks transport.
            return (ulong)(m_Client.LoadBalancingPeer.RoundTripTime * 2);
        }

        /// <summary>
        /// 断开远程客户端连接
        /// 主机/服务器用于踢出指定客户端
        /// </summary>
        /// <param name="clientId">要断开的客户端ID</param>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (this.m_Client != null && m_Client.InRoom && this.m_Client.LocalPlayer.IsMasterClient)
            {
                ArraySegment<byte> payload = s_EmptyArraySegment;
                RaisePhotonEvent(clientId, true, payload, this.m_KickEventCode);
            }
        }

        /// <summary>
        /// 断开本地客户端连接
        /// 客户端主动断开与服务器的连接
        /// </summary>
        public override void DisconnectLocalClient()
        {
            this.Shutdown();
        }

        // -------------- Event Handlers ------------------------------------------------------------------------------

        /// <summary>
        /// Photon事件回调接口实现
        /// 处理从Photon网络接收到的事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnEvent(EventData eventData)
        {
            if (eventData.Code >= 200) { return; } // EventCode is a photon event.

            var senderId = GetMlapiClientId(eventData.Sender, false);

            // handle kick
            if (eventData.Code == this.m_KickEventCode)
            {
                if (this.m_Client.InRoom && eventData.Sender == m_originalRoomMasterClient)
                {
                    InvokeTransportEvent(NetworkEvent.Disconnect, senderId);
                }

                return;
            }

            // handle data
            using (ByteArraySlice slice = eventData.CustomData as ByteArraySlice)
            {
                if (slice == null)
                {
                    Debug.LogError("Photon option UseByteArraySlicePoolForEvents should be set to true.");
                    return;
                }

                if (eventData.Code == this.m_BatchedTransportEventCode)
                {
                    var segment = new ArraySegment<byte>(slice.Buffer, slice.Offset, slice.Count);
                    using var reader = new FastBufferReader(segment, Allocator.Temp);
                    while (reader.Position < segment.Count) // TODO Not using reader.Lenght here becaues it's broken: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1310
                    {
                        reader.ReadValueSafe(out int length);
                        byte[] dataArray = new byte[length];
                        reader.ReadBytesSafe(ref dataArray, length);

                        InvokeTransportEvent(NetworkEvent.Data, senderId, new ArraySegment<byte>(dataArray, 0, dataArray.Length));
                    }
                }
                else
                {
                    // Event is a non-batched data event.
                    ArraySegment<byte> payload = new ArraySegment<byte>(slice.Buffer, slice.Offset, slice.Count);

                    InvokeTransportEvent(NetworkEvent.Data, senderId, payload);
                }
            }
        }

        // -------------- Utility Methods -----------------------------------------------------------------------------

        /// <summary>
        /// 调用传输事件
        /// 向网络管理器通知传输层事件
        /// </summary>
        /// <param name="networkEvent">网络事件类型</param>
        /// <param name="senderId">发送者ID</param>
        /// <param name="payload">事件数据</param>
        private void InvokeTransportEvent(NetworkEvent networkEvent, ulong senderId = 0, ArraySegment<byte> payload = default)
        {
            switch (networkEvent)
            {
                case NetworkEvent.Nothing:
                    // do nothing
                    break;
                case NetworkEvent.Disconnect:
                    if (m_IsHostOrServer && ServerClientId == senderId)
                    {
                        ForceStopPeer();
                    }

                    goto default;
                default:
                    InvokeOnTransportEvent(networkEvent, senderId, payload, Time.realtimeSinceStartup);
                    break;
            }
        }

        /// <summary>
        /// 将传输模式转换为可靠性标志
        /// 根据NetworkDelivery枚举确定传输是否可靠
        /// </summary>
        /// <param name="deliveryMode">传输模式</param>
        /// <returns>是否可靠传输</returns>
        private bool DeliveryModeToReliable(NetworkDelivery deliveryMode)
        {
            switch (deliveryMode)
            {
                case NetworkDelivery.Unreliable:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 根据Photon ID获取MLAPI客户端ID
        /// 在Photon ActorNumber和Unity Netcode客户端ID之间转换
        /// </summary>
        /// <param name="photonId">Photon ActorNumber</param>
        /// <param name="isServer">是否为服务器</param>
        /// <returns>对应的MLAPI客户端ID</returns>
        private ulong GetMlapiClientId(int photonId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return (ulong)(photonId + 1);
            }
        }

        /// <summary>
        /// 根据MLAPI客户端ID获取Photon Realtime ID
        /// 在Unity Netcode客户端ID和Photon ActorNumber之间转换
        /// </summary>
        /// <param name="clientId">MLAPI客户端ID</param>
        /// <returns>对应的Photon ActorNumber</returns>
        private int GetPhotonRealtimeId(ulong clientId)
        {
            if (clientId == 0)
            {
                return CurrentMasterId;
            }
            else
            {
                return (int)(clientId - 1);
            }
        }

        /// <summary>
        /// 反初始化传输层
        /// 清理客户端资源和回调
        /// </summary>
        private void DeInitialize()
        {
            m_originalRoomMasterClient = -1;
            m_IsHostOrServer = false;
            m_Client?.RemoveCallbackTarget(this);
            m_Client = null;
        }

        /// <summary>
        /// 强制停止对等端
        /// 立即断开网络连接
        /// </summary>
        private void ForceStopPeer()
        {
            if (NetworkManager.Singleton == null) { return; }

            NetworkManager.Singleton.Shutdown();
        }

        // -------------- Utility Types -------------------------------------------------------------------------------

        /// <summary>
        /// 发送队列类
        /// 用于批处理网络消息以提高传输效率
        /// 实现IDisposable接口以正确管理资源
        /// </summary>
        class SendQueue : IDisposable
        {
            /// <summary>
            /// 快速缓冲区写入器，用于序列化数据
            /// </summary>
            FastBufferWriter m_Writer;

            /// <summary>
            /// 队列的最大大小
            /// </summary>
            public int Size { get; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="size">队列大小</param>
            public SendQueue(int size)
            {
                Size = size;
                m_Writer = new FastBufferWriter(size, Allocator.Persistent);
            }

            /// <summary>
            /// 添加事件到队列
            /// </summary>
            /// <param name="data">事件数据</param>
            /// <returns>是否成功添加</returns>
            internal bool AddEvent(ArraySegment<byte> data)
            {
                if (m_Writer.TryBeginWrite(data.Count + 4) == false)
                {
                    return false;
                }

                m_Writer.WriteValue(data.Count);
                m_Writer.WriteBytes(data.Array, data.Count, data.Offset);

                return true;
            }

            /// <summary>
            /// 清空队列
            /// </summary>
            internal void Clear()
            {
                m_Writer.Truncate(0);
            }

            /// <summary>
            /// 检查队列是否为空
            /// </summary>
            /// <returns>队列是否为空</returns>
            internal bool IsEmpty()
            {
                return m_Writer.Position == 0;
            }

            /// <summary>
            /// 获取队列中的数据
            /// </summary>
            /// <returns>序列化后的数据</returns>
            internal ArraySegment<byte> GetData()
            {
                var array = m_Writer.ToArray();
                return new ArraySegment<byte>(array);
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                m_Writer.Dispose();
            }
        }

        /// <summary>
        /// 发送目标结构体
        /// 定义消息的发送目标和传输方式
        /// </summary>
        struct SendTarget
        {
            /// <summary>客户端ID</summary>
            public ulong ClientId;
            /// <summary>是否可靠传输</summary>
            public bool IsReliable;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="clientId">客户端ID</param>
            /// <param name="isReliable">是否可靠传输</param>
            public SendTarget(ulong clientId, bool isReliable)
            {
                ClientId = clientId;
                IsReliable = isReliable;
            }
        }

        /// <summary>
        /// 批处理模式枚举
        /// 定义传输层的批处理策略
        /// </summary>
        enum BatchMode : byte
        {
            /// <summary>
            /// 传输层不执行批处理
            /// </summary>
            None = 0,
            /// <summary>
            /// 将所有MLAPI事件批处理为可靠顺序消息
            /// </summary>
            SendAllReliable = 1,
            /// <summary>
            /// 将所有可靠MLAPI事件批处理为单个photon事件，
            /// 将所有不可靠MLAPI事件批处理为不可靠photon事件
            /// </summary>
            ReliableAndUnreliable = 2,
        }
    }
}
