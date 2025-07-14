using UnityEngine;
using UnityEngine.InputSystem;
using System;
using PongHub.Gameplay.Paddle;
using PongHub.Gameplay.Ball;
using PongHub.VR;
using PongHub.Arena.VFX;
using PongHub.Arena.Player;
using PongHub.Arena.Spectator;
using PongHub.App;
using static UnityEngine.InputSystem.InputAction;

namespace PongHub.Input
{
    /// <summary>
    /// 输入状态结构，用于调试和状态查询
    /// </summary>
    [System.Serializable]
    public struct InputState
    {
        public Vector2 leftStick, rightStick;
        public bool leftButtonA, leftButtonB, leftButtonMeta;
        public bool rightButtonA, rightButtonB, rightButtonMeta;
        public float leftGrip, rightGrip;
        public float leftTrigger, rightTrigger;
        public bool leftAB, rightAB; // 组合键
    }

    /// <summary>
    /// PongHub主输入管理器 - 完全替代PlayerInputController
    /// 采用混合模式：事件驱动的离散输入 + 优化的连续输入轮询
    /// 包含观战者模式、快速转向、移动特效等完整功能
    /// </summary>
    public class PongHubInputManager : MonoBehaviour
    {
        [Header("输入动作配置")]
        [SerializeField]
        [Tooltip("Input Actions / 输入动作 - Input action asset containing all input bindings")]
        private InputActionAsset m_inputActions;

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Player Rig / 玩家装备 - Player rig transform")]
        private Transform m_playerRig;

        [SerializeField]
        [Tooltip("Left Hand Anchor / 左手锚点 - Transform anchor for left hand")]
        private Transform m_leftHandAnchor;

        [SerializeField]
        [Tooltip("Right Hand Anchor / 右手锚点 - Transform anchor for right hand")]
        private Transform m_rightHandAnchor;

        [SerializeField]
        [Tooltip("Height Controller / 高度控制器 - Component for controlling player height")]
        private PlayerHeightController m_heightController;

        [SerializeField]
        [Tooltip("Teleport Controller / 传送控制器 - Component for teleportation system")]
        private TeleportController m_teleportController;

        [SerializeField]
        [Tooltip("Serve Ball Controller / 发球控制器 - Component for serving ball mechanics")]
        private ServeBallController m_serveBallController;

        [SerializeField]
        [Tooltip("Paddle Controller / 球拍控制器 - Component for paddle mechanics")]
        private PaddleController m_paddleController;

        [Header("移动设置")]
        [SerializeField]
        [Tooltip("Move Speed / 移动速度 - Speed of player movement")]
        private float m_moveSpeed = 3f;

        [SerializeField]
        [Tooltip("Dead Zone / 死区 - Input dead zone threshold")]
        private float m_deadZone = 0.1f;

        [SerializeField]
        [Tooltip("Snap Turn Angle / 快速转向角度 - Degrees to rotate for snap turn")]
        private float m_snapTurnAngle = 30f;

        [Header("性能优化设置")]
        [SerializeField]
        [Tooltip("Continuous Input Update Rate / 连续输入更新率 - Update rate for continuous input polling")]
        private float m_continuousInputUpdateRate = 120f; // 120Hz for VR

        [SerializeField]
        [Tooltip("Use Optimized Polling / 使用优化轮询 - Whether to use optimized input polling")]
        public bool m_useOptimizedPolling = true;

        [SerializeField]
        [Tooltip("Enable Performance Logging / 启用性能日志 - Whether to enable performance logging")]
        public bool m_enablePerformanceLogging = false;

        // 输入动作组
        private InputActionMap m_playerActions;
        private InputActionMap m_spectatorActions;

        // 玩家模式输入动作
        private InputAction m_moveAction;
        private InputAction m_leftPaddleGripAction;
        private InputAction m_rightPaddleGripAction;
        private InputAction m_generateServeBallLeftAction;
        private InputAction m_generateServeBallRightAction;
        private InputAction m_teleportControlAction;
        private InputAction m_heightUpAction;
        private InputAction m_heightDownAction;
        private InputAction m_menuAction;
        private InputAction m_pauseSinglePlayerAction;
        private InputAction m_resetPositionAction;
        private InputAction m_snapTurnLeftAction;
        private InputAction m_snapTurnRightAction;

        // 观战者模式输入动作
        private InputAction m_spectatorMoveAction;
        private InputAction m_spectatorMenuAction;
        private InputAction m_spectatorTriggerLeftAction;
        private InputAction m_spectatorTriggerRightAction;

        // 观战者模式支持
        private SpectatorNetwork m_spectatorNet = null;

        // 状态管理
        private bool m_isLeftPaddleGripped = false;
        private bool m_isRightPaddleGripped = false;
        private Vector2 m_currentMoveInput = Vector2.zero;
        private Vector2 m_currentTeleportInput = Vector2.zero;
        private InputState m_currentInputState = new InputState();

        // 输入控制状态
        private bool m_inputEnabled = true;
        private bool m_movementEnabled = true;
        private bool m_freeLocomotionEnabled = true;

        // 移动状态跟踪
        private bool m_wasMoving = false;
        private InputAction m_currentMoveActionRef = null;

        // 性能优化变量
        private float m_lastContinuousInputUpdate = 0f;
        private float m_continuousInputInterval;

        // 缓存变量，减少GC分配
        private Vector2 m_cachedMoveInput;
        private Vector2 m_cachedTeleportInput;

        // 事件定义 - 兼容旧系统
        public static event Action<bool> OnPaddleGripped; // bool: isLeftHand
        public static event Action<bool> OnPaddleReleased; // bool: isLeftHand
        public static event Action<bool> OnServeBallGenerated; // bool: isLeftHand
        public static event Action OnMenuToggled;
        public static event Action OnGamePaused;
        public static event Action OnPositionReset;

        // 新增事件
        public static event Action<bool> OnSnapTurn; // bool: toRight

        // 单例实例
        public static PongHubInputManager Instance { get; private set; }

        // 属性 - 兼容PlayerInputController API
        public bool IsLeftPaddleGripped => m_isLeftPaddleGripped;
        public bool IsRightPaddleGripped => m_isRightPaddleGripped;
        public Vector2 CurrentMoveInput => m_currentMoveInput;
        public Vector2 CurrentTeleportInput => m_currentTeleportInput;
        public InputState CurrentInputState => m_currentInputState;

        /// <summary>
        /// 是否启用输入 - 兼容PlayerInputController
        /// </summary>
        public bool InputEnabled
        {
            get => m_inputEnabled;
            set => m_inputEnabled = value;
        }

        /// <summary>
        /// 是否启用移动 - 兼容PlayerInputController
        /// </summary>
        public bool MovementEnabled
        {
            get => m_movementEnabled;
            set => m_movementEnabled = value;
        }

        /// <summary>
        /// 获取当前输入状态（兼容旧API）
        /// </summary>
        /// <returns>当前输入状态</returns>
        public InputState GetCurrentInputState()
        {
            return m_currentInputState;
        }

        /// <summary>
        /// 设置观战者模式 - 兼容PlayerInputController API
        /// </summary>
        /// <param name="spectator">观战者网络组件</param>
        public void SetSpectatorMode(SpectatorNetwork spectator)
        {
            m_spectatorNet = spectator;

            if (m_spectatorNet != null)
            {
                SwitchToSpectatorMode();
            }
            else
            {
                SwitchToPlayerMode();
            }
        }

        /// <summary>
        /// 更新游戏设置 - 兼容PlayerInputController API
        /// </summary>
        public void OnSettingsUpdated()
        {
            m_freeLocomotionEnabled = !GameSettings.Instance.IsFreeLocomotionDisabled;
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.RotationEitherThumbstick = !m_freeLocomotionEnabled;
            }
        }

        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // 计算更新间隔
                m_continuousInputInterval = 1f / m_continuousInputUpdateRate;

                InitializeInputActions();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 初始化移动设置
            OnSettingsUpdated();
        }

        private void OnDestroy()
        {
            // 清理时重置设置
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.RotationEitherThumbstick = true;
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
            BindInputEvents();
        }

        private void OnDisable()
        {
            DisableInputActions();
            UnbindInputEvents();
        }

        private void Update()
        {
            // 处理观战者模式
            if (m_spectatorNet == null)
            {
                ProcessPlayerInput();
            }

            // 处理连续输入
            if (m_useOptimizedPolling)
            {
                HandleOptimizedContinuousInputs();
            }
            else
            {
                HandleContinuousInputs();
            }
        }

        /// <summary>
        /// 初始化输入动作
        /// </summary>
        private void InitializeInputActions()
        {
            if (m_inputActions == null)
            {
                Debug.LogError("Input Actions Asset is not assigned!");
                return;
            }

            // 获取动作组
            m_playerActions = m_inputActions.FindActionMap("Player");
            m_spectatorActions = m_inputActions.FindActionMap("Spectator");

            if (m_playerActions != null)
            {
                // 获取玩家模式动作
                m_moveAction = m_playerActions.FindAction("Move");
                m_leftPaddleGripAction = m_playerActions.FindAction("LeftPaddleGrip");
                m_rightPaddleGripAction = m_playerActions.FindAction("RightPaddleGrip");
                m_generateServeBallLeftAction = m_playerActions.FindAction("GenerateServeBallLeft");
                m_generateServeBallRightAction = m_playerActions.FindAction("GenerateServeBallRight");
                m_teleportControlAction = m_playerActions.FindAction("TeleportControl");
                m_heightUpAction = m_playerActions.FindAction("HeightUp");
                m_heightDownAction = m_playerActions.FindAction("HeightDown");
                m_menuAction = m_playerActions.FindAction("Menu");
                m_pauseSinglePlayerAction = m_playerActions.FindAction("PauseSinglePlayer");
                m_resetPositionAction = m_playerActions.FindAction("ResetPosition");
                m_snapTurnLeftAction = m_playerActions.FindAction("SnapTurnLeft");
                m_snapTurnRightAction = m_playerActions.FindAction("SnapTurnRight");
            }

            if (m_spectatorActions != null)
            {
                // 获取观战者模式动作
                m_spectatorMoveAction = m_spectatorActions.FindAction("Move");
                m_spectatorMenuAction = m_spectatorActions.FindAction("Menu");
                m_spectatorTriggerLeftAction = m_spectatorActions.FindAction("TriggerLeft");
                m_spectatorTriggerRightAction = m_spectatorActions.FindAction("TriggerRight");
            }
        }

        /// <summary>
        /// 启用输入动作
        /// </summary>
        private void EnableInputActions()
        {
            m_playerActions?.Enable();
        }

        /// <summary>
        /// 禁用输入动作
        /// </summary>
        private void DisableInputActions()
        {
            m_playerActions?.Disable();
            m_spectatorActions?.Disable();
        }

        /// <summary>
        /// 绑定输入事件
        /// </summary>
        private void BindInputEvents()
        {
            // 玩家模式事件绑定
            if (m_leftPaddleGripAction != null)
            {
                m_leftPaddleGripAction.performed += OnLeftPaddleGripPerformed;
                m_leftPaddleGripAction.canceled += OnLeftPaddleGripCanceled;
            }
            if (m_rightPaddleGripAction != null)
            {
                m_rightPaddleGripAction.performed += OnRightPaddleGripPerformed;
                m_rightPaddleGripAction.canceled += OnRightPaddleGripCanceled;
            }

            // 发球事件
            if (m_generateServeBallLeftAction != null)
                m_generateServeBallLeftAction.performed += OnGenerateServeBallLeft;
            if (m_generateServeBallRightAction != null)
                m_generateServeBallRightAction.performed += OnGenerateServeBallRight;

            // 移动事件 - 使用事件驱动模式兼容PlayerInputController
            if (m_moveAction != null)
            {
                m_moveAction.performed += OnMove;
                m_moveAction.canceled += OnMove;
            }

            // 快速转向事件
            if (m_snapTurnLeftAction != null)
                m_snapTurnLeftAction.performed += OnSnapTurnLeft;
            if (m_snapTurnRightAction != null)
                m_snapTurnRightAction.performed += OnSnapTurnRight;

            // 高度调整事件
            if (m_heightUpAction != null)
            {
                m_heightUpAction.performed += OnHeightUp;
                m_heightUpAction.canceled += OnHeightUpCanceled;
            }
            if (m_heightDownAction != null)
            {
                m_heightDownAction.performed += OnHeightDown;
                m_heightDownAction.canceled += OnHeightDownCanceled;
            }

            // 菜单和控制事件
            if (m_menuAction != null)
                m_menuAction.performed += OnMenuPerformed;
            if (m_pauseSinglePlayerAction != null)
                m_pauseSinglePlayerAction.performed += OnPauseSinglePlayerPerformed;
            if (m_resetPositionAction != null)
                m_resetPositionAction.performed += OnResetPositionPerformed;

            // 观战者模式事件绑定
            if (m_spectatorTriggerLeftAction != null)
                m_spectatorTriggerLeftAction.performed += OnSpectatorTriggerLeft;
            if (m_spectatorTriggerRightAction != null)
                m_spectatorTriggerRightAction.performed += OnSpectatorTriggerRight;
            if (m_spectatorMenuAction != null)
                m_spectatorMenuAction.performed += OnMenuPerformed;
        }

        /// <summary>
        /// 解绑输入事件
        /// </summary>
        private void UnbindInputEvents()
        {
            // 玩家模式事件解绑
            if (m_leftPaddleGripAction != null)
            {
                m_leftPaddleGripAction.performed -= OnLeftPaddleGripPerformed;
                m_leftPaddleGripAction.canceled -= OnLeftPaddleGripCanceled;
            }
            if (m_rightPaddleGripAction != null)
            {
                m_rightPaddleGripAction.performed -= OnRightPaddleGripPerformed;
                m_rightPaddleGripAction.canceled -= OnRightPaddleGripCanceled;
            }

            // 发球事件
            if (m_generateServeBallLeftAction != null)
                m_generateServeBallLeftAction.performed -= OnGenerateServeBallLeft;
            if (m_generateServeBallRightAction != null)
                m_generateServeBallRightAction.performed -= OnGenerateServeBallRight;

            // 移动事件
            if (m_moveAction != null)
            {
                m_moveAction.performed -= OnMove;
                m_moveAction.canceled -= OnMove;
            }

            // 快速转向事件
            if (m_snapTurnLeftAction != null)
                m_snapTurnLeftAction.performed -= OnSnapTurnLeft;
            if (m_snapTurnRightAction != null)
                m_snapTurnRightAction.performed -= OnSnapTurnRight;

            // 高度调整事件
            if (m_heightUpAction != null)
            {
                m_heightUpAction.performed -= OnHeightUp;
                m_heightUpAction.canceled -= OnHeightUpCanceled;
            }
            if (m_heightDownAction != null)
            {
                m_heightDownAction.performed -= OnHeightDown;
                m_heightDownAction.canceled -= OnHeightDownCanceled;
            }

            // 菜单和控制事件
            if (m_menuAction != null)
                m_menuAction.performed -= OnMenuPerformed;
            if (m_pauseSinglePlayerAction != null)
                m_pauseSinglePlayerAction.performed -= OnPauseSinglePlayerPerformed;
            if (m_resetPositionAction != null)
                m_resetPositionAction.performed -= OnResetPositionPerformed;

            // 观战者模式事件解绑
            if (m_spectatorTriggerLeftAction != null)
                m_spectatorTriggerLeftAction.performed -= OnSpectatorTriggerLeft;
            if (m_spectatorTriggerRightAction != null)
                m_spectatorTriggerRightAction.performed -= OnSpectatorTriggerRight;
            if (m_spectatorMenuAction != null)
                m_spectatorMenuAction.performed -= OnMenuPerformed;
        }

        /// <summary>
        /// 处理连续输入（移动、传送等）
        /// </summary>
        private void HandleContinuousInputs()
        {
            // 处理传送控制输入
            Vector2 teleportInput = m_teleportControlAction?.ReadValue<Vector2>() ?? Vector2.zero;
            m_currentTeleportInput = teleportInput;

            if (m_teleportController != null)
            {
                m_teleportController.HandleTeleportInput(teleportInput);
            }

            // 更新输入状态结构（用于调试和兼容性）
            UpdateInputState();
        }

        /// <summary>
        /// 🚀 优化的连续输入处理 - 限制更新频率，减少CPU开销
        /// </summary>
        private void HandleOptimizedContinuousInputs()
        {
            float currentTime = Time.unscaledTime;

            // 只在指定间隔后更新连续输入
            if (currentTime - m_lastContinuousInputUpdate >= m_continuousInputInterval)
            {
                m_lastContinuousInputUpdate = currentTime;

                // 性能监控
                var startTime = Time.realtimeSinceStartup;
                ProcessContinuousInputsOptimized();
                var duration = Time.realtimeSinceStartup - startTime;

                // 记录性能数据
                RecordPerformanceData(duration);

                // 详细性能日志（仅在启用时）
                if (m_enablePerformanceLogging && duration > 0.0005f) // 超过0.5ms记录
                {
                    Debug.LogWarning($"[PongHubInputManager] 连续输入处理耗时: {duration * 1000f:F2}ms");
                }
            }
        }

        /// <summary>
        /// 处理优化的连续输入 - 减少每帧ReadValue调用
        /// </summary>
        private void ProcessContinuousInputsOptimized()
        {
            bool hasTeleportChanged = false;

            // 缓存当前传送输入值，避免重复ReadValue调用
            m_cachedTeleportInput = m_teleportControlAction?.ReadValue<Vector2>() ?? Vector2.zero;

            // 检查传送输入变化
            if ((m_cachedTeleportInput - m_currentTeleportInput).sqrMagnitude > 0.001f)
            {
                hasTeleportChanged = true;
                m_currentTeleportInput = m_cachedTeleportInput;
            }

            if (hasTeleportChanged && m_teleportController != null)
            {
                m_teleportController.HandleTeleportInput(m_currentTeleportInput);
            }

            // 减少UpdateInputState的调用频率
            if (hasTeleportChanged)
            {
                UpdateInputStateOptimized();
            }
        }

        /// <summary>
        /// 优化的输入状态更新 - 减少不必要的ReadValue调用
        /// </summary>
        private void UpdateInputStateOptimized()
        {
            // 摇杆输入（使用已缓存的值）
            m_currentInputState.leftStick = m_currentMoveInput;
            m_currentInputState.rightStick = m_cachedTeleportInput;

            // 握力状态（使用状态标记，避免ReadValue）
            m_currentInputState.leftGrip = m_isLeftPaddleGripped ? 1.0f : 0.0f;
            m_currentInputState.rightGrip = m_isRightPaddleGripped ? 1.0f : 0.0f;

            // 扳机状态（仅在需要时读取）
            if (m_generateServeBallLeftAction?.WasPressedThisFrame() == true ||
                m_generateServeBallRightAction?.WasPressedThisFrame() == true)
            {
                m_currentInputState.leftTrigger = m_generateServeBallLeftAction?.ReadValue<float>() ?? 0.0f;
                m_currentInputState.rightTrigger = m_generateServeBallRightAction?.ReadValue<float>() ?? 0.0f;
            }
        }

        /// <summary>
        /// 更新输入状态结构
        /// </summary>
        private void UpdateInputState()
        {
            // 摇杆输入
            m_currentInputState.leftStick = m_currentMoveInput;
            m_currentInputState.rightStick = m_teleportControlAction?.ReadValue<Vector2>() ?? Vector2.zero;

            // 握力状态（从Grip动作推导）
            m_currentInputState.leftGrip = m_isLeftPaddleGripped ? 1.0f : 0.0f;
            m_currentInputState.rightGrip = m_isRightPaddleGripped ? 1.0f : 0.0f;

            // 扳机状态（从发球动作推导）
            m_currentInputState.leftTrigger = m_generateServeBallLeftAction?.ReadValue<float>() ?? 0.0f;
            m_currentInputState.rightTrigger = m_generateServeBallRightAction?.ReadValue<float>() ?? 0.0f;

            // 按钮状态（从现有动作推导）
            m_currentInputState.leftButtonA = m_heightUpAction?.ReadValue<float>() > 0.5f;
            m_currentInputState.leftButtonB = m_heightDownAction?.ReadValue<float>() > 0.5f;
            m_currentInputState.leftButtonMeta = m_menuAction?.ReadValue<float>() > 0.5f;
            m_currentInputState.rightButtonA = m_pauseSinglePlayerAction?.ReadValue<float>() > 0.5f;
            m_currentInputState.rightButtonB = false; // 暂时没有对应的动作
            m_currentInputState.rightButtonMeta = m_resetPositionAction?.ReadValue<float>() > 0.5f;

            // 组合键检测
            m_currentInputState.leftAB = m_currentInputState.leftButtonA && m_currentInputState.leftButtonB;
            m_currentInputState.rightAB = m_currentInputState.rightButtonA && m_currentInputState.rightButtonB;
        }

        /// <summary>
        /// 处理玩家移动输入 - 兼容PlayerInputController的移动处理
        /// </summary>
        private void ProcessPlayerInput()
        {
            if (!m_inputEnabled)
            {
                if (m_wasMoving)
                {
                    ScreenFXManager.Instance?.ShowLocomotionFX(false);
                    m_wasMoving = false;
                }
                return;
            }

            if (m_movementEnabled && m_freeLocomotionEnabled)
            {
                var direction = m_currentMoveActionRef?.ReadValue<Vector2>() ?? Vector2.zero;
                if (direction != Vector2.zero)
                {
                    var dir = new Vector3(direction.x, 0, direction.y);
                    if (PlayerMovement.Instance != null)
                    {
                        PlayerMovement.Instance.WalkInDirectionRelToForward(dir);
                    }

                    if (!m_wasMoving && ScreenFXManager.Instance != null)
                    {
                        ScreenFXManager.Instance.ShowLocomotionFX(true);
                    }

                    m_wasMoving = true;
                }
                else if (m_wasMoving)
                {
                    if (ScreenFXManager.Instance != null)
                    {
                        ScreenFXManager.Instance.ShowLocomotionFX(false);
                    }
                    m_wasMoving = false;
                }
            }
        }

        /// <summary>
        /// 处理玩家移动（基础版本）
        /// </summary>
        private void HandleMovement(Vector2 moveInput)
        {
            if (m_playerRig == null) return;

            // 计算移动方向（相对于玩家朝向）
            Vector3 forward = m_playerRig.forward;
            Vector3 right = m_playerRig.right;

            // 消除Y轴分量，保持在水平面移动
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // 计算最终移动向量
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            Vector3 moveVector = moveDirection * m_moveSpeed * Time.deltaTime;

            // 应用移动
            m_playerRig.position += moveVector;
        }

        #region 球拍控制事件处理

        private void OnLeftPaddleGripPerformed(InputAction.CallbackContext context)
        {
            m_isLeftPaddleGripped = true;
            m_paddleController?.GripPaddle(true);
            OnPaddleGripped?.Invoke(true);
            Debug.Log("左手球拍已抓取");
        }

        private void OnLeftPaddleGripCanceled(InputAction.CallbackContext context)
        {
            m_isLeftPaddleGripped = false;
            m_paddleController?.ReleasePaddle(true);
            OnPaddleReleased?.Invoke(true);
            Debug.Log("左手球拍已释放");
        }

        private void OnRightPaddleGripPerformed(InputAction.CallbackContext context)
        {
            m_isRightPaddleGripped = true;
            m_paddleController?.GripPaddle(false);
            OnPaddleGripped?.Invoke(false);
            Debug.Log("右手球拍已抓取");
        }

        private void OnRightPaddleGripCanceled(InputAction.CallbackContext context)
        {
            m_isRightPaddleGripped = false;
            m_paddleController?.ReleasePaddle(false);
            OnPaddleReleased?.Invoke(false);
            Debug.Log("右手球拍已释放");
        }

        #endregion

        #region 发球事件处理

        private void OnGenerateServeBallLeft(InputAction.CallbackContext context)
        {
            // 只有在右手持拍（左手非持拍）时才能左手发球
            if (!m_isLeftPaddleGripped && m_isRightPaddleGripped)
            {
                m_serveBallController?.GenerateServeBall(true);
                OnServeBallGenerated?.Invoke(true);
                Debug.Log("左手发球");
            }
            else
            {
                Debug.Log("发球条件不满足：左手必须非持拍状态，右手必须持拍");
            }
        }

        private void OnGenerateServeBallRight(InputAction.CallbackContext context)
        {
            // 只有在左手持拍（右手非持拍）时才能右手发球
            if (!m_isRightPaddleGripped && m_isLeftPaddleGripped)
            {
                m_serveBallController?.GenerateServeBall(false);
                OnServeBallGenerated?.Invoke(false);
                Debug.Log("右手发球");
            }
            else
            {
                Debug.Log("发球条件不满足：右手必须非持拍状态，左手必须持拍");
            }
        }

        #endregion

        #region 移动相关事件处理

        /// <summary>
        /// 处理移动输入 - 兼容PlayerInputController的事件模式
        /// </summary>
        private void OnMove(CallbackContext context)
        {
            m_currentMoveActionRef = context.phase is InputActionPhase.Disabled ? null : context.action;

            // 立即更新移动输入值
            if (m_currentMoveActionRef != null)
            {
                m_currentMoveInput = m_currentMoveActionRef.ReadValue<Vector2>();
            }
            else
            {
                m_currentMoveInput = Vector2.zero;
            }
        }

        /// <summary>
        /// 处理左快速转向
        /// </summary>
        private void OnSnapTurnLeft(CallbackContext context)
        {
            if (context.performed)
            {
                HandleSnapTurn(context, false);
            }
        }

        /// <summary>
        /// 处理右快速转向
        /// </summary>
        private void OnSnapTurnRight(CallbackContext context)
        {
            if (context.performed)
            {
                HandleSnapTurn(context, true);
            }
        }

        /// <summary>
        /// 执行快速转向 - 兼容PlayerInputController
        /// </summary>
        private void HandleSnapTurn(CallbackContext context, bool toRight)
        {
            if (context.performed && PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.DoSnapTurn(toRight);
                OnSnapTurn?.Invoke(toRight);
            }
        }

        #endregion

        #region 高度调整事件处理

        private void OnHeightUp(InputAction.CallbackContext context)
        {
            m_heightController?.StartHeightAdjustment(true);
        }

        private void OnHeightUpCanceled(InputAction.CallbackContext context)
        {
            m_heightController?.StopHeightAdjustment();
        }

        private void OnHeightDown(InputAction.CallbackContext context)
        {
            m_heightController?.StartHeightAdjustment(false);
        }

        private void OnHeightDownCanceled(InputAction.CallbackContext context)
        {
            m_heightController?.StopHeightAdjustment();
        }

        #endregion

        #region 菜单和控制事件处理

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            OnMenuToggled?.Invoke();
            Debug.Log("菜单切换");
        }

        private void OnPauseSinglePlayerPerformed(InputAction.CallbackContext context)
        {
            OnGamePaused?.Invoke();
            Debug.Log("游戏暂停");
        }

        private void OnResetPositionPerformed(InputAction.CallbackContext context)
        {
            OnPositionReset?.Invoke();
            Debug.Log("位置重置");
        }

        #endregion

        #region 观战者模式事件处理

        /// <summary>
        /// 处理观战者左扳机 - 兼容PlayerInputController API
        /// </summary>
        private void OnSpectatorTriggerLeft(InputAction.CallbackContext context)
        {
            if (context.phase is InputActionPhase.Performed)
            {
                m_spectatorNet?.TriggerLeftAction();
            }
        }

        /// <summary>
        /// 处理观战者右扳机 - 兼容PlayerInputController API
        /// </summary>
        private void OnSpectatorTriggerRight(InputAction.CallbackContext context)
        {
            if (context.phase is InputActionPhase.Performed)
            {
                m_spectatorNet?.TriggerRightAction();
            }
        }

        #endregion

        #region 模式切换

        /// <summary>
        /// 切换到观战者模式
        /// </summary>
        public void SwitchToSpectatorMode()
        {
            m_playerActions?.Disable();
            m_spectatorActions?.Enable();
        }

        /// <summary>
        /// 切换到玩家模式
        /// </summary>
        public void SwitchToPlayerMode()
        {
            m_spectatorActions?.Disable();
            m_playerActions?.Enable();
        }

        #endregion

        /// <summary>
        /// 获取手部锚点
        /// </summary>
        public Transform GetHandAnchor(bool isLeftHand)
        {
            return isLeftHand ? m_leftHandAnchor : m_rightHandAnchor;
        }

        #region 性能监控

        // 性能监控属性
        public float LastFrameCPUTime { get; private set; }
        public float ActualUpdateRate { get; private set; }

        private float m_performanceTimer = 0f;
        private int m_updateCount = 0;

        /// <summary>
        /// 记录性能数据
        /// </summary>
        private void RecordPerformanceData(float cpuTime)
        {
            LastFrameCPUTime = cpuTime * 1000000f; // 转换为微秒

            m_updateCount++;
            m_performanceTimer += Time.unscaledDeltaTime;

            // 每秒计算一次实际更新频率
            if (m_performanceTimer >= 1f)
            {
                ActualUpdateRate = m_updateCount / m_performanceTimer;
                m_updateCount = 0;
                m_performanceTimer = 0f;
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"CPU: {LastFrameCPUTime:F1}μs | Rate: {ActualUpdateRate:F1}Hz";
        }

        #endregion
    }
}