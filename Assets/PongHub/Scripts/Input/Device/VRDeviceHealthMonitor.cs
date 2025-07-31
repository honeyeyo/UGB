using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.XR.Oculus;

namespace PongHub.Input.Device
{
    /// <summary>
    /// VR设备健康监控器 - Epic-3设备管理核心组件
    /// 功能：监控VR设备状态，处理热插拔，提供设备诊断和自动恢复
    /// 目标：生产级VR设备稳定性，无缝的设备连接体验
    /// </summary>
    public class VRDeviceHealthMonitor : MonoBehaviour
    {
        [Header("监控配置")]
        [SerializeField]
        [Tooltip("Health Check Interval / 健康检查间隔 - Interval between device health checks (seconds)")]
        private float m_healthCheckInterval = 1.0f;

        [SerializeField]
        [Tooltip("Connection Timeout / 连接超时 - Timeout for device connection attempts (seconds)")]
        private float m_connectionTimeout = 5.0f;

        [SerializeField]
        [Tooltip("Max Retry Attempts / 最大重试次数 - Maximum number of retry attempts for device connection")]
        private int m_maxRetryAttempts = 3;

        [Header("设备阈值")]
        [SerializeField]
        [Tooltip("Battery Warning Level / 电池警告级别 - Battery level to show warning (0-1)")]
        private float m_batteryWarningLevel = 0.2f;

        [SerializeField]
        [Tooltip("Temperature Warning Level / 温度警告级别 - Temperature level to show warning (Celsius)")]
        private float m_temperatureWarningLevel = 45f;

        [SerializeField]
        [Tooltip("Tracking Loss Threshold / 跟踪丢失阈值 - Time without tracking before warning (seconds)")]
        private float m_trackingLossThreshold = 2.0f;

        [Header("自动恢复设置")]
        [SerializeField]
        [Tooltip("Enable Auto Recovery / 启用自动恢复 - Whether to enable automatic device recovery")]
        private bool m_enableAutoRecovery = true;

        [SerializeField]
        [Tooltip("Recovery Delay / 恢复延迟 - Delay before attempting recovery (seconds)")]
        private float m_recoveryDelay = 1.0f;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        [SerializeField]
        [Tooltip("Enable Device Logging / 启用设备日志 - Whether to enable detailed device logging")]
        private bool m_enableDeviceLogging = true;

        // 设备状态跟踪
        private Dictionary<XRNode, DeviceHealthStatus> m_deviceStates = new Dictionary<XRNode, DeviceHealthStatus>();
        private Dictionary<XRNode, float> m_lastTrackingTime = new Dictionary<XRNode, float>();
        private Dictionary<XRNode, int> m_retryAttempts = new Dictionary<XRNode, int>();
        
        // 监控协程
        private Coroutine m_healthCheckCoroutine;
        private Coroutine m_batteryMonitorCoroutine;
        
        // 统计数据
        private int m_totalDisconnections = 0;
        private int m_successfulRecoveries = 0;
        private float m_totalDowntime = 0f;
        private List<DeviceEvent> m_deviceEvents = new List<DeviceEvent>();

        // 事件系统
        public System.Action<XRNode, DeviceHealthStatus> OnDeviceStatusChanged;
        public System.Action<XRNode, string> OnDeviceWarning;
        public System.Action<XRNode> OnDeviceDisconnected;
        public System.Action<XRNode> OnDeviceReconnected;
        public System.Action<DeviceDiagnostics> OnDiagnosticsUpdated;

        /// <summary>
        /// 设备健康状态枚举
        /// </summary>
        public enum DeviceHealthStatus
        {
            Unknown,        // 未知状态
            Healthy,        // 健康
            Warning,        // 警告（低电量、高温等）
            Disconnected,   // 断开连接
            Reconnecting,   // 重连中
            Failed          // 故障
        }

        /// <summary>
        /// 设备事件结构
        /// </summary>
        [System.Serializable]
        public struct DeviceEvent
        {
            public float timestamp;
            public XRNode device;
            public DeviceHealthStatus status;
            public string message;
        }

        /// <summary>
        /// 设备诊断信息结构
        /// </summary>
        [System.Serializable]
        public struct DeviceDiagnostics
        {
            public int connectedDevices;
            public int healthyDevices;
            public int warningDevices;
            public int disconnectedDevices;
            public float averageBatteryLevel;
            public float averageTemperature;
            public int totalDisconnections;
            public int successfulRecoveries;
            public float totalDowntime;
            public float recoverySuccessRate;
        }

        private void Awake()
        {
            InitializeDeviceTracking();
        }

        private void Start()
        {
            StartHealthMonitoring();
            
            if (m_showDebugInfo)
            {
                Debug.Log("[VRDeviceHealthMonitor] VR设备健康监控器启动");
            }
        }

        private void OnDestroy()
        {
            StopHealthMonitoring();
        }

        /// <summary>
        /// 初始化设备跟踪
        /// </summary>
        private void InitializeDeviceTracking()
        {
            // 初始化主要VR设备节点
            XRNode[] importantNodes = {
                XRNode.Head,
                XRNode.LeftHand,
                XRNode.RightHand,
                XRNode.CenterEye
            };

            foreach (var node in importantNodes)
            {
                m_deviceStates[node] = DeviceHealthStatus.Unknown;
                m_lastTrackingTime[node] = Time.unscaledTime;
                m_retryAttempts[node] = 0;
            }
        }

        /// <summary>
        /// 开始健康监控
        /// </summary>
        private void StartHealthMonitoring()
        {
            if (m_healthCheckCoroutine == null)
            {
                m_healthCheckCoroutine = StartCoroutine(HealthCheckLoop());
            }
            
            if (m_batteryMonitorCoroutine == null)
            {
                m_batteryMonitorCoroutine = StartCoroutine(BatteryMonitorLoop());
            }
        }

        /// <summary>
        /// 停止健康监控
        /// </summary>
        private void StopHealthMonitoring()
        {
            if (m_healthCheckCoroutine != null)
            {
                StopCoroutine(m_healthCheckCoroutine);
                m_healthCheckCoroutine = null;
            }
            
            if (m_batteryMonitorCoroutine != null)
            {
                StopCoroutine(m_batteryMonitorCoroutine);
                m_batteryMonitorCoroutine = null;
            }
        }

        /// <summary>
        /// 健康检查循环
        /// </summary>
        private IEnumerator HealthCheckLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_healthCheckInterval);
                
                PerformHealthCheck();
            }
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        private void PerformHealthCheck()
        {
            foreach (var kvp in m_deviceStates)
            {
                XRNode node = kvp.Key;
                CheckDeviceHealth(node);
            }
            
            // 更新诊断信息
            var diagnostics = GenerateDiagnostics();
            OnDiagnosticsUpdated?.Invoke(diagnostics);
        }

        /// <summary>
        /// 检查单个设备健康状态
        /// </summary>
        private void CheckDeviceHealth(XRNode node)
        {
            var currentStatus = m_deviceStates[node];
            var newStatus = EvaluateDeviceHealth(node);
            
            // 状态变化处理
            if (newStatus != currentStatus)
            {
                HandleStatusChange(node, currentStatus, newStatus);
            }
            
            m_deviceStates[node] = newStatus;
        }

        /// <summary>
        /// 评估设备健康状态
        /// </summary>
        private DeviceHealthStatus EvaluateDeviceHealth(XRNode node)
        {
            // 检查设备连接状态
            if (!IsDeviceConnected(node))
            {
                return DeviceHealthStatus.Disconnected;
            }
            
            // 检查跟踪状态
            if (!IsDeviceTracked(node))
            {
                float timeSinceTracking = Time.unscaledTime - m_lastTrackingTime[node];
                if (timeSinceTracking > m_trackingLossThreshold)
                {
                    return DeviceHealthStatus.Warning;
                }
            }
            else
            {
                m_lastTrackingTime[node] = Time.unscaledTime;
            }
            
            // 检查电池电量
            float batteryLevel = GetDeviceBatteryLevel(node);
            if (batteryLevel > 0 && batteryLevel < m_batteryWarningLevel)
            {
                return DeviceHealthStatus.Warning;
            }
            
            // 检查设备温度
            float temperature = GetDeviceTemperature(node);
            if (temperature > m_temperatureWarningLevel)
            {
                return DeviceHealthStatus.Warning;
            }
            
            return DeviceHealthStatus.Healthy;
        }

        /// <summary>
        /// 检查设备是否连接
        /// </summary>
        private bool IsDeviceConnected(XRNode node)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, inputDevices);
            return inputDevices.Count > 0 && inputDevices[0].isValid;
        }

        /// <summary>
        /// 检查设备是否被跟踪
        /// </summary>
        private bool IsDeviceTracked(XRNode node)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                var device = inputDevices[0];
                if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked))
                {
                    return isTracked;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 获取设备电池电量
        /// </summary>
        private float GetDeviceBatteryLevel(XRNode node)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                var device = inputDevices[0];
                if (device.TryGetFeatureValue(CommonUsages.batteryLevel, out float batteryLevel))
                {
                    return batteryLevel;
                }
            }
            
            return -1f; // 未知电量
        }

        /// <summary>
        /// 获取设备温度（模拟实现）
        /// </summary>
        private float GetDeviceTemperature(XRNode node)
        {
            // 在实际项目中，这需要与设备厂商的SDK集成
            // 这里返回模拟温度
            return 35f + Random.Range(-5f, 15f);
        }

        /// <summary>
        /// 处理状态变化
        /// </summary>
        private void HandleStatusChange(XRNode node, DeviceHealthStatus oldStatus, DeviceHealthStatus newStatus)
        {
            // 记录事件
            var deviceEvent = new DeviceEvent
            {
                timestamp = Time.unscaledTime,
                device = node,
                status = newStatus,
                message = $"状态从 {oldStatus} 变更为 {newStatus}"
            };
            
            m_deviceEvents.Add(deviceEvent);
            
            // 限制事件历史大小
            if (m_deviceEvents.Count > 100)
            {
                m_deviceEvents.RemoveAt(0);
            }
            
            // 触发事件
            OnDeviceStatusChanged?.Invoke(node, newStatus);
            
            // 处理特定状态变化
            switch (newStatus)
            {
                case DeviceHealthStatus.Disconnected:
                    HandleDeviceDisconnection(node);
                    break;
                    
                case DeviceHealthStatus.Healthy:
                    if (oldStatus == DeviceHealthStatus.Disconnected || oldStatus == DeviceHealthStatus.Reconnecting)
                    {
                        HandleDeviceReconnection(node);
                    }
                    break;
                    
                case DeviceHealthStatus.Warning:
                    HandleDeviceWarning(node);
                    break;
            }
            
            if (m_enableDeviceLogging)
            {
                Debug.Log($"[VRDeviceHealthMonitor] 设备 {node} 状态变更: {oldStatus} → {newStatus}");
            }
        }

        /// <summary>
        /// 处理设备断开连接
        /// </summary>
        private void HandleDeviceDisconnection(XRNode node)
        {
            m_totalDisconnections++;
            OnDeviceDisconnected?.Invoke(node);
            
            if (m_enableAutoRecovery)
            {
                StartCoroutine(AttemptDeviceRecovery(node));
            }
            
            string warning = $"设备 {node} 已断开连接";
            OnDeviceWarning?.Invoke(node, warning);
        }

        /// <summary>
        /// 处理设备重新连接
        /// </summary>
        private void HandleDeviceReconnection(XRNode node)
        {
            m_successfulRecoveries++;
            m_retryAttempts[node] = 0; // 重置重试计数
            OnDeviceReconnected?.Invoke(node);
            
            if (m_enableDeviceLogging)
            {
                Debug.Log($"[VRDeviceHealthMonitor] 设备 {node} 已重新连接");
            }
        }

        /// <summary>
        /// 处理设备警告
        /// </summary>
        private void HandleDeviceWarning(XRNode node)
        {
            string warningMessage = GenerateWarningMessage(node);
            OnDeviceWarning?.Invoke(node, warningMessage);
        }

        /// <summary>
        /// 生成警告消息
        /// </summary>
        private string GenerateWarningMessage(XRNode node)
        {
            var warnings = new List<string>();
            
            // 检查电池电量
            float batteryLevel = GetDeviceBatteryLevel(node);
            if (batteryLevel > 0 && batteryLevel < m_batteryWarningLevel)
            {
                warnings.Add($"电池电量低: {batteryLevel*100:F0}%");
            }
            
            // 检查跟踪状态
            if (!IsDeviceTracked(node))
            {
                warnings.Add("跟踪丢失");
            }
            
            // 检查温度
            float temperature = GetDeviceTemperature(node);
            if (temperature > m_temperatureWarningLevel)
            {
                warnings.Add($"设备温度过高: {temperature:F1}°C");
            }
            
            return warnings.Count > 0 ? string.Join(", ", warnings) : "设备状态异常";
        }

        /// <summary>
        /// 尝试设备恢复
        /// </summary>
        private IEnumerator AttemptDeviceRecovery(XRNode node)
        {
            if (m_retryAttempts[node] >= m_maxRetryAttempts)
            {
                if (m_enableDeviceLogging)
                {
                    Debug.LogError($"[VRDeviceHealthMonitor] 设备 {node} 恢复失败，已达到最大重试次数");
                }
                m_deviceStates[node] = DeviceHealthStatus.Failed;
                yield break;
            }
            
            yield return new WaitForSeconds(m_recoveryDelay);
            
            m_deviceStates[node] = DeviceHealthStatus.Reconnecting;
            m_retryAttempts[node]++;
            
            if (m_enableDeviceLogging)
            {
                Debug.Log($"[VRDeviceHealthMonitor] 尝试恢复设备 {node} (第{m_retryAttempts[node]}次)");
            }
            
            // 尝试重新初始化设备
            yield return StartCoroutine(ReconnectDevice(node));
        }

        /// <summary>
        /// 重新连接设备
        /// </summary>
        private IEnumerator ReconnectDevice(XRNode node)
        {
            float startTime = Time.unscaledTime;
            
            // 等待设备重新连接或超时
            while (Time.unscaledTime - startTime < m_connectionTimeout)
            {
                if (IsDeviceConnected(node))
                {
                    if (m_enableDeviceLogging)
                    {
                        Debug.Log($"[VRDeviceHealthMonitor] 设备 {node} 恢复成功");
                    }
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // 连接超时，稍后重试
            StartCoroutine(AttemptDeviceRecovery(node));
        }

        /// <summary>
        /// 电池监控循环
        /// </summary>
        private IEnumerator BatteryMonitorLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f); // 每30秒检查一次电池
                
                MonitorBatteryLevels();
            }
        }

        /// <summary>
        /// 监控电池电量
        /// </summary>
        private void MonitorBatteryLevels()
        {
            foreach (var kvp in m_deviceStates)
            {
                XRNode node = kvp.Key;
                float batteryLevel = GetDeviceBatteryLevel(node);
                
                if (batteryLevel > 0 && batteryLevel < m_batteryWarningLevel)
                {
                    string warning = $"设备 {node} 电池电量低: {batteryLevel*100:F0}%";
                    OnDeviceWarning?.Invoke(node, warning);
                }
            }
        }

        /// <summary>
        /// 生成诊断信息
        /// </summary>
        private DeviceDiagnostics GenerateDiagnostics()
        {
            int connected = 0, healthy = 0, warning = 0, disconnected = 0;
            float totalBattery = 0f;
            float totalTemperature = 0f;
            int batteryCount = 0;
            
            foreach (var kvp in m_deviceStates)
            {
                switch (kvp.Value)
                {
                    case DeviceHealthStatus.Healthy:
                        healthy++;
                        connected++;
                        break;
                    case DeviceHealthStatus.Warning:
                        warning++;
                        connected++;
                        break;
                    case DeviceHealthStatus.Disconnected:
                    case DeviceHealthStatus.Failed:
                        disconnected++;
                        break;
                    case DeviceHealthStatus.Reconnecting:
                        connected++;
                        break;
                }
                
                float battery = GetDeviceBatteryLevel(kvp.Key);
                if (battery > 0)
                {
                    totalBattery += battery;
                    batteryCount++;
                }
                
                totalTemperature += GetDeviceTemperature(kvp.Key);
            }
            
            return new DeviceDiagnostics
            {
                connectedDevices = connected,
                healthyDevices = healthy,
                warningDevices = warning,
                disconnectedDevices = disconnected,
                averageBatteryLevel = batteryCount > 0 ? totalBattery / batteryCount : 0f,
                averageTemperature = m_deviceStates.Count > 0 ? totalTemperature / m_deviceStates.Count : 0f,
                totalDisconnections = m_totalDisconnections,
                successfulRecoveries = m_successfulRecoveries,
                totalDowntime = m_totalDowntime,
                recoverySuccessRate = m_totalDisconnections > 0 ? (float)m_successfulRecoveries / m_totalDisconnections : 1f
            };
        }

        /// <summary>
        /// 强制重新检查所有设备
        /// </summary>
        public void ForceHealthCheck()
        {
            PerformHealthCheck();
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStatistics()
        {
            m_totalDisconnections = 0;
            m_successfulRecoveries = 0;
            m_totalDowntime = 0f;
            m_deviceEvents.Clear();
            
            foreach (var node in m_retryAttempts.Keys.ToList())
            {
                m_retryAttempts[node] = 0;
            }
        }

        /// <summary>
        /// 获取设备事件历史
        /// </summary>
        public List<DeviceEvent> GetDeviceEventHistory()
        {
            return new List<DeviceEvent>(m_deviceEvents);
        }

        /// <summary>
        /// 获取设备诊断信息 - 测试用方法
        /// </summary>
        public DeviceDiagnostics GetDeviceDiagnostics()
        {
            return GenerateDiagnostics();
        }

        private void OnGUI()
        {
            if (!m_showDebugInfo) return;

            var diagnostics = GenerateDiagnostics();
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };

            string debugText = $"=== VR设备健康监控器 ===\n" +
                             $"已连接设备: {diagnostics.connectedDevices}\n" +
                             $"健康设备: {diagnostics.healthyDevices}\n" +
                             $"警告设备: {diagnostics.warningDevices}\n" +
                             $"断开设备: {diagnostics.disconnectedDevices}\n" +
                             $"平均电量: {diagnostics.averageBatteryLevel*100:F0}%\n" +
                             $"平均温度: {diagnostics.averageTemperature:F1}°C\n" +
                             $"断开次数: {diagnostics.totalDisconnections}\n" +
                             $"恢复次数: {diagnostics.successfulRecoveries}\n" +
                             $"恢复成功率: {diagnostics.recoverySuccessRate*100:F1}%";

            GUI.Box(new Rect(790, 10, 250, 220), debugText, style);
        }
    }
}