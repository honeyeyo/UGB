using UnityEngine;
using UnityEngine.InputSystem;
using System;
using PongHub.Gameplay.Paddle;
using PongHub.Gameplay.Ball;
using PongHub.VR;

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
    /// PongHub主输入管理器
    /// 处理所有VR控制器输入，包括移动、球拍控制、发球、传送、高度调整等
    /// </summary>
    public class PongHubInputManager : MonoBehaviour
    {
        [Header("输入动作配置")]
        [SerializeField] private InputActionAsset m_inputActions;

        [Header("组件引用")]
        [SerializeField] private Transform m_playerRig;
        [SerializeField] private Transform m_leftHandAnchor;
        [SerializeField] private Transform m_rightHandAnchor;
        [SerializeField] private PlayerHeightController m_heightController;
        [SerializeField] private TeleportController m_teleportController;
        [SerializeField] private ServeBallController m_serveBallController;
        [SerializeField] private PaddleController m_paddleController;

        [Header("移动设置")]
        [SerializeField] private float m_moveSpeed = 3f;
        [SerializeField] private float m_deadZone = 0.1f;

        // 输入动作组
        private InputActionMap m_playerActions;
        private InputActionMap m_spectatorActions;

        // 具体输入动作
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

        // 状态管理
        private bool m_isLeftPaddleGripped = false;
        private bool m_isRightPaddleGripped = false;
        private Vector2 m_currentMoveInput = Vector2.zero;
        private Vector2 m_currentTeleportInput = Vector2.zero;
        private InputState m_currentInputState = new InputState();

        // 事件定义
        public static event Action<bool> OnPaddleGripped; // bool: isLeftHand
        public static event Action<bool> OnPaddleReleased; // bool: isLeftHand
        public static event Action<bool> OnServeBallGenerated; // bool: isLeftHand
        public static event Action OnMenuToggled;
        public static event Action OnGamePaused;
        public static event Action OnPositionReset;

        // 单例实例
        public static PongHubInputManager Instance { get; private set; }

        // 属性
        public bool IsLeftPaddleGripped => m_isLeftPaddleGripped;
        public bool IsRightPaddleGripped => m_isRightPaddleGripped;
        public Vector2 CurrentMoveInput => m_currentMoveInput;
        public Vector2 CurrentTeleportInput => m_currentTeleportInput;
        public InputState CurrentInputState => m_currentInputState;

        /// <summary>
        /// 获取当前输入状态（兼容旧API）
        /// </summary>
        /// <returns>当前输入状态</returns>
        public InputState GetCurrentInputState()
        {
            return m_currentInputState;
        }

        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInputActions();
            }
            else
            {
                Destroy(gameObject);
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
            HandleContinuousInputs();
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

            // 获取具体动作
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
        }

        /// <summary>
        /// 绑定输入事件
        /// </summary>
        private void BindInputEvents()
        {
            // 球拍抓取事件
            m_leftPaddleGripAction.performed += OnLeftPaddleGripPerformed;
            m_leftPaddleGripAction.canceled += OnLeftPaddleGripCanceled;
            m_rightPaddleGripAction.performed += OnRightPaddleGripPerformed;
            m_rightPaddleGripAction.canceled += OnRightPaddleGripCanceled;

            // 发球事件
            m_generateServeBallLeftAction.performed += OnGenerateServeBallLeft;
            m_generateServeBallRightAction.performed += OnGenerateServeBallRight;

            // 高度调整事件
            m_heightUpAction.performed += OnHeightUp;
            m_heightUpAction.canceled += OnHeightUpCanceled;
            m_heightDownAction.performed += OnHeightDown;
            m_heightDownAction.canceled += OnHeightDownCanceled;

            // 菜单和控制事件
            m_menuAction.performed += OnMenuPerformed;
            m_pauseSinglePlayerAction.performed += OnPauseSinglePlayerPerformed;
            m_resetPositionAction.performed += OnResetPositionPerformed;
        }

        /// <summary>
        /// 解绑输入事件
        /// </summary>
        private void UnbindInputEvents()
        {
            // 球拍抓取事件
            m_leftPaddleGripAction.performed -= OnLeftPaddleGripPerformed;
            m_leftPaddleGripAction.canceled -= OnLeftPaddleGripCanceled;
            m_rightPaddleGripAction.performed -= OnRightPaddleGripPerformed;
            m_rightPaddleGripAction.canceled -= OnRightPaddleGripCanceled;

            // 发球事件
            m_generateServeBallLeftAction.performed -= OnGenerateServeBallLeft;
            m_generateServeBallRightAction.performed -= OnGenerateServeBallRight;

            // 高度调整事件
            m_heightUpAction.performed -= OnHeightUp;
            m_heightUpAction.canceled -= OnHeightUpCanceled;
            m_heightDownAction.performed -= OnHeightDown;
            m_heightDownAction.canceled -= OnHeightDownCanceled;

            // 菜单和控制事件
            m_menuAction.performed -= OnMenuPerformed;
            m_pauseSinglePlayerAction.performed -= OnPauseSinglePlayerPerformed;
            m_resetPositionAction.performed -= OnResetPositionPerformed;
        }

        /// <summary>
        /// 处理连续输入（移动、传送等）
        /// </summary>
        private void HandleContinuousInputs()
        {
            // 处理移动输入
            Vector2 moveInput = m_moveAction.ReadValue<Vector2>();
            if (moveInput.magnitude > m_deadZone)
            {
                m_currentMoveInput = moveInput;
                HandleMovement(moveInput);
            }
            else
            {
                m_currentMoveInput = Vector2.zero;
            }

            // 处理传送控制输入
            Vector2 teleportInput = m_teleportControlAction.ReadValue<Vector2>();
            m_currentTeleportInput = teleportInput;

            if (m_teleportController != null)
            {
                m_teleportController.HandleTeleportInput(teleportInput);
            }

            // 更新输入状态结构（用于调试和兼容性）
            UpdateInputState();
        }

        /// <summary>
        /// 更新输入状态结构
        /// </summary>
        private void UpdateInputState()
        {
            // 摇杆输入
            m_currentInputState.leftStick = m_moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
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
        /// 处理玩家移动
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

        /// <summary>
        /// 获取手部锚点
        /// </summary>
        public Transform GetHandAnchor(bool isLeftHand)
        {
            return isLeftHand ? m_leftHandAnchor : m_rightHandAnchor;
        }
    }
}