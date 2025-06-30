using UnityEngine;
using System;

namespace PongHub.Gameplay.Serve
{
    /// <summary>
    /// 发球状态枚举
    /// </summary>
    public enum ServeState
    {
        WaitingForServer,       // 等待发球方准备
        BallGenerated,          // 球已生成在手中
        ServingMotion,          // 发球动作中
        BallReleased,           // 球已释放
        ServeValidation,        // 发球有效性检查
        ServeComplete,          // 发球完成
        ServeFault              // 发球失误
    }

    /// <summary>
    /// 发球失误类型
    /// </summary>
    public enum ServeFaultType
    {
        None,                   // 无失误
        ThrowTooLow,           // 抛球过低
        ThrowAngleTooLarge,    // 抛球角度过大
        ServeTimeout,          // 发球超时
        BallNotHitCorrectly,   // 球未正确击打
        NetFault,              // 触网
        OutOfBounds            // 出界
    }

    /// <summary>
    /// 发球验证结果
    /// </summary>
    [Serializable]
    public struct ServeValidationResult
    {
        public bool IsValid;
        public ServeFaultType FaultType;
        public string FaultDescription;
        public Vector3 ThrowPosition;
        public Vector3 ThrowVelocity;
        public float ThrowHeight;
        public float ThrowAngle;

        public ServeValidationResult(bool isValid, ServeFaultType faultType = ServeFaultType.None, string description = "")
        {
            IsValid = isValid;
            FaultType = faultType;
            FaultDescription = description;
            ThrowPosition = Vector3.zero;
            ThrowVelocity = Vector3.zero;
            ThrowHeight = 0f;
            ThrowAngle = 0f;
        }
    }

    /// <summary>
    /// 乒乓球发球规则验证器
    /// 根据国际乒乓球联合会(ITTF)规则验证发球动作
    /// </summary>
    public class ServeValidator : MonoBehaviour
    {
        #region 发球规则设置
        [Header("发球规则")]
        [SerializeField] private float minThrowHeight = 0.16f;        // 最小抛球高度16cm (ITTF规则)
        [SerializeField] private float maxThrowAngle = 30f;           // 最大偏离垂直角度
        [SerializeField] private float throwValidationTime = 2f;      // 抛球动作验证时间
        [SerializeField] private float serveTimeLimit = 30f;          // 发球时间限制
        [SerializeField] private float ballReleaseDetectionRadius = 0.05f; // 球释放检测半径

        [Header("物理验证")]
        [SerializeField] private LayerMask tableLayerMask = 1 << 8;   // 球台层级
        [SerializeField] private LayerMask netLayerMask = 1 << 9;     // 球网层级
        [SerializeField] private float gravityAcceleration = 9.81f;   // 重力加速度

        [Header("调试设置")]
        [SerializeField] private bool enableDebugVisualization = true;
        [SerializeField] private bool logValidationDetails = true;
        #endregion

        #region 事件系统
        public static event Action<ServeState> OnServeStateChanged;
        public static event Action<ServeValidationResult> OnServeValidated;
        public static event Action<ServeFaultType> OnServeFault;
        #endregion

        #region 私有变量
        private ServeState currentServeState = ServeState.WaitingForServer;
        private Vector3 ballInitialPosition;
        private Vector3 ballReleasePosition;
        private Vector3 ballReleaseVelocity;
        private float serveStartTime;
        private float ballReleaseTime;
        private bool isValidatingServe;
        #endregion

        #region 属性
        public ServeState CurrentServeState => currentServeState;
        public float MinThrowHeight => minThrowHeight;
        public float MaxThrowAngle => maxThrowAngle;
        public bool IsValidatingServe => isValidatingServe;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeValidator();
        }

        private void Update()
        {
            UpdateServeValidation();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化验证器
        /// </summary>
        private void InitializeValidator()
        {
            ResetServeState();
            Debug.Log("[ServeValidator] 发球验证器初始化完成");
        }

        /// <summary>
        /// 重置发球状态
        /// </summary>
        private void ResetServeState()
        {
            currentServeState = ServeState.WaitingForServer;
            isValidatingServe = false;
            serveStartTime = 0f;
            ballReleaseTime = 0f;
        }
        #endregion

        #region 发球状态管理
        /// <summary>
        /// 设置发球状态
        /// </summary>
        public void SetServeState(ServeState newState)
        {
            if (currentServeState == newState) return;

            if (logValidationDetails)
            {
                Debug.Log($"[ServeValidator] 发球状态转换: {currentServeState} -> {newState}");
            }

            currentServeState = newState;
            OnServeStateChanged?.Invoke(newState);

            HandleServeStateChange(newState);
        }

        /// <summary>
        /// 处理发球状态变化
        /// </summary>
        private void HandleServeStateChange(ServeState state)
        {
            switch (state)
            {
                case ServeState.WaitingForServer:
                    ResetServeState();
                    break;

                case ServeState.BallGenerated:
                    StartServeTimer();
                    break;

                case ServeState.ServingMotion:
                    isValidatingServe = true;
                    break;

                case ServeState.BallReleased:
                    RecordBallRelease();
                    break;

                case ServeState.ServeValidation:
                    ValidateServeAction();
                    break;

                case ServeState.ServeComplete:
                case ServeState.ServeFault:
                    CompleteServeValidation();
                    break;
            }
        }

        /// <summary>
        /// 更新发球验证
        /// </summary>
        private void UpdateServeValidation()
        {
            if (!isValidatingServe) return;

            // 检查发球超时
            if (currentServeState == ServeState.BallGenerated || currentServeState == ServeState.ServingMotion)
            {
                if (Time.time - serveStartTime > serveTimeLimit)
                {
                    HandleServeFault(ServeFaultType.ServeTimeout, "发球超时");
                    return;
                }
            }
        }
        #endregion

        #region 发球验证核心逻辑
        /// <summary>
        /// 开始发球计时
        /// </summary>
        private void StartServeTimer()
        {
            serveStartTime = Time.time;
            if (logValidationDetails)
            {
                Debug.Log("[ServeValidator] 开始发球计时");
            }
        }

        /// <summary>
        /// 记录球的释放
        /// </summary>
        private void RecordBallRelease()
        {
            ballReleaseTime = Time.time;
            // 这里应该从球的物理组件获取实际数据
            // 暂时使用示例数据
            ballReleasePosition = ballInitialPosition + Vector3.up * 0.2f;
            ballReleaseVelocity = Vector3.up * 2f; // 示例速度

            if (logValidationDetails)
            {
                Debug.Log($"[ServeValidator] 记录球释放 - 位置: {ballReleasePosition}, 速度: {ballReleaseVelocity}");
            }

            SetServeState(ServeState.ServeValidation);
        }

        /// <summary>
        /// 验证发球动作
        /// </summary>
        private void ValidateServeAction()
        {
            var result = ValidateServe(ballReleasePosition, ballReleaseVelocity);

            OnServeValidated?.Invoke(result);

            if (result.IsValid)
            {
                SetServeState(ServeState.ServeComplete);
                if (logValidationDetails)
                {
                    Debug.Log("[ServeValidator] 发球有效");
                }
            }
            else
            {
                HandleServeFault(result.FaultType, result.FaultDescription);
            }
        }

        /// <summary>
        /// 验证发球规则
        /// </summary>
        public ServeValidationResult ValidateServe(Vector3 releasePosition, Vector3 releaseVelocity)
        {
            var result = new ServeValidationResult(true);
            result.ThrowPosition = releasePosition;
            result.ThrowVelocity = releaseVelocity;

            // 1. 检查抛球高度
            float throwHeight = CalculateMaxThrowHeight(releasePosition, releaseVelocity);
            result.ThrowHeight = throwHeight;

            if (throwHeight < minThrowHeight)
            {
                return new ServeValidationResult(false, ServeFaultType.ThrowTooLow,
                    $"抛球高度不足: {throwHeight:F3}m < {minThrowHeight:F3}m");
            }

            // 2. 检查抛球角度
            float throwAngle = Vector3.Angle(Vector3.up, releaseVelocity);
            result.ThrowAngle = throwAngle;

            if (throwAngle > maxThrowAngle)
            {
                return new ServeValidationResult(false, ServeFaultType.ThrowAngleTooLarge,
                    $"抛球角度过大: {throwAngle:F1}° > {maxThrowAngle:F1}°");
            }

            // 3. 检查抛球是否垂直向上（ITTF规则要求近似垂直）
            if (throwAngle > 20f) // 允许20度的偏差
            {
                return new ServeValidationResult(false, ServeFaultType.ThrowAngleTooLarge,
                    $"抛球必须接近垂直向上，当前角度: {throwAngle:F1}°");
            }

            if (logValidationDetails)
            {
                Debug.Log($"[ServeValidator] 发球验证通过 - 高度: {throwHeight:F3}m, 角度: {throwAngle:F1}°");
            }

            return result;
        }

        /// <summary>
        /// 计算抛球最大高度
        /// </summary>
        private float CalculateMaxThrowHeight(Vector3 initialPosition, Vector3 initialVelocity)
        {
            // 使用物理公式计算最大高度: h = v²/(2g)
            float verticalVelocity = initialVelocity.y;
            if (verticalVelocity <= 0) return 0f;

            float maxHeight = (verticalVelocity * verticalVelocity) / (2f * gravityAcceleration);
            return maxHeight;
        }

        /// <summary>
        /// 处理发球失误
        /// </summary>
        private void HandleServeFault(ServeFaultType faultType, string description)
        {
            SetServeState(ServeState.ServeFault);
            OnServeFault?.Invoke(faultType);

            if (logValidationDetails)
            {
                Debug.LogWarning($"[ServeValidator] 发球失误: {faultType} - {description}");
            }
        }

        /// <summary>
        /// 完成发球验证
        /// </summary>
        private void CompleteServeValidation()
        {
            isValidatingServe = false;

            if (logValidationDetails)
            {
                float totalServeTime = Time.time - serveStartTime;
                Debug.Log($"[ServeValidator] 发球验证完成，总用时: {totalServeTime:F2}秒");
            }
        }
        #endregion

        #region 外部接口
        /// <summary>
        /// 开始新的发球
        /// </summary>
        public void StartNewServe(Vector3 ballPosition)
        {
            ballInitialPosition = ballPosition;
            SetServeState(ServeState.BallGenerated);
        }

        /// <summary>
        /// 球开始运动（发球动作开始）
        /// </summary>
        public void OnBallMotionStarted()
        {
            if (currentServeState == ServeState.BallGenerated)
            {
                SetServeState(ServeState.ServingMotion);
            }
        }

        /// <summary>
        /// 球被释放
        /// </summary>
        public void OnBallReleased(Vector3 position, Vector3 velocity)
        {
            if (currentServeState == ServeState.ServingMotion)
            {
                ballReleasePosition = position;
                ballReleaseVelocity = velocity;
                SetServeState(ServeState.BallReleased);
            }
        }

        /// <summary>
        /// 强制重置发球状态
        /// </summary>
        public void ForceResetServe()
        {
            ResetServeState();
            SetServeState(ServeState.WaitingForServer);
        }

        /// <summary>
        /// 设置发球规则参数
        /// </summary>
        public void SetServeRules(float minHeight, float maxAngle, float timeLimit)
        {
            minThrowHeight = minHeight;
            maxThrowAngle = maxAngle;
            serveTimeLimit = timeLimit;

            if (logValidationDetails)
            {
                Debug.Log($"[ServeValidator] 发球规则更新 - 最小高度: {minHeight}m, 最大角度: {maxAngle}°, 时间限制: {timeLimit}s");
            }
        }
        #endregion

        #region 调试和可视化
        private void OnDrawGizmos()
        {
            if (!enableDebugVisualization) return;

            // 绘制抛球轨迹预测
            if (isValidatingServe && ballReleaseVelocity != Vector3.zero)
            {
                DrawThrowTrajectory(ballReleasePosition, ballReleaseVelocity);
            }

            // 绘制最小抛球高度线
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Gizmos.DrawWireCube(center + Vector3.up * minThrowHeight, new Vector3(0.5f, 0.01f, 0.5f));
        }

        /// <summary>
        /// 绘制抛球轨迹
        /// </summary>
        private void DrawThrowTrajectory(Vector3 startPos, Vector3 velocity)
        {
            Gizmos.color = Color.green;
            Vector3 currentPos = startPos;
            Vector3 currentVel = velocity;
            float timeStep = 0.1f;

            for (int i = 0; i < 20; i++)
            {
                Vector3 nextPos = currentPos + currentVel * timeStep;
                currentVel.y -= gravityAcceleration * timeStep;

                Gizmos.DrawLine(currentPos, nextPos);
                currentPos = nextPos;

                if (currentPos.y < startPos.y) break; // 停止在原始高度以下
            }
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"发球状态: {currentServeState}\n" +
                   $"验证中: {isValidatingServe}\n" +
                   $"最小抛球高度: {minThrowHeight:F3}m\n" +
                   $"最大抛球角度: {maxThrowAngle:F1}°\n" +
                   $"发球时间限制: {serveTimeLimit:F1}s";
        }
        #endregion
    }
}