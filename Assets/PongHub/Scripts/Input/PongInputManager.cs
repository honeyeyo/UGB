using UnityEngine;
using UnityEngine.XR;
using Meta.Utilities.Input;
using System;
using System.Collections;

/// <summary>
/// PongInputManager - 优化版本
/// 解决性能问题，引入事件系统，改善代码架构
/// </summary>
public class PongInputManager : MonoBehaviour
{
    // 单例实例
    public static PongInputManager Instance { get; private set; }

    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float heightChangeSpeed = 1f;
    [SerializeField] private float teleportDistance = 5f;

    [Header("瞬移设置")]
    [SerializeField] private Transform[] teleportPoints;
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float metaKeyHoldTime = 2f;

    [Header("球拍设置")]
    [SerializeField] private Transform paddleAttachPoint;
    [SerializeField] private GameObject paddlePrefab;
    [SerializeField] private float gripHoldTime = 1f;

    [Header("球生成设置")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform ballSpawnPoint;

    [Header("组件引用")]
    [SerializeField] private XRInputManager xrInputManager;
    [SerializeField] private Transform playerRig;
    [SerializeField] private Transform cameraRig;
    [SerializeField] private PaddleConfigurationManager paddleConfigManager;

    [Header("输入设置")]
    [SerializeField] private float inputCheckInterval = 0.016f; // 60fps
    [SerializeField] private float deadZone = 0.1f;

    // 事件定义
    public static event Action<bool> OnPaddleGrabbed;
    public static event Action OnPaddleReleased;
    public static event Action<bool> OnBallGenerated;
    public static event Action OnTeleportPerformed;

    // 缓存的Actions句柄（获取一次，持续有效）
    private XRInputControlActions.Controller leftActions;
    private XRInputControlActions.Controller rightActions;

    // 输入状态缓存
    private InputState currentInputState = new InputState();
    private InputState previousInputState = new InputState();

    // 状态管理
    private bool isPaddleHeld = false;
    private bool isLeftHandHoldingPaddle = false;
    private GameObject currentPaddle;

    // 计时器
    private float metaKeyHoldTimer = 0f;
    private float gripHoldTimer = 0f;
    private float lastInputCheckTime = 0f;

    // 输入状态结构体
    [System.Serializable]
    public struct InputState
    {
        public Vector2 leftStick;
        public Vector2 rightStick;
        public bool leftButtonA;
        public bool leftButtonB;
        public bool leftButtonMeta;
        public bool rightButtonA;
        public bool rightButtonB;
        public bool rightButtonMeta;
        public float leftGrip;
        public float rightGrip;
        public float leftTrigger;
        public float rightTrigger;
        public bool leftAB; // A+B同时按下
        public bool rightAB;
    }

    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 获取输入句柄（一次获取，持续读取实时状态）
        leftActions = xrInputManager.GetActions(true);
        rightActions = xrInputManager.GetActions(false);

        Debug.Log("PongInputManager 已初始化");
    }

    private void Update()
    {
        // 按固定间隔检查输入（减少不必要的检查）
        if (Time.time - lastInputCheckTime >= inputCheckInterval)
        {
            UpdateInputState();
            ProcessInputEvents();
            lastInputCheckTime = Time.time;
        }

        // 连续输入处理（需要高频率更新的输入）
        HandleContinuousInput();
        UpdateTimers();
    }

    /// <summary>
    /// 更新输入状态
    /// </summary>
    private void UpdateInputState()
    {
        // 保存上一帧状态
        previousInputState = currentInputState;

        // 更新当前状态（支持反复按键操作）
        currentInputState.leftStick = leftActions.ButtonPrimaryThumbstick.action.ReadValue<Vector2>();
        currentInputState.rightStick = rightActions.ButtonPrimaryThumbstick.action.ReadValue<Vector2>();

        currentInputState.leftButtonA = leftActions.ButtonOne.action.ReadValue<float>() > 0.5f;
        currentInputState.leftButtonB = leftActions.ButtonTwo.action.ReadValue<float>() > 0.5f;
        currentInputState.leftButtonMeta = leftActions.ButtonThree.action.ReadValue<float>() > 0.5f;

        currentInputState.rightButtonA = rightActions.ButtonOne.action.ReadValue<float>() > 0.5f;
        currentInputState.rightButtonB = rightActions.ButtonTwo.action.ReadValue<float>() > 0.5f;
        currentInputState.rightButtonMeta = rightActions.ButtonThree.action.ReadValue<float>() > 0.5f;

        currentInputState.leftGrip = leftActions.AxisHandTrigger.action.ReadValue<float>();
        currentInputState.rightGrip = rightActions.AxisHandTrigger.action.ReadValue<float>();

        currentInputState.leftTrigger = leftActions.AxisIndexTrigger.action.ReadValue<float>();
        currentInputState.rightTrigger = rightActions.AxisIndexTrigger.action.ReadValue<float>();

        // 组合键检测
        currentInputState.leftAB = currentInputState.leftButtonA && currentInputState.leftButtonB;
        currentInputState.rightAB = currentInputState.rightButtonA && currentInputState.rightButtonB;
    }

    /// <summary>
    /// 处理输入事件（基于状态变化）
    /// </summary>
    private void ProcessInputEvents()
    {
        // A+B组合键事件（按下时触发）
        if (currentInputState.leftAB && !previousInputState.leftAB)
        {
            OnLeftABPressed();
        }
        if (currentInputState.rightAB && !previousInputState.rightAB)
        {
            OnRightABPressed();
        }

        // 摇杆前推瞬移检测
        if (currentInputState.rightStick.y > 0.8f && previousInputState.rightStick.y <= 0.8f)
        {
            PerformTeleport();
        }

        // Trigger生成球检测
        ProcessBallGenerationEvents();
    }

    /// <summary>
    /// 处理连续输入（需要高频率更新）
    /// </summary>
    private void HandleContinuousInput()
    {
        // 移动输入
        ProcessMovementInput();

        // Grip握持检测
        ProcessGripInput();
    }

    /// <summary>
    /// 更新计时器
    /// </summary>
    private void UpdateTimers()
    {
        // Meta键长按检测
        if (currentInputState.leftButtonMeta || currentInputState.rightButtonMeta)
        {
            metaKeyHoldTimer += Time.deltaTime;
            if (metaKeyHoldTimer >= metaKeyHoldTime)
            {
                TeleportToDefaultSpawn();
                metaKeyHoldTimer = 0f;
            }
        }
        else
        {
            metaKeyHoldTimer = 0f;
        }
    }

    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void ProcessMovementInput()
    {
        // 左手摇杆移动
        if (currentInputState.leftStick.magnitude > deadZone)
        {
            Vector3 moveDirection = cameraRig.forward * currentInputState.leftStick.y +
                                  cameraRig.right * currentInputState.leftStick.x;
            moveDirection.y = 0;
            playerRig.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 右手摇杆旋转
        if (Mathf.Abs(currentInputState.rightStick.x) > deadZone)
        {
            playerRig.Rotate(0, currentInputState.rightStick.x * rotationSpeed * Time.deltaTime, 0);
        }

        // 左手A、B键上下移动
        if (currentInputState.leftButtonA)
        {
            playerRig.position += Vector3.up * heightChangeSpeed * Time.deltaTime;
        }
        if (currentInputState.leftButtonB)
        {
            playerRig.position += Vector3.down * heightChangeSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 处理Grip输入
    /// </summary>
    private void ProcessGripInput()
    {
        bool anyGripPressed = currentInputState.leftGrip > 0.8f || currentInputState.rightGrip > 0.8f;

        if (anyGripPressed)
        {
            gripHoldTimer += Time.deltaTime;
            if (gripHoldTimer >= gripHoldTime)
            {
                if (!isPaddleHeld)
                {
                    GrabPaddle(currentInputState.leftGrip > 0.8f);
                }
                else
                {
                    ReleasePaddle();
                }
                gripHoldTimer = 0f;
            }
        }
        else
        {
            gripHoldTimer = 0f;
        }
    }

    /// <summary>
    /// 处理球生成和释放事件
    /// 改动点：
    /// 1. 支持球的生成和释放两种操作
    /// 2. 检查球的当前状态决定操作类型
    /// 3. 集成发球权限检查
    /// </summary>
    private void ProcessBallGenerationEvents()
    {
        if (!isPaddleHeld) return;

        bool leftTriggerPressed = currentInputState.leftTrigger > 0.8f && previousInputState.leftTrigger <= 0.8f;
        bool rightTriggerPressed = currentInputState.rightTrigger > 0.8f && previousInputState.rightTrigger <= 0.8f;

        // 非持拍手的Trigger操作
        if (isLeftHandHoldingPaddle && rightTriggerPressed)
        {
            HandleBallOperation(false); // 右手（非持拍手）
        }
        else if (!isLeftHandHoldingPaddle && leftTriggerPressed)
        {
            HandleBallOperation(true);  // 左手（非持拍手）
        }
    }

    /// <summary>
    /// 处理球操作（生成或释放）
    /// </summary>
    /// <param name="fromLeftHand">是否来自左手</param>
    private void HandleBallOperation(bool fromLeftHand)
    {
        // 查找当前附着的球
        var attachedBall = FindAttachedBall();

        if (attachedBall != null)
        {
            // 如果有球附着，则释放球
            ReleaseBall(attachedBall, fromLeftHand);
        }
        else
        {
            // 如果没有球附着，则尝试生成新球
            GenerateBall(fromLeftHand);
        }
    }

    /// <summary>
    /// 查找当前附着的球
    /// </summary>
    /// <returns>附着的球组件，如果没有则返回null</returns>
    private PongHub.Arena.Balls.PongBallNetworking FindAttachedBall()
    {
        var allBalls = FindObjectsOfType<PongHub.Arena.Balls.PongBallNetworking>();
        foreach (var ball in allBalls)
        {
            if (ball.IsAttached && ball.AttachedPlayerId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                return ball;
            }
        }
        return null;
    }

    /// <summary>
    /// 释放球
    /// </summary>
    /// <param name="ball">要释放的球</param>
    /// <param name="fromLeftHand">是否来自左手</param>
    private void ReleaseBall(PongHub.Arena.Balls.PongBallNetworking ball, bool fromLeftHand)
    {
        // 获取手部锚点
        Transform handAnchor = xrInputManager.GetAnchor(fromLeftHand);

        // 计算释放速度（基于手部移动）
        Vector3 releaseVelocity = CalculateReleaseVelocity(handAnchor, fromLeftHand);

        // 计算旋转参数
        Vector3 spinAxis = CalculateSpinAxis(handAnchor, releaseVelocity);
        float spinRate = CalculateSpinRate(releaseVelocity);

        // 释放球
        ball.ReleaseBall(releaseVelocity, spinAxis, spinRate);

        Debug.Log($"球已从{(fromLeftHand ? "左手" : "右手")}释放，速度: {releaseVelocity.magnitude:F2} m/s");
    }

    /// <summary>
    /// 计算释放速度
    /// </summary>
    /// <param name="handAnchor">手部锚点</param>
    /// <param name="fromLeftHand">是否来自左手</param>
    /// <returns>释放速度</returns>
    private Vector3 CalculateReleaseVelocity(Transform handAnchor, bool fromLeftHand)
    {
        // 基础发球速度
        Vector3 baseVelocity = handAnchor.forward * 5f; // 5 m/s 基础速度

        // 根据Trigger按压强度调整速度
        float triggerStrength = fromLeftHand ? currentInputState.leftTrigger : currentInputState.rightTrigger;
        float speedMultiplier = Mathf.Lerp(0.5f, 2f, triggerStrength); // 0.5x 到 2x 速度

        // 添加一些向上的分量使球有弧线
        Vector3 arcVelocity = Vector3.up * 1f;

        return (baseVelocity * speedMultiplier) + arcVelocity;
    }

    /// <summary>
    /// 计算旋转轴
    /// </summary>
    /// <param name="handAnchor">手部锚点</param>
    /// <param name="velocity">球速度</param>
    /// <returns>旋转轴</returns>
    private Vector3 CalculateSpinAxis(Transform handAnchor, Vector3 velocity)
    {
        // 简单的旋转轴计算：垂直于速度方向
        Vector3 spinAxis = Vector3.Cross(velocity.normalized, handAnchor.up).normalized;
        return spinAxis;
    }

    /// <summary>
    /// 计算旋转速率
    /// </summary>
    /// <param name="velocity">球速度</param>
    /// <returns>旋转速率</returns>
    private float CalculateSpinRate(Vector3 velocity)
    {
        // 根据球速度计算旋转强度
        return velocity.magnitude * 2f; // 速度越快，旋转越强
    }

    /// <summary>
    /// 左手A+B事件处理
    /// </summary>
    private void OnLeftABPressed()
    {
        if (!isPaddleHeld && paddleConfigManager != null)
        {
            paddleConfigManager.StartConfiguration(true);
            Debug.Log("开始左手配置模式");
        }
    }

    /// <summary>
    /// 右手A+B事件处理
    /// </summary>
    private void OnRightABPressed()
    {
        if (!isPaddleHeld && paddleConfigManager != null)
        {
            paddleConfigManager.StartConfiguration(false);
            Debug.Log("开始右手配置模式");
        }
    }

    /// <summary>
    /// 握持球拍
    /// </summary>
    private void GrabPaddle(bool useLeftHand)
    {
        if (isPaddleHeld) return;

        isPaddleHeld = true;
        isLeftHandHoldingPaddle = useLeftHand;

        currentPaddle = Instantiate(paddlePrefab);
        Transform handAnchor = xrInputManager.GetAnchor(useLeftHand);
        currentPaddle.transform.SetParent(handAnchor);

        if (paddleConfigManager != null)
        {
            paddleConfigManager.GetPaddleTransform(useLeftHand, out Vector3 configPos, out Vector3 configRot);
            currentPaddle.transform.localPosition = configPos;
            currentPaddle.transform.localRotation = Quaternion.Euler(configRot);
        }
        else
        {
            currentPaddle.transform.localPosition = Vector3.zero;
            currentPaddle.transform.localRotation = Quaternion.identity;
        }

        // 触发事件
        OnPaddleGrabbed?.Invoke(useLeftHand);
        Debug.Log($"球拍已握持到{(useLeftHand ? "左手" : "右手")}");
    }

    /// <summary>
    /// 释放球拍
    /// </summary>
    private void ReleasePaddle()
    {
        if (!isPaddleHeld) return;

        isPaddleHeld = false;

        if (currentPaddle != null)
        {
            Destroy(currentPaddle);
            currentPaddle = null;
        }

        // 触发事件
        OnPaddleReleased?.Invoke();
        Debug.Log("球拍已释放");
    }

    /// <summary>
    /// 生成球 - 集成乒乓球网络系统
    /// 改动点：
    /// 1. 使用网络球生成系统而非直接实例化
    /// 2. 检查发球权限
    /// 3. 球附着到非持拍手而非直接发射
    /// </summary>
    private void GenerateBall(bool fromLeftHand)
    {
        // 检查是否为非持拍手
        bool isNonPaddleHand = (isLeftHandHoldingPaddle && !fromLeftHand) ||
                              (!isLeftHandHoldingPaddle && fromLeftHand);

        if (!isNonPaddleHand)
        {
            Debug.LogWarning("只能从非持拍手生成球");
            return;
        }

        // 查找乒乓球网络组件并请求生成球
        var pongBallNetworking = FindObjectOfType<PongHub.Arena.Balls.PongBallNetworking>();
        if (pongBallNetworking != null)
        {
            // 使用网络系统生成球
            pongBallNetworking.RequestGenerateBallServerRpc();
            Debug.Log($"已请求从{(fromLeftHand ? "左手" : "右手")}(非持拍手)生成球");
        }
        else
        {
            // 回退到原始生成方式（用于兼容性）
            Transform handAnchor = xrInputManager.GetAnchor(fromLeftHand);
            Vector3 spawnPosition = handAnchor.position + handAnchor.forward * 0.05f; // 改为5cm偏移
            GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);

            // 附着到手部而非直接发射
            var ballAttachment = ball.GetComponent<PongHub.Arena.Balls.PongBallAttachment>();
            if (ballAttachment != null)
            {
                ballAttachment.AttachToNonPaddleHand(handAnchor);
            }

            Debug.Log($"使用回退方式生成球到{(fromLeftHand ? "左手" : "右手")}");
        }

        // 触发事件
        OnBallGenerated?.Invoke(fromLeftHand);
    }

    /// <summary>
    /// 执行瞬移
    /// </summary>
    private void PerformTeleport()
    {
        Vector3 teleportPosition = cameraRig.position + cameraRig.forward * teleportDistance;
        teleportPosition.y = playerRig.position.y;
        playerRig.position = teleportPosition;

        // 触发事件
        OnTeleportPerformed?.Invoke();
        Debug.Log("执行瞬移");
    }

    /// <summary>
    /// 瞬移到默认出生点
    /// </summary>
    private void TeleportToDefaultSpawn()
    {
        if (defaultSpawnPoint != null)
        {
            playerRig.position = defaultSpawnPoint.position;
            playerRig.rotation = defaultSpawnPoint.rotation;
            OnTeleportPerformed?.Invoke();
            Debug.Log("已瞬移到默认出生点");
        }
    }

    /// <summary>
    /// 瞬移到指定点位（供UI调用）
    /// </summary>
    public void TeleportToPoint(int pointIndex)
    {
        if (pointIndex >= 0 && pointIndex < teleportPoints.Length)
        {
            playerRig.position = teleportPoints[pointIndex].position;
            playerRig.rotation = teleportPoints[pointIndex].rotation;
            OnTeleportPerformed?.Invoke();
            Debug.Log($"已瞬移到点位 {pointIndex}");
        }
    }

    /// <summary>
    /// 外部调用：开始配置模式
    /// </summary>
    public void StartPaddleConfiguration(bool forLeftHand)
    {
        if (paddleConfigManager != null && !isPaddleHeld)
        {
            paddleConfigManager.StartConfiguration(forLeftHand);
        }
    }

    // 公共属性
    public bool IsPaddleHeld => isPaddleHeld;
    public bool IsLeftHandHoldingPaddle => isLeftHandHoldingPaddle;
    public InputState CurrentInputState => currentInputState;

    /// <summary>
    /// 获取当前输入状态（为DebugUI提供接口）
    /// </summary>
    public InputState GetCurrentInputState()
    {
        return currentInputState;
    }

    private void OnDestroy()
    {
        // 清理事件订阅（避免内存泄漏）
        OnPaddleGrabbed = null;
        OnPaddleReleased = null;
        OnBallGenerated = null;
        OnTeleportPerformed = null;
    }
}