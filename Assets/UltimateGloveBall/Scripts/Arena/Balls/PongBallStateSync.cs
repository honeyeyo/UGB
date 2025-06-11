// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Balls
{
    /// <summary>
    /// 乒乓球状态同步脚本 - 处理球状态的网络同步
    /// 功能：
    /// 1. 球位置、旋转、速度的同步
    /// 2. 球附着状态的同步
    /// 3. 数据包排序和防重复处理
    /// 4. 平滑插值和预测
    /// </summary>
    public class PongBallStateSync : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("同步设置")]
        [SerializeField] private float syncRate = 20f;                     // 同步频率 (Hz)
        [SerializeField] private float positionThreshold = 0.01f;          // 位置同步阈值
        [SerializeField] private float rotationThreshold = 1f;             // 旋转同步阈值
        [SerializeField] private float velocityThreshold = 0.1f;           // 速度同步阈值

        [Header("插值设置")]
        [SerializeField] private float positionLerpRate = 10f;             // 位置插值速率
        [SerializeField] private float rotationLerpRate = 10f;             // 旋转插值速率
        [SerializeField] private float velocityLerpRate = 5f;              // 速度插值速率

        [Header("预测设置")]
        [SerializeField] private bool enablePrediction = true;             // 启用预测
        [SerializeField] private float maxPredictionTime = 0.1f;           // 最大预测时间
        [SerializeField] private float predictionConfidence = 0.8f;        // 预测置信度

        [Header("缓冲设置")]
        [SerializeField] private int maxBufferSize = 32;                   // 最大缓冲区大小
        [SerializeField] private float maxPacketAge = 1f;                  // 最大数据包年龄
        [SerializeField] private bool enableJitterBuffer = true;           // 启用抖动缓冲

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = false;              // 启用调试日志
        [SerializeField] private bool showSyncGizmos = false;              // 显示同步调试图形
        #endregion

        #region Events
        public event Action DetectedBallShotFromServer;                    // 检测到服务器发球
        public event Action<PongBallPacket> OnPacketReceived;              // 数据包接收事件
        public event Action<float> OnNetworkLatencyUpdated;                // 网络延迟更新
        #endregion

        #region Private Fields
        private Queue<PongBallPacket> packetBuffer = new();               // 数据包缓冲区
        private uint lastAppliedSequence = 0;                             // 最后应用的序列号
        private uint localSequence = 0;                                   // 本地序列号
        private float lastSyncTime = 0f;                                  // 最后同步时间
        private float networkLatency = 0f;                                // 网络延迟

        // 组件引用
        private Rigidbody ballRigidbody;
        private PongBallNetworking ballNetworking;
        private PongBallSpin ballSpin;
        private PongBallAttachment ballAttachment;

        // 同步状态
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private Vector3 lastSentVelocity;
        private PongBallPacket lastReceivedPacket;
        private bool hasReceivedFirstPacket = false;

        // 预测状态
        private Vector3 predictedPosition;
        private Vector3 predictedVelocity;
        private float predictionTime = 0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 获取组件引用
            ballRigidbody = GetComponent<Rigidbody>();
            ballNetworking = GetComponent<PongBallNetworking>();
            ballSpin = GetComponent<PongBallSpin>();
            ballAttachment = GetComponent<PongBallAttachment>();
        }

        private void Start()
        {
            // 初始化同步状态
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSentVelocity = ballRigidbody != null ? ballRigidbody.velocity : Vector3.zero;
        }

        private void FixedUpdate()
        {
            // 服务器发送状态
            if (IsServer)
            {
                HandleServerSync();
            }

            // 客户端处理预测和插值
            if (!IsServer)
            {
                HandleClientPrediction();
                ProcessPacketBuffer();
            }
        }
        #endregion

        #region Server Sync
        /// <summary>
        /// 服务器端同步处理
        /// </summary>
        private void HandleServerSync()
        {
            if (Time.fixedTime - lastSyncTime < 1f / syncRate) return;

            // 检查是否需要同步
            if (ShouldSyncState())
            {
                // 创建数据包
                var packet = CreateCurrentStatePacket();

                // 发送给所有客户端
                SendBallStateClientRpc(packet);

                // 更新同步状态
                UpdateLastSentState();
                lastSyncTime = Time.fixedTime;
            }
        }

        /// <summary>
        /// 检查是否需要同步状态
        /// </summary>
        /// <returns>是否需要同步</returns>
        private bool ShouldSyncState()
        {
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;
            Vector3 currentVelocity = ballRigidbody != null ? ballRigidbody.velocity : Vector3.zero;

            // 位置变化检查
            bool positionChanged = Vector3.Distance(currentPosition, lastSentPosition) > positionThreshold;

            // 旋转变化检查
            bool rotationChanged = Quaternion.Angle(currentRotation, lastSentRotation) > rotationThreshold;

            // 速度变化检查
            bool velocityChanged = Vector3.Distance(currentVelocity, lastSentVelocity) > velocityThreshold;

            // 附着状态检查
            bool attachmentChanged = ballNetworking != null &&
                                   (ballNetworking.IsAttached != lastReceivedPacket.IsAttached ||
                                    ballNetworking.AttachedPlayerId != lastReceivedPacket.AttachedPlayerId);

            return positionChanged || rotationChanged || velocityChanged || attachmentChanged;
        }

        /// <summary>
        /// 创建当前状态数据包
        /// </summary>
        /// <returns>状态数据包</returns>
        private PongBallPacket CreateCurrentStatePacket()
        {
            localSequence++;

            var packet = new PongBallPacket
            {
                Sequence = localSequence,
                Timestamp = Time.time,
                IsAttached = ballNetworking != null && ballNetworking.IsAttached,
                AttachedPlayerId = ballNetworking != null ? ballNetworking.AttachedPlayerId : ulong.MaxValue,
                Position = transform.position,
                Rotation = transform.rotation,
                SyncVelocity = true,
                LinearVelocity = ballRigidbody != null ? ballRigidbody.velocity : Vector3.zero,
                AngularVelocity = ballRigidbody != null ? ballRigidbody.angularVelocity : Vector3.zero
            };

            // 添加旋转信息
            if (ballSpin != null)
            {
                packet.SpinAxis = ballSpin.SpinAxis;
                packet.SpinRate = ballSpin.SpinRate;
            }

            return packet;
        }

        /// <summary>
        /// 更新最后发送的状态
        /// </summary>
        private void UpdateLastSentState()
        {
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            lastSentVelocity = ballRigidbody != null ? ballRigidbody.velocity : Vector3.zero;
        }
        #endregion

        #region Client Sync
        /// <summary>
        /// 客户端预测处理
        /// </summary>
        private void HandleClientPrediction()
        {
            if (!enablePrediction || !hasReceivedFirstPacket) return;

            // 基于最后接收的状态进行预测
            PredictBallState();

            // 应用预测
            ApplyPrediction();
        }

        /// <summary>
        /// 预测球状态
        /// </summary>
        private void PredictBallState()
        {
            float deltaTime = Time.fixedTime - lastReceivedPacket.Timestamp;
            deltaTime = Mathf.Clamp(deltaTime, 0, maxPredictionTime);

            if (deltaTime > 0 && lastReceivedPacket.SyncVelocity)
            {
                // 简单的物理预测
                predictedPosition = lastReceivedPacket.Position + lastReceivedPacket.LinearVelocity * deltaTime;
                predictedVelocity = lastReceivedPacket.LinearVelocity;

                // 考虑重力影响
                if (!lastReceivedPacket.IsAttached)
                {
                    predictedPosition += 0.5f * Physics.gravity * deltaTime * deltaTime;
                    predictedVelocity += Physics.gravity * deltaTime;
                }

                predictionTime = deltaTime;
            }
        }

        /// <summary>
        /// 应用预测
        /// </summary>
        private void ApplyPrediction()
        {
            if (predictionTime > 0 && predictionConfidence > 0.5f)
            {
                // 插值到预测位置
                Vector3 targetPosition = Vector3.Lerp(transform.position, predictedPosition,
                                                    predictionConfidence * Time.fixedDeltaTime * positionLerpRate);
                transform.position = targetPosition;

                // 更新速度
                if (ballRigidbody != null && !ballRigidbody.isKinematic)
                {
                    Vector3 targetVelocity = Vector3.Lerp(ballRigidbody.velocity, predictedVelocity,
                                                        predictionConfidence * Time.fixedDeltaTime * velocityLerpRate);
                    ballRigidbody.velocity = targetVelocity;
                }
            }
        }

        /// <summary>
        /// 处理数据包缓冲区
        /// </summary>
        private void ProcessPacketBuffer()
        {
            if (packetBuffer.Count == 0) return;

            // 清理过期数据包
            CleanupOldPackets();

            // 按序列号排序处理
            var sortedPackets = new List<PongBallPacket>(packetBuffer);
            sortedPackets.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

            foreach (var packet in sortedPackets)
            {
                if (packet.Sequence > lastAppliedSequence)
                {
                    ApplyBallState(packet);
                    lastAppliedSequence = packet.Sequence;
                }
            }

            packetBuffer.Clear();
        }

        /// <summary>
        /// 清理过期数据包
        /// </summary>
        private void CleanupOldPackets()
        {
            var validPackets = new Queue<PongBallPacket>();
            float currentTime = Time.time;

            while (packetBuffer.Count > 0)
            {
                var packet = packetBuffer.Dequeue();
                if (currentTime - packet.Timestamp <= maxPacketAge)
                {
                    validPackets.Enqueue(packet);
                }
            }

            packetBuffer = validPackets;
        }
        #endregion

        #region Network RPCs
        /// <summary>
        /// 发送球状态（服务器调用）
        /// </summary>
        /// <param name="packet">状态数据包</param>
        [ClientRpc]
        public void SendBallStateClientRpc(PongBallPacket packet)
        {
            if (IsServer) return; // 服务器不处理自己发送的数据

            // 添加到缓冲区
            if (enableJitterBuffer)
            {
                packetBuffer.Enqueue(packet);

                // 限制缓冲区大小
                while (packetBuffer.Count > maxBufferSize)
                {
                    packetBuffer.Dequeue();
                }
            }
            else
            {
                // 直接应用
                if (packet.Sequence > lastAppliedSequence)
                {
                    ApplyBallState(packet);
                    lastAppliedSequence = packet.Sequence;
                }
            }

            // 更新网络延迟
            UpdateNetworkLatency(packet);

            // 触发事件
            OnPacketReceived?.Invoke(packet);
        }

        /// <summary>
        /// 客户端发送状态更新请求
        /// </summary>
        /// <param name="packet">状态数据包</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestStateUpdateServerRpc(PongBallPacket packet)
        {
            // 服务器接收客户端的状态更新请求
            // 这里可以进行验证和处理

            if (IsValidStateUpdate(packet))
            {
                ApplyBallState(packet);

                // 转发给其他客户端
                SendBallStateClientRpc(packet);
            }
        }

        /// <summary>
        /// 验证状态更新是否有效
        /// </summary>
        /// <param name="packet">状态数据包</param>
        /// <returns>是否有效</returns>
        private bool IsValidStateUpdate(PongBallPacket packet)
        {
            // 基本的反作弊检查
            float maxSpeed = 50f; // 最大速度限制
            float maxAcceleration = 100f; // 最大加速度限制

            // 检查速度是否合理
            if (packet.SyncVelocity && packet.LinearVelocity.magnitude > maxSpeed)
            {
                LogDebug($"状态更新被拒绝 - 速度过高: {packet.LinearVelocity.magnitude}");
                return false;
            }

            // 检查位置变化是否合理
            if (hasReceivedFirstPacket)
            {
                float deltaTime = packet.Timestamp - lastReceivedPacket.Timestamp;
                if (deltaTime > 0)
                {
                    Vector3 displacement = packet.Position - lastReceivedPacket.Position;
                    float speed = displacement.magnitude / deltaTime;

                    if (speed > maxSpeed)
                    {
                        LogDebug($"状态更新被拒绝 - 位移速度过高: {speed}");
                        return false;
                    }

                    // 检查加速度是否合理
                    if (packet.SyncVelocity)
                    {
                        Vector3 velocityChange = packet.LinearVelocity - lastReceivedPacket.LinearVelocity;
                        float acceleration = velocityChange.magnitude / deltaTime;

                        if (acceleration > maxAcceleration)
                        {
                            LogDebug($"状态更新被拒绝 - 加速度过高: {acceleration}");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        #endregion

        #region State Application
        /// <summary>
        /// 应用球状态
        /// </summary>
        /// <param name="packet">状态数据包</param>
        private void ApplyBallState(PongBallPacket packet)
        {
            // 更新最后接收的数据包
            lastReceivedPacket = packet;
            hasReceivedFirstPacket = true;

            if (packet.IsAttached)
            {
                // 球附着状态
                ApplyAttachedState(packet);
            }
            else
            {
                // 球自由状态
                ApplyFreeState(packet);
            }

            // 应用旋转
            if (ballSpin != null)
            {
                ballSpin.SetSpin(packet.SpinAxis, packet.SpinRate);
            }

            LogDebug($"应用球状态 - 序列: {packet.Sequence}, 位置: {packet.Position:F3}");
        }

        /// <summary>
        /// 应用附着状态
        /// </summary>
        /// <param name="packet">状态数据包</param>
        private void ApplyAttachedState(PongBallPacket packet)
        {
            // 如果球当前未附着，需要附着到指定玩家
            if (ballAttachment != null && !ballAttachment.IsAttached)
            {
                // 这里需要找到目标玩家并附着
                // 实际实现中可能需要通过玩家管理系统来找到对应的手部变换
                LogDebug($"需要将球附着到玩家: {packet.AttachedPlayerId}");
            }

            // 直接设置位置（附着状态下位置由附着系统控制）
            if (ballAttachment != null)
            {
                ballAttachment.ForceSetPosition(packet.Position, packet.Rotation);
            }
            else
            {
                transform.position = packet.Position;
                transform.rotation = packet.Rotation;
            }
        }

        /// <summary>
        /// 应用自由状态
        /// </summary>
        /// <param name="packet">状态数据包</param>
        private void ApplyFreeState(PongBallPacket packet)
        {
            // 如果球当前是附着状态，需要分离
            if (ballAttachment != null && ballAttachment.IsAttached)
            {
                ballAttachment.DetachBall();
                LogDebug("球已从附着状态分离");

                // 检测是否是发球
                DetectedBallShotFromServer?.Invoke();
            }

            // 平滑插值到目标状态
            ApplyPositionAndRotation(packet);

            // 应用速度
            if (packet.SyncVelocity && ballRigidbody != null)
            {
                ApplyVelocity(packet);
            }
        }

        /// <summary>
        /// 应用位置和旋转
        /// </summary>
        /// <param name="packet">状态数据包</param>
        private void ApplyPositionAndRotation(PongBallPacket packet)
        {
            if (positionLerpRate <= 0)
            {
                // 直接设置
                transform.position = packet.Position;
                transform.rotation = packet.Rotation;
            }
            else
            {
                // 平滑插值
                float deltaTime = Time.fixedDeltaTime;
                transform.position = Vector3.Lerp(transform.position, packet.Position,
                                                positionLerpRate * deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, packet.Rotation,
                                                   rotationLerpRate * deltaTime);
            }
        }

        /// <summary>
        /// 应用速度
        /// </summary>
        /// <param name="packet">状态数据包</param>
        private void ApplyVelocity(PongBallPacket packet)
        {
            if (ballRigidbody.isKinematic) return;

            if (velocityLerpRate <= 0)
            {
                // 直接设置
                ballRigidbody.velocity = packet.LinearVelocity;
                ballRigidbody.angularVelocity = packet.AngularVelocity;
            }
            else
            {
                // 平滑插值
                float deltaTime = Time.fixedDeltaTime;
                ballRigidbody.velocity = Vector3.Lerp(ballRigidbody.velocity, packet.LinearVelocity,
                                                    velocityLerpRate * deltaTime);
                ballRigidbody.angularVelocity = Vector3.Lerp(ballRigidbody.angularVelocity, packet.AngularVelocity,
                                                           velocityLerpRate * deltaTime);
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// 更新网络延迟
        /// </summary>
        /// <param name="packet">接收的数据包</param>
        private void UpdateNetworkLatency(PongBallPacket packet)
        {
            float latency = Time.time - packet.Timestamp;
            networkLatency = Mathf.Lerp(networkLatency, latency, 0.1f);

            OnNetworkLatencyUpdated?.Invoke(networkLatency);
        }

        /// <summary>
        /// 获取网络统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetNetworkStats()
        {
            return $"网络延迟: {networkLatency * 1000:F1}ms\n" +
                   $"缓冲区大小: {packetBuffer.Count}\n" +
                   $"最后序列号: {lastAppliedSequence}\n" +
                   $"同步频率: {syncRate}Hz";
        }

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[PongBallStateSync] {message}");
            }
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            if (!showSyncGizmos) return;

            // 绘制预测位置
            if (enablePrediction && predictionTime > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(predictedPosition, 0.02f);
                Gizmos.DrawLine(transform.position, predictedPosition);
            }

            // 绘制同步状态
            if (hasReceivedFirstPacket)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(lastReceivedPacket.Position, 0.025f);
            }
        }
        #endregion
    }

    /// <summary>
    /// 乒乓球状态数据包 - 用于网络同步
    /// </summary>
    [System.Serializable]
    public struct PongBallPacket : INetworkSerializable
    {
        public uint Sequence;              // 包序列号
        public float Timestamp;            // 时间戳
        public bool IsAttached;            // 是否附着到手部
        public ulong AttachedPlayerId;     // 附着的玩家ID
        public Vector3 Position;           // 球位置
        public Quaternion Rotation;        // 球旋转
        public bool SyncVelocity;          // 是否同步速度
        public Vector3 LinearVelocity;     // 线性速度
        public Vector3 AngularVelocity;    // 角速度
        public Vector3 SpinAxis;           // 旋转轴
        public float SpinRate;             // 旋转速率

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref IsAttached);
            serializer.SerializeValue(ref AttachedPlayerId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref SyncVelocity);

            if (SyncVelocity)
            {
                serializer.SerializeValue(ref LinearVelocity);
                serializer.SerializeValue(ref AngularVelocity);
            }

            serializer.SerializeValue(ref SpinAxis);
            serializer.SerializeValue(ref SpinRate);
        }
    }
}