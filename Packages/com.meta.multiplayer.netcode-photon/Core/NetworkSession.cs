// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 网络会话管理器
    /// 保持当前网络会话的状态，从服务器向客户端共享状态信息
    /// 处理备用主机的解析和语音房间名称的管理
    /// 确保多人游戏会话的连续性和稳定性
    /// </summary>
    public class NetworkSession : NetworkBehaviour
    {
        /// <summary>
        /// 备用主机ID
        /// 静态属性，存储当前指定的备用主机客户端ID
        /// 用于主机迁移时快速确定新主机
        /// </summary>
        public static ulong FallbackHostId { get; private set; } = ulong.MaxValue;

        /// <summary>
        /// Photon语音房间名称
        /// 静态属性，存储当前语音通信使用的Photon房间名称
        /// 用于所有客户端加入同一个语音频道
        /// </summary>
        public static string PhotonVoiceRoom { get; private set; } = "";

        /// <summary>
        /// 组件初始化
        /// 重置备用主机ID为最大值，表示尚未指定备用主机
        /// </summary>
        private void Awake()
        {
            FallbackHostId = ulong.MaxValue;
        }

        /// <summary>
        /// 对象销毁时清理
        /// 清空语音房间名称，防止其他系统引用已销毁会话的数据
        /// </summary>
        public override void OnDestroy()
        {
            PhotonVoiceRoom = "";
            base.OnDestroy();
        }

        #region FallbackHost

        /// <summary>
        /// 确定备用主机
        /// 当新客户端加入时调用，决定是否将其设为备用主机
        /// 使用最小客户端ID作为备用主机选择策略
        /// </summary>
        /// <param name="clientId">新加入的客户端ID</param>
        public void DetermineFallbackHost(ulong clientId)
        {
            // 如果新加入的客户端ID比当前备用主机ID小
            // 将其设为新的备用主机
            if (clientId < FallbackHostId)
            {
                // 广播给所有客户端
                SetFallbackHostClientRpc(clientId);
            }
            // 新加入的客户端没有改变备用主机信息
            // 只需向这个新客户端发送当前备用主机信息
            else
            {
                // 只向新客户端广播
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };

                SetFallbackHostClientRpc(FallbackHostId, clientRpcParams);
            }
        }

        /// <summary>
        /// 重新确定备用主机
        /// 当客户端断开连接时调用，如果断开的是备用主机则重新选择
        /// </summary>
        /// <param name="clientId">断开连接的客户端ID</param>
        public void RedetermineFallbackHost(ulong clientId)
        {
            // 如果断开的不是备用主机，直接返回
            if (clientId != FallbackHostId) return;

            // 重置备用主机ID
            FallbackHostId = ulong.MaxValue;

            // 如果只剩下原始主机，不需要备用主机
            if (NetworkManager.Singleton.ConnectedClients.Count < 2) return;

            // 备用主机离开了，从其他客户端中选择一个作为新的备用主机
            foreach (var id in NetworkManager.Singleton.ConnectedClients.Keys)
            {
                // 服务器或正在断开连接的客户端不能作为备用主机
                if (id == NetworkManager.ServerClientId || id == clientId)
                    continue;

                // 选择ID最小的客户端作为备用主机
                if (id < FallbackHostId)
                    FallbackHostId = id;
            }

            // 向所有客户端广播新的备用主机
            SetFallbackHostClientRpc(FallbackHostId);
        }

        /// <summary>
        /// 设置备用主机客户端RPC
        /// 在所有客户端上更新备用主机信息
        /// </summary>
        /// <param name="fallbackHostId">新的备用主机ID</param>
        /// <param name="clientRpcParams">RPC参数，用于指定目标客户端</param>
        [ClientRpc]
        private void SetFallbackHostClientRpc(ulong fallbackHostId, ClientRpcParams clientRpcParams = default)
        {
            // 如果是当前主机且备用主机ID是本地客户端ID，则不处理
            // 我们不会将当前主机设为备用主机
            if (NetworkManager.Singleton.IsHost && fallbackHostId == NetworkManager.Singleton.LocalClientId)
            {
                return;
            }

            // 如果备用主机ID没有变化，直接返回
            if (fallbackHostId == FallbackHostId)
            {
                return;
            }

            // 更新备用主机ID
            FallbackHostId = fallbackHostId;

            Debug.Log("------FALLBACK HOST STATE-------");
            Debug.Log("Client ID: " + fallbackHostId.ToString());

            // 如果本地客户端成为新的备用主机，输出警告
            if (fallbackHostId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("You are the new fallback host");
            }
            Debug.Log("--------------------------------");
        }

        #endregion // FallbackHost

        #region PhotonVoiceRoom

        /// <summary>
        /// 设置Photon语音房间名称
        /// 由主机调用，设置所有客户端应该加入的语音房间
        /// </summary>
        /// <param name="voiceRoomName">语音房间名称</param>
        public void SetPhotonVoiceRoom(string voiceRoomName)
        {
            PhotonVoiceRoom = voiceRoomName;
        }

        /// <summary>
        /// 向指定客户端更新Photon语音房间信息
        /// 通常在新客户端加入时调用，确保其获得正确的语音房间信息
        /// </summary>
        /// <param name="clientId">目标客户端ID</param>
        public void UpdatePhotonVoiceRoomToClient(ulong clientId)
        {
            // 设置RPC参数，只向指定客户端发送
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            // 发送语音房间信息给指定客户端
            SetPhotonVoiceRoomClientRpc(PhotonVoiceRoom, clientRpcParams);
        }

        /// <summary>
        /// 设置Photon语音房间客户端RPC
        /// 在客户端上更新语音房间名称
        /// </summary>
        /// <param name="photonVoiceRoom">语音房间名称</param>
        /// <param name="clientRpcParams">RPC参数</param>
        [ClientRpc]
        private void SetPhotonVoiceRoomClientRpc(string photonVoiceRoom, ClientRpcParams clientRpcParams)
        {
            Debug.Log("PHOTON VOICE ROOM TO JOIN: " + photonVoiceRoom);
            PhotonVoiceRoom = photonVoiceRoom;
        }

        #endregion
    }
}