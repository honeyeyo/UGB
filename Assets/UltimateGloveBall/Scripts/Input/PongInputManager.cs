using UnityEngine;
using UnityEngine.XR;
using Meta.Utilities.Input;
using System.Collections;

public class PongInputManager : MonoBehaviour
{
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

    // 私有变量
    private bool isPaddleHeld = false;
    private bool isLeftHandHoldingPaddle = false;
    private GameObject currentPaddle;
    private float metaKeyHoldTimer = 0f;
    private float gripHoldTimer = 0f;
    private bool isGripHeld = false;

    private void Update()
    {
        HandleMovementInput();
        HandleTeleportInput();
        HandlePaddleInput();
        HandleBallGenerationInput();
        HandleMetaKeyInput();
        HandleConfigurationInput();
    }

    /// <summary>
    /// 处理移动和视角输入
    /// </summary>
    private void HandleMovementInput()
    {
        // 获取控制器动作
        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // 左手摇杆：前后左右移动
        Vector2 leftStick = leftActions.ButtonPrimaryThumbstick.action.ReadValue<Vector2>();
        if (leftStick.magnitude > 0.1f)
        {
            Vector3 moveDirection = cameraRig.forward * leftStick.y + cameraRig.right * leftStick.x;
            moveDirection.y = 0; // 保持水平移动
            playerRig.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 右手摇杆：左右旋转视角
        Vector2 rightStick = rightActions.ButtonPrimaryThumbstick.action.ReadValue<Vector2>();
        if (Mathf.Abs(rightStick.x) > 0.1f)
        {
            playerRig.Rotate(0, rightStick.x * rotationSpeed * Time.deltaTime, 0);
        }

        // 右手摇杆前推：瞬移
        if (rightStick.y > 0.8f)
        {
            PerformTeleport();
        }

        // 左手A、B键：上下移动
        bool leftAButton = leftActions.ButtonOne.action.ReadValue<float>() > 0.5f;
        bool leftBButton = leftActions.ButtonTwo.action.ReadValue<float>() > 0.5f;

        if (leftAButton)
        {
            playerRig.position += Vector3.up * heightChangeSpeed * Time.deltaTime;
        }
        if (leftBButton)
        {
            playerRig.position += Vector3.down * heightChangeSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 处理瞬移到固定点位
    /// </summary>
    private void HandleTeleportInput()
    {
        // 这里可以添加特定的瞬移逻辑，比如UI按钮触发
        // 或者通过特定手势触发瞬移到球桌两侧
    }

    /// <summary>
    /// 执行瞬移
    /// </summary>
    private void PerformTeleport()
    {
        Vector3 teleportPosition = cameraRig.position + cameraRig.forward * teleportDistance;
        teleportPosition.y = playerRig.position.y; // 保持当前高度
        playerRig.position = teleportPosition;
    }

    /// <summary>
    /// 处理球拍握持输入
    /// </summary>
    private void HandlePaddleInput()
    {
        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // 检查左手Grip
        float leftGrip = leftActions.AxisHandTrigger.action.ReadValue<float>();
        bool leftGripPressed = leftGrip > 0.8f;

        // 检查右手Grip
        float rightGrip = rightActions.AxisHandTrigger.action.ReadValue<float>();
        bool rightGripPressed = rightGrip > 0.8f;

        // 处理握持逻辑
        if ((leftGripPressed || rightGripPressed) && !isGripHeld)
        {
            isGripHeld = true;
            gripHoldTimer = 0f;
        }
        else if (!(leftGripPressed || rightGripPressed) && isGripHeld)
        {
            isGripHeld = false;
            gripHoldTimer = 0f;
        }

        if (isGripHeld)
        {
            gripHoldTimer += Time.deltaTime;

            if (gripHoldTimer >= gripHoldTime)
            {
                if (!isPaddleHeld)
                {
                    // 握持球拍
                    GrabPaddle(leftGripPressed);
                }
                else
                {
                    // 释放球拍
                    ReleasePaddle();
                }
                gripHoldTimer = 0f;
                isGripHeld = false;
            }
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

        // 实例化球拍
        currentPaddle = Instantiate(paddlePrefab);

        // 附加到相应的手部锚点
        Transform handAnchor = xrInputManager.GetAnchor(useLeftHand);
        currentPaddle.transform.SetParent(handAnchor);

        // 应用配置的位置和旋转
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

        Debug.Log("球拍已释放");
    }

    /// <summary>
    /// 处理球生成输入
    /// </summary>
    private void HandleBallGenerationInput()
    {
        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // 非持拍手的Trigger键生成球
        bool leftTrigger = leftActions.AxisIndexTrigger.action.ReadValue<float>() > 0.8f;
        bool rightTrigger = rightActions.AxisIndexTrigger.action.ReadValue<float>() > 0.8f;

        if (isPaddleHeld)
        {
            // 如果左手持拍，右手Trigger生成球
            if (isLeftHandHoldingPaddle && rightTrigger)
            {
                GenerateBall(false);
            }
            // 如果右手持拍，左手Trigger生成球
            else if (!isLeftHandHoldingPaddle && leftTrigger)
            {
                GenerateBall(true);
            }
        }
    }

    /// <summary>
    /// 生成球
    /// </summary>
    private void GenerateBall(bool fromLeftHand)
    {
        Transform handAnchor = xrInputManager.GetAnchor(fromLeftHand);

        // 在掌心位置生成球
        Vector3 spawnPosition = handAnchor.position + handAnchor.forward * 0.1f;
        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);

        // 给球添加一个小的向前推力
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.AddForce(handAnchor.forward * 2f, ForceMode.Impulse);
        }

        Debug.Log($"球已从{(fromLeftHand ? "左手" : "右手")}生成");
    }

    /// <summary>
    /// 处理Meta键长按回到出生点
    /// </summary>
    private void HandleMetaKeyInput()
    {
        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // 检查Meta键（通常是ButtonThree）
        bool leftMeta = leftActions.ButtonThree.action.ReadValue<float>() > 0.5f;
        bool rightMeta = rightActions.ButtonThree.action.ReadValue<float>() > 0.5f;

        if (leftMeta || rightMeta)
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
    /// 瞬移到默认出生点
    /// </summary>
    private void TeleportToDefaultSpawn()
    {
        if (defaultSpawnPoint != null)
        {
            playerRig.position = defaultSpawnPoint.position;
            playerRig.rotation = defaultSpawnPoint.rotation;
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
            Debug.Log($"已瞬移到点位 {pointIndex}");
        }
    }

    /// <summary>
    /// 获取当前是否持有球拍
    /// </summary>
    public bool IsPaddleHeld => isPaddleHeld;

    /// <summary>
    /// 获取持拍手信息
    /// </summary>
    public bool IsLeftHandHoldingPaddle => isLeftHandHoldingPaddle;

    /// <summary>
    /// 处理配置模式输入（同时按住A+B键进入配置）
    /// </summary>
    private void HandleConfigurationInput()
    {
        if (paddleConfigManager == null) return;

        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // 左手同时按住A+B键进入左手配置
        bool leftAB = leftActions.ButtonOne.action.ReadValue<float>() > 0.5f &&
                      leftActions.ButtonTwo.action.ReadValue<float>() > 0.5f;

        // 右手同时按住A+B键进入右手配置
        bool rightAB = rightActions.ButtonOne.action.ReadValue<float>() > 0.5f &&
                       rightActions.ButtonTwo.action.ReadValue<float>() > 0.5f;

        if (leftAB && !isPaddleHeld)
        {
            paddleConfigManager.StartConfiguration(true);
        }
        else if (rightAB && !isPaddleHeld)
        {
            paddleConfigManager.StartConfiguration(false);
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
}