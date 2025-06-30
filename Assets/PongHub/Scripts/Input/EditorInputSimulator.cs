using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using PongHub.Gameplay.Serve;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PongHub.Input
{
    /// <summary>
    /// Editor输入模拟器
    /// 在Unity Editor中使用键盘和鼠标模拟VR控制器输入
    /// 支持头显和双手控制器的模拟
    /// </summary>
    public class EditorInputSimulator : MonoBehaviour
    {
        #region 设置配置
        [Header("模拟器设置")]
        [SerializeField] private bool enableInEditor = true;
        [SerializeField] private bool enableInBuild = false;
        [SerializeField] private bool showInstructions = true;
        [SerializeField] private bool verboseLogging = false;

        [Header("头显模拟")]
        [SerializeField] private float headMoveSpeed = 2f;
        [SerializeField] private float headRotateSpeed = 100f;
        [SerializeField] private KeyCode headMoveUpKey = KeyCode.Q;
        [SerializeField] private KeyCode headMoveDownKey = KeyCode.E;

        [Header("控制器模拟")]
        [SerializeField] private float controllerMoveSpeed = 1f;
        [SerializeField] private float controllerRotateSpeed = 90f;
        [SerializeField] private Transform leftControllerTransform;
        [SerializeField] private Transform rightControllerTransform;
        [SerializeField] private Transform headTransform;

        [Header("乒乓球输入映射")]
        [SerializeField] private KeyCode leftPaddleHitKey = KeyCode.Q;        // 左拍击球
        [SerializeField] private KeyCode rightPaddleHitKey = KeyCode.E;       // 右拍击球
        [SerializeField] private KeyCode serveKey = KeyCode.Space;            // 发球
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;             // 左移
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;            // 右移
        [SerializeField] private KeyCode moveForwardKey = KeyCode.W;          // 前移
        [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;         // 后移

        [Header("调试功能")]
        [SerializeField] private KeyCode togglePauseKey = KeyCode.P;          // 暂停
        [SerializeField] private KeyCode resetPositionKey = KeyCode.R;        // 重置位置
        [SerializeField] private KeyCode toggleMenuKey = KeyCode.Tab;         // 菜单
        [SerializeField] private KeyCode toggleInstructionsKey = KeyCode.F1;  // 帮助

        [Header("默认位置")]
        [SerializeField] private Vector3 defaultHeadPosition = new Vector3(0, 1.6f, 0);
        [SerializeField] private Vector3 defaultLeftHandPosition = new Vector3(-0.3f, 1.3f, 0.3f);
        [SerializeField] private Vector3 defaultRightHandPosition = new Vector3(0.3f, 1.3f, 0.3f);
        #endregion

        #region 私有变量
        private bool isActive = false;
        private bool leftControllerActive = false;
        private bool rightControllerActive = false;
        private Vector3 lastMousePosition;
        private bool isMouseLookActive = false;

        // 输入状态
        private Vector2 currentMoveInput;
        private bool leftTriggerPressed = false;
        private bool rightTriggerPressed = false;
        private bool leftGripPressed = false;
        private bool rightGripPressed = false;
        private bool menuPressed = false;

        // 组件引用
        private ServeValidator serveValidator;
        private Camera mainCamera;
        private XROrigin xrOrigin;
        #endregion

        #region 事件系统
        public static System.Action<bool> OnLeftPaddleHit;
        public static System.Action<bool> OnRightPaddleHit;
        public static System.Action<Vector3, Vector3> OnServeAction; // 位置, 速度
        public static System.Action<Vector2> OnMoveInput;
        public static System.Action<bool> OnMenuPressed;
        public static System.Action OnPositionReset;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeSimulator();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;

            if (enableInEditor && !IsVRActive())
            {
                HandleEditorInput();
            }
#else
            if (enableInBuild && !IsVRActive())
            {
                HandleEditorInput();
            }
#endif
        }

        private void OnGUI()
        {
            if (showInstructions && isActive)
            {
                DrawInstructions();
            }
        }

        private void OnDestroy()
        {
            CleanupEvents();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化模拟器
        /// </summary>
        private void InitializeSimulator()
        {
            // 检查是否应该激活
#if UNITY_EDITOR
            isActive = enableInEditor && !IsVRActive();
#else
            isActive = enableInBuild && !IsVRActive();
#endif

            if (!isActive)
            {
                if (verboseLogging)
                    Debug.Log("[EditorInputSimulator] VR设备已连接或模拟器已禁用");
                return;
            }

            // 获取组件引用
            FindRequiredComponents();

            // 设置默认位置
            ResetToDefaultPositions();

            // 设置鼠标
            SetupMouseControl();

            if (verboseLogging)
                Debug.Log("[EditorInputSimulator] Editor输入模拟器已启动");
        }

        /// <summary>
        /// 查找必要的组件
        /// </summary>
        private void FindRequiredComponents()
        {
            // 查找ServeValidator
            serveValidator = FindObjectOfType<ServeValidator>();

            // 查找主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            // 查找XROrigin
            xrOrigin = FindObjectOfType<XROrigin>();

            // 如果没有指定变换，尝试自动查找
            if (headTransform == null && mainCamera != null)
                headTransform = mainCamera.transform;

            if (leftControllerTransform == null || rightControllerTransform == null)
            {
                // 尝试从XROrigin查找控制器
                if (xrOrigin != null)
                {
                    var controllers = xrOrigin.GetComponentsInChildren<Transform>();
                    foreach (var controller in controllers)
                    {
                        if (controller.name.ToLower().Contains("left") && leftControllerTransform == null)
                            leftControllerTransform = controller;
                        else if (controller.name.ToLower().Contains("right") && rightControllerTransform == null)
                            rightControllerTransform = controller;
                    }
                }
            }

            // 如果仍然没有找到，创建虚拟控制器
            if (leftControllerTransform == null)
                leftControllerTransform = CreateVirtualController("LeftController");
            if (rightControllerTransform == null)
                rightControllerTransform = CreateVirtualController("RightController");
        }

        /// <summary>
        /// 创建虚拟控制器
        /// </summary>
        private Transform CreateVirtualController(string name)
        {
            var controllerObj = new GameObject(name + "_Simulated");
            controllerObj.transform.parent = transform;

            if (verboseLogging)
                Debug.Log($"[EditorInputSimulator] 创建虚拟控制器: {name}");

            return controllerObj.transform;
        }

        /// <summary>
        /// 设置鼠标控制
        /// </summary>
        private void SetupMouseControl()
        {
            lastMousePosition = UnityEngine.Input.mousePosition;
        }
        #endregion

        #region 输入处理
        /// <summary>
        /// 处理Editor输入
        /// </summary>
        private void HandleEditorInput()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            HandleGameplayInput();
            UpdateVirtualControllers();
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            // 头部移动
            if (headTransform != null)
            {
                Vector3 headMovement = Vector3.zero;

                if (UnityEngine.Input.GetKey(moveForwardKey))
                    headMovement += headTransform.forward;
                if (UnityEngine.Input.GetKey(moveBackwardKey))
                    headMovement -= headTransform.forward;
                if (UnityEngine.Input.GetKey(moveLeftKey))
                    headMovement -= headTransform.right;
                if (UnityEngine.Input.GetKey(moveRightKey))
                    headMovement += headTransform.right;
                if (UnityEngine.Input.GetKey(headMoveUpKey))
                    headMovement += Vector3.up;
                if (UnityEngine.Input.GetKey(headMoveDownKey))
                    headMovement -= Vector3.up;

                if (headMovement != Vector3.zero)
                {
                    headTransform.position += headMovement * headMoveSpeed * Time.deltaTime;
                }
            }

            // 功能键
            if (UnityEngine.Input.GetKeyDown(togglePauseKey))
            {
                HandlePauseToggle();
            }

            if (UnityEngine.Input.GetKeyDown(resetPositionKey))
            {
                ResetToDefaultPositions();
                OnPositionReset?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(toggleMenuKey))
            {
                menuPressed = !menuPressed;
                OnMenuPressed?.Invoke(menuPressed);
            }

            if (UnityEngine.Input.GetKeyDown(toggleInstructionsKey))
            {
                showInstructions = !showInstructions;
            }
        }

        /// <summary>
        /// 处理鼠标输入
        /// </summary>
        private void HandleMouseInput()
        {
            // 鼠标右键进行视角旋转
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                isMouseLookActive = true;
                lastMousePosition = UnityEngine.Input.mousePosition;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                isMouseLookActive = false;
                Cursor.lockState = CursorLockMode.None;
            }

            // 鼠标视角控制
            if (isMouseLookActive && headTransform != null)
            {
                Vector2 mouseDelta = new Vector2(
                    UnityEngine.Input.GetAxis("Mouse X"),
                    UnityEngine.Input.GetAxis("Mouse Y")
                );

                Vector3 rotation = headTransform.eulerAngles;
                rotation.y += mouseDelta.x * headRotateSpeed * Time.deltaTime;
                rotation.x -= mouseDelta.y * headRotateSpeed * Time.deltaTime;
                rotation.x = ClampAngle(rotation.x, -90f, 90f);

                headTransform.eulerAngles = rotation;
            }

            // 控制器选择
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                leftControllerActive = true;
                if (verboseLogging)
                    Debug.Log("[EditorInputSimulator] 左控制器激活");
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                leftControllerActive = false;
            }

            if (UnityEngine.Input.GetMouseButtonDown(2)) // 中键
            {
                rightControllerActive = true;
                if (verboseLogging)
                    Debug.Log("[EditorInputSimulator] 右控制器激活");
            }
            else if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                rightControllerActive = false;
            }
        }

        /// <summary>
        /// 处理游戏相关输入
        /// </summary>
        private void HandleGameplayInput()
        {
            // 移动输入
            Vector2 moveInput = Vector2.zero;
            if (UnityEngine.Input.GetKey(moveLeftKey))
                moveInput.x -= 1f;
            if (UnityEngine.Input.GetKey(moveRightKey))
                moveInput.x += 1f;
            if (UnityEngine.Input.GetKey(moveForwardKey))
                moveInput.y += 1f;
            if (UnityEngine.Input.GetKey(moveBackwardKey))
                moveInput.y -= 1f;

            if (moveInput != currentMoveInput)
            {
                currentMoveInput = moveInput;
                OnMoveInput?.Invoke(currentMoveInput);
            }

            // 球拍击球
            if (UnityEngine.Input.GetKeyDown(leftPaddleHitKey))
            {
                leftTriggerPressed = true;
                OnLeftPaddleHit?.Invoke(true);
                if (verboseLogging)
                    Debug.Log("[EditorInputSimulator] 左拍击球");
            }
            else if (UnityEngine.Input.GetKeyUp(leftPaddleHitKey))
            {
                leftTriggerPressed = false;
                OnLeftPaddleHit?.Invoke(false);
            }

            if (UnityEngine.Input.GetKeyDown(rightPaddleHitKey))
            {
                rightTriggerPressed = true;
                OnRightPaddleHit?.Invoke(true);
                if (verboseLogging)
                    Debug.Log("[EditorInputSimulator] 右拍击球");
            }
            else if (UnityEngine.Input.GetKeyUp(rightPaddleHitKey))
            {
                rightTriggerPressed = false;
                OnRightPaddleHit?.Invoke(false);
            }

            // 发球
            if (UnityEngine.Input.GetKeyDown(serveKey))
            {
                HandleServeAction();
            }
        }

        /// <summary>
        /// 更新虚拟控制器位置
        /// </summary>
        private void UpdateVirtualControllers()
        {
            if (leftControllerTransform != null && leftControllerActive)
            {
                // 根据鼠标移动调整左控制器
                Vector2 mouseMovement = (Vector2)UnityEngine.Input.mousePosition - (Vector2)lastMousePosition;
                Vector3 controllerMovement = new Vector3(
                    mouseMovement.x * controllerMoveSpeed * Time.deltaTime * 0.01f,
                    -mouseMovement.y * controllerMoveSpeed * Time.deltaTime * 0.01f,
                    0
                );

                if (headTransform != null)
                {
                    leftControllerTransform.position += headTransform.TransformDirection(controllerMovement);
                }
            }

            if (rightControllerTransform != null && rightControllerActive)
            {
                // 类似地处理右控制器
                Vector2 mouseMovement = (Vector2)UnityEngine.Input.mousePosition - (Vector2)lastMousePosition;
                Vector3 controllerMovement = new Vector3(
                    mouseMovement.x * controllerMoveSpeed * Time.deltaTime * 0.01f,
                    -mouseMovement.y * controllerMoveSpeed * Time.deltaTime * 0.01f,
                    0
                );

                if (headTransform != null)
                {
                    rightControllerTransform.position += headTransform.TransformDirection(controllerMovement);
                }
            }

            lastMousePosition = UnityEngine.Input.mousePosition;
        }
        #endregion

        #region 游戏功能
        /// <summary>
        /// 处理发球动作
        /// </summary>
        private void HandleServeAction()
        {
            Vector3 servePosition = headTransform != null ?
                headTransform.position + headTransform.forward * 0.5f :
                transform.position;

            Vector3 serveVelocity = Vector3.up * 3f + Vector3.forward * 2f;

            // 验证发球（如果有验证器）
            if (serveValidator != null)
            {
                var result = serveValidator.ValidateServe(servePosition, serveVelocity);
                if (verboseLogging)
                {
                    Debug.Log($"[EditorInputSimulator] 发球验证结果: {(result.IsValid ? "有效" : "无效")}");
                    if (!result.IsValid)
                        Debug.Log($"失误原因: {result.FaultDescription}");
                }
            }

            OnServeAction?.Invoke(servePosition, serveVelocity);

            if (verboseLogging)
                Debug.Log($"[EditorInputSimulator] 模拟发球 - 位置: {servePosition}, 速度: {serveVelocity}");
        }

        /// <summary>
        /// 处理暂停切换
        /// </summary>
        private void HandlePauseToggle()
        {
            bool isPaused = Time.timeScale == 0f;
            Time.timeScale = isPaused ? 1f : 0f;

            if (verboseLogging)
                Debug.Log($"[EditorInputSimulator] 暂停状态: {(Time.timeScale == 0f ? "已暂停" : "已恢复")}");
        }

        /// <summary>
        /// 重置到默认位置
        /// </summary>
        private void ResetToDefaultPositions()
        {
            if (headTransform != null)
            {
                headTransform.position = defaultHeadPosition;
                headTransform.rotation = Quaternion.identity;
            }

            if (leftControllerTransform != null)
                leftControllerTransform.position = defaultLeftHandPosition;

            if (rightControllerTransform != null)
                rightControllerTransform.position = defaultRightHandPosition;

            if (verboseLogging)
                Debug.Log("[EditorInputSimulator] 已重置到默认位置");
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 检查VR是否激活
        /// </summary>
        private bool IsVRActive()
        {
            return XRSettings.isDeviceActive && XRSettings.loadedDeviceName != "None";
        }

        /// <summary>
        /// 限制角度范围
        /// </summary>
        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// 绘制指令说明
        /// </summary>
        private void DrawInstructions()
        {
            float yOffset = 10f;
            float lineHeight = 20f;
            float boxWidth = 400f;

            GUI.Box(new Rect(10, yOffset, boxWidth, 380), "Editor VR输入模拟器");
            yOffset += 25f;

            var instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            string[] instructions = {
                "=== 基本控制 ===",
                "右键拖拽: 视角旋转",
                "WASD: 头部移动",
                "Q/E: 头部上下移动",
                "",
                "=== 乒乓球控制 ===",
                $"{leftPaddleHitKey}: 左拍击球",
                $"{rightPaddleHitKey}: 右拍击球",
                $"{serveKey}: 发球",
                "",
                "=== 控制器 ===",
                "左键: 激活左控制器",
                "中键: 激活右控制器",
                "拖拽: 移动激活的控制器",
                "",
                "=== 功能键 ===",
                $"{togglePauseKey}: 暂停/继续",
                $"{resetPositionKey}: 重置位置",
                $"{toggleMenuKey}: 菜单",
                $"{toggleInstructionsKey}: 切换帮助显示",
                "",
                $"VR状态: {(IsVRActive() ? "已连接" : "未连接")}",
                $"模拟器状态: {(isActive ? "激活" : "未激活")}"
            };

            foreach (string instruction in instructions)
            {
                GUI.Label(new Rect(15, yOffset, boxWidth - 10, lineHeight), instruction, instructionStyle);
                yOffset += lineHeight;
            }
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        private void CleanupEvents()
        {
            OnLeftPaddleHit = null;
            OnRightPaddleHit = null;
            OnServeAction = null;
            OnMoveInput = null;
            OnMenuPressed = null;
            OnPositionReset = null;
        }
        #endregion

        #region 公共接口
        /// <summary>
        /// 手动启用/禁用模拟器
        /// </summary>
        public void SetSimulatorEnabled(bool enabled)
        {
            isActive = enabled && !IsVRActive();

            if (verboseLogging)
                Debug.Log($"[EditorInputSimulator] 模拟器状态设置为: {isActive}");
        }

        /// <summary>
        /// 获取当前输入状态
        /// </summary>
        public InputState GetCurrentInputState()
        {
            return new InputState
            {
                MoveInput = currentMoveInput,
                LeftTriggerPressed = leftTriggerPressed,
                RightTriggerPressed = rightTriggerPressed,
                LeftGripPressed = leftGripPressed,
                RightGripPressed = rightGripPressed,
                MenuPressed = menuPressed,
                HeadPosition = headTransform != null ? headTransform.position : Vector3.zero,
                HeadRotation = headTransform != null ? headTransform.rotation : Quaternion.identity,
                LeftHandPosition = leftControllerTransform != null ? leftControllerTransform.position : Vector3.zero,
                RightHandPosition = rightControllerTransform != null ? rightControllerTransform.position : Vector3.zero
            };
        }

        /// <summary>
        /// 设置控制器位置
        /// </summary>
        public void SetControllerPosition(bool isLeft, Vector3 position)
        {
            if (isLeft && leftControllerTransform != null)
                leftControllerTransform.position = position;
            else if (!isLeft && rightControllerTransform != null)
                rightControllerTransform.position = position;
        }
        #endregion

        #region 嵌套类型
        /// <summary>
        /// 输入状态结构
        /// </summary>
        [System.Serializable]
        public struct InputState
        {
            public Vector2 MoveInput;
            public bool LeftTriggerPressed;
            public bool RightTriggerPressed;
            public bool LeftGripPressed;
            public bool RightGripPressed;
            public bool MenuPressed;
            public Vector3 HeadPosition;
            public Quaternion HeadRotation;
            public Vector3 LeftHandPosition;
            public Vector3 RightHandPosition;
        }
        #endregion
    }
}