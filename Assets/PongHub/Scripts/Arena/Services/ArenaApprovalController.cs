// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Multiplayer.Core;
using PongHub.App;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 竞技场连接审批控制器
    /// 实现Netcode for GameObjects的连接审批检查。评估玩家是否可以连接到竞技场,
    /// 或是否存在阻止其加入的限制条件,如达到最大玩家数或最大观众数。
    /// 这是对Photon房间总人数限制的补充检查。
    /// 如果玩家被拒绝,我们会向NetworkLayer发送拒绝原因。
    /// </summary>
    public class ArenaApprovalController : MonoBehaviour
    {
        /// <summary>
        /// 连接负载数据类
        /// 包含连接请求的相关信息
        /// </summary>
        [Serializable]
        public class ConnectionPayload
        {
            /// <summary>
            /// 是否为玩家(true)或观众(false)
            /// </summary>
            public bool IsPlayer;
        }

        /// <summary>
        /// 连接状态枚举
        /// </summary>
        public enum ConnectionStatus
        {
            /// <summary>
            /// 未定义状态
            /// </summary>
            Undefined = 0,
            /// <summary>
            /// 连接成功
            /// </summary>
            Success,
            /// <summary>
            /// 玩家位已满
            /// </summary>
            PlayerFull,
            /// <summary>
            /// 观众位已满
            /// </summary>
            SpectatorFull,
        }

        /// <summary>
        /// 连接负载的最大字节数
        /// 用于防止DDOS攻击和大量垃圾数据
        /// </summary>
        private const int MAX_CONNECT_PAYLOAD = 1024;

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        private const int MAX_PLAYER_COUNT = 6;
        /// <summary>
        /// 最大观众数量
        /// </summary>
        private const int MAX_SPECTATOR_COUNT = 4;

        /// <summary>
        /// NetworkManager组件引用
        /// </summary>
        [SerializeField] private NetworkManager m_networkManager;

        /// <summary>
        /// 已连接玩家的客户端ID集合
        /// </summary>
        private readonly HashSet<ulong> m_playersClientIds = new();
        /// <summary>
        /// 已连接观众的客户端ID集合
        /// </summary>
        private readonly HashSet<ulong> m_spectatorClientIds = new();

        /// <summary>
        /// 玩家位是否已满
        /// </summary>
        private bool m_playersSetFull;
        /// <summary>
        /// 观众位是否已满
        /// </summary>
        private bool m_spectatorsSetFull;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            m_networkManager.ConnectionApprovalCallback += ApprovalCheck;
        }

        /// <summary>
        /// 组件销毁时的清理
        /// </summary>
        private void OnDestroy()
        {
            if (m_networkManager != null)
            {
                m_networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            }
        }

        /// <summary>
        /// 连接审批检查
        /// 处理客户端的连接请求,根据连接负载和当前状态决定是否批准连接
        /// </summary>
        /// <param name="request">连接请求信息</param>
        /// <param name="response">连接响应对象</param>
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;
            if (connectionData.Length > MAX_CONNECT_PAYLOAD)
            {
                // 如果负载数据过大,立即拒绝请求,这是垃圾数据
                // 可以防止服务器浪费时间,轻量级防护大缓冲区DDOS
                response.Approved = false;
                return;
            }

            // 当主机连接时,需要批准它
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("HOST CONNECTED APPROVED");
                m_playersClientIds.Clear();
                m_spectatorClientIds.Clear();
                _ = m_playersClientIds.Add(clientId);
                // 主机连接时注册客户端断开回调
                m_networkManager.OnClientDisconnectCallback += OnClientDisconnected;
                response.Approved = true;
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);
            var connectionStatus = CanClientConnect(connectionPayload);

            if (connectionStatus == ConnectionStatus.Success)
            {
                if (connectionPayload.IsPlayer)
                {
                    Debug.Log($"{clientId} - PLAYER CONNECTED APPROVED");
                    _ = m_playersClientIds.Add(clientId);
                    if (m_playersClientIds.Count >= MAX_PLAYER_COUNT)
                    {
                        var props = new ExitGames.Client.Photon.Hashtable()
                        {
                            {PhotonConnectionHandler.PLAYER_SLOT_OPEN, 0}
                        };
                        UGBApplication.Instance.NetworkLayer.SetRoomProperty(props);
                        m_playersSetFull = true;
                    }
                }
                else
                {
                    Debug.Log($"{clientId} - SPECTATOR CONNECTED APPROVED");
                    _ = m_spectatorClientIds.Add(clientId);
                    if (m_spectatorClientIds.Count >= MAX_SPECTATOR_COUNT)
                    {
                        var props = new ExitGames.Client.Photon.Hashtable()
                        {
                            {PhotonConnectionHandler.SPECTATOR_SLOT_OPEN, 0}
                        };
                        UGBApplication.Instance.NetworkLayer.SetRoomProperty(props);
                        m_spectatorsSetFull = true;
                    }
                }

                response.Approved = true;
                return;
            }

            Debug.LogWarning($"{clientId} - {connectionStatus} CONNECTION REJECTED");

            // 为了让客户端不会在没有反馈的情况下断开连接,服务器需要告诉客户端断开的原因
            // 这可能发生在服务验证检查后或由于游戏原因(服务器已满、版本错误等)
            // 由于网络对象尚未同步(仍在审批过程中),我们需要向客户端发送自定义消息,
            // 等待UTP更新一帧并刷新该消息,然后向NetworkManager的连接审批过程提供拒绝的响应
            IEnumerator DelayDenyApproval(NetworkManager.ConnectionApprovalResponse resp)
            {
                resp.Pending = true; // 给服务器一些时间发送连接状态消息到客户端
                resp.Approved = false;
                SendServerToClientSetDisconnectReason(clientId, (int)connectionStatus);
                yield return null; // 等待一帧,让UTP在下次更新时刷新消息
                resp.Pending = false; // 连接审批过程可以完成
            }

            _ = StartCoroutine(DelayDenyApproval(response));
        }

        /// <summary>
        /// 检查客户端是否可以连接
        /// 根据连接负载和当前状态判断是否允许连接
        /// </summary>
        /// <param name="connectionPayload">连接负载数据</param>
        /// <returns>连接状态</returns>
        private ConnectionStatus CanClientConnect(ConnectionPayload connectionPayload)
        {
            if (connectionPayload.IsPlayer)
            {
                if (m_playersClientIds.Count >= MAX_PLAYER_COUNT)
                {
                    return ConnectionStatus.PlayerFull;
                }
            }
            else if (m_spectatorClientIds.Count >= MAX_SPECTATOR_COUNT)
            {
                return ConnectionStatus.SpectatorFull;
            }

            return ConnectionStatus.Success;
        }

        /// <summary>
        /// 客户端断开连接的处理
        /// 从相应列表中移除客户端,并更新房间状态
        /// </summary>
        /// <param name="clientId">断开连接的客户端ID</param>
        private void OnClientDisconnected(ulong clientId)
        {
            // 从对应列表中移除客户端
            _ = m_playersClientIds.Remove(clientId);
            _ = m_spectatorClientIds.Remove(clientId);

            if (m_playersSetFull && m_playersClientIds.Count < MAX_PLAYER_COUNT)
            {
                var props = new ExitGames.Client.Photon.Hashtable() { { PhotonConnectionHandler.PLAYER_SLOT_OPEN, 1 } };
                UGBApplication.Instance.NetworkLayer.SetRoomProperty(props);
                m_playersSetFull = false;
            }

            if (m_spectatorsSetFull && m_spectatorClientIds.Count < MAX_SPECTATOR_COUNT)
            {
                var props = new ExitGames.Client.Photon.Hashtable() { { PhotonConnectionHandler.SPECTATOR_SLOT_OPEN, 1 } };
                UGBApplication.Instance.NetworkLayer.SetRoomProperty(props);
                m_spectatorsSetFull = false;
            }

            if (clientId == m_networkManager.LocalClientId)
            {
                // 服务器断开连接时进行清理
                m_networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        /// <summary>
        /// 向客户端发送断开连接原因
        /// </summary>
        /// <param name="clientID">客户端ID</param>
        /// <param name="status">断开连接状态码</param>
        private static void SendServerToClientSetDisconnectReason(ulong clientID, int status)
        {
            var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(status);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                nameof(NetworkLayer.ReceiveServerToClientSetDisconnectReason_CustomMessage), clientID, writer);
        }
    }
}