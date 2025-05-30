// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Utilities;
using Meta.Multiplayer.Core;
using Unity.Netcode;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// Avatar网络同步组件
    /// 处理Avatar的网络同步功能
    /// 本地Avatar通过RPC向其他用户发送状态
    /// 远程Avatar接收状态RPC并应用到Avatar实体
    /// </summary>
    public class AvatarNetworking : NetworkBehaviour
    {
        /// <summary>播放平滑因子</summary>
        private const float PLAYBACK_SMOOTH_FACTOR = 0.25f;

        /// <summary>
        /// LOD频率结构体
        /// 定义不同LOD级别的更新频率
        /// </summary>
        [Serializable]
        private struct LodFrequency
        {
            /// <summary>LOD级别</summary>
            public StreamLOD LOD;
            /// <summary>更新频率</summary>
            public float UpdateFrequency;
        }

        /// <summary>LOD更新频率列表</summary>
        [SerializeField] private List<LodFrequency> m_updateFrequenySecondsByLodList;
        /// <summary>流延迟乘数</summary>
        [SerializeField] private float m_streamDelayMultiplier = 0.5f;

        /// <summary>用户ID网络变量</summary>
        private NetworkVariable<ulong> m_userId = new(
            ulong.MaxValue,
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>流延迟计时器</summary>
        private Stopwatch m_streamDelayWatch = new();
        /// <summary>当前流延迟</summary>
        private float m_currentStreamDelay;

        /// <summary>LOD更新频率字典</summary>
        private Dictionary<StreamLOD, float> m_updateFrequencySecondsByLod;
        /// <summary>上次更新时间字典</summary>
        private Dictionary<StreamLOD, double> m_lastUpdateTime = new();
        /// <summary>Avatar实体引用</summary>
        [SerializeField, AutoSet] private AvatarEntity m_entity;

        /// <summary>
        /// 用户ID属性
        /// 获取或设置用户ID
        /// </summary>
        public ulong UserId
        {
            get => m_userId.Value;
            set => m_userId.Value = value;
        }

        /// <summary>
        /// 初始化组件
        /// 设置LOD更新频率和用户ID变更事件
        /// </summary>
        public void Init()
        {
            m_updateFrequencySecondsByLod = new Dictionary<StreamLOD, float>();
            foreach (var val in m_updateFrequenySecondsByLodList)
            {
                m_updateFrequencySecondsByLod[val.LOD] = val.UpdateFrequency;
                m_lastUpdateTime[val.LOD] = 0;
            }
            if (!m_entity.IsLocal)
            {
                m_userId.OnValueChanged += OnUserIdChanged;

                if (m_userId.Value != ulong.MaxValue)
                    OnUserIdChanged(ulong.MaxValue, m_userId.Value);
            }
        }

        /// <summary>
        /// 用户ID变更处理
        /// 加载新用户ID对应的Avatar
        /// </summary>
        private void OnUserIdChanged(ulong previousValue, ulong newValue)
        {
            m_entity.LoadUser(newValue);
        }

        /// <summary>
        /// 网络对象生成时调用
        /// 触发用户ID变更事件并初始化Avatar实体
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_userId.OnValueChanged?.Invoke(ulong.MaxValue, m_userId.Value);
            m_entity.Initialize();
        }

        /// <summary>
        /// 每帧更新
        /// 更新本地Avatar的位置和旋转，并更新数据流
        /// </summary>
        private void Update()
        {
            if (m_entity && m_entity.IsLocal)
            {
                var rigTransform = CameraRigRef.Instance.transform;
                transform.SetPositionAndRotation(
                    rigTransform.position,
                    rigTransform.rotation);

                UpdateDataStream();
            }
        }

        /// <summary>
        /// 更新数据流
        /// 根据LOD级别和更新频率发送Avatar数据
        /// </summary>
        private void UpdateDataStream()
        {
            if (isActiveAndEnabled)
            {
                if (m_entity.IsCreated && m_entity.HasJoints && NetworkObject?.IsSpawned is true)
                {
                    var now = Time.unscaledTimeAsDouble;
                    var lod = StreamLOD.Low;
                    double timeSinceLastUpdate = default;
                    foreach (var lastUpdateKvp in m_lastUpdateTime)
                    {
                        var lastLod = lastUpdateKvp.Key;
                        var time = now - lastUpdateKvp.Value;
                        var frequency = m_updateFrequencySecondsByLod[lastLod];
                        if (time >= frequency)
                        {
                            if (time > timeSinceLastUpdate)
                            {
                                timeSinceLastUpdate = time;
                                lod = lastLod;
                            }
                        }
                    }

                    if (timeSinceLastUpdate != default)
                    {
                        // 同时更新所有较低频率的LOD
                        var lodFrequency = m_updateFrequencySecondsByLod[lod];
                        foreach (var lodFreqKvp in m_updateFrequencySecondsByLod)
                        {
                            if (lodFreqKvp.Value <= lodFrequency)
                            {
                                m_lastUpdateTime[lodFreqKvp.Key] = now;
                            }
                        }

                        SendAvatarData(lod);
                    }
                }
            }
        }

        /// <summary>
        /// 发送Avatar数据
        /// 记录并发送指定LOD级别的Avatar数据
        /// </summary>
        private void SendAvatarData(StreamLOD lod)
        {
            var bytes = m_entity.RecordStreamData(lod);
            SendAvatarData_ServerRpc(bytes);
        }

        /// <summary>
        /// 发送Avatar数据服务器RPC
        /// 将数据转发给除发送者外的所有客户端
        /// </summary>
        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ServerRpc(byte[] data, ServerRpcParams args = default)
        {
            var allClients = NetworkManager.Singleton.ConnectedClientsIds;
            var targetClients = allClients.Except(args.Receive.SenderClientId).ToTempArray(allClients.Count - 1);
            SendAvatarData_ClientRpc(data, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIdsNativeArray = targetClients } });
        }

        /// <summary>
        /// 发送Avatar数据客户端RPC
        /// 接收并处理Avatar数据
        /// </summary>
        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendAvatarData_ClientRpc(byte[] data, ClientRpcParams args)
        {
            ReceiveAvatarData(data);
        }

        /// <summary>
        /// 接收Avatar数据
        /// 应用接收到的数据并更新播放延迟
        /// </summary>
        private void ReceiveAvatarData(byte[] data)
        {
            if (!m_entity)
            {
                return;
            }

            var latency = (float)m_streamDelayWatch.Elapsed.TotalSeconds;

            m_entity.ApplyStreamData(data);

            var delay = Mathf.Clamp01(latency * m_streamDelayMultiplier);
            m_currentStreamDelay = Mathf.LerpUnclamped(m_currentStreamDelay, delay, PLAYBACK_SMOOTH_FACTOR);
            m_entity.SetPlaybackTimeDelay(m_currentStreamDelay);
            m_streamDelayWatch.Restart();
        }
    }
}
