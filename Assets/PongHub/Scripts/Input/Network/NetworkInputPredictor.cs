using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using PongHub.Input.Performance;

namespace PongHub.Input.Network
{
    /// <summary>
    /// 网络输入预测器 - Epic-3网络优化核心组件
    /// 功能：客户端输入预测和服务器权威回滚，实现平滑的多人VR输入体验
    /// 目标：减少网络延迟感知，提供流畅的多人交互
    /// </summary>
    public class NetworkInputPredictor : NetworkBehaviour
    {
        [Header("预测配置")]
        [SerializeField]
        [Tooltip("Prediction Buffer Size / 预测缓冲区大小 - Number of frames to buffer for prediction")]
        private int m_predictionBufferSize = 60; // 1秒 @ 60FPS

        [SerializeField]
        [Tooltip("Max Prediction Time / 最大预测时间 - Maximum prediction time in seconds")]
        private float m_maxPredictionTime = 0.5f;

        [SerializeField]
        [Tooltip("Rollback Threshold / 回滚阈值 - Position difference threshold for rollback (meters)")]
        private float m_rollbackThreshold = 0.02f; // 2cm

        [Header("网络设置")]
        [SerializeField]
        [Tooltip("Input Send Rate / 输入发送频率 - Rate to send input to server (Hz)")]
        private int m_inputSendRate = 30;

        [SerializeField]
        [Tooltip("Enable Client Prediction / 启用客户端预测 - Whether to enable client-side prediction")]
        private bool m_enableClientPrediction = true;

        [SerializeField]
        [Tooltip("Enable Server Reconciliation / 启用服务器校正 - Whether to enable server reconciliation")]
        private bool m_enableServerReconciliation = true;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        [SerializeField]
        [Tooltip("Debug Draw Predictions / 调试绘制预测 - Whether to draw prediction debug lines")]
        private bool m_debugDrawPredictions = false;

        // 输入状态缓冲区
        private CircularBuffer<PredictedInputState> m_inputBuffer;
        private CircularBuffer<PredictedInputState> m_serverStateBuffer;
        
        // 当前状态
        private PredictedInputState m_currentLocalState;
        private PredictedInputState m_lastConfirmedState;
        
        // 网络统计
        private float m_averageRTT = 0f;
        private float m_lastInputSendTime = 0f;
        private int m_inputSequenceNumber = 0;
        private int m_lastConfirmedSequence = 0;
        
        // 性能统计
        private int m_totalPredictions = 0;
        private int m_correctPredictions = 0;
        private int m_rollbackCount = 0;
        private float m_averagePredictionError = 0f;

        // 事件系统
        public System.Action<PredictedInputState> OnInputPredicted;
        public System.Action<PredictedInputState, PredictedInputState> OnRollbackOccurred;
        public System.Action<NetworkPredictionStats> OnStatsUpdated;

        /// <summary>
        /// 预测输入状态结构
        /// </summary>
        [System.Serializable]
        public struct PredictedInputState : INetworkSerializable
        {
            public int sequenceNumber;
            public float timestamp;
            public Vector3 leftHandPosition;
            public Vector3 rightHandPosition;
            public Quaternion leftHandRotation;
            public Quaternion rightHandRotation;
            public Vector2 leftStick;
            public Vector2 rightStick;
            public float leftGrip;
            public float rightGrip;
            public uint buttonStates;
            public Vector3 predictedVelocity;
            public bool isConfirmed;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref sequenceNumber);
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref leftHandPosition);
                serializer.SerializeValue(ref rightHandPosition);
                serializer.SerializeValue(ref leftHandRotation);
                serializer.SerializeValue(ref rightHandRotation);
                serializer.SerializeValue(ref leftStick);
                serializer.SerializeValue(ref rightStick);
                serializer.SerializeValue(ref leftGrip);
                serializer.SerializeValue(ref rightGrip);
                serializer.SerializeValue(ref buttonStates);
                serializer.SerializeValue(ref predictedVelocity);
                serializer.SerializeValue(ref isConfirmed);
            }

            /// <summary>
            /// 计算与另一个状态的位置差异
            /// </summary>
            public float GetPositionDifference(PredictedInputState other)
            {
                float leftDiff = Vector3.Distance(leftHandPosition, other.leftHandPosition);
                float rightDiff = Vector3.Distance(rightHandPosition, other.rightHandPosition);
                return Mathf.Max(leftDiff, rightDiff);
            }

            /// <summary>
            /// 线性插值到目标状态
            /// </summary>
            public PredictedInputState Lerp(PredictedInputState target, float t)
            {
                return new PredictedInputState
                {
                    sequenceNumber = this.sequenceNumber,
                    timestamp = Mathf.Lerp(this.timestamp, target.timestamp, t),
                    leftHandPosition = Vector3.Lerp(this.leftHandPosition, target.leftHandPosition, t),
                    rightHandPosition = Vector3.Lerp(this.rightHandPosition, target.rightHandPosition, t),
                    leftHandRotation = Quaternion.Lerp(this.leftHandRotation, target.leftHandRotation, t),
                    rightHandRotation = Quaternion.Lerp(this.rightHandRotation, target.rightHandRotation, t),
                    leftStick = Vector2.Lerp(this.leftStick, target.leftStick, t),
                    rightStick = Vector2.Lerp(this.rightStick, target.rightStick, t),
                    leftGrip = Mathf.Lerp(this.leftGrip, target.leftGrip, t),
                    rightGrip = Mathf.Lerp(this.rightGrip, target.rightGrip, t),
                    buttonStates = t > 0.5f ? target.buttonStates : this.buttonStates,
                    predictedVelocity = Vector3.Lerp(this.predictedVelocity, target.predictedVelocity, t),
                    isConfirmed = target.isConfirmed
                };
            }
        }

        /// <summary>
        /// 循环缓冲区实现
        /// </summary>
        private class CircularBuffer<T>
        {
            private readonly T[] m_buffer;
            private int m_head = 0;
            private int m_tail = 0;
            private int m_count = 0;

            public CircularBuffer(int capacity)
            {
                m_buffer = new T[capacity];
            }

            public void Add(T item)
            {
                m_buffer[m_head] = item;
                m_head = (m_head + 1) % m_buffer.Length;
                
                if (m_count < m_buffer.Length)
                {
                    m_count++;
                }
                else
                {
                    m_tail = (m_tail + 1) % m_buffer.Length;
                }
            }

            public T GetByIndex(int index)
            {
                if (index < 0 || index >= m_count) return default(T);
                int actualIndex = (m_tail + index) % m_buffer.Length;
                return m_buffer[actualIndex];
            }

            public T GetLatest()
            {
                if (m_count == 0) return default(T);
                int latestIndex = (m_head - 1 + m_buffer.Length) % m_buffer.Length;
                return m_buffer[latestIndex];
            }

            public int Count => m_count;

            public void Clear()
            {
                m_head = 0;
                m_tail = 0;
                m_count = 0;
            }
        }

        /// <summary>
        /// 网络预测统计结构
        /// </summary>
        [System.Serializable]
        public struct NetworkPredictionStats
        {
            public float averageRTT;
            public int totalPredictions;
            public int correctPredictions;
            public float predictionAccuracy;
            public int rollbackCount;
            public float averagePredictionError;
            public int bufferedInputs;
        }

        private void Awake()
        {
            // 初始化缓冲区
            m_inputBuffer = new CircularBuffer<PredictedInputState>(m_predictionBufferSize);
            m_serverStateBuffer = new CircularBuffer<PredictedInputState>(m_predictionBufferSize);
            
            // 初始化状态
            m_currentLocalState = new PredictedInputState();
            m_lastConfirmedState = new PredictedInputState();
        }

        private void Update()
        {
            if (!IsSpawned) return;

            // 更新RTT估算
            UpdateRTTEstimate();

            if (IsOwner)
            {
                // 客户端：处理输入预测
                ProcessClientInputPrediction();
                
                // 发送输入到服务器
                if (ShouldSendInput())
                {
                    SendInputToServer();
                }
            }
        }

        /// <summary>
        /// 处理客户端输入预测
        /// </summary>
        private void ProcessClientInputPrediction()
        {
            if (!m_enableClientPrediction) return;

            // 创建新的预测状态
            var newState = CreatePredictedState();
            
            // 添加到缓冲区
            m_inputBuffer.Add(newState);
            m_currentLocalState = newState;
            
            // 触发预测事件
            OnInputPredicted?.Invoke(newState);
            
            // 绘制调试信息
            if (m_debugDrawPredictions)
            {
                DrawPredictionDebug(newState);
            }

            m_totalPredictions++;
        }

        /// <summary>
        /// 创建预测状态
        /// </summary>
        private PredictedInputState CreatePredictedState()
        {
            var state = new PredictedInputState
            {
                sequenceNumber = ++m_inputSequenceNumber,
                timestamp = Time.unscaledTime,
                isConfirmed = false
            };

            // 从当前输入系统获取数据
            if (TryGetComponent<PongHubInputManager>(out var inputManager))
            {
                // 获取手部位置和旋转
                state.leftHandPosition = GetHandPosition(true);
                state.rightHandPosition = GetHandPosition(false);
                state.leftHandRotation = GetHandRotation(true);
                state.rightHandRotation = GetHandRotation(false);
                
                // 获取控制器输入
                // 这里需要与PongHubInputManager集成
                // state.leftStick = inputManager.GetLeftStick();
                // state.rightStick = inputManager.GetRightStick();
                // state.leftGrip = inputManager.GetLeftGrip();
                // state.rightGrip = inputManager.GetRightGrip();
                // state.buttonStates = inputManager.GetButtonStates();
            }

            // 预测速度（用于外推）
            if (m_inputBuffer.Count > 0)
            {
                var lastState = m_inputBuffer.GetLatest();
                float deltaTime = state.timestamp - lastState.timestamp;
                if (deltaTime > 0)
                {
                    state.predictedVelocity = (state.leftHandPosition - lastState.leftHandPosition) / deltaTime;
                }
            }

            return state;
        }

        /// <summary>
        /// 获取手部位置（临时实现，需要与实际VR系统集成）
        /// </summary>
        private Vector3 GetHandPosition(bool isLeft)
        {
            // 这里需要与实际的VR手部跟踪系统集成
            // 临时返回零向量
            return Vector3.zero;
        }

        /// <summary>
        /// 获取手部旋转（临时实现，需要与实际VR系统集成）
        /// </summary>
        private Quaternion GetHandRotation(bool isLeft)
        {
            // 这里需要与实际的VR手部跟踪系统集成
            // 临时返回单位四元数
            return Quaternion.identity;
        }

        /// <summary>
        /// 检查是否应该发送输入
        /// </summary>
        private bool ShouldSendInput()
        {
            float interval = 1f / m_inputSendRate;
            return Time.unscaledTime - m_lastInputSendTime >= interval;
        }

        /// <summary>
        /// 发送输入到服务器
        /// </summary>
        private void SendInputToServer()
        {
            if (m_currentLocalState.sequenceNumber == 0) return;

            SendInputServerRpc(m_currentLocalState);
            m_lastInputSendTime = Time.unscaledTime;
        }

        /// <summary>
        /// 服务器RPC：接收客户端输入
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        private void SendInputServerRpc(PredictedInputState inputState)
        {
            if (!IsServer) return;

            // 服务器处理输入并确认
            var confirmedState = ProcessServerInput(inputState);
            
            // 发送确认回客户端
            SendConfirmationClientRpc(confirmedState);
        }

        /// <summary>
        /// 服务器处理输入
        /// </summary>
        private PredictedInputState ProcessServerInput(PredictedInputState inputState)
        {
            // 服务器权威处理输入
            var confirmedState = inputState;
            confirmedState.isConfirmed = true;
            confirmedState.timestamp = Time.unscaledTime;

            // 服务器可以在这里进行验证和修正
            // 例如：反作弊检查、物理约束等

            return confirmedState;
        }

        /// <summary>
        /// 客户端RPC：接收服务器确认
        /// </summary>
        [ClientRpc]
        private void SendConfirmationClientRpc(PredictedInputState confirmedState)
        {
            if (!IsOwner) return;

            ProcessServerConfirmation(confirmedState);
        }

        /// <summary>
        /// 处理服务器确认
        /// </summary>
        private void ProcessServerConfirmation(PredictedInputState confirmedState)
        {
            if (!m_enableServerReconciliation) return;

            // 找到对应的本地预测状态
            var localState = FindLocalStateBySequence(confirmedState.sequenceNumber);
            if (localState.sequenceNumber == 0) return;

            // 计算预测误差
            float predictionError = localState.GetPositionDifference(confirmedState);
            UpdatePredictionAccuracy(predictionError);

            // 检查是否需要回滚
            if (predictionError > m_rollbackThreshold)
            {
                PerformRollback(confirmedState, localState);
            }
            else
            {
                m_correctPredictions++;
            }

            m_lastConfirmedState = confirmedState;
            m_lastConfirmedSequence = confirmedState.sequenceNumber;
        }

        /// <summary>
        /// 查找本地预测状态
        /// </summary>
        private PredictedInputState FindLocalStateBySequence(int sequenceNumber)
        {
            for (int i = 0; i < m_inputBuffer.Count; i++)
            {
                var state = m_inputBuffer.GetByIndex(i);
                if (state.sequenceNumber == sequenceNumber)
                {
                    return state;
                }
            }
            return new PredictedInputState();
        }

        /// <summary>
        /// 执行回滚
        /// </summary>
        private void PerformRollback(PredictedInputState serverState, PredictedInputState localState)
        {
            m_rollbackCount++;
            
            // 通知系统发生了回滚
            OnRollbackOccurred?.Invoke(serverState, localState);
            
            // 重新预测从服务器状态开始的所有后续状态
            ReplayInputsFromServerState(serverState);
            
            if (m_showDebugInfo)
            {
                Debug.LogWarning($"[NetworkInputPredictor] 回滚发生 - 误差: {localState.GetPositionDifference(serverState):F4}m");
            }
        }

        /// <summary>
        /// 从服务器状态重新播放输入
        /// </summary>
        private void ReplayInputsFromServerState(PredictedInputState serverState)
        {
            // 找到需要重播的输入
            var inputsToReplay = new List<PredictedInputState>();
            
            for (int i = 0; i < m_inputBuffer.Count; i++)
            {
                var input = m_inputBuffer.GetByIndex(i);
                if (input.sequenceNumber > serverState.sequenceNumber)
                {
                    inputsToReplay.Add(input);
                }
            }

            // 重新应用这些输入
            var currentState = serverState;
            foreach (var input in inputsToReplay)
            {
                currentState = ExtrapolateState(currentState, input);
            }

            m_currentLocalState = currentState;
        }

        /// <summary>
        /// 状态外推
        /// </summary>
        private PredictedInputState ExtrapolateState(PredictedInputState fromState, PredictedInputState input)
        {
            float deltaTime = input.timestamp - fromState.timestamp;
            
            var extrapolatedState = input;
            
            // 使用速度进行位置外推
            if (deltaTime > 0 && fromState.predictedVelocity != Vector3.zero)
            {
                extrapolatedState.leftHandPosition = fromState.leftHandPosition + fromState.predictedVelocity * deltaTime;
                extrapolatedState.rightHandPosition = fromState.rightHandPosition + fromState.predictedVelocity * deltaTime;
            }

            return extrapolatedState;
        }

        /// <summary>
        /// 更新预测准确率
        /// </summary>
        private void UpdatePredictionAccuracy(float error)
        {
            m_averagePredictionError = (m_averagePredictionError * (m_totalPredictions - 1) + error) / m_totalPredictions;
        }

        /// <summary>
        /// 更新RTT估算
        /// </summary>
        private void UpdateRTTEstimate()
        {
            // 这里需要与实际网络系统集成来获取真实RTT
            // 临时使用固定值
            m_averageRTT = 0.05f; // 50ms
        }

        /// <summary>
        /// 绘制预测调试信息
        /// </summary>
        private void DrawPredictionDebug(PredictedInputState state)
        {
            // 绘制预测轨迹
            Debug.DrawRay(state.leftHandPosition, state.predictedVelocity * 0.1f, Color.green, 0.1f);
            Debug.DrawRay(state.rightHandPosition, state.predictedVelocity * 0.1f, Color.blue, 0.1f);
        }

        /// <summary>
        /// 获取网络预测统计
        /// </summary>
        public NetworkPredictionStats GetNetworkStats()
        {
            return new NetworkPredictionStats
            {
                averageRTT = m_averageRTT,
                totalPredictions = m_totalPredictions,
                correctPredictions = m_correctPredictions,
                predictionAccuracy = m_totalPredictions > 0 ? (float)m_correctPredictions / m_totalPredictions : 0f,
                rollbackCount = m_rollbackCount,
                averagePredictionError = m_averagePredictionError,
                bufferedInputs = m_inputBuffer.Count
            };
        }

        private void OnGUI()
        {
            if (!m_showDebugInfo) return;

            var stats = GetNetworkStats();
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };

            string debugText = $"=== 网络输入预测器 ===\n" +
                             $"平均RTT: {stats.averageRTT*1000:F1} ms\n" +
                             $"预测总数: {stats.totalPredictions}\n" +
                             $"正确预测: {stats.correctPredictions}\n" +
                             $"预测准确率: {stats.predictionAccuracy*100:F1}%\n" +
                             $"回滚次数: {stats.rollbackCount}\n" +
                             $"平均误差: {stats.averagePredictionError*1000:F2} mm\n" +
                             $"缓冲输入: {stats.bufferedInputs}\n" +
                             $"序列号: {m_inputSequenceNumber}";

            GUI.Box(new Rect(530, 10, 250, 200), debugText, style);
        }
    }
}