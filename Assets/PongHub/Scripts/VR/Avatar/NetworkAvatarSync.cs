using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using Oculus.Avatar2;
using PongHub.Core;
using PongHub.VR;
using PongHub.VR.Avatar;

namespace PongHub.VR.Avatar
{
    /// <summary>
    /// Avatar网络同步组件
    /// 负责在多人游戏中同步Avatar动作、表情和状态数据
    /// </summary>
    public class NetworkAvatarSync : NetworkBehaviour
    {
        /// <summary>
        /// 同步模式枚举
        /// </summary>
        public enum SyncMode
        {
            Full,           // 完整同步（所有数据）
            Optimized,      // 优化同步（关键数据）
            Minimal         // 最小同步（基础数据）
        }

        /// <summary>
        /// 网络传输优先级
        /// </summary>
        public enum NetworkPriority
        {
            Low,            // 低优先级
            Medium,         // 中优先级  
            High,           // 高优先级
            Critical        // 关键优先级
        }

        [Header("Network Sync Settings")]
        [SerializeField]
        [Tooltip("启用网络同步")]
        private bool m_enableNetworkSync = true;

        [SerializeField]
        [Tooltip("同步模式")]
        private SyncMode m_syncMode = SyncMode.Optimized;

        [SerializeField]
        [Tooltip("网络优先级")]
        private NetworkPriority m_networkPriority = NetworkPriority.Medium;

        [SerializeField]
        [Tooltip("同步频率")]
        [Range(10f, 60f)]
        private float m_syncFrequency = 30f;

        [Header("Data Sync")]
        [SerializeField]
        [Tooltip("同步头部数据")]
        private bool m_syncHeadData = true;

        [SerializeField]
        [Tooltip("同步手部数据")]
        private bool m_syncHandData = true;

        [SerializeField]
        [Tooltip("同步手指数据")]
        private bool m_syncFingerData = false;

        [SerializeField]
        [Tooltip("同步表情数据")]
        private bool m_syncExpressionData = true;

        [SerializeField]
        [Tooltip("同步语音数据")]
        private bool m_syncVoiceData = false;

        [Header("Optimization")]
        [SerializeField]
        [Tooltip("启用数据压缩")]
        private bool m_enableCompression = true;

        [SerializeField]
        [Tooltip("位置精度（厘米）")]
        [Range(0.1f, 5f)]
        private float m_positionPrecision = 1f;

        [SerializeField]
        [Tooltip("旋转精度（度）")]
        [Range(0.1f, 10f)]
        private float m_rotationPrecision = 2f;

        [SerializeField]
        [Tooltip("启用预测")]
        private bool m_enablePrediction = true;

        [SerializeField]
        [Tooltip("预测时间（秒）")]
        [Range(0.01f, 0.5f)]
        private float m_predictionTime = 0.1f;

        [Header("Bandwidth Control")]
        [SerializeField]
        [Tooltip("最大带宽使用（KB/s）")]
        [Range(1f, 50f)]
        private float m_maxBandwidthUsage = 20f;

        [SerializeField]
        [Tooltip("启用自适应质量")]
        private bool m_enableAdaptiveQuality = true;

        [SerializeField]
        [Tooltip("网络质量阈值")]
        [Range(0.1f, 1f)]
        private float m_networkQualityThreshold = 0.7f;

        // 组件引用
        private VRAvatarManager m_avatarManager;
        private AvatarMotionSync m_motionSync;
        private AvatarExpressionSystem m_expressionSystem;
        private OvrAvatarEntity m_avatarEntity;

        // 网络变量
        private NetworkVariable<AvatarNetworkData> m_networkAvatarData = new NetworkVariable<AvatarNetworkData>();
        private NetworkVariable<ExpressionNetworkData> m_networkExpressionData = new NetworkVariable<ExpressionNetworkData>();

        // 本地数据
        private AvatarNetworkData m_localAvatarData;
        private ExpressionNetworkData m_localExpressionData;
        private AvatarNetworkData m_lastSentAvatarData;
        private ExpressionNetworkData m_lastSentExpressionData;

        // 预测和插值
        private AvatarNetworkData m_predictedData;
        private float m_lastUpdateTime = 0f;
        private float m_syncInterval = 0f;

        // 网络统计
        private float m_totalBytesSent = 0f;
        private float m_totalBytesReceived = 0f; 
        private int m_packetsPerSecond = 0;
        private float m_networkLatency = 0f;

        // 事件
        public UnityEvent<ulong> OnAvatarConnected = new UnityEvent<ulong>();
        public UnityEvent<ulong> OnAvatarDisconnected = new UnityEvent<ulong>();
        public UnityEvent<float> OnNetworkQualityChanged = new UnityEvent<float>();

        /// <summary>
        /// Avatar网络数据结构
        /// </summary>
        [System.Serializable]
        public struct AvatarNetworkData : INetworkSerializable
        {
            public Vector3 headPosition;
            public Quaternion headRotation;
            public Vector3 leftHandPosition;
            public Quaternion leftHandRotation;
            public Vector3 rightHandPosition;
            public Quaternion rightHandRotation;
            public float[] fingerFlexions; // 10个手指的弯曲数据
            public float timestamp;
            public bool isValid;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref headPosition);
                serializer.SerializeValue(ref headRotation);
                serializer.SerializeValue(ref leftHandPosition);
                serializer.SerializeValue(ref leftHandRotation);
                serializer.SerializeValue(ref rightHandPosition);
                serializer.SerializeValue(ref rightHandRotation);
                
                if (fingerFlexions == null)
                    fingerFlexions = new float[10];
                
                for (int i = 0; i < 10; i++)
                {
                    serializer.SerializeValue(ref fingerFlexions[i]);
                }
                
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref isValid);
            }
        }

        /// <summary>
        /// 表情网络数据结构
        /// </summary>
        [System.Serializable]
        public struct ExpressionNetworkData : INetworkSerializable
        {
            public int currentExpression; // BasicExpression枚举值
            public float expressionIntensity;
            public float[] visemeWeights; // 15个视素权重
            public Vector3 gazeDirection;
            public float eyeOpenness;
            public float speechVolume;
            public float timestamp;
            public bool isValid;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref currentExpression);
                serializer.SerializeValue(ref expressionIntensity);
                
                if (visemeWeights == null)
                    visemeWeights = new float[15];
                
                for (int i = 0; i < 15; i++)
                {
                    serializer.SerializeValue(ref visemeWeights[i]);
                }
                
                serializer.SerializeValue(ref gazeDirection);
                serializer.SerializeValue(ref eyeOpenness);
                serializer.SerializeValue(ref speechVolume);
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref isValid);
            }
        }

        /// <summary>
        /// 是否启用网络同步
        /// </summary>
        public bool IsNetworkSyncEnabled => m_enableNetworkSync;

        /// <summary>
        /// 当前网络延迟
        /// </summary>
        public float NetworkLatency => m_networkLatency;

        /// <summary>
        /// 每秒数据包数量
        /// </summary>
        public int PacketsPerSecond => m_packetsPerSecond;

        /// <summary>
        /// 带宽使用量（KB/s）
        /// </summary>
        public float BandwidthUsage => m_totalBytesSent / 1024f;

        private void Awake()
        {
            InitializeComponents();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsOwner)
            {
                StartCoroutine(InitializeNetworkSyncAsync());
            }
            else
            {
                // 订阅远程Avatar数据变化
                m_networkAvatarData.OnValueChanged += OnRemoteAvatarDataChanged;
                m_networkExpressionData.OnValueChanged += OnRemoteExpressionDataChanged;
            }

            Debug.Log($"[NetworkAvatarSync] Network spawned for client {OwnerClientId}");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                m_networkAvatarData.OnValueChanged -= OnRemoteAvatarDataChanged;
                m_networkExpressionData.OnValueChanged -= OnRemoteExpressionDataChanged;
            }

            OnAvatarDisconnected?.Invoke(OwnerClientId);
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsOwner && m_enableNetworkSync)
            {
                if (Time.time - m_lastUpdateTime >= m_syncInterval)
                {
                    UpdateNetworkSync();
                    m_lastUpdateTime = Time.time;
                }
            }
            else if (!IsOwner)
            {
                // 应用远程数据到本地Avatar
                ApplyRemoteDataToAvatar();
            }
        }

        private void InitializeComponents()
        {
            // 获取Avatar相关组件
            m_avatarManager = GetComponent<VRAvatarManager>();
            m_motionSync = GetComponent<AvatarMotionSync>();
            m_expressionSystem = GetComponent<AvatarExpressionSystem>();

            // 计算同步间隔
            m_syncInterval = 1f / m_syncFrequency;

            // 初始化数据结构
            m_localAvatarData = new AvatarNetworkData
            {
                fingerFlexions = new float[10],
                isValid = false
            };

            m_localExpressionData = new ExpressionNetworkData
            {
                visemeWeights = new float[15],
                isValid = false
            };

            Debug.Log("[NetworkAvatarSync] Components initialized");
        }

        private IEnumerator InitializeNetworkSyncAsync()
        {
            // 等待Avatar组件初始化
            while (m_avatarManager == null || !m_avatarManager.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            m_avatarEntity = m_avatarManager.AvatarEntity;
            
            // 根据同步模式配置数据同步
            ConfigureSyncMode();

            OnAvatarConnected?.Invoke(OwnerClientId);
            Debug.Log("[NetworkAvatarSync] Network sync initialized");
        }

        private void ConfigureSyncMode()
        {
            switch (m_syncMode)
            {
                case SyncMode.Minimal:
                    m_syncHeadData = true;
                    m_syncHandData = true;
                    m_syncFingerData = false;
                    m_syncExpressionData = false;
                    m_syncVoiceData = false;
                    break;
                    
                case SyncMode.Optimized:
                    m_syncHeadData = true;
                    m_syncHandData = true;
                    m_syncFingerData = false;
                    m_syncExpressionData = true;
                    m_syncVoiceData = false;
                    break;
                    
                case SyncMode.Full:
                    m_syncHeadData = true;
                    m_syncHandData = true;
                    m_syncFingerData = true;
                    m_syncExpressionData = true;
                    m_syncVoiceData = true;
                    break;
            }

            Debug.Log($"[NetworkAvatarSync] Configured for {m_syncMode} sync mode");
        }

        private void UpdateNetworkSync()
        {
            // 收集本地Avatar数据
            CollectLocalAvatarData();
            CollectLocalExpressionData();

            // 检查是否需要发送数据
            if (ShouldSendAvatarData())
            {
                SendAvatarData();
            }

            if (ShouldSendExpressionData())
            {
                SendExpressionData();
            }

            // 更新网络统计
            UpdateNetworkStats();
        }

        private void CollectLocalAvatarData()
        {
            if (m_avatarEntity == null) return;

            m_localAvatarData.timestamp = Time.time;
            m_localAvatarData.isValid = true;

            // 收集头部数据
            if (m_syncHeadData)
            {
                var headTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.Head);
                if (headTransform != null)
                {
                    m_localAvatarData.headPosition = CompressPosition(headTransform.position);
                    m_localAvatarData.headRotation = CompressRotation(headTransform.rotation);
                }
            }

            // 收集手部数据
            if (m_syncHandData)
            {
                var leftHandTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.LeftHandWrist);
                var rightHandTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.RightHandWrist);

                if (leftHandTransform != null)
                {
                    m_localAvatarData.leftHandPosition = CompressPosition(leftHandTransform.position);
                    m_localAvatarData.leftHandRotation = CompressRotation(leftHandTransform.rotation);
                }

                if (rightHandTransform != null)
                {
                    m_localAvatarData.rightHandPosition = CompressPosition(rightHandTransform.position);
                    m_localAvatarData.rightHandRotation = CompressRotation(rightHandTransform.rotation);
                }
            }

            // 收集手指数据
            if (m_syncFingerData && m_motionSync != null)
            {
                // 这里应该从MotionSync组件获取手指弯曲数据
                // 暂时使用模拟数据
                for (int i = 0; i < 10; i++)
                {
                    m_localAvatarData.fingerFlexions[i] = 0f;
                }
            }
        }

        private void CollectLocalExpressionData()
        {
            if (m_expressionSystem == null) return;

            m_localExpressionData.timestamp = Time.time;
            m_localExpressionData.isValid = true;

            // 收集表情数据
            if (m_syncExpressionData)
            {
                m_localExpressionData.currentExpression = (int)m_expressionSystem.CurrentExpression;
                m_localExpressionData.gazeDirection = m_expressionSystem.GazeDirection;
            }

            // 收集语音数据
            if (m_syncVoiceData)
            {
                m_localExpressionData.speechVolume = m_expressionSystem.SpeechVolume;
            }
        }

        private bool ShouldSendAvatarData()
        {
            // 检查数据是否有显著变化
            if (!m_localAvatarData.isValid || !m_lastSentAvatarData.isValid)
                return true;

            float positionThreshold = m_positionPrecision / 100f;
            float rotationThreshold = m_rotationPrecision;

            // 检查头部变化
            if (Vector3.Distance(m_localAvatarData.headPosition, m_lastSentAvatarData.headPosition) > positionThreshold)
                return true;

            if (Quaternion.Angle(m_localAvatarData.headRotation, m_lastSentAvatarData.headRotation) > rotationThreshold)
                return true;

            // 检查手部变化
            if (Vector3.Distance(m_localAvatarData.leftHandPosition, m_lastSentAvatarData.leftHandPosition) > positionThreshold)
                return true;

            if (Vector3.Distance(m_localAvatarData.rightHandPosition, m_lastSentAvatarData.rightHandPosition) > positionThreshold)
                return true;

            return false;
        }

        private bool ShouldSendExpressionData()
        {
            if (!m_localExpressionData.isValid || !m_lastSentExpressionData.isValid)
                return true;

            // 检查表情变化
            if (m_localExpressionData.currentExpression != m_lastSentExpressionData.currentExpression)
                return true;

            // 检查语音变化
            if (Mathf.Abs(m_localExpressionData.speechVolume - m_lastSentExpressionData.speechVolume) > 0.05f)
                return true;

            return false;
        }

        private void SendAvatarData()
        {
            m_networkAvatarData.Value = m_localAvatarData;
            m_lastSentAvatarData = m_localAvatarData;
            m_totalBytesSent += EstimateAvatarDataSize();
        }

        private void SendExpressionData()
        {
            m_networkExpressionData.Value = m_localExpressionData;
            m_lastSentExpressionData = m_localExpressionData;
            m_totalBytesSent += EstimateExpressionDataSize();
        }

        private void OnRemoteAvatarDataChanged(AvatarNetworkData previousValue, AvatarNetworkData newValue)
        {
            if (!newValue.isValid) return;

            // 计算网络延迟
            m_networkLatency = Time.time - newValue.timestamp;
            m_totalBytesReceived += EstimateAvatarDataSize();

            // 应用预测
            if (m_enablePrediction)
            {
                m_predictedData = PredictAvatarData(newValue);
            }
        }

        private void OnRemoteExpressionDataChanged(ExpressionNetworkData previousValue, ExpressionNetworkData newValue)
        {
            if (!newValue.isValid) return;

            m_totalBytesReceived += EstimateExpressionDataSize();

            // 直接应用表情数据（表情通常不需要预测）
            ApplyExpressionDataToAvatar(newValue);
        }

        private void ApplyRemoteDataToAvatar()
        {
            if (m_avatarEntity == null) return;

            // 使用预测数据或原始数据
            AvatarNetworkData dataToApply = m_enablePrediction ? m_predictedData : m_networkAvatarData.Value;
            
            if (!dataToApply.isValid) return;

            // 应用头部数据
            if (m_syncHeadData)
            {
                var headTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.Head);
                if (headTransform != null)
                {
                    headTransform.position = Vector3.Lerp(headTransform.position, dataToApply.headPosition, Time.deltaTime * 10f);
                    headTransform.rotation = Quaternion.Lerp(headTransform.rotation, dataToApply.headRotation, Time.deltaTime * 10f);
                }
            }

            // 应用手部数据
            if (m_syncHandData)
            {
                var leftHandTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.LeftHandWrist);
                var rightHandTransform = m_avatarEntity.GetSkeletonTransformByType(CAPI.ovrAvatar2JointType.RightHandWrist);

                if (leftHandTransform != null)
                {
                    leftHandTransform.position = Vector3.Lerp(leftHandTransform.position, dataToApply.leftHandPosition, Time.deltaTime * 15f);
                    leftHandTransform.rotation = Quaternion.Lerp(leftHandTransform.rotation, dataToApply.leftHandRotation, Time.deltaTime * 15f);
                }

                if (rightHandTransform != null)
                {
                    rightHandTransform.position = Vector3.Lerp(rightHandTransform.position, dataToApply.rightHandPosition, Time.deltaTime * 15f);
                    rightHandTransform.rotation = Quaternion.Lerp(rightHandTransform.rotation, dataToApply.rightHandRotation, Time.deltaTime * 15f);
                }
            }
        }

        private void ApplyExpressionDataToAvatar(ExpressionNetworkData expressionData)
        {
            if (m_expressionSystem == null) return;

            // 应用表情
            if (m_syncExpressionData)
            {
                var expression = (AvatarExpressionSystem.BasicExpression)expressionData.currentExpression;
                m_expressionSystem.SetExpression(expression, expressionData.expressionIntensity);
            }
        }

        private AvatarNetworkData PredictAvatarData(AvatarNetworkData data)
        {
            // 简单的线性预测
            // 在实际应用中可以使用更复杂的预测算法
            
            float predictionFactor = m_predictionTime * 10f; // 简单的速度估算
            
            AvatarNetworkData predictedData = data;
            
            // 这里可以基于之前的数据来预测位置变化
            // 暂时返回原始数据
            
            return predictedData;
        }

        private Vector3 CompressPosition(Vector3 position)
        {
            if (!m_enableCompression) return position;

            // 将位置压缩到指定精度
            float precision = m_positionPrecision / 100f;
            return new Vector3(
                Mathf.Round(position.x / precision) * precision,
                Mathf.Round(position.y / precision) * precision,
                Mathf.Round(position.z / precision) * precision
            );
        }

        private Quaternion CompressRotation(Quaternion rotation)
        {
            if (!m_enableCompression) return rotation;

            // 将旋转压缩到指定精度
            Vector3 euler = rotation.eulerAngles;
            float precision = m_rotationPrecision;
            
            return Quaternion.Euler(
                Mathf.Round(euler.x / precision) * precision,
                Mathf.Round(euler.y / precision) * precision,
                Mathf.Round(euler.z / precision) * precision
            );
        }

        private float EstimateAvatarDataSize()
        {
            // 估算Avatar数据大小（字节）
            float size = 0f;
            size += 12f; // Vector3 * 3 (head, leftHand, rightHand positions)
            size += 16f; // Quaternion * 3 (rotations)
            
            if (m_syncFingerData)
            {
                size += 40f; // float * 10 (finger flexions)
            }
            
            size += 8f; // timestamp + isValid
            
            return size;
        }

        private float EstimateExpressionDataSize()
        {
            // 估算表情数据大小（字节）
            float size = 0f;
            size += 4f; // int (expression)
            size += 4f; // float (intensity)
            size += 60f; // float * 15 (viseme weights)
            size += 12f; // Vector3 (gaze direction)
            size += 12f; // float * 3 (eye openness, speech volume, timestamp)
            size += 1f; // bool (isValid)
            
            return size;
        }

        private void UpdateNetworkStats()
        {
            // 重置每秒统计
            if (Time.time - m_lastUpdateTime >= 1f)
            {
                m_packetsPerSecond = 0;
                m_totalBytesSent = 0f;
                m_totalBytesReceived = 0f;
            }
            
            m_packetsPerSecond++;

            // 自适应质量调整
            if (m_enableAdaptiveQuality)
            {
                AdjustQualityBasedOnNetwork();
            }
        }

        private void AdjustQualityBasedOnNetwork()
        {
            float networkQuality = CalculateNetworkQuality();
            
            if (networkQuality < m_networkQualityThreshold)
            {
                // 降低同步质量
                if (m_syncMode == SyncMode.Full)
                {
                    SetSyncMode(SyncMode.Optimized);
                }
                else if (m_syncMode == SyncMode.Optimized)
                {
                    SetSyncMode(SyncMode.Minimal);
                }
            }
            else if (networkQuality > 0.9f)
            {
                // 提高同步质量
                if (m_syncMode == SyncMode.Minimal)
                {
                    SetSyncMode(SyncMode.Optimized);
                }
                else if (m_syncMode == SyncMode.Optimized && BandwidthUsage < m_maxBandwidthUsage * 0.7f)
                {
                    SetSyncMode(SyncMode.Full);
                }
            }

            OnNetworkQualityChanged?.Invoke(networkQuality);
        }

        private float CalculateNetworkQuality()
        {
            // 基于延迟和丢包率计算网络质量
            float latencyScore = Mathf.Clamp01(1f - (m_networkLatency / 0.2f)); // 200ms为差网络
            float bandwidthScore = Mathf.Clamp01(1f - (BandwidthUsage / m_maxBandwidthUsage));
            
            return (latencyScore + bandwidthScore) / 2f;
        }

        /// <summary>
        /// 设置同步模式
        /// </summary>
        public void SetSyncMode(SyncMode mode)
        {
            m_syncMode = mode;
            ConfigureSyncMode();
            Debug.Log($"[NetworkAvatarSync] Sync mode changed to: {mode}");
        }

        /// <summary>
        /// 设置同步频率
        /// </summary>
        public void SetSyncFrequency(float frequency)
        {
            m_syncFrequency = Mathf.Clamp(frequency, 10f, 60f);
            m_syncInterval = 1f / m_syncFrequency;
            Debug.Log($"[NetworkAvatarSync] Sync frequency set to: {frequency}Hz");
        }

        /// <summary>
        /// 获取网络同步诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Network Avatar Sync Diagnostics ===");
            diagnostics.AppendLine($"Network Sync Enabled: {m_enableNetworkSync}");
            diagnostics.AppendLine($"Is Owner: {IsOwner}");
            diagnostics.AppendLine($"Owner Client ID: {OwnerClientId}");
            diagnostics.AppendLine($"Sync Mode: {m_syncMode}");
            diagnostics.AppendLine($"Network Priority: {m_networkPriority}");
            diagnostics.AppendLine($"Sync Frequency: {m_syncFrequency:F1}Hz");
            diagnostics.AppendLine($"Network Latency: {m_networkLatency * 1000f:F1}ms");
            diagnostics.AppendLine($"Packets Per Second: {m_packetsPerSecond}");
            diagnostics.AppendLine($"Bandwidth Usage: {BandwidthUsage:F2}KB/s");
            diagnostics.AppendLine($"Max Bandwidth: {m_maxBandwidthUsage:F1}KB/s");
            diagnostics.AppendLine($"Network Quality: {CalculateNetworkQuality():F2}");
            diagnostics.AppendLine($"Adaptive Quality: {m_enableAdaptiveQuality}");
            diagnostics.AppendLine($"Data Compression: {m_enableCompression}");
            diagnostics.AppendLine($"Prediction Enabled: {m_enablePrediction}");
            diagnostics.AppendLine($"Sync Head Data: {m_syncHeadData}");
            diagnostics.AppendLine($"Sync Hand Data: {m_syncHandData}");
            diagnostics.AppendLine($"Sync Finger Data: {m_syncFingerData}");
            diagnostics.AppendLine($"Sync Expression Data: {m_syncExpressionData}");
            diagnostics.AppendLine($"Sync Voice Data: {m_syncVoiceData}");
            
            return diagnostics.ToString();
        }
    }
}